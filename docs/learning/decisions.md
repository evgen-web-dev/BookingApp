# Decisions

The live rulebook: everything that binds work in flight, which is why this file
is @-imported. master.md is orientation — read at phase boundaries, not here.

## Standing constraints (C-xx)
Rules that bind choices not yet made. Test: would violating this in a future
phase be a bug, or merely a different design? Only a bug belongs here. A decision
is promoted to a constraint when a SECOND phase has to honour it.
Every constraint carries `Verified by:` — "not yet" until a command proved it
under this harness. Lifted from a pre-harness doc counts as "not yet".

- **C-01 One failure literal per auth flow.** Login emits only
  `InvalidEmailOrPassword`; refresh/logout only `InvalidRefreshToken`.
  - Why: enumeration safety is an invariant, not a default. The paths *forward*
    errors rather than hardcoding one, so a second distinct literal reopens the
    leak silently, and the Identity error mapper won't catch it — it filters
    Identity codes, not ours. This is why `P3-06` lockout is deferred.
  - Verified by: not yet

- **C-02 `500` means exactly "nothing caught or classified it."**
  - Why: every expected failure is a deliberately-returned `Failure(...)`. An
    uncaught 500 is a bug, never a control-flow branch.
  - Verified by: not yet

- **C-03 Error-placement discriminator.** A check is field-validation
  (`ValidationProblemDetails`) iff it is resolvable from the payload alone, needs
  no I/O, and naming the field leaks nothing. Otherwise it is a domain code
  (`ProblemDetails`).
  - Why: classification follows *where the check runs*, not what it feels like.
  - Verified by: not yet

- **C-04 An interface is owned by the layer that depends on it**, not the layer
  that implements it.
  - Why: revised in 3.2 after getting it backwards there. If a use-case
    seems to require an Infrastructure type, invert with a port — don't relocate
    the use-case.
  - Boundary test: a concrete dependency *within* a layer is fine; a use-case
    depending on a concrete *across* the boundary is the violation.
  - Verified by: not yet

- **C-05 Adapters vs orchestration decides the sub-layer.**
  `Infrastructure/Services` wraps concrete external technology;
  `Application/Services` composes ports with zero concrete framework dependency.
  - Why: this is the rule that relocated `TokenFamilyService` in Phase 4 — a
    service with no infrastructure dependency belongs in Application.
  - Verified by: not yet

- **C-06 An invariant the database can enforce is enforced there.** If a rule can
  be expressed as a constraint, index, or column type, application-level checking
  is belt-and-suspenders on top of it, never the mechanism.
  - Why: proven twice — the partial unique index on `NormalizedEmail` protects
    every `Single*` lookup, and `date` columns are the real date-only guarantee
    (`.Date` at the seam is belt-and-suspenders, not the mechanism).
  - Tool: a `CHECK` constraint structurally cannot reference other rows, so a
    partial unique index is the tool for conditional uniqueness.
  - Verified by: not yet

- **C-07 New DTOs use `required` init members.**
  - Why: init-only properties gave test-data ergonomics but cost the
    compile-time non-null guarantee positional records had (`= null!`
    suppression). `required` gets both. Open as `P5-01`.
  - Verified by: not yet

- **C-08 Validators reject; services transform.**
  - Why: a validator's contract is pass/fail — the filter branches on `.IsValid`
    alone and FluentValidation gives rules no sanctioned write-back. Init-only
    DTOs make in-validator normalization impossible; on a DTO with settable
    props it would instead persist as an invisible side effect. Either way,
    transformation belongs in the service or in construction.
  - Verified by: not yet

## Standing checks (re-run per change, never "closed")
A constraint is a rule to honour; a standing check is a question to re-ask.
Record the trigger, not the current answer.
- **Enumeration safety.** See C-01. Trigger: any second auth-failure literal.
- **ID exposure.** `int` PKs are fine *while* every client-exposed ID is
  independently authorized. Carry the check, not the answer — re-run "is this ID
  client-exposed, and is access independently authorized?" per new entity.
- **Orphan check.** Before recording anything as deleted, grep for references.
  "Deleted" has meant "removed from the interface" before.
- **Enum serialization.** `JsonStringEnumConverter` is deliberately unregistered
  — no enum currently crosses the wire. Trigger: the first enum in a response
  DTO, which `System.Text.Json` would serialize as an integer.

## Pre-harness decisions (imported from archive)
From phases that predate this harness, extracted only where still load-bearing.
Each entry names the archive file it came from. "Verified by" is "not yet" unless
re-proved under this harness — the original docs had no code access and cannot
count as evidence.

