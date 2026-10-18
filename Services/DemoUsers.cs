using Microsoft.AspNetCore.Identity;

namespace LeaveManager.Services;

public sealed record DemoUser(string Id, string Name, string Email, string Role);

public sealed class DemoUsers
{
    private readonly PasswordHasher<DemoUser> hasher = new();
    private readonly (DemoUser User, string Hash)[] accounts;

    public DemoUsers()
    {
        DemoUser[] users = [
            new("alice", "Alice Demo", "alice@example.test", "Employee"),
            new("bob", "Bob Demo", "bob@example.test", "Employee"),
            new("manager", "Manager Demo", "manager@example.test", "Manager")
        ];
        // Intentional PUBLIC demonstration password, not a real credential.
        accounts = users.Select(user => (user, hasher.HashPassword(user, "DemoPass123!"))).ToArray();
    }

    public DemoUser? Authenticate(string email, string password)
    {
        var account = accounts.FirstOrDefault(a =>
            string.Equals(a.User.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
        return account.User is not null &&
            hasher.VerifyHashedPassword(account.User, account.Hash, password) != PasswordVerificationResult.Failed
            ? account.User : null;
    }
}
