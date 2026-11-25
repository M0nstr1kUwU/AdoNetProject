using System.Net.Mail;

namespace StudentRegistry;

internal sealed class StudentEditorForm : Form
{
    private readonly TextBox _name = new() { MaxLength = 100 };
    private readonly TextBox _group = new() { MaxLength = 24 };
    private readonly NumericUpDown _course = new() { Minimum = 1, Maximum = 6 };
    private readonly TextBox _email = new() { MaxLength = 120 };

    public StudentDetails? Details { get; private set; }

    public StudentEditorForm(StudentDetails? details)
    {
        Text = details is null ? "Добавить студента" : "Редактировать";
        Font = new Font("Segoe UI", 10);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(490, 240);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5,
            Padding = new Padding(16)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddField(fields, 0, "&ФИО", _name);
        AddField(fields, 1, "&Группа", _group);
        AddField(fields, 2, "&Курс", _course);
        AddField(fields, 3, "&Почта", _email);

        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var save = new Button { Text = "Сохранить", Size = new Size(108, 32) };
        var cancel = new Button { Text = "Отмена", Size = new Size(90, 32), DialogResult = DialogResult.Cancel };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0)
        };
        actions.Controls.AddRange([cancel, save]);
        fields.Controls.Add(actions, 0, 4);
        fields.SetColumnSpan(actions, 2);
        Controls.Add(fields);

        AcceptButton = save;
        CancelButton = cancel;
        save.Click += (_, _) => Save();
        if (details is not null)
        {
            _name.Text = details.FullName;
            _group.Text = details.GroupName;
            _course.Value = details.Course;
            _email.Text = details.Email;
        }
    }

    private static void AddField(TableLayoutPanel fields, int row, string text, Control input)
    {
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        fields.Controls.Add(new Label
        {
            Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        input.Anchor = input is TextBox ? AnchorStyles.Left | AnchorStyles.Right : AnchorStyles.Left;
        fields.Controls.Add(input, 1, row);
    }

    private void Save()
    {
        var name = _name.Text.Trim();
        var group = _group.Text.Trim().ToUpperInvariant();
        var email = _email.Text.Trim();
        if (name.Length < 2)
        {
            ShowError("ФИО", _name);
            return;
        }

        if (group.Length == 0)
        {
            ShowError("Группа", _group);
            return;
        }

        if (email.Length > 0 && (!MailAddress.TryCreate(email, out var address) || address.Address != email))
        {
            ShowError("Формат должен быть: student@example.com (или пустым)", _email);
            return;
        }

        Details = new StudentDetails(name, group, (int)_course.Value, email);
        DialogResult = DialogResult.OK;
    }

    private void ShowError(string message, Control input)
    {
        MessageBox.Show(this, message, "Что-то не так...", MessageBoxButtons.OK, MessageBoxIcon.Information);
        input.Focus();
    }
}
