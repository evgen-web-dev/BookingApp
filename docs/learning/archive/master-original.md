# Booking App — Learning Path Master Document


## Context for Claude

This document tracks overall progress across the project and is the parent of the per-phase archives.
The reader is a developer transitioning into .NET backend (junior/trainee level),
with prior background in JavaScript, PHP, WordPress, and some Laravel.
Each Phase is studied in a separate chat.

IDE: JetBrains Rider (non-commercial license). SDKs installed: .NET 8 and .NET 10.
The project targets **.NET 10**.

**How this document relates to the others.** This is the **program-level** doc: durable architecture,
live debt, principles, phase status. Per-phase archives hold the detailed reasoning and are *pointed at*
from here, not duplicated. This doc is not pasted raw into working chats — each phase gets a narrow
**launch extract** generated from it, scoped to that phase's work (see §Process). The **Debt Ledger** is
the single live ledger for the project: phase archives keep their historical tables, but new debt is
recorded here.

**Teaching mode (applies to every chat):** be a constructive critic, not a yes-man.
Challenge weak reasoning, hidden assumptions, and overconfidence — target the *reasoning*,
not just the conclusion. Separate fact from opinion from uncertainty. Verify claims with
tooling (real HTTP, real DB state, forced failures) rather than accepting explanations.
Timebox low-stakes reversible decisions. **Gate on unresolved decisions — don't list them
and move on.** Ask for real source before designing against it.

---

## Project Goal

Build a booking system resembling Airbnb — starting with user registration,
login, JWT-based authentication, and roles (Client, Host; Admin planned later).

Stack: ASP.NET Core, PostgreSQL, Entity Framework Core, Clean Architecture,
xUnit + Shouldly + Moq for tests, FluentValidation.

This will be a portfolio project on GitHub — real-world quality, not a toy.
It is the first part of a larger app that will be extended later.

---

## Phases

### Phase 0 — Orientation *(no code)* ✅
Understand Clean Architecture as a concept before touching the project.

### Phase 1 — Project Scaffold ✅
Solution + layers + one request flowing end-to-end.

### Phase 2 — Data Layer ✅
EF Core + PostgreSQL, migrations, User entity.

### Phase 3 — Auth ✅
Registration, login, JWT access tokens, refresh tokens, Client/Host roles.
*Run as four sub-chats (3.1 data layer, 3.2 register+login, 3.3 access tokens, 3.4 refresh tokens).*

### Phase 4 — Hardening ✅
The API behaves predictably on bad input; errors are informative and uniform. Ran as a phase-level
orchestrator with two sub-chats — 4.1 (predictable errors & input) and 4.2 (auth correctness & cleanup),
both mentor-approved.
**Delivered:** partial unique index on `NormalizedEmail` (`P3-01`), global `IExceptionHandler` + unified
RFC 7807 `ProblemDetails` contract (`P3-02`/`P3-03`), `ILogger` (`P3-04`), FluentValidation via action
filter, idempotent family revocation (`P3-05`), cookie-cleared-on-failed-refresh (`P3-08`), explicit
commit/rollback on early returns (`P3-09`), `TokenFamilyService` → Application (`P3-10`), orphan +
housekeeping cleanup (`P3-11`/`P3-12`), and Mapster global static config → injected `IMapper`
(`P3-14`, pulled forward).
**Deferred (not descoped):** rate limiting (`P3-06` + global) — account lockout would break the
single-failure-literal enumeration invariant. **Full detail: phase-4 archive.**

### Phase 5 — Tests ✅
xUnit + Shouldly + Moq. **Mentor-PR-approved and synced.** Closed via **Option A** — the committed core
(unit tests for `AuthService.RegisterAsync` + `AuthService.LoginAsync`, plus the register/login request
validators) was delivered and approved; recommended extensions (enumeration-invariant test, pure-service
tests, `RefreshAsync`/`LogoutAsync` unit tests) were deferred as one coherent future testing pass, and
integration tests were out of scope for the whole phase. **27 tests** in a new `BookingApp.UnitTests`
project. ⚠️ **Correction to the prior interim note:** Phase 5 was *not* purely additive — a DTO shape
refactor (positional records → init-only) touched production code in Application + Infrastructure
(`RegisterRequest`/`LoginRequest`/`AuthenticatedUserResult` and two sites in `UserIdentityService`). Per
the close-out's own diff audit this does **not** overlap Phase 6's booking code. **Full detail: phase-5
archive.**

### Phase 6 — Booking core ✅
The first business domain on top of the Phase-4 auth substrate, added **additively** (no re-architecture).
Ran as three sub-chats — 6.1 (read), 6.2 (write + security), 6.3 (pagination, pulled in from deferred).
**Shipped:** `Apartment` + `Booking` entities (EF configs + migration `AddApartmentsAndBookings`; `date`
columns for check-in/out, composite index; dev-gated idempotent seeder); a **public** paginated
available-apartments query with an optional both-or-neither date filter; `[Authorize(Roles = Client)]`
booking creation (client id from JWT, with apartment-exists / not-past / no-overlap checks); caller-scoped,
ownership-checked `GET /api/bookings` and `/{id}` (**404 for both missing and not-owned**); offset
pagination on both lists. **Injected `TimeProvider`** for past-check-in rejection (booking code only).
**Mentor-locked model, as built:** half-open `[check-in, check-out)`, back-to-back allowed, date-only UTC
comparison with **`date` columns as the real guarantee**; no status lifecycle; availability from bookings.
**Stands (deliberate):** no concurrency / double-booking prevention (`P6-01`) and no automated tests.
**Full detail: phase-6 archive.**

### Phase 7 — *(next — scope not yet decided)*
Candidate areas from the Phase 6 close-out (none committed): Host endpoints (apartment create/edit/manage,
then revisit `P6-05`); booking cancellation / lifecycle; the role-gating decision (`P6-08`); the
paginated-list sort-key decision (`P6-09`); the `P6-06`/`P3-15` null-fold call; solution-wide adoption of
the implicit `Success` operator (`P6-07`); Hosts seeing bookings on their own apartments (a filter
dimension, not a role branch); integration tests (starting with the pure overlap function,
`P6-TEST-OVERLAP`). A launch extract will be generated once scope is chosen.

---

## Progress Log

