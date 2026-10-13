# RaceDay - Additional Project Notes

This file covers things a typical README doesn't - design reasoning,
troubleshooting, sample requests, and a rubric-to-code map. Keep it
alongside `README.md`, not instead of it.

## Architecture & Design Decisions

**Why separate `Organiser` and `Participant` tables instead of one `Users`
table with a `Role` column?**
Each role carries fields the other doesn't - Organisers have `Phone`,
Participants have `DateOfBirth` and `Gender`. Splitting them avoids nullable
columns that only apply to half the rows. Authentication is still unified:
both roles hash passwords the same way (`PasswordService`) and are issued
the same kind of session, distinguished only by the `Role` claim.

**Why is `Enrolment` linked to `Category` and not directly to `Event`?**
A Participant doesn't just enter an Event - they choose a specific Category
within it (e.g. the 10km, not the 21km). The Event is still reachable via
`Enrolment -> Category -> Event`, so nothing is lost; it's the more accurate
relationship for what's actually being recorded.

**Why server-side sessions instead of JWT?**
The brief asks for a server-side session that stores `UserId` and `Role`. At login
the API writes both into `HttpContext.Session` (held in server memory); the browser only
gets an opaque session-id cookie. `SessionAuthenticationHandler` reads the session on
every request and builds the user's identity, so `[Authorize(Roles = "...")]` works
normally and a role can never be tampered with client-side. Swagger's browser session
carries the cookie automatically, so no extra tooling is needed.

**Why is `Result` one-to-one with `Enrolment` and not a field on
`Participant`?**
A Participant can enrol in many Events over time, and each Enrolment may or
may not have a Result yet (results only exist after the event has happened).
Modelling it as its own table with a unique `EnrolmentID` keeps the "at most
one Result per Enrolment" rule enforced at the database level, not just in
application code.

## Sample Requests (for manual testing outside Swagger)

Replace `<port>` with whatever `dotnet run` reports (Swagger shows this too).

**Register an Organiser**
```bash
curl -X POST https://localhost:<port>/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Jane Organiser","email":"jane@raceday.test","password":"Password123!","role":0}'
```

**Log in and keep the session cookie** (roles are numeric: 0 = Organiser, 1 = Participant)
```bash
curl -c cookies.txt -X POST https://localhost:<port>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"jane@raceday.test","password":"Password123!"}'
```

**Create an Event using that session**
```bash
curl -b cookies.txt -X POST https://localhost:<port>/api/events \
  -H "Content-Type: application/json" \
  -d '{"eventName":"City 10K","eventDate":"2026-11-01T07:00:00","location":"Johannesburg","description":"Annual city 10K","distance":10,"eventType":0}'
```

**Confirm a Participant gets 403 on the same request**
```bash
curl -b participant_cookies.txt -X POST https://localhost:<port>/api/events \
  -H "Content-Type: application/json" \
  -d '{"eventName":"Should Fail","eventDate":"2026-11-01T07:00:00","location":"Test","description":"Should fail","distance":5,"eventType":1}'
# expect: HTTP/1.1 403 Forbidden
```

## Environment / Configuration Reference

| Setting | Where | Required for Part 2? | Notes |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.json` | Yes | Points at your SQL Server instance |
| `MvcAppUrl` | `appsettings.json` | No | CORS origin for a future Part 3 front end; harmless with no client present |
| `DB_CONNECTION_STRING` | `.env` (Docker only) | No | Only read by `docker-compose.yml` |

## Troubleshooting

**Swagger loads but every request returns 401.**
You haven't logged in yet in that browser session, you logged out, or the API was
restarted (sessions live in server memory and the 4-hour idle timeout applies). Call `POST /api/auth/login` from the same
Swagger tab first - the cookie is scoped to the browser session, not per
request.

**`dotnet ef migrations add` fails or can't find the project.**
Run the command from inside `RaceDay.API/`, not the solution root - the EF
tools need to find `RaceDay.API.csproj` as the current project.

**Tests fail locally but the app runs fine.**
Tests use an EF Core in-memory database (`CustomWebApplicationFactory`), so
they don't depend on your local SQL Server at all. A local test failure
that doesn't reproduce in GitHub Actions usually means stale build output -
try `dotnet clean` then `dotnet test` again.

## Rubric-to-Code Map

| Rubric criterion | Where to look |
|---|---|
| Correct Submission | `docs/` (Part 1 files + `Part2_Changes_From_Part1.md`), this file, `README.md` |
| GitHub and CI/CD | `.github/workflows/dotnet-ci.yml`, commit history |
| Authentication and Role-Based Access | `Controllers/AuthController.cs`, `Services/SessionAuthenticationHandler.cs`, `Services/PasswordService.cs`, `[Authorize]` attributes across all controllers |
| API Endpoints and Database | `Models/`, `RaceDay.Contracts/Dtos.cs`, `Data/RaceDayContext.cs`, `Controllers/EventsController.cs`, `CategoriesController.cs`, `EnrolmentsController.cs`, `ResultsController.cs` |
| Swagger Integration | `Program.cs` (`AddSwaggerGen`), XML doc comments on every controller action |
| Unit Testing | `RaceDay.Tests/` (auth, session, role-access, profile, event, category, enrolment and result tests - success and failure paths) |
| Video Presentation | `RaceDay_Part2_Video_Script.md` |

## Known Limitations & Possible Future Work

- No pagination on `GET /api/events` or `GET /api/events/{eventId}/categories` -
  fine at current scale, worth adding if the dataset grows.
- `ProfileUpdateDto` allows partial updates but doesn't validate phone
  number or gender formats.
- Sessions are held in server memory, so they reset when the API restarts (fine for
  a single-instance Part 2 API; a distributed cache would be used in production).
- No email confirmation step on registration - not required by the brief.
