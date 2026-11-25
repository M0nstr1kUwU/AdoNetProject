using Microsoft.Data.Sqlite;

namespace StudentRegistry;

internal sealed class StudentRepository(string databasePath)
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder{ DataSource = databasePath }.ToString();

    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS students (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                full_name TEXT NOT NULL CHECK (length(trim(full_name)) BETWEEN 2 AND 100),
                group_name TEXT NOT NULL CHECK (length(trim(group_name)) BETWEEN 1 AND 24),
                course INTEGER NOT NULL CHECK (course BETWEEN 1 AND 6),
                email TEXT NOT NULL DEFAULT '' CHECK (length(email) <= 120)
            );
            """;
        command.ExecuteNonQuery();
    }

    public List<Student> Find(string query, string? groupName)
    {
        using var connection = OpenConnection();
        connection.CreateFunction<string, string, bool>("contains_ci", (value, term) => value.Contains(term, StringComparison.OrdinalIgnoreCase));

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, full_name, group_name, course, email
            FROM students
            WHERE ($group IS NULL OR group_name = $group)
              AND ($query = '' OR contains_ci(full_name, $query) OR contains_ci(email, $query))
            ORDER BY group_name, id;
            """;
        command.Parameters.Add("$group", SqliteType.Text).Value = (object?)groupName ?? DBNull.Value;
        command.Parameters.AddWithValue("$query", query);

        var students = new List<Student>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var details = new StudentDetails(reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetString(4));
            students.Add(new Student(reader.GetInt64(0), details));
        }
        return students;
    }

    public List<string> GetGroups()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT group_name FROM students ORDER BY group_name;";

        var groups = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            groups.Add(reader.GetString(0));
        return groups;
    }

    public long Add(StudentDetails details)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO students (full_name, group_name, course, email)
            VALUES ($name, $group, $course, $email)
            RETURNING id;
            """;
        AddDetails(command, details);
        return (long)command.ExecuteScalar()!;
    }

    public bool Update(long id, StudentDetails details)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE students
            SET full_name = $name, group_name = $group, course = $course, email = $email
            WHERE id = $id;
            """;
        AddDetails(command, details);
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    public bool Delete(long id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM students WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        try{
            connection.Open();
            return connection;
        }
        catch{
            connection.Dispose();
            throw;
        }
    }

    private static void AddDetails(SqliteCommand command, StudentDetails details)
    {
        command.Parameters.AddWithValue("$name", details.FullName);
        command.Parameters.AddWithValue("$group", details.GroupName);
        command.Parameters.AddWithValue("$course", details.Course);
        command.Parameters.AddWithValue("$email", details.Email);
    }
}
