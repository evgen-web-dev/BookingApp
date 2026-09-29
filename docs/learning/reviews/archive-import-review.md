# Archive import review — one run over `docs/learning/archive/`

Run: `/import-history 7` · 2026-09-29 · lens = Phase 7 (JSON data importer: hosts +
apartments, idempotent, provenance recorded).

Sources read in full: `archive/README.md`, `master-original.md`, `phase-3.md`,
`phase-4.md`, `phase-5.md`, `phase-6.md`. Audited against `src/`, `tests/` and
`git log`. Nothing in the archive was edited.

Every code claim below is a source read in this session. **No build, test, HTTP or
psql command was run** — no prediction was on file for one, so every "the code
does X" statement here is *source-verified, runtime-unverified*.

---

## 1. Contradictions — archive claims the code does not support

Ordered by how much they bear on Phase 7.

### 1.1 `P5-01`'s directive was not followed by Phase 6 — and Phase 7 owns it
> "**New DTOs — including Phase 6's — should use `required` init members from the
> start.**" — `master-original.md:283` (`P5-01`), repeated at `:336`

Of the eight DTOs Phase 6 added, exactly **one** uses `required`:

| DTO | Line | Shape |
|---|---|---|
| `Common/PageQueryParams.cs` | `:5-8` | `required` ✅ |
| `Booking/CreateBookingRequest.cs` | `:5-7` | plain `init` |
| `Booking/BookingResponse.cs` | `:5-9` | plain `init` |
| `Booking/MyBookingsPaginatedRequest.cs` | `:5` | inherits `PaginatedRequest` |
| `Apartment/ApartmentDetailsResponse.cs` | `:6-8` | `init` + `= default!` |
| `Apartment/AvailableApartmentsPaginatedRequest.cs` | `:7-8` | `set`, not even `init` |
| `Common/PaginatedRequest.cs` | `:5-6` | `init` with literal defaults |
| `Common/PagedResult.cs` | `:5-6` | `init` + `= []` |
| `Common/PaginatedResponse.cs` | `:18-21` | `init` + `= []` |

Phase 6's own close-out states the opposite as achieved: "`required` forces
named-initializer construction (kills the positional `pageNumber`/`pageSize` swap)"
(`phase-6.md:220`) — true of `PageQueryParams` alone, not of the phase.

This is the single most load-bearing contradiction for Phase 7: `current-phase.md`
names `P5-01`/`C-07` as debt the phase **must honour, not fix**, and the archive's
claim that the convention is already in force is false. The importer's JSON payload
DTO would be the second DTO in the codebase to use `required`, not the ninth.

### 1.2 Phase 6 states a response-DTO convention its own response DTOs break
> "validated *request* DTOs use property-init records with nullable props …
> *response* DTOs use positional records" — `phase-6.md:95`

`ApartmentDetailsResponse.cs:3-10`, `BookingResponse.cs:3-9` and
`PaginatedResponse.cs:3-21` are all init-property records. Only the Phase-3-era
`RegisterResponse.cs:3`, `LoginResponse.cs:3`, `RefreshResponse.cs:3`,
`IssuedTokens.cs:3` and `CreateUserResult.cs:3` are positional. The convention
describes the substrate Phase 6 inherited, not what Phase 6 wrote — and it is
stated as a live rule for sub-chats to follow.

### 1.3 `P4-03` describes a line of code and a map row that do not exist
> "`RequireUniqueEmail = false` leaves the `DuplicateEmail`/`InvalidEmail` map rows
> **dead** — remove the rows or flip the flag." — `master-original.md:276`

- There is **no** `RequireUniqueEmail` anywhere in `src/` (grep: zero hits).
  `Infrastructure/DependencyInjectionExtensions.cs:34-36` is a bare
  `AddIdentityCore<User>().AddRoles<…>().AddEntityFrameworkStores<…>()` with no
  options lambda. The claim rests on the framework default being `false`, which is
  not stated and was not verified here.
