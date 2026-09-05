# Craftsmen and Job Bookings — plan.md

## 0. The demo moment

Two customers request the same craftsman for the same Tuesday. Neither is answered yet — both
requests sit there, nothing refused. The craftsman accepts the first; the second accept is refused
by the server, naming the job that already holds the date. Everything else in this plan is built to
make that path work first and survive contact with the database. The build order in §8 starts and
ends by proving this moment, not by rehearsing it once at the end.


## 1. Roles and what they can do

| Who | Can do | Everything else |
|---|---|---|
| **Administrator** | Approve/refuse craftsmen, manage the craft list. Asks for nothing, owns no work. | |
| **Craftsman — approved** | Declare crafts worked in, accept/decline requests naming them, mark day-sheet items done, edit own profile. | |
| **Craftsman — pending/refused** | See only their own status, ask the helper. | 403 |
| **Customer** | Find craftsmen, request jobs, withdraw unanswered requests, read own requests/profile. Answers nothing. | |
| **Not signed in** | Login + the two sign-up screens. | 401 |

Single login screen for all three; the server determines role from stored data (no client-side
"which are you?" toggle).

**Ownership rule (stated once, applies everywhere in §5):** who is asking comes from the JWT, never
from an id in the URL. A request for a row you don't own — another craftsman's requests, another
customer's profile — is **403**, not an empty list and not a 404. Implement this as a single
authorization policy / query filter applied per resource type, not as a check repeated in every
controller action. One place to get it right beats twenty places to forget it in one.


## 2. Tables

### `Users`
Shared identity/auth row for **all three** roles (one address = one person, enforced by one table,
never asked which kind on login).

| Field | Notes |
|---|---|
| Id | PK |
| Email | unique |
| PasswordHash | hashed, never returned — see §6a for algorithm |
| Role | Administrator / Craftsman / Customer |
| CreatedAt | |

### `Craftsmen`
One row per craftsman, created at sign-up.

| Field | Notes |
|---|---|
| Id | PK |
| UserId | FK → Users, unique |
| Name | |
| ContactInfo | |
| DailyRate | `decimal(18,2)` — see note below |
| Status | Pending / Approved / Refused — decided once, cannot flip back |
| IsTakingWork | `bool`, default true — the field `GET /customers/craftsmen?craft=` actually filters on; editable by the craftsman on their own profile |
| LastCheckedAt | `datetime`, nullable — cleared/reset each time they open their notifications list; see §6 |

### `Crafts`
Administrator-maintained list.

| Field | Notes |
|---|---|
| Id | PK |
| Name | unique, required |
| Description | shown to customers |

### `CraftsmanCrafts` (join table)
Which crafts a craftsman currently works in — editable by them, not duplicable.

| Field | Notes |
|---|---|
| CraftsmanId | FK, composite PK |
| CraftId | FK, composite PK |

### `Customers`
| Field | Notes |
|---|---|
| Id | PK |
| UserId | FK → Users, unique |
| Name | |
| ContactInfo | |
| LastCheckedAt | `datetime`, nullable — same pattern as Craftsmen, see §6 |

### `Requests` (the jobs)
| Field | Notes |
|---|---|
| Id | PK |
| CustomerId | FK |
| CraftsmanId | FK — the one craftsman it's addressed to |
| CraftId | FK — must be one the named craftsman works in |
| NeededOn | one date, not a range (renamed from `Date` — reads better next to the unique index below) |
| Price | `decimal(18,2)`, > 0 — see note below |
| Description | required |
| Status | Pending / Accepted / Declined / Withdrawn / Completed |
| CreatedAt | |
| RespondedAt | nullable — used for "what's new" fallback and for audit |

**Money type:** EF Core maps `decimal` to SQLite `TEXT`, and SQLite compares/orders that
lexicographically, not numerically — sums and comparisons on `Price`/`DailyRate` can be wrong.
Two acceptable fixes, pick one and note it in the migration comment:
- Store as `decimal` and add an explicit EF value comparer / do all aggregation in C# after
  materializing rows (never `SUM()` in raw SQL against SQLite for this column), **or**
- Store minor units as `int` (piastres, not pounds) and format for display in the client.
Never `double` for money, either way.

### Relationships
- `Users` 1—1 `Craftsmen` (optional; only present for craftsman accounts)
- `Users` 1—1 `Customers` (optional; only present for customer accounts)
- `Craftsmen` M—M `Crafts` via `CraftsmanCrafts`
- `Customers` 1—M `Requests`
- `Craftsmen` 1—M `Requests` (as the named craftsman)
- `Crafts` 1—M `Requests`

