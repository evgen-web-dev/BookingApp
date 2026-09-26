---
name: import-history
description: One-time migration of pre-harness project docs into the learning docs
argument-hint: "<upcoming phase number — the lens for what counts as load-bearing>"
disable-model-invocation: true
effort: xhigh
allowed-tools: Read, Grep, Glob, Write, Edit, Bash(git log:*), Bash(git diff:*), Bash(git show:*)
---
One-time import. The originals in docs/learning/archive/ were written in Claude
web chats with no access to this codebase, before this harness existed. Treat
them as a historical record and as claims to test — never as a source of truth,
and never as something to summarize.

Do not rewrite them. Do not invent rationale that isn't in them. Step 4 produces
a report, step 5 moves the debt ledger, and step 7 appends to decisions.md —
only entries I explicitly accepted.

0. This is one run over the whole archive, not one run per phase. If the
   shortlist in step 4 would exceed roughly 25 candidates, say so and narrow to
   what phase "$1" touches rather than padding it out.
1. Read docs/learning/archive/ in full, then read the actual code and git log.
2. **Contradictions first.** List every claim in the archive that the code does
   not support: things described that don't exist, things built differently than
   described, decisions recorded that the code doesn't honor. path:line for the
   code, quote for the doc. This is the most valuable section — lead with it.
3. Classify the rest of the archive's content into:
   - Rationale still load-bearing for phase "$1" specifically — load-bearing means
     phase "$1" must honor it or explicitly overturn it. Everything else is history.
   - Rationale about settled history, unlikely to be reopened.
   - Descriptions of what exists (say so and move on — the code owns these now).
   - Open questions never resolved.
   - Debt items with ledger IDs. List them in the report; they are moved in step 5,
     verbatim, IDs intact — never renumbered, never rewritten as decisions.
4. Write the report to docs/learning/reviews/archive-import-review.md: the
   contradictions from step 2, the classification from step 3, and a numbered
   SHORTLIST of candidate decisions — proposed ID, one-line summary, and which
   archive file and phase each came from. Propose IDs as D<archive phase>-xx,
   landing in decisions.md §Pre-harness. A candidate whose substance is already
   in decisions.md still goes on the shortlist, marked "already present as
   <ID>" and keeping that ID — C-01–C-08 were hand-lifted before this harness
   and have never been checked against their source. No full entries yet. End the file with
   which archive content you'd deliberately leave behind, and why.
5. Move the debt items into docs/learning/debt.md verbatim, IDs intact, creating
   the file from its template if absent. An item whose ID is already there was
   moved before this run: do not re-move or renumber it — diff it against the
   archive text instead, and report any that differ or are missing entirely.
   This is a move, not an extraction — no rationale is invented, so it needs no
   per-item approval. Say how many moved, how many matched, how many differed.
6. Stop writing. Tell me how many decision candidates are on the shortlist and wait.
7. Only when I say to start, walk the shortlist with me conversationally, one
   candidate at a time, waiting for my response on each. Show the proposed
   decisions.md entry as a diff block — for one marked "already present", diff
   against the live entry, never a fresh append — and distinguish what the archive actually
   states from what you are inferring — if "Why:" or "Rejected:" is not in the
   source, write "not recorded, you'll need to supply it" rather than
   reconstructing it. "Verified by:" is "not yet" unless a command proved it in
   this session. I accept, rewrite or reject each one.
   Rejecting an entry that is already live deletes nothing. Append one line to
   that entry — "**REJECTED <date> — still binding until I remove it by hand.**"
   — with the grep results for its ID, and list it in the report.

Write only the report (step 4) and debt.md (step 5). Append to decisions.md only
during step 7 — entries I accepted, plus the rejection marker above. Never
delete from it.