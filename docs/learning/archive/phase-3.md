# Phase 3 — Auth — Master Progress Doc

> **This is the Phase-3 archive / backstop — it lives in the orchestrator chat, not the sub-chats.**
> It holds the full decision history, open questions, and learnings log. At the end of each sub-chat,
> update this first (it's the quality gate), then regenerate the lean **launch extract** from it — the
> extract is the boarding pass that actually gets pasted into the next sub-chat.

---

## 0. How to teach me (applies to every sub-chat)

- **Constructive critic, not a yes-man.** Challenge weak reasoning, hidden assumptions, missing
  evidence, overconfidence. Give counterarguments when justified. Agree only when the reasoning holds.
- I'm a **.NET/C# newbie** with real experience in **JS, PHP, WordPress, Laravel** — analogies to
  those are welcome, but flag where they mislead (Identity ≠ Laravel auth, etc.).
- **Concept before code.** "Why" first, then "how." Don't paste code walls I don't understand. When a
  line does something non-obvious (e.g. what `.AddEntityFrameworkStores<AppDbContext>()` actually
  wires, why it needs the DbContext type), explain it.
- **Recurring failure mode to watch for:** I tend to reach *correct conclusions via wrong or
  incomplete reasoning*. Make me articulate the actual domain/architectural rationale, not just the
  answer.
- **Verify, don't assert.** Correctness is confirmed by tooling — build output, migration diffs,
  Adminer inspection, restart-idempotency — not by taking an explanation at face value.
- **Mentor is the decision authority** on major architectural choices. Your job is to surface
  tradeoffs and challenge reasoning, not to substitute for the mentor.
- I'll ask for **self-reflect checkpoints** before moving on — honor them.
- **Timebox low-stakes reversible decisions** (naming, etc.): pick one, note the tradeoff in a line,
  move on. Don't let me over-deliberate them. *(Added after 3.2 — this bit me twice.)*

---

## 1. Project at a glance

**Booking app (Airbnb-like), portfolio-grade.** ASP.NET Core · Clean Architecture
(Domain / Application / Infrastructure / API) · PostgreSQL in Docker · EF Core (Npgsql) · .NET 10 · Rider.
**Planned:** ASP.NET Core Identity · JWT (access + refresh) · Mapster · FluentValidation ·
xUnit / Shouldly / Moq. **DTOs:** `record` types. **Roles:** Client, Host (Admin later).

---

## 2. Phase 3 roadmap

| Sub-chat | Scope | Density | Output |
|---|---|---|---|
| **3.1** ✅ | Identity **data layer** | — | Identity wired, roles seeded idempotently, `AspNet*` schema verified |
| **3.2** ✅ | Register + login, **no tokens** | High — but **held as one chat** (no split needed) | `/register`, `/login` working & verified; `IAuthService` over Identity; record DTOs; Mapster; transactional register; `AddIdentityCore` migration done |
| **3.3** ✅ | JWT **access** tokens + protected routes | Medium — **held as one chat** | login returns a JWT; `[Authorize]` + `[Authorize(Roles=…)]` enforced (401/403 both verified); `ITokenService` port; options validated at startup |
| **3.4** ✅ | **Refresh** tokens | **High — split into 3.4.1 (decisions) / 3.4.2 (build)** | opaque tokens hashed at rest, single-use rotation, reuse/theft detection, `TokenFamily` + sliding TTL under an absolute ceiling, `HttpOnly` cookie transport, `/refresh` + `/logout` |

> **PHASE 3 IS COMPLETE.** All four sub-chats merged after mentor PR review. This doc is now the
> Phase-3 archive; its durable content is rolled up into the program-level BookingApp master, which
> takes ownership of the cross-phase debt ledger (see §6 and §8).

**Splitting policy:** the 4-chat plan is the *map*, not a guarantee. If a sub-chat gets heavy, split it
(3.2.1/3.2.2, 3.4.1/3.4.2), carry a rolling seed block back to the orchestrator chat, update this doc.

---

## 3. Settled decisions — 3.1 (Identity data layer)

| Decision | Rationale |
|---|---|
| `User : IdentityUser<int>` lives in **Domain** | Accepted, mentor-approved CA violation. Pragmatic; not critical. |
| `AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>` | Identity's EF schema (`AspNet*` tables) with `int` keys. |
| Roles as **seeded string constants** (`static class Roles`) | Not an int enum — string role names are what Identity stores & checks; avoids enum↔string mapping. |
| `IdentitySeeder.SeedRolesAsync`, **idempotent**, via `CreateAsyncScope` in `Program.cs` | Roles seeded once; verified they don't re-seed on restart. `CreateAsyncScope` + `await using` for correct async disposal in startup code. |
| Domain NuGet: **`Microsoft.Extensions.Identity.Stores`**; Infrastructure: **`Microsoft.AspNetCore.Identity.EntityFrameworkCore`** | Keeps EF Core out of Domain's compile-time vocabulary — Domain sees Identity abstractions, not EF. |
| Phase-2 hand-rolled user code **deleted** | `UserService`, `IUserService`, `UserRepository`, `IUserRepository`, `UserRoleConfiguration`, `UsersController`, `CreateUserDto`, `UserDto`, `UserRole` — all superseded by Identity. |

**Key mechanics confirmed at 3.1:**
- `.AddEntityFrameworkStores<AppDbContext>()` registers the concrete EF-backed `IUserStore`/`IRoleStore`.
  Without it, managers resolve at compile time but throw `InvalidOperationException` at runtime. Needs
  the DbContext type so the stores know *where* to persist.
- `NormalizedName` / `NormalizedUserName` / `NormalizedEmail` exist for **portable** case-insensitive
  uniqueness, computed in C# rather than relying on DB collation.
- Property-collision rule: colliding props (vs `IdentityUser`) must be **deleted** from `User`, not
  redeclared with `new` (which creates two independent slots) or hidden.

---

## 3B. Settled decisions — 3.2 (register + login, no tokens) — ✅ merged after PR review

> Reflects the **post-PR-review** design. The mentor's main correction: `AuthService` was misplaced in
> Infrastructure; it's a use-case orchestrator, so it moved to **Application** behind ports. (See the
> learnings log for why the original placement — which an earlier sync endorsed — was wrong.)

| Decision | Rationale |
|---|---|
| DI: `AddIdentityCore<User>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AppDbContext>()`, in Infra's `AddInfrastructurePersistence(config)` | No default auth scheme → 3.3's JwtBearer becomes the challenge scheme → clean **401**, not a cookie **302**. `AddRoles` restores `RoleManager` for the seeder. Registration split into `AddInfrastructureServices()` + `AddInfrastructurePersistence()`; Application has `AddApplicationServices()` + `AddApplicationMapping()`. |
| **`AuthService` in Application/Services**, depends only on ports `IUnitOfWork` + `IUserIdentityService` | Use-case/workflow logic belongs in **Application**; Identity/EF concretes hide behind ports. **Layering verified via project refs:** Application → Domain only (never Infrastructure); Infrastructure → Application+Domain; API → Application+Infrastructure. |
| Ports **shaped by the caller**, not the wrapped library | `IUserIdentityService` = `CreateAsync(User,pw)` / `AddToRoleAsync(User,role)` / `VerifyCredentialsAsync(email,pw)` — only what `AuthService` needs, not `UserManager`'s surface. `IUnitOfWork` = `Begin/Commit/Rollback(ct)` — a **transaction scope**, not `SaveChanges` (Identity's `CreateAsync` self-saves). |
| `UnitOfWork` (Infra/Persistence) holds the `IDbContextTransaction`, implements `IAsyncDisposable` | Begin guards double-start; Commit/Rollback guard no-transaction; both dispose the tx; `DisposeAsync` is the safety net (DI disposes the concrete instance even though registered as `IUnitOfWork`). |
| `OperationResult` / `OperationResult<TValue>` (replaced `AuthResult`) | `record` with `protected init` + static `Success`/`Failure` factories. `Value` nullable — convention: check `Succeeded` first (not type-enforced; `OneOf`/DU judged overkill). The Application seam — no `IdentityResult` crosses the boundary. |
| Login via `IUserIdentityService.VerifyCredentialsAsync` (wraps `FindByEmailAsync` + `CheckPasswordAsync`) | Scheme-free. On failure the port returns an empty error list and **`AuthService` emits the single fixed `"InvalidEmailOrPassword"`** — enumeration-safe. Lockout deferred to 3.3 (`SignInManager` needs a scheme). |
| Role gate = **Domain-level** `Roles.RolesAvailableForPublicRegistration` (`HashSet`), checked in `AuthService` | "Which roles may self-register" is a domain policy, not infra detail. Blocks self-registering as Admin. (`RoleExistsAsync` ≠ "may self-assign".) |
| Error **codes**, not messages, via `IdentityErrorCodesDefaultDenyMapper` (Infra/Identity, static) | Returns stable codes (client/localization maps to text). **Default-deny allowlist**: separate scoped maps (register / add-to-role) + a generic map; unknown Identity codes dropped, not leaked. Lives in Infra (used by `UserIdentityService`). |
| Controller returns **unwrapped** payloads; HTTP status is the sole outcome signal | `Ok(value)` / `BadRequest(ErrorResponse(codes))`. `CreatedAtAction` deferred (TODO in code) until `UsersController` exists. |

