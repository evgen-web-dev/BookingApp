# Handoff

Updated: 2026-09-28 — written only during a sync I start.

## Where we stopped
Phase 7 was scoped. Topic: a **JSON data importer** that brings in hosts and their
apartments from a third-party booking company we are notionally acquiring.

`current-phase.md` is now partially filled, deliberately: Goal, In scope, Out of
scope, Open questions, Current step, and one known debt ID. **Done-when is still
the template** — it waits for the import's contradictions report.

No production or test code was written. No `dotnet build`, `dotnet test`,
`dotnet run`, curl or psql command was executed, so nothing here rests on runtime
evidence. Working tree at sync time: `current-phase.md`, `handoff.md`, `master.md`
modified; nothing committed.

## Next action
1. **`/import-history 7`, in a fresh session of its own.** The lens now travels by
   itself: CLAUDE.md `@`-imports `current-phase.md`, so a clean session gets the
   topic for free. That was the point of putting it in the file.
2. Finish `current-phase.md` — **Done-when** and the rest of **Debt this phase
   owns** — informed by the import's step-2 contradictions report.
3. `/plan-review`.

## Decisions locked this session
Harness and scope decisions, not project-design rules, so deliberately NOT in
decisions.md — none would bind a line of booking-app code. Each is encoded where
it operates; this list is the index.

- **The import's lens lives in `current-phase.md`, not the invocation message.**
  The handoff's carried-forward open question, now closed. `/import-history`
  never reads that file — grep confirms zero references to it in the skill, while
  `sync`, `plan-review` and `review-as-mentor` all name it. The file route works
  through CLAUDE.md's `@`-import instead, which survives compaction; a topic
  stated only in the opening message does not. Encoded in `current-phase.md`.
- **Three Phase 7 scope exclusions.** No bookings imported; no dedicated `Host`
  entity; no forced-credentials-rotation for imported hosts. Encoded in
  `current-phase.md` §Out of scope.
- **Provenance recording is In scope; its mechanism is an Open question.** Phase 7
  is the only code that will ever know which company a host came from, so
  deferring the question forecloses it rather than defers it. The *shape* — column
  on `AspNetUsers`, side table, something else — genuinely doesn't need answering
  to run the import. Encoded in `current-phase.md` (In scope + Open questions).
  Note: the Out-of-scope line was narrowed from "new entities" to `Host`
  specifically, so a provenance side table is not forbidden.
- **Do not correct `P4-03`'s wording before `/import-history` runs.**
  `/import-history` step 5 diffs debt.md against the archive text and reports
  differences; editing the entry now manufactures a spurious mismatch. The
  imprecision is recorded under Still unverified instead.
- **Carried from the previous handoff so the rewrite doesn't lose it:** a
  devcontainer rebuild recreates the `app` container and so ends the session
  running inside it. Sync before rebuilding, never after.

## Open questions carried forward
The five in `current-phase.md` §Open questions are the live ones — importer shape
(console app / library / other), new indexes or constraints, provenance mechanism,
idempotency state, and the transaction boundary. Not duplicated here; that file is
`@`-imported and this one is not.

Still open and not owned by that file:
- Whether debt surfaced outside a phase needs an ID space of its own. Carried
  unchanged. Phase 7 now exists, so it can number anything that survives.
- Whether `C-06`'s `Verified by:` should be able to record *partial* verification.
  This session proved half of it from source but left the field at "not yet",
  because the file's own rule is "not yet until a command proved it" and reading a
  migration is not that. See Still unverified.

## Predictions and outcomes
None. No command requiring a prediction was run this session — the work was
scoping and document reconciliation only.

## Still unverified
- **`C-06`, half-closed by source inspection.** The partial unique index is real:
  `Migrations/20260814180923_AddUniqueIndexOnUserNormalizedEmail.cs:17-22` drops
  `EmailIndex` and recreates it on `NormalizedEmail` with `unique: true` and
  `filter: "NormalizedEmail" IS NOT NULL`. That is migration *source*, not live
  schema, and the `date`-columns half is unchecked. Cheapest close:
  `psql -h postgres -U booking_app_user -d booking_app -c "\d \"AspNetUsers\""`
  for the index, and the same on `Bookings` for the column types.
- **The other seven constraints.** `C-01`–`C-05`, `C-07`, `C-08` all still read
  `Verified by: not yet`.
- **`P4-03`'s premise.** The entry says "`RequireUniqueEmail = false`", but there
  is no such setting anywhere in `src/`:
  `Infrastructure/DependencyInjectionExtensions.cs:34` is a bare
  `services.AddIdentityCore<User>()` with no options lambda. The claim therefore
  rests on the framework default being `false`, which I did not verify. Its
  conclusion is probably right; its wording describes a line that does not exist.
- **The seeder's crash-recovery shape, reasoned from source only.**
  `ApartmentsWithBookingsSeeder` guards on `Apartments.AnyAsync()` (line 22) while
  `SeedHostUser` recovers an existing user by `FindByEmailAsync` (line 51). So a
  crash between user creation and the apartment `SaveChangesAsync` appears to
  self-heal on re-run. **Unverified** — never forced. Worth forcing in Phase 7,
  because it is the shape of idempotency the importer wants.
- **Two EF/Identity mechanism claims I asserted in conversation and never ran.**
  Phase 7's persistence approach leans on both, so they are predictions, not facts:
  (a) EF would populate `Apartment.OwnerId` from `apartment.Owner = newUser` even
  though only the one-sided nav exists — `User` has no `List<Apartment>`;
  (b) a `User` inserted through the EF graph rather than `UserManager` would be
  invisible to `FindByEmailAsync`, because `NormalizedEmail`, `PasswordHash`,
  `SecurityStamp` and `ConcurrencyStamp` are never populated. Claim (b) is the
  reason the importer must go through `UserManager`, so it is worth forcing rather
  than assuming — and forcing it also exercises the partial unique index in `C-06`.
- **Carried unchanged from the previous handoff, still unmeasured:** the API port
  (`5184` deduced from `launchSettings.json`, app not run since); host access to
  Scalar (loopback bind + devcontainer forwarding — mechanism reasoning only); and
  whether the two compose stacks have separate databases (both declare an
  unqualified `booking_app_pgdata`; no docker CLI in this container).