### On delete / removal rules
- A `Craft` **cannot** be removed if any craftsman currently works in it, or any request names it —
  400, message: *"This craft is still in use and can't be removed."*
- Renaming a `Craft` to a name already in use — 400, same shape as create's duplicate-name check.
- A `Request` is never hard-deleted; withdrawal and decline are status changes, not row deletions
  (so "what's new since you last looked" stays accurate).
- Deleting a `Craftsman` or `Customer` account is **out of scope** — but if either has any
  `Requests` in Pending or Accepted status, the API must still refuse deletion (400) rather than
  silently orphaning rows, in case this is added later. Documented now so it isn't a surprise if a
  judge asks.
- Deleting a `User` is out of scope entirely.



## 3. The core invariant

**A craftsman cannot hold two accepted jobs on the same date.**

Enforced **at accept time**, not at request time — two customers can both have pending requests for
the same craftsman on the same Tuesday. The first accept wins; the second accept is refused and must
name the job that already holds the date. The customer of the refused one is told plainly so they
can ask someone else.

**This is a race, and a read-then-write check alone is not safe against it.** Two accepts arriving
together can both read zero conflicts and both commit. The fix lives in the database, not the
service:

- A **unique index on `Requests(CraftsmanId, NeededOn)` filtered to `WHERE Status = 'Accepted'`.**
  SQLite supports partial indexes; this is the actual source of truth for the invariant.
- The service still does the friendly read first (find the conflicting job, so the 400 can name it)
  — but the **insert/update is what's allowed to fail**, and the service catches that constraint
  violation and turns it into the same readable 400. The query is the nice path; the constraint is
  the guarantee.
- This means the accept action is genuinely two steps in code: attempt the update inside a
  try/catch (or check + save wrapped so the DB is the final arbiter), not a single "if no conflict,
  then accept."

No availability table anywhere. "Free on Tuesday" is never stored — it's the absence of an accepted
row for that craftsman/date, guaranteed by the index above.

### 3a. Completing a job — decided (D6)

As originally written, "mark done" only worked on the exact day (`400 not today's date`), which
meant a job nobody clicked complete on the day stayed `Accepted` forever — and kept blocking that
date on the craftsman's record even though the work either happened or didn't. Background jobs are
out of scope (§12), so there's no sweep that can close it out automatically.

**Decision:** a craftsman can mark an accepted job complete on its date **or any day after** —
never before. The day sheet still defaults to showing *today's* accepted jobs, but a craftsman can
also open a past date and clean up anything left over. `POST /craftsman/day-sheet/{requestId}/complete`
now checks `NeededOn <= today`, not `NeededOn == today`. This keeps the rule server-enforced and
keeps the day sheet honest, without adding a scheduled task.

## 4. Error response shape

Every error, from every endpoint, uses one shape so the client renders any failure generically:

```json
{
  "status": 400,
  "message": "That date is already booked.",
  "errors": {
    "neededOn": "You already have an accepted job on this date (Request #482)."
  }
}
```

- `status` — the HTTP status, repeated in the body for clients that only see the parsed JSON.
- `message` — one human sentence, safe to show as-is.
- `errors` — dictionary keyed by field name; empty/omitted for errors that aren't field-specific
  (404, 403, the accept-conflict case which names a request, not a field — put it under a key like
  `"neededOn"` or `"conflict"` consistently).

Every 400 in §5 below gets its actual message text, not just a code, so writing this out is also
where the rules that hadn't been fully thought through show up.



## 5. Endpoints

Auth applies everywhere except login/signup (401 if missing/expired). Ownership and role/status
checks return 403 (see the rule in §1). Not-found IDs return 404. Validation failures return 400
using the shape in §4. List endpoints (see §7a) default-sort and cap results.

### Auth
| Method & path | Who | Success | Failure modes |
|---|---|---|---|
| POST /auth/signup/craftsman | anon | 201, craftsman record (Pending) | 400 duplicate email — *"An account with this email already exists."*; 400 missing field |
| POST /auth/signup/customer | anon | 201, customer record | 400 duplicate email; 400 missing field |
| POST /auth/login | anon | 200, `{ token, role }` | 401 — *"Email or password is incorrect."* (never say which one) |
| GET /auth/me | any signed-in | 200, `{ id, role, status }` | 401 expired/missing token |

