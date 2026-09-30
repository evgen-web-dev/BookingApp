# Phase 7 — Data importer

## Goal
We need to implement data-importer of JSON data that is meant to import data about hosts and their apartments.
Those "new hosts and apartments" are treated as new data, from third-party booking company we are "acquiring".

## In scope
- implement import of data about hosts and apartments - with using Apartment entity, repositories, 
IUnitOfWork, Identity user creation with ApartmentsWithBookingsSeeder.cs as an example / reference for hosts can be created at the current state of the project
- ensure our import flow is idempotent
- provenance of imported users is recorded 


## Out of scope (say so if I ask for these)
- do not import data about bookings of new hosts/apartments
- do not implement dedicated User-related `Host` entity
- do not implement force-credentials-rotation for newly-imported host-users

## Debt this phase owns
Pending — filled after `/import-history 7`. Known already:
- `P5-01` — must honour, not fix: any new DTO for the JSON payload uses `required`
  init members, per `C-07`.


## Done when
- [ ] <observable condition> — evidence: <test / HTTP response / DB state / console behavior>
- [ ] <observable condition> — evidence: <...>

## Current step
`/import-history 7` is done (2026-09-30) — its report is
`docs/learning/reviews/archive-import-review.md`; 22 decision candidates were walked and
accepted, `P6-03` was moved into debt.md, `P7-02`/`P7-03` were added.
Next: **I fill in Done-when and the rest of Debt this phase owns** in this file, informed
by §1 of that report (the four findings that bear on "done": §1.1 `required` DTOs,
§1.6 transaction shape, §1.7 the seeder's two idempotency mechanisms, §1.10 the
role-whitelist gap). Then commit, then `/plan-review` in a fresh session — fresh so the
critique meets the draft cold.

## Open questions
- decide the "shape" of importing tool - console app, a library, or something else
- decide about if we need to introduce some new indexes/constraints within the scope of this phase
- decide how provenance of imported users is recorded - column on AspNetUsers table, a side table, "something else"
- decide how we can persist current state of import-process and state of upsert operations
to ensure idempotency
- decide the transaction boundary. `UserManager.CreateAsync` and `AddToRoleAsync` each write
immediately, but through the same scoped `DbContext`, so an open `IUnitOfWork` transaction
*does* span them (`D3-03` — unverified under this harness, worth forcing): what happens to
hosts already created when a later record in the same import fails?

## Predictions outstanding
- none outstanding (no build/test/run command was executed in the scoping session)
