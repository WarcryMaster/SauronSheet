# Implementation Plan: Category Management (Feature 2)

**Branch**: `002-category-management` | **Date**: March 7, 2026 | **Spec**: [specs/002-category-management/spec.md](specs/002-category-management/spec.md)
**Input**: Feature specification from `/specs/002-category-management/spec.md` with 5 resolved design clarifications

**Status**: Ready for Phase 1 Design + Task Generation

## Summary

**MVP Category Management System** with complete CRUD operations for custom categories + 24 immutable system defaults organized in 6 groups (Income, Fixed Expenses, Variable Expenses, Lifestyle, Finance & Other). Full-stack implementation using Clean Architecture + CQRS/MediatR pattern. Hybrid validation (Domain ValueObjects + Domain Service + Application handlers + Frontend defense-in-depth). Delete guarded by transaction count check via Domain Service pattern.

**Technical Approach**: 
- **Domain**: CategoryId, CategoryName, ColorHex ValueObjects + CategoryService for cross-entity logic (ValidateUniqueName, CanDeleteCategory)
- **Application**: 5 MediatR handlers (GetAll, Search, Create, Update, Delete) + CategoryDto
- **Infrastructure**: SupabaseCategoryRepository with SQL migration seeding 24 system defaults
- **Frontend**: Razor Pages (/Categories) with MDBootstrap form + HTML5 color picker + Bootstrap icon selector

## Technical Context

**Language/Version**: C# / .NET Core 10 (LTS)
**Primary Dependencies**: MediatR 12+, Postgrest Client (Supabase), xUnit, Moq  
**Storage**: Supabase PostgreSQL with database triggers for CreatedAt/UpdatedAt management  
**Testing**: xUnit (Domain: 100% coverage required for Phase 2 Full-Stack scope) + Moq for repository mocking  
**Target Platform**: Web (.NET/Razor Pages backend)
**Project Type**: Full-Stack (Backend CQRS + Frontend Razor Pages)  
**Performance Goals**: <100ms for List query (with 100 custom categories), <500ms for Create/Update/Delete including persistence  
**Constraints**: 24 system defaults immutable; Category names unique per user; Delete blocked if transactions exist; Multi-tenancy enforced per UserId  
**Scale/Scope**: Full Category CRUD lifecycle; 24 predefined categories seeded; supports unlimited custom categories per user

## Constitution Check

**GATE: Phase 2 Full-Stack Completion (must pass before task generation)**

| Principle | Status | Details |
|-----------|--------|---------|
| **Clean Architecture Layering** | ✅ PASS | Domain → Application → Infrastructure; no upward refs. Repository pattern via Domain interfaces. |
| **CQRS + MediatR** | ✅ PASS | 5 handlers specified (GetAll, Search, Create, Update, Delete). All routed through mediator. |
| **Domain-Driven Design** | ✅ PASS | Strong-typed IDs (CategoryId), ValueObjects (CategoryName, ColorHex), Domain Service (CategoryService), Guard methods (CanDeleteCategory). |
| **Test-First Development** | ✅ PASS | 100% Domain coverage required (Phase 2 Full-Stack). Tests prove spec compliance before implementation. |
| **Spec-Driven Development** | ✅ PASS | Single-file spec (spec.md). Phase 2 scope: Full-Stack (all layers). Clarifications resolved (5 decisions). |
| **System Defaults Pattern** | ✅ PASS | 24 system categories marked IsSystemDefault=true, seeded via SQL migration, guarded from modification/deletion. |
| **Hybrid Validation Architecture** | ✅ PASS | Clarification #1 (D): Domain ValueObjects + Domain Service + Application handlers + Frontend validation (defense-in-depth). |
| **Tenant Scoping** | ✅ PASS | All queries filtered by UserId; enforced in MediatR handlers + repository contracts. System categories global but filtered per user in UI. |

**No violations detected. Feature ready for implementation task generation.**

## Project Structure

### Documentation (this feature)

```text
specs/002-category-management/
├── spec.md              # Feature specification (complete, clarified)
├── plan.md              # This file (implementation planning)
├── data-model.md        # Phase 1 design artifact (GENERATED)
├── quickstart.md        # Phase 1 developer guide (GENERATED)
├── research.md          # Phase 0 research (GENERATED - minimal, clarifications done)
└── contracts/           # Phase 1 API contracts (GENERATED)
    ├── category-commands.schema.json  # CreateCategoryCommand, UpdateCategoryCommand, DeleteCategoryCommand
    ├── category-queries.schema.json   # GetAllCategoriesQuery, SearchCategoriesQuery
    └── category-dto.schema.json       # CategoryDto response format
```

