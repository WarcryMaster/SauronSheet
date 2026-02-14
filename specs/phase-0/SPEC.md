# SauronSheet Phase 0: Foundation & Infrastructure Setup

**Version**: 1.0.0  
**Duration**: 2-3 weeks  
**Status**: ⏳ BLOCKED (awaiting execution)  
**Created**: 2026-02-14

---

## Executive Summary

Phase 0 establishes the foundational architecture for SauronSheet by creating a 4-layer Clean Architecture solution with MediatR CQRS pattern, comprehensive test infrastructure, and CI/CD pipeline. This phase is **non-functional** (no user features) but **critical**: all subsequent phases depend on these foundations.

**Success Metric**: 11 tests passing, 0 compiler warnings, CI/CD pipeline green.

---

## Goal

Establish a robust, testable architecture foundation that enforces:
- Clean Architecture principles (4-layer separation)
- CQRS + MediatR pattern (Command/Query handlers)
- Test-First Development (11 unit/integration tests)
- Multi-tenancy readiness (ScopedQueryBehavior middleware)
- Infrastructure-agnostic domain layer

---

## Requirements

### Functional Requirements

None (foundation phase has no user-facing features)

### Non-Functional Requirements

| Requirement | Description | Test Evidence |
|-------------|-------------|-----------------|
| **NF-001: Clean Architecture** | 4-layer structure with unidirectional dependencies | `dotnet build` compiles, no upward refs |
| **NF-002: CQRS Pattern** | Command/Query handlers via MediatR | Example CreateCategoryCommand + GetCategoriesQuery execute |
| **NF-003: Test-First** | 11 tests written before implementation | `dotnet test` passes all 11 tests |
| **NF-004: Multi-Tenancy** | ScopedQueryBehavior validates tenant isolation via ITenantScoped marker | T00-008: ScopedQueryBehavior rejects cross-tenant ITenantScoped responses |
| **NF-005: Exception Hierarchy** | Domain exceptions for strong typing | T00-006: DomainException throws/catches correctly |
| **NF-006: Pagination Default** | ISpecification<T> enforces 1000-row limit | T00-007: ISpecification enforces MaxResults = 1000 |
| **NF-007: CI/CD Pipeline** | GitHub Actions builds + tests on push | GitHub Actions workflow green |
| **NF-008: DI Container** | MediatR + IUserContext resolvable | T00-002, T00-009: Handler resolution + context injection |

---

## Architecture

### 4-Layer Structure

```
Frontend (Console/Web UI)
    ↓ (depends on)
Application (MediatR Handlers, Business Logic Orchestration)
    ↓ (depends on)
Domain (Entities, Value Objects, Specifications)
    ↓ (depends on)
Infrastructure (Persistence, External Services)

Dependency Rule: Upward = FORBIDDEN. Domain = ZERO external dependencies.
```

### Project Layout

```
SauronSheet/
├── src/
│   ├── Domain/
│   │   ├── Common/
│   │   │   ├── Entity.cs (abstract)
│   │   │   ├── ValueObject.cs (abstract)
│   │   │   ├── DomainException.cs
│   │   │   └── IRepository.cs
│   │   ├── Exceptions/
│   │   │   ├── EntityNotFoundException.cs
│   │   │   └── ValueObjectValidationException.cs
│   │   ├── Specifications/
│   │   │   └── ISpecification<T>.cs
│   │   └── Domain.csproj
│   ├── Application/
│   │   ├── Common/
│   │   │   ├── IUserContext.cs (interface)
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── ScopedQueryBehavior.cs ⭐
│   │   │   ├── Examples/
│   │   │   │   ├── CreateCategoryCommand.cs
│   │   │   │   └── GetCategoriesQuery.cs
│   │   │   └── Dto/
│   │   │       └── BaseDto.cs
│   │   └── Application.csproj
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── SupabaseContext.cs
│   │   │   ├── Migrations/
│   │   │   │   └── 001_InitialSchema.sql
│   │   │   └── ConnectionTests.cs
│   │   └── Infrastructure.csproj
│   └── Frontend/
│       ├── Pages/Shared/Layout.cshtml
│       ├── Pages/Index.cshtml
│       ├── Program.cs (DI + MediatR config)
│       └── Frontend.csproj
├── tests/
│   ├── Domain.Tests/
│   │   └── Common/ValueObjectTests.cs (T00-001)
│   ├── Application.Tests/
│   │   └── Common/MediatRTests.cs (T00-002, T00-008, T00-010, T00-011)
│   └── Infrastructure.Tests/
│       └── Persistence/ConnectionTests.cs (T00-003)
├── .github/workflows/
│   └── build-test-deploy.yml (T00-005)
└── docs/
    ├── ARCHITECTURE.md
    ├── PROJECT_STRUCTURE.md
    └── SETUP.md
```