### Administrator
| Method & path | Success | Failure modes |
|---|---|---|
| GET /admin/craftsmen?status=pending | 200, list | 403 non-admin |
| POST /admin/craftsmen/{id}/approve | 200, updated record | 400 — *"This craftsman has already been decided."*; 404 |
| POST /admin/craftsmen/{id}/refuse | 200, updated record | 400 already decided; 404 |
| GET /admin/crafts | 200, list | |
| POST /admin/crafts | 201, created craft | 400 empty/duplicate name — *"A craft with this name already exists."* |
| PUT /admin/crafts/{id} | 200, updated craft | 400 empty/duplicate name; 404 |
| DELETE /admin/crafts/{id} | 204 | 400 in use — *"This craft is still in use and can't be removed."*; 404 |

### Craftsman (approved only, else 403)
| Method & path | Success | Failure modes |
|---|---|---|
| GET /craftsman/crafts | 200, all crafts + which are theirs | |
| PUT /craftsman/crafts | 200, updated list | 400 duplicate entry in submitted list |
| GET /craftsman/requests | 200, list, paged | |
| POST /craftsman/requests/{id}/accept | 200, updated request | 400 date conflict — *"You already have an accepted job on this date (Request #{id})."*; 400 already answered — *"This request has already been answered."*; 403 not the named craftsman; 404 |
| POST /craftsman/requests/{id}/decline | 200, updated request | 400 already answered; 403; 404 |
| GET /craftsman/board | 200, grouped counts + totals computed on read | |
| GET /craftsman/day-sheet?date= | 200, list | |
| POST /craftsman/day-sheet/{requestId}/complete | 200, updated request | 400 — *"Only jobs on or before today can be marked done."*; 400 not accepted; 404 |
| GET /craftsman/profile | 200 | |
| PUT /craftsman/profile | 200, updated profile | 400 — *"Status and crafts can't be changed here."* for attempts to edit non-editable fields |
| GET /craftsman/notifications | 200, list; opening this clears `LastCheckedAt` | |

### Customer
| Method & path | Success | Failure modes |
|---|---|---|
| GET /customers/craftsmen?craft={id} | 200, list — Approved **and** `IsTakingWork = true` only | |
| GET /customers/craftsmen/{id} | 200 | 404 |
| POST /customers/requests | 201, created request | 400 past date; 400 price ≤ 0; 400 empty description; 400 — *"This craftsman doesn't offer that craft."*; 400 craftsman not approved (each next to its field per §4) |
| GET /customers/requests | 200, own only, paged | |
| POST /customers/requests/{id}/withdraw | 200, updated request | 400 — *"This request has already been accepted and can't be withdrawn."*; 400 already withdrawn; 403; 404 |
| GET /customers/profile | 200 | |
| PUT /customers/profile | 200 | 400 non-editable field |
| GET /customers/notifications | 200, list; clears `LastCheckedAt` | |

> Renamed `DELETE /customers/requests/{id}` → `POST /customers/requests/{id}/withdraw`. A `DELETE`
> that doesn't delete the row (§2 says requests are never hard-deleted) invites exactly the question
> a reviewer will ask. The verb now matches what actually happens.

### Shared
| Method & path | Success | Failure modes |
|---|---|---|
| POST /helper/ask | 200, `{ answer, link? }` | scoped to caller's role and reach — never offers a screen the asker can't open; 400 if question is empty |


## 6. "What's new since last looked" — decided

Using the stored-timestamp design, not the "or" left open in the draft:

- `Craftsmen.LastCheckedAt` and `Customers.LastCheckedAt` are real columns (added in §2).
- `GET /.../notifications` computes "new" as `Requests` touching that person where `CreatedAt` (for
  a craftsman, a new incoming request) or `RespondedAt` (for a customer, their request just got
  answered) is after the stored `LastCheckedAt`.
- The **same call that returns the list also updates `LastCheckedAt` to now**, server-side, in the
  same transaction as the read. Nothing is pre-computed or persisted as a running badge count —
  the timestamp is the only state kept.



## 6a. Auth specifics

- **Hashing:** ASP.NET Core `PasswordHasher<T>` (PBKDF2 under the hood) — no custom crypto, no
  plain bcrypt package needed for a project this size.