### Source Code (repository root) – OPTION 3: Full-Stack .NET (Multi-layer)

```text
src/
├── SauronSheet.Domain/                        # Layer: Domain (entities, VOs, services, interfaces)
│   ├── Entities/
│   │   └── Category.cs                        # AggregateRoot with CanDelete() guard method
│   ├── ValueObjects/
│   │   ├── CategoryId.cs                      # Strong-typed ID (Guid wrapper)
│   │   ├── CategoryName.cs                    # Validated: 1-50 chars, non-empty
│   │   ├── ColorHex.cs                        # Validated: regex #[0-9A-F]{6}
│   │   └── CategoryType.cs                    # Enum: Income | Expense
│   ├── Services/
│   │   └── CategoryService.cs                 # Domain Service: ValidateUniqueName(), CanDeleteCategory(), GetSystemDefaults()
│   ├── Repositories/
│   │   └── ICategoryRepository.cs             # Interface ONLY: GetByIdAsync, GetByUserIdAsync, FindByNameAndUserAsync, AddAsync, UpdateAsync, DeleteAsync, GetCountAsync
│   ├── Specifications/
│   │   └── (Uses existing ISpecification<T> base)
│   └── Exceptions/
│       └── (Uses existing DomainException, EntityNotFoundException)
│
├── SauronSheet.Application/                   # Layer: Application (CQRS orchestration)
│   ├── Features/
│   │   └── Categories/
│   │       ├── Commands/
│   │       │   ├── CreateCategoryCommand.cs
│   │       │   ├── CreateCategoryCommandHandler.cs
│   │       │   ├── UpdateCategoryCommand.cs
│   │       │   ├── UpdateCategoryCommandHandler.cs
│   │       │   ├── DeleteCategoryCommand.cs
│   │       │   └── DeleteCategoryCommandHandler.cs
│   │       ├── Queries/
│   │       │   ├── GetAllCategoriesQuery.cs
│   │       │   ├── GetAllCategoriesQueryHandler.cs
│   │       │   ├── SearchCategoriesQuery.cs
│   │       │   └── SearchCategoriesQueryHandler.cs
│   │       └── DTOs/
│   │           └── CategoryDto.cs             # API/Frontend response: Id, Name, Type, Color, IconName, IsSystemDefault, CreatedAt, UpdatedAt
│   └── Common/
│       └── (Existing: pipeline behaviors, tenant scoping)
│
├── SauronSheet.Infrastructure/                # Layer: Infrastructure (persistence, auth)
│   ├── Persistence/
│   │   ├── SupabaseCategoryRepository.cs      # Implements ICategoryRepository using Postgrest
│   │   ├── Migrations/
│   │   │   └── 20260307_SeedSystemDefaultCategories.sql  # Creates categories table + inserts 24 system defaults
│   │   └── (Existing: TransactionRepository, BudgetRepository etc.)
│   └── (Existing: Auth/, PDF/, Monitoring/)
│
└── SauronSheet.Frontend/                      # Layer: Frontend (Razor Pages + UI)
    ├── Pages/
    │   ├── Categories.cshtml
    │   └── Categories.cshtml.cs                # PageModel handler: OnGetAsync(), OnPostCreateAsync(), OnPostUpdateAsync(), OnPostDeleteAsync()
    ├── Shared/
    │   └── (Existing: _Layout.cshtml with MDBootstrap CDN)
    └── wwwroot/
        └── (Existing: static assets, CSS, JS)

tests/
├── SauronSheet.Domain.Tests/
│   └── Categories/
│       ├── CategoryTests.cs                    # Unit: Category entity invariants, CanDelete() logic
│       ├── CategoryNameTests.cs                # Unit: CategoryName validation (1-50 chars, non-empty)
│       ├── ColorHexTests.cs                    # Unit: ColorHex regex validation
│       ├── CategoryServiceTests.cs             # Unit: ValidateUniqueName(), CanDeleteCategory() with mocked repository
│       └── CategoryServiceSystemDefaultsTests.cs  # Unit: GetSystemDefaults() returns 24 correct categories
│
└── SauronSheet.Application.Tests/
    └── Features/Categories/
        ├── CreateCategoryCommandHandlerTests.cs       # Integration: validates via service, persists, returns CategoryId
        ├── UpdateCategoryCommandHandlerTests.cs       # Integration: prevents Type/IsSystemDefault changes, validates uniqueness
        ├── DeleteCategoryCommandHandlerTests.cs       # Integration: queries transaction count, guards via service, throws if blocked
        ├── GetAllCategoriesQueryHandlerTests.cs       # Integration: returns system + custom categories, sorted correctly
        └── SearchCategoriesQueryHandlerTests.cs       # Integration: case-insensitive name filtering
```

