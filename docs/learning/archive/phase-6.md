# BookingApp — Phase 6 Master Document (Booking core)

> **This chat is the Phase 6 master/orchestrator.** Concrete work happens in dedicated sub-chats (6.1, 6.2, …) launched from a narrow extract. This document is the single source of truth for Phase 6 progress; the sub-chat extract is a *derived, filtered projection* of it — always update this master first, then regenerate the extract.

---

## 0. How this document is used (update protocol)

When you return from a sub-chat with a **summary + updated `.zip`**:

1. **Audit the zip against source, not the summary prose.** Summary text and actual code diverge regularly — read the files.
2. **Update this master first:** progress tracker (§9), debt ledger (§8), and any settled-decision or substrate-fact that changed.
3. **Then regenerate** the "Current Phase 6 sub-chat extract" for the *next* sub-chat, derived from this updated master.
4. **Master before extract, never the reverse.** Regenerating the extract first introduces drift.

At the end of the last sub-chat: final master update, then produce a Phase 6 summary to hand up to the main program master.

---

## STATUS: ✅ PHASE 6 COMPLETE (all sub-chats done, audited, mentor-approved)

- **6.1 (read side)** ✅ · **6.2 (write side + security)** ✅ · **6.3 (pagination)** ✅ — all built, verified against a running app (Scalar + Adminer), mentor-reviewed, review comments resolved.
- **Scope note:** pagination was *originally deferred* (see §2) and was pulled back in as **6.3**, a deliberate scope addition on both `GET /api/apartments` and `GET /api/bookings`. So Phase 6 ran **n = 3** sub-chats, not the 2 originally predicted.
- **Post-approval fix (this master chat):** a residual `.Date` write-path drift (sibling of P6-02) was found in audit, fixed, and **runtime-verified** — stray-time input now returns midnight in both the response echo and the DB (`date` column). See ledger P6-02.
- **Remaining open items are all consciously deferred to Phase 7+** — none block Phase 6 close-out. See §8.

---

## 1. Phase 6 one-liner & scope

**Phase 6 adds the first business domain: booking.** Greenfield feature built additively on the merged **end-of-Phase-4** codebase (auth: register/login/JWT + rotating refresh tokens/theft detection, behind an RFC 7807 error surface, on Identity + EF Core + Postgres). Phase 5 added only an unreviewed `BookingApp.UnitTests` project (zero production code) — it does not affect what Phase 6 builds on, and its test conventions are **not** authoritative.

**Verified against the zip:** production code is end-of-Phase-4; greenfield confirmed (no `Apartment`/`Booking` references anywhere); all reuse seams present as described.

Deliverables: `Apartment` + `Booking` domain, EF config/migration/seeder, an **available-apartments query** (public), **create booking**, **my-bookings list**, and **`GET /bookings/{id}` with an ownership check**.

---

## 2. Out of scope for Phase 6 (do NOT build)

- **Concurrency / locking (`P6-01`)** — no exclusion constraint, no `SELECT … FOR UPDATE`, no optimistic-concurrency token. Deliberately deferred; the create path is intentionally racy.
- **Tests** — Phase 6 ships without tests (mentor-approved). The pure overlap function is the obvious *future* test target; note it, don't build it.
- **Host-side functionality** — no apartment create/edit/manage endpoints; apartments come from the seeder only.
- **Booking cancellation / modification** — creation only; no status lifecycle (no pending/confirmed/cancelled).
- **Filters other than date range** — no location/price/capacity filtering.
- ~~**Pagination / "load more."**~~ → **DELIVERED in 6.3** (offset pagination on both list endpoints). Moved from deferred into scope.
- **Payment / price snapshot.**
- **Auth-token `TimeProvider` retrofit** (`P3-13`) — the clock seam is for *new* booking code only; leave existing token code on `DateTime.UtcNow`.
- **`OperationResult` reshape** (`P3-15`) — use it as-is.
- **DoB → DateTime retrofit** — out of Phase 6 scope (see §3).

