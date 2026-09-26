# BookingApp — Phase 5 Master Document

**Phase 5 = unit tests only.** Integration tests are deferred to a later, not-yet-numbered phase (6/7/8).
This chat is the **Phase 5 orchestrator/master**. Concrete work happens in numbered sub-chats. The program-level master lives in the original orchestrator chat and is updated separately when Phase 5 is fully closed.

**STATUS (updated after 5.1):** Phase **5.1 is COMPLETE and mentor-PR-approved** (Tier-1 committed scope delivered). **Phase-5 closure decision is pending** — see §10. Tiers 2–3 were not built and are reclassified to a future testing pass (§5).

---

## 0. How to use this document

- This is the durable tracker for Phase 5. It wins over memory/prose on any conflict. When something conflicts, say so out loud rather than silently reconciling.
- When Yevhenii returns from a sub-chat with a **summary + updated zip**, follow the **Update Protocol (§8)** — audit the zip first, update this doc, *then* regenerate the extract if a further sub-chat is needed. Master before extract, never the reverse.
- The **Deferred Ledger (§5)** is the anti-forgetting mechanism. Nothing deferred is dropped — it is parked here with a reason and a destination.

---

## 1. Current state (verified against source)

Phase 4 ("Hardening") is complete and was audited against source at the start of Phase 5. Phase **5.1** is complete and audited against its delivered zip (`BookingApp-phase-5_1-completed-and-mentor-approved.zip`) — see §1.5 for exactly what landed and §9 for audit findings.

Confirmed-true substrate (carried from the Phase-4 audit):
- **P3-14 done** — Mapster global static config gone; `AddApplicationMapping` builds a local `TypeAdapterConfig` and registers `new Mapper(config)` as a singleton `IMapper`. Parallel test runs unblocked.
- **`TokenFamilyRepository.Revoke` guarded** — `WHERE Id == @id AND RevokedAt == null`; idempotency holds at the DB level.
- **Login single failure literal** — `UserIdentityService.AuthenticateAsync` returns `[InvalidEmailOrPassword]` for both "no such user" and "wrong password" (`CheckPasswordAsync` hash-only). *Note: this invariant currently holds in code but is pinned by NO test — see §5, Tier 2.*
- **Unified ProblemDetails contract present** — `ValidationProblemDetails`/400 for field failures; plain `ProblemDetails` + `errorDetails` string-code array for domain failures; empty-errors → `UnexpectedError`/500.

**Phase 5 scope decision — Scenario #1 (confirmed):** cover `AuthService.RegisterAsync` and `AuthService.LoginAsync` with unit tests only. Integration tests are a separate future phase. Acknowledged tradeoff: behaviors unit tests structurally cannot reach (the ObjectResult HTTP-status question; DB-guard idempotency) are unmonitored until that future phase. Accepted; tracked in §5.

---

## 1.5. Phase 5.1 — completed (what landed)

**Delivered tests — 27 total, in `BookingApp.UnitTests` (references Application + Domain + API):**
- `RegisterRequestValidatorTests` — 14 (all 7 rules, both directions)
- `LoginRequestValidatorTests` — 4 (Email, Password)
- `AuthServiceTests` — 9 (`RegisterAsync` ×5, `LoginAsync` ×4); full branch coverage of both in-scope methods, including the `HasActiveTransaction` catch-branch in **both** directions for each method.

