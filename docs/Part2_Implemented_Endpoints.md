# Part 2 - implemented endpoints

"Part 1 route" is the path in the Part 1 endpoint plan (the primary route). "Alias" is the equivalent path from the Part 2 guide.
Access is enforced by `[Authorize(Roles = "...")]`: **401** when not logged in, **403** when the role is wrong or the user does not own the resource.

## Auth
| Method | Route | Access | Success | Failure |
|---|---|---|---|---|
| POST | `/api/auth/register` | Public | 201 | 400 (missing/invalid fields, short password, invalid role, email in use) |
| POST | `/api/auth/login` | Public | 200 (session created) | 401 |
| POST | `/api/auth/logout` | Public | 200 (session cleared) | - |

## Profile
| Method | Route | Access | Success | Failure |
|---|---|---|---|---|
| GET | `/api/users/me` (alias `/api/profile/me`) | Any logged-in user | 200 | 401 |
| PUT | `/api/users/me` (alias `/api/profile/me`) | Any logged-in user | 200 | 400 / 401 |

## Events
| Method | Route | Access | Success | Failure |
|---|---|---|---|---|
| GET | `/api/events` (upcoming events only, as in Part 1) | Public | 200 | - |
| GET | `/api/events/{id}` | Public | 200 | 404 |
| GET | `/api/events/mine` | Organiser | 200 | 401 / 403 |
| POST | `/api/events` | Organiser | 201 | 400 / 401 / 403 |
| PUT | `/api/events/{id}` | Organiser (owner) | 200 | 400 / 401 / 403 / 404 |
| DELETE | `/api/events/{id}` | Organiser (owner) | 204 | 401 / 403 / 404 |

## Categories
| Method | Route | Access | Success | Failure |
|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Public | 200 | 404 |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) | 201 | 400 / 401 / 403 / 404 |
| PUT | `/api/categories/{id}` | Organiser (owner) | 200 | 400 / 401 / 403 / 404 |
| DELETE | `/api/categories/{id}` | Organiser (owner) | 204 | 401 / 403 / 404 |

## Enrolments
| Method | Route | Access | Success | Failure |
|---|---|---|---|---|
| POST | `/api/categories/{categoryId}/enrol` (alias `POST /api/enrolments`, body `{categoryId}`) | Participant | 201 (status Confirmed) | 400 / 401 / 403 / 404 / 409 (already enrolled or full) |
| GET | `/api/enrolments/me` (alias `/api/enrolments/mine`) | Participant | 200 | 401 / 403 |
| GET | `/api/events/{eventId}/enrolments` (alias `/api/enrolments/event/{eventId}`) | Organiser (owner) | 200 | 401 / 403 / 404 |
| PUT | `/api/enrolments/{id}/status` (body `{status}`) | Organiser (owner of the event) | 200 | 400 / 401 / 403 / 404 |
| DELETE | `/api/enrolments/{id}` | Participant (owner) | 204 | 401 / 403 / 404 |

## Results
| Method | Route | Access | Success | Failure |
|---|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/results` (alias `POST /api/results`, body also has `enrolmentId`) | Organiser (owner of the event) | 201 | 400 / 401 / 403 / 404 / 409 (result already captured) |
| GET | `/api/participants/me/results` (alias `/api/results/mine`) | Participant | 200 | 401 / 403 |
| GET | `/api/results/event/{eventId}` | Organiser (owner) | 200 | 401 / 403 / 404 |
| GET | `/api/categories/{categoryId}/results` | Public (names and positions only) | 200 | - |

## Status codes used
200 OK, 201 Created, 204 No Content, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict.
