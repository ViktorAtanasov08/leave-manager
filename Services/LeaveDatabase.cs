using System.Globalization;
using LeaveManager.Models;
using Microsoft.Data.Sqlite;

namespace LeaveManager.Services;

public sealed class LeaveDatabase
{
    private readonly string connectionString;

    public LeaveDatabase(string databasePath)
    {
        var path = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connectionString = new SqliteConnectionStringBuilder {
            DataSource = path, Pooling = false, DefaultTimeout = 10
        }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS LeaveRequests (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId TEXT NOT NULL,
                EmployeeName TEXT NOT NULL,
                Start TEXT NOT NULL,
                End TEXT NOT NULL CHECK(End >= Start),
                Days INTEGER NOT NULL CHECK(Days BETWEEN 1 AND 20),
                Reason TEXT NOT NULL CHECK(length(trim(Reason)) BETWEEN 1 AND 200),
                Status INTEGER NOT NULL DEFAULT 0 CHECK(Status IN (0, 1, 2)),
                DecidedBy TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_LeaveRequests_Employee ON LeaveRequests(EmployeeId, Start);
            """;
        command.ExecuteNonQuery();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    public static List<LeaveRequest> Read(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Id, EmployeeId, EmployeeName, Start, End, Days, Reason, Status, DecidedBy FROM LeaveRequests ORDER BY Id DESC";
        using var reader = command.ExecuteReader();
        var result = new List<LeaveRequest>();
        while (reader.Read()) result.Add(new LeaveRequest(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
            DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateOnly.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            reader.GetInt32(5), reader.GetString(6), (LeaveStatus)reader.GetInt32(7),
            reader.IsDBNull(8) ? null : reader.GetString(8)));
        return result;
    }
}
