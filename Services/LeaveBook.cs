using System.Globalization;
using LeaveManager.Models;

namespace LeaveManager.Services;

// Database transactions serialize balance checks and writes across app instances.
public sealed class LeaveBook(TimeProvider clock, string databasePath)
{
    private readonly LeaveDatabase database = new(databasePath);
    public const int AnnualAllowance = 20;

    public Journal Read(string employeeId, bool manager)
    {
        using var connection = database.Open();
        var requests = LeaveDatabase.Read(connection);
        var visible = requests.Where(r => manager || r.EmployeeId == employeeId)
            .OrderByDescending(r => r.Id).ToArray();
        return new Journal(visible, manager ? null : GetBalance(requests, employeeId, clock.GetLocalNow().Year));
    }

    private static Balance GetBalance(List<LeaveRequest> requests, string employeeId, int year)
    {
        var own = requests.Where(r => r.EmployeeId == employeeId && r.Start.Year == year);
        return new Balance(AnnualAllowance,
            own.Where(r => r.Status == LeaveStatus.Approved).Sum(r => r.Days),
            own.Where(r => r.Status == LeaveStatus.Pending).Sum(r => r.Days));
    }

    public string? Submit(string employeeId, string employeeName, RequestForm form)
    {
        using var connection = database.Open();
        // IMMEDIATE transaction: another writer waits before reading the balance.
        using var transaction = connection.BeginTransaction(deferred: false);
        var requests = LeaveDatabase.Read(connection, transaction);
        if (form.Start is not { } start || form.End is not { } end)
            return "Choose both dates.";
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        if (start < today) return "Leave must start today or later.";
        if (end < start) return "End date must be on or after the start date.";
        if (start.Year != today.Year || end.Year != today.Year)
            return "This milestone supports leave within the current calendar year only.";
        if (string.IsNullOrWhiteSpace(form.Reason) || form.Reason.Trim().Length > 200)
            return "Enter a reason of 1 to 200 characters.";

        var days = CountWeekdays(start, end);
        if (days == 0) return "Choose a range containing at least one weekday.";
        if (requests.Any(r => r.EmployeeId == employeeId && r.Status != LeaveStatus.Rejected
                && start <= r.End && end >= r.Start))
            return "This overlaps an existing pending or approved request.";
        if (days > GetBalance(requests, employeeId, today.Year).Available)
            return "There are not enough available days for this request.";

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO LeaveRequests (EmployeeId, EmployeeName, Start, End, Days, Reason)
            VALUES ($employee, $name, $start, $end, $days, $reason);
            """;
        command.Parameters.AddWithValue("$employee", employeeId);
        command.Parameters.AddWithValue("$name", employeeName);
        command.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$days", days);
        command.Parameters.AddWithValue("$reason", form.Reason.Trim());
        command.ExecuteNonQuery();
        transaction.Commit();
        return null;
    }

    public string? Decide(int id, bool approve, string managerName)
    {
        using var connection = database.Open();
        using var command = connection.CreateCommand();
        // A conditional UPDATE allows exactly one decision, even across processes.
        command.CommandText = """
            UPDATE LeaveRequests SET Status=$status, DecidedBy=$manager
            WHERE Id=$id AND Status=0;
            """;
        command.Parameters.AddWithValue("$status", (int)(approve ? LeaveStatus.Approved : LeaveStatus.Rejected));
        command.Parameters.AddWithValue("$manager", managerName);
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1 ? null : "Request not found or already decided.";
    }

    public static int CountWeekdays(DateOnly start, DateOnly end)
    {
        var days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) days++;
        return days;
    }
}