---

## Deliverables

### Domain Layer
- [ ] `Domain/Common/IDomainEvent.cs` - Empty interface (placeholder for Phase 2+ event sourcing)
- [ ] `Domain/Common/Entity.cs` - Abstract base class with Id property + GetDomainEvents() stub
- [ ] `Domain/Common/ValueObject.cs` - Abstract base with GetEqualityComponents()
- [ ] `Domain/Common/DomainException.cs` - Base exception class
- [ ] `Domain/Exceptions/EntityNotFoundException.cs` - Subclass
- [ ] `Domain/Exceptions/ValueObjectValidationException.cs` - Subclass
- [ ] `Domain/Common/IRepository.cs` - Generic repository interface with CRUD + GetBySpecificationAsync
  - Methods: `AddAsync(T)`, `UpdateAsync(T)`, `DeleteAsync(Guid)`, `GetByIdAsync(Guid)`, `GetAllAsync()`, `GetBySpecificationAsync(ISpecification<T>)`
  - Generic constraint: `where T : Entity<Guid>`
- [ ] `Domain/Specifications/ISpecification<T>` - Base class with MaxResults = 1000 property

### Application Layer
- [ ] `Application/Common/IUserContext.cs` - Interface (implementation in Phase 1)
  - Property: `UserId` (Guid)
  - Method: `IsAuthenticated()` → bool
  - Method: `IsAdmin()` → bool
- [ ] `Application/Common/MockUserContext.cs` - **Phase 0 test implementation** (replaced by SupabaseUserContext in Phase 1)
  - Property: `UserId` = Guid.Empty
  - Method: `IsAuthenticated()` → false
  - Method: `IsAdmin()` → false
- [ ] `Application/Common/Behaviors/ValidationBehavior.cs` - FluentValidation middleware
- [ ] `Application/Common/Behaviors/LoggingBehavior.cs` - Logging middleware
- [ ] `Application/Common/Behaviors/ScopedQueryBehavior.cs` - **Tenant isolation enforcement**
  - Validates Query responses implementing ITenantScoped
  - Throws exception if response.TenantId != currentUser.UserId
- [ ] `Application/Common/Abstractions/ITenantScoped.cs` - Marker interface for tenant-scoped queries/responses
  - Property: `Guid TenantId { get; }`
- [ ] `Application/Common/Dto/BaseDto.cs` - Base DTO class
- [ ] `Application/Common/Handlers/ICommandHandler.cs` - Command handler interface
- [ ] `Application/Common/Handlers/IQueryHandler.cs` - Query handler interface
- [ ] `Application/Common/Examples/CreateCategoryCommand.cs` - Example command + handler
- [ ] `Application/Common/Examples/GetCategoriesQuery.cs` - Example query + handler
- [ ] `Application/Tests/Helpers/MockRepositoryFactory.cs` - Test helper for repository mocking

### Infrastructure Layer
- [ ] `Infrastructure/Persistence/SupabaseContext.cs` - Connection management (abstract; no DB schema)
- [ ] `Infrastructure/Persistence/Migrations/README.md` - Folder structure for future migrations (Phase 1+)
- [ ] `Infrastructure/DependencyInjection.cs` - DI registration module (SupabaseContext, mock repository for testing)

### Frontend Layer
- [ ] `Frontend/Pages/Shared/Layout.cshtml` - Base layout template
- [ ] `Frontend/Pages/Index.cshtml` - Landing page
- [ ] `Frontend/Program.cs` - MediatR + DI configuration

### CI/CD
- [ ] `.github/workflows/build-test-deploy.yml` - GitHub Actions pipeline

### Documentation
- [ ] `docs/ARCHITECTURE.md` - 4-layer diagram + explanation
- [ ] `docs/PROJECT_STRUCTURE.md` - Folder structure guide
- [ ] `docs/SETUP.md` - Local development setup

---

## Test Specifications (Test-First)

All 11 tests must be written **BEFORE** implementation code is written.

### T00-001: Domain Entity Constructor Enforces Immutability
**Given**: Entity base class with Id property  
**When**: Entity is created  
**Then**: Entity state is sealed (cannot be modified after construction)  
**Implementation**: Unit test in `Domain.Tests/Common/ValueObjectTests.cs`

