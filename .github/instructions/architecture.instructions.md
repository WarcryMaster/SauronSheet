---
description: "Use when designing, reviewing, or refactoring architecture, entity design, CQRS handlers, domain services, database migrations, or authentication. Covers Clean Architecture, DDD, CQRS, Supabase integration, and auth rules."
---

# Architecture Rules

Apply these rules when making architectural decisions, writing domain entities, handlers, or infrastructure code.

---

## Clean Architecture (Mandatory)

- Unidirectional dependencies: Frontend -> Application -> Domain.
- Infrastructure -> Domain only.
- Domain must NOT reference Application, Infrastructure, or Frontend.
- Application must NOT reference Infrastructure or Frontend directly.

---

## CQRS + MediatR

- Commands for state-changing operations (CreateExpense, ImportTransactionsFromPdf, UpdateBudget).
- Queries for read-only operations (GetExpensesByMonth, GetCategoryBreakdown).
- Dispatch via `await _mediator.Send(command)`.
- All requests routed through MediatR pipeline for consistency and middleware support.
- Handlers are thin orchestrators — keep domain logic in Domain layer.

---

## Domain-Driven Design

- Strong-typed IDs mandatory (TransactionId, UserId). Never raw Guid/string.
- Immutability and explicit invariants required in the Domain.
- Domain services for cross-entity logic (CategoryService, BudgetService).
- Repositories abstract persistence via specifications.
- System defaults flagged with boolean property (`IsSystemDefault`), protected by guard methods.
- Entities use parameterized constructors; no public setters.

---

## Supabase Integration (Infrastructure Only)

- Use Postgrest client in Infrastructure layer only.
- Never expose Supabase directly to Application or Domain layers.
- Repository implementations live ONLY in `Infrastructure/Persistence/`.
- All queries scoped to current user's tenant (enforced in handler, not UI).

---

## Authentication

- Supabase Auth for multi-user login (signup, login, logout, JWT refresh).
- JWT token stored in secure HttpOnly cookies.
- PageModel: extract userId from `User.FindFirst("sub")`.
- Never trust client-side userId from form inputs.

---

## Database Migrations (Supabase CLI)

- All migrations live in `supabase/migrations/` — this is the single source of truth.
- Naming convention: `YYYYMMDDHHMMSS_descriptive_name.sql` (sequential timestamps).
- Never modify migration files after they have been applied to production.
- To create a new migration: `supabase migration new <name>`, then write SQL in the generated file.
- To apply migrations locally: `supabase migration up`.
- To apply migrations to production: `supabase db push --linked` (automated in CI/CD).
- To reset local database: `supabase db reset`.
- CI/CD runs `supabase db push --linked` automatically on deploy (GitHub Actions).
- Required GitHub Actions secrets: `SUPABASE_ACCESS_TOKEN`.
- Never put migration files in `src/` — they belong in `supabase/migrations/`.
- If you see `PGRST205` errors about missing tables, a migration was not applied. Check `supabase/migrations/` and apply it.
