# Documentation

This directory contains **architecture decisions and informal product context** that inform
development. For formal specifications and SDD artifacts, see the directories below.

## Directory Structure

```
docs/
├── adr/                           # Architecture Decision Records (format: [NNNN]-[short-title])
└── README.md                      # This file
```

## Architecture Decision Records (ADRs)

ADRs capture **why** we made certain architectural choices, linking implementation details
to business context. They are not specifications but reference material for future decisions.

> No active ADRs. Previous ADRs (e.g., PDF parser dual-format normalization) were retired
> when the PDF import module was replaced with Excel (see `openspec/` for the change archive).

## Related Documentation

| Directory | Purpose |
|-----------|---------|
| `openspec/` | SDD artifacts: specs, changes (archived), and roadmap |
| `specs/` | Phase-based specifications (phase-0 through phase-6) and feature specs |
| `sdd/` | Active SDD working files for current changes |
| `DESIGN.md` | Visual design source of truth (colors, typography, spacing, components) |
| `.specify/memory/constitution.md` | Governance and core principles |
| `.github/copilot-instructions.md` | Code patterns and quality rules |
| `AGENTS.md` | AI behavior rules and architecture constraints |
