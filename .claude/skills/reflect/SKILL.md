---
name: reflect
description: Self-reflect checkpoint on reasoning and code drift
argument-hint: "[base-ref]"
disable-model-invocation: true
allowed-tools: Read, Grep, Glob, Bash(git diff:*), Bash(git status:*), Bash(git log:*), Bash(git merge-base:*)
---
Checkpoint with two inputs:
1. Reasoning: this conversation since the last /reflect, or since the start.
2. Code: base ref "$1". If blank, use `git merge-base` with the integration
   branch named in CLAUDE.md. Run `git diff <base>` (committed, staged and
   unstaged changes since base), `git status --short` (untracked files, which
   git diff omits) and `git log --oneline <base>..HEAD`.

If this is not a git repository, say so and report on the reasoning half only.

Report with quotes and path:line:
1. Right conclusion, wrong mechanism: reasons of mine that wouldn't survive a
   follow-up question.
2. Decided-then-drifted: entries in docs/learning/decisions.md that the code or
   my latest reasoning doesn't honor.
3. Local pattern-transplant: code copied from elsewhere in the repo — or from a
   previous project in this learning series — without checking why it was that
   way there. Name the source it came from.
4. Machinery that sounds responsible: additions with no named failure they
   prevent. An interface with one implementation, a layer that only forwards, a
   try/catch that rethrows.
5. My direct questions you haven't answered.

Then ask me one question about the weakest spot. Change nothing.

---