### T00-002: MediatR Handler Resolves Correctly from DI Container
**Given**: MediatR configured in DI  
**When**: Handler is resolved via IMediator  
**Then**: Handler instance is not null and is correct type  
**Implementation**: Integration test in `Application.Tests/Common/MediatRTests.cs`

### T00-003: Supabase Connection String Configurable from appsettings
**Given**: SupabaseContext reads from IConfiguration  
**When**: Connection string is set in appsettings.json  
**Then**: Context connects successfully  
**Implementation**: Integration test in `Infrastructure.Tests/Persistence/ConnectionTests.cs`

### T00-004: Test Helpers (FakeRepository, FakeUserContext) Work
**Given**: Mock repository factory  
**When**: MockRepositoryFactory creates Moq<IRepository>  
**Then**: Mock can be configured with Setup()  
**Implementation**: Unit test in `Application.Tests/Helpers/MockRepositoryFactoryTests.cs`

### T00-005: GitHub Actions Runs Tests on Push
**Given**: `.github/workflows/build-test-deploy.yml` configured  
**When**: Code is pushed to main branch  
**Then**: GitHub Actions workflow triggers, builds, and runs tests  
**Implementation**: Manual verification in GitHub Actions tab

### T00-006: Domain Exception Hierarchy Compiles and Throws Correctly
**Given**: DomainException + EntityNotFoundException + ValueObjectValidationException  
**When**: Exception is thrown  
**Then**: Exception can be caught and handled  
**Implementation**: Unit test in `Domain.Tests/Exceptions/ExceptionHierarchyTests.cs`

### T00-007: ISpecification<T> Enforces MaxResults = 1000 Default Limit
**Given**: ISpecification<T> base class  
**When**: Specification is created without MaxResults override  
**Then**: Default MaxResults = 1000  
**Implementation**: Unit test in `Domain.Tests/Specifications/SpecificationTests.cs`

### T00-008: ScopedQueryBehavior Rejects Queries Returning Cross-Tenant Data
**Given**: ScopedQueryBehavior middleware + Query returning response implementing ITenantScoped with TenantId != Current.UserId  
**When**: Handler returns cross-tenant ITenantScoped response  
**Then**: Behavior throws SecurityException  
**Implementation**: Integration test in `Application.Tests/Behaviors/ScopedQueryBehaviorTests.cs`

### T00-009: IUserContext Can Be Injected from DI Container
**Given**: MockUserContext registered in DI container  
**When**: IUserContext is injected into handler constructor  
**Then**: Injection succeeds and returns MockUserContext instance  
**Implementation**: Unit test in `Application.Tests/Common/DependencyInjectionTests.cs`

### T00-010: Example CreateCategoryCommand Handler Receives Request and Returns Result
**Given**: CreateCategoryCommand + handler implemented  
**When**: Handler is invoked via MediatR  
**Then**: Handler returns CategoryId result  
**Implementation**: Integration test in `Application.Tests/Examples/CreateCategoryCommandTests.cs`

### T00-011: Example GetCategoriesQuery Handler Executes and Applies Pagination
**Given**: GetCategoriesQuery + handler with pagination  
**When**: Handler is invoked via MediatR with Skip=0, Take=10  
**Then**: Handler returns paginated list  
**Implementation**: Integration test in `Application.Tests/Examples/GetCategoriesQueryTests.cs`

---

## Success Criteria

✅ **Phase 0 is complete when:**

1. `dotnet build` executes with **0 warnings**
2. `dotnet test` shows **11/11 tests passing** (T00-001 through T00-011)
3. `dotnet run --project src/Frontend/Frontend.csproj` starts without errors
4. GitHub Actions pipeline triggers on commit and shows **green build**
5. All 4 layers (Domain, Application, Infrastructure, Frontend) have test files
6. Exception hierarchy is defined and proven in tests
7. ISpecification<T> enforces pagination default in tests
8. ScopedQueryBehavior blocks cross-tenant data in tests
9. IUserContext can be injected (verified by DI tests)
10. Example CQRS command + query execute end-to-end
11. Repository mocking pattern documented in test helpers
12. Commit message: **"feat: phase 0 foundation setup complete"**

---

## Dependencies

### Incoming Dependencies
- None (Phase 0 is the foundation; nothing blocks it)