| Phase | Status | Key things learned / decisions made |
|-------|--------|--------------------------------------|
| 0 | ✅ Done | Dependency rule: the arrow always points inward. Four layers: Presentation → Application → Domain ← Infrastructure. Domain knows nothing about anyone. Application can be driven by HTTP, CLI, or tests — Presentation is just one driver. **Interface placement (revised in Phase 3): an interface is owned by the layer that *depends* on it, not the layer that implements it.** Infrastructure still implements; the contract lives with its consumer. |
| 1 | ✅ Done | Four projects created: Domain, Application, Infrastructure, API (webapi template). References wired via CLI. API is the composition root — references both Application and Infrastructure to wire DI at startup. `Program.cs` registers services and maps controllers. First endpoint `GET /api/status` flows through API → Application, returning `AppStatusDto`. Domain and Infrastructure have no role yet — forcing them into a status endpoint would be dishonest. `ActionResult<T>` preferred over `IActionResult` *(justification corrected in Phase 3: the type-safety benefit holds for the **declared** return type and for Swagger/OpenAPI, but **not** for the `Ok()` path — `Ok(object)` returns a non-generic `ActionResult` that converts implicitly without checking the payload. Preference survives; the original stated reason was partly wrong)*. Controller route convention: `[Route("api/[controller]")]`. |
| 2 | ✅ Done | Active Record vs Data Mapper: EF Core keeps Domain entities plain, persistence logic in `DbContext`. **`DbContext` in Infrastructure because of what it depends on, not what it does.** Fluent API over annotations, to avoid leaking Infrastructure concerns into Domain. `DbContext` lifetime: Scoped via `AddDbContext` — one instance per HTTP request. `CancellationToken` always from the caller, never instantiated internally. Eager loading: `.Include()` must be explicit. DTO boundary: Domain entities don't leak across the Application boundary. Post-review refactor: per-entity `IEntityTypeConfiguration<T>` classes discovered via `ApplyConfigurationsFromAssembly(...)`; `OnModelCreating` reduced to one line; equivalence verified by empty migration. ⚠️ **Superseded by Phase 3:** `IUserRepository` (Domain-owned), `UserRole` with composite PK `(UserId, Role)`, and the `Role` enum were all replaced by ASP.NET Core Identity — roles are now seeded string constants in `AspNetRoles`. The global `JsonStringEnumConverter` went with them: the `Role` enum was its only consumer, so its removal was a consequence, not a regression. No enum currently crosses the wire *(see Standing checks for the re-registration trigger)*. Mapping strategy also changed: hand-rolled DTO mapping gave way to **Mapster** (`AddApplicationMapping`, `.Adapt<T>()`) — see `P3-14` for the global-static-config debt that came with it. |
| 3 | ✅ Done | **Auth complete end to end.** ASP.NET Core Identity (`AddIdentityCore`, no default scheme → clean 401s), JWT access tokens (15 min), opaque refresh tokens with single-use rotation, reuse/theft detection, and revocation. Use-case logic in Application behind ports; Identity/EF/JWT concretes confined to Infrastructure. `OperationResult` as the Application seam — no `IdentityResult` crosses the boundary; errors returned as stable **codes**, not messages. Register is transactional; role self-registration is whitelisted in Domain. Verified throughout against real HTTP, real DB state, and deliberately forced failures. Run as four sub-chats with a mentor PR gate on each. **Full detail: phase-3 archive.** |
| 4 | ✅ Done | **Hardening complete; ran as 4.1 (errors & input) + 4.2 (auth correctness & cleanup), both mentor-approved.** Unified RFC 7807 **`ProblemDetails`** contract: `ValidationProblemDetails` for payload-resolvable field failures (matches `[ApiController]`), plain `ProblemDetails` + an `errorDetails` code array for domain failures, empty-errors → `UnexpectedError`/500. Global `IExceptionHandler` (nested-try + `HasStarted` guard) — closed a **real leak** that returned the live `refreshToken` cookie in a diagnostic dump. Partial unique index on `NormalizedEmail` makes email uniqueness DB-enforced (protects every `Single*` Identity lookup). FluentValidation via reflection-based action filter, emitting through the contract, closing the null-required-fields gap. Family revocation now **idempotent** (guarded write; original reason/timestamp immutable — runtime-verified via Adminer). Reuse logged at the shared choke point; refresh cookie cleared on failed `/refresh`; all in-transaction early returns explicit. `TokenFamilyService` moved to **Application** (orchestration over ports). Mapster global static config replaced by injected **`IMapper`** → **unblocks parallel xUnit**. Orphans deleted; DI extensions return `IServiceCollection`; role sets immutable `FrozenSet`. **Locked decisions:** one failure literal per flow (`InvalidEmailOrPassword`; `InvalidRefreshToken`); error-placement discriminator (field-validation iff payload-resolvable, no I/O, no leak — else domain code); `500` means exactly "nothing caught or classified it"; `Infrastructure/Services` = adapters over concrete tech, `Application/Services` = orchestration over ports. **Verification:** all code audited against source (both sub-summaries had drifted — reconciled); `P3-05` runtime-verified; RefreshAsync-rollback and cookie-on-failure rest on source inspection (Phase 5 test targets). **Full detail: phase-4 archive.** |
| 5 | ✅ Done | **Mentor-PR-approved & synced (closed via Option A).** New `BookingApp.UnitTests` project (in `.slnx`; references Application + Domain + **API** — the validators live in the API layer). **27 tests:** `RegisterRequestValidatorTests` (14, all 7 rules both directions), `LoginRequestValidatorTests` (4), `AuthServiceTests` (9 — `RegisterAsync` ×5, `LoginAsync` ×4). Complete branch coverage of both in-scope `AuthService` methods incl. the `catch` path in **both** `HasActiveTransaction` directions; correctness confirmed by **mutation probe** (test shown red on a mutated condition, green on revert). **Durable decisions:** (1) **DTO shape convention — positional records → init-only properties** for `RegisterRequest`/`LoginRequest`/`AuthenticatedUserResult` (driver: test-data ergonomics; updated 2 sites in `UserIdentityService`). **Pattern precedent going forward.** Tradeoff: weakened positional records' compile-time non-null guarantee (CS8618/CS8601; `= null!` suppression) → future direction is **`required` init members** (see `P5-01`). (2) **`RegisterRequestValidator` name-regex rework (mentor-directed):** `.Matches` → `.Must()` combining `NameChars` allow-pattern AND a NOT-`GluedPeriod` rule — `"Jack.Jack"` now rejected, `"Jack JR."` accepted, `"J.Jack"` accepted by design. (3) **Program-wide test conventions** (see Engineering Principles → Testing). **Tooling note:** created via `dotnet new xunit3 --test-runner vstest`, which yields `xunit.v3.mtp-off` 3.2.2 (VSTest, not MTP) — not interchangeable with plain `xunit.v3`; `OutputType=Exe`, no `global.json`. **In-flight hotfix (`§Action items`):** `Microsoft.NET.Test.Sdk` shipped as **17.4.1** (typo for 17.14.1) — ran green, `--vulnerable` scan clean; bump pending. **Lineage:** a `ToProblemDetailsResult` HTTP-200 finding surfaced in the 5.1 audit is owned/fixed by **Phase 6 as `P6-FIX-200`** — not a Phase 5 item. **Full detail: phase-5 archive.** |
| 6 | ✅ Done | **Booking domain shipped end to end, mentor-approved; verified by zip audit + runtime probes (Scalar + Adminer). Ran n=3 sub-chats (6.1 read / 6.2 write+security / 6.3 pagination — pulled in from deferred).** First business domain on the Phase-4 auth substrate, **additive, no re-architecture.** **Domain:** `Apartment` (Id, OwnerId→User, Title, Description, Location, Price, Capacity) + `Booking` (Id, ApartmentId→Apartment, ClientId→User, CheckIn, CheckOut, CreatedAt); nav `Apartment.Bookings ↔ Booking.Apartment`, `Owner`/`Client` reference-only (Identity `User` kept lean); migration `AddApartmentsAndBookings`; **`date` columns** for check-in/out, `timestamptz` for `CreatedAt`, composite index `(ApartmentId, CheckIn, CheckOut)`; dev-gated idempotent seeder (Host + 10 apartments + Client + 26 bookings). **Endpoints:** `GET /api/apartments` (public, available-apartments over the half-open range, paginated, optional both-or-neither `availableFrom`/`availableTo`); `POST /api/bookings` (`[Authorize(Roles=Client)]`, client id from JWT `NameIdentifier` never body, apartment-exists/not-past/no-overlap); `GET /api/bookings/{id}` (`[Authorize]`, ownership-checked, **404 for both missing and not-owned** to avoid leaking existence); `GET /api/bookings` (`[Authorize]`, caller-scoped, paginated). **Pagination (6.3):** offset; `PaginatedRequest`(abstract)/`PageQueryParams`(50-cap in accessor)/`PagedResult<T>`/`PaginatedResponse<T>`(private ctor + `Create` factory); repo `CountAsync` on the once-built filtered `IQueryable`, then `OrderBy(Id).Skip().Take()`; default (not floor) page size 5, `<=0`→400, clamp-at-50 in construction not validator. **Locked decisions:** date-only UTC half-open model with **`date` columns as the real guarantee** (`.Date` at the seam is belt-and-suspenders); injected **`TimeProvider`** for past-check-in rejection (booking code only — auth retrofit still deferred); reuse `AuthService`'s `BeginTransaction→Commit`+`SafeRollback` (atomicity, **not** race protection); `BookingErrorCodes` through the existing `OperationResult`→RFC 7807 surface (ApartmentNotFound 404, ApartmentNotAvailable 409, InvalidCheckInDate 400, BookingNotFound 404). **Durable learnings** → Engineering Principles (construction discipline; validators reject / services transform) + framework gotchas (`[JsonIgnore]` inert on `[FromQuery]` complex types; `out` illegal on `async`; `RuleFor(x=>x).Must` needs `.WithName()`, cross-field `.When()` guards both operands). **Resolved:** `P6-FIX-200`, `P6-02`. **Full detail: phase-6 archive.** |
| 7 | Not scoped | Next phase — scope not yet decided (candidates in the Phases entry). |

