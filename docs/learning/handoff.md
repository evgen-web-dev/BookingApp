# Handoff

Updated: 2026-09-30 — written only during a sync I start.

## Where we stopped
**`/import-history 7` is complete**, in a session of its own, plus this sync. The run read
the whole archive, audited it against source, and produced
`docs/learning/reviews/archive-import-review.md`: **14 contradictions** between archive and
code, a classification of the rest, a 22-candidate shortlist, and the walk outcomes. The
debt ledger was moved, and all 22 candidates were walked one at a time.

`current-phase.md` is still **partially filled** — Done-when is the template, and Debt this
phase owns names only `P5-01`. That is the next piece of work and it is mine to write.

No production or test code was written. No `dotnet build`, `dotnet test`, `dotnet run`, curl
or psql command was executed, so **nothing in this session rests on runtime evidence** — it
is archive reading, source inspection and document work only.

Working tree at sync time: the import's own output is committed as `114d97b`
(`decisions.md`, `debt.md`, `reviews/archive-import-review.md`). This sync then modified
`current-phase.md`, `handoff.md`, `debt.md` and one line of `master.md`; not yet committed.

## Next action
1. **Fill in `current-phase.md`** — Done-when, and the rest of Debt this phase owns. §1 of
   the review is the input; four findings bear directly on what "done" means:
   §1.1 (`required` DTOs were never adopted), §1.6 (the create-booking transaction shape is
   not what the docs say), §1.7 (the seeder has two idempotency mechanisms, not one),
   §1.10 (the role whitelist is not enforced where the archive says it is).
2. **Commit.** `current-phase.md` is `@`-imported, so the finished plan travels by itself —
   it needs no handoff entry to reach the next session.
3. **`/plan-review` in a fresh session.** Fresh on purpose: a reviewer that co-authored the
   draft is worth less than one meeting it cold. That is this project's own calibration
   finding, not a procedural preference.

## Decisions locked this session
22 candidates walked, **22 accepted, 0 rejected** — so no entry carries a `REJECTED` marker
and nothing was deleted from `decisions.md`.

- **Amended live entries (3):** `C-04` (boundary test added — a concrete dependency *within*
  a layer is fine, across the boundary is the violation; revision site corrected Phase 0 →
  3.2), `C-06` (tool line — `CHECK` can't reference other rows, so a partial unique index is
  the tool for conditional uniqueness), `C-08` (`Why` rewritten: the old mechanism did not
  survive reading `AsyncValidationFilter.cs:31-39` — the same DTO instance reaches the
  action, so a mutation would persist rather than be discarded).
- **New in §Pre-harness (13):** `D3-01`…`D3-08`, `D4-01`, `D5-01`, `D6-01`, `D6-02`, `D6-03`.
- **Accepted unchanged (6):** `C-01`, `C-02`, `C-03`, `C-05`, `C-07`, §Standing checks
  *ID exposure*.
- Shaped during the walk rather than accepted as proposed: `D3-07` gained the
  service-wrapper corollary; `D5-01` was **narrowed** from eight test conventions to the two
  that decide whether a test asserts anything; `D6-01`'s live violation was split out as
  debt (`P7-03`) instead of being recorded inside a decision, per CLAUDE.md's "don't convert
  one into the other"; `D6-02`'s `Revisit if` was supplied by me, the archive having none.
- `D6-02` and `D6-03` were recorded in the archive as **substrate facts**, not decisions.
  Promoting them was a deliberate call, noted in both entries.

Corrections made during this sync, each resolved by me before writing:
- `current-phase.md`'s transaction-boundary open question carried a **false premise** — it
  said `IUnitOfWork` does not span Identity writes. `D3-03` says it does. Premise removed,
  question kept.
- `P4-03`'s premise corrected now that the import's verbatim diff has run (the handoff's
  standing "don't touch it yet" instruction has expired). Its **conclusion is unchanged**.