**`RegisterAsync` (Application):** Domain role-whitelist (fail-fast, pre-tx) → `Begin` → `try` { `CreateAsync` → `AddToRoleAsync` → `Commit`, `isCommitted=true` } `finally` { rollback if `!isCommitted` }. **`LoginAsync`:** `VerifyCredentialsAsync` → fixed generic error. Rollback-on-failure verified in 3.2.

---

## 3C. Settled decisions — 3.3 (JWT access tokens) — ✅ merged after PR review

> Verified against the 3.3 archive, not just the summary. Where the summary and the code disagree, the
> code wins and the difference is noted.

| Decision | Rationale |
|---|---|
| **Package split:** `Microsoft.IdentityModel.JsonWebTokens` in **Infrastructure** (issuance), `Microsoft.AspNetCore.Authentication.JwtBearer` in **API** (validation) | Issuance and validation are different jobs in different layers. Confirmed in the `.csproj` files. |
| `ITokenService` port in Application (`string GenerateAccessToken(AuthenticatedUserResult user)`); impl `JsonWebTokenService` in Infra | Same port pattern as `IUserIdentityService`/`IUnitOfWork`. **Synchronous by design** — token construction is pure CPU, no I/O, so an `async` state machine would suspend on nothing. |
| `JsonWebTokenHandler` + `SecurityTokenDescriptor` over legacy `JwtSecurityTokenHandler` | Fewer allocations, current MS recommendation. Different API shape (`CreateToken(descriptor)`, not `new JwtSecurityToken(...)` + `WriteToken`) — most online examples show the legacy shape. |
| **`AuthenticateAsync` replaced `VerifyCredentialsAsync`** (prerequisite refactor, not on the task list) | The old port returned a bare yes/no, so `AuthService` had no id/email/roles to build claims from. New shape returns `OperationResult<AuthenticatedUserResult>` (Id, Email, Roles) from **one** `FindByEmailAsync` reused across `CheckPasswordAsync` + `GetRolesAsync` — the naive split would have double-looked-up the email. **Port shape follows the caller's available data** (an email string), not the wrapped library. |
| Claims emitted as **`ClaimTypes.*`** (Role / NameIdentifier / Email) | `RoleClaimType` already defaults to `ClaimTypes.Role`, so matching the default = zero extra config, nothing to keep in sync. *(Tradeoff not priced: long WS-* URIs are ~50–60 chars per claim of token bloat on every request, and are a .NET-ism rather than the interoperable `sub`/`email`/`role`. Defensible, but argued on one side only.)* |
| JWT config via **validated options**, `JwtOptions` in `Application/Options/Auth/` | 4 keys in `dotnet user-secrets` under `Jwt`. `AddOptions<JwtOptions>().Bind(...)` + 4 `.Validate(...)` + `.ValidateOnStart()` → app refuses to start on blank key or zero lifetime (both verified). Placement caveat recorded by the user: Application is the only project **both** Infra and API can see — shared *visibility*, not conceptual ownership; Application never consumes it. Two separate types would also have been valid. |
| `JwtBearerOptions` configured via **named options + deferred dependency** | Actual code: `AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<JwtOptions>>(...)`, then a bare `.AddJwtBearer(scheme)`. This is the correct alternative to `BuildServiceProvider()` for reading one options object while configuring another. *(Summary described two methods `AddJwtOptions`/`AddJwtAuthentication`; the code has one `AddJwtAuthentication(services, configuration)`.)* |
| `TokenValidationParameters`: all four validations on, `ValidAlgorithms = [HmacSha256]`, `MapInboundClaims = false`, `ClockSkew = 3 min` | `ValidAlgorithms` pins the algorithm so the token's own header can't choose how it's verified (the `alg: none` class of attack). `ClockSkew` **still unjustified** — see Q14. |
| Pipeline: `UseHttpsRedirection` → `UseAuthentication` → `UseAuthorization` → `MapControllers` | Authn→authz is a **data dependency** (authz reads the principal authn populates), not convention. |
| Access token lifetime **15 min** | Access tokens can't be revoked before expiry; short window limits damage. Refresh tokens (3.4) make renewal cheap. |
| Error codes extracted to `Application/Errors/AuthErrorCodes.cs` (post-PR) | Closes the "codes as scattered literals" item (old Q13d). |

**Verification highlights (all real HTTP):** 401 without token vs 401 `error="invalid_token"` with a garbage one — *the differing `WWW-Authenticate` header proves validation actually ran*; a hand-crafted jwt.io token accepted **before** any issuance code existed (gate proven to accept, not just reject); app-issued token → 200; `[Authorize(Roles=Host)]` → 200 for Host, **403** for wrong role (authz distinguishes wrong-role from unauthenticated); `ValidateOnStart` halts startup on bad config.

---

## 3D. Settled decisions — 3.4 (refresh tokens) — ✅ merged after PR review

> Split into **3.4.1** (decisions only, no code) and **3.4.2** (build). Verified against the archive.

