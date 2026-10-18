# LeaveManager

[![Build and verify](https://github.com/ViktorAtanasov08/leave-manager/actions/workflows/build.yml/badge.svg)](https://github.com/ViktorAtanasov08/leave-manager/actions/workflows/build.yml)

**In progress — initial MVC milestone.** A small C# / ASP.NET Core MVC application for practising authentication, permissions and leave-request business rules.

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

This milestone runs only in the Development environment. It is a local learning demo, not an employee HR system for deployment. Accounts and requests are held in memory, shared in one app instance and reset on restart. Cookie-protection keys are kept in ignored `.local-keys/`; there is no registration, account database, password recovery or login throttling.

Leave must fit within the **current calendar year** and start today or later. Every employee receives a simplified fixed allowance of 20 days. Saturdays and Sundays are excluded; public holidays, carry-over, part days, cancellation, accrual and regional employment policies are not implemented. The application uses the server's local date. These are demonstration rules.

## Code map

| Path | Responsibility |
| --- | --- |
| `Controllers/AccountController.cs` | Sign-in, sign-out and claims |
| `Controllers/LeaveController.cs` | Forms and role checks |
| `Services/DemoUsers.cs` | Three fixed demo identities and hashed demo passwords |
| `Services/LeaveBook.cs` | In-memory requests, weekday counting and balances |
| `Models/LeaveModels.cs` | Form validation and immutable request records |
| `Views/` | Razor pages and accessible labelled forms |

## Verification

GitHub Actions builds with warnings treated as errors, runs deterministic business checks and exercises the application over HTTP. To run locally:

```sh
dotnet build tests/LeaveManager.Checks/LeaveManager.Checks.csproj -c Release --warnaserror
dotnet run --project tests/LeaveManager.Checks/LeaveManager.Checks.csproj -c Release --no-build
python scripts/smoke.py
```

The console checks use a fixed clock, so date-dependent business tests work throughout the year. They cover reservation/approval/rejection, overlapping requests, invalid dates, insufficient allowance, isolated employee views and concurrent submissions. HTTP checks cover sign-in, wrong passwords, employee/manager endpoint permissions, form validation, CSRF protection and logout. Browser appearance and production security are outside these checks.

## Next milestones

- [ ] Walk through the code together and make a small independent change.
- [ ] Replace temporary storage with SQL and migrations.
- [ ] Introduce database-backed accounts and a suitable identity solution.
- [ ] Add request cancellation, year selection and public-holiday rules.
- [ ] Capture screenshots from a verified running browser session.

## Learning notes

This initial scaffold was prepared with AI assistance. It is an unfinished learning project; the next step is to run, explain and extend it together. See [the first walkthrough](docs/day-01.md).

Reference: [Microsoft cookie authentication documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0).
