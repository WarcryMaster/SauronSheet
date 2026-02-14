# SauronSheet - AI Coding Instructions

## Quick Links to Planning Documents

📚 **Planning Hub**: See `.specify/memory/master-index.md` for complete documentation index  
📋 **Constitution**: See `.specify/memory/constitution.md` for 5 core principles & governance (v1.0.0)  
🚀 **Roadmap**: See `.specify/memory/project-roadmap.md` for 6-phase execution plan  
✅ **Execution**: See `.specify/memory/execution-checklist.md` for daily tasks & phase checklists  
📊 **Timeline**: See `.specify/memory/visual-roadmap.md` for Gantt charts, critical path, risks

## Project Overview
SauronSheet is a multi-user expense tracking web application that imports bank transactions from PDF statements, provides analytics, and generates spending reports.

**Stack:** .NET Core 10 (Razor Pages + C# backend), Supabase PostgreSQL, Tailwind CSS, JavaScript  
**Version**: Roadmap v1.0.0 (created 2026-02-14) | Constitution v1.0.0 (ratified 2026-02-14)

## Project Phases & Current Context

**Phase Execution Model**: 6 sequential phases (Phases 0-6), each with specific deliverables and exit criteria.  
**Current Phase**: TBD (start with Phase 0: Foundation)  
**MVP Launch**: End of Phase 4 (Week 18) — Includes PDF upload + analytics dashboard  
**Production Release**: End of Phase 6 (Week 24)

For current phase objectives, consult `.specify/memory/project-roadmap.md` and `.specify/memory/execution-checklist.md`.

---

## 📋 Specification Management (IMPORTANT)

### Single-File Rule
**ALL phase specifications MUST be in a single file: `specs/phase-X-spec.md`**

❌ **DO NOT CREATE**:
- `.clarification-*.md` files
- `.decisions-*.md` files  
- `.resolved-*.md` files
- Duplicate/split spec documents

✅ **DO**:
- Consolidate ALL information into ONE `specs/phase-X-spec.md` file
- Include clarifications, decisions, user stories, requirements, architecture in same file
- Use clear section headers to organize content
- Update single file incrementally as spec evolves

### Why Single File?
- No information redundancy
- Single source of truth
- Easier to track changes
- Clear narrative from user stories → implementation

### Structure Template for Phase Specs
```markdown
# Phase X: [Feature Name]

## Quick Reference
- Status: [Draft/Ready for Tasks]
- Duration: X-Y weeks
- Dependencies: Phase X-1

## Critical Decisions (if any)
| Decision | Answer | Rationale |
| ... |

## Executive Summary
...

## User Scenarios & Testing
### Story 1: ...

## Requirements
...

## Architecture & Design
...

## Deliverables
...

## Test Specifications
...

## Success Criteria
...

## Next Phase
...
```

---

## Architecture

### Clean Architecture + CQRS + Mediator Pattern
```
SauronSheet/
├── Frontend/               # .NET Razor Pages + JS + Tailwind
├── Application/            # CQRS commands/queries, business logic orchestration
├── Domain/                 # Entities, value objects, domain services, specifications
└── Infrastructure/         # Data access, external services, Supabase integration
```

**Dependency Rules:**
- Frontend → Application (mediator queries/commands)
- Application → Domain (orchestrates domain logic)
- Infrastructure → Domain (implementations only)
- **NO upward dependencies** (Domain/Application never reference Infrastructure/Frontend)

### Core Workflows

**PDF Import Pipeline:**
1. Frontend: User uploads PDF
2. Application: Parse PDF (MediatR command `ImportTransactionsFromPdf`)
3. Domain: Validate transactions, apply business rules
4. Infrastructure: Persist to Supabase
5. Frontend: Refresh dashboard

**Analytics:**
- Query layer in Application (MediatR queries: `GetSpendingByCategoryQuery`, `GetMonthlyTrendQuery`)
- Domain specifications filter transactions
- Results formatted in Frontend Razor controllers

## Key Patterns & Conventions

### CQRS Structure
- **Commands:** `CreateExpense`, `ImportTransactionsFromPdf`, `UpdateBudget`
- **Queries:** `GetExpensesByMonth`, `GetCategoryBreakdown`, `GetYearlyComparison`
- Use MediatR for dispatch: `await _mediator.Send(new GetSpendingByCategoryQuery(userId, month))`

### Entities (Domain Layer)
```csharp
// User, Transaction, Category, Budget
// Each with domain logic (IsOverBudget, CalculateBalance, etc.)
```

### Supabase Integration (Infrastructure)
- Use Postgrest client in Infrastructure layer
- Create PostgreSQL migrations for schema
- Never expose Supabase directly to Application layer

### Frontend (Razor Pages)
- Each page has a PageModel handling GET/POST
- MediatR calls in PageModel constructor or OnGetAsync
- Pass data to views as `Model`
- Use Alpine.js for interactivity; Tailwind for styling

### Authentication
- Supabase Auth for multi-user login
- JWT token in session/cookies
- PageModel: Extract userId from User.FindFirst("sub")

## Important Directories
- `Domain/` → Entities, ValueObjects, DomainServices, Specifications
- `Application/Features/` → Organized by feature (Transactions/, Analytics/, etc.)
- `Infrastructure/Persistence/` → Supabase repository implementations
- `Frontend/Pages/` → Razor Pages (.cshtml + .cshtml.cs)
- `Frontend/wwwroot/` → CSS (Tailwind output), JS
- `specs/` → Phase specifications (ONE FILE PER PHASE, consolidated)

## Development Commands
```bash
dotnet build
dotnet run --project Frontend/
dotnet test                    # Run domain/application tests
```

## Deployment

### Vercel Hosting (Free Tier)
- Deploy the Frontend project to Vercel
- Vercel supports .NET Core with `vercel.json` configuration
- Environment variables (Supabase URL, API keys) configured in Vercel dashboard
- Auto-deploy on push to main branch
- See `vercel.json` in Frontend/ for build settings

## Testing Strategy
- Unit tests for Domain entities & value objects
- Integration tests for Application handlers (mock Supabase)
- Keep Infrastructure testable with interfaces

## Common Pitfalls to Avoid
- ❌ Application layer referencing Infrastructure directly (use interfaces)
- ❌ Domain logic in handlers (keep handlers thin orchestrators)
- ❌ Mixing query/command logic (separate concerns)
- ❌ Supabase client leaking into Application layer
- ❌ **Creating multiple spec files for same phase** (consolidate into ONE)
- ❌ **Splitting spec information across `.clarification-`, `.decisions-`, `.resolved-` files** (keep in main spec)

## Constitutional Compliance (Non-Negotiable)

**5 Core Principles** (see `.specify/memory/constitution.md` for full details):

1. **Clean Architecture & Layered Dependencies** — No upward layer references; Frontend → Application → Domain; Infrastructure → Domain only
2. **CQRS + MediatR Pattern** — Commands for state-changing ops, Queries for read-only; all routed through MediatR
3. **Domain-Driven Design** — Core business logic in entities/value objects; repositories abstract persistence
4. **Test-First Development (NON-NEGOTIABLE)** — Write tests before code; 80% Domain, 70% Application coverage minimum
5. **Spec-Driven Development** — Specifications in tests, implementation follows specs

**Critical Rules**:
- ❌ Domain/Application never reference Infrastructure or Frontend directly
- ❌ No direct Supabase client calls outside Infrastructure layer
- ❌ All queries must be scoped to current user's tenant
- ❌ Creating redundant specification files (ONE file per phase)
- ✅ Every Command/Query requires MediatR handler + integration test
- ✅ Domain invariants prevent invalid states (proven in unit tests)

## Spec-Driven Development Workflow

1. **Write Test Spec** — Behavior spec in Application tests (TDD)
2. **Define Handler** — Create MediatR Command/Query handler stub
3. **Build Domain** — Implement entities/value objects to satisfy handler
4. **Implement Persistence** — Add Infrastructure repository implementations
5. **Wire UI** — Create Razor Pages/commands to trigger operations

For Phase X tasks, always consult `.specify/memory/execution-checklist.md` Phase X section.

---

**Last Updated**: 2026-02-14  
**Version**: 1.1.0 (Added spec consolidation rules)