### Outgoing Dependencies
- **Blocks**: All subsequent phases (1, 2, 3, 4, 5, 6)
- **Critical for**: Phase 1 (Auth) depends on IUserContext abstraction, DI setup, SupabaseContext abstract class
- **Phase 1 adds**: First migration (001_CreateUsersTable.sql), SupabaseUserContext implementation
- **Phase 2 adds**: Domain entities (Category, Transaction, Budget) + corresponding migrations
- **Pattern**: Each phase creates migrations when entities are implemented (spec-driven approach)

### External Dependencies
- .NET Core 10 SDK
- Supabase account (for connection string)
- GitHub Actions (for CI/CD)
- NuGet packages: MediatR, FluentValidation, xUnit, Moq

---

## Clarifications

### Session 2026-02-14

- Q: IDomainEvent in Entity base class - definition and use? → A: Empty stub (interface + empty list in Phase 0; implement event sourcing Phase 2+)
- Q: IRepository<T> methods - which are required in Phase 0? → A: CRUD + Query (Add, Update, Delete, GetByIdAsync, GetAllAsync, GetBySpecificationAsync)
- Q: IUserContext - implementation Phase 0 or stub? → A: Mock implementation (MockUserContext in Phase 0; replaced by SupabaseUserContext in Phase 1)
- Q: ScopedQueryBehavior validation - what exactly validates? → A: Marker interface pattern (ITenantScoped); behavior validates response.TenantId == currentUser.UserId only if response implements ITenantScoped
- Q: Supabase schema/migrations - Phase 0 or Phase 1? → A: Phase 1+ responsibility (separate migrations per phase follow spec-driven approach; Phase 0 creates folder structure only)

---

## Implementation Notes

### Key Files for Architecture

**IDomainEvent.cs** - Event sourcing stub (Phase 0 placeholder):
```csharp
// Empty interface; full implementation in Phase 2+
public interface IDomainEvent
{
}
```

**Entity.cs** - Base class for all domain entities:
```csharp
public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
    protected Entity(TId id) => Id = id;
    protected Entity() { }
    
    // Phase 0: Returns empty list. Phase 2+: Returns actual domain events.
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => new List<IDomainEvent>();
}
```

**Note on IDomainEvent:**
Phase 0 defines the contract (`IDomainEvent` interface) but does not populate events. This allows future phases to implement event sourcing or event-driven architecture without refactoring Entity base class. Test T00-001 validates Entity immutability; event collection is not tested in Phase 0.

**ValueObject.cs** - Base class for immutable value objects:
```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    public override bool Equals(object? obj) => obj is ValueObject vo && 
        GetEqualityComponents().SequenceEqual(vo.GetEqualityComponents());
}
```

**IRepository<T>** - Generic repository interface (CRUD + Query pattern):
```csharp
public interface IRepository<T> where T : Entity<Guid>
{
    // Create
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    
    // Read
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> GetBySpecificationAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    
    // Update
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    
    // Delete
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

Note: Phase 0 defines interface only. Phase 1+ implements SupabaseRepository.

**IUserContext** - Current user context abstraction:
```csharp
public interface IUserContext
{
    Guid UserId { get; }
    bool IsAuthenticated();
    bool IsAdmin();
}
```

**MockUserContext** - Phase 0 test implementation (Phase 1 replaced by SupabaseUserContext):
```csharp
public class MockUserContext : IUserContext
{
    public Guid UserId { get; set; } = Guid.Empty;
    
