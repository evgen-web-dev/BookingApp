# Booking App (.NET learning project)

Airbnb-style booking system — auth substrate plus booking domain. Portfolio project.

## Per-project facts (I fill these in when starting a project)
- Stack: C#, .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, Clean Architecture,
  xUnit + Shouldly + Moq, FluentValidation, Mapster, ASP.NET Core Identity
- Project shape: web API (four layers: Domain, Application, Infrastructure, API)
- Integration branch: develop
- External services: PostgreSQL; Adminer and Scalar used for runtime probes

## Working agreement
- The goal is my understanding; working code is the evidence. Teaching behavior comes from the Tutor output style. If the marker TUTOR-MODE-ACTIVE is not in your instructions, say so in your first reply.
- I write all production and test code. You may edit only docs/learning/decisions.md, docs/learning/debt.md, docs/learning/handoff.md and docs/learning/reviews/, plus other docs/learning/ files during a sync I start.
- Session scope is docs/learning/current-phase.md. If a request falls outside it, say so before helping. If that file is empty, say that and ask me to fill it — don't infer the scope.
- The scope gate applies to project work. Skills I invoke explicitly (/reflect, /sync, /plan-review, /recall, /review-as-mentor, /import-history) are harness rituals and are always in scope, including when current-phase.md is empty or is itself what we're reviewing.
- This harness is reused across projects. Don't carry a pattern in from an earlier project without saying where it came from and why it applies here.
- I draft each phase plan, then run /plan-review before committing to it. You critique the draft and never rewrite it — current-phase.md is what constrains you, so it stays mine to author.

## Sources of truth
- What exists: the source code. What should be built: docs/learning/. Memory and earlier sessions are background only.
- Read docs/learning/handoff.md at the start of every session. It is the only sanctioned carry-over — a written artifact, not memory — and it is written only during a /sync I start.
- A settled choice goes in docs/learning/decisions.md; a known-unfixed problem goes in docs/learning/debt.md with a permanent ID. Don't convert one into the other.
- When docs disagree with each other or with the code, stop and name the conflict. Never reconcile silently.
- Runtime claims need evidence appropriate to this project's shape: test output, a real HTTP response, database state, observed console behavior, a forced failure. Label unverified reasoning as unverified.

@docs/learning/current-phase.md
@docs/learning/decisions.md

## Commands
I run these, after writing a prediction. You may run them too — they're in the
`ask` list — but if I haven't written a prediction yet, ask me for one first
rather than just running it.
- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project src/BookingApp.API`
- Probe HTTP: `curl -s -i http://localhost:5184/api/apartments`
- Probe DB: `psql -h postgres -U booking_app_user -d booking_app -c "<query>"`
  (or Adminer at http://localhost:8080 from the host)

## Git
I own every git write; read-only git is fine. Default review base: `git merge-base develop HEAD`.
That collapses to HEAD when HEAD *is* the integration branch, so the diff comes back empty.
If that happens, say so and ask me for an explicit base — never report "no changes".
Scaffolding (`dotnet new`, `dotnet add`, `dotnet ef`) is mine too — ask me to run it, don't run it for me.
Enforcement is settings.json plus my approval of each Bash call. Nothing here is automatic:
if you need one of these run, ask me.

## Compact Instructions
When compacting, preserve verbatim: decision and constraint IDs touched this session, open questions, my predictions and their outcomes, my unanswered direct questions, and the current step.