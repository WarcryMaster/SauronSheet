# SauronSheet AI Instructions

## Source of Truth

This file is the single source of truth for AI behavior in this repository.
Read it before acting, and follow the linked instruction files for file-type-specific rules.

## Working Rules

- Operate in agent mode: avoid asking for permission on routine read-only tasks.
- Follow Spec-Driven Development: Spec -> Plan -> Task -> Implement.
- Minimize conversational overhead; execute clear tasks directly.
- If a command fails, analyze, fix, and retry. Stop only if you are stuck in a loop.
- Use Sentry for runtime logging, tracing, and diagnostics. Do not use Console.WriteLine, Debug.WriteLine, or Trace.WriteLine.
- Keep source code, identifiers, comments, docstrings, commit messages, PRs, and ADRs in English.
- Keep specs, plans, requirements, and acceptance criteria in Spanish.

## Architecture Rules

- Clean Architecture with unidirectional dependencies.
- Frontend -> Application -> Domain.
- Infrastructure -> Domain only.
- Domain must not reference Application, Infrastructure, or Frontend.
- Application must not reference Infrastructure or Frontend directly.
- Use MediatR for commands and queries.
- Tests are mandatory before implementation in every feature.
- Strong-typed IDs, immutability, and explicit invariants are required in the Domain.

## Quality Rules

- Prefer explicit types over `var`.
- Keep `CancellationToken` last and forward it downstream.
- Dispose resources deterministically.
- Use modern throw helpers and preserve stack traces with `throw;`.
- Do not expose `List<T>` in public APIs.
- Avoid multiple enumeration and prefer `TryGetValue` where applicable.
- Parameterize SQL and validate all external inputs.
- Use `ConfigureAwait(false)` in infrastructure/library code when context capture is unnecessary.

## Frontend Rules

- Razor Pages should use PageModel patterns and antiforgery protection.
- Use MDBootstrap via CDN, not Bootstrap or local alternatives.
- Keep JavaScript modern: `const` / `let`, event listeners, null checks, and server-side revalidation.

## Documentation and Review Rules

- Design docs should lead with the decision, then the details.
- Use tables, checklists, and clear review paths.
- For chained work, state what is in scope, what is out of scope, and the dependency order.

## Auto-Loaded Instruction Files

These files are referenced by the editor/IDE through `applyTo`-style scoping and should be kept in sync with this root policy:

| File | Applies to | Notes |
|---|---|---|
| `.github/instructions/csharp-quality.instructions.md` | `**/*.cs` | Always-on C# baseline rules |
| `.github/instructions/csharp-rules-design-and-naming.instructions.md` | on-demand | API, design, naming, globalization rules |
| `.github/instructions/csharp-rules-performance-and-maintainability.instructions.md` | on-demand | Performance and maintainability rules |
| `.github/instructions/csharp-rules-reliability-and-usage.instructions.md` | on-demand | Reliability, async, disposal, usage rules |
| `.github/instructions/csharp-rules-security-platform-and-il.instructions.md` | on-demand | Security, platform, serialization, IL rules |
| `.github/instructions/razor-frontend.instructions.md` | `**/*.cshtml` | Razor Pages and frontend rules |

## Priority

If there is any conflict, this file and the linked instruction files take priority over other conversational context.
