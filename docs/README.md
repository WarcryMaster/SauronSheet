# Documentation & Architecture Decision Records (ADRs)

This directory contains **informal product context and architecture decisions** that inform
development but are not formal speckit specifications.

## Directory Structure

```
docs/
├── adr/                           # Architecture Decision Records
│   ├── 00001-description.md       # Format: [NNNN]-[short-title]
│   └── ...
├── PDF_PARSER_AMOUNT_NORMALIZATION.md   # Technical implementation notes
└── README.md                      # This file
```

## Architecture Decision Records (ADRs)

ADRs capture **why** we made certain architectural choices, linking implementation details
to business context. They are not specifications but reference material for future decisions.

| ID | Title | Date |
|----|----- |------|
| [00001](adr/00001-pdf-parser-dual-format-normalization.md) | PDF Parser Amount Normalization — Dual-Format Support | 2026-03-13 |

## Technical Notes

- **[PDF Parser Amount Normalization](PDF_PARSER_AMOUNT_NORMALIZATION.md)**: Implementation
  details for dual-format number parsing in Infrastructure/PDF/Parsers

## Reference

- **Specifications**: See `specs/` for formal phase-based requirements and plans.
- **Constitution**: See `.specify/memory/constitution.md` for governance and core principles.
- **Code**: All source code must follow patterns in `.github/copilot-instructions.md`.
