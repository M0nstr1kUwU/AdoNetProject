using Microsoft.Data.Sqlite;

namespace StudentRegistry;

internal sealed class MainForm : Form
{
    private readonly StudentRepository _repository;
    private readonly TextBox _search = new() { Width = 260, PlaceholderText = "ФИО или электронная почта" };
    private readonly ComboBox _groups = new() { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _count = new() { AutoSize = true, Margin = new Padding(0, 9, 0, 0) };
    private readonly Button _edit = new() { Text = "Изменить", AutoSize = true };
    private readonly Button _delete = new() { Text = "Удалить", AutoSize = true };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        MultiSelect = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None
    };
    private bool _refreshingGroups;

    public MainForm(StudentRepository repository)
    {
        _repository = repository;
        Text = "Журнал студентов";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(920, 570);
        MinimumSize = new Size(720, 420);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(242, 245, 249);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Журнал студентов",
            Font = new Font(Font.FontFamily, 21, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        var filters = NewBar(new Padding(0, 0, 0, 10));
        filters.Controls.AddRange([
            new Label { Text = "Поиск", AutoSize = true, Margin = new Padding(0, 7, 4, 0) }, _search,
            new Label { Text = "Группа", AutoSize = true, Margin = new Padding(14, 7, 4, 0) }, _groups
        ]);

        var add = new Button
        {
            Text = "Добавить студента",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 74, 151),
            ForeColor = Color.White
        };
        var actions = NewBar(new Padding(0, 10, 0, 0));
        actions.Controls.AddRange([add, _edit, _delete, _count]);
        _count.Margin = new Padding(12, 8, 0, 0);

        AddColumn("ФИО", 45, 170);
        AddColumn("Группа", 18, 85);
        AddColumn("Курс", 10, 55);
        AddColumn("Почта", 27, 145);
        _grid.ColumnHeadersHeight = 34;
        _grid.RowTemplate.Height = 30;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 253);

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(filters, 0, 1);
        layout.Controls.Add(_grid, 0, 2);
        layout.Controls.Add(actions, 0, 3);
        Controls.Add(layout);

        add.Click += (_, _) => OpenEditor(null);
        _edit.Click += (_, _) => EditSelection();
        _delete.Click += (_, _) => DeleteSelection();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelection(); };
        _grid.SelectionChanged += (_, _) => UpdateActions();
        _search.TextChanged += (_, _) => RunDatabaseAction(() => RefreshStudents());
        _groups.SelectedIndexChanged += (_, _) =>
        {
            if (!_refreshingGroups)
                RunDatabaseAction(() => RefreshStudents());
        };

        RefreshGroups();
        RefreshStudents();
    }

    private Student? SelectedStudent => _grid.CurrentRow?.Tag as Student;

    private string? SelectedGroup => _groups.SelectedIndex > 0 ? _groups.SelectedItem as string : null;

    private static FlowLayoutPanel NewBar(Padding padding) => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        WrapContents = false,
        Padding = padding,
        Margin = Padding.Empty
    };

    private void AddColumn(string header, float weight, int minimumWidth)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            FillWeight = weight,
            MinimumWidth = minimumWidth,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void RefreshGroups()
    {
        var currentGroup = SelectedGroup;
        var groups = _repository.GetGroups();
        _refreshingGroups = true;
        try
        {
            _groups.Items.Clear();
            _groups.Items.Add("Все группы");
            _groups.Items.AddRange(groups.ToArray());
            _groups.SelectedIndex = currentGroup is null ? 0 : groups.IndexOf(currentGroup) + 1;
        }
        finally { _refreshingGroups = false;}
    }

    private void RefreshStudents(long? selectId = null)
    {
        var students = _repository.Find(_search.Text.Trim(), SelectedGroup);
        _grid.Rows.Clear();
        foreach (var student in students)
        {
            var details = student.Details;
            var index = _grid.Rows.Add(details.FullName, details.GroupName, details.Course, details.Email);
            var row = _grid.Rows[index];
            row.Tag = student;
            if (student.Id == selectId)
                _grid.CurrentCell = row.Cells[0];
        }

        _count.Text = $"Записей в списке: {students.Count}";
        UpdateActions();
    }

    private void UpdateActions() { _edit.Enabled = _delete.Enabled = SelectedStudent is not null; }

    private void EditSelection()
    {
        if (SelectedStudent is { } student)
            OpenEditor(student);
    }

    private void OpenEditor(Student? student)
    {
        using var editor = new StudentEditorForm(student?.Details);
        if (editor.ShowDialog(this) != DialogResult.OK || editor.Details is not { } details)
            return;

        RunDatabaseAction(() =>
        {
            long id;
            if (student is null)
                id = _repository.Add(details);
            else
            {
                id = student.Id;
                if (!_repository.Update(id, details))
                    MessageBox.Show(this, "Запись уже удалена", Text);
            }

            RefreshGroups();
            RefreshStudents(id);
        });
    }

    private void DeleteSelection()
    {
        if (SelectedStudent is not { } student)
            return;

        var answer = MessageBox.Show(this, $"Удалить запись «{student.Details.FullName}»?", "Удаление записи", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (answer == DialogResult.Yes)
        {
            RunDatabaseAction(() =>
            {
                if (!_repository.Delete(student.Id))
                    MessageBox.Show(this, "Запись уже удалена", Text);

                RefreshGroups();
                RefreshStudents();
            });
        }
    }

    private void InitializeComponent() { }

    private void RunDatabaseAction(Action action)
    {
        try { action(); }
        catch (SqliteException ex)
        {
            MessageBox.Show(this, $"Ошибка db\n\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
