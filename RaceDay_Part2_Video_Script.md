# RaceDay - Part 2 Video Presentation Script

**Target length: 5-7 minutes.**
**Record your screen (Visual Studio, Swagger, Test Explorer, GitHub) and read
this in your own voice. Upload as an Unlisted YouTube video and put the link
in README.md.**

This script follows the running order from section 30 of the PROG6212 Part 2
student guide.

---

## 1. Introduction (~20 seconds)

"Hi, I'm [Your Name], and this is my Part 2 submission for PROG6212 - the
RaceDay RESTful API. In this video I'll walk through the project structure,
the database, authentication and role-based access, the API endpoints in
Swagger, the automated tests, and the GitHub Actions CI/CD pipeline."

## 2. Repository and project structure (~30 seconds)
*(Screen: Visual Studio Solution Explorer, or the repo in GitHub)*

"Here's the repository. `docs` holds the Part 1 planning - the ERD, the API
endpoint plan, and the SQL script. `RaceDay.API` is the Web API project, and
`RaceDay.Tests` is the xUnit test project. The `.github/workflows` folder
holds the CI/CD pipeline, which I'll show running later."

## 3. Models and relationships (~40 seconds)
*(Screen: `RaceDay.API/Models`)*

"These are my entities: Organiser, Participant, Event, Category, Enrolment,
and Result. An Organiser creates Events. Each Event has Categories - for
example a 10km and a 21km option. A Participant enrols by choosing a
Category, and that Enrolment is what connects the Participant, the Category,
and through it, the Event. After the event, an Organiser captures a Result
against that Enrolment, which is why Result has a one-to-one relationship
with Enrolment rather than living directly on Participant."

## 4. DbContext and the database (~30 seconds)
*(Screen: `RaceDayContext.cs`, then SQL Server Management Studio)*

"`RaceDayContext` is my EF Core DbContext - it exposes a DbSet for each
entity, and `OnModelCreating` configures the relationships, unique
constraints like one email per account, and delete behaviour. I used the
Code-First approach: `Add-Migration` generated the schema from these C#
classes, and `Update-Database` applied it. Here it is in SQL Server
Management Studio - you can see all six tables exist, matching my Part 1
ERD in the docs folder."

## 5. Registration and password hashing (~40 seconds)
*(Screen: Swagger, `POST /api/auth/register`)*

"Let's register an Organiser." *(Fill in the request body, execute, show the
201 response.)* "Behind the scenes, `AuthController` hashes the password
with BCrypt before it ever touches the database - I never store the
original password. I can show that in SQL Server too." *(Show the
Organisers table - PasswordHash column, no plain text.)*

## 6. Login and session management (~30 seconds)
*(Screen: Swagger, `POST /api/auth/login`)*

"Now I log in with those same credentials." *(Execute, show 200 OK.)* "This
creates a server-side session that stores the user's id and role in server
memory - the browser only gets an opaque session cookie. In the code,
`AuthController` writes UserId and Role into the session, and
`SessionAuthenticationHandler` reads them back on every request, which is what
makes `[Authorize(Roles = ...)]` work on every protected endpoint."

## 7. Organiser-only functionality (~50 seconds)
*(Screen: Swagger, still logged in as Organiser)*

"As an Organiser, I can create an Event." *(POST /api/events, show 201.)*
"...add a Category to it." *(POST /api/events/{eventId}/categories, show
201.)* "...and once someone's enrolled, view the enrolments for my event, confirm
one, and capture their result." *(Show GET /api/enrolments/event/{eventId},
PUT /api/enrolments/{id}/status, then POST /api/results.)*

## 8. Participant-only functionality (~50 seconds)
*(Screen: Swagger - log in as a Participant, or use a second browser tab)*

"Now as a Participant. I can browse Events and Categories without even being
logged in, since those are public reads." *(GET /api/events.)* "But entering
one requires a session." *(POST /api/enrolments with a categoryId, show 201.)* "I can see my own
enrolments," *(GET /api/enrolments/mine)* "and once the Organiser records a
result, I can view it here." *(GET /api/results/mine.)*

## 9. Rejected wrong-role request (~30 seconds)
*(Screen: Swagger, still logged in as Participant)*

"Role enforcement matters as much as allowing the right thing - so let's try
something a Participant shouldn't be able to do." *(POST /api/events as the
Participant.)* "That returns 403 Forbidden, not 200 - the API checked my
role and rejected it, exactly as the brief requires."

## 10. Swagger tour (~30 seconds)
*(Screen: Swagger UI, scroll through all controllers)*

"Here's the full Swagger UI - Auth, Profile, Events, Categories, Enrolments,
and Results are all documented, with summaries and the expected response
codes for each endpoint."

## 11. Test Explorer (~40 seconds)
*(Screen: Visual Studio Test Explorer)*

"All my automated tests live in RaceDay.Tests. They cover the happy paths -
valid registration, valid login, Organisers creating Events, Participants
enrolling - and the failure paths - wrong passwords, Participants trying to
create Events, unauthenticated requests to protected endpoints. Let's run
them all." *(Run all tests, show them passing.)*

## 12. GitHub Actions green build (~20 seconds)
*(Screen: the Actions tab on GitHub)*

"And here's the same build and test run happening automatically in GitHub
Actions on every push - restore, build, test, all green."

## 13. Closing - Part 3 readiness (~20 seconds)

"That's RaceDay Part 2 - a SQL Server database built Code-First with EF
Core, a RESTful API with server-side session authentication and role enforcement,
documented in Swagger, and covered by automated tests running in CI. This
API is what Part 3's MVC front end will consume for every data operation.
Thanks for watching."

---

## Recording notes

- Record with OBS Studio or the Windows Snipping Tool's screen recorder,
  screen only.
- Keep the Swagger walkthrough moving - show the request, the response code,
  and move on. Don't read every field out loud.
- If a request fails on camera because you forgot to log in first, that's
  fine to leave in - it's still your own code producing the correct 401.
- Upload as an **Unlisted** YouTube video and paste the link into
  `README.md` under **Videos**.