**Scaffolding facts (recorded):**
- Created via `dotnet new xunit3 --test-runner vstest`. The `--test-runner` flag defaults to MTP; requesting VSTest explicitly yields the **`xunit.v3.mtp-off`** package variant (3.2.2), *not* plain `xunit.v3` — they are not interchangeable.
- `OutputType=Exe` (xUnit v3 test projects are self-hosting). No `global.json` anywhere. `DisableTestingPlatformServerCapability` is inert when no MTP package is present, so it was omitted.
- Package pins: `xunit.v3.mtp-off` 3.2.2, `xunit.runner.visualstudio` 3.1.5, `Moq` 4.20.72, `Shouldly` 4.3.0 (stable 4.x, not 5.x preview), `Microsoft.NET.Test.Sdk` **17.4.1** *(⚠ see §9 finding #1 — summary claimed 17.14.1; confirm/bump)*.
- Earlier Rider instability did not recur on Rider 2026.2.1; original fix was a full reinstall (not cache invalidation).

**Production changes made in 5.1 (with tradeoffs — mentor-approved; recorded, not relitigated):**

1. **DTOs positional-record → init-only properties** — `RegisterRequest`, `LoginRequest`, `AuthenticatedUserResult`. Driver: object-initializer construction for the tests' `readonly`-field data pattern. Blast radius: two hand-construction sites in `UserIdentityService` (`AuthenticateAsync`, `GetWithRolesById`), both updated. **Tradeoff:** positional records forced non-null values at construction; the init versions do not — this introduced nullable warnings (CS8618 on `AuthenticatedUserResult.Email`/`Roles`; CS8601 on `LoginRequest = default`; `RegisterRequest` used `= null!` to suppress). Compiles (no warnings-as-errors), and the validator catches nulls at runtime, so no live bug — but a weakened compile-time guarantee. **Cleaner fix if ever revisited:** `required` init members (keeps initializer syntax *and* compile-time enforcement, zero warnings). `RegisterResponse`/`IssuedTokens`/`CreateUserResult` remain positional (unchanged).

2. **`RegisterRequestValidator` name-regex rework (mentor-directed)** — `.Matches(NameChars)` replaced by `.Must(name => Regex(NameChars).IsMatch(name) && !Regex(GluedPeriod).IsMatch(name))` on FirstName/LastName/MiddleName, each with a custom message. `GluedPeriodRegExpPattern = @"\p{L}\p{L}\.\p{L}"`. Net: `"Jack.Jack"` now rejected (was accepted — the impl/comment discrepancy that triggered this); `"Jack JR."` still accepted; `"J.Jack"` (single-letter initial) accepted, since the pattern needs two letters before the period. **Tradeoff:** constructs 2 `new Regex(...)` per field per validation call — worse than `.Matches()` (which compiled once per validator). Immaterial at current scale; `[GeneratedRegex]` or `static readonly Regex` is the fix.

**Test conventions established in 5.1 (durable — reuse in all future test phases):**
- **Mutation probes** — deliberately break the SUT, confirm the test goes red, revert. A test that can't be shown to fail on a real defect is ceremony. (Used in 5.1 to verify the `HasActiveTransaction` gate, argument-swap detection, `RegisterResponse` Id pass-through, empty-error-list propagation.)
- **Exact argument matching** for values the SUT passes through unchanged (catches field swaps like `request.Password`→`request.Role`); `It.IsAny<T>()` only where the SUT legitimately constructs/transforms the value.
- **Derived test data** (e.g. build the expected value from the same input the SUT uses) rather than two hardcoded strings that happen to match.
- **`readonly` fields, not `=> new X{}` properties**, for test data matched by reference (a property hands out a fresh instance per access and silently breaks exact matching for reference types like `User`).
- **Constructor-based happy-path setup** stored in `readonly` fields; each failure test overrides one/two mocks; happy-path bodies have zero `Setup` calls. Safe because xUnit constructs a fresh test-class instance per test.
- **`out`-param mocking** via `out It.Ref<string>.IsAny` + a delegate that assigns the out value.
- **Relative dates** for time-dependent validators, with a one-day buffer only where the inequality's strictness makes the exact boundary unreachable.

---

## 2. The rule that decides unit vs integration (so it's transferable)

> **Integration is *forced* exactly when the behavior under test lives in the database engine or the HTTP pipeline — not in your C#.** Mock those and the test asserts your *mock's setup*, which keeps passing after the real behavior breaks. That is ceremony, and it fails the "honest, not ceremonial" bar.

Everything in your C# (branching, propagation, mapping-shape, pure functions) is unit-reachable through the ports already built. Everything Postgres or the framework does *for* you is not — that is the future integration phase.

---

## 3. Teaching mode (applies to every sub-chat, verbatim)

Be a constructive critic, not a yes-man. Challenge weak reasoning, hidden assumptions, and overconfidence — target the *reasoning*, not just the conclusion. Separate fact from opinion from uncertainty. Verify claims with tooling (real HTTP, real DB, forced failures) rather than accepting explanations. Timebox low-stakes reversible decisions. Gate on unresolved decisions — don't list them and move on. Ask for real source before designing against it. No copy-pasting code without understanding — concept before code.

**Recurring failure modes (Yevhenii's, tracked deliberately):**
- *"Right conclusion, wrong stated mechanism"* — correct answer via borrowed reasoning from an adjacent system. Call out the mechanism, not just the conclusion.
- *"Decided-then-drifted"* — a decision made correctly early not carried into later code, especially during refactors.
- *Extended deliberation on low-stakes reversible decisions* (naming/placement). Flag when it recurs.

---

## 4. Phase 5 roadmap (status)

**Standing bar:** every test must be able to *fail on a real bug*. If it would pass regardless of a plausible defect, it doesn't get written.

### Tier 1 — Crucial (committed scenario-#1 core) — ✅ DONE (5.1, mentor-approved)
1. ✅ Scaffold `BookingApp.UnitTests`.
2. ✅ Validator tests (`LoginRequestValidator`, `RegisterRequestValidator`).
3. ✅ `AuthService.RegisterAsync` tests (all branches incl. both `HasActiveTransaction` directions).
4. ✅ `AuthService.LoginAsync` tests (auth-fail literal propagated; happy path; both hashing-failure `HasActiveTransaction` directions).

### Tier 2 — Strongly recommended — ❌ NOT DONE (reclassified → §5)
5. `UserIdentityService.AuthenticateAsync` enumeration invariant (both failure causes → the **same** `InvalidEmailOrPassword`). **Zero coverage anywhere.** Cannot be proven from `AuthService` (only propagation can). Guards a regression whose trigger (lockout) is itself deferred.

### Tier 3 — Cheap pure freebies — ❌ NOT DONE (reclassified → §5)
6. `RefreshTokenService` (Base64Url/32-byte/SHA-256 round-trip + malformed inputs).
7. `JsonWebTokenService` (claims/alg/issuer/audience; **not `exp`**).
8. [Optional] `ToProblemDetailsResult` body-shape (covers body construction, NOT the HTTP status — that is the deferred integration probe).

---

## 5. Deferred Ledger (parked, not forgotten)

| Item | What it is | Why deferred | Destination |
|---|---|---|---|
| **Tier 2 — enumeration invariant** | Both login failure causes must yield the *same* `InvalidEmailOrPassword`; lives in `UserIdentityService`, needs the `Mock<UserManager<User>>` harness | Not built in 5.1; low interim risk (invariant currently holds; lockout deferred) | **Future testing pass** — batch with the deferred `RefreshAsync`/token-logic unit tests; RECORD prominently so "later" ≠ "never" |
| **Tier 3 — pure freebies** | `RefreshTokenService`, `JsonWebTokenService` (not `exp`), `ToProblemDetailsResult` body-shape | Not built in 5.1 | **Future testing pass** |
| **`RefreshAsync` / `LogoutAsync` unit tests** | Orchestration branches (expired→family Expired; replay→family TheftDetected; happy rotation); mental model established in 5.1 | Deferred by choice | **Future *unit* pass** (some branches need P3-13 for `exp`). ⚠ Strict is OFF (see below) — these must add explicit `Verify(Times.Never)` for `_tokenFamilyService`/`_refreshTokenRevoker`/`_logger` or the safety net is gone |
| **Integration test suite (whole)** | Contract HTTP status/body end-to-end, `Set-Cookie` on failed refresh, rollback DB-state (P3-09), guarded-write idempotency, at-most-one-live-token (partial unique index), P4-02 race | Scenario #1 is unit-only | **Future phase (6/7/8)** — additive (`BookingApp.IntegrationTests` + `public partial class Program {}`); non-blocking |
| **ObjectResult HTTP-status hypothesis** | `ToProblemDetailsResult` returns `new ObjectResult(pd)` with no `StatusCode` — suspected HTTP 200 with a 401/400/500 body. Contrast: validation uses `BadRequestObjectResult`; exception handler uses `IProblemDetailsService.WriteAsync` | Needs real HTTP → integration | **Future integration phase — run the ~5-min probe FIRST** (`/api/auth/login`, bad creds, assert `StatusCode`). If 200: one-liner `new ObjectResult(pd){ StatusCode = pd.Status }` |
| **P3-13 — `TimeProvider` seam** | Direct `DateTime.UtcNow` in 3 layers | Not on scenario-#1 path | **Whenever token-logic / `exp` / booking-deadline tests are wanted.** Shared by future unit + integration — not throwaway |
| **P3-15 — `OperationResult<TValue>` gate** | `.Value` public, `default!` on failure; `Success(value)` folds null → error-less failure | Deferred | **Future.** Do NOT write a test cementing the null→`Success` fold; keep the `empty-errors → 500` assertion |
| **Debt: nullable-annotation** (NEW, from 5.1) | Init-only DTOs default non-nullable strings to null (CS8618/CS8601/`null!`) | Introduced by the DTO conversion | **Future cleanup** — prefer `required` init members |
| **Debt: regex allocation** (NEW, from 5.1) | `RegisterRequestValidator` builds 2 `new Regex` per field per call | Introduced by regex rework; immaterial now | **Future cleanup** — `[GeneratedRegex]` / `static readonly Regex` |
| **Debt: test-class granularity** (NEW, from 5.1) | `AuthServiceTests` covers two SUT methods in one class; per-method split deferred | Mechanical; grows per method | **Future refactor** when a third method is added |
| **Debt: `"J.Jack"` untested** (NEW, from 5.1) | Single-letter-initial-glued-to-name is accepted (intended) but no test pins it | Not added in 5.1 | Add `[InlineData("J.Jack")]` to the FirstName/LastName invalid theories |
| **Validator relocation** | Validators live in API but validate Application DTOs | Refactor, not a test task | **Future refactor** — moving to Application lets tests drop the API reference |
| **Deferred hardening** | `P3-06` rate limiting/lockout (lockout stays out — enumeration invariant), `P4-01` global rate limiting, `P4-03` `RequireUniqueEmail` dead rows, `P4-04` small batch, `P3-07` request size limit | Not tests | **Later hardening pass** — tests may reveal; flag, don't fix |
| **Future features** | `P3-17` account-wide invalidation, `P3-18` `/logout-all`, `P3-19` Redis blacklist, `P3-20` retention/cleanup | Out of scope | **Later feature phases** |

**⚠ Standing caveat — `MockBehavior.Strict` removed in 5.1.** `_tokenFamilyService`, `_refreshTokenRevoker`, `_logger` are unconfigured Loose mocks. Under Strict, an unexpected call threw; under Loose it silently returns default. **If `RegisterAsync`/`LoginAsync` ever start calling those, no existing test fails.** All "must not be called" claims now rest solely on explicit `Verify(..., Times.Never)`. Any future test touching those dependencies must add such a `Verify` or the safety net is absent.

---

## 6. Ceremonial-test traps to pre-load

- **The `HasActiveTransaction` trap.** The `catch { if (HasActiveTransaction) SafeRollback }` branch only runs if the mock reports `true`; a default `Mock<IUnitOfWork>` returns `false`, so a "rollback on exception" test passes exercising nothing. (Handled correctly in 5.1 — set it deliberately per test, ideally flipping true-after-Begin / false-after-Commit.)
- **Outcomes vs. boundaries.** Push to edges: the failure literal (both causes → one code), malformed refresh-token strings, under-18/absurd age, the empty-error-list mapping.
- **The enumeration invariant lives in `UserIdentityService`, not `AuthService`.** A pure-AuthService suite proves only *propagation* (as 5.1's does).
- **Assert Result shape, not exceptions.** Auth failures **return** `OperationResult` with codes; they do **not** throw. `Should.ThrowAsync<...>` is the wrong shape for those (right only for the genuine guard exceptions like `InvalidRefreshTokenHashGenerationException`).

---

## 7. Judgment calls made (don't re-litigate)

- **P3-13 deferred** for scenario #1 (time-free `RegisterAsync`; `LoginAsync`'s clock reads are in mocked collaborators; age rule tested with relative dates). Cost: skip the exact-birthday edge.
- **Test project references `BookingApp.API`** to reach the validators (they live in the host project). Known mild smell; relocation is a deferred refactor.
- **401 is mentor-blessed** for `InvalidEmailOrPassword`/`InvalidRefreshToken`/`UserNotFound`; pin it when the contract layer is tested. Do NOT also assert `WWW-Authenticate` (deferred; anonymous endpoints).
- **DTOs converted positional→init** (mentor-approved) — tradeoff/debt recorded in §1.5/§5, not reversed.
- **`MockBehavior.Strict` removed** (mentor-directed) — consequence recorded as a standing caveat in §5.

---

## 8. Update Protocol (run when Yevhenii returns from a sub-chat)

Trigger: a sub-chat summary + updated zip, work mentor-PR-approved.

1. **Audit the zip against the summary.** Every archive read has surfaced drift. Diff production code against the previous base to catch *undeclared* changes; read the actual test files. Note discrepancies explicitly. *(5.1 pass: diff was clean on scope; caught the Test.Sdk version drift and the nullable debt — see §9.)*
2. **Update THIS master doc** — move completed roadmap items; update the ledger; record new findings + any new audit-failure entries. Master before any extract.
3. **Decide whether a further sub-chat is needed.** If Phase-5 scope is satisfied, prepare the Phase-5 close-out summary for the program-level master instead.
4. **Only if a further sub-chat is needed**, regenerate the launch extract via the appendix spec. Never before the master is updated.

---

## 9. Audit findings & audit-failure log

**5.1 audit findings (this pass):**
1. **ACTION — Test.Sdk version drift.** Summary §1 claims `Microsoft.NET.Test.Sdk` 17.14.1; the shipped csproj pins **17.4.1** (2022-era, anomalously old for .NET 10 + xUnit v3). Restored/ran green, but almost certainly a typo. **Confirm and likely bump to 17.14.1.**
2. **RECORDED — nullable-annotation debt** from the DTO conversion (CS8618/CS8601/`null!`), not flagged in the summary. See §1.5 / §5.
3. **CONFIRMED accurate** — `"J.Jack"` accepted and untested; Strict-removal consequence real. Both carried to §5.
4. **Process note:** the production diff vs the 4.2 base showed exactly the 5 declared files, nothing undeclared — the "audit against source" step worked as intended.

**Audit-failure log (Claude's own misses — carried forward):**
- **3.2** — endorsed wrong architectural placement for `AuthService` (origin of the "sync only after PR approval, audit against source" gate).
- **3.3** — incorrect empirical prediction about compiler behavior.
- **3.4** — wrong entity-name recommendation.
- **Phase-4 audit** — background notes placed `TokenFamilyService` in Infrastructure; it is in Application. Corrected.
- **Standing humility flag:** the ObjectResult-status item is a hypothesis, not a finding (could not compile; have mis-modeled framework internals before). Filed as "verify first."
- **5.1 audit:** no new Claude miss identified; drift was caught, not missed.

---

## 10. Phase-5 closure decision (OPEN)

Committed Tier-1 scope is delivered and approved, so Phase 5's **contractual** scope is met. The open question is whether to close Phase 5 here or add one small sub-chat first.

- **Option A — Close Phase 5 now.** Fold Tier 2 (enumeration invariant) + Tier 3 into a future testing pass alongside the deferred `RefreshAsync`/`LogoutAsync` and token-logic tests (they share infrastructure and are more coherent done together). Low interim risk: the enumeration invariant currently holds in code and its threat (lockout) is itself deferred. **Recommended**, provided the ledger keeps Tier 2 prominent.
- **Option B — One small 5.2** for Tier 2 (+ optionally Tier 3) before closing, to pin the enumeration invariant now while the `UserManager`-mock context is fresh.

On close: write the Phase-5 close-out summary for the program-level master chat (which then updates the program master and generates the Phase-6 launch extract).

---

## Appendix — Launch-extract generation spec (orchestrator-only)

**Guiding principle:** the extract's value is **narrowness** — everything in it must be load-bearing for the target sub-chat. Not brevity; *relevance*.

**Required sections:** (1) Banner — "this doc wins on conflict; get the zip and re-verify signatures before writing test code; the zip is authoritative." (2) Teaching mode verbatim (§3). (3) Project one-liner + stack. (4) Scope + explicit out-of-scope walls. (5) Current shape — *audited* signatures, flagged "re-verify against the zip"; never fabricate unconfirmed shapes. (6) Build order, dependency-ordered, with per-item traps. (7) Judgment calls already made. (8) Traps to pre-load. (9) Standing checks / mentor gates. (10) Reference material — patterns that transfer vs. don't.

**Non-negotiables:** master updated before extract; audit the zip before writing docs; never design tests against prose when a zip is available.