| Decision | Rationale |
|---|---|
| **D1 — refresh token is an opaque random string, not a JWT** | Two independent load-bearing reasons: (a) revocation *requires* an external check — a JWT's validity is self-contained and can't encode "…but not this one"; (b) baked-in claims go stale over a multi-day life (role change, ban). Together: a JWT refresh token would still need a full DB read every refresh → **zero work left for the JWT structure to do.** Wins outright, not on balance. **Build consequence honored:** on `/refresh` the token is a *lookup key only* — never decoded, never trusted for claims; new access-token claims come from a fresh `GetWithRolesById`. |
| **D2 — SHA-256 hash at rest, looked up by hash** | Not a perf tradeoff — **bcrypt is structurally incompatible.** bcrypt embeds a random per-call salt, so the same input hashes differently every time; passwords dodge this by finding the row *by email* first. A refresh token is found **by the hash itself** (`WHERE token_hash = @incoming`), so the hash must exist before the lookup — circular under bcrypt. Secondary: slow hashes buy nothing against a high-entropy server-generated secret. Hashing at rest is still required (DB leak / SQLi / log spill must not yield live credentials). |
| **D3 — single-use rotation + strict reuse detection, no grace window** | Revoked rows are **kept, never deleted** — without the row, a replayed stolen token and random garbage both yield "not found," so *reuse detection could never fire*. Identity comes from the matched row's FK, **never from the request** (a client-supplied identifier lets anyone who knows a victim's email force-revoke their families with a garbage token — trivial DoS; tested and rejected). Revocation is **family-scoped, not user-scoped** (multi-device). A grace window would have to hand the loser the *same live token* the winner got — handing a live credential to a possibly-unverified requester in exactly the scenario being defended against; client-side dedup is frequency mitigation, never correctness. |
| **Exclusivity mechanism:** `UPDATE … WHERE id = @id AND revoked_at IS NULL`, branch on rows-affected | `1` = won, `0` = lost → theft assumed. Postgres row-level write locking is baseline MVCC at **every** isolation level; under `READ COMMITTED` the blocked statement re-reads and re-evaluates its `WHERE` on unblock. Free exclusivity, no `SERIALIZABLE`. |
| **G2 — `ExecuteUpdateAsync` over `UseXminAsConcurrencyToken`** | The deciding argument was **not** performance. `xmin` only answers *"has this row changed since I read it"* — in the **stale-intent** case (attacker rotates at 23:00, victim presents the dead token at 23:07) nothing changes between read and write, so `xmin` matches and the update **silently succeeds**: a false negative on precisely the scenario D3 exists to catch. `ExecuteUpdateAsync` folds the business condition into the same `WHERE` that provides exclusivity — **one mechanism covers both the race and stale intent, by construction**, and the check *is* the query so it can't be forgotten. Accepted cost: bypasses the change tracker and executes immediately (documented at the call site) — which is the point, since the result must be reported synchronously to branch on. |
| **D4 — sliding 3-day token TTL under a 14-day absolute family ceiling** | `RefreshToken.ExpiresAt` recomputed on every rotation; `TokenFamily.AbsoluteExpiresAt` set once at login and **never touched by rotation**. Both checked independently — without the second, nothing ever consults the ceiling because the sliding window resets first. **Why the ceiling lives on the family:** copying it per-token works mechanically but makes the invariant depend on rotation code *remembering to copy rather than recompute* — one natural-looking `AddDays(14)` silently restores unbounded sliding. Storing it once means rotation code has no reason to touch it. **This became the decisive argument for keeping the family table when the mentor floated dropping it.** |
| **Transport: `HttpOnly` + `Secure` + `SameSite=Lax`, `Path=/api/auth`, `MaxAge`=14d** | `HttpOnly` stops JS *reading* the cookie, not *using* it (same-origin `fetch` from an XSS payload still carries it). A CSRF on `/refresh` can't steal a token (SOP blocks reading the response) — the real damage is an **attacker-triggered false-positive theft detection**, i.e. on-demand logout DoS. `SameSite=Lax` blocks cross-site POST; **the Lax+POST 2-minute grace applies only to cookies with no `SameSite` attribute**, so it must be set explicitly in code. **CORS is not the CSRF defense** — a plain `<form method=POST>` never preflights. CSRF token deferred, not a gap, for a POST-only JSON endpoint never rendered as a form. **Contingency:** viable only while FE and API share a registrable domain; a true cross-domain split reopens this and makes a CSRF token non-optional. |
| **32-byte token, Base64url** | Sized against the scenario you *can't* rate-limit: offline GPU-speed guessing of a leaked DB, where D2's fast hash is deliberate. **The cost curve is flat** — storage is fixed by hash width, CPU/bandwidth differences negligible — so unlike D4 there's no two-sided tradeoff and no minimal-sufficient number to find. Establish you're far past the danger zone, pick a conventional value, move on. ⚠ The initial "32 bytes, same as the JWT signing key" reasoning was **wrong**: the JWT key matches HMAC-SHA256's output width (an algorithm constraint); a bearer secret has no algorithm to match. Same number, unrelated reason. |
| Generation + hashing live in **Infrastructure** | Not the BCL-vs-NuGet test (weak, doesn't discriminate). **Cohesion:** the crypto exists solely to feed a persistence workflow (generate → hash → store; hash → look up), leaving Application as pure orchestration. |
| Interface split: `IAccessTokenService` + `IRefreshTokenService` | `JsonWebTokenService` producing an opaque non-JWT would be a lie in the class name → two implementors → ISP says split. Hashing sits on `IRefreshTokenService`, **not** the repository: the repo needs it on both insert and lookup paths, i.e. two implementations of one algorithm free to drift; and a repository's boundary is persistence, not deciding how a raw input becomes the stored value. |
| **Entity shape** | **No `UserId` on `RefreshToken`** — 3NF violation (transitively dependent on `TokenFamilyId`, letting one fact live in two rows that can disagree); identity flows `RefreshToken.TokenFamilyId → TokenFamily.UserId`. **No `ReplacedByTokenId`** — nothing needs the ordered rotation chain (D3 needs "find by hash, is it revoked"; family revocation is a bulk sweep). **Partial unique index `(TokenFamilyId) WHERE RevokedAt IS NULL`** converts "one live token per family" from a convention rotation code must remember into a **DB-enforced invariant**; a revoked row *exits* the indexed set, so rotate-then-insert never conflicts. Postgres `CHECK` constraints structurally cannot reference other rows, so a partial unique index is the only tool. Verified by forcing `23505`. |
| `RevocationReason` as a real C# `enum` + `.HasConversion<string>()` | Compile-time exhaustiveness in code, human-readable values in Adminer. The `const string` reflex copied from `Roles.cs`/`AuthErrorCodes.cs` was wrong — **those are strings only because external APIs force them to be.** Enum collapsed `{Logout, TheftDetected, AbsoluteExpiry, SlidingExpiry}` → `{Logout, TheftDetected, Expired}`: no consumer distinguished the two expiry causes, and needing an arbitrary priority rule for the both-true case was the tell that the distinction wasn't real. |
| All six timestamps explicitly `timestamp with time zone` | Npgsql ≥6.0 maps a bare `DateTime` to `timestamp without time zone` and **throws at write time** on a `Kind=Utc` value. `DateTime` carries a `Kind` *tag*, not an offset (that's `DateTimeOffset`), and no C# syntax constrains `Kind` at the type level — so the **column type** is what turns a `DateTime.Now` slip into a loud failure instead of silent corruption. |
| `int` PKs retained; `Guid` migration considered and **rejected** | Enumeration is only exploitable where authorization is missing; with per-resource ownership checks planned, `Guid` pays index fragmentation and 4× key size to guard a door that already has a lock. **Carry the check, not the answer** — re-run "is this ID client-exposed, and is access independently authorized?" per entity. |
| `OnDelete(Cascade)` configured **explicitly** on both FKs | Matches EF's convention default, but the difference is "cascade because I decided it" vs "cascade because nobody overrode it." Named cost: deleting a user erases their theft-incident history. |
| **G1 — `IUnitOfWork` had no save path** | It worked only because `AuthService` persisted exclusively via `UserManager.CreateAsync` (which saves internally); a non-Identity entity couldn't be persisted from Application at all. Resolved by adding `SaveChangesAsync`, then **superseded in PR review**: `CommitAsync` now saves internally and `SaveChangesAsync` was removed from the interface — nothing legitimately needs to save without committing, and exposing both was a foot-gun. `isCommitted` bool replaced by `HasActiveTransaction`, so correctness no longer depends on every future branch remembering to set a flag. |
| Error codes: `/refresh` + `/logout` failures collapse to `InvalidRefreshToken` | Malformed / not-found / expired / reuse-detected are indistinguishable to the client and the frontend's action is identical (clear state, redirect) — but distinguishing them hands an attacker *"that token was genuine and already used"* vs *"that was never a token."* The internal distinction survives in `TokenFamily.RevokedReason` *(see Q22 — that field is currently overwritable)*. |
| Two DTO layers: `IssuedTokens` (internal) vs `LoginResponse`/`RefreshResponse` (wire) | **Structural protection, not style.** `Ok(object)` returns a non-generic `ActionResult` that converts to `ActionResult<T>` **without type-checking the payload** (verified empirically). A single combined DTO would let `return Ok(result.Value)` silently ship the raw refresh token in the JSON body with the declared return type still reading correctly. |
| `Session` → **`TokenFamily`** (mentor) | It's a token family, not a session — no session semantics (no device/IP/last-seen), and the name invited them. Migration removed (`database update InitialIdentity` first, since applied) and regenerated as `AddTokenFamiliesAndRefreshTokens`. |
| `ClockSkew` 3 → **2 min**; `IRoleService` → **implemented** as `IRoleIdentityService` | Skew applies to **access tokens only** — it exists because a *validating* server's clock may differ from the issuer's; refresh expiry is checked against our own clock and a timestamp we wrote. Seeder rewired through the port and now iterates `Roles.AllRoles`. |

**Transaction pattern (all four service methods):** `BeginTransactionAsync` → `try { work; CommitAsync }` → `catch { if (HasActiveTransaction) SafeRollbackAsync; throw; }`. `SafeRollbackAsync` swallows a failing rollback so the **original** exception propagates *(this also resolves old Q13c)*.

---

## 4. Current code — where it lives

> **Code's source of truth is the repo (+ GitHub), not this doc.** Master carries decisions and
> learnings; the launch extract carries the signatures the next sub-chat needs. Full source is no
> longer pasted here — duplicating code across two docs invites drift, and the repo is the real backup.
> (3.2's summary didn't include source, so the extract holds signatures, not literal code — paste real
> files into the sub-chat when they're being modified.)

**Post-3.4 file inventory — FINAL Phase-3 state (verified against the archive):**
- **Domain** — `Entities/`: `User`, `TokenFamily`, `RefreshToken`, `RevocationReason` (enum), `RevokeOutcome` (enum); `Roles.cs` (`Host`, `Client`, `RolesAvailableForPublicRegistration`, `AllRoles`).
- **Application** — `Interfaces/`: `IAuthService`, `IUserIdentityService`, `IRoleIdentityService`, `IAccessTokenService`, `IRefreshTokenService`, `IRefreshTokenRevoker`, `IRefreshTokenReuseHandler`, `ITokenFamilyService`, `IRefreshTokenRepository`, `ITokenFamilyRepository`, `IUnitOfWork`. `Services/`: `AuthService`, `RefreshTokenRevoker`, `RefreshTokenReuseHandler` (stub). `Options/Auth/`: `JwtOptions`, `TokenFamilyOptions`. `Errors/AuthErrorCodes`. `Exceptions/`: `InvalidRefreshTokenHashGenerationException`, `AuthenticatedUserHasNoEmailException`. `DTOs/OperationResult`; `DTOs/Auth/`: `IssuedTokens`, `LoginResponse`, `RefreshResponse`, `LoginRequest`, `RegisterRequest`, `RegisterResponse`, `AuthenticatedUserResult`, `CreateUserResult`, `ErrorResponse` *(+ orphans `LoginResult`, `LogoutRequest` — Q25)*.
- **Infrastructure** — `Identity/`: `UserIdentityService`, `RoleIdentityService`, `IdentityErrorCodesDefaultDenyMapper`. `Services/`: `JsonWebTokenService`, `RefreshTokenService`, `TokenFamilyService` *(placement — Q24)*. `Persistence/`: `AppDbContext`, `UnitOfWork`, `Repositories/{RefreshTokenRepository, TokenFamilyRepository}`. `EntityTypeConfigurations/`: `RefreshTokenEntityTypeConfiguration`, `TokenFamilyEntityTypeConfiguration`. `Seeders/IdentitySeeder`. `Migrations/`: `InitialIdentity`, `AddTokenFamiliesAndRefreshTokens`.
- **API** — `DependencyInjectionExtensions` (`AddJwtAuthentication`); `Program.cs`; `Controllers/AuthController` (`POST /register`, `/login`, `/refresh`, `/logout` + cookie helpers). Throwaway test endpoint **removed**.

---

## 5. Phase 3 closed — what comes next

**Phase 3 is done.** Auth is complete end to end: register, login, JWT access tokens, protected routes
with role checks, and refresh tokens with rotation, reuse detection, and revocation.

**Phase 4 gets its own dedicated chat** (sub-split to 4.1/4.2 only if needed), launched from a boarding
pass generated off the **program-level master**, not this doc. Ordered frontier:

1. **Unique index on `NormalizedEmail`** — first task; `AuthenticateAsync` → `FindByEmailAsync` is a live latent 500.
2. **Global exception handling + `ProblemDetails`** — also fixes `/logout` leaving the cookie uncleared on an unhandled exception.
3. **FluentValidation** (`RegisterRequest` names are `string?` but `User` requires them).
4. **Rate limiting on auth endpoints** (`Microsoft.AspNetCore.RateLimiting`) — no brute-force protection exists today.
5. **Request body size limit** (Kestrel / `[RequestSizeLimit]`) — the correct layer for oversized-payload defense.
6. **`ILogger`** to replace `Console.WriteLine` in `SafeRollbackAsync`.
7. `CreatedAtAction` for register once a GET endpoint exists.

**Flag early in Phase 4:** Q20 (Mapster global static config) is a **Phase 5 blocker** and is cheapest to
fix while already touching Application wiring.

**Phase 5:** unit tests (xUnit/Shouldly/Moq), the `TimeProvider` seam (Q19), Q18 (`OperationResult` shape).

---

## 6. Open questions carried across Phase 3

| # | Question | Status |
|---|---|---|
| Q1 | Login path & lockout | ✅ **Resolved** — `UserManager.CheckPasswordAsync` used; lockout deferred to 3.3 (needs an auth scheme). |
| Q2 | `IAuthService` interface layer | ✅ **Resolved** — Application. |
| Q3 | `AddIdentity` → `AddIdentityCore` | ✅ **Resolved** — done in 3.2. |
| Q4 | **Email uniqueness is EMERGENT, not enforced** | 🔴 **Deferred → Phase 4, FIRST task** (decided in 3.3). Still a live latent 500. Holds only because `UserName==Email`; `AuthenticateAsync`→`FindByEmailAsync` is the crash path. Fix: unique index on `NormalizedEmail` (consider **filtered** `WHERE NormalizedEmail IS NOT NULL`) + optional app pre-check (UX only). *(Observed exception is version-dependent; the durable fix is the index.)* |
| Q5 | No global exception handling | 🔴 **Deferred → Phase 4.** Raw 500s from the Q4 path and from null required fields (`RegisterRequest` names are `string?` but `User` requires them → NOT-NULL insert failure). **Confirm the 400-vs-500 boundary empirically.** |
| Q6 | `ProblemDetails` (RFC 7807) for errors | **Deferred → Phase 4** (with Q5). Error **codes** now exist (`AuthErrorCodes`), so the payload shape is the remaining piece. |
| Q7 | Logging strategy | Open — log full unfiltered `IdentityResult.Errors` server-side, return filtered to client; own pass later. |
| Q8 | `CreatedAtAction`/`Route` for register success | Open — when `UsersController` + get-user endpoint exist. (Login stays 200 — nothing created.) |
| Q9 | `DuplicateEmail` error mapping | ✅ Added to the mapper — but **dormant** until Q4 (still unreachable; `DuplicateUserName` fires first via `UserName==Email`). |
| Q10 | `AddApplicationServices()` grouping | ✅ **Resolved** — exists (`AddScoped<IAuthService, AuthService>`). |
| Q11 | `CancellationToken` propagation | **Partial** — `RegisterAsync` now threads `ct` to the UoW (`Begin/Commit/Rollback`); `LoginAsync` and `IUserIdentityService` still don't expose one (the `UserManager` calls can't take it anyway). |
| Q12 | `IRoleService` dead abstraction | ✅ **Resolved in 3.4** — implemented as `IRoleIdentityService` (Infra `RoleIdentityService`); `IdentitySeeder` rewired through the port and now iterates `Roles.AllRoles`. |
| Q13 | Minor cleanups (batch) | **Mostly done.** ✅ (c) rollback no longer masks the original exception — `SafeRollbackAsync` swallows a failing rollback and the original `throw;` propagates. ✅ (d) error codes centralized. Still open: (a) duplicate `MiddleName` Mapster line *(still present)*; (b) unused `User→RegisterResponse` map *(still present)*; (e) `Roles` HashSets are mutable `public static readonly` → `IReadOnlySet`/`FrozenSet` *(now two of them)*. |
| Q14 | `ClockSkew` unjustified | ✅ **Resolved in 3.4** — set to **2 min** with rationale: skew applies to **access tokens only**, since it exists for clock difference between *validating* and *issuing* servers. Refresh expiry is checked against our own clock and a timestamp we wrote, so it's unaffected. |
| Q15 | `MapInboundClaims = false` never reasoned through | 🟡 Open — almost certainly **inert today**: the inbound map keys on short names (`sub`/`role`/`email`) while the token emits long `ClaimTypes.*` URIs, so nothing matches either way. Becomes load-bearing only if issuance switches to short claim names — a latent cross-file coupling worth a comment. |
| Q16 | No brute-force protection on `/login` | 🔴 Open → **Phase 4** (rate limiting). `CheckPasswordAsync` remains hash-only: no lockout, no `AccessFailedCount`. TODO still in `AuthenticateAsync`. |
| Q17 | Enumeration-safety conditional on one literal | ✅ **Verified still intact after 3.4.** `AuthenticateAsync` emits exactly one literal (`InvalidEmailOrPassword`). The new `UserNotFound` code comes only from `GetWithRolesById` and never reaches the client (`RefreshAsync` maps it to `ErrorDuringTokenRefresh`). **Keep re-checking when auth failure paths change** — this is a standing trap, not a closed item. |
| Q18 | **`OperationResult<TValue>` final shape** | 🟡 Open by explicit deferral — `Value` is public/ungated (`Failure(...).Value` returns null typed non-nullable, no warning, no throw), and `Success(null!)` silently returns a **contentless failure with an empty `Errors` list**. Not reachable by any current call site. See the learnings entry for the disagreement (domain failure vs contract violation) — and the note that the discussed-but-rejected error-code variant dominates the adopted one. |
| Q19 | **`TokenService` uses `DateTime.UtcNow` directly** | Open — not injectable, so `exp` can't be asserted exactly in unit tests. `TimeProvider` (.NET 8+) + `FakeTimeProvider` is the seam. Deliberately deferred to **Phase 5 testing**. |
| Q20 | **Mapster global static config** | 🟠 Open — `AddApplicationMapping(this IServiceCollection _)` mutates Mapster's **global static** `TypeAdapterConfig` via a discarded parameter: hidden global state disguised as DI, and `AuthService`'s `.Adapt<User>()` has an invisible dependency on it. **Test-order-dependent under xUnit's parallel runner** → matters for Phase 5. Proposed fix (instance `TypeAdapterConfig` + injected `IMapper` via `Mapster.DependencyInjection`) discussed, not implemented. |
| Q21 | Housekeeping (3.3 batch) | **Partly done.** ✅ throwaway `test/protected` removed; ✅ orphaned `VerifyCredentialsAsync` gone. Still open: DI extension methods return `void` (convention is `IServiceCollection` for chaining); unused `using`s; `IdentitySeeder` message typo ("Failed to see a role"); `AuthController.Login` doesn't accept/forward a `CancellationToken`. |
| Q22 | **Family revocation is not idempotent → benign replay recorded as theft** | 🔴 **Open — highest-value finding of the 3.4 audit.** `TokenFamilyRepository.Revoke` has **no `WHERE RevokedAt IS NULL` guard**, and `RefreshAsync` never checks `TokenFamily.RevokedAt` (only token `ExpiresAt` + family `AbsoluteExpiresAt`). Any token from an already-revoked family therefore lands in the `IsAlreadyRevoked` branch → family re-revoked as `TheftDetected`, **overwriting both `RevokedAt` and the original `RevokedReason`**. Realistic path: family revoked as `Expired` → 400 returned **without clearing the cookie** (Q26) → client retries the dead token → reason rewritten to `TheftDetected`. Today = audit-trail corruption, which undermines §3D's claim that the internal distinction "survives in `RevokedReason`." **Once the deferred `HandleReuseAsync` lands** (security-stamp / forced password change), a normal logout or expiry plus one stale retry would trigger **account-wide invalidation** — cosmetic issue becomes user-facing DoS. **Fix before implementing that TODO.** Cheap: guard the family update with `RevokedAt IS NULL` (first revocation wins) and/or short-circuit an already-revoked family as plain-invalid without escalating. |
| Q23 | **`RefreshAsync` early return leaves the transaction neither committed nor rolled back** | 🟡 Open — the `GetWithRolesById` failure path returns from inside `try`; only `catch` rolls back. Outcome is still correct because the scoped `UnitOfWork` is `IAsyncDisposable` and disposing an uncommitted `IDbContextTransaction` rolls it back — but that's **implicit**, and it's the one failure path *not* covered by the "rollback verified at three injection points" testing (those were exception paths). Make it explicit. |
| Q24 | **`TokenFamilyService` placement contradicts `RefreshTokenRevoker`** | 🟡 Open — it sits in **Infrastructure** but has **zero Infrastructure dependencies**: it takes two Application ports and orchestrates two calls, structurally identical to `RefreshTokenRevoker`, which was placed in **Application** in the same PR. By the project's own 3.2 rule (orchestration → Application), it belongs in Application. Fold into the pending mentor review of service placement. |
| Q25 | **Orphaned code (third consecutive sync)** | 🟡 Open — `LoginResult.cs` (summary says merged into `IssuedTokens`; file present, zero references), `LogoutRequest.cs` (never mentioned, zero references), `UnitOfWork.SaveChangesAsync` (removed from the interface as stated, still public on the concrete). **Pattern: "deleted" in a summary has meant "removed from the interface / no longer called" three sub-chats running.** Standing check: grep for references before recording a deletion. |
| Q26 | **Refresh cookie not cleared on failed `/refresh`** | 🟡 Open — `/logout` clears it on every path; `/refresh` returns 400 leaving the dead cookie in place, so the client keeps replaying a known-invalid token (which feeds Q22). |
| Q27 | Doc-vs-code drift on error-code collapse | 🟢 Minor — a second code, `ErrorDuringTokenRefresh`, is reachable on the `GetWithRolesById` path. Defensible (the caller already proved possession, so not an enumeration vector), but the decision is documented as absolute and isn't. Fold it in or soften the claim. |
| Q28 | Deferred-but-specified work from 3.4 | Open — `HandleReuseAsync` → account-wide invalidation via `UpdateSecurityStampAsync`, hooked at `RevokeTokenFamily` where `reason == TheftDetected` (single funnel; do **not** duplicate per service) — **blocked on Q22**. `/logout-all` gated by `[Authorize]` (a valid access token *is* cryptographic proof of identity — categorically different from the client-supplied identifier D3 rejected); needs `RevokeAllLiveForUser`. Redis access-token blacklist keyed by **`TokenFamilyId`**, to close the ≤15-min window where a stolen access token outlives its revoked family. |

---

## 7. Running learnings log

**From 3.1 / refactor (carried in):**
- **Constraint reasoning:** domain semantics decides DB constraints (NOT NULL, unique). "Fails softly"
  is a *tiebreaker only* when the domain genuinely doesn't decide. "Open vs closed by default" is never
  the principle.
- **Surface signals ≠ coupling.** Package/file/namespace names pull risk assessment more than
  warranted. Real question: do EF/Identity types appear in a layer's *compile-time vocabulary*?
- **NuGet versions:** bare versions are floors (min), not exact pins; `[x]` pins exactly. All-bare
  graphs are conflict-proof (resolution = `max` of floors, no ceiling collisions).
- **Identity wiring:** `AddIdentity<TUser,TRole>()` registers managers; `.AddEntityFrameworkStores<T>()`
  registers concrete EF stores (needs the DbContext). Missing the latter → runtime
  `InvalidOperationException`.
- **`NormalizedName` cols** → portable case-insensitive uniqueness, computed in C#.
- **Filtered/partial unique indexes** are the portable answer for uniqueness over nullable columns
  (plain unique indexes treat NULLs inconsistently across engines).
- **DI scope:** `AppDbContext` is Scoped → two repos in one request share one change tracker.
  `CreateAsyncScope()` + `await using` for manual startup scopes ("Async" = disposal capability, not
  creation cost). GC never calls `Dispose`; disposal is always caller-triggered.
- **Inheritance:** colliding props → delete from derived class (not `new`, not hide).
- **Async idioms:** `Task.Yield()` to simulate a suspension point (≈ `Promise.resolve()`), not
  `await Task.Run(() => {})`. `lock` is thread-affine & can't wrap `await`; use `SemaphoreSlim` +
  `WaitAsync()`. Read-modify-write races: the whole read→calc→write is one atomic critical section.

**3.2 (register + login):**
- **Interface placement = the contract's vocabulary, not the impl's dependencies.** A multi-step,
  variable *workflow* → Application; a core entity *invariant* → Domain. `IAuthService` is Application
  because its contract touches only App/Domain types, even though the impl needs Infra.
- **Abstraction-seam integrity is a compile-time type question.** Leaking `IdentityResult` would couple
  Application to `Microsoft.AspNetCore.Identity`; the wrapper (now `OperationResult`) is the seam. The
  leak is the *type* crossing the boundary, not any runtime value.
- **Concurrency guarantee = index-level constraint serialization, NOT transaction isolation.** Isolation
  governs read snapshots; a unique index makes the 2nd concurrent inserter block on the 1st's pending
  entry, then inherit its outcome — at *every* isolation level. *(Corrected from an isolation-based
  mis-explanation — failure-mode watch caught it.)*
- **DB constraint vs app pre-check do different jobs:** constraint = the guarantee (closes TOCTOU);
  app-check = fast-fail UX nicety, never the authority. "Both enforce" is right for the wrong reason
  until you name the two different jobs. *(Also a caught mis-justification.)*
- **Emergent uniqueness is fragile.** Email uniqueness held only via `UserName==Email` + the username
  unique index. `FindByEmailAsync` (EF store) assumes email unique (Single-style in the in-use version)
  → duplicates throw. Enforce at the index; don't rely on the coupling.
- **`SignInManager` establishes a signed-in state** (writes a ticket into a scheme) → ctor needs
  `IAuthenticationSchemeProvider`; wrong tool with no scheme. `UserManager.CheckPasswordAsync` is a
  scheme-free credential check.
- **`RoleExistsAsync` ≠ authorization.** "Does the role exist" is not "may this caller self-assign it."
  On a public endpoint, whitelist the self-registerable roles.
- **`UserManager` is not transaction-aware (confirmed in practice):** `CreateAsync`/`AddToRoleAsync`
  each `SaveChanges`; the ambient `DbContext` transaction is what makes them atomic (rollback verified).
- **Enumeration-safety verified:** identical generic response for wrong-password AND nonexistent-email;
  default-deny error allowlist stops Identity codes leaking account existence.
- **HTTP status is the sole outcome signal** — don't duplicate success/failure into the payload
  (two-sources-of-truth smell).
- **Process/pace:** twice over-deliberated low-stakes *reversible* calls (naming). Rule going forward:
  pick one, note the tradeoff in a line, move on.

**3.2 (post-PR-review refactor):**
- **Use-case logic belongs in Application — full stop.** If an orchestrator "needs" Infra types (Identity,
  EF) and that seems to force Infra placement, the conclusion is *invert the dependency with a port*, not
  relocate the logic down. `AuthService` moved Infra → Application behind `IUserIdentityService` /
  `IUnitOfWork`. **DIP boundary rule (confirmed):** a concrete dependency *within* a layer (`UserManager`
  inside `UserIdentityService`, `AppDbContext` inside `UnitOfWork`) is fine; a use-case depending on a
  concrete *across* the boundary is the violation.
- **⚠ Failure-mode log (mine, not just Yevhenii's):** the earlier sync **endorsed** the Infra placement
  and called the reasoning correct. That reasoning — "the impl uses Infra types, so it belongs in Infra" —
  took the concrete dependency as fixed instead of questioning whether a use-case should depend on
  concretes at all. Same "right-sounding-but-wrong-premise" trap; PR review caught what the audit didn't.
  (This is why summaries now sync only *after* PR approval.)
- **Ports are shaped by the caller, not the wrapped library.** `IUserIdentityService` exposes the three
  verbs `AuthService` needs; `IUnitOfWork` exposes a transaction scope, not `SaveChanges`. Don't mirror
  `UserManager`'s API surface into the port.
- **Return error *codes*, not messages, at the boundary** — stable, localizable, testable; default-deny
  mapping so unknown Identity codes never leak. Message text becomes a presentation concern (client /
  ProblemDetails).
- **Verify layering with tooling:** the project-reference graph proved Application never references
  Infrastructure — arrows, not vibes, confirm the refactor.
- **Watch for orphaned abstractions:** `IRoleService` shipped with no impl/caller. An interface with zero
  implementations fails the YAGNI-with-trigger test as surely as a premature one.

**3.3 (JWT access tokens):**

*JWT / crypto*
- **⚠️ CORRECTION to the sub-chat's own note:** an HS256 key ≥256 bits **is** a spec requirement, not
  merely best practice. RFC 7518 (JWA) §3.2 says a key at least the size of the hash output — 256 bits
  for HS256 — **MUST** be used, citing NIST SP 800-107 on effective security strength. The library
  minimum in `Microsoft.IdentityModel.Tokens` *enforces* the spec; it isn't the origin of the rule.
  (Right conclusion — use ≥256-bit keys — via a wrong premise.)
- Entropy comes from the **generation method**, not from bytes-vs-string: `openssl rand -base64 32` and
  a CSPRNG-picked 43-char string carry the same entropy. The byte path is preferred because it's harder
  to accidentally reach for `Random`/`Guid`/a typed passphrase.
- `Encoding.UTF8.GetBytes(base64)` ≠ `Convert.FromBase64String(base64)` — the first encodes *characters*,
  the second decodes to the original bytes. Mixing them silently destroys entropy.
- base64 vs base64url are two text encodings of the same bits. JWT mandates base64url for its three
  segments *because tokens travel in URLs/headers*; a signing key never travels, so plain base64 in
  config is correct.
- **Never let untrusted input decide the rules by which it's checked.** `TokenValidationParameters`
  trusts the token's own `alg` header unless `ValidAlgorithms` pins it (the `alg: none` family). Same
  shape as default-deny error mapping.
- `aud`/`iss` are inert with a single token purpose, but they guard **scope confusion**: a correctly
  signed token minted for purpose A must be rejectable at purpose B. Goes live in 3.4.
- **`AuthenticationHandler<T>` and `JsonWebTokenHandler` are unrelated hierarchies sharing a word.**
  `AddJwtBearer` registers the former (pipeline component); it does not pick the latter (token
  serialization). Separate packages, separate layers, separate jobs.

*ASP.NET Core / DI / Options*
- `IServiceCollection` lives in `…DependencyInjection.Abstractions` — a generic .NET abstraction, not
  ASP.NET Core. Per-layer DI extension methods therefore don't drag the web framework into Application.
- Composition root = `Program.cs` choosing **which modules**, not all registration in one file. API
  registering Infrastructure's concretes would be an OCP violation.
- `services.BuildServiceProvider()` inside a registration extension is an anti-pattern: a second
  container, duplicate singletons, nothing disposed, real registration-order coupling. Use
  `configuration.GetSection(...).Get<T>()` for eager reads, or **`AddOptions<T>(name).Configure<TDep>(...)`**
  for genuinely deferred resolution — which is what the JwtBearer wiring ended up using.
- **`IOptions<T>` is an open generic** — it always resolves, even with zero `Configure<T>` calls,
  returning a default-constructed instance. `GetRequiredService<IOptions<T>>()` will **not** throw on
  missing config; only `.Validate(...).ValidateOnStart()` catches that.
- `.Validate()` on `AddOptions<T>()` and implementing `IValidateOptions<T>` are two registrations of the
  **same** mechanism, not complementary layers.
- `ValidateOnStart()` registers rules; it doesn't run them at `AddOptions` time. Once it's proven to halt
  startup, downstream code may trust the config by construction — redundant guards were removed on that
  basis (after empirical confirmation).
- `UseAuthentication()` before `UseAuthorization()` is a **data dependency**, not a convention.
- `UseHttpsRedirection()` first is fail-fast/efficiency, **not** protection — the request line and headers
  (credentials included) have already arrived in plaintext before any middleware runs. TLS protects; the
  redirect only steers.
- 401 vs 403 is worth testing separately: unauthenticated vs authenticated-but-wrong-role.

*C# / nullability*
- `out`/`ref`/`in` parameters are **illegal on `async` methods** (hard language rule) — the `TryParse`
  pattern is unavailable for async APIs; return a result type instead.
- `where TValue : notnull` cannot stop `Success(null!)` — `!` is an explicit suppression no compile-time
  constraint overrides. Only a **runtime** check sees through it. The constraint still does real work:
  it rejects `OperationResult<int?>` at the instantiation site (verified).
- Nullable analysis is static-type-based and can't see runtime invariants across method boundaries. A `!`
  recurring at every call site signals the fix belongs on the **type**, not the consumers.
- `default!` on a property initializer silences nullable warnings **unconditionally**, including for reads
  that legitimately can be null. It doesn't make the value non-null; it makes the compiler stop asking.
- `IdentityUser<TKey>.Email` is `string?` (Identity supports username-only accounts). Invariants the
  compiler can't see should be made explicit (`?? throw` with an accurate message), not silenced.
- **Result-vs-exception is a channel question, not a style question** *(unresolved — see Q18)*. The Result
  pattern replaces exceptions for **expected domain outcomes**; whether it should also absorb **contract
  violations** (API misuse, the `ArgumentNullException` category used throughout the BCL) is the actual
  disagreement. "A Result type should never throw" is a slogan, not an argument, until that distinction
  is addressed.

*Process*
- **Ports are shaped by the caller's available data.** `VerifyCredentialsAsync(User, …)` was rejected not
  on layering grounds (Application→Domain is legal) but because `AuthService` only holds an email string
  at that point.
- YAGNI-with-trigger-condition applied twice successfully: `IUserRepository` and a hand-rolled validator
  were both dropped once the trigger was tested for and found absent. *(The `IUserRepository` trigger may
  finally fire in 3.4.)*
- `UserManager.CheckPasswordAsync` is hash-check only — **no lockout, no `AccessFailedCount`** (Q16).

**3.4 (refresh tokens):**

*Web / security*
- `HttpOnly` prevents *reading* a cookie, not *using* it — a same-origin `fetch` from an XSS payload still carries it.
- `SameSite=Lax` blocks cross-site POST; the **Lax+POST 2-minute grace applies only to cookies with no `SameSite` attribute**, so `Lax` must be set explicitly rather than assumed from framework defaults.
- **CORS is not a CSRF defense** — form submissions never preflight. **SOP** (not CORS) is what stops cross-origin JS reading cookies, which is why double-submit CSRF works at all.
- Cookie `Path` is a prefix match — a cookie scoped to one endpoint won't reach a sibling.
- Enumeration-resistant IDs don't substitute for authorization checks.
- The realistic damage from CSRF on a rotation endpoint isn't token theft (SOP blocks reading the response) — it's **attacker-triggered false-positive theft detection**, i.e. an on-demand logout DoS.

*Postgres / EF Core / .NET*
- **`CHECK` constraints structurally cannot reference other rows** — a **partial unique index** is the tool for conditional uniqueness. A row *leaves* the indexed set when it stops matching the filter, which is what makes rotate-then-insert conflict-free.
- Npgsql ≥6.0: `DateTime.Kind` must match the column type (`Utc` ↔ `timestamptz`), enforced **at write time**. `DateTime` carries a `Kind` *tag*; `DateTimeOffset` carries a real offset. No C# syntax constrains `Kind` at the type level — so the **column type** is the enforcement point.
- `bytea` indexes fine — preferring `string` here was a **legibility** decision, not a performance one.
- `ApplyConfigurationsFromAssembly` registers entities; no `DbSet<T>` property required.
- **EF graph behavior:** `Add()` on a root marks *everything reachable* as Added — attaching an **untracked** parent to a new child causes a duplicate-PK insert. Use the scalar FK.
- `ExecuteUpdateAsync` bypasses the change tracker, executes immediately, and enlists in an open transaction.
- Multiple `SaveChangesAsync` calls inside one transaction are safe — each flushes; only the transaction decides durability.
- **`ActionResult<T>` does not type-check the `Ok()` path** — `Ok(object)` returns a non-generic `ActionResult` that converts implicitly. Verified empirically; caused three separate bugs. *This is why the internal/wire DTO split is structural protection rather than style.*
- Tuples serialize as `item1`/`item2` — never an API contract.
- `UserManager`'s public API is key-type-erased to `string` regardless of the store's real key type.
- EF migration scaffolding doesn't detect renames semantically (emits drop + create); `migrations remove` requires the migration to be un-applied first.
- `Base64Url.TryDecodeFromChars` was **observed to throw** on padded standard-Base64 input rather than returning `false` — guard with `Base64Url.IsValid` first.
- Postgres row-level write locking is baseline MVCC at **every** isolation level; under `READ COMMITTED` a blocked `UPDATE` re-reads and re-evaluates its `WHERE` on unblock. That's what makes `UPDATE … WHERE id=@id AND revoked_at IS NULL` + rows-affected a free exclusivity primitive.

*Design principles established*
- **Merge on shared *cause*, not shared *shape*.** Two things that look alike today aren't duplication unless the reason keeps them alike. *(Correctly applied: `AllRoles` and `RolesAvailableForPublicRegistration` have identical contents today and were still kept separate.)*
- **Invariant vs. policy.** A branch with *zero* legitimate variation across callers belongs inside the shared method; one that genuinely differs belongs at the call site. Conflating them produced first a duplicated reaction, then a boolean-flag method.
- **Translation vs. decision.** Converting rows-affected into an enum is translation (repository); acting on its meaning is a decision (orchestrator).
- **A service wrapper earns its place** when there's a rule to enforce or more than one caller — *not* merely because more than one entity is touched.
- **Enforced vs. currently-true.** Prefer invariants the data layer guarantees over conventions application code must remember. *(The D4 ceiling-on-the-family argument is the cleanest instance: store it once so rotation code has no reason to touch it.)*
- **Redundant checks are worse than absent ones when the redundant one is staler** — the pre-check on `RevokedAt` was removed because the atomic write answers it more reliably.
- **Asymmetric failure cost decides centralization.** Forgetting a security step should be impossible; forgetting a return-value branch fails loudly and cheaply.
- **A named type beats a tuple** wherever the grouping is meaningful rather than incidental.
- **Interfaces are contracts, not implementations** — mechanism (`ExecuteUpdateAsync`) belongs in the implementation and its comment, never in the method name.
- **Defer input-size / rate-limit concerns to the layer that owns them** — a length check inside a hashing helper arrives long after the request body was materialized.
- **When the cost curve is flat, stop optimizing.** Token entropy had no two-sided tradeoff and therefore no minimal-sufficient value to find — establish you're far past the danger zone, pick a conventional number, move on. *(Contrast D4's TTLs, which do trade off.)*

---

## 7B. Claude audit-failure log

> Kept deliberately, so the *pattern* is visible rather than each miss being forgotten individually.
> Purpose: calibrate how much weight my "this reasoning is correct" is worth. It's a filter, not a proof.

**3.2 sync:** endorsed `AuthService` living in Infrastructure and called the reasoning sound. The premise
("the impl uses Infra types, so it belongs in Infra") took the concrete dependency as fixed instead of
asking whether a use-case should depend on concretes at all. Mentor's PR review caught it. → produced the
**sync-only-after-PR-approval** rule.

**3.3 sub-chat (reported by Yevhenii):**
1. Steered toward `services.BuildServiceProvider()` as a live option; it was implemented, then walked back
   the next turn. *Bad option set, not a bad fact* — the more dangerous kind, since each individual claim
   was defensible.
2. Suggested `IValidateOptions<T>` without first checking whether options validation was already wired —
   it was; the suggestion was pure duplication.
3. **Wrong empirical prediction:** claimed `CS8625` behavior would differ with `where TValue : notnull`
   present vs absent. Both were tested; warnings were identical. The correction came from Yevhenii's test,
   not from me.
4. Flagged unresolved decisions in self-reflect checkpoints without **gating** on them — accounting
   instead of action. Token lifetime nearly shipped as a silent `0`.
5. Did not request repo source until ~10 turns in, despite the extract instructing it up front.

**3.3 sync:** read the archive before auditing (fixes #5 at the orchestrator level). Doing so surfaced
three summary-vs-code drifts the summary alone would have enshrined — including a method the summary
reported as deleted that is still in the codebase. **Standing rule: audit against the archive, not the
summary.**

**3.4 sub-chat (reported by Yevhenii):**
1. **Argued for `Session` over `RefreshTokenFamily`**, justified by future device/IP metadata that was never going to be built. The mentor spotted the mismatch immediately. Cost a rename, a migration redo, and review time — and the concept was never questioned during design, *including when Yevhenii flagged his own uncertainty*.
2. **Reversed its own recommendation** on `HandleReuseAsync` placement, correcting only after pushback; the first answer conflated an invariant with policy because they shared a syntactic shape.
3. **Two gate failures** — locked decisions while its own questions were still open (`ITokenService` method placement; an entity spec with `Id`/`TokenHash` types unstated). Exactly the failure the 3.4.2 extract warned about *by example*.
4. Told Yevhenii to hex-encode `GenerateRefreshToken`'s output — that's the *hash's* encoding.
5. **Wrongly flagged `AuthErrorCodes.InvalidEmailOrUserName` as dead code after claiming to have audited the file** — it's the default-deny mapping target for `DuplicateUserName`/`DuplicateEmail`/`InvalidUserName`.
6. Recommended `WebEncoders.Base64UrlEncode` over the better BCL `System.Buffers.Text.Base64Url` (found by Yevhenii).
7. Only half-applied the extract's own error-code enumeration warning — caught the temporary debug codes, missed that the *permanent* set was equally distinguishable.
8. Weak BCL-vs-NuGet placement test; buried the value-converter option mid-list; let entropy sizing run three turns against an explicit timebox; never reviewed the `/refresh` controller after its signature changed.

**3.4 sync:** archive audit surfaced Q22 (non-idempotent family revocation → benign replay recorded as
theft, which becomes a real DoS once `HandleReuseAsync` is implemented), Q23, Q24, Q25, Q26, Q27 — none
of which appear in the summary. Confirms the archive-over-summary rule is load-bearing, not ceremony.

**Meta-observation (Yevhenii's, worth preserving):** *external review caught three things internal review
didn't* — the `Session`/`TokenFamily` conceptual mismatch, the SRP violation, and the DTO naming
confusion; **two were actively endorsed by Claude.** Extended dialogue converges on *internally
consistent* designs, and internal consistency is not correctness. A fresh reader catches framing errors
neither participant can see from inside. **Practical rule adopted:** get a mentor read on
conceptual/naming decisions *before* entities and migrations are built on them.

---

## 7C. Yevhenii's tracked failure modes — Phase 3 close-out

- **"Right conclusion, wrong stated mechanism" (9 instances in 3.4):** JWT key sizing · CORS-vs-SOP · "TOCTOU" (×2) · `bytea` indexing · retry loops · "stack overflow" · Guid PKs · **refresh-token lifetime**. Stated precisely: *when a correct answer arrives quickly, the first stated reason is usually not the load-bearing one.* The conclusions were mostly right; the confidence attached to the first explanation wasn't. **This one reached the mentor as an argument for removing a security control** (claiming a stolen refresh token "expires in 15 minutes" — that's the *access* token; a rotated stolen refresh token yields a fresh 3-day token rideable to the 14-day ceiling). Highest-stakes instance so far.
- **"Decided-then-drifted" (12 instances, dominant in the second half):** `LoginResponse` leak · missing cookie `Path` · `[HttpGet]` on `/refresh` · `[Authorize]` on `/refresh` · missing `CommitAsync` (×2) · error codes · `LogoutAsync` never committing · bare-string `Ok()` · `LoginAsync` transaction scope · seeding from the wrong role set. Every one was decided **correctly**, then not carried into code written later — almost always during a refactor. **This is the cost of a long design session: the decision log outgrew working memory.** A written constraints checklist was proposed twice and never built — **highest-leverage unaddressed fix; carry it into Phase 4.**
- **Local pattern-transplant:** copying `const string` from `Roles.cs` without checking that those are strings *only because Identity's API forces it*. More dangerous than cross-technology transplant, because in-codebase precedent **feels** like evidence.
- **Reaching for machinery that sounds responsible:** Guid PKs, retry loops, three new `User` columns for forced password change (Identity's `SecurityStamp` already does it). Each dissolved on *"what specific threat or failure does this address?"*

---

## 8. Update protocol

At the end of each sub-chat: (1) flip its roadmap status, (2) move resolved items out of Open
Questions, (3) append to the learnings log, (4) refresh the file inventory if files changed, (5) rewrite
the "current frontier" pointer, (6) **regenerate the launch extract from this doc** and paste *that*
(not the master) into the next sub-chat.

**Code's source of truth is the repo**, not these docs — master carries decisions/learnings/inventory,
the extract carries the signatures the next sub-chat needs. If a summary omits code, the extract
references files by signature and the user pastes current source into the sub-chat.

**Master vs extract when syncing:** master first (it's the quality gate — scrutinize the summary's
reasoning before enshrining it), then derive the extract. Never the reverse.

**Sync only after PR approval.** A sub-chat summary is provisional until its PR is reviewed/merged —
mentor comments can move architecture (e.g. 3.2's `AuthService` relocation). Don't enshrine a summary
in master until the PR lands, or the docs drift from the repo and carry reasoning the review overturned.

**Audit against the archive, not the summary.** When a solution zip is attached, read the code *before*
auditing. 3.3's and 3.4's summaries each drifted from their own codebase in several places; only reading
the source caught it. **Specifically check for orphans** — "deleted" has meant "removed from the
interface" three sub-chats running.

---

## 9. Phase-3 close-out status

**This doc is now an archive.** Its durable content is rolled up into the program-level BookingApp
master, which takes ownership of:
- the **cross-phase debt ledger** — §6's open items migrate and are re-prefixed `P3-Q…`; the table above
  stays as the historical record but is **no longer the live ledger**;
- the Phase-4 frontier (§5) that seeds Phase 4's own dedicated chat.

**Stays here** (pointer only from the program master): per-sub-chat decision tables (§3–§3D), the full
learnings log (§7), verification detail, and the audit-failure log (§7B/§7C). Recoverable when needed,
noise when not.

**The launch extract is frozen, not deleted** — it's the working template for future phase boarding
passes and evidence of the process if this becomes portfolio material.