- **D3-01 Ports are shaped by the caller's available data**, never mirrored from
  the wrapped library's API surface. *(source: archive/phase-3.md, Phase 3)*
  - Why: a port that demands what the caller doesn't hold forces the caller to
    fetch it first; a port that returns less than the caller needs forces a
    second call. `AuthenticateAsync(email, password)` returns id + email + roles
    from one `FindByEmailAsync` reused across the password check and the role
    fetch — the mirrored split would have looked the same user up twice.
  - Rejected: mirroring `UserManager`'s surface into the port; and
    `VerifyCredentialsAsync(User, …)` — rejected not on layering grounds
    (Application → Domain is legal) but because `AuthService` holds only an
    email string at that point.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D3-02 `IUnitOfWork` is a transaction scope, not a save path.** `Begin` /
  `Commit` / `Rollback` only; `CommitAsync` calls `SaveChangesAsync` itself and
  throws if no transaction is open. Repositories track, never save.
  *(source: archive/phase-3.md, Phase 3)*
  - Why: nothing legitimately needs to save without committing, so exposing both
    is a foot-gun. Folding the save into commit means a write from Application
    cannot escape a transaction boundary by omission.
  - Rejected: a `SaveChangesAsync` member on the interface (added, then removed
    in PR review); and an `isCommitted` bool, replaced by `HasActiveTransaction`
    so correctness doesn't depend on every branch remembering to set a flag.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D3-03 Identity writes self-save; the ambient transaction is what makes them
  atomic.** `UserManager.CreateAsync` / `AddToRoleAsync` each call
  `SaveChangesAsync` internally, but through the same scoped `DbContext`, so an
  open `IUnitOfWork` transaction spans them and a rollback undoes them.
  *(source: archive/phase-3.md, Phase 3)*
  - Why: Identity exposes no deferred-write API, so multi-step user setup can
    only be made atomic from outside it. `RegisterAsync`'s role-failure branch
    rolls back an already-created user and is correct only because of this.
  - Rejected: not recorded — presented as a discovered mechanism, not a choice.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet under this harness. Phase 3 reports a forced-rollback
    runtime check; that predates this harness and doesn't count here.

