# Handoff

Updated: 2026-09-26 — written only during a sync I start.

## Where we stopped
No phase was open. This session was a harness review — the first real use of the
Tutor setup against this project — plus the fixes it surfaced. `current-phase.md`
is still the unedited template, deliberately.

The repo was restructured: four projects moved to `src/`, `BookingApp.UnitTests`
to `tests/`, committed through `03a2167`. The devcontainer was then rebuilt and
the bin/obj volume work verified — see Predictions. Uncommitted at time of
writing: `CLAUDE.md`, `debt.md`, `handoff.md`.

## Next action
The rebuild is done and verified. Start the next session like this:

1. **Open with a plain message, not a slash command.** A skill's instructions take
   over the turn, and `import-history` step 1 never mentions handoff.md — so
   leading with the command risks skipping the `CLAUDE.md:22` handoff read on the
   one session where it matters most. Confirm it read this file.
2. Name Phase 7's topic. Candidates are in the Phase 6 close-out,
   `docs/learning/archive/phase-6.md`; `master.md:21` says nothing is committed.
3. `/import-history 7`, in a session of its own — it reads ~1,500 archive lines,
   writes a report and the debt move, then stops at step 6 and waits.
4. Finish `current-phase.md` — Done-when and Current step — informed by the
   import's step-2 contradictions report. Those two depend on the import; Goal and
   In scope do not (see Open questions).
5. `/plan-review`.

Learned this session and worth keeping: a devcontainer rebuild recreates the `app`
container and so ends the session running inside it. Sync before rebuilding,
never after.

## Decisions locked this session
Harness decisions, not project-design rules, so deliberately NOT in decisions.md —
none of them would bind a line of booking-app code. Each is encoded where it
operates, which is the record; this list is the index.

- **`C-01`–`C-08` go on the `/import-history` shortlist.** They were hand-lifted
  from the archive and passed through no gate. That run is the only time the
  archive is ever read, so it is the last chance to attach provenance and
  evidence to them. Encoded in `import-history/SKILL.md` step 4.
- **Rejecting an already-live entry marks, never deletes.** A rejected `C-xx`
  stays binding until removed by hand, carrying an appended `REJECTED <date>` line
  plus the grep results for its ID. `C-01` alone is referenced by `P3-06` in
  debt.md and by the Enumeration-safety standing check, so a silent delete would
  orphan both. Encoded in `import-history/SKILL.md` step 7 and its closing line.
- **Ten named volumes over `ArtifactsPath`.** Keeps container build output off the
  host and out of a shared `obj/`, so the container and host Rider stop
  overwriting each other's `nuget.g.props`. Encoded in
  `.devcontainer/docker-compose.yml` and the `mkdir` loop in the Dockerfile.
- **`Verified by:` added to the `C-xx` format.** Constraints outrank decisions but
  had the weaker evidence discipline — `D<N>-xx` carried an evidence field and
  `C-xx` did not. Encoded in `decisions.md:10-11`.

## Open questions carried forward
- Phase 7's topic. Nothing committed.
- **Where the import's lens lives.** Either write Phase 7's Goal and In scope into
  `current-phase.md` before running `/import-history 7`, or state the topic in the
  invocation message. It matters because the skill's only lens is `$1`, and the
  digit alone carries no information — step 0 ("narrow to what phase `$1`
  touches"), step 3 ("load-bearing for phase `$1` specifically") and step 4 all
  classify against it. Without a topic the filter stops filtering and the shortlist
  pads out. In the file it is durable and `@`-imported; in the message it is
  ephemeral and can drift out of a long session's context. Decide in that session.
- Whether debt surfaced outside a phase needs an ID space of its own. Deferred:
  the one remaining item is held under Still unverified rather than numbered.
  Phase 7 numbers it if it survives.

## Predictions and outcomes
- `dotnet test` after the project move → predicted all pass, same count as before
  the move; **got** all pass, same count. *(Yevhenii's observation — I did not see
  the output. The move itself I verified structurally: `src/`, `tests/`, and all
  five `<Project Path>` entries in `BookingApp.slnx`.)*
- Ownership of `bin`/`obj` after the devcontainer rebuild → **`containerdev`.**
  So the Dockerfile's `mkdir` + `chown` rule *does* hold for a named volume nested
  under a bind mount: Docker's copy-up consulted the image directories despite
  `/workspace` being bind-mounted, which was the genuinely uncertain part. The
  `chown`-in-`postStartCommand` fallback is not needed and is dropped.
- Isolation of the ten volumes → **verified by forced experiment.** `dotnet build`
  run both in the container and on the host, each time with the opposite side's
  `bin`/`obj` emptied first; neither side's output appeared on the other. Container
  and host build output are genuinely separate, which was the actual goal —
  ownership was only the thing that could have broken it.

## Still unverified
- **All eight `C-01`–`C-08`** — every one reads `Verified by: not yet`. Cheapest
  first: `C-06` claims a partial unique index on `NormalizedEmail` and `date`
  column types, both checkable against the migrations and then the live schema via
  `psql -h postgres`. Its `Why:` says "proven twice," but that proof happened in a
  pre-harness web chat with no repo access.
- **The API port.** The ambiguity is gone: `ASPNETCORE_URLS` was removed from
  `containerEnv`, leaving `launchSettings.json`'s `5184` as the only source. But
  the app has not run since, so 5184 is deduced, not observed.
- **Host access to Scalar.** `applicationUrl: http://localhost:5184` binds loopback
  only. That works through devcontainer client forwarding, because the forwarder
  runs inside the container — it would NOT work through a `ports:` publish, which
  delivers to `eth0`. Mechanism reasoning, not a measurement.
- **The two compose stacks have separate databases.** Both declare an unqualified
  `booking_app_pgdata`, and Compose prefixes volume names with the project name, so
  seed data and applied migrations in one are invisible in the other. `docker
  volume ls` settles it; there is no docker CLI in this container.