- There is **no `InvalidEmail` row**. `IdentityErrorCodesDefaultDenyMapper.cs:22-24`
  and `:36-38` carry `DuplicateUserName`, `DuplicateEmail`, `InvalidUserName` — the
  named pair is half wrong.

Already flagged as unverified in `handoff.md` §Still unverified and deliberately
left unedited so this run's diff would be clean. Phase 7 has a direct stake: an
importer re-run that hits an existing email needs to know *which* Identity code it
gets back, and this entry is not a reliable answer.

### 1.4 `P6-03` exists in the phase-6 archive and in no live ledger
> "`P6-03` | `FindAvailableAsync` safe by upstream guard, not locally …" —
> `phase-6.md:167`

It appears in the Phase 6 master's own debt table and **never** reached
`master-original.md`'s §Debt Ledger, so it never reached `debt.md`. It is the only
ID in the entire archive that was lost in the roll-up (see §4 for the move).

Still live in code: `ApartmentRepository.cs:18-22` takes `DateTime? startDate,
DateTime? endDate` and filters `booking.CheckIn < endDate && booking.CheckOut >
startDate`; both-null works only because SQL `NULL` comparisons are never true.
`AvailableApartmentsRequestValidator.cs:14-21` is what guarantees both-or-neither.

### 1.5 `P3-13`'s scope understates where the clock actually is
> "The **auth token services still read `DateTime.UtcNow` directly**" —
> `master-original.md:267`; "the clock seam now exists in the app" (for booking code)

`DateTime.UtcNow` call sites in `src/`: `AuthService.cs:107,108,121,122,163,194,195`
(Application, not a "token service"), `RefreshTokenRepository.cs:34,43`,
`TokenFamilyRepository.cs:29`, `JsonWebTokenService.cs:33`,
`RegisterRequestValidator.cs:83,85` — **and** `ApartmentsWithBookingsSeeder.cs:142`,
which stamps `Booking.CreatedAt` with `DateTime.UtcNow` even though
`TimeProvider.System` is registered at `Infrastructure/DependencyInjectionExtensions.cs:25`
and the seeder is booking code. The retrofit is larger than "auth token services",
and the booking-code claim has one counter-example inside the file Phase 7 is
using as its reference.

### 1.6 The documented create-booking transaction shape is not the code's shape
> "reuse the existing `BeginTransactionAsync` → work → `CommitAsync` pattern" —
> `phase-6.md:63`, repeated `master-original.md:239`

`BookingService.cs:55-67`: the entity is built, mapped and `Add`ed at `:55-61`,
**before** `BeginTransactionAsync` at `:63`; the `try` block contains only
`CommitAsync`. Compare `AuthService.RegisterAsync:58-76`, which begins first and
does the work inside. The outcome is the same (`Add` only tracks; the INSERT is
emitted by `SaveChangesAsync` inside `CommitAsync`) — but "work" and "Begin" are
in the opposite order from the stated pattern, and the two services now differ.

Phase 7's open question about the transaction boundary is asked against this
pattern, so it matters which shape is actually precedent.

### 1.7 "Idempotent (check-exists-then-create)" describes one mechanism; the seeder has two
> "dev-gated idempotent seeder" — `master-original.md:117`; "**idempotent**
> (check-exists-then-create)" — `phase-6.md:93`

`ApartmentsWithBookingsSeeder.cs:22-25` short-circuits the **whole** seeder on
`appDbContext.Set<Apartment>().AnyAsync()` — apartments are the idempotency key for
users, apartments and bookings alike. Separately, `SeedHostUser:51` and
`SeedClientUser:87` each recover an existing user by `FindByEmailAsync`. So the
per-entity check the docs describe exists only for users, and it is reachable only
when the apartment guard lets execution through.

That combination is exactly the crash-recovery shape `handoff.md` flags as
unverified, and exactly what Phase 7 has to decide for itself. The archive's
one-word "idempotent" does not transfer.

