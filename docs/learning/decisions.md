# Decisions

The live rulebook: everything that binds work in flight, which is why this file
is @-imported. master.md is orientation — read at phase boundaries, not here.

## Standing constraints (C-xx)
Rules that bind choices not yet made. Test: would violating this in a future
phase be a bug, or merely a different design? Only a bug belongs here. A decision
is promoted to a constraint when a SECOND phase has to honour it.

- **C-01 One failure literal per auth flow.** Login emits only
  `InvalidEmailOrPassword`; refresh/logout only `InvalidRefreshToken`.
  - Why: enumeration safety is an invariant, not a default. The paths *forward*
    errors rather than hardcoding one, so a second distinct literal reopens the
    leak silently, and the Identity error mapper won't catch it — it filters
    Identity codes, not ours. This is why `P3-06` lockout is deferred.

- **C-02 `500` means exactly "nothing caught or classified it."**
  - Why: every expected failure is a deliberately-returned `Failure(...)`. An
    uncaught 500 is a bug, never a control-flow branch.

- **C-03 Error-placement discriminator.** A check is field-validation
  (`ValidationProblemDetails`) iff it is resolvable from the payload alone, needs
  no I/O, and naming the field leaks nothing. Otherwise it is a domain code
  (`ProblemDetails`).
  - Why: classification follows *where the check runs*, not what it feels like.

- **C-04 An interface is owned by the layer that depends on it**, not the layer
  that implements it.
  - Why: revised in Phase 3 after getting it backwards in Phase 0. If a use-case
    seems to require an Infrastructure type, invert with a port — don't relocate
    the use-case.

- **C-05 Adapters vs orchestration decides the sub-layer.**
  `Infrastructure/Services` wraps concrete external technology;
  `Application/Services` composes ports with zero concrete framework dependency.
  - Why: this is the rule that relocated `TokenFamilyService` in Phase 4 — a
    service with no infrastructure dependency belongs in Application.

- **C-06 An invariant the database can enforce is enforced there.** If a rule can
  be expressed as a constraint, index, or column type, application-level checking
  is belt-and-suspenders on top of it, never the mechanism.
  - Why: proven twice — the partial unique index on `NormalizedEmail` protects
    every `Single*` lookup, and `date` columns are the real date-only guarantee
    (`.Date` at the seam is belt-and-suspenders, not the mechanism).

- **C-07 New DTOs use `required` init members.**
  - Why: init-only properties gave test-data ergonomics but cost the
    compile-time non-null guarantee positional records had (`= null!`
    suppression). `required` gets both. Open as `P5-01`.

- **C-08 Validators reject; services transform.**
  - Why: the validation filter reads only `.IsValid` and never re-reads a
    mutated DTO, so normalization in a validator is silently discarded.

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


## Phase <N> decisions
<!-- ID format: D<phase>-<nn>, no spaces, e.g. D7-01. Constraints are C-01, C-02.
     /recall and /reflect locate entries by these IDs, so they must be greppable
     and must never be renumbered once written. -->
- **D<N>-01** <decision>
  - Why: <mechanism, not preference>
  - Rejected: <alternative>, because <reason>
  - Revisit if: <condition that would reopen it>
  - Verified by: <test / HTTP probe / DB state / console behavior / not yet>