- **Token:** JWT, HMAC-signed, **60 minute** lifetime, no refresh token in scope (out of scope list
  in §9 already excludes anything session-refresh-adjacent). On expiry the Angular interceptor
  catches the 401, clears the session, and routes to `/login` — the user re-authenticates, no
  silent retry.
- **Signing key:** `appsettings.Development.json` (git-ignored) for local dev; documented as an
  environment variable for anything beyond the demo. Never committed.
- **Claims:** `sub` = UserId, `role`, and — for craftsmen/customers — the profile id, so the
  ownership check in §1 doesn't need a lookup per request.



## 7. Screens (Angular) → routes

| Screen | Route | List states needed |
|---|---|---|
| Login | `/login` | — |
| Sign up — craftsman | `/signup/craftsman` | — |
| Sign up — customer | `/signup/customer` | — |
| Craftsman: standing (pending/refused) | `/craftsman/status` | loading, error |
| Craftsman: crafts editor | `/craftsman/crafts` | loading, error, empty |
| Craftsman: incoming requests | `/craftsman/requests` | loading, error, empty |
| Craftsman: board | `/craftsman/board` | loading, error, empty |
| Craftsman: day sheet | `/craftsman/day-sheet` | loading, error, empty |
| Craftsman: profile | `/craftsman/profile` | loading, error |
| Admin: pending craftsmen | `/admin/craftsmen` | loading, error, empty |
| Admin: crafts | `/admin/crafts` | loading, error, empty |
| Customer: browse craftsmen | `/customer/craftsmen` | loading, error, empty |
| Customer: request form | `/customer/craftsmen/:id/request` | — |
| Customer: my requests | `/customer/requests` | loading, error, empty |
| Customer: profile | `/customer/profile` | loading, error |
| Not found | `**` (wildcard, last) | — |

Every row marked with list states must actually implement all three — not just the one screen
that gets rehearsed for the demo.

### 7a. List endpoint bounds
Every `GET` that returns a list: default sort (newest first, by `CreatedAt`), page size capped
(e.g. 20), `page`/`pageSize` query params. Stated once here, applies to every list endpoint in §5.



## 8. Build order (thinnest vertical slice first)

