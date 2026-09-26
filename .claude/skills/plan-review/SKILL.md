---
name: plan-review
description: Critique my draft phase plan before I commit to it
argument-hint: "[path to the draft, default docs/learning/current-phase.md]"
disable-model-invocation: true
allowed-tools: Read, Grep, Glob, Bash(git log:*), Bash(git diff:*)
---
This is a report ritual (see tutor.md), so deliver all ten checks in one pass —
but stay in the coaching register throughout: name problems and ask, never fix.
Reviewing a plan is the most Socratic moment in the cycle; a bad plan has no
compiler to catch it.

Read the draft at "$1" (default docs/learning/current-phase.md), plus master.md's
roadmap, decisions.md, debt.md, and enough of the code to know what already exists.

Do NOT rewrite the plan. Do not hand me a revised version, not even "as an
example". Name problems and ask questions; I revise it.

Check for these, in this order — the first three are the ones that waste a phase:

1. **Unfalsifiable Done-when.** For each item: what exactly would I run, and what
   would I see, to know it's done? "Tests pass" and "understand X" are not
   evidence. "I can explain why this is registered scoped and predict what breaks
   if it's a singleton" is. Quote any item that fails this.
2. **Nothing here can fail.** Is there at least one claim in this phase I could be
   wrong about? A phase with no falsifiable prediction teaches nothing — I'll
   finish it and have learned that I can already do what I could already do.
3. **Two phases wearing one hat.** Could any Done-when item be its own phase? Does
   the plan touch unrelated subsystems? Name the split you'd make.
4. **Build goal masquerading as a learning goal.** "Add pagination" is a feature.
   "Understand how EF translates Skip/Take and what it costs at 100k rows" is a
   phase. If every Done-when item is feature-shaped, I'm learning by accident.
5. **Empty or generic out-of-scope list.** This is the section that does the real
   work. If I can't name what I'm refusing to do, scope will creep and I won't
   notice. Push me for the specific temptations this phase invites.
6. **Mechanism count.** Zero new mechanisms means a comfortable, low-yield phase.
   Three or more at once means that when something breaks I won't be able to
   attribute it. Say which it is.
7. **Evidence I can't actually produce.** Does a Done-when item need a test project,
   a database, or an HTTP surface that doesn't exist yet? Then either that setup is
   part of this phase or the item is unverifiable as written.
8. **Unresolved dependency.** Does the plan presume a decision that isn't in
   decisions.md yet? Gate on it — name the decision I owe before starting.
9. **Over-specified.** Does the plan dictate the implementation rather than the
   outcome? If it does, there's nothing left to discover and the phase is typing.
10. **Roadmap fit.** Does this advance the arc in master.md, or did it drift into
   whatever was most interesting last session? Does it honor the locked decisions
   in decisions.md, or does it need one overturned first — and have I said so?
11. **Debt IDs resolve.** Mechanical, but cheap. Every ID under "Debt this phase
   owns" must exist in debt.md and be in its Open section, not Resolved. Name any
   that don't, and say whether it reads as a typo or as a claim on work already
   done. Then, at most three: open debt whose area this phase's In-scope already
   touches but which the plan doesn't claim — cheaper to fix while I'm in that
   code than to come back for it.

End with:
- The single weakest item, and one question about it.
- Whether you'd start this phase as written: yes / yes after fixing item N / no.

Change no file.