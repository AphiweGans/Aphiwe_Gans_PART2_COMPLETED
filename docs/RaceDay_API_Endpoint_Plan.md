# RaceDay - API Endpoint Plan (Part 1, Section B)

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either a Participant or Organiser | None (public) | { fullName, email, password, role } | 201 Created - user object; 400 Bad Request - validation error |
| POST | /api/auth/login | Authenticates a user and returns an access token | None (public) | { email, password } | 200 OK - token; 401 Unauthorized - invalid credentials |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's own profile | Any (logged in) | None | 200 OK - profile object |
| PUT | /api/users/me | Updates the logged-in user's profile details | Any (logged in) | { fullName, phone } | 200 OK - updated profile; 400 Bad Request |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events | None (public) | None | 200 OK - array of events |
| GET | /api/events/{id} | Returns details of a single event | None (public) | None | 200 OK - event object; 404 Not Found |
| POST | /api/events | Creates a new event | Organiser | { eventName, eventDate, location, description } | 201 Created - event object |
| PUT | /api/events/{id} | Updates an existing event | Organiser | { eventName, eventDate, location, description } | 200 OK; 403 Forbidden; 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event | Organiser | None | 204 No Content; 403 Forbidden; 404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{id}/categories | Lists all categories for a specific event | None (public) | None | 200 OK - array of categories |
| POST | /api/events/{id}/categories | Adds a new category to an event | Organiser | { categoryName, distanceKm, maxParticipants, entryFee } | 201 Created - category object |
| PUT | /api/categories/{id} | Updates an existing category | Organiser | { categoryName, distanceKm, maxParticipants, entryFee } | 200 OK; 404 Not Found |
| DELETE | /api/categories/{id} | Deletes a category | Organiser | None | 204 No Content; 404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{id}/enrol | Enrols the logged-in participant into a category | Participant | None | 201 Created - enrolment object; 409 Conflict - already enrolled |
| GET | /api/enrolments/me | Returns the logged-in participant's own enrolments | Participant | None | 200 OK - array of enrolments |
| GET | /api/events/{id}/enrolments | Returns all enrolments for an event (for the organiser) | Organiser | None | 200 OK - array of enrolments |
| DELETE | /api/enrolments/{id} | Cancels the logged-in participant's own enrolment | Participant | None | 204 No Content; 403 Forbidden |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{id}/results | Captures a race result for an enrolment | Organiser | { finishTime, position, status } | 201 Created - result object |
| GET | /api/participants/me/results | Returns the logged-in participant's personal results history | Participant | None | 200 OK - array of results |
| GET | /api/categories/{id}/results | Returns the full results leaderboard for a category | None (public) | None | 200 OK - array of results |
