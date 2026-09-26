# BookingApp — Phase 4 Master (Hardening) — COMPLETE

> **Role.** Orchestrator/tracker for Phase 4 (master chat). Ran as two sub-chats: 4.1 (predictable errors & input) and 4.2 (auth correctness & cleanup). **Phase 4 is complete.** The handoff artifact for the program master is the separate *Phase 4 Completion Summary*.
>
> **Provenance note.** All 4.1/4.2 code was audited against the sub-chat zips (both drifted from their summaries — see §3a). The **five final loose-end changes** (explicit rollback on `AuthService:66`/`72`; deletion of `ErrorDuringTokenRefresh` const and the `User→RegisterResponse` map; `Roles.ToFrozenSet()`) were **owner-applied and have now been verified against the updated zip — all five confirmed, no drift.** Phase 4's record is fully source-backed.

---

## 1. Teaching mode *(verbatim)*

Be a constructive critic, not a yes-man. Challenge weak reasoning, hidden assumptions, and overconfidence — target the *reasoning*, not just the conclusion. Separate fact from opinion from uncertainty. Verify claims with tooling (real HTTP, real DB state, forced failures). Timebox low-stakes reversible decisions. Gate on unresolved decisions. **No copy-paste-without-understanding.**

---

## 2. Project one-liner

**BookingApp** — Airbnb-like booking system, portfolio-grade. ASP.NET Core / **.NET 10**, PostgreSQL + EF Core (Npgsql), Clean Architecture, Identity, JWT access + opaque rotating refresh tokens, Mapster (now injected `IMapper`), FluentValidation. Secrets in user-secrets. Phase 5 = xUnit/Shouldly/Moq.

---

## 3. What shipped in Phase 4 *(final)*

**4.1 — Predictable errors & input:** partial unique index on `NormalizedEmail` (P3-01, deliberate DB-enforced uniqueness + `Single*`-lookup safety, **not** a race fix); `AppExceptionHandler : IExceptionHandler` with nested-try/`HasStarted` guard (fixed a real Production `refreshToken`-cookie leak); unified **ProblemDetails** contract (`ValidationProblemDetails` for input, `ProblemDetails` + `errorDetails` array for domain, empty→`UnexpectedError`/500); `ILogger` replacing `Console.WriteLine`; centralized status/type mapping; FluentValidation on register/login via reflection-based `AsyncValidationFilter`, emitting through the contract.

**4.2 — Auth correctness & cleanup:**
- **P3-05** idempotent family revocation — `Revoke` guards `&& RevokedAt == null`; reason/timestamp immutable after first write. **Runtime-verified** (Adminer). Double-logout kept as single `401 InvalidRefreshToken` literal (enumeration-safe).
- **Reuse logging** at the shared choke point (`RefreshTokenReuseHandler.HandleReuseAsync` → `LogWarning`, `ILogger<T>` DI bug fixed); covers replays via both `/refresh` and `/logout`; the only surviving trace once the DB write is a no-op.
- **RefreshAsync:197** returns `InvalidRefreshToken` (401) with explicit rollback; `ErrorDuringTokenRefresh` code removed.
- **P3-08** refresh cookie cleared on failed `/refresh`.
- **P3-09** explicit rollback on **all** in-transaction early returns — `RefreshAsync:202` and both `RegisterAsync` sites (`:66`, `:72`). Every early return is now before-tx, post-commit, or post-rollback. *(Rationale: `:72` had a real uncommitted user insert relying on scope-disposal; `:66` is a no-op but kept for pattern uniformity.)*
- **AddToRole map patch** — `DuplicateUserName`/`DuplicateEmail`/`InvalidUserName` → `InvalidEmailOrUserName` (was degrading to 500).
- **P3-14** global `TypeAdapterConfig` mutation → `TypeAdapterConfig` instance + `AddSingleton<IMapper>(new Mapper(config))` (`MapsterMapper`, base package — not `ServiceMapper`); `AuthService` uses `_mapper.Map<User>(request)`. Unblocks parallel xUnit.
- **P3-10** `TokenFamilyService` moved to **Application** (orchestration over ports, zero infra deps — the outlier in its old folder). Deliberate owner decision; mentor to be informed.
- **P3-11** `LoginResult`, `LogoutRequest`, off-interface `UnitOfWork.SaveChangesAsync` deleted.
- **P3-12** 8 DI extensions return `IServiceCollection` (`AddAppFilters` excluded); duplicate `.Map(MiddleName)` removed; unused `User→RegisterResponse` map removed; `Roles` → `IReadOnlySet<string>` via `.ToFrozenSet()`; seeder typo fixed.
- **P3-06** rate limiting — implemented then **fully reverted** (owner learning-quality call); deferred.

