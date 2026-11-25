using Microsoft.Data.Sqlite;

namespace StudentRegistry;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try{
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StudentRegistry");
            Directory.CreateDirectory(directory);
            var repository = new StudentRepository(Path.Combine(directory, "students.db"));
            repository.Initialize();
            Application.Run(new MainForm(repository));
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException){
            MessageBox.Show($"Не удалось открыть журнал\n\n{ex.Message}", "Журнал студентов", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