---

## Sync status

**Marker convention:** ✅ = complete **and** mentor-approved/synced. ⏳ = code complete but awaiting
review (not yet synced). 🔨 = phase in progress.

**Current state.** All phases through **Phase 6 are ✅ closed and synced** (mentor-approved). **No phase
is currently in flight** — Phase 7 scope is not yet decided. Phase 5 and Phase 6 were both closed
retroactively while the next was already underway; both landed cleanly (their diffs didn't overlap), and
that non-linear window is now fully resolved.

**Watch-item resolved.** The Phase 5 sync flagged that Phase 6 might introduce the `TimeProvider` clock
seam and warned about double-counting against Phase 5's original plan. Phase 6 *did* introduce it (for
booking code); Phase 5 never touched it, so there is **no double-count** — `P3-13` now reflects "clock
seam exists for booking; auth retrofit still deferred."

**Prior correction (retained for the record):** the Phase 5 interim note wrongly claimed "no production
code"; Phase 5's DTO refactor did touch auth DTOs + `UserIdentityService`. It doesn't overlap booking, so
the closes stayed clean — the "purely additive" premise is retired.

**Still in-flight (small):** the `Microsoft.NET.Test.Sdk` 17.4.1 → 17.14.1 hotfix. Not blocking.

**The launch extract is now stale** — it holds the *spent* Phase 6 boarding pass. It will be regenerated
once Phase 7 scope is chosen; until then it is not a live "next phase" document.

**Go-forward rule (unchanged): don't stack non-linearity.** One phase in flight at a time. When Phase 7
is scoped and built, sync it here (master first, then regenerate the extract for the phase after).

---

## Auth Architecture — as built *(the durable shape)*

**Layering.** Domain → nothing. Application → Domain only. Infrastructure → Application + Domain.
API → Application + Infrastructure. Verified by project references, not by assertion.

**Ports live in Application; implementations in Infrastructure.** `IAuthService`,
`IUserIdentityService`, `IRoleIdentityService`, `IAccessTokenService`, `IRefreshTokenService`,
`IRefreshTokenRepository`, `ITokenFamilyRepository`, `IUnitOfWork`. **Ports are shaped by what the
caller needs**, never mirrored from the wrapped library's API surface.

