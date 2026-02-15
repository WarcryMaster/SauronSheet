# SauronSheet - AI Coding Instructions

## 🔗 Quick Links to Planning Documents
* 📋 **Constitution**: See `.specify/memory/constitution.md` for 5 core principles & governance (v1.1.0).

---

## 📝 Project Overview
SauronSheet is a multi-user expense tracking web application that imports bank transactions from PDF statements, provides analytics, and generates spending reports.

* **Stack:** .NET Core 10 (Razor Pages + C# backend), Supabase PostgreSQL, Tailwind CSS, JavaScript.
* **Version:** Roadmap v1.1.0 (updated 2026-02-15) | Constitution v1.1.0 (amended 2026-02-15).

---

## 📅 Project Phases & Current Context
**Phase Execution Model:** 6 sequential phases (Phases 0-6).
**Current Phase:** TBD (Start with Phase 0: Foundation).
**MVP Launch:** End of Phase 4 (Week 18).
**Production Release:** End of Phase 6 (Week 24).

### Phase Scope Types
Each phase declares which layers are in scope. Deliverables **MUST NOT** cross layer boundaries.

| Phase Type | Layers In Scope | Example Phases |
| :--- | :--- | :--- |
| **Foundation** | All layers | Phase 0 |
| **Full-Stack (Auth)** | All layers | Phase 1 |
| **Domain-Only** | Domain | Phase 2 |
| **Full-Stack (Features)**| All layers | Phase 3, 4, 5 |
| **Polish** | Frontend + Infrastructure | Phase 6 |

---

## 📋 Specification Management (IMPORTANT)

### Single-File Rule
**ALL phase specifications MUST be in a single file: `specs/phase-X-spec.md`**

* ❌ **DO NOT CREATE:** `.clarification-*.md`, `.decisions-*.md`, `.resolved-*.md` or split spec documents.
* ✅ **DO:** Consolidate ALL information into ONE file. Update it incrementally and **declare layer scope explicitly** at the top.

### Structure Template for Phase Specs
```markdown
# Phase X: [Feature Name]
## Quick Reference
- Status: [Draft/Ready for Tasks]
- Layer Scope: [Domain-Only / Full-Stack / etc.]
## Critical Decisions
## Executive Summary (In Scope / Deferred)
## User Scenarios & Testing
## Requirements | Architecture | Deliverables | Test Specs | Success Criteria
```
🏛️ Architecture & Dependency Rules
Pattern: Clean Architecture + CQRS + Mediator Pattern.

**Directory Structure**
```
SauronSheet/
├── Frontend/                          # .NET Razor Pages + JS + Tailwind
│   ├── Pages/                         # Page handlers (.cshtml.cs) and views (.cshtml)
│   ├── Shared/                        # Layouts, partial views
│   ├── wwwroot/                       # Static assets (Tailwind CSS, JavaScript)
│   └── Program.cs                     # Startup configuration
│
├── Application/                       # Business logic orchestration (CQRS)
│   ├── Features/                      # Organized by feature
│   │   └── [Feature]/
│   │       ├── Commands/              # CreateXCommand.cs + handler
│   │       ├── Queries/               # GetXQuery.cs + handler
│   │       └── DTOs/                  # XDto.cs
│   └── Common/                        # Base handlers, IUserContext, pipeline behaviors
│
├── Domain/                            # Core business entities and rules
│   ├── Entities/                      # Transaction, Category, Budget (AggregateRoots)
│   ├── ValueObjects/                  # Money, DateRange, TransactionId, UserId, CategoryId
│   ├── Services/                      # CategoryService (cross-entity domain logic)
│   ├── Specifications/                # ISpecification<T>, filtering by date/category/amount
│   ├── Repositories/                  # Interfaces ONLY (ITransactionRepository, etc.)
│   ├── Exceptions/                    # DomainException, EntityNotFoundException
│   └── Common/                        # Base Entity, ValueObject abstractions
│
├── Infrastructure/                    # External integrations and persistence
│   ├── Persistence/                   # Supabase repository implementations
│   ├── Auth/                          # Supabase authentication (SupabaseAuthService)
│   └── PDF/                           # PDF parsing implementation
│
└── specs/                             # Phase specifications (ONE FILE PER PHASE)
```
**Non-Negotiable Dependency Rules**
Frontend → Application (via MediatR commands/queries).
Application → Domain (orchestrates domain logic via abstractions).
Infrastructure → Domain (implements domain contracts only).
Domain MUST NOT reference any other layer.
Application MUST NOT reference Infrastructure or Frontend directly.
Application accesses Infrastructure ONLY via Domain-defined interfaces.

**Core Workflows**

**PDF Import Pipeline:**
- Frontend: User uploads PDF.
- Application: Parse PDF via MediatR command (ImportTransactionsFromPdf).
- Domain: Validate transactions, apply business rules.
- Infrastructure: Persist to Supabase.
- Frontend: Refresh dashboard.

**Analytics:**
- Query layer in Application (MediatR queries: GetSpendingByCategoryQuery, GetMonthlyTrendQuery).
- Domain specifications filter transactions.
- Results formatted in Frontend Razor Pages.

---

## 💎 Key Patterns & Conventions

### Domain Layer Patterns

**Entities (Aggregate Roots):**
```csharp
// Parameterized constructors only, no public setters
// Throw DomainException on invariant violation
public class Transaction : AggregateRoot
{
    public TransactionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }
    public string? ImportedFrom { get; private set; }

    public Transaction(TransactionId id, UserId userId, Money amount, DateTime date, string description)
    {
        if (date > DateTime.UtcNow) throw new DomainException("Transaction date cannot be in the future.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Description is required.");
        // ... assign properties
    }
}
```

**Value Objects (Strong-Typed IDs + Business Values):**
```csharp
// Strong-typed IDs prevent mixing entity identifiers at compile time
public record TransactionId(Guid Value)
{
    public TransactionId() : this(Guid.Empty) =>
        throw new DomainException("TransactionId cannot be empty.");
}
public record UserId(string Value);
public record CategoryId(Guid Value);

// Business value objects encapsulate validation and arithmetic
public record Money(decimal Amount, string Currency = "EUR")
{
    public Money Plus(Money other) => /* ... */;
    public Money Minus(Money other) => /* ... */;
}
public record DateRange(DateTime StartDate, DateTime EndDate);
```

**Domain Services:**
```csharp
// Cross-entity logic that doesn't belong to a single aggregate
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepo;

    public async Task ValidateUniqueName(UserId userId, string name)
    {
        var existing = await _categoryRepo.FindByNameAndUserAsync(userId, name);
        if (existing is not null) throw new DomainException($"Category '{name}' already exists.");
    }

    public bool CanDeleteCategory(Category category, bool hasActiveTransactions)
        => !category.IsSystemDefault && !hasActiveTransactions;

    public IReadOnlyList<Category> GetSystemDefaults() => /* 4 default categories */;
}
```

**Guard Methods & System Defaults:**
```csharp
// Guard methods prevent invalid operations
public bool CanDelete() => !IsSystemDefault && !HasActiveTransactions;

// System defaults are immutable seeded values
public bool IsSystemDefault { get; private set; }
```

**Domain Patterns Quick Reference**
| Pattern         | Convention                                 | Example                       |
|----------------|--------------------------------------------|-------------------------------|
| Aggregate Root | Base class; parameterized constructor; no public setters | Transaction, Category, Budget |
| Value Object   | Immutable; value-based equality; validated on construction | Money, DateRange             |
| Strong-Typed ID| Wrapper around Guid/string; prevents ID mixing at compile time | TransactionId(Guid), UserId(string) |
| Domain Service | Cross-entity logic; depends on repository interfaces only | CategoryService               |
| Specification  | Filtering with domain language; MaxResults default 1000 | TransactionByDateRangeSpecification |
| Domain Exception| Thrown on invariant violation; caught in Application layer | DomainException               |
| Guard Method   | Returns bool to prevent invalid operations  | Category.CanDelete()          |
| System Default | Immutable seeded values; flagged with boolean property | Category.IsSystemDefault      |

---

## CQRS & MediatR
- Commands (state-changing): CreateExpense, ImportTransactionsFromPdf, UpdateBudget.
- Queries (read-only): GetExpensesByMonth, GetCategoryBreakdown, GetYearlyComparison.
- Dispatch: `await _mediator.Send(new GetSpendingByCategoryQuery(userId, month));`
- All requests routed through MediatR pipeline for consistency and middleware support.

---

## Supabase Integration (Infrastructure Only)
- Use Postgrest client in Infrastructure layer.
- Create PostgreSQL migrations for schema.
- Never expose Supabase directly to Application layer.
- Repository implementations live ONLY in Infrastructure/Persistence/.

---

## Frontend (Razor Pages)
- Each page has a PageModel handling GET/POST.
- MediatR calls in PageModel `OnGetAsync` / `OnPostAsync`.
- Pass data to views as Model.
- Use Alpine.js for interactivity; Tailwind for styling.

```csharp
// Example PageModel pattern
public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public DashboardModel(IMediator mediator) => _mediator = mediator;

    public SpendingByCategoryDto SpendingData { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst("sub")?.Value;
        SpendingData = await _mediator.Send(new GetSpendingByCategoryQuery(userId));
    }
}
```

---

## Authentication
- Supabase Auth for multi-user login (signup, login, logout, JWT refresh).
- JWT token stored in secure cookies.
- PageModel: Extract userId from `User.FindFirst("sub")`.
- All queries scoped to current user's tenant (enforced in handler, not UI).

---

## 🧪 Testing Strategy

### Testing Pyramid
| Level         | Scope                                | Tools         | When                              |
|--------------|--------------------------------------|---------------|-----------------------------------|
| Unit Tests   | Domain entities, VOs, domain services| xUnit + Moq   | Every phase with domain changes   |
| Integration  | Application handlers (mocked repos)  | xUnit + Moq + in-memory doubles | App layer scope phases |
| End-to-End   | API → Database round-trip            | xUnit + test Supabase instance  | Infrastructure scope phases |

### Coverage Requirements
| Scope                | Minimum Coverage | Notes                        |
|----------------------|------------------|------------------------------|
| Domain Layer (domain-only phases) | 100%             | When phase scope is Domain-only (e.g., Phase 2) |
| Domain Layer (global minimum)     | 80%              | Constitution minimum across all phases |
| Application Layer                 | 70%              | Handlers, pipeline behaviors, DTOs |

### Testing Rules
- Domain service tests MUST mock repository interfaces (not real databases).
- Write tests BEFORE implementation (Red-Green-Refactor).
- Tests serve as executable specification and regression prevention.

---

## ⚙️ Development Commands
```bash
# Build solution
dotnet build

# Run application (development)
dotnet run --project Frontend/

# Run all tests
dotnet test

# Run domain tests only
dotnet test --filter "Category=Domain"

# Run application tests only
dotnet test --filter "Category=Application"

# Build for production
dotnet publish -c Release
```

---

## 🚀 Deployment (Vercel — Free Tier)
- Deploy the Frontend project to Vercel.
- Environment variables (Supabase URL, API keys) configured in Vercel dashboard.
- Auto-deploy on push to main branch.

**Vercel Configuration (Frontend/vercel.json):**
```json
{
  "buildCommand": "dotnet publish -c Release -o ./out",
  "outputDirectory": "./out",
  "framework": "dotnet",
  "nodeVersion": "18.x"
}
```

**Environment Variables (Vercel Dashboard):**
```
Supabase__Url=https://your-project.supabase.co
Supabase__Key=your-public-anon-key
```

---

## ⚠️ Common Pitfalls to Avoid
- ❌ Application referencing Infrastructure directly (use Domain interfaces).
- ❌ Domain logic in MediatR handlers (keep handlers as thin orchestrators).
- ❌ Mixing query/command logic (separate concerns strictly).
- ❌ Supabase client leaking into Application layer.
- ❌ Public setters on domain entities (use parameterized constructors).
- ❌ Raw Guid or string for entity IDs (MUST use strong-typed value objects).
- ❌ Creating multiple spec files for same phase (consolidate into ONE).
- ❌ Splitting spec info across .clarification-, .decisions-, .resolved- files.
- ❌ Deliverables crossing declared phase layer boundaries.
- ❌ Domain events without IDomainEvent base infrastructure (verify Phase 0 first).

---

## 📜 Constitutional Compliance (Non-Negotiable)
5 Core Principles (see .specify/memory/constitution.md v1.1.0 for full details):

- **Clean Architecture & Layered Dependencies** — No upward layer references; Application accesses Infrastructure via Domain interfaces only.
- **CQRS + MediatR Pattern** — Commands for state-changing ops, Queries for read-only; all routed through MediatR pipeline.
- **Domain-Driven Design** — Strong-typed IDs mandatory; domain services for cross-entity logic; system defaults pattern; guard methods; repositories abstract persistence via specifications.
- **Test-First Development (NON-NEGOTIABLE)** — Write tests before code; 100% domain coverage for domain-only phases; 80% domain global; 70% application minimum.
- **Spec-Driven Development** — Single-file rule per phase; phase scope boundaries enforced; out-of-scope deliverables are constitution violations.

**Critical Rules:**
- ❌ Domain/Application MUST NOT reference Infrastructure or Frontend directly.
- ❌ No direct Supabase client calls outside Infrastructure layer.
- ❌ All queries must be scoped to current user's tenant.
- ❌ Raw Guid/string for entity IDs (MUST use strong-typed value objects).
- ❌ Deliverables crossing declared phase layer boundaries.
- ✅ Every Command/Query requires MediatR handler + integration test.
- ✅ Domain invariants prevent invalid states (proven in unit tests).
- ✅ Entities use parameterized constructors; no public setters.
- ✅ System defaults flagged with boolean property; protected by guard methods.

---

## 🔄 Spec-Driven Development Workflow
1. **Write Test Spec** — Behavior spec in tests (TDD red phase).
2. **Define Handler** — Create MediatR Command/Query handler stub.
3. **Build Domain** — Implement entities/value objects to satisfy requirements.
4. **Implement Persistence** — Add Infrastructure repository implementations.
5. **Wire UI** — Create Razor Pages / Alpine.js to trigger operations.

**Phase Scope Enforcement:**
- Steps 2, 4, 5 are ONLY executed if the phase spec declares those layers in scope.
- For Domain-only phases (e.g., Phase 2): execute steps 1 and 3 only.
- Out-of-scope work is documented as "deferred to Phase X" in the spec.

---

_Last Updated: 2026-02-15 | Version: 1.2.0 (Aligned with Constitution v1.1.0)_