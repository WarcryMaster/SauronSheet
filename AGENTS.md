# SauronSheet AI Instructions

## Source of Truth

This file is the single source of truth for AI behavior in this repository.
Read it before acting, and follow the linked instruction files for file-type-specific rules.

## Working Rules

- Operate in agent mode: avoid asking for permission on routine read-only tasks.
- Follow Spec-Driven Development: Spec -> Plan -> Task -> Implement.
- Minimize conversational overhead; execute clear tasks directly.
- If a command fails, analyze, fix, and retry. Stop only if you are stuck in a loop.
- Use Sentry for ALL observability: runtime logging, tracing, diagnostics, error capture, and important business events. Do not use Console.WriteLine, Debug.WriteLine, Trace.WriteLine, or any other logging mechanism. Sentry is the single pipeline for backend (SentrySdk) and frontend (JavaScript Sentry SDK).
- Keep source code, identifiers, comments, docstrings, commit messages, PRs, ADRs, and ALL runtime traces (Sentry, logs, breadcrumbs, metrics) in English.
- Keep specs, plans, requirements, and acceptance criteria in Spanish.
- Converse with the AI in Spanish. Code, identifiers, and traces stay in English; everything else — specs, docs, discussions with the AI — goes in Spanish.

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

## Error Handling and Leak Prevention

- **Never expose internal exception details to users.** Show generic messages only.
- `catch (Exception ex)` blocks must NEVER include `ex.Message` in user-facing output.
- `catch (DomainException ex)` may show `ex.Message` since domain exceptions are user-safe by design (business rule violations).
- `catch (HttpRequestException ex)` must always show a generic network error message.
- **Every catch block must capture to Sentry** via `SentrySdk.CaptureException` with appropriate scope tags and level.
- Infrastructure layer must never embed raw exception messages in return values (`AuthResult.Failure`, etc.). Log to Sentry, return generic.
- Application layer command handlers must not propagate infrastructure error messages as DomainException messages. Use fixed/translated messages.
- Validation: before adding a new catch block or error path, verify the message cannot contain sensitive infrastructure details (hostnames, IPs, connection strings, file paths, stack traces).
- Exception type hierarchy: catch specific types (HttpRequestException, DomainException) before the generic `Exception` fallback.

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
