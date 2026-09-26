---
name: sync
description: End-of-session sync; write handoff and reconcile the learning docs
disable-model-invocation: true
allowed-tools: Read, Grep, Glob, Edit, Bash(git status:*), Bash(git diff:*), Bash(git log:*)
---
A sync I started, so you may edit other docs/learning/ files this turn.

1. Read master.md, current-phase.md, decisions.md, debt.md, handoff.md and the
   working tree state.
2. Name every conflict you find before writing anything: docs vs docs, docs vs
   code. Do not reconcile silently — list them and ask me which way to resolve
   each one.
3. Once resolved, update in this order:
   - decisions.md: entries are appended when a decision locks, not here. Verify
     each one from this session is present, correctly formatted, and has all
     four fields. Append only what's missing; never duplicate an existing ID.
   - current-phase.md: tick Done-when boxes the code actually satisfies, and say
     which evidence proved each. Update Current step; carry open questions forward.
   - handoff.md: rewrite from its template, including predictions and their real
     outcomes.
   - debt.md: add debt this session surfaced, numbered by the current phase
     (P7-01, …). IDs are permanent — never renumber or reuse.
   - master.md: if a phase opened or closed this session, update its roadmap line
     and status marker. Nothing else in master.md is yours to change.
4. Sweep decisions.md for every entry still marked "Verified by: not yet". For
   each, say whether this session's work could now verify it and how. Update only
   the ones a command actually proved this session.
5. If decisions.md has passed ~200 lines, say so and name which closed-phase
   entries could move to docs/learning/decisions-closed.md — a file NOT
   @-imported by CLAUDE.md, read on demand by /recall. Propose the split; move
   nothing without my go-ahead, since CLAUDE.md's import list is mine to change.
   Never archive the Standing constraints or Standing checks sections — they are
   the permanently-hot part of this file and must stay loaded.
6. End with two lists: what I claimed this session that is still unverified, and
   which decisions are now load-bearing for the next phase.