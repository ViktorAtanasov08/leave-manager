using System.ComponentModel.DataAnnotations;

namespace LeaveManager.Models;

public sealed class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public sealed class RequestForm
{
    [Required, DataType(DataType.Date)] public DateOnly? Start { get; set; }
    [Required, DataType(DataType.Date)] public DateOnly? End { get; set; }
    [Required, StringLength(200)] public string Reason { get; set; } = "";
}

public enum LeaveStatus { Pending, Approved, Rejected }

public sealed record LeaveRequest(int Id, string EmployeeId, string EmployeeName,
    DateOnly Start, DateOnly End, int Days, string Reason,
    LeaveStatus Status = LeaveStatus.Pending, string? DecidedBy = null);

public sealed record Balance(int Allowance, int Approved, int Pending)
{
    public int Available => Allowance - Approved - Pending;
}

public sealed record Journal(IReadOnlyList<LeaveRequest> Requests, Balance? Balance);