## Open questions carried forward
The five in `current-phase.md` §Open questions are the live ones — importer shape, new
indexes or constraints, provenance mechanism, idempotency state, and the transaction
boundary (now reworded). Not duplicated here; that file is `@`-imported and this one is not.

New from the review, §2.4:
- **How `AddToRoleAsync` fails on a role that doesn't exist** — an exception from the user
  store, or a failed `IdentityResult`? Filed *verify-first*, following the `ObjectResult`
  precedent that became `P6-FIX-200`. Decides whether role assignment belongs inside
  per-record error handling or in a pre-flight check. Deliberately not in `debt.md` (nothing
  known broken) and not in `decisions.md` (nothing chosen).
- **Two un-IDed debt items with no home:** the `Microsoft.NET.Test.Sdk` 17.4.1 → 17.14.1
  bump, and validator relocation (validators live in the API layer but validate Application
  DTOs, forcing the test project to reference the API). Both need IDs from me to be tracked.

Still open and not owned by `current-phase.md`:
- Whether `C-06`'s `Verified by:` should be able to record *partial* verification. Carried
  unchanged — this session added more half-proved-from-source claims, so the question is
  now load-bearing rather than theoretical.
- Whether debt surfaced outside a phase needs its own ID space. **Partially answered in
  practice:** `P7-02`/`P7-03` were numbered by the current phase even though Phase 7 hasn't
  started building, and `P7-01` was left unassigned for Phase 7's own first finding.
- Carried unchanged: a devcontainer rebuild recreates the `app` container and so ends the
  session running inside it. Sync before rebuilding, never after.

## Predictions and outcomes
**None.** No command requiring a prediction was run this session. Every claim below is
source-read or reasoning, and is labelled as such.

## Still unverified
- **Every entry in `decisions.md`.** All 8 constraints and all 13 pre-harness decisions read
  `Verified by: not yet`. No command ran this session, so none could be closed.
- **`C-06`, unchanged from the last handoff.** The partial unique index is real in migration
  *source* (`Migrations/20260814180923_…cs:17-22`) and in the EF config
  (`UserEntityTypeConfiguration.cs:14-16`), but that is not live schema, and the
  `date`-columns half is unchecked. Cheapest close:
  `psql -h postgres -U booking_app_user -d booking_app -c '\d "AspNetUsers"'`, same on
  `Bookings` for the column types.
- **`D3-03`** — that an open `IUnitOfWork` transaction really does span
  `UserManager.CreateAsync`. Phase 3 claims a forced-rollback check, pre-harness. Cheapest
  close: begin a transaction, create a user, roll back, inspect `AspNetUsers`. **Highest
  value to force in Phase 7** — the phase's transaction boundary rests on it.
- **`D6-02`'s index-exclusion reasoning** — that an EF-graph insert leaves `NormalizedEmail`
  null, putting the row outside the *filtered* unique index and so outside `C-06`'s
  guarantee entirely, not merely leaving it unhashed. Inference from the filter expression
  plus where normalization happens. Forcing it also exercises `C-06`.
- **`D6-03`'s failure mode** — see Open questions.
- **`P4-03`'s conclusion.** Its premise is now corrected, but "the rows are dead" still
  rests on Identity's `RequireUniqueEmail` defaulting to `false`, which I did not verify —
  there is no such setting anywhere in `src/`.
- **`P7-02`'s rationale** — that `init` binds fine from `[FromQuery]`. The evidence is that
  `PaginatedRequest`'s own `init` properties bind today on that exact type
  (`ApartmentsController.cs:24`); the mechanism (init is a compiler restriction, not a CLR
  one) was reasoned, not forced.
- **Carried unchanged, still unmeasured:** the API port (`5184`, deduced from
  `launchSettings.json`; app not run since), host access to Scalar (loopback bind +
  devcontainer forwarding, mechanism reasoning only), and whether the two compose stacks
  have separate databases (both declare an unqualified `booking_app_pgdata`; no docker CLI
  in this container).
