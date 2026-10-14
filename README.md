# RaceDay - PROG6212 PoE Part 2 (RESTful API)

RaceDay is an event management system for the road running, walking and cycling
community. This repository contains the Part 1 planning documents (`docs/`) and
the Part 2 deliverable: a SQL Server database built with EF Core Code-First and a
RESTful ASP.NET Core API with server-side sessions and role-based access control
for two roles - **Organiser** and **Participant**.

## Project Description

Organisers create running/walking/cycling **Events** made up of one or more
**Categories** (e.g. 10km, 21km). Participants browse Events, **enrol** in a
Category, and later view their **Results**, which the Organiser captures after the
event.

## User Roles

### Organiser
- Register, log in, view and update their own profile.
- Create, update and delete **their own** Events (never another Organiser's).
- Add, update and delete Categories on their own Events.
- View enrolments for their own Events and confirm/cancel them.
- Capture each Participant's finish time and finishing position.

### Participant
- Register, log in, view and update their own profile.
- Browse Events and Categories (public).
- Enrol in an Event by choosing a Category, view and withdraw their own enrolments.
- View their own race results.
- Can **not** create/edit/delete Events or Categories, change enrolment statuses or capture results (403).

## Repository Structure

```
RaceDay.sln
docs/
  RaceDay_ERD.png / .mermaid      Part 1 ERD
  RaceDay_API_Endpoint_Plan.md    Part 1 endpoint plan
  RaceDay_Database_Script.sql     Part 1 SQL script
  Part2_Changes_From_Part1.md     every deliberate difference from Part 1, with reasons
  Part2_Implemented_Endpoints.md  every implemented endpoint, access rule and status code
  images/                         CI screenshot goes here
RaceDay.API/
  Controllers/                    Auth, Profile, Events, Categories, Enrolments, Results
  Models/                         Organiser, Participant, Event, Category, Enrolment, Result
  Data/RaceDayContext.cs          EF Core DbContext + relationships
  Migrations/                     Committed EF Core migration and model snapshot
  Services/                       PasswordService (BCrypt), SessionAuthenticationHandler
RaceDay.Contracts/                shared DTOs (Dtos.cs) and enums used by the API, the tests and Part 3
RaceDay.Tests/                    xUnit tests (in-memory database)
.github/workflows/dotnet-ci.yml   restore, build and test on every push
```

## Part 1

The Part 1 planning documents - ERD (`docs/RaceDay_ERD.png` and `docs/RaceDay_ERD.mermaid`), API endpoint plan and SQL script - are in `docs/`, and Part 1 is the
specification for Part 2: the same six tables, columns and relationships, and the same endpoint routes.
Where Part 2 differs, it is because the Part 2 brief requires it, and every difference is listed with its reason in
[`docs/Part2_Changes_From_Part1.md`](docs/Part2_Changes_From_Part1.md). In short:
- Events also store **distance and event type** (Part 2 brief: every Event captures them).
- Login creates a **server-side session** instead of returning a token, and there is a **logout** endpoint.
- A few endpoints from the Part 2 guide were added (`events/mine`, enrolment status, results by event), and the guide's route
  names also work as aliases of the Part 1 routes.

## Part 2

### Technologies Used
- ASP.NET Core 8 Web API, C#
- Entity Framework Core 8 - SQL Server, **Code-First** with migrations
- Server-side **session** authentication (`AddSession` + a custom authentication handler)
- BCrypt.Net-Next for password hashing
- Swashbuckle / Swagger (XML-documented endpoints)
- Three-project solution: `RaceDay.API`, `RaceDay.Contracts` (shared DTOs), `RaceDay.Tests`
- xUnit, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory
- GitHub Actions (CI)

### Project Structure
See *Repository Structure* above.

### Database Setup
1. Install SQL Server (Express/Developer) and SQL Server Management Studio.
2. In `RaceDay.API/appsettings.json`, set `ConnectionStrings:DefaultConnection` for your
   instance, e.g. `Server=.\SQLEXPRESS;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True;`
3. The initial EF Core migration is already included in `RaceDay.API/Migrations/`. Do **not** generate another initial migration. Apply the existing migration:

   From the solution root, run:
   ```powershell
   dotnet ef database update --project RaceDay.API/RaceDay.API.csproj --startup-project RaceDay.API/RaceDay.API.csproj
   ```
   Alternatively, in Visual Studio Package Manager Console with `RaceDay.API` as the Default project, run:
   ```powershell
   Update-Database
   ```
4. Open SSMS and confirm the database and its six core tables exist: `Organisers`,
   `Participants`, `Events`, `Categories`, `Enrolments`, `Results` (the same tables as the Part 1 script, plus Event distance/type).
5. Commit the generated `RaceDay.API/Migrations` folder.

### How to Run the API
1. Open `RaceDay.sln` in Visual Studio 2022 and set `RaceDay.API` as the startup project.
2. Press **F5** (or run `dotnet run` in `RaceDay.API`). The root URL redirects to Swagger.

### Swagger

Swagger loads at the root URL and documents every controller (Auth, Profile,
Events, Categories, Enrolments, Results). Log in via
`POST /api/auth/login` in Swagger first (the browser session keeps the
cookie), then exercise the protected endpoints from the same page.


## Note on schema deviation from Part 1

The Events table stores `Distance` and `EventType`, which the original Part 1
ERD only captured at the Category level. This was added because the Part 2
functional requirements ask for it on Events directly. Documented here as
the brief requires for any deliberate design change from Part 1.

### Authentication
- `POST /api/auth/register` - creates an Organiser or Participant. The password is
  hashed with BCrypt; the original password is never stored.
- `POST /api/auth/login` - verifies the password against the stored hash and creates a
  **server-side session** that stores the user's `UserId` and `Role`. The browser only
  receives an opaque session-id cookie.
- `POST /api/auth/logout` - clears the session.
- Every protected endpoint uses `[Authorize(Roles = "...")]`: **401** when not logged in,
  **403** when logged in with the wrong role. Endpoints that act on a specific Event,
  Category, Enrolment or Result also check the logged-in Organiser/Participant owns it.
- Profile endpoints use the `UserId` from the session - a user can only ever read or edit
  their own account.

### API Endpoints

Routes follow the Part 1 endpoint plan; the guide's equivalent route is shown in brackets where it differs.

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| Profile | `GET /api/users/me`, `PUT /api/users/me` (also `/api/profile/me`) |
| Events | `GET /api/events`, `GET /api/events/{id}`, `GET /api/events/mine`, `POST /api/events`, `PUT /api/events/{id}`, `DELETE /api/events/{id}` |
| Categories | `GET /api/events/{id}/categories`, `POST /api/events/{id}/categories`, `PUT /api/categories/{id}`, `DELETE /api/categories/{id}` |
| Enrolments | `POST /api/categories/{id}/enrol` (also `POST /api/enrolments`), `GET /api/enrolments/me` (also `/mine`), `GET /api/events/{id}/enrolments` (also `/api/enrolments/event/{id}`), `PUT /api/enrolments/{id}/status`, `DELETE /api/enrolments/{id}` |
| Results | `POST /api/enrolments/{id}/results` (also `POST /api/results`), `GET /api/participants/me/results` (also `/api/results/mine`), `GET /api/results/event/{eventId}`, `GET /api/categories/{id}/results` |

Full access rules and status codes: [`docs/Part2_Implemented_Endpoints.md`](docs/Part2_Implemented_Endpoints.md).

### Swagger
Swagger UI loads at the root URL and lists every endpoint grouped by controller, each
with a summary, expected input and the possible responses (200/201/204/400/401/403/404/409).
To test: `POST /api/auth/register` -> `POST /api/auth/login` (Swagger keeps the session cookie)
-> call any protected endpoint. Enum values are numeric: `role` 0 = Organiser, 1 = Participant;
`eventType` 0 = Run, 1 = Walk, 2 = Cycle.

Suggested demo flow: register Organiser -> login -> create Event -> create Categories ->
logout -> register/login Participant -> enrol -> logout -> login Organiser -> view enrolments,
confirm one, capture a Result -> login Participant -> view personal Results.

### Unit Testing
```
dotnet test RaceDay.sln
```
Run them from Visual Studio via **Test > Test Explorer > Run All**. Tests use an EF Core
in-memory database, so no SQL Server is needed (this is also how CI runs them). They cover:
- **Authentication:** valid/invalid registration (duplicate email, short password, invalid
  role, missing email), BCrypt hashing verified in the database, valid and incorrect login,
  unknown user.
- **Sessions:** login creates a session with the right role, logout clears it, separate
  clients have separate sessions.
- **Role enforcement:** every protected endpoint returns 401 when logged out; Organiser-only
  endpoints return 403 for Participants and Participant-only endpoints return 403 for Organisers.
- **Events, Categories, Enrolments, Results:** create/read/update/delete, ownership rules
  (another Organiser is rejected), duplicate/full enrolment (409), enrolment status management,
  result capture, personal results, and no password hashes in any response.

### CI/CD
`.github/workflows/dotnet-ci.yml` runs on every push and pull request: checks out the repo,
installs .NET 8, **validates the repository structure**, restores, builds the solution and
runs all unit tests.

### GitHub Actions Screenshot
Green workflow run: <img width="954" height="950" alt="image" src="https://github.com/user-attachments/assets/786b8d25-63d1-4b71-9b0d-098eb074366d" />

### Video Presentation
Unlisted YouTube video: https://www.youtube.com/watch?v=vV5pQ7NL8O4

### Github Link
https://github.com/AphiweGans/Aphiwe_Gans_PART2_COMPLETED.git


