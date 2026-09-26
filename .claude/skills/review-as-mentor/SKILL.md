---
name: review-as-mentor
description: Mentor-style review of a phase or large task; run in a fresh session
argument-hint: "<base-ref: tag or commit just before the scope's first commit>"
disable-model-invocation: true
effort: xhigh
allowed-tools: Read, Grep, Glob, Write, Bash(git diff:*), Bash(git log:*), Bash(git show:*), Bash(git merge-base:*)
---
This is a report ritual, not a coaching turn: the tutor interaction rules are
suspended for it (see tutor.md). Coach nothing here — judge the work as evidence.
If TUTOR-MODE-ACTIVE is in your instructions, say so once at the top so I know
the suspension is by instruction, not by config. 
Act as a senior .NET reviewer who
wasn't in the sessions that produced this work: the only evidence is the code, the
docs and git history.

Base ref: "$1". If blank, stop and ask me for it. If it's a branch name, resolve it
with `git merge-base <base> HEAD` first.

## 1. Identify the project shape first
Read the solution and project files and the entry point. Note the SDK
(`Microsoft.NET.Sdk`, `.Web`, `.Razor`, `.Worker`), `OutputType`,
`TargetFramework`, and the `PackageReference` list. State the shape in one line
before reviewing anything.

Then apply **Universal** plus only the conditional sections that match. Do not
hunt for machinery this project doesn't have — a missing authorization check is
not a finding in a console app. Equally, do not skip a section because the
project is small.

## 2. Map the scope
`git log --oneline <base>..HEAD` and `git diff --stat <base> HEAD`.

## 3. Read
Every changed file in full, plus what connects to it: callers, the entry point and
composition root, tests, and whatever the project shape adds below. Look for what
should have changed but didn't.

## 4. Read the docs as claims to test
docs/learning/decisions.md, docs/learning/debt.md and docs/learning/current-phase.md
are claims, not ground truth. Challenge any decision you'd push back on in a real
review. A decision whose "Verified by" says "not yet" and whose code has shipped is
a finding, and so is debt this scope introduced that never reached the ledger.

## 5. Review

### Universal
- Correctness and edge cases: boundaries, empty and single-element cases, integer
  overflow, culture-sensitive parsing and formatting.
- Nullability: is `<Nullable>` enabled, and is it respected or suppressed with `!`.
- Seams and testability: dependencies injected or `new`ed in place; time, randomness
  and I/O behind something substitutable.
- Async: `CancellationToken` accepted and passed down; no sync-over-async
  (`.Result`, `.Wait()`); `async void` outside event handlers.
- Lifetime and disposal: `IDisposable`/`IAsyncDisposable` honored; nothing captured
  past its scope.
- The error contract: which layer translates exceptions into a result, and whether
  failures are distinguishable from empty success.
- Layer boundaries, naming, and whether each type has one reason to change.
- Whether each test would fail on a real defect, or only on a rewrite. Check that
  at least one test fails if you mentally invert a condition.
- Debt added without a ledger entry; commit granularity and message honesty.

### If it exposes an HTTP surface (ASP.NET Core, minimal API, MVC, gRPC)
- Authorization on every client-supplied identifier — the IDOR check. "The caller
  is authenticated" is not "the caller owns this row."
- Input validation at the boundary; over-posting; request DTOs distinct from
  persistence entities.
- Status codes and the shape of error responses; does a validation failure look
  different from a server fault.
- Secrets: configuration precedence, user-secrets in development, nothing in
  appsettings that shouldn't be in git.
- Unbounded work: missing pagination or result limits.
- Where exceptions become responses, and whether stack traces can escape.

### If it touches a database (EF Core, Dapper, raw ADO)
- Review migrations as code, not as output: destructive column changes, missing
  indices, nullability changes against existing rows.
- N+1 queries; missing `AsNoTracking` on read paths; `Include` graphs pulled per row.
- `SaveChanges` and transaction boundaries: is one logical operation one unit of work.
- Constraints and cascade behavior expressed in the schema, not only in C#.
- Parameterization everywhere raw SQL appears.

### If it is a console app or worker service
- Input handling: unmapped keys, malformed arguments, EOF and redirected stdin.
- The main loop and cancellation: Ctrl+C mid-operation, graceful shutdown.
- Whether domain logic is separable from `Console`/host so it can be tested at all.

### If it renders UI (Blazor, Razor Pages, MVC views)
- State and re-render correctness; component parameters mutated from inside.
- Encoding: any `MarkupString` or `Html.Raw` reachable from user input.
- Logic in views that belongs behind them.

### If it is a class library
- The public surface: what is `public` that didn't need to be; what is the
  intended consumption pattern; are breaking changes visible in the diff.

## 6. Write it up
docs/learning/reviews/<scope>-review.md. Order findings
Blocking → Should fix → Consider → Question; each gets path:line, the problem, the
mechanism behind it, and the question you'd ask the author. Describe fixes; don't
write them.

## 7. Close
End with a verdict (Approve / Approve with comments / Request changes) and the
three things in this scope I should be able to explain without notes.

Edit no other file. Don't soften findings.

---