### 1.8 Phase-3 inventory lists a type that does not exist, and its deletion is unrecorded
> "`DTOs/Auth/`: … `ErrorResponse`" — `phase-3.md:167`; "`BadRequest(ErrorResponse(codes))`"
> — `phase-3.md:97`

No `ErrorResponse` type exists in `src/`. It was presumably displaced by the RFC 7807
contract in Phase 4, but `P3-11`'s orphan list names only `LoginResult`,
`LogoutRequest` and `UnitOfWork.SaveChangesAsync` (all three genuinely gone —
verified). This is the standing **Orphan check** failing on its own terms: a type
left the codebase and no ledger line records it.

### 1.9 Two smaller drifts, recorded for completeness
- **Seeded booking count.** "Client + **26** bookings" (`master-original.md:117`)
  vs 20 `BookingFactory` calls (`ApartmentsWithBookingsSeeder.cs:186-235`) and the
  seeder's own table (`:146-180`). `phase-6.md:108` says 20 and is right.
- **`P3-Q…` renumbering scheme never happened.** "§6's open items migrate and are
  re-prefixed `P3-Q…`" (`phase-3.md:514`); the program ledger uses `P3-01`…`P3-20`
  and no `P3-Q` ID exists anywhere.
- **`MapInboundClaims` dropped from `P4-04`'s batch.** Listed in the Phase 4
  archive's carry-forward (`phase-4.md:58`), absent from `P4-04` in
  `master-original.md:277`. The setting is in fact live at
  `API/DependencyInjectionExtensions.cs:39`, and the archive itself calls it inert
  — so nothing is broken; the item vanished without being closed.
- **`phase-6.md:102` documents `Task<List<Apartment>> FindAvailableAsync(DateTime?,
  DateTime?)`**; the shipped signature is `Task<PagedResult<Apartment>>
  FindAvailableAsync(DateTime?, DateTime?, PageQueryParams, CancellationToken)`
  (`IApartmentRepository.cs:9`). Superseded by 6.3 inside the same document, which
  never updates §4.1.

