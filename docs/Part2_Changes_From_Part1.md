# Part 2 - changes from the Part 1 design

Part 1 (ERD, SQL script, endpoint plan) is the specification. Part 2 implements it exactly except for
the items below. Each one is needed to meet a Part 2 requirement, and the reason is given.

## Database / models

| # | Change | Reason |
|---|---|---|
| 1 | `Events` gains `Distance` (decimal 6,2) and `EventType` (Run / Walk / Cycle) | The Part 2 brief says every Event must capture name, description, date, location, **distance and event type**. The Part 1 `Events` table only had name, date, location and description. |
| 2 | The API requires an Event `Description` (the Part 1 column is nullable and stays nullable) | Same Part 2 requirement: every Event captures a description. |
| 3 | Tables are created by EF Core migrations (Code-First) instead of by running the Part 1 script | Part 2 requires Code-First. The migration produces the same six tables, columns, sizes, keys and unique constraints as the Part 1 script, plus the two Event columns above. |
| 4 | `CreatedAt` is set by the application (UTC `datetime2`) instead of `DEFAULT GETDATE()` | EF Core Code-First default mapping. |
| 5 | Deleting an Event also deletes its Categories, and deleting an Enrolment also deletes its Result (cascade) | The Part 1 script had no delete rules, so an Organiser could never delete an Event that had Categories. Participants and Categories that still have Enrolments are protected (restricted), as in Part 1. |

Everything else matches Part 1: Organisers, Participants, Events, Categories, Enrolments and Results; the same
columns, lengths and nullability; unique email per role; `UNIQUE (ParticipantID, CategoryID)` on Enrolments;
`UNIQUE (EnrolmentID)` on Results; and the Enrolment default status `Confirmed`.

> Do not run the Part 1 script and the migration against the same database - the script's `USE Raceday` targets a
> different database name to the one in `appsettings.json`. Use the migration for the API; keep the Part 1 script as
> the reference design and seed-data example.

## Endpoints

| # | Change | Reason |
|---|---|---|
| 6 | `POST /api/auth/login` creates a **server-side session** (UserId + Role) instead of returning an access token | The Part 2 brief requires server-side session management. |
| 7 | Added `POST /api/auth/logout` | Part 2 brief: login, **logout**, session cleared. |
| 8 | Added `GET /api/events/mine` | Part 2 guide event endpoints: an Organiser lists only their own events. |
| 9 | Added `PUT /api/enrolments/{id}/status` (Pending / Confirmed / Cancelled) | Part 2 guide: Organiser manages enrolments for their own events. |
| 10 | Added `GET /api/results/event/{eventId}` | Part 2 guide: Organiser views the results captured for their event. |
| 11 | Guide-style routes also work as aliases of the Part 1 routes: `/api/profile/me`, `POST /api/enrolments` (categoryId in body), `/api/enrolments/mine`, `/api/enrolments/event/{eventId}`, `POST /api/results` (enrolmentId in body), `/api/results/mine` | The Part 2 guide lists these paths. Both sets behave identically, so the Part 1 plan and the guide are both satisfied. |
| 12 | Extra checks beyond the Part 1 plan: ownership (403) on every Category/Event/Enrolment/Result action, a full category returns 409, request validation returns 400 | Part 2 requires role enforcement and correct status codes. |
