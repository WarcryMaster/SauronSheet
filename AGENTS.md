# SauronSheet AI Instructions

## Source of Truth

This file is the single source of truth for AI behavior in this repository.
Read it before acting, and follow the linked instruction files for file-type-specific rules.

**This file MUST be injected into EVERY agent and sub-agent prompt** (including `task` delegations, review agents, and any spawned AI process). No delegated work shall begin without this file as part of its instructions. If the execution environment does not automatically inject it, the delegating agent MUST include its full contents in the prompt.

## Instruction Files Pattern

Detailed AI rules live in `.github/instructions/*.md` files, auto-loaded by file type or on-demand.
- **Always-on** files (matched by `applyTo` glob) are injected automatically by the IDE for matching files.
- **On-demand** files must be manually loaded when the task matches their description.

To add new AI rules for a specific context (e.g., JavaScript, Docker, CI/CD):
1. Create a new `.github/instructions/<topic>.instructions.md` file with a descriptive `applyTo` glob or leave it on-demand.
2. Add it to the Auto-Loaded Instruction Files table below.
3. Do NOT bloat this file with per-technology rules — use instruction files instead.

## Language

Always when user interact with the IA and sdd artifacts must be in neutral Spanish. Never with Argentinian Accent or Vose! Code and IA Code like this document Agent.md must be in english.

## Working Rules

- Operate in agent mode: avoid asking for permission on routine read-only tasks.
- Follow Spec-Driven Development: Spec -> Plan -> Task -> Implement.
- Minimize conversational overhead; execute clear tasks directly.
- If a command fails, analyze, fix, and retry. Stop only if you are stuck in a loop.
- **E2E test coupling**: every code change that affects UI behavior (Razor Pages, forms, modals, navigation, JS interactions, Alpine.js components) MUST be accompanied by a corresponding review and update of the affected E2E tests. Never modify frontend code without ensuring the E2E tests still match the new behavior.
- Use Sentry for ALL observability: runtime logging, tracing, diagnostics, error capture, and important business events. Do not use Console.WriteLine, Debug.WriteLine, Trace.WriteLine, or any other logging mechanism. Sentry is the single pipeline for backend (SentrySdk) and frontend (JavaScript Sentry SDK).
- Keep source code, identifiers, comments, docstrings, commit messages, PRs, ADRs, and ALL runtime traces (Sentry, logs, breadcrumbs, metrics) in English.
- Keep specs, plans, requirements, and acceptance criteria in Spanish.
- Converse with the AI in Spanish. Code, identifiers, and traces stay in English; everything else — specs, docs, discussions with the AI — goes in Spanish.
- Use neutral Spanish (from Spain / castellano) in all AI conversations. No regional dialects (Rioplatense, Mexican, etc.). This overrides any global or personal configuration that specifies otherwise.

## Architecture (see `.github/instructions/architecture.instructions.md`)

Full rules for Clean Architecture, CQRS + MediatR, Domain-Driven Design, Supabase integration, Authentication, and Database Migrations.

## Domain Patterns Quick Reference

| Pattern | Convention | Example |
|---|---|---|
| Aggregate Root | Base class; parameterized constructor; no public setters | Transaction, Category, Budget |
| Value Object | Immutable; value-based equality; validated on construction | Money, DateRange |
| Strong-Typed ID | Wrapper around Guid/string; prevents ID mixing at compile time | TransactionId(Guid), UserId(string) |
| Domain Service | Cross-entity logic; depends on repository interfaces only | CategoryService |
| Specification | Filtering with domain language; MaxResults default 1000 | TransactionByDateRangeSpecification |
| Domain Exception | Thrown on invariant violation; caught in Application layer | DomainException |
| Guard Method | Returns bool to prevent invalid operations | Category.CanDelete() |
| System Default | Immutable seeded values; flagged with boolean property | Category.IsSystemDefault |

## Quality Rules (see `.github/instructions/csharp-quality.instructions.md`)

- **Never use `var`** — always use explicit type declarations. `var` oculta la intención del código y obliga al lector a inferir el tipo, lo que reduce la legibilidad. Como dice Uncle Bob: la claridad es lo primero. En tests también: cada declaración debe ser autoevidente sin necesidad de hover en el IDE.
- Keep `CancellationToken` last and forward it downstream.
- Dispose resources deterministically.
- Use modern throw helpers and preserve stack traces with `throw;`.
- Do not expose `List<T>` in public APIs.
- Avoid multiple enumeration and prefer `TryGetValue` where applicable.
- Parameterize SQL and validate all external inputs.
- Use `ConfigureAwait(false)` in infrastructure/library code when context capture is unnecessary.