### 1.10 The role whitelist is not enforced where the archive says it is
*(found during the §3 walk, 2026-09-29, after this report's first draft)*

> "Role gate = **Domain-level** `Roles.RolesAvailableForPublicRegistration`
> (`HashSet`), **checked in `AuthService`**" — `phase-3.md:95`
> "**`RegisterAsync` (Application):** Domain role-whitelist (fail-fast, pre-tx) →
> `Begin` → …" — `phase-3.md:99`; "role self-registration is whitelisted in
> Domain" — `master-original.md:114`

`AuthService.RegisterAsync` (`AuthService.cs:54-89`) contains **no** whitelist
check: it maps the request, begins the transaction, calls `CreateAsync`, then
`AddToRoleAsync`, then commits. A grep for the set returns exactly two call sites,
both in the API layer: `RegisterRequestValidator.cs:91-92`.

So the *set* is Domain-owned as described (`Roles.cs:10`), but the *check* runs only
in an API validator. The archive's claim that the gate is enforced in the
Application use-case is false.

Consequence for Phase 7, and it cuts both ways: any caller that does not pass
through the API — a seeder, an importer — gets no role-whitelist enforcement at
all. `ApartmentsWithBookingsSeeder.cs:48` supplies `Roles.Host` as a literal, so it
never needed one. An importer assigning roles from JSON would be the first caller
where "which roles may this path assign?" has no answer in code. Note that for an
acquisition import the *public-registration* whitelist may well be the wrong gate
anyway — the set is named for self-registration, not for privileged paths.

### 1.11 Claims that checked out (so the contradictions above are not selection bias)
`P3-01` partial unique index (`UserEntityTypeConfiguration.cs:14-16` +
`Migrations/20260814180923_…cs:17-22`) · `P3-05` guarded revoke
(`TokenFamilyRepository.cs:24-26`) · `P3-11` orphans gone · `P3-12` `FrozenSet`
(`Roles.cs:10-12`) and `ErrorDuringTokenRefresh` gone · `P6-FIX-200` fixed
(`API/DependencyInjectionExtensions.cs:110`) · `P6-01` racy check commented
(`BookingService.cs:49`) · `P6-06` null-fold still present
(`OperationResult.cs:35-37`) · `date`/`timestamptz` columns and the composite index
(`BookingEntityTypeConfiguration.cs:25-38`) · one-live-token partial index
(`RefreshTokenEntityTypeConfiguration.cs:35-37`) · `ClockSkew` 2 min
(`API/DependencyInjectionExtensions.cs:52`) · 27 tests in three classes ·
`P5-02` regex-per-call still live (`RegisterRequestValidator.cs:47-48,56-57,66-67`)
· `P5-04` `"J.Jack"` still unpinned (no match in `tests/`) · `Microsoft.NET.Test.Sdk`
still `17.4.1` (`tests/BookingApp.UnitTests/BookingApp.UnitTests.csproj:18`).

---

## 2. Classification of the rest of the archive

### 2.1 Rationale still load-bearing for Phase 7
Phase 7 must honour these or explicitly overturn them. Each is a shortlist
candidate in §3.

- **Identity writes are not transaction-aware.** `UserManager.CreateAsync` /
  `AddToRoleAsync` each `SaveChanges`; atomicity comes only from the ambient
  `DbContext` transaction (`phase-3.md:277`). This *is* Phase 7's transaction-boundary
  open question, already answered in part by the archive.
- **`IUnitOfWork` is a transaction scope, not a save path.** `CommitAsync` saves
  internally; a standalone `SaveChangesAsync` was deliberately removed as a foot-gun
  (`phase-3.md:147`, G1). Constrains how the importer persists apartments.
- **Users must be created through the Identity path** (`UserManager` or
  `IUserIdentityService`) for hashing and normalization; plain entities go through
  the `DbContext` (`phase-6.md:93`). The importer's central mechanism.
- **Role seeding must run before data seeding** — the Host role must exist before
  it can be assigned (`phase-6.md:93`; `Program.cs:34` then `:47`). Ordering
  constraint for any import entry point.
- **Ports are shaped by the caller's available data, not the wrapped library**
  (`phase-3.md:113,298`). The importer needs lookups that no port exposes today —
  `IUserIdentityService.cs:9-12` has no find-by-email, `IApartmentRepository.cs:9-10`
  has no `Add`. Both gaps are real and this is the rule that decides their shape.
- **Error codes, not messages, cross the Application boundary; unknown Identity
  codes are dropped by a default-deny allowlist** (`phase-3.md:96`). Decides how
  per-record import failures are reported.
- **Explicit `timestamp with time zone` for any UTC `DateTime` column** — Npgsql
  throws at write time otherwise (`phase-3.md:144,396`). Binds any "imported at"
  provenance timestamp.
- **An invariant the DB can enforce is enforced there; a partial unique index is
  the only tool for conditional uniqueness** (`phase-3.md:142,395`). Bears directly
  on the "new indexes/constraints" open question — a provenance uniqueness rule
  (one imported user per external id) is exactly this shape.
- **Construction discipline** — collapse two same-typed values describing one
  concept, make the safe construction the only door (`phase-6.md:216-220`). Bears
  on the import DTO and on any provenance value.
- **Merge on shared cause, not shared shape** (`phase-3.md:410`). The importer's
  host creation *looks* like `RegisterAsync`; whether it may reuse it is this rule's
  question.
- **YAGNI with a named trigger; "what specific failure does this address?"**
  (`phase-3.md:351,480`). The discipline for the provenance-shape and
  extra-index questions.
- **Domain entities never cross the Application boundary** (`phase-6.md:95,198`).
- **Store a value once rather than copying it** — the family-ceiling argument
  (`phase-3.md:137`, `master-original.md:339`). Provenance recorded in one place.

### 2.2 Rationale about settled history, unlikely to be reopened
Read once, don't carry: the refresh-token design in full (opaque-not-JWT, SHA-256
lookup-by-hash, single-use rotation, no grace window, `ExecuteUpdateAsync` over
`xmin`, 32-byte sizing, sliding TTL under an absolute ceiling, cookie transport and
the CSRF reasoning) — `phase-3.md:132-139`; the JWT issuance/validation package
split, `ValidAlgorithms` pinning, `ClockSkew` reasoning — `phase-3.md:110-121`; the
`Session`→`TokenFamily` rename; the half-open `[check-in, check-out)` overlap
formula and its boundary table — `phase-6.md:135-153`; the pagination decisions
(default-not-floor 5, reject low / clamp high, `TotalCount` via a second
`CountAsync`) — `phase-6.md:209-214`; the whole orchestrator/launch-extract process
machinery — `master-original.md:451-517`, superseded by this harness.

