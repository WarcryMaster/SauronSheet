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

| ADR | Decision | Status |
|---|---|---|
| [0001-version-local-static-assets](adr/0001-version-local-static-assets.md) | Version local CSS, JS, and image assets with `asp-append-version` to avoid production/local drift caused by stale caches. | Active |
| [0002-safe-json-data-passing](adr/0002-safe-json-data-passing.md) | Pass data from Razor to JS using `data-*` + delegated listeners (per-item) or `<script type="application/json">` + `JSON.parse` (payloads). Never use `Html.Raw(Json.Serialize(...))` or `Html.Encode` for that flow. | Active |

## Current Context Notes

- Production and local visual drift in the header/navbar can come from **stale local static assets** even when the Razor markup is identical.
- In this repository, **all local CSS, JS, and image assets referenced from Razor must use `~/...` plus `asp-append-version="true"`**.
- CDN assets (MDBootstrap, Font Awesome, Chart.js, Sentry) are external resources and are **not** covered by ASP.NET Core asset versioning.
- When a Razor view needs to send data to JavaScript, **do not** use `Html.Raw(Json.Serialize(...))` inside an `on*` attribute or inside a `<script>` block. Use `data-*` attributes with a delegated listener for per-item data, or a `<script type="application/json">` block read with `JSON.parse` for larger payloads. See [ADR 0002](adr/0002-safe-json-data-passing.md).

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
