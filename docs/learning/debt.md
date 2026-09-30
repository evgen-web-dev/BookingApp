# Debt Ledger — the single live ledger

Add here; don't start another. **IDs are permanent and never renumbered**, so an
item stays traceable to the phase that surfaced it. New debt is numbered by the
surfacing phase (`P7-01`, …).

Not @-imported by CLAUDE.md — it's read on demand. Each phase names the IDs it
owns in current-phase.md, and /review-as-mentor checks for debt added without an
entry here.

Standing checks live in `decisions.md` (they are constraints you re-verify, not open
work). Everything else below is verbatim from archive/master-original.md §Debt Ledger,
with three recorded exceptions: `P3-16` was updated when the harness answered its own
question, `P4-03`'s premise was corrected on 2026-09-30 (conclusion unchanged), and
`P6-03` was moved in from archive/phase-6.md, which the program ledger had never carried.

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
| `P4-03` | The `DuplicateEmail` map rows are **dead** — remove the rows or flip the flag. **Premise corrected 2026-09-30, conclusion unchanged:** the archive wrote this as "`RequireUniqueEmail = false` leaves the `DuplicateEmail`/`InvalidEmail` map rows dead". Neither named thing exists as written. There is no `RequireUniqueEmail` anywhere in `src/` — `Infrastructure/DependencyInjectionExtensions.cs:34` is a bare `AddIdentityCore<User>()` with no options lambda — so "dead" rests on the framework default being `false`, which is **still unverified**. And there is no `InvalidEmail` row: `Infrastructure/Identity/IdentityErrorCodesDefaultDenyMapper.cs:22-24,36-38` carry `DuplicateUserName`, `DuplicateEmail`, `InvalidUserName`. Note also that the partial unique index on `NormalizedEmail` (`P3-01`) means a duplicate email now fails as a DB error, not as an `IdentityError`, which kills the row a second way. |
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
| `P6-03` | `FindAvailableAsync` safe by upstream guard, not locally — **Low / note — stands**. Nullable params only behave for both-null or both-set; the validator guarantees both-or-neither. Not reused from an unvalidated path in Phase 6. |
| `P6-TEST-OVERLAP` | Unit-test the pure overlap function — the obvious first automated test (Phase 6 shipped test-free). |

*`P6-03` was moved here by `/import-history` on 2026-09-29 from
`archive/phase-6.md:167`, the only ID-carrying item that never reached
`master-original.md`'s ledger. Its source table has a separate Status column; the
status text was folded into the Item cell unchanged. No wording was altered.*

### Introduced in Phase 7 *(surfaced during the archive import)*
*`P7-01` is deliberately unassigned — it stays free for Phase 7's first finding from its own
build work. The gap is not a lost item.*

| # | Item |
|---|---|
| `P7-02` | **`AvailableApartmentsPaginatedRequest.AvailableFrom`/`AvailableTo` use `set`, not `init`** (`Application/DTOs/Apartment/AvailableApartmentsPaginatedRequest.cs:7-8`) — the only DTO where `C-08` ("validators reject; services transform") rests on project convention rather than the compiler; a validator-side mutation there would persist into the action instead of being impossible. Its own base `PaginatedRequest.PageSize`/`PageNumber` are already `init` and bind fine from `[FromQuery]`, so the `set` buys nothing. **Fix: `set` → `init`.** No live bug — no validator mutates anything (all six checked 2026-09-29). Surfaced by `/import-history`, outside Phase 7's scope. |
| `P7-03` | **`PageQueryParams` built from a raw `PaginatedRequest` in two places, in two layers** — `API/Controllers/ApartmentsController.cs:26` and `Application/Services/BookingService.cs:96`, identical lines. `archive/phase-6.md:220` claims the escape hatch is "mitigated by reading it in exactly one place"; it is read in two. Not a live bug (both sites agree today); the risk is drift if the clamp policy or property set changes. Structural cause: Phase 6 deliberately gave the read path no service layer (`archive/phase-6.md:106`), so apartments translate in the controller while bookings translate in a service. **Fix: one door in** — e.g. a `PageQueryParams.From(PaginatedRequest)` factory — which is `D6-01` applied to itself. Surfaced by `/import-history` 2026-09-30, outside Phase 7's scope. |

### Future features / specified but not built
| # | Item |
|---|---|
| `P3-16` | **Constraints checklist** — the decisions-already-made list consulted before each change. **Confirmed live 2026-09-26:** `decisions.md` §Standing constraints is that list, and it is `@`-imported by CLAUDE.md, so it is consulted every turn by construction. Carry it forward. |
| `P3-17` | `HandleReuseAsync` → account-wide invalidation via `UpdateSecurityStampAsync`, at the `RevokeTokenFamily` funnel where `reason == TheftDetected`. **Now unblocked by `P3-05`.** |
| `P3-18` | `/logout-all`, gated by `[Authorize]` — a valid access token *is* cryptographic proof of identity, categorically different from the client-supplied identifier rejected on DoS grounds. Needs `RevokeAllLiveForUser`. |
| `P3-19` | Redis access-token blacklist keyed by **`TokenFamilyId`**, closing the ≤15-min window where a stolen access token outlives its revoked family. |
| `P3-20` | Revoked-row retention/cleanup; idempotency keys for `/refresh`; device/IP risk signals. Out of current scope: DDoS resilience (infrastructure-layer concern — reverse proxy / CDN / WAF). |