### 2.3 Descriptions of what exists — the code owns these now
`master-original.md` §Auth Architecture (`:151-204`) and §Booking Domain
(`:208-240`); `phase-3.md` §4 file inventory (`:165-169`); `phase-6.md` §4 substrate
facts (`:80-96`) and §4.1 (`:99-108`). All were audited against source in §1;
where they drift, §1 says so. They are useful as a reading order for Phase 7, not
as authority.

One description worth keeping as an *entry point* rather than a fact: the seeded
credentials `host_seeded_user_john_doe@example.com` / `client_seeded_user_bob_smith@example.com`,
password `Pa$$word1` (`phase-6.md:108`) — confirmed at
`ApartmentsWithBookingsSeeder.cs:46-48,82-84`.

### 2.4 Open questions never resolved
- **`P6-06`/`P3-15` — `OperationResult<TValue>.Success(null)` folds to a codeless
  failure.** Still live at `OperationResult.cs:35-37`. Explicitly Yevhenii's call,
  not the mentor's (`master-original.md:430`). Phase 7 reaches it the moment an
  import step returns a value through `OperationResult<T>`.
- **`P6-07`** — whether to adopt the implicit `Success` operator solution-wide.
  Still scoped to `BookingService` (`OperationResult.cs:44`, used at
  `BookingService.cs:68,91,103`).
- **`P6-09`** — sort key for paginated lists.
- **`Microsoft.NET.Test.Sdk` 17.4.1 → 17.14.1** — called "still in-flight"
  (`master-original.md:141`); csproj still pins 17.4.1. **Has no ledger ID**, so it
  lives only in archive prose and would be lost by this import.
- **Validator relocation** — validators live in `API/Validators/` but validate
  Application DTOs, forcing the test project to reference the API
  (`tests/BookingApp.UnitTests.csproj:26`). Recorded in `phase-5.md:116` with **no
  ledger ID**; not covered by any `P5-xx`.
- **`P3-07`** — whether an explicit request-size cap ever shipped; still unconfirmed.
- **Q15 `MapInboundClaims`** — reasoned as inert, never pinned (`phase-3.md:214`).
- **How `AddToRoleAsync` fails on a role that doesn't exist** *(surfaced during the
  §3 walk, 2026-09-30)*. **Unverified:** the failure may be an exception from the
  user store rather than a failed `IdentityResult`. If it throws, the seeder's own
  `if (!addToRoleResult.Succeeded) throw` branch
  (`ApartmentsWithBookingsSeeder.cs:67-70`) never fires for that case, and an
  importer treating role assignment as a per-record error would instead take an
  exception — which by `C-02` becomes a 500 and ends the whole run rather than
  reporting one bad record. **Decides** whether role assignment belongs inside
  per-record error handling or in a pre-flight check. **Probe:** assign a garbage
  role name and observe. Filed *verify-first*, following the precedent of the
  `ObjectResult` status hypothesis (`phase-5.md:109`) — recorded as a hypothesis,
  proved, and only then ledgered as `P6-FIX-200`. It is deliberately **not** in
  `debt.md` (nothing is known broken) and **not** in `decisions.md` (nothing was
  chosen); its home is `current-phase.md` §Open questions, which is the owner's to
  author.

