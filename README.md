# LeaveManager

[![Build and verify](https://github.com/ViktorAtanasov08/leave-manager/actions/workflows/build.yml/badge.svg)](https://github.com/ViktorAtanasov08/leave-manager/actions/workflows/build.yml)

**Persistent local demo.** A small C# / ASP.NET Core MVC application with SQLite storage for practising authentication, permissions and leave-request business rules. Requests, decisions and balances survive restarts.

## What works

- Sign in as one of two demo employees or a manager using cookie authentication.
- Employees request leave and see only their own requests and annual balance.
- Managers see the team journal and approve or reject pending requests.
- Weekdays count against a 20-day allowance. Pending requests reserve days; approval converts the reservation to approved days; rejection releases it.
- Refuse overlapping active requests, reversed or past dates, weekend-only requests and insufficient balances.
- Server-side role checks, signed identity cookies, password hashing, Razor escaping and anti-forgery validation on POST forms.

## Run locally

Install the .NET 10 SDK. From the repository folder:

```sh
dotnet restore
dotnet run -- --environment Development --urls http://127.0.0.1:5085
```

Open http://127.0.0.1:5085. These are **public demo accounts**, not real credentials:

| Role | Email | Password |
| --- | --- | --- |
| Employee | alice@example.test | DemoPass123! |
| Employee | bob@example.test | DemoPass123! |
| Manager | manager@example.test | DemoPass123! |

Submit a request as Alice, sign out, approve it as Manager, then sign in as Alice to inspect the updated balance. Use fictional details only.

## Scope and limitations

This demo runs only in the Development environment. Keep it on localhost and use fictional data. Back up the database with the app stopped. Never commit the database or local protection keys. It is a local learning demo, not an employee HR system for deployment. Requests and decisions are stored in **data/leave.db**, created automatically. Balances are calculated from the saved requests. The three demo identities are still fixed in code. Set **Database__Path** to use another database file. SQLite transactions serialize balance checks and writes across instances using the same file. Cookie-protection keys are kept in ignored `.local-keys/`; there is no registration, account database, password recovery or login throttling.

Leave must fit within the **current calendar year** and start today or later. Every employee receives a simplified fixed allowance of 20 days. Saturdays and Sundays are excluded; public holidays, carry-over, part days, cancellation, accrual and regional employment policies are not implemented. The application uses the server's local date. These are demonstration rules.

## Code map

| Path | Responsibility |
| --- | --- |
| `Controllers/AccountController.cs` | Sign-in, sign-out and claims |
| `Controllers/LeaveController.cs` | Forms and role checks |
| `Services/DemoUsers.cs` | Three fixed demo identities and hashed demo passwords |
| `Services/LeaveBook.cs` | Transactional request rules, weekday counting and balances |
| `Services/LeaveDatabase.cs` | SQLite initialization and request mapping |
| `Models/LeaveModels.cs` | Form validation and immutable request records |
| `Views/` | Razor pages and accessible labelled forms |

## Verification

GitHub Actions builds with warnings treated as errors, runs deterministic business checks and exercises the application over HTTP. To run locally:

```sh
dotnet build tests/LeaveManager.Checks/LeaveManager.Checks.csproj -c Release --warnaserror
dotnet run --project tests/LeaveManager.Checks/LeaveManager.Checks.csproj -c Release --no-build
python scripts/smoke.py
python scripts/persistence.py
```

The console checks use a fixed clock, so date-dependent business tests work throughout the year. They cover reservation/approval/rejection, overlapping requests, invalid dates, insufficient allowance, isolated employee views and concurrent submissions. HTTP checks cover sign-in, wrong passwords, employee/manager endpoint permissions, form validation, CSRF protection and logout. Separate database instances test competing approvals, overlapping submissions and non-overlapping requests that would exceed the allowance. The persistence suite creates requests over HTTP, restarts the process and checks saved approval, balances and employee isolation. Tests use temporary databases; they do not erase the local demo database. Browser appearance and production security are outside these checks.

## Next milestones

- [x] Replace temporary request storage with SQLite and verify restart persistence.
- [ ] Introduce versioned schema migrations when the model changes.
- [ ] Introduce database-backed accounts and a suitable identity solution.
- [ ] Add request cancellation, year selection and public-holiday rules.
- [ ] Capture screenshots from a verified running browser session.

## Learning notes

Built with AI assistance, including the SQLite persistence update. The demo is not a production HR system and does not establish independent authorship. See [the persistence walkthrough](docs/persistence.md) for design decisions, interview questions and a follow-up exercise. The [first walkthrough](docs/day-01.md) records the earlier in-memory milestone.

Reference: [Microsoft cookie authentication documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0).
