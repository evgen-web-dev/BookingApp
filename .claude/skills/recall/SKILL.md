---
name: recall
description: Spaced retrieval on decisions and code I haven't touched recently
argument-hint: "[count, default 2]"
disable-model-invocation: true
allowed-tools: Read, Grep, Glob, Bash(git log:*), Bash(git diff:*)
---
Pick "$1" (default 2) items I have NOT worked on recently — prefer decisions in
decisions.md whose area the last few commits didn't touch, and the code paths
untouched longest per `git log`. Decisions from an earlier project in this series
are fair game and often the most useful.

For each, one at a time:
1. Name the decision ID or path:line. Quote nothing else.
2. Ask me to reconstruct from memory: what it does, why it was chosen, and what
   the rejected alternative was.
3. After I answer, diff my answer against the file. Report precisely where I was
   wrong, where I was vague, and where I was right for the wrong reason.
4. Then ask what would have to change for it to be reopened. For a D-xx the
   "Revisit if" field is the answer, so check mine against it. A C-xx constraint
   has no such field — ask instead what would justify overturning it, and whether
   that's a cost I'd actually accept.

Change nothing. If I get one fully right, say so plainly and move on.