    public bool IsAuthenticated() => false;
    public bool IsAdmin() => false;
}
```

Note on IUserContext: Phase 0 uses MockUserContext for DI testing. Phase 1 implements SupabaseUserContext with real Supabase Auth integration.

**ITenantScoped** - Marker interface for tenant-scoped queries/responses:
```csharp
public interface ITenantScoped
{
    Guid TenantId { get; }
}
```

**ScopedQueryBehavior<TRequest, TResponse>** - Tenant isolation enforcement:
```csharp
public class ScopedQueryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Validates that response (if ITenantScoped) has TenantId == currentUser.UserId
    // Throws SecurityException if cross-tenant access detected
    // Ignores responses not implementing ITenantScoped (assumed tenant-safe)
}
```

Note: ScopedQueryBehavior uses marker interface pattern for explicit opt-in to tenant validation. Queries/commands that return ITenantScoped responses are automatically validated.

**SupabaseContext** - Connection management abstraction (Phase 0 placeholder):
```csharp
public abstract class SupabaseContext
{
    // Phase 0: Abstract; no DB operations.
    // Phase 1+: Implement connection pool, DbSet<T> definitions for each entity added.
    // Migrations (001_Users, 002_Categories, etc.) created when entities are implemented.
}
```

Note on Migrations: Phase 0 creates `Infrastructure/Persistence/Migrations/` folder structure only. Each phase creates its migration when entities are implemented (Phase 1 → 001_CreateUsersTable.sql, Phase 2 → 002_CreateCategoriesTable.sql, etc.). This keeps schema versioning synchronized with code deliverables.

**ScopedQueryBehavior.cs** - **CRITICAL** for multi-tenancy enforcement:

**ISpecification<T>** - Base class for domain queries:
```csharp
public abstract class Specification<T>
{
    public int MaxResults { get; set; } = 1000; // DEFAULT PAGINATION
}
```

### NuGet Packages Required

| Package | Version | Layer | Purpose |
|---------|---------|-------|---------|
| MediatR | 12.0.0 | Application | CQRS pattern |
| MediatR.Extensions.Microsoft.DependencyInjection | 11.0.0 | Application | DI integration |
| FluentValidation | 11.8.0 | Application | Command validation |
| xUnit | 2.6.0 | Tests | Test framework |
| xUnit.Runner.VisualStudio | 2.5.0 | Tests | VS integration |
| Moq | 4.18.0 | Tests | Mocking |
| Supabase | 7.0.0 | Infrastructure | Supabase client |

---

## Acceptance Scenarios

### Scenario 1: Developer Starts Phase 0
**Given**: Fresh repository  
**When**: Developer runs `dotnet build`  
**Then**: Solution compiles with 0 warnings in < 30 seconds

### Scenario 2: First Unit Test Runs
**Given**: T00-001 written  
**When**: `dotnet test` runs  
**Then**: T00-001 passes (Entity immutability proven)

### Scenario 3: CQRS Example Command Executes
**Given**: CreateCategoryCommand + handler implemented  
**When**: `dotnet test` runs T00-010  
**Then**: Test passes (command returns result)

### Scenario 4: Multi-Tenancy Protection Active
**Given**: ScopedQueryBehavior middleware registered  
**When**: Query returns cross-tenant data  
**Then**: Behavior throws SecurityException

### Scenario 5: CI/CD Pipeline Triggers
**Given**: `.github/workflows/build-test-deploy.yml` defined  
**When**: Developer pushes commit to main  
**Then**: GitHub Actions runs, builds, tests, and shows green status

---

## Exit Criteria Checklist

- [ ] Solution structure created (src/, tests/, .github/workflows/, docs/)
- [ ] All 4 project files created and added to .csproj
- [ ] NuGet packages installed
- [ ] Domain foundation: Entity.cs, ValueObject.cs, DomainException.cs + exceptions
- [ ] Domain specifications: ISpecification<T> with MaxResults default
- [ ] Domain repositories: IRepository<T> interface
- [ ] Application: IUserContext.cs interface + MockUserContext implementation
- [ ] Application: ITenantScoped marker interface
- [ ] Application: Behaviors (Validation, Logging, ScopedQuery with ITenantScoped validation)
- [ ] Application: Example command (CreateCategoryCommand + handler)
- [ ] Application: Example query (GetCategoriesQuery + handler)
- [ ] Application: MockRepositoryFactory test helper
- [ ] Infrastructure: SupabaseContext.cs (abstract; no DB schema in Phase 0)
- [ ] Infrastructure: Migrations folder structure + README.md (actual migrations Phase 1+)
- [ ] Infrastructure: DependencyInjection.cs module
- [ ] Frontend: Layout.cshtml + Index.cshtml
- [ ] Tests: 11 tests written (T00-001 through T00-011)
- [ ] CI/CD: GitHub Actions workflow created
- [ ] Docs: ARCHITECTURE.md, PROJECT_STRUCTURE.md, SETUP.md
- [ ] `dotnet build` → 0 warnings
- [ ] `dotnet test` → 11/11 passing
- [ ] `dotnet run --project src/Frontend/` → success
- [ ] Commit: "feat: phase 0 foundation setup complete"

---

## Timeline

- **Week 1**: Solution structure + Domain foundation + first tests
- **Week 2**: Application layer + examples + test helpers
- **Week 3**: Infrastructure + Frontend + CI/CD + docs

**Target**: All 11 tests green by end of Week 3

---

**Specification Version**: 1.0.0  
**Last Updated**: 2026-02-14  
**Status**: Ready for Implementation
