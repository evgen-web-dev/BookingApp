# Master plan — Booking App (.NET learning project)

## Why this project
Build a booking system resembling Airbnb — users, JWT auth, roles, then booking
domain. A GitHub portfolio project at real-world quality, not a toy; the first
part of a larger app. *(source: §Project Goal)*

## What I'm bringing in from earlier work
Junior/trainee transitioning into .NET backend, from JavaScript, PHP, WordPress
and some Laravel. Anything I claim to know from that background gets harder
treatment, not gentler. *(source: §Context for Claude)*

## Phase roadmap
0. **Orientation** ✅ — Clean Architecture as a concept, no code.
1. **Project scaffold** ✅ — four layers, one request end to end.
2. **Data layer** ✅ — EF Core + PostgreSQL, migrations, User entity.
3. **Auth** ✅ — register, login, JWT access tokens, refresh rotation, roles.
4. **Hardening** ✅ — RFC 7807 contract, global exception handler, FluentValidation.
5. **Tests** ✅ — xUnit + Shouldly + Moq, 27 unit tests.
6. **Booking core** ✅ — Apartment/Booking, availability, pagination, ownership checks.
7. **Scoped, plan not yet committed** — JSON data importer: hosts and their apartments
   from a third-party booking company. Goal, In scope and Out of scope drafted in
   current-phase.md; `/import-history 7` done (2026-09-30), Done-when and Debt-this-phase-owns
   still pending, then `/plan-review`.
   *(Not one of the Phase 6 close-out candidates — that list was set aside.)*

## Calibration — why external review is a gate and internal review is a filter
Extended dialogue converges on *internally consistent* designs, and internal
consistency is not correctness. The mentor caught three framing errors in Phase 3
— a conceptual naming mismatch, an SRP violation, and a DTO naming confusion —
**two of which Claude had actively endorsed.** Across Phase 3 Claude also
endorsed a wrong architectural placement, argued for a wrong entity name, made a
wrong empirical prediction, and locked decisions while its own questions were
open. Treat its audits as a filter, not a gate.

## Process that earned its place
- **Audit against source, not the summary.** Every sync where this was done
  surfaced drift the prose alone would have hidden — including two sub-chat
  summaries in Phase 4 that had drifted from their own code.
- **Don't stack non-linearity.** One phase in flight at a time.

## What "understood" means here
I can explain it without notes, predict what breaks if I change it, and name the
alternative I rejected.