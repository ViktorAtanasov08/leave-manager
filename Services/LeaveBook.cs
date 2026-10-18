using LeaveManager.Models;

namespace LeaveManager.Services;

// One shared local demo store. Every operation takes the same lock so decisions
// and balance checks cannot race; immutable records prevent edits outside it.
public sealed class LeaveBook(TimeProvider clock)
{
    private readonly object gate = new();
    private readonly List<LeaveRequest> requests = [];
    private int nextId = 1;
    public const int AnnualAllowance = 20;

    public Journal Read(string employeeId, bool manager)
    {
        lock (gate)
        {
            var visible = requests.Where(r => manager || r.EmployeeId == employeeId)
                .OrderByDescending(r => r.Id).ToArray();
            return new Journal(visible, manager ? null : GetBalance(employeeId, clock.GetLocalNow().Year));
        }
    }

    private Balance GetBalance(string employeeId, int year)
    {
        var own = requests.Where(r => r.EmployeeId == employeeId && r.Start.Year == year);
        return new Balance(AnnualAllowance,
            own.Where(r => r.Status == LeaveStatus.Approved).Sum(r => r.Days),
            own.Where(r => r.Status == LeaveStatus.Pending).Sum(r => r.Days));
    }

    public string? Submit(string employeeId, string employeeName, RequestForm form)
    {
        lock (gate)
        {
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
            if (days > GetBalance(employeeId, today.Year).Available)
                return "There are not enough available days for this request.";

            requests.Add(new LeaveRequest(nextId++, employeeId, employeeName, start, end,
                days, form.Reason.Trim()));
            return null;
        }
    }

    public string? Decide(int id, bool approve, string managerName)
    {
        lock (gate)
        {
            var index = requests.FindIndex(r => r.Id == id);
            if (index < 0) return "Request not found.";
            if (requests[index].Status != LeaveStatus.Pending)
                return "This request has already been decided.";
            requests[index] = requests[index] with {
                Status = approve ? LeaveStatus.Approved : LeaveStatus.Rejected,
                DecidedBy = managerName
            };
            return null;
        }
    }

    public static int CountWeekdays(DateOnly start, DateOnly end)
    {
        var days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) days++;
        return days;
    }
}