### 3a. Sub-chat drift (historical — all now resolved)
Both sub-chat summaries diverged from source; caught by audit. 4.1: claimed the `ErrorDuringTokenRefresh` collapse + map patch that didn't land (fixed in 4.2). 4.2: claimed P3-10 deferred (it was done), 3 rollback sites (only 1 was), the const deleted (still declared), the `User→RegisterResponse` map removed (still present), `ToFrozenSet` (was plain `HashSet`). **All five 4.2 drifts have since been resolved by the owner and verified against the updated zip — no residual drift.** Lesson retained: summaries are not a reliable record — independent source audit is mandatory (it caught 7 divergences this phase).

---

## 4. Tracker

| Sub-chat | Status | Mentor-approved |
|---|---|---|
| 4.1 | ✅ Complete | Yes |
| 4.2 | ✅ Complete | Yes (P3-10 = deliberate owner decision; inform mentor) |

---

## 5. Debt ledger → carries to Phase 5 / future *(feeds the program Debt Ledger)*

- **P3-06 IP rate limiting** — deferred. Notes: `AddPolicy(name, ctx => RateLimitPartition.GetFixedWindowLimiter(key, …))` (not `AddFixedWindowLimiter`, a single global bucket); 429 is middleware-level, outside the ProblemDetails contract; account lockout stays out (2nd failure literal → enumeration).
- **Global (all-endpoint) rate limiting** — new, distinct from P3-06 (coarse DoS, higher ceiling).
- **Refresh-token read/write race** — `AsNoTracking` pre-tx read can see stale `RevokedAt` under READ COMMITTED; **safe** (guarded `ExecuteUpdateAsync` resolves at write, second writer matches 0 rows). Window exists but not exploitable into double-issuance. Worth a two-terminal test.
- **`RequireUniqueEmail = false`** → dead `DuplicateEmail`/`InvalidEmail` map rows; remove rows or flip the flag.
- **`WWW-Authenticate` on 401s** (RFC 9110 §15.5.2); **503-vs-500** for DB-unreachable; **validator `Type` caching**; **age boundary `<=`/`<`** confirm (code uses `<=`); **`CreatedAtAction`** (needs a GET endpoint); **`MapInboundClaims`** (inert); **login timing side-channel** (awareness).
- **P3-13** `TimeProvider` seam (testability); **P3-15** `OperationResult<TValue>` shape (`Success(null)`→empty-failure); **P3-17** account-wide invalidation (unblocked by P3-05); **P3-18** `/logout-all`; **P3-19** Redis blacklist; **P3-20** retention/cleanup.

---

## 6. Audit-failure log *(historical record)*

**Claude's errors across Phase 4:** endorsed the "race condition" framing for P3-01 before testing; the `IEnumerable<IValidator>` diagnosis chased registration when the fault was consumption-side; copied the `ServiceMapper` pattern (unused package) and a redundant `AddSingleton(config)` (**caught by owner**) — both local-pattern-transplant; reached for `AddFixedWindowLimiter` (global) vs partitioned `AddPolicy`; repeatedly delivered prose summaries instead of diff-per-item under a no-copy-paste workflow. **Audit meta-finding:** both sub-chat summaries drifted from source (2 in 4.1, 5 in 4.2) while asserting source-verification — reinforcing mandatory independent audit.

**Owner patterns:** *right-conclusion-wrong-mechanism* recurred (P3-01 race framing; drift#1 status predicted 500 vs actual 400; "rollback does nothing here" missing the already-executed `ExecuteUpdateAsync`; the line-66/72 "before-tx or committed" rule that didn't cover those two). Verification instinct improving (asks "how would I verify this?"). Strong, unprompted pushback on the assistant (caught the redundant `AddSingleton`; rejected the rate-limiter on learning grounds; twice demanded a diff instead of a summary). Empirical discipline caught the Production cookie leak and the `RequireUniqueEmail` self-contamination pre-ship.

---

## 7. Next step

Produce the **Phase 4 Completion Summary** (done — separate artifact) for integration into `Bookingapp program master`. The five owner-applied loose-ends were audited against the updated zip — all confirmed. **Phase 4 is fully closed and source-backed; ready to fold into the program master.**