**Composition.** `Program.cs` chooses *which modules*, not all registration: Application exposes
`AddApplicationServices` / `AddApplicationMapping` / `AddTokenFamilyOptions`; Infrastructure exposes
`AddInfrastructureServices` / `AddInfrastructurePersistence`; API exposes `AddJwtAuthentication`.
Options are bound + `.Validate(...)` + `.ValidateOnStart()`, so startup fails loudly on bad config
and downstream code trusts config by construction. *(Phase 4: `AddApplicationMapping` now registers an
injected `IMapper` instance — the global static `TypeAdapterConfig` mutation is gone, removing hidden
global state and unblocking parallel test runs; DI extension methods return `IServiceCollection`.)*

**Token model.** Access token = JWT, 15 minutes, claims read fresh at issuance, validated in-process
by `JwtBearer` with algorithms pinned to HS256 and **`ClockSkew` set to 2 minutes** (down from the
5-minute default — skew exists because a *validating* server's clock may differ from the *issuing*
server's, so it applies to access tokens only; refresh expiry is checked against our own clock and a
timestamp we wrote). Refresh token = **opaque 32-byte random string**, SHA-256 hashed at rest, delivered
in an `HttpOnly` + `Secure` + `SameSite=Lax` cookie scoped to `/api/auth`. Rotation is single-use;
a replayed revoked token is treated as theft and revokes the whole family. Sliding 3-day token TTL
under a 14-day absolute ceiling stored **once** on the family, so rotation code has no reason to touch it.