Nothing else in this domain can exist until a craft does — `Craft` has no dependencies and a real
form behind it (admin creates one, sees it in a list, refreshes, it's still there). Auth goes on
**after** that pipe is proven end-to-end, not before it.

1. `Craft` CRUD, no auth — one form → one POST → EF write → refresh → read back. Proves the whole
   Angular→API→SQLite→Angular pipe with the simplest possible entity.
2. Auth: signup (craftsman + customer), login, JWT issuance, `[Authorize]` wired up. Retrofit auth
   onto the Craft endpoints (admin-only) once the pipe from step 1 already works.
3. Admin approve/refuse craftsman queue.
4. Craftsman declares crafts worked in (`CraftsmanCrafts`).
5. Customer browses craftsmen by craft, submits a request. (§5 validations land here.)
6. **The demo moment (§0):** craftsman accept/decline, including the unique index + conflict 400.
   Build this before anything downstream of it — board, day sheet, notifications all read data
   this step produces.
7. Board, day sheet, mark-complete.
8. Notifications (`LastCheckedAt`) on both sides.
9. Withdraw, profile edit, helper widget.
10. Polish pass: loading/error/empty states everywhere (§7), list bounds (§7a), README + seed
    (§10).

Steps 1–6 are the spine; if the cut list below gets invoked, everything cut comes from 7 onward.

## 8a. Schedule — days 16–19, checkpoint per half-day

Maps the build order in §8 onto the four checkpoint days, with a named point after which the cut
list (§9) is consulted rather than improvised, and a named freeze after which only fixes land — no
new features, cut or otherwise.

| Day | AM | PM | End-of-day checkpoint |
|---|---|---|---|
| **16** | Write this plan: tables, relationships, endpoint table with every failure code, cut list, schedule — all in `plan.md`, before any code exists. | Scaffold both projects; get `AddCors`/`UseCors` and a blank Angular shell talking to a blank .NET API. | `plan.md` approved before any code exists. |
| **17** | Build order steps 1–2: `Craft` CRUD proving the full pipe, then auth (signup × 2, login, JWT, `[Authorize]`) retrofitted onto it. | Build order steps 3–5: admin approve/refuse queue, craftsman crafts-worked-in, customer browse + request creation with §5's validations. | **All three sign-ins work, and one table (`Craft`) travels screen → API → database → screen.** *If this slips past end-of-day, the cut list fires starting from the bottom (§9 item 6 first) before day 18 begins — not during it.* |
| **18 — AM** | Build order step 6: **the demo moment itself** — accept/decline, the unique index, the conflict 400 naming the job. This is the last feature-shaped work of the project. | — | — |
| **18 — midday** | — | **FEATURE FREEZE.** Whatever is built by midday day 18 is the feature set. Nothing new starts after this point, cut or not — only fixes to what already exists. | Freeze checkpoint: run through §9's "never cut" list (index, ownership check, loading/error/empty on surviving screens) and confirm each is real, not aspirational. |
| **18 — PM** | Build order steps 7–9 *if time remains under the freeze* (board, day sheet, notifications, withdraw, profile edit, helper) — each one only if the previous is done and tested, per the cut list order in §9. | One full timed run-through of the demo moment from a clean seed (§13), timed with a clock, not estimated. | Clean-clone check: `git clone` into a scratch folder, follow the README exactly, confirm it runs. |
| **19** | Polish pass (§8 step 10): loading/error/empty states, list bounds, final README pass. | Live demo. Be ready to explain any three lines a judge points at — including the accept-conflict code path in §3, since that's the one most likely to get asked about. | — |

The freeze moment is **midday, day 18** — stated once here so it isn't negotiated in the moment.

## 9. Cut list — ordered, decided now

Written in order of what goes first if behind schedule, weighed by hours saved vs. damage to the
demo moment in §0. The demo moment itself (auth, craft list, craftsman approval, crafts-worked-in,
request creation, accept/decline with the conflict rule) is never on this list.

1. **Helper widget** — nice-to-have, zero dependents, cheapest cut.
2. **Craftsman/customer profile editing** — read-only profiles still satisfy "own screen, own
   data"; editing is additive.
3. **Notifications / "what's new"** — real design work (§6) but not required to demonstrate §0;
   cut the screen, keep the schema so it's a fast add-back if time allows.
4. **Day sheet + mark-complete** — depends on accepted requests existing, but isn't part of the
   graded clash scenario itself.
5. **Board (grouped view)** — a read/aggregation over data that already exists elsewhere; lowest
   value relative to the hours it costs once loading/error/empty states are included.
6. **List pagination polish beyond a hard cap** — keep the cap (§7a), cut the page-through UI if
   short on time; a capped, unpaginated list is still correct, just not scalable-looking.

Never cut: the unique index in §3, the ownership check in §1, or any of the loading/error/empty
states on whichever screens survive the cut — a half-built screen that crashes on empty data is
worse than one less screen.



## 10. Testing the rules that can't be clicked

Named tests for the five server-enforced rules (the ones a human clicking through Postman can't
reliably exercise, above all the race):

- `Accept_ConcurrentRequests_OnlyOneSucceeds` — fire two accepts for different requests against the
  same craftsman/date concurrently (e.g. `Task.WhenAll` against the running API in a test), assert
  exactly one 200 and one 400 naming the winning request.
- `Accept_SameRequestTwice_SecondIsRejected` — idempotency on the "already answered" rule.
- `CreateRequest_CraftsmanNotOfferingCraft_Returns400` — the pairing check enforced server-side,
  not filtered out of a dropdown.
- `CreateRequest_CraftsmanNotApproved_Returns400` — same, for status.
- `GetOwnResource_WrongOwnerValidToken_Returns403` — the ownership rule from §1, checked with a
  valid token for a *different* user than the resource belongs to (not just "no token").

These are integration tests against a real (in-memory or throwaway file) SQLite instance — the
unique index behavior specifically won't reproduce against EF's InMemory provider, which doesn't
enforce constraints the same way.


## 11. Stack

- Angular + .NET API + EF Core + SQLite
- `AddCors` before `UseCors`
- JWT auth, `[Authorize]` guarding writes
- Passwords hashed (see §6a), never returned in any response

## 12. Out of scope

No payments, uploads, email/SMS, third-party login, websockets, background jobs, availability
calendar (one job = one date, not a diary).

## 13. README + seed

- `README.md` runs from a clean clone: restore, migrate, seed, run API, run Angular — numbered
  steps, no assumed state.
- `dotnet run --seed` (or a separate console command) produces: one admin, three approved
  craftsmen across different crafts, two customers, and — specifically — **two pending requests
  for the same craftsman on the same date**, so the demo moment in §0 is one command away instead
  of five minutes of clicking to set up live.