**Structure Decision**: Option 3 (Full-Stack .NET Multi-layer) selected because:
- **Existing Architecture**: SauronSheet already uses layered structure with Domain/Application/Infrastructure/Frontend separation
- **Aligns with Constitution**: Clean Architecture + CQRS pattern enforced across existing phases
- **Feature Scope**: Full-Stack Phase 2 deliverable (all layers required)
- **Test Organization**: Domain tests in dedicated project; Application integration tests in separate project
- **Maintainability**: Clear separation of concerns; repository implementation hidden from Domain
- **Scalability**: Easy to add new commands/queries to Categories feature without refactoring structures

## Complexity Tracking

**Status**: No violations detected. All design decisions clarified and Constitution-compliant. Complexity is managed through explicit layering and CQRS pattern.

**Note**: This section tracks exceptional cases when Constitution principles must be violated. Feature 2 requires no exceptions or justifications.

---

## Phase 0: Outline & Research

**Status**: ✅ COMPLETE — All 5 clarifications resolved in preceding clarification session (March 7, 2026).

**Key Findings**:
1. **Validation Architecture** (Clarification #1): Hybrid Pattern — Domain ValueObjects + Domain Service + Application handlers + Frontend
2. **CQRS Handlers** (Clarification #2): 5 handlers (GetAll, Search, Create, Update, Delete)
3. **Delete Guard Logic** (Clarification #3): Hybrid — Application EXISTS query + Domain Service guard
4. **Seeding Strategy** (Clarification #4): SQL Migration — 24 INSERT statements in source control
5. **Color Hex Validation** (Clarification #5): Defense-in-Depth — Frontend picker + Domain regex validation

**Research Artifacts**: None generated (clarifications supersede research phase). Proceed to Phase 1 Design.

---

## Phase 1: Design & Contracts

**Deliverables**: Core design artifacts generated by following sections

### 1. Domain Model (`data-model.md`)

**Entities (Aggregate Roots)**:
- `Category(CategoryId, UserId, CategoryName, CategoryType, ColorHex, string IconName, bool IsSystemDefault, DateTime CreatedAt, DateTime UpdatedAt)`
  - Constructor: Parameterized only; validates invariants; throws DomainException on violation
  - Methods: `CanDelete(bool hasTransactions): bool` — Returns false if IsSystemDefault OR hasTransactions; true otherwise

**Value Objects**:
- `CategoryId(Guid value)` — Strong-typed ID; prevents ID mixing at compile time
- `CategoryName(string value)` — 1-50 chars, non-empty after trim; throws on invalid
- `ColorHex(string value)` — Regex `#[0-9A-F]{6}`, immutable hex color; throws on invalid
- `CategoryType` — Enum: Income (0) | Expense (1)
- `UserId(string value)` — Tenant isolation

**Domain Service**:
- `CategoryService`
  - `ValidateUniqueName(UserId userId, string name): Task` — Queries repository; throws DomainException if duplicate
  - `CanDeleteCategory(Category cat, bool hasTransactions): bool` — Guard logic
  - `GetSystemDefaults(): IReadOnlyList<Category>` — Returns 24 fixed categories

**Repository Interface** (Domain layer):
- `ICategoryRepository`
  - `GetByIdAsync(CategoryId): Task<Category>`
  - `GetByUserIdAsync(UserId): Task<List<Category>>`
  - `FindByNameAndUserAsync(UserId, string name): Task<Category?>`
  - `AddAsync(Category): Task`
  - `UpdateAsync(Category): Task`
  - `DeleteAsync(CategoryId): Task`
  - `GetCountAsync(CategoryId): Task<int>` — Transaction count for delete guard

**Exceptions**:
- `DomainException` — Invariant violations (duplicate name, cannot delete IsSystemDefault)
- `EntityNotFoundException` — Category not found

### 2. CQRS Handlers (`contracts/.schema.json`)

**Commands**:
- `CreateCategoryCommand { UserId, Name, Type, Color, IconName }: IRequest<CategoryId>`
  - Handler validates via CategoryService, creates entity, persists; returns new CategoryId
- `UpdateCategoryCommand { CategoryId, Name, Color, IconName }: IRequest<Unit>`
  - Handler prevents Type/IsSystemDefault changes; validates uniqueness; persists
- `DeleteCategoryCommand { CategoryId }: IRequest<Unit>`
  - Handler queries transaction count, calls CategoryService.CanDeleteCategory(), deletes or throws

**Queries**:
- `GetAllCategoriesQuery { UserId }: IRequest<List<CategoryDto>>`
  - Returns system + custom categories grouped by Type, sorted alphabetically
- `SearchCategoriesQuery { UserId, searchTerm }: IRequest<List<CategoryDto>>`
  - Case-insensitive name filtering

**DTOs**:
- `CategoryDto { CategoryId, string Name, CategoryType Type, string Color, string IconName, bool IsSystemDefault, DateTime CreatedAt, DateTime UpdatedAt }`

### 3. Infrastructure Persistence

**Repository Implementation**: `SupabaseCategoryRepository : ICategoryRepository`
- Maps `CategoryRow` (Postgrest DTO) ↔ `Category` (Domain entity)
- Uses `FromDomainForInsert()` to exclude CreatedAt/UpdatedAt (database triggers manage)
- Methods implement all ICategoryRepository contracts

**Database Migration**: `20260307_SeedSystemDefaultCategories.sql`
```sql
-- Creates categories table (if not exists)
-- Inserts 24 rows with IsSystemDefault=true:
--   Income: Salary, Sales, Investments, Gifts, Other Income
--   Fixed Expenses: Housing, Utilities, Insurance, Subscriptions, Education
--   Variable Expenses: Groceries, Transportation, Personal Care, Home, Pets
--   Lifestyle: Restaurants, Entertainment, Shopping, Travel, Health & Wellness
--   Finance & Other: Debt Payments, Savings & Investment, Donations, Unexpected Expenses
-- Idempotent: IF NOT EXISTS logic prevents duplicate inserts on redeploy
```

### 4. Frontend Implementation

**Page**: `/Categories` (Razor Pages + MediatR)
- Route: `/Categories`
- Handler: `CategoriesModel : PageModel`
  - `OnGetAsync()` — Send GetAllCategoriesQuery; bind results to view
  - `OnPostCreateAsync()` — Bind form; send CreateCategoryCommand; handle DomainException errors
  - `OnPostUpdateAsync()` — Send UpdateCategoryCommand; validate form
  - `OnPostDeleteAsync()` — Send DeleteCategoryCommand; handle guard errors gracefully

**UI Components** (MDBootstrap via CDN):
- Category list view with grouping chips (Income/Expense)
- System vs. Custom badges using IsSystemDefault flag
- Color picker (HTML5 `<input type="color">`) for hex input
- Icon selector dropdown (Bootstrap icon library)
- Form validation feedback (error messages from DomainException)
- Disabled Edit/Delete buttons for system categories

---

## Phase 2: Tasks & Execution

**Status**: Ready for task generation via `/speckit.tasks`

**Task Generation**: Run `/speckit.tasks` to create `tasks.md` with:
- Dependency-ordered task list (Domain → Infrastructure → Application → Frontend)
- Subtitle, description, effort estimate, blockages per task
- Test validation criteria for each task integration point

**Expected Task Breakdown** (~18-22 tasks):
- **Domain Layer** (7-8 tasks): Category entity + ValueObjects + CategoryService + ICategoryRepository + unit tests
- **Infrastructure Layer** (2-3 tasks): SupabaseCategoryRepository impl + SQL migration + integration tests
- **Application Layer** (4-5 tasks): 5 MediatR handlers (Create/Update/Delete/GetAll/Search) + DTOs + integration tests
- **Frontend Layer** (3-4 tasks): Categories.cshtml.cs Page handler + form UI + form validation + error handling

**Execution Sequence**:
1. Domain layer (entities, services, interfaces) + unit tests
2. Infrastructure layer (repository impl, migration) + integration tests
3. Application layer (handlers, DTOs) + integration tests
4. Frontend layer (Pages, UI, validation) + E2E validation

**Success Criteria**:
- All 24 system default categories seed correctly on database initialization
- Create/Read/Update/Delete operations work end-to-end via MediatR pipeline
- Delete guard prevents deletion of categories with transactions
- Name uniqueness validated across users (tenant isolation verified)
- Frontend validation matches Domain validation rules (defense-in-depth)
- 100% Domain layer test coverage maintained
- ≥80% Application layer test coverage achieved
