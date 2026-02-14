# SauronSheet - AI Coding Instructions

## Project Overview
SauronSheet is a multi-user expense tracking web application that imports bank transactions from PDF statements, provides analytics, and generates spending reports.

**Stack:** .NET Core 10 (Razor Pages + C# backend), Supabase PostgreSQL, Tailwind CSS, JavaScript

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

## Spec-Driven Development
- Start with behavior specs in Application tests
- Define Mediator handlers to satisfy specs
- Build Domain entities to support handlers
- Implement Infrastructure to persist domain state
- Wire Frontend UI to trigger commands/queries