If something genuinely requires changing existing auth/error/validation *behavior* (not additive wiring), **flag it and stop** rather than reshaping the substrate. (The one intentional exception this phase is the 200-status fix, §3 / §8 — explicitly logged.)

---

## 3. Settled decisions (LOCKED — sub-chats must not relitigate)

- **Booking date model:** `DateTime` at the C# level, **compared on the date part only**, all UTC, no time zones. `DateOnly` is *not* used (mentor rule — practicing the pattern needed on legacy 5–10-yr projects; on this stack it isn't a technical necessity, but it's the chosen convention).
- **Column types:** `Booking.CheckIn` / `Booking.CheckOut` → Postgres **`date`** (`HasColumnType("date")`); `Booking.CreatedAt` → **`timestamptz`** (`HasColumnType("timestamp with time zone")`, matching the existing convention for real timestamps). `Apartment.Price` → `HasPrecision(18,2)`.
- **The trap-killer:** normalize incoming request dates with **`.Date` at the use-case seam** *before* they touch the overlap query or an insert. The `date` column protects *stored* values; `.Date` protects the *incoming* comparison parameters. Both, belt-and-suspenders. (See the boundary reasoning in §7.)
- **Model shape:** a booking is a **half-open range `[check-in, check-out)`**; back-to-back (checkout == next check-in) is allowed. Availability is derived **purely from existing bookings** (no host calendar, no blackout windows).
- **Past-date policy:** reject a check-in in the past → adopt **`TimeProvider`** (built-in .NET clock seam) in the create-booking use-case. **New booking code only.** Register `TimeProvider.System` in DI; inject and call `GetUtcNow()` instead of `DateTime.UtcNow`. (Honest note: Phase 6 has no tests, so the deterministic-clock payoff lands later; adopting the seam now is near-free and avoids a retrofit.)
- **Persist mechanism for create-booking:** **reuse the existing `BeginTransactionAsync` → work → `CommitAsync` pattern** (there is no standalone `SaveChanges` port; `CommitAsync` requires an active transaction). The transaction gives atomicity of the single insert, **not** concurrency safety — `P6-01` stays racy; the racy check gets a marker comment.
- **HTTP-status fix (`P6-FIX-200`):** `ToProblemDetailsResult` currently returns `new ObjectResult(problemDetails)` with `StatusCode` left null → the HTTP status line is **200** while the body says 401/404/409. Fix = set the status on the result: `new ObjectResult(problemDetails) { StatusCode = errorStatusCode }`. **Lands in 6.2** (where booking error paths make it visible). This is a substrate behavior change (fixes latent auth wart too) but breaks no unit tests (they assert on `OperationResult`, not HTTP status). Logged in the ledger.
  - *Mechanism precision:* the 200 is **not** caused by the ProblemDetails body carrying a 4xx code — ASP.NET Core never reads the body's `Status` to set the HTTP status. The status comes only from the result's `StatusCode`. This is also why validation errors are already correct: the filter returns `BadRequestObjectResult` (a subclass whose ctor sets `StatusCode = 400`).
- **Navigation properties:** `Apartment.Bookings : List<Booking>` ↔ `Booking.Apartment : Apartment` (bidirectional). `Booking.Client : User` and `Apartment.Owner : User` are **reference navs only** — the Identity `User` gets **no** new collections (keep it lean; query from the Booking/Apartment side).
- **DoB:** **left as `DateOnly`/`date`** (not retrofitted). DB column is already `date`; retrofitting churns the Phase-4 register path (DTO stays `DateOnly` → Mapster gains a conversion; the register validator is touched) for zero booking benefit. Revisit only as a separate cleanup if the mentor insists on uniformity.
- **`Apartment.CreatedAt`:** **omitted** (seeder-only entity, no functional use this phase).

### Field set (pin when writing entities)

