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
Run `/import-history 7` in a fresh session. This file's Goal + In scope are the lens
it filters against; it does not read this file directly — CLAUDE.md's `@`-import is
what puts it in context.

## Open questions
- decide the "shape" of importing tool - console app, a library, or something else
- decide about if we need to introduce some new indexes/constraints within the scope of this phase
- decide how provenance of imported users is recorded - column on AspNetUsers table, a side table, "something else"
- decide how we can persist current state of import-process and state of upsert operations
to ensure idempotency
- decide the transaction boundary. `UserManager.CreateAsync` and `AddToRoleAsync` each write
immediately, so `IUnitOfWork` does not span Identity writes: what happens to hosts already
created when a later record in the same import fails?

## Predictions outstanding
- none outstanding (no build/test/run command was executed in the scoping session)
