using LeaveManager.Models;
using LeaveManager.Services;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAILED: {name}");
    checks++;
    Console.WriteLine($"PASS: {name}");
}
RequestForm Range(int start, int end, string reason = "Family break") => new() {
    Start = new DateOnly(2026, 1, start), End = new DateOnly(2026, 1, end), Reason = reason
};
var book = new LeaveBook(new FixedClock());
Check(LeaveBook.CountWeekdays(new(2026, 1, 2), new(2026, 1, 5)) == 2, "Weekends do not consume days");
Check(book.Submit("alice", "Alice", Range(5, 9, "  Family break  ")) is null, "Five-day request accepted");
var journal = book.Read("alice", false);
Check(journal.Balance == new Balance(20, 0, 5), "Pending request reserves five days");
Check(journal.Requests.Single().Reason == "Family break", "Reason is trimmed");
Check(book.Read("bob", false).Requests.Count == 0, "Employee data is isolated");
Check(book.Read("manager", true).Requests.Count == 1, "Manager sees team requests");
Check(book.Submit("alice", "Alice", Range(8, 12)) is not null, "Overlapping request refused");
Check(book.Decide(1, true, "Manager") is null, "Manager can approve pending request");
Check(book.Read("alice", false).Balance == new Balance(20, 5, 0), "Approval moves reserved days to approved");
Check(book.Decide(1, false, "Manager") is not null, "Repeated decision refused");
Check(book.Submit("alice", "Alice", Range(12, 16)) is null, "Second request accepted");
Check(book.Read("alice", false).Balance!.Available == 10, "Approved and pending both reduce availability");
Check(book.Decide(2, false, "Manager") is null, "Pending request can be rejected");
Check(book.Read("alice", false).Balance!.Available == 15, "Rejection releases only pending days");
Check(book.Submit("alice", "Alice", Range(12, 16)) is null, "Rejected date range can be requested again");
Check(book.Submit("alice", "Alice", new() { Start = new(2026, 2, 2), End = new(2026, 2, 27), Reason = "Too long" }) is not null,
    "Balance cannot be exceeded");
Check(book.Submit("bob", "Bob", Range(3, 4)) is not null, "Weekend-only request refused");
Check(book.Submit("bob", "Bob", Range(7, 6)) is not null, "Reversed range refused");
Check(book.Submit("bob", "Bob", Range(5, 5, "   ")) is not null, "Whitespace reason refused");
Check(book.Submit("bob", "Bob", Range(5, 5, new string('a', 201))) is not null, "Long reason refused");
Check(book.Submit("bob", "Bob", new() { Start = new(2025, 12, 31), End = new(2026, 1, 2), Reason = "Past" }) is not null,
    "Past leave refused");
Check(book.Submit("bob", "Bob", new() { Start = new(2026, 12, 31), End = new(2027, 1, 2), Reason = "Cross-year" }) is not null,
    "Cross-year range refused");
Check(book.Decide(999, true, "Manager") is not null, "Unknown request refused");
var racing = new LeaveBook(new FixedClock());
var outcomes = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
    racing.Submit("alice", "Alice", Range(5, 9)))));
Check(outcomes.Count(error => error is null) == 1, "Concurrent overlapping submissions create only one request");
Console.WriteLine($"{checks} business checks passed.");

sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