**Apartment:** `Id int` (PK) · `OwnerId int` (FK→User) · `Title string` · `Description string` · `Location string` (free text) · `Price decimal` (18,2) · `Capacity int` · navs `Owner`, `Bookings`.

**Booking:** `Id int` (PK) · `ApartmentId int` (FK→Apartment) · `ClientId int` (FK→User) · `CheckIn DateTime` (`date`) · `CheckOut DateTime` (`date`) · `CreatedAt DateTime` (`timestamptz`) · navs `Apartment`, `Client`.

*Sub-chat-settleable detail (not blocking):* `OnDelete` behavior. Sensible defaults — `Booking→Apartment`: Cascade; `Booking→Client` and `Apartment→Owner`: Restrict (don't cascade-delete business/identity data). No delete endpoints exist this phase, so this only affects DB integrity/future-proofing.

---

## 4. Substrate facts a sub-chat must know (discovered from source)

So a fresh sub-chat doesn't re-derive these:

- **JWT claims:** user id is in **`ClaimTypes.NameIdentifier`** (not `sub`); roles in `ClaimTypes.Role`; email in `ClaimTypes.Email`. `MapInboundClaims = false`. → ownership check reads `User.FindFirstValue(ClaimTypes.NameIdentifier)`. `[Authorize(Roles = "Client")]` *should* work (default `RoleClaimType` = `ClaimTypes.Role`, which is what's emitted) — **verify with a real 200-vs-403 probe**, don't assume.
- **Persist pattern:** every write goes `BeginTransactionAsync` → work → `CommitAsync` (which calls `SaveChangesAsync` + commits). `CommitAsync` **throws if no active transaction**. **No standalone `SaveChanges` port.** (Seeders are exempt — they touch the `DbContext` directly, see below.)
- **Repository pattern:** repos take `AppDbContext` via ctor; use `_dbContext.Set<T>()`; `Add(...)` is **void** (doesn't save); queries use `AsNoTracking()` / `Include(...)` / `FirstOrDefaultAsync(...)`.
- **DbContext:** `AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>` with **no `DbSet<>` properties** — entities are registered purely via `ApplyConfigurationsFromAssembly(...)` + navigations. An `IEntityTypeConfiguration<T>` is sufficient to register a new entity; no `DbSet` needed.
- **EF config conventions:** `builder.ToTable("…")`, `HasKey`, `HasOne(x => x.Nav).WithMany([coll]).HasForeignKey(...).OnDelete(...)`, `Property(...).HasColumnType(...)` / `.HasMaxLength(...)` / `.HasPrecision(...)`, `HasIndex(...)` (optionally `.IsUnique()` / `.HasFilter("…")`), enums stored via `.HasConversion<string>().HasMaxLength(...)`.
- **Mapster:** a manually-built `TypeAdapterConfig` in `Application.DependencyInjectionExtensions.AddApplicationMapping`, `NewConfig<Src,Dest>()` with explicit `.Map(...)`, `.IgnoreNonMapped(true)`, then `AddSingleton<IMapper>(new Mapper(config))`. Add booking mappings here.
- **Validation filter:** `AsyncValidationFilter` iterates `context.ActionArguments` and resolves `IValidator<T>` by each argument's **runtime type**. → query params must be bound as **one `[FromQuery]` object** for a validator to fire (two loose `DateTime?` params won't validate). Validators registered via `AddValidatorsFromAssembly(API assembly)`; they live in `BookingApp.API/Validators/`.
- **Error surface:** `OperationResult` carries `Errors : IReadOnlyList<string>` (stable **codes**, not messages). `ToProblemDetailsResult` maps the first code → status via `ErrorStatusCodeMapper` (fallback 400) → type via `ProblemDetailsTypeStatusCodeMapper`. To add booking errors: (a) new `BookingErrorCodes` class in `Application/Errors`; (b) register each code in `ErrorStatusCodeMapper.StatusCodesMap` with its HTTP status (e.g. 404/409/400); (c) add any new status codes (404, 409) to `ProblemDetailsTypeStatusCodeMapper`.
- **DI extension points:** `Application.AddApplicationServices` / `AddApplicationMapping`; `Infrastructure.AddInfrastructureServices` (domain services) / `AddInfrastructurePersistence` (DbContext, Identity, `IUnitOfWork`, repos). Extend these; register new services/repos **Scoped** (matches existing).
- **Seeder pattern:** static class, `CreateAsyncScope()`, resolve services, **idempotent** (check-exists-then-create), called from `Program.cs` after `builder.Build()`. Role seeding (`IdentitySeeder.SeedRolesAsync`) runs first — a data seeder must run **after** it (Host role must exist to assign). Users must be created via the Identity path (`UserManager<User>` or the existing `IUserIdentityService.CreateAsync(user, password)` + `AddToRoleAsync`) to get hashing/normalization; plain entities (apartments, bookings) go through the `DbContext` directly + `SaveChangesAsync` inside the seeder scope.
- **Migrations:** EF Core migrations are the schema mechanism (`dotnet ef migrations add …`, startup = `BookingApp.API`; `dotnet-ef` is a local tool). **Always add a new migration; never hand-edit an already-applied one** (they're recorded in `__EFMigrationsHistory`; editing desyncs the DB from the files and only "works" if you drop-and-recreate).
- **DTO conventions:** validated *request* DTOs use property-init records with nullable props (so validators can catch nulls); *response* DTOs use positional records. Domain entities never cross the Application boundary — map to responses via `IMapper`.

---

## 4.1 Established by sub-chat 6.1 (read side) — facts 6.2 builds on

- **Entities & schema shipped:** `Apartment` / `Booking` (fields as pinned), configs, migration `AddApartmentsAndBookings` applied. `date` columns for `CheckIn`/`CheckOut`, `timestamptz` for `CreatedAt`, composite index `(ApartmentId, CheckIn, CheckOut)`. FK deletes: `Booking→Apartment` Cascade (see P6-05), `Apartment→Owner` & `Booking→Client` Restrict; `User` has no collection navs.
- **Repository:** `IApartmentRepository` (Application) / `ApartmentRepository` (Infrastructure), registered Scoped. Current method: `Task<List<Apartment>> FindAvailableAsync(DateTime? start, DateTime? end)` using `!a.Bookings.Any(b => b.CheckIn < end && b.CheckOut > start)`, `AsNoTracking()`. 6.2 will **add** to it (e.g. an apartment-exists check) and introduce `IBookingRepository`.
- **Overlap formula is proven** in this exact code form (strict `<` / `>`, back-to-back allowed): `b.CheckIn < reqEnd && b.CheckOut > reqStart`. Reuse verbatim for the create-booking overlap check (the deliberately-racy check).
- **`.Date` vs `date` column:** the `date` column is the real trap-killer (EF truncates the compared parameter to `date`); `.Date` at the seam is defensive redundancy. 6.2 should still normalize on write, but know the column type is the guarantee.
- **Mapping:** `Apartment → ApartmentDetailsResponse` added in `AddApplicationMapping`; controller uses **injected `IMapper`** (a static-`.Adapt` mistake was caught and fixed in 6.1 — don't repeat it). Add `Booking → BookingResponse` the same way.
- **No service layer on the read path** (correct — validation via filter, empty result is valid, no `OperationResult` branch). **6.2 is different:** create/get-by-id have real business rules → a proper `IBookingService` returning `OperationResult` is warranted, mirroring `AuthService`.
- **Route convention:** `[Route("api/apartments")]`, `[ApiController]`. Booking endpoints → `api/bookings`.
- **Seeded data for 6.2 testing (dev only):** Host `host_seeded_user_john_doe@example.com` and Client `bob` `client_seeded_user_bob_smith@example.com`, both password `Pa$$word1`. 10 apartments (owned by Host), 20 bookings in **Feb–Mar 2028**. Implications for 6.2: those dates are **future** (now is Sep 2026) → good for forcing overlaps (409) without tripping the past-date rule; use a genuinely past date (e.g. 2020) to test past-check-in rejection. Bob owns the seeded bookings → to test the **ownership trap**, register a *second* client and try to `GET` one of Bob's booking ids → expect not-found.

---

## 5. Prioritization thesis (crucial-first)

The spine of Phase 6 is **schema + the overlap formula**, not "the endpoints." The overlap formula is the single hardest, most bug-prone piece (the half-open boundary trap) and it is **reused** by both the availability query and the create-booking check. So:

- Build the entities/schema/seed data and the **overlap formula in the read path first**, where it can be verified end-to-end **without auth**. `GET /apartments` doubles as the test harness that proves the formula.
- Then reuse a **proven** formula for the create path and add the **security-critical** ownership check (inherently write-side: it needs a created booking + a second user to attempt cross-access).
- The only genuinely deferrable in-scope item is the **my-bookings list** (a plain scoped read, no tricky logic). It + the one-line 200 fix are the spill target if a sub-chat runs long.

---

## 6. Sub-chat plan (the roadmap) — actual **n = 3**

### Sub-chat 6.1 — read side ✅ ("establish schema + prove the overlap formula")
Entities (`Apartment`, `Booking`) → EF config → migration → **seeder** (Host + apartments + a Client + fixed-date bookings exercising overlap) → **overlap predicate (hand-verified in isolation)** → availability query → **`GET /api/apartments`** (public) + query-params DTO & validator. **Delivered & audited.**

### Sub-chat 6.2 — write side + security ✅ ("core write + the ownership trap")
Clock seam (`TimeProvider`) → **create booking** (apartment exists, not past, overlap check reusing 6.1's formula — the *deliberately racy* check, commented at the `BookingService` call site; persist via Begin/Commit) → **`GET /api/bookings/{id}`** with the **ownership check** (`booking.ClientId == callerId`, 404-for-both) → **my-bookings list** → **`P6-FIX-200`** → `BookingErrorCodes` + status/type-map registration. **Delivered & audited** (audited as part of the 6.3 zip). Decided along the way: `CreateBookingRequest` uses non-nullable `int`/`DateTime` with `NotEmpty()`/`GreaterThan(0)` (the CLR default is rejected by an independent rule).

### Sub-chat 6.3 — pagination ✅ (scope addition — originally deferred)
Offset pagination on **both** `GET /api/apartments` and `GET /api/bookings`. Shipped: `PaginatedRequest` (abstract) / `PageQueryParams` (50-cap in the `PageSize` accessor) / `PagedResult<T>` (repo shape) / `PaginatedResponse<T>` (private ctor + `Create` factory). `P6-02` fixed. Both repo queries `CountAsync` on the once-built filtered `IQueryable`, then `OrderBy(Id).Skip().Take()`. **Delivered, audited, mentor-reviewed** (both comments resolved — see §11).

---

## 7. The overlap formula (state it, then test it by hand)

Half-open ranges `[checkIn, checkOut)`. A requested `[reqStart, reqEnd)` is **UNAVAILABLE** for an apartment iff some existing booking satisfies:

```
existing.CheckIn < reqEnd  &&  reqStart < existing.CheckOut      // strict inequalities
```

The available-apartments query returns apartments with **no** such booking: `apartments.Where(a => !a.Bookings.Any(b => b.CheckIn < reqEnd && reqStart < b.CheckOut))`, where `reqStart`/`reqEnd` are `.Date`-normalized. "Available" means the **whole** requested range is free (any single overlapping booking disqualifies).

**Boundary check (why strict `<`, back-to-back allowed):**

| Case | reqStart | reqEnd | vs existing [exStart, exEnd) | Result |
|---|---|---|---|---|
| back-to-back after | `= exEnd` | later | `reqStart < exEnd` is **false** → no overlap | **available** ✔ |
| back-to-back before | earlier | `= exStart` | `exStart < reqEnd` is **false** → no overlap | **available** ✔ |
| identical range | `= exStart` | `= exEnd` | both true → overlap | unavailable ✔ |

`<=` on either side would wrongly mark back-to-back as unavailable. This is the off-by-one to guard.

---

## 8. Debt ledger

| ID | Item | Status | Notes |
|---|---|---|---|
| P3-13 | `TimeProvider` for auth token code | **Deferred** | Booking code adopts the clock; auth token code stays on `DateTime.UtcNow`. Not touched in Phase 6. |
| P3-15 | `OperationResult` shape / null-fold | **Deferred** | Now intertwined with P6-06 — the implicit operator (6.3) made the null-fold easier to hit. Deciding P6-06 = deciding P3-15's open question. With the study over, this is now Yevhenii's call, not the mentor's. |
| P3-16 | Constraints checklist | **Carry forward** | Consult before each change; "decided-then-drifted" is the project's dominant failure mode — it fired repeatedly in 6.3 while editing shipped code. |
| P6-01 | Double-booking under concurrency (TOCTOU) | **Deliberate gap — stands** | Check-then-insert, no lock, no exclusion constraint. Commented at `BookingService.CreateBookingAsync` call site (not the repo, despite the 6.3 summary saying otherwise). Also the core unsolved problem in the parallel assessment task. |
| P6-FIX-200 | `ToProblemDetailsResult` returned HTTP 200 on domain failure | **✅ Resolved (6.2)** | Verified in audit: `new ObjectResult(problemDetails) { StatusCode = errorStatusCode }`. Also fixed the latent wrong-password-returns-200 auth wart. |
| P6-02 | One-sided `.Date` normalization (apartments validator) + residual write-path drift | **✅ Closed (6.3 + master-chat fix)** | Validator fixed in 6.3 (whole-object rule, both operands `.Date`, `.WithName`). Audit then found the create path normalized `.Date` only for the past-check — overlap call, entity map, and response echo used raw values, so a stray-time payload echoed a time the DB didn't store. Fixed (`.Date` on the overlap-call args + on the *source* selectors of `CreateBookingRequest→Booking`; response-map reverted to raw). **Runtime-verified:** stray-time input → midnight in both echo and DB. |
| P6-03 | `FindAvailableAsync` safe by upstream guard, not locally | **Low / note — stands** | Nullable params only behave for both-null or both-set; the validator guarantees both-or-neither. Not reused from an unvalidated path in Phase 6. |
| P6-04 | Inconsistent repo DI registration (`AddInfrastructureServices` vs `AddInfrastructurePersistence`) | **Cosmetic — stands** | The `CancellationToken` signature-inconsistency half was resolved in 6.3; the DI-placement split remains. Trivial fold when convenient. |
| P6-05 | `Booking→Apartment` `OnDelete: Cascade` | **Deferred decision → Phase 7** | Unreachable now (no apartment-delete path). When host endpoints land, cascade would erase a client's booking history on listing delete — revisit with soft-delete/deactivate as a deliberate choice, not an inherited default. |
| **P6-06** | `OperationResult<TValue>.Success(null)` folds to a codeless failure | **NEW — deferred (P3-15 territory)** | Renders as **HTTP 500 + `errorDetails: ["UnexpectedError"]`** (not an empty array, as the 6.3 summary stated). The 6.3 implicit operator makes it reachable *invisibly* via `return value;`. Candidate fix: **throw** (`ArgumentNullException.ThrowIfNull`) so a null-success blows up loudly at origin. Not a live bug today (all current implicit-success values are non-null). Deferred by Yevhenii's choice. |
| **P6-07** | Adopt the `OperationResult` implicit operator solution-wide | **NEW — optional follow-up** | 6.3 scoped it to `BookingService` only; `AuthService`/`UserIdentityService`/`RoleIdentityService`/`RefreshTokenReuseHandler`/`AuthServiceTests` untouched. Belongs in its own reviewed pass. Hard ceiling: the non-generic `OperationResult.Success()` has no value to convert, so those sites can never use it. |
| **P6-08** | Bookings reads use plain `[Authorize]` vs `[Authorize(Roles = Client)]` | **NEW — consciously deferred to Phase 7** | Both reads are caller-scoped (a Host gets a 404-collapse / empty list), so safe as-is. Open question is whether to add role-gating as a fail-fast. **Yevhenii's decision: leave as-is for now.** |
| **P6-09** | Sort key for paginated lists (`Id` vs domain-meaningful) | **NEW — Phase 7+ product decision** | Sorting itself is non-negotiable (proven necessary — see §11 comment 1). Whether "my bookings" should sort by `CheckIn`/`CreatedAt` rather than `Id` is a UX call, not correctness. |
| P6-TEST-OVERLAP | Test the pure overlap function | **Future** | Highest-value uncovered assertion once the testing phase resumes. Note only. |

---

## 9. Progress tracker

| Sub-chat | Scope | Status | Zip audited | Notes |
|---|---|---|---|---|
| 6.1 | read side (schema + overlap + `GET /api/apartments`) | **✅ Complete** | ✅ Yes | Summary matched source. Overlap claims independently re-verified against seed data. All additive. |
| 6.2 | write side (create + ownership + my-bookings + 200 fix) | **✅ Complete** | ✅ Yes (via 6.3 zip) | `IBookingService` + `OperationResult`, clock seam, ownership trap (404-for-both), `BookingErrorCodes`, P6-FIX-200 resolved. |
| 6.3 | pagination on both list endpoints | **✅ Complete** | ✅ Yes | Mentor-reviewed; both comments resolved (§11). P6-02 closed. Construction-discipline pattern established. |
| — | post-approval `.Date` fix (this chat) | **✅ Done + runtime-verified** | Runtime-confirmed | Stray-time payload → midnight in response echo + DB. Closes the P6-02 residual. |

**Phase 6 = ✅ COMPLETE.** Next: hand the Phase 6 summary (companion artifact) up to the program master. No 6.4 needed.

---

## 10. Traps carried into every sub-chat

- **`DateTime`-date invariant** — `.Date` at the use-case seam **and** `date` columns. Single most likely source of a subtle overlap bug.
- **Off-by-one at the boundary** — back-to-back must count as available; strict `<` on both sides (never `<=`). See §7.
- **ID exposure on `GET /bookings/{id}`** (6.2) — load the booking, then verify `booking.ClientId == callerId` from the JWT `NameIdentifier` claim; else a client reads another's booking by guessing an int. The list endpoint is safe only because it's scoped to the caller by construction.
- **Identity from claims, never from input** — never trust a `userId` in body/query for "my" data.
- **`P6-01` gap is deliberate** — don't "helpfully" add locking/exclusion constraints; comment the racy check; don't claim safety.
- **DTO boundary** — Domain entities never cross the Application boundary; map to responses.
- **Machinery that sounds responsible** — before adding a Guid, retry, extra column, or index, ask "what specific failure does this address?" (For locking, the answer is "one we've chosen not to solve yet.")

---

## 11. 6.3 outcomes — mentor review, pagination decisions, durable learnings

### Mentor review (both comments resolved)
- **Comment 1 — "`OrderBy(Id)` is redundant" → kept, settled empirically.** Removing `OrderBy` made EF Core emit `RowLimitingOperationWithoutOrderByWarning` on **26 rows / 1 request** — a static query-shape diagnostic, not something needing adverse conditions, because a table is an unordered set and no SQL DB guarantees row order without `ORDER BY` (Postgres specifics: `UPDATE` relocates tuples, `VACUUM` reuses space, parallel scans interleave, plans switch indexes). Cost of keeping ≈ 0 (PK btree already provides the order). **The right way it was settled: tested, not argued.** Open charitable reading → P6-09 (sort *key*, not sorting).
- **Comment 2 — implicit `Success` operator → adopted, scoped.** `implicit operator OperationResult<TValue>(TValue value) => Success(value)`. Happy path returns collapse to bare values; **failures stay explicit** (deliberate asymmetry — the error path should read loud). Second `List<string>→Failure` operator correctly rejected (collides with the value operator at `OperationResult<List<string>>`). Scoped to `BookingService` only (→ P6-07). Sharpens the null-fold (→ P6-06).

### Pagination decisions (locked in 6.3)
- **Min page size 5 is a *default*, not a floor** — omitted → 5; an explicit `pageSize=1` is honoured.
- **`pageNumber`/`pageSize <= 0` → 400 reject**, not silent clamp (malformed input shouldn't be silently corrected).
- **Deliberate asymmetry on `pageSize`:** low end *rejects* (400), high end *clamps* (caps at 50). Defensible only because the response echoes the **applied** value.
- **Clamping lives in construction, not the validator** (a validator can reject, not transform — see below). The 50-cap is in `PageQueryParams.PageSize`'s accessor.
- **`TotalCount`** = a second `CountAsync()` on the *same* filtered `IQueryable`, built once and branched into count + paged read.

### Durable principle — **construction discipline** (the most reusable thing from 6.3)
The recurring 6.3 bug was *two same-typed values describing one concept, side by side, with call sites expected to remember which to reach for* — it produced the same class of bug ~4 times; naming didn't prevent it, the compiler did.
> **When two values describing one concept can drift apart, collapse them into a single already-correct value, and make the safe construction path the only way to build it.**

Applied: `PageQueryParams.PageSize` clamps *in its accessor* (no adjacent "raw" value to grab); `required` forces named-initializer construction (kills the positional `pageNumber`/`pageSize` swap); `PaginatedResponse<T>` has a `private` ctor + `Create(items, pageQueryParams, totalCount)` factory (the only door in, always sourcing page metadata from the same `PageQueryParams` the repo received) — so the response *physically cannot* claim a page size the query didn't apply. Known limit: a query-bindable value must stay publicly readable, so the raw `PaginatedRequest.PageSize` is still readable — mitigated by reading it in exactly one place.

### Framework findings (verified, worth carrying forward)
- **Validators reject; services transform.** `IValidator.ValidateAsync` returns pass/fail + failures; the filter reads only `.IsValid` and short-circuits *before* the action, never re-reading a mutated DTO. So reject-rules → validator, transform-rules → construction. (Not about query-vs-body — the filter resolves `IValidator<T>` by the argument's runtime type either way.)
- **`[JsonIgnore]` does nothing on `[FromQuery]` complex types.** Bodies are described from `System.Text.Json` metadata (which `[JsonIgnore]` controls); `[FromQuery]` types go through `ApiExplorer`, which reflects public properties and consults no JSON attributes. No property-level ignore exists for `Microsoft.AspNetCore.OpenApi` yet. A computed `SafePageSize` leaked into the OpenAPI doc as a phantom param — resolved structurally (moved the clamp off the bindable type), not by attribute. Not a security issue (no setter → unbindable), a doc-accuracy one.
- **`out` params are illegal on `async` methods** — the state machine can't guarantee assignment before the first `await` suspension. (Why `PagedResult<T>` exists instead of an out-param count.)
- **`RuleFor(x => x).Must(...)` needs an explicit `.WithName()`** or the `errors` key comes out blank (FluentValidation can't derive a name from the whole-object selector). Also: a cross-field `.When()` must guard on **both** operands non-null, or the rule fires against `DateTime.MinValue` when one is merely absent.
- **Property initializers bypass custom `init` accessors** — a `= 5` default is *not* clamped by the accessor (harmless at 5, relevant if a default ever exceeded the ceiling). And unparseable query input (`pageSize=abc`) fails model binding before any validator runs → `[ApiController]`'s framework 400, not the custom filter's shape (same root cause as the `CreateBookingRequest` non-nullable deviation).
