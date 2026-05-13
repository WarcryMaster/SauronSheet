# SauronSheet AI Instructions

See `AGENTS.md` for the single source of truth for AI behavior in this repository.

File-type-specific rules are loaded automatically by the editor/IDE via `applyTo` scoping:

| File | Applies to |
|------|------------|
| `.github/instructions/csharp-quality.instructions.md` | `**/*.cs` |
| `.github/instructions/csharp-rules-design-and-naming.instructions.md` | on-demand |
| `.github/instructions/csharp-rules-performance-and-maintainability.instructions.md` | on-demand |
| `.github/instructions/csharp-rules-reliability-and-usage.instructions.md` | on-demand |
| `.github/instructions/csharp-rules-security-platform-and-il.instructions.md` | on-demand |
| `.github/instructions/razor-frontend.instructions.md` | `**/*.cshtml` |

> ⚠️ Do NOT add duplicate agent instructions to this file. `AGENTS.md` is the canonical source.