## Error Handling (see `.github/instructions/error-handling.instructions.md`)

Full rules for Sentry logging, exception hierarchy, user-safe messages, and leak prevention. Always-on for `.cs` files.

## Frontend (see `.github/instructions/razor-frontend.instructions.md`)

Full rules for MDBootstrap, Alpine.js, HTMX, Chart.js, Flatpickr, modals, forms, and Cross-Attribute Compatibility. Auto-loaded for `.cshtml` files.

## Testing Strategy (see `.github/instructions/testing.instructions.md`)

### Testing Pyramid
| Level | Scope | Tools | When |
|---|---|---|---|
| Unit Tests | Domain entities, VOs, domain services | xUnit + Moq | Every phase with domain changes |
| Integration | Application handlers (mocked repos) | xUnit + Moq + in-memory doubles | App layer scope phases |
| End-to-End | Playwright browser tests | Playwright | UI/UX scope phases |

### Coverage Requirements
| Scope | Minimum Coverage |
|---|---|
| Domain Layer | 80% |
| Application Layer | 70% |

**E2E test coupling** (see Working Rules): every frontend code change MUST include E2E test review/update.

## Common Pitfalls & Lessons Learned (see `.github/instructions/common-pitfalls.instructions.md`)

Architecture & Code anti-patterns, Supabase/Postgrest C# client gotchas, and PDF Parser number normalization.

## Auto-Loaded Instruction Files

| File | Applies to | Notes |
|---|---|---|
| `.github/instructions/csharp-quality.instructions.md` | `**/*.cs` | Always-on C# baseline rules |
| `.github/instructions/error-handling.instructions.md` | `**/*.cs` | Always-on error handling rules |
| `.github/instructions/razor-frontend.instructions.md` | `**/*.cshtml` | Razor Pages and frontend rules |
| `.github/instructions/architecture.instructions.md` | on-demand | Architecture, DDD, CQRS, migrations |
| `.github/instructions/domain-patterns.instructions.md` | on-demand | Domain building blocks reference |
| `.github/instructions/csharp-rules-design-and-naming.instructions.md` | on-demand | API, design, naming, globalization rules |
| `.github/instructions/csharp-rules-performance-and-maintainability.instructions.md` | on-demand | Performance and maintainability rules |
| `.github/instructions/csharp-rules-reliability-and-usage.instructions.md` | on-demand | Reliability, async, disposal, usage rules |
| `.github/instructions/csharp-rules-security-platform-and-il.instructions.md` | on-demand | Security, platform, serialization, IL rules |
| `.github/instructions/testing.instructions.md` | on-demand | Testing pyramid, coverage, E2E policy |
| `.github/instructions/common-pitfalls.instructions.md` | on-demand | Anti-patterns, gotchas, lessons learned |

## Priority

If there is any conflict, this file and the linked instruction files take priority over other conversational context.


<!-- headroom:rtk-instructions -->
# RTK (Rust Token Killer) - Token-Optimized Commands

When running shell commands, **always prefix with `rtk`**. This reduces context
usage by 60-90% with zero behavior change. If rtk has no filter for a command,
it passes through unchanged — so it is always safe to use.

## Key Commands
```bash
# Git (59-80% savings)
rtk git status          rtk git diff            rtk git log

# Files & Search (60-75% savings)
rtk ls <path>           rtk read <file>         rtk grep <pattern>
rtk find <pattern>      rtk diff <file>

# Test (90-99% savings) — shows failures only
rtk pytest tests/       rtk cargo test          rtk test <cmd>

# Build & Lint (80-90% savings) — shows errors only
rtk tsc                 rtk lint                rtk cargo build
rtk prettier --check    rtk mypy                rtk ruff check

# Analysis (70-90% savings)
rtk err <cmd>           rtk log <file>          rtk json <file>
rtk summary <cmd>       rtk deps                rtk env

# GitHub (26-87% savings)
rtk gh pr view <n>      rtk gh run list         rtk gh issue list

# Infrastructure (85% savings)
rtk docker ps           rtk kubectl get         rtk docker logs <c>

# Package managers (70-90% savings)
rtk pip list            rtk pnpm install        rtk npm run <script>
```

## Rules
- In command chains, prefix each segment: `rtk git add . && rtk git commit -m "msg"`
- For debugging, use raw command without rtk prefix
- `rtk proxy <cmd>` runs command without filtering but tracks usage
<!-- /headroom:rtk-instructions -->