**Why opaque, not JWT, for refresh:** revocation requires an external check (a JWT's validity is
self-contained and can't encode "…but not this one"), and multi-day claims go stale. Together, a JWT
refresh token would still need a full DB read on every refresh — leaving the JWT structure nothing to do.

**Why SHA-256, not bcrypt:** bcrypt embeds a random per-call salt, so the same input hashes differently
every time. A refresh token is found **by its hash** (`WHERE token_hash = @incoming`), so the hash must
exist before the lookup — circular under bcrypt. Structurally incompatible, not merely slower.

**Persistence.** `IUnitOfWork` is a transaction scope (`Begin` / `Commit` / `Rollback`); `CommitAsync`
saves internally. Repositories track, never save. Concurrency-critical revocation uses
`UPDATE … WHERE id = @id AND revoked_at IS NULL` and branches on rows-affected — Postgres row-level
write locking gives free exclusivity at every isolation level, and folding the business condition into
the same `WHERE` covers the stale-intent case that an `xmin` concurrency token would silently miss.
The same guarded-write pattern makes **family revocation idempotent** (a re-revoke is a no-op, leaving
the original reason/timestamp immutable).

**Error & validation contract (Phase 4).** One unified RFC 7807 shape: `ValidationProblemDetails` for
input/field failures (matching `[ApiController]`'s auto-shape), plain `ProblemDetails` + an `errorDetails`
code array for domain failures, empty-errors collapsing to `UnexpectedError`/500. A global
`IExceptionHandler` (nested-try + `HasStarted` guard) is the safety net — **`500` now means exactly
"nothing caught or classified it"**; every domain failure is a deliberately-returned `Failure(...)`.
FluentValidation runs in a reflection-based action filter *before* the mapping, emitting through the same
contract. **Enumeration invariant:** each flow emits exactly one failure literal — login
`InvalidEmailOrPassword`, refresh/logout `InvalidRefreshToken` — load-bearing, because any future lockout
or status refinement that adds a second literal reopens the leak (the reason `P3-06` lockout was deferred).
Email uniqueness is a partial unique index on `NormalizedEmail`, protecting every `Single*` Identity lookup.
`TokenFamilyService` lives in **Application** (orchestration over ports, zero concrete framework dependency).

---

## Booking Domain — as built *(Phase 6; the durable shape)*

**Additive on the auth substrate** — no re-architecture. Same layering, same `OperationResult` seam, same
RFC 7807 error surface, same validation filter, same `IMapper` DTO boundary.

**Entities.** `Apartment` (Id, OwnerId → `User`, Title, Description, Location, Price, Capacity) and
`Booking` (Id, ApartmentId → `Apartment`, ClientId → `User`, CheckIn, CheckOut, CreatedAt). Navs:
`Apartment.Bookings ↔ Booking.Apartment`; `Apartment.Owner` / `Booking.Client` are **reference-only**
(the Identity `User` stays lean). Migration `AddApartmentsAndBookings`. **`CheckIn`/`CheckOut` are `date`
columns** (the real date-only guarantee), `CreatedAt` is `timestamptz`; composite index
`(ApartmentId, CheckIn, CheckOut)`.

**Date/availability model.** Half-open `[check-in, check-out)`, **back-to-back allowed**; compared on the
date part only, all UTC. The `date` columns enforce it; `.Date` at the seam is belt-and-suspenders.
Availability is derived **purely from bookings** — an apartment is available for `[from, to)` iff no
booking overlaps (`existing.CheckIn < to && from < existing.CheckOut`). **No status lifecycle.**

**Endpoints.** `GET /api/apartments` (**public**, available + paginated, optional both-or-neither date
filter); `POST /api/bookings` (**`[Authorize(Roles = Client)]`**, client id from the JWT `NameIdentifier`
claim — never the body — with apartment-exists / not-past / no-overlap checks); `GET /api/bookings`
(**`[Authorize]`**, caller-scoped, paginated); `GET /api/bookings/{id}` (**`[Authorize]`**,
ownership-checked, **404 for both missing and not-owned** to avoid leaking existence).

**Pagination.** Offset. `PaginatedRequest` (abstract) / `PageQueryParams` (50-cap in the accessor) /
`PagedResult<T>` / `PaginatedResponse<T>` (private ctor + `Create` factory). Repositories `CountAsync` the
once-built filtered `IQueryable`, then `OrderBy(Id).Skip().Take()`. Default (not floor) page size 5;
`<= 0` → 400; high end clamps at 50; **clamp lives in construction, not the validator**.

**Time & persistence.** Past-check-in rejection uses an **injected `TimeProvider`** (booking code only;
the auth-token retrofit stays deferred — see `P3-13`). Create reuses `AuthService`'s
`BeginTransaction → Commit` + `SafeRollback` — that transaction is **atomicity, not race protection**
(the double-booking window `P6-01` is deliberately open). `BookingErrorCodes`: ApartmentNotFound 404,
ApartmentNotAvailable 409, InvalidCheckInDate 400, BookingNotFound 404.

---

## Debt Ledger *(the single LIVE ledger — add here, don't start another)*

Cross-references in brackets point to the phase-3 archive's original numbering. New debt is numbered by the
phase that surfaced it (`P4-xx`, `P5-xx`, `P6-xx`, …). **IDs are permanent and never renumbered.**

### Resolved in Phase 4 ✅ *(detail in the phase-4 archive; kept here for traceability)*
| # | Resolution |
|---|---|
| `P3-01` | Partial unique index on `NormalizedEmail` — email uniqueness is now DB-enforced. |
| `P3-02` | Global `IExceptionHandler` (nested-try + `HasStarted` guard); closed the live-`refreshToken`-cookie leak. |
| `P3-03` | Unified RFC 7807 `ProblemDetails` / `ValidationProblemDetails` contract. |
| `P3-04` | `ILogger` replaced `Console.WriteLine`; full errors logged server-side, filtered codes to client. |
| `P3-05` | Family revocation idempotent (guarded write; original reason/timestamp immutable) — **runtime-verified**. |
| `P3-08` | Refresh cookie cleared on failed `/refresh` (stops dead-token replay). |
| `P3-09` | All in-transaction early returns explicit (commit or rollback); no reliance on scope disposal. |
| `P3-10` | `TokenFamilyService` moved to Application (orchestration over ports). |
| `P3-11` | Orphaned types deleted (`LoginResult`, `LogoutRequest`, off-interface `SaveChangesAsync`). |
| `P3-12` | Housekeeping done (DI returns `IServiceCollection`; `FrozenSet` role sets; dead `User→RegisterResponse` map + seeder typo removed; `ErrorDuringTokenRefresh` deleted so refresh collapses to one literal). *`CreatedAtAction` remains — see `P4-04`.* |
| `P3-14` | Mapster global static config → injected `IMapper`; **parallel xUnit unblocked**. |

### Deferred seams & design *(tracked; not owned by a running phase)*
| # | Item |
|---|---|
| `P3-13` | `TimeProvider` seam. **Phase 6 introduced an injected `TimeProvider`** for booking code (past-check-in rejection) — the clock seam now exists in the app. The **auth token services still read `DateTime.UtcNow` directly**; that retrofit remains deferred. *[Q19]* |
| `P3-15` | `OperationResult<TValue>` shape — `Value` public/ungated on the failure path; `Success(null!)` folds to a contentless failure. **The current shape is mentor-approved; the design call is now Yevhenii's, not the mentor's.** **Resurfaced concretely as `P6-06`** — the 6.3 implicit `Success` operator makes the null-fold reachable via `return value;`. Underlying question stands: does the channel absorb contract violations, or only domain outcomes? *[Q18]* |

### Deferred / future hardening *(not this phase — a later hardening pass)*
| # | Item |
|---|---|
| `P3-06` | Auth-endpoint rate limiting — **deferred, not descoped.** Correct API is partitioned `AddPolicy`; `429` is middleware-level (outside the `ProblemDetails` contract); account **lockout stays out** because a second failure literal breaks the enumeration invariant. *[Q16]* |
| `P4-01` | Global all-endpoint rate limiting (distinct from `P3-06`'s auth focus). |
| `P4-02` | Refresh-token read/write **race**: a stale-read window exists under READ COMMITTED but is **not** exploitable into double-issuance (the guarded write resolves it). Worth a two-terminal confirmation. |
| `P4-03` | `RequireUniqueEmail = false` leaves the `DuplicateEmail`/`InvalidEmail` map rows **dead** — remove the rows or flip the flag. |
| `P4-04` | Smaller batch: `WWW-Authenticate` on 401s (RFC 9110); `503`-vs-`500` for DB-unreachable; validator `Type` caching; age boundary `<=`/`<`; `CreatedAtAction` for register (needs a GET endpoint; carried from `P3-12`); login timing side-channel. |
| `P3-07` | Request body size limit (Kestrel / `[RequestSizeLimit]`) — **status not reported in the Phase 4 summary;** recorded as deferred per the triage note that Kestrel's default backstops it. *Confirm whether an explicit cap shipped.* |

### Introduced in Phase 5 *(test-layer debt, a standing caveat, deferred tests)*
| # | Item |
|---|---|
| `P5-01` | **Nullable-annotation debt from init-only DTOs.** Init-only string members default non-nullable-to-null (CS8618/CS8601; `= null!` suppression), weakening the compile-time guarantee positional records gave. **Fix: `required` init members** (initializer syntax *and* compile-time enforcement). Low priority (validator catches nulls at runtime; no live bug). **New DTOs — including Phase 6's — should use `required` init members from the start.** |
| `P5-02` | **Regex allocation in `RegisterRequestValidator`.** The `.Must()` rework builds 2 `new Regex(...)` per name field per call (the prior `.Matches()` compiled once). **Fix: `[GeneratedRegex]` / `static readonly Regex`.** Low priority (immaterial at current scale). |
| `P5-03` | **Test-class granularity.** `AuthServiceTests` covers two SUT methods in one class; per-method split deferred. Grows with each method added. |
| `P5-04` | **`"J.Jack"` decision unpinned.** Single-letter-initial-glued-to-name is accepted by design but no test pins it. **Fix: `[InlineData("J.Jack")]`** in the FirstName/LastName invalid theories. |
| `P5-05` | 🟡 **Standing caveat — `MockBehavior.Strict` removed (mentor-directed).** In `AuthServiceTests`, `_tokenFamilyService` / `_refreshTokenRevoker` / `_logger` are unconfigured **Loose** mocks → an unexpected call silently returns default instead of throwing. **Any future test touching them (notably the deferred `RefreshAsync`/`LogoutAsync` tests) must add explicit `Verify(..., Times.Never)`** or the "must-not-be-called" guarantee is absent. |
| `P5-06` | 🟡 **Deferred tests → future testing pass.** **Highest-value uncovered:** the **enumeration invariant** (both login-failure causes collapse to one `InvalidEmailOrPassword`; in `UserIdentityService`; needs `Mock<UserManager<User>>`) — **keep prominent.** Also: pure-service tests (`RefreshTokenService` hash round-trip/malformed input; `JsonWebTokenService` claims/alg/issuer/audience, *not* `exp`; `ToProblemDetailsResult` body-shape) and `RefreshAsync`/`LogoutAsync` unit tests (some need `P3-13`; mind `P5-05`). |
| `P5-07` | **Integration test suite (whole) → future integration phase.** Additive (`BookingApp.IntegrationTests` + `public partial class Program {}`): contract HTTP status/body, `Set-Cookie` on failed refresh, rollback DB-state, guarded-write idempotency, at-most-one-live-token index, concurrency race. |

### Resolved in Phase 6 ✅ *(detail in the phase-6 archive)*
| # | Resolution |
|---|---|
| `P6-FIX-200` | `ToProblemDetailsResult` now sets `ObjectResult.StatusCode` — domain failures (and wrong-password) returned HTTP **200** with a non-200 body before; fixed. *(Surfaced in the Phase 5 5.1 audit; owned + fixed here.)* |
| `P6-02` | One-sided `.Date` normalization fixed (6.3 validator) plus a residual **write-path** drift found in audit, fixed, and **runtime-verified** (a stray-time payload now returns midnight in both the response echo and the DB). |

### Introduced in Phase 6 *(stands / deferred)*
| # | Item |
|---|---|
| `P6-01` | 🟡 **Double-booking TOCTOU — stands (deliberate, mentor-approved).** Check-then-insert with **no lock and no exclusion constraint**; two clients racing the same apartment + overlapping dates can both succeed. Commented in code at `BookingService.CreateBookingAsync`. Consistent fix: a Postgres `EXCLUDE USING gist (apartment_id WITH =, during WITH &&)` constraint (`btree_gist`; EF doesn't model EXCLUDE → raw-SQL migration); alternatives `SELECT … FOR UPDATE` or optimistic concurrency. **Also the core unsolved problem in the parallel assessment task.** |
| `P6-04` | Repo DI registered across two extension methods (cosmetic); `CancellationToken` half resolved. |
| `P6-05` | `Booking → Apartment` `OnDelete: Cascade` is unreachable today; revisit with soft-delete when host-delete lands. |
| `P6-06` | 🟡 **`OperationResult<TValue>.Success(null)` folds to a codeless failure → HTTP 500 + `["UnexpectedError"]`.** The 6.3 implicit `Success` operator makes it reachable **invisibly** via `return value;`. Candidate fix: **throw**. This is **`P3-15` resurfacing — now Yevhenii's call, not the mentor's.** Not a live bug today. |
| `P6-07` | Adopt the implicit `Success` operator **solution-wide** (scoped to `BookingService` in 6.3); own reviewed pass. Ceiling: non-generic `OperationResult.Success()` can never use it. |
| `P6-08` | Bookings reads use plain `[Authorize]` rather than role-gating; both are caller-scoped so safe. **Decided: leave as-is for now.** |
| `P6-09` | Sort key for paginated lists (`Id` vs `CheckIn`/`CreatedAt`) — sorting is non-negotiable, the key is a UX call. |
| `P6-TEST-OVERLAP` | Unit-test the pure overlap function — the obvious first automated test (Phase 6 shipped test-free). |

### Future features / specified but not built
| # | Item |
|---|---|
| `P3-16` | **Constraints checklist** — the decisions-already-made list consulted before each change. Scheduled to start in Phase 4; **confirm it's live** and carry it forward. |
| `P3-17` | `HandleReuseAsync` → account-wide invalidation via `UpdateSecurityStampAsync`, at the `RevokeTokenFamily` funnel where `reason == TheftDetected`. **Now unblocked by `P3-05`.** |
| `P3-18` | `/logout-all`, gated by `[Authorize]` — a valid access token *is* cryptographic proof of identity, categorically different from the client-supplied identifier rejected on DoS grounds. Needs `RevokeAllLiveForUser`. |
| `P3-19` | Redis access-token blacklist keyed by **`TokenFamilyId`**, closing the ≤15-min window where a stolen access token outlives its revoked family. |
| `P3-20` | Revoked-row retention/cleanup; idempotency keys for `/refresh`; device/IP risk signals. Out of current scope: DDoS resilience (infrastructure-layer concern — reverse proxy / CDN / WAF). |

### Standing checks *(re-run, never "closed")*
- **Enumeration safety.** Locked in Phase 4: login emits one literal (`InvalidEmailOrPassword`), refresh/logout one (`InvalidRefreshToken`). The paths still *forward* errors rather than hardcoding one, so the moment a second distinct auth-failure literal exists (account lockout being the obvious one — `P3-06`), the leak reopens silently, and the Identity error mapper will not catch it because it filters Identity codes, not ours. This is why `P3-06` lockout was deferred.
- **ID exposure.** `int` PKs are fine *while* every client-exposed ID is independently authorized. Re-run "is this ID client-exposed, and is access independently authorized?" per new entity — carry the check, not the answer. **Handled in Phase 6:** `GET /bookings/{id}` verifies `booking.ClientId == callerId` (JWT `NameIdentifier`) and returns **404 for both missing and not-owned** so existence never leaks; `GET /bookings` is caller-scoped by construction. Re-run on the next client-exposed entity.
- **Orphan check.** Before recording anything as "deleted," grep for references. "Deleted" has meant "removed from the interface" before.
- **Enum serialization.** `JsonStringEnumConverter` is **not** registered — still correct: error surfaces use **string** codes, and **Phase 6 added no status enum** (no booking lifecycle), so nothing new crosses the wire. **Trigger remains: the first enum that appears in a response DTO** — without the converter, `System.Text.Json` serializes it as an integer, a brittle contract that silently changes meaning if members are reordered.

---

## Engineering Principles Established

**Architecture**
- An interface is owned by the layer that **depends** on it. If a use-case seems to require an Infrastructure type, invert with a port — don't relocate the use-case.
- A concrete dependency *within* a layer is fine; a use-case depending on a concrete *across* a boundary is the violation.
- Ports are shaped by the caller's available data, not by the wrapped library's surface.
- The composition root chooses *which modules*, not all registration.
- A service wrapper earns its place when there's a rule to enforce or more than one caller — not merely because it touches more than one entity.
- **Translation vs. decision:** converting a raw result into a meaningful value is translation (belongs low); acting on its meaning is a decision (belongs in the orchestrator).
- **Adapters vs. orchestration decides the sub-layer.** `Infrastructure/Services` wraps concrete external technology (adapters); `Application/Services` composes Application ports with zero concrete framework dependency (orchestration). A service with no infrastructure dependency belongs in Application — the rule that relocated `TokenFamilyService` in Phase 4.
- **DTOs are shaped for object-initializer construction** (init-only properties; Phase 5 precedent). **Prefer `required` init members** so the initializer ergonomics don't cost the compile-time non-null guarantee — new DTOs should start there rather than repeating the `= null!` suppression (`P5-01`).

**Correctness**
- **Enforced beats currently-true.** Prefer invariants the data layer guarantees over conventions the application must remember. Store a value once rather than copying it, so nothing has a reason to recompute it wrongly.
- Uniqueness under concurrency comes from **index-level constraint serialization**, not transaction isolation.
- `CHECK` constraints cannot reference other rows; **partial unique indexes** are the tool for conditional uniqueness.
- **Never let untrusted input decide the rules by which it's checked** (pin JWT algorithms; default-deny error mapping).
- **Redundant checks are worse than absent ones when the redundant one is staler.**
- **Asymmetric failure cost decides centralization:** forgetting a security step should be impossible; forgetting a return-value branch fails loudly and cheaply.
- **Merge on shared *cause*, not shared *shape*.** Two things identical today aren't duplication unless the reason keeps them identical.
- **Collapse drift-prone duplicates; make the safe construction the only one.** When two same-typed values describe one concept and can drift apart, reduce them to a single already-correct value and route all construction through the one safe path (clamp-in-accessor + `required` + private-ctor/factory). Phase 6's pagination made the drift bugs **uncompilable** rather than caught-by-review.

**Judgment**
- **Dominant cost first.** Before optimizing a constant factor, check whether a much larger irreducible cost already dominates the path.
- **When the cost curve is flat, stop optimizing.** No two-sided tradeoff means no minimal-sufficient value to find — clear the danger zone, pick a conventional number, move on.
- **YAGNI with a trigger condition:** name the condition that would justify the abstraction, then test whether it holds. An interface with zero implementations fails this as surely as a premature one.
- **Verify with tooling, not explanation.** Real HTTP, real DB state, deliberately forced failures. Reasoned-but-unverified claims get flagged as such.

**Error handling & API contract**
- **Error-placement discriminator:** a check is field-validation (`ValidationProblemDetails`) iff it is resolvable from the payload alone, needs no I/O, and naming the field leaks nothing; otherwise it is a domain code (`ProblemDetails`). Classification follows *where the check runs*, not what it feels like.
- **`500` means exactly "nothing caught or classified it."** Every expected failure is a deliberately-returned caught `Failure(...)`; an uncaught 500 is a bug, never a control-flow branch.
- **One failure literal per auth flow.** Enumeration safety is an invariant, not a default: the moment a second distinct auth-failure literal exists, the leak reopens silently. Preserve the single literal (see Standing checks).
- **Validators reject; services transform.** The validation filter reads only `.IsValid` and never re-reads a mutated DTO — normalization/transformation belongs in the service, not the validator (Phase 6).

**Testing** *(program-wide conventions established in Phase 5 — apply to all future test phases)*
- **Mutation probes.** A test that can't be shown to fail on a real defect is ceremony: break the SUT, confirm red, revert. Honest, not ceremonial.
- **Exact argument matching** for pass-through values (catches field swaps); `It.IsAny<T>()` only for values the SUT itself constructs or transforms.
- **Derived test data** over independently-hardcoded values that merely happen to match.
- **`readonly` fields, not `=> new X{}` properties,** for reference-type test data matched by reference.
- **Constructor-based happy-path mock setup** with per-test overrides; happy-path bodies contain zero `Setup` calls (safe — fresh test-class instance per test).
- **`out`-param mocking** via `out It.Ref<string>.IsAny` plus an assigning delegate.
- **Relative dates** for time-dependent validators, with a buffer only where the inequality's strictness makes the exact boundary unreachable.
- **Loose mocks demand explicit negative assertions.** Where `MockBehavior.Strict` is off, a "must not be called" guarantee exists only if you write `Verify(..., Times.Never)` (`P5-05`).

---

## Process — what worked, and how to run a phase

**Multi-chat structure.** A phase that needs more than one sitting gets an **orchestrator chat** holding
a phase master doc (the archive) plus a **launch extract** per sub-chat. The extract's selection rule is
*"does the next sub-chat need this to build?"* — narrow, not merely short. Master is updated first (it's
the quality gate); the extract is derived from it, never the reverse.

**PR gate.** A sub-chat summary is provisional until its PR is reviewed and merged. Syncing before that
enshrines reasoning the review may overturn — which happened once, costing a rework.

**Audit against the archive, not the summary.** When source is available, read it before accepting a
summary. Every sync where this was done surfaced drift the prose alone would have hidden. *(Phase 4:
both sub-chat summaries had again drifted from their own code — caught and reconciled against source.)*

**External review catches what internal review can't.** Extended dialogue converges on *internally
consistent* designs, and internal consistency is not correctness. The mentor caught three framing errors
in Phase 3 — a conceptual naming mismatch, an SRP violation, and a DTO naming confusion — **two of which
Claude had actively endorsed.** Practical rule: get a mentor read on conceptual and naming decisions
*before* entities and migrations are built on them.

---

## Failure Modes Being Actively Tracked

**"Right conclusion, wrong stated mechanism"** — when a correct answer arrives quickly, the *first*
stated reason is usually not the load-bearing one. Nine instances in Phase 3's final sub-chat alone.
Highest-stakes case: a wrong claim about token lifetimes reached the mentor as an argument for
*removing* a security control.

**"Decided-then-drifted"** — twelve instances, dominant in long sessions. Each was decided correctly,
then not carried into code written later, almost always during a refactor. **This is the cost of a long
design session: the decision log outgrows working memory.** Fix is `P3-16`, the constraints checklist.

**Local pattern-transplant** — copying a pattern from elsewhere in the same codebase without checking
*why* it was that way there. More dangerous than cross-technology transplant, because in-codebase
precedent feels like evidence.

**Reaching for machinery that sounds responsible** — Guid PKs, retry loops, extra columns. Each
dissolved on *"what specific threat or failure does this address?"*

**Claude's own calibration:** "this reasoning is correct" is a claim about internal consistency, not a
proof. Across Phase 3, Claude endorsed a wrong architectural placement, argued for a wrong entity name,
made a wrong empirical prediction, and locked decisions while its own questions were open. Treat its
audits as a filter, not a gate — the PR review is the gate.

---

## Current Status

**Currently on:** nothing in flight. **Phase 6 is ✅ closed and synced** (booking domain shipped,
mentor-approved). **Phase 7 scope is not yet decided.**

**Where to pick up:** decide Phase 7 scope from the candidate list (see the Phases §Phase 7 entry), then
ask for a launch extract to be generated for it. The current launch-extract artifact is the **spent
Phase 6 boarding pass** and should be regenerated once Phase 7 is chosen. Carry the `P3-16` constraints
checklist forward; use `required` init members for any new DTOs (`P5-01`).

**Decisions now on Yevhenii (no longer the mentor's):**
- **`P6-06` / `P3-15`** — `OperationResult<TValue>.Success(null)` folds to a codeless 500; the implicit `Success` operator makes it reachable via `return value;`. Candidate fix: throw. This is the long-standing Result-shape question, now yours to settle.
- **`P6-09`** — the sort key for paginated lists (`Id` vs `CheckIn`/`CreatedAt`); a UX call.
- **`P6-08`** — already decided: bookings reads stay on plain `[Authorize]` (caller-scoped, safe).

**Open questions / carried items:**
- **`P6-07`** — whether to adopt the implicit `Success` operator solution-wide (its own reviewed pass).
- **`P6-TEST-OVERLAP`** — the pure overlap function is the obvious first automated test whenever testing resumes.
- **`§Sync status` in-flight** — `Microsoft.NET.Test.Sdk` 17.4.1 → 17.14.1 hotfix pending.
- **`P5-06`** — the enumeration-invariant test remains the highest-value uncovered assertion for the future testing pass.
- Carried: `P3-07` request-size-limit status; `P3-16` checklist confirmation; `P4-03` `RequireUniqueEmail` dead-rows; `P6-04`/`P6-05` (cosmetic DI; `OnDelete` revisit with soft-delete).

**Notes from mentor:**
- Use PostgreSQL (or SQL Server if already worked with Postgres — sticking with PostgreSQL here)
- Clean Architecture required
- Unit tests: xUnit, Shouldly, Moq
- Validation: FluentValidation
- Roles: Client, Host (Admin planned later)
- **Get a mentor read on conceptual/naming decisions before building entities and migrations on them**

---

## Orchestration & Update Protocol *(orchestrator-chat only — NOT part of phase tracking; never pasted into a working chat)*

This section is maintenance machinery for the chat that owns these two artifacts. It is not project
content and is never copied into a phase's launch extract.

**Two artifacts live in this orchestrator chat:**
1. **Bookingapp program master** (this document) — the durable, program-level record of where the whole
   project stands. Updated *first* when a phase completes; it is the quality gate.
2. **Current phase launch extract** — a boarding pass for the *next* phase, regenerated to be pasted as
   the first message of that phase's dedicated chat.

**Trigger + steps.** The user states their intent on arrival, so match the action to the stated goal:
- *"Here is the completed **Phase N** summary"* →
  1. Analyze the summary. When real source is available, **audit against it, not the prose** (see §Process).
  2. Update **Bookingapp program master**: flip Phase N to done in the Progress Log, fold in durable
     decisions/architecture, migrate any new debt into the Debt Ledger with fresh ledger IDs, and update
     Current Status to point at Phase N+1.
  3. Regenerate **Current phase launch extract** so it targets **Phase N+1**, using the spec below.
- *"Here is a new/additional future phase"* → record it in Phases + Progress Log; don't invent scope the
  user hasn't defined.

**More phases will exist over time** (Phase 6, 7, …), each a logical continuation of the prior one. Their
scope may be unknown until the user defines it — don't pre-invent them.

**Ledger IDs are permanent.** New debt found in Phase N is numbered `PN-01`, `PN-02`, … and keeps that ID
for life so it stays traceable back to here. Don't renumber closed or migrated items.

### Launch-extract generation spec *(the reusable mechanism — verbatim; do not re-request it from the user)*

```
Generate the launch extract for Phase N.
WHAT IT IS
A self-contained handoff pasted as the first message into a fresh Phase N chat
that has no other memory of this project. It is a boarding pass for one phase's
work — not a summary of this master document.
SELECTION RULE
Include something only if the Phase N chat needs it to BUILD. The value is
narrowness, not brevity: dense is fine, off-scope is not. Leave out other
phases' debt, the full principles list, the process section, and any history
Phase N doesn't stand on.
REQUIRED SECTIONS, IN ORDER
1. Banner — "You are Claude starting Phase N. Treat this document as the authoritative source for where the project stands. You may also have memory of this Project or be able to search earlier chats in it — use those for background only. Where anything conflicts with this document, this document wins; say so explicitly rather than reconciling it silently." Flag here if the phase looks likely to need splitting.
2. Teaching mode — copied VERBATIM from this document's Context section. Do not
   paraphrase or trim it.
3. Project one-liner — stack, architecture, what's already working.
4. Where the previous phase landed — only the parts Phase N builds on, plus any
   live issues it inherits (with their ledger IDs) and which are its to fix vs.
   just be aware of.
5. Current shape — signature level: interfaces, DTO shapes, file layout, layer
   boundaries. Then instruct the reader to ask me for the real source or the
   solution zip before designing against it.
6. Phase N frontier — decisions that shape everything else FIRST, build steps
   after, in dependency order. Include the debt-ledger items Phase N owns, the
   traps worth pre-loading, and any standing checks that bear on this work.
7. Out of scope — what Phase N must NOT build, so it doesn't drift into the
   next phase.
CONSTRAINTS
- Never fabricate source code. You don't have the repo; signatures only.
- Preserve ledger IDs (P3-01 etc.) so items stay traceable back to here.
- Ordering carries meaning: if one item blocks another, say so explicitly.
- Don't include regeneration instructions in the extract — this prompt is the
  reusable mechanism.
- If anything you'd need is missing or ambiguous, ask before generating rather
  than filling the gap with a plausible guess.
OUTPUT
A single markdown artifact I can paste whole into the Phase N chat.
```
