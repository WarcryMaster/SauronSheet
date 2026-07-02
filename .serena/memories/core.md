# SauronSheet

Multi-user expense tracking app. .NET 10 + Clean Architecture + CQRS/MediatR + DDD.
Supabase (PostgreSQL) backend, Supabase Auth. Sentry for all observability.
Razor Pages frontend with MDBootstrap 5 + Alpine.js + HTMX + Chart.js (all CDN-only).

## Source map

`src/SauronSheet.Domain/` — Zero-dependency domain: Entities, ValueObjects, Repositories, Specifications, Services
`src/SauronSheet.Application/` — CQRS handlers, pipeline behaviors, IUserContext
`src/SauronSheet.Infrastructure/` — Supabase persistence, Excel parsing, Sentry config, Supabase auth
`src/SauronSheet.Frontend/` — ASP.NET Razor Pages, MDBootstrap UI, page models

`tests/` — xUnit + Moq (unit + integration), Playwright (E2E)
`e2e/` — Playwright E2E test specs
`supabase/` — Supabase migrations + seed

## Key rules (from AGENTS.md)

- All conversation with AI in neutral Spanish; code, ids, traces in English
- SDD workflow: Spec -> Plan -> Task -> Implement
- No `var` — explicit types always
- Sentry for ALL logging (no Console.WriteLine / Debug / Trace)
- E2E test coupling: every frontend change MUST update affected E2E tests
- Domain layer: 80% coverage min; Application: 70%

## Structure

- See `mem:tech_stack` for versions and build tools
- See `mem:conventions` for code patterns
- See `mem:suggested_commands` for dev workflow
- See `mem:task_completion` for verification commands