- **D3-04 Error codes, not messages, cross the Application boundary — through a
  default-deny allowlist.** Identity errors are translated by per-operation
  scoped maps; a code in no map is dropped, never forwarded.
  *(source: archive/phase-3.md, Phase 3)*
  - Why: codes are stable, localizable and testable, and message text is a
    presentation concern. Default-deny means a future Identity code cannot leak
    by accident — the same shape as pinning JWT algorithms.
  - Rejected: forwarding Identity's own errors (allow-by-default) — restatement
    by the import; the archive records the leak it prevents, not the option.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D3-05 Any UTC `DateTime` column is declared `timestamp with time zone`
  explicitly.** *(source: archive/phase-3.md, Phase 3)*
  - Why: `Kind` is a tag, not part of the type, so no C# construct constrains it.
    Npgsql's default mapping is `timestamp without time zone`, which throws at
    write time on a `Kind=Utc` value — the column type is the enforcement point
    that makes a `DateTime.Now` slip loud instead of silently wrong.
  - Rejected: not recorded — `DateTimeOffset` is contrasted, not weighed.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D3-06 Merge on shared *cause*, not shared *shape*.** Two values identical
  today are duplication only if the reason keeps them identical.
  *(source: archive/phase-3.md, Phase 3)*
  - Why: merging on appearance creates a coupling that a later change discovers
    by breaking an unrelated caller. `AllRoles` and
    `RolesAvailableForPublicRegistration` hold the same two strings and have
    disjoint consumers — the seeder and the register validator.
  - Rejected: collapsing those two sets into one, on the grounds that their
    contents match.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D3-07 Name the trigger before building the abstraction; name the failure
  before adding the machinery.** An abstraction needs a condition that would
  justify it, tested for now; machinery needs a specific threat or failure it
  addresses. *(source: archive/phase-3.md + master-original.md, Phase 3)*
  - Why: "we might need it" is unfalsifiable. An interface with zero
    implementations fails this as surely as a premature one.
  - Rejected: Guid PKs, retry loops, three `User` columns for forced password
    change (Identity's `SecurityStamp` already does it), `IUserRepository`, a
    hand-rolled validator, `IRoleService` — each dissolved on the question.
  - Corollary: a service wrapper earns its place when there's a rule to enforce
    or more than one caller — not merely because it touches more than one entity.
  - Revisit if: not recorded — meta-rule.
  - Verified by: not yet

- **D3-08 Store a fact once; don't copy it where it can drift.** If a value
  belongs to a parent, reach it through the relationship rather than duplicating
  it on the child. *(source: archive/phase-3.md, Phase 3)*
  - Why: a copy makes the invariant depend on future code remembering to copy
    rather than recompute. `TokenFamily.AbsoluteExpiresAt` is stored once, so
    rotation code has no reason to touch it; one stray `AddDays(14)` on a
    per-token copy would silently restore unbounded sliding with nothing failing.
  - Rejected: copying the absolute ceiling onto each rotated token; a `UserId`
    column on `RefreshToken` (3NF — transitively dependent on `TokenFamilyId`,
    letting one fact live in two rows that can disagree).
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D4-01 One RFC 7807 error surface, in two variants.**
  `ValidationProblemDetails` (400, field-keyed) for input failures;
  plain `ProblemDetails` + an `errorDetails` code array for domain failures.
  Status comes from the first code; an empty code list collapses to
  `UnexpectedError`/500. *(source: archive/master-original.md + phase-4.md, Phase 4)*
  - Why: the validation variant matches `[ApiController]`'s own auto-generated
    shape, so a client sees one contract whether the framework or a validator
    rejected the request. Domain failures have no field to name, so they carry
    codes instead.
  - Rejected: the bespoke `ErrorResponse` envelope Phase 4 replaced (`P3-03`).
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D5-01 A test must be able to fail on a real defect.** Prove it by mutation
  probe — break the code under test, confirm red, revert. Match mock arguments
  exactly for values the SUT passes through unchanged; `It.IsAny<T>()` only for
  values it constructs or transforms. Where `MockBehavior.Strict` is off, a
  "must not be called" guarantee exists only as an explicit
  `Verify(..., Times.Never)`. *(source: archive/phase-5.md, Phase 5)*
  - Why: a test that passes regardless of a plausible defect is ceremony. Loose
    mocks return `default` on an unexpected call instead of throwing, so the
    safety net is absent unless written out (`P5-05`).
  - Rejected: not recorded.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D6-01 Make the unsafe construction unavailable, not discouraged.** When two
  same-typed values describe one concept, collapse them into a single
  already-correct value and make the safe construction path the only door in.
  *(source: archive/phase-6.md, Phase 6)*
  - Why: naming didn't prevent the bug class — it recurred ~4 times in 6.3 —
    the compiler did. Techniques used: clamp in the accessor (no adjacent raw
    value), `required` for named-initializer construction, private ctor +
    factory so a response can't claim a page size the query didn't apply.
  - Rejected: not recorded as options; the archive records the bug class.
  - Known limit: a query-bindable value must stay publicly readable, so the raw
    `PaginatedRequest.PageSize` can always be read directly; the discipline can
    only narrow where it's read, not forbid it. Current violation: `P7-03`.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet

- **D6-02 Users are created through the Identity path; plain entities go through
  the `DbContext`.** `UserManager.CreateAsync` / `IUserIdentityService.CreateAsync`
  + `AddToRoleAsync` for users; direct `Add`/`AddRange` for apartments and
  bookings. *(source: archive/phase-6.md, Phase 6 — recorded there as a substrate
  fact, promoted to a decision by the 2026-09-30 import)*
  - Why: `NormalizedEmail`, `NormalizedUserName`, `PasswordHash`,
    `SecurityStamp` and `ConcurrencyStamp` are populated by `UserManager`, not by
    EF or the database. An EF-graph insert leaves `NormalizedEmail` null, which
    puts the row *outside* the filtered unique index
    (`WHERE "NormalizedEmail" IS NOT NULL`) — so it loses the `C-06`
    email-uniqueness guarantee and is invisible to `FindByEmailAsync`, not just
    unhashed. (Index-exclusion reasoning: unverified, worth forcing.)
  - Rejected: not recorded — EF-graph insertion was never weighed.
  - Revisit if: the `NormalizedEmail` unique-index filter changes, or if user
    creation stops going through ASP.NET Core Identity.
  - Verified by: not yet

- **D6-03 Role seeding runs before anything that assigns a role.**
  `IdentitySeeder.SeedRolesAsync` is unconditional and runs first; data seeding
  runs after it, and only in Development. *(source: archive/phase-6.md, Phase 6 —
  recorded there as a substrate fact, promoted to a decision by the 2026-09-30
  import)*
  - Why: a role is a row in `AspNetRoles`, not a code constant, so
    `AddToRoleAsync` has nothing to assign until it exists. Roles are part of the
    schema's meaning and so are seeded in every environment; sample data is not.
  - Rejected: not recorded.
  - Revisit if: not recorded, you'll need to supply it.
  - Verified by: not yet


## Phase <N> decisions
<!-- ID format: D<phase>-<nn>, no spaces, e.g. D7-01. Constraints are C-01, C-02.
     /recall and /reflect locate entries by these IDs, so they must be greppable
     and must never be renumbered once written. -->
- **D<N>-01** <decision>
  - Why: <mechanism, not preference>
  - Rejected: <alternative>, because <reason>
  - Revisit if: <condition that would reopen it>
  - Verified by: <test / HTTP probe / DB state / console behavior / not yet>