### 2.5 Debt items with ledger IDs
41 rows carry IDs in `master-original.md` §Debt Ledger; all 41 are already in
`debt.md`. One further ID, **`P6-03`**, exists only in `phase-6.md:167`. See §4.

---

## 3. SHORTLIST — candidate decisions

Proposed IDs land in `decisions.md` §Pre-harness, except the eight marked
"already present", which keep their live IDs. **No entries written yet** — §5 of the
skill walks these one at a time, on your word.

Items 1–8 are the hand-lifted `C-01`–`C-08`, now checked against their sources for
the first time. Items 9–22 are new.

| # | Proposed ID | One-line summary | Source |
|---|---|---|---|
| 1 | **already present as `C-01`** | One failure literal per auth flow; a second literal reopens enumeration silently | `phase-3.md:148,216` (Q17), `master-original.md:357` — Phase 3/4 |
| 2 | **already present as `C-02`** | `500` means exactly "nothing caught or classified it" | `master-original.md:356`, `phase-4.md:23` — Phase 4 |
| 3 | **already present as `C-03`** | Error-placement discriminator: payload-resolvable + no I/O + no leak ⇒ field validation | `master-original.md:355` — Phase 4 |
| 4 | **already present as `C-04`** | An interface is owned by the layer that depends on it; invert with a port, don't relocate the use-case | `phase-3.md:289-292`, `master-original.md:111,329` — Phase 3 (revised) |
| 5 | **already present as `C-05`** | Adapters (`Infrastructure/Services`) vs orchestration (`Application/Services`) decides the sub-layer | `phase-3.md:223` (Q24), `master-original.md:335` — Phase 3/4 |
| 6 | **already present as `C-06`** | An invariant the DB can enforce is enforced there; partial unique index is the tool for conditional uniqueness | `phase-3.md:142,395`, `phase-6.md:59-60` — Phase 3/6 |
| 7 | **already present as `C-07`** | New DTOs use `required` init members | `master-original.md:283` (`P5-01`), `phase-5.md:47` — Phase 5. ⚠ §1.1: stated as adopted, adopted once |
| 8 | **already present as `C-08`** | Validators reject; services transform — the filter never re-reads a mutated DTO | `phase-6.md:223` — Phase 6 |
| 9 | `D3-01` | Ports are shaped by the caller's available data, never mirrored from the wrapped library's surface | `phase-3.md:113,298,376` — Phase 3 |
| 10 | `D3-02` | `IUnitOfWork` is a transaction scope; `CommitAsync` saves internally and no standalone save path is exposed | `phase-3.md:91,147` (G1) — Phase 3 |
| 11 | `D3-03` | Identity writes self-save; atomicity across `CreateAsync`+`AddToRoleAsync` comes only from the ambient transaction | `phase-3.md:277`, `phase-3.md:99` — Phase 3 |
| 12 | `D3-04` | Error **codes**, not messages, cross the Application boundary; unknown Identity codes are dropped by a default-deny allowlist | `phase-3.md:96,301` — Phase 3 |
| 13 | `D3-05` | Any UTC `DateTime` column is declared `timestamp with time zone` explicitly; the column type is the enforcement point | `phase-3.md:144,396` — Phase 3 |
| 14 | `D3-06` | Merge on shared *cause*, not shared *shape* | `phase-3.md:410` — Phase 3 |
| 15 | `D3-07` | YAGNI with a named trigger condition; machinery that "sounds responsible" must name the failure it addresses | `phase-3.md:351,379,480` — Phase 3 |
| 16 | `D3-08` | Store a value once rather than copying it, so no code has a reason to recompute it wrongly | `phase-3.md:137,414` — Phase 3. *Adjacent to `C-06`; may belong inside it* |
| 17 | **already present as §Standing checks — ID exposure** | `int` PKs are fine while every client-exposed ID is independently authorized — carry the check, not the answer | `phase-3.md:145,391` — Phase 3 |
| 18 | `D4-01` | Unified RFC 7807 surface: `ValidationProblemDetails` for field failures, `ProblemDetails` + `errorDetails` code array for domain failures, empty ⇒ `UnexpectedError`/500 | `master-original.md:194-198`, `phase-4.md:23` — Phase 4. *Binds only if the importer has an HTTP surface* |
| 19 | `D5-01` | Test conventions: mutation probes, exact argument matching for pass-throughs, explicit `Verify(Times.Never)` wherever `MockBehavior.Strict` is off | `phase-5.md:52-58,120`, `master-original.md:360-368` — Phase 5. *Arguably principles, not a constraint — your call* |
| 20 | `D6-01` | Construction discipline: collapse drift-prone same-typed values into one already-correct value and make the safe construction the only door | `phase-6.md:216-220` — Phase 6 |
| 21 | `D6-02` | Users are created through the Identity path (`UserManager` / `IUserIdentityService`); plain entities go through the `DbContext` | `phase-6.md:93` — Phase 6. *Archive states this as a substrate fact, not a decision* |
| 22 | `D6-03` | Role seeding runs before any data seeding that assigns a role | `phase-6.md:93`; observable at `Program.cs:34,47` — Phase 6 |

**22 candidates.** Under the ~25 cap, so no narrowing was needed; the cut was made
by relevance to Phase 7, not by count (see §5).

### 3.1 Walk outcomes *(step 7, 2026-09-30)*

All 22 walked one at a time. **22 accepted, 0 rejected** — so no entry carries a
`REJECTED` marker, and nothing was deleted from `decisions.md`.

**Accepted unchanged (6)** — wording confirmed faithful to its source:
`C-01`, `C-02`, `C-03`, `C-05`, `C-07`, §Standing checks *ID exposure* (#17).

**Accepted with an amendment to the live entry (3):**
- `C-04` — added the boundary test ("a concrete dependency *within* a layer is fine;
  a use-case depending on a concrete *across* the boundary is the violation",
  `master-original.md:330`), and corrected the revision site from `Phase 0` to `3.2`.
- `C-06` — added the tool line (`CHECK` can't reference other rows, so a partial
  unique index is the tool for conditional uniqueness, `master-original.md:341`).
- `C-08` — `Why` rewritten. The original mechanism ("the filter never re-reads a
  mutated DTO, so normalization is discarded") does not survive a read of
  `AsyncValidationFilter.cs:31-39`: the same instance reaches the action, so a
  mutation would *persist*, not be discarded. Right conclusion, wrong stated
  mechanism — the tracked failure mode, caught here.

**Accepted as new §Pre-harness entries (13):** `D3-01`, `D3-02`, `D3-03`, `D3-04`,
`D3-05`, `D3-06`, `D3-07`, `D3-08`, `D4-01`, `D5-01`, `D6-01`, `D6-02`, `D6-03`.
Four were shaped during the walk rather than accepted as proposed:
- `D3-07` — accepted **with** the service-wrapper corollary (`master-original.md:333`).
- `D5-01` — **narrowed** from eight test conventions to the two that decide whether
  a test asserts anything (mutation probe + exact argument matching; explicit
  `Verify(Times.Never)` under loose mocks). The six technique items stay in
  `phase-5.md`.
- `D6-01` — `Known limit` trimmed to the *structural* limit; the current
  two-call-site violation was split out as debt (`P7-03`) rather than recorded
  inside a decision, per CLAUDE.md's "don't convert one into the other".
- `D6-02` — `Revisit if` **supplied by the owner** (the archive records none): "the
  `NormalizedEmail` unique-index filter changes, or user creation stops going
  through ASP.NET Core Identity". Its `Why` was also strengthened beyond the
  archive's "to get hashing/normalization" to name the consequence the archive
  misses — an EF-graph insert leaves `NormalizedEmail` null, which puts the row
  outside the *filtered* unique index and so outside `C-06`'s guarantee entirely.

`D6-02` and `D6-03` were recorded in the archive as **substrate facts**, not
decisions; promoting them was an explicit owner call, noted in both entries.

Every `Verified by:` still reads "not yet" — no build, test, HTTP or `psql` command
ran during the walk.

---

## 4. Debt move — result

`debt.md` already held all 41 ID-carrying rows from `master-original.md` §Debt
Ledger (moved before this run).

- **Moved this run: 1** — `P6-03`, from `phase-6.md:167`, appended to §Introduced in
  Phase 6 with its ID intact. Its source table has four columns (ID / Item / Status
  / Notes) against `debt.md`'s two, so Status was folded into the Item cell; **no
  word was changed**, and a footnote in `debt.md` records the collapse.
- **Matched verbatim: 40.**
- **Differed: 1** — `P3-16`. `debt.md` reads "**Confirmed live 2026-09-26:**
  `decisions.md` §Standing constraints is that list, and it is `@`-imported by
  CLAUDE.md, so it is consulted every turn by construction. Carry it forward.";
  the archive reads "Scheduled to start in Phase 4; **confirm it's live** and carry
  it forward." This is a harness-era update that answered the archive's own
  question, not drift. **Not touched** — reported only, per the skill.
- **Missing entirely: 0** after the `P6-03` move.

**Added during the §3 walk — not moves (2):** `P7-02` (`AvailableApartmentsPaginatedRequest`
uses `set`, not `init`) and `P7-03` (`PageQueryParams` built from a raw
`PaginatedRequest` in two places, in two layers — contradicting `phase-6.md:220`'s
claim that the escape hatch is read in exactly one place). Both are new findings
from reading code during the walk, both are outside Phase 7's scope, and both were
given IDs by the owner. `P7-01` is deliberately unassigned and remains free.

Two un-IDed debt items were found and **not** moved, because inventing IDs for them
is not this skill's job: the `Microsoft.NET.Test.Sdk` bump and the validator
relocation (§2.4). If you want either tracked, they need `P7-xx` IDs you assign.

---

## 5. What I would deliberately leave behind

- **The whole orchestrator / launch-extract / sub-chat machinery**
  (`master-original.md:451-517`, `phase-5.md:183-189`, `phase-6.md:7-16`,
  `phase-3.md:484-506`). This harness replaced it: `current-phase.md` is the boarding
  pass, `/sync` writes the handoff, `/plan-review` is the gate. Importing it would
  create two competing process descriptions.
- **The "mentor is the decision authority" framing** (`phase-3.md:24`). The archive
  itself records the handover — "now Yevhenii's call, not the mentor's"
  (`master-original.md:429`). Carrying it forward would re-open an authority that
  has already moved.
- **The audit-failure logs** (`phase-3.md:424-481`, `phase-4.md:63-67`,
  `phase-5.md:162-168`). They are calibration history, already distilled into
  `master.md` §Calibration. Re-importing the instance lists adds volume, not
  constraint.
- **Per-sub-chat status tables and tracker rows** (`phase-3.md:43-48`,
  `phase-6.md:180-187`, `phase-5.md:85-97`). Pure bookkeeping for a workflow that no
  longer runs.
- **The refresh-token and JWT design rationale in full** (§2.2). It is excellent and
  it is finished; Phase 7 touches none of it. Leaving it in the archive keeps it
  recoverable without putting it in front of every turn via the `@`-imported
  `decisions.md`.
- **The overlap formula and pagination decisions.** Phase 7 imports apartments and
  explicitly **not** bookings (`current-phase.md` §Out of scope), so neither binds
  this phase. If a later phase imports bookings, re-run the question then.
- **`phase-5.md`'s scaffolding facts** (xunit3 `--test-runner vstest`,
  `xunit.v3.mtp-off`, `OutputType=Exe`). True and verified in the csproj — but the
  csproj is the source of truth for them now.
