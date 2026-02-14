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
| **NF-004: Multi-Tenancy** | ScopedQueryBehavior validates tenant isolation | T00-008: ScopedQueryBehavior rejects cross-tenant data |
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
- [ ] `Domain/Common/Entity.cs` - Abstract base class with Id property
- [ ] `Domain/Common/ValueObject.cs` - Abstract base with GetEqualityComponents()
- [ ] `Domain/Common/DomainException.cs` - Base exception class
- [ ] `Domain/Exceptions/EntityNotFoundException.cs` - Subclass
- [ ] `Domain/Exceptions/ValueObjectValidationException.cs` - Subclass
- [ ] `Domain/Common/IRepository.cs` - Generic repository interface
- [ ] `Domain/Specifications/ISpecification<T>` - Base class with MaxResults = 1000 property

### Application Layer
- [ ] `Application/Common/IUserContext.cs` - Interface (implementation in Phase 1)
  - Property: `UserId` (Guid)
  - Method: `IsAuthenticated()` → bool
  - Method: `IsAdmin()` → bool
- [ ] `Application/Common/Behaviors/ValidationBehavior.cs` - FluentValidation middleware
- [ ] `Application/Common/Behaviors/LoggingBehavior.cs` - Logging middleware
- [ ] `Application/Common/Behaviors/ScopedQueryBehavior.cs` - **Tenant isolation enforcement**
  - Validates every Query result has UserId == current.UserId
  - Throws exception if cross-tenant data detected
- [ ] `Application/Common/Dto/BaseDto.cs` - Base DTO class
- [ ] `Application/Common/Handlers/ICommandHandler.cs` - Command handler interface
- [ ] `Application/Common/Handlers/IQueryHandler.cs` - Query handler interface
- [ ] `Application/Common/Examples/CreateCategoryCommand.cs` - Example command + handler
- [ ] `Application/Common/Examples/GetCategoriesQuery.cs` - Example query + handler
- [ ] `Application/Tests/Helpers/MockRepositoryFactory.cs` - Test helper for repository mocking

### Infrastructure Layer
- [ ] `Infrastructure/Persistence/SupabaseContext.cs` - Connection management
- [ ] `Infrastructure/Persistence/Migrations/001_InitialSchema.sql` - Initial migration template
- [ ] `Infrastructure/DependencyInjection.cs` - DI registration module

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
**Given**: ScopedQueryBehavior middleware + Query returning UserId != Current.UserId  
**When**: Handler tries to return cross-tenant data  
**Then**: Behavior throws exception  
**Implementation**: Integration test in `Application.Tests/Behaviors/ScopedQueryBehaviorTests.cs`

### T00-009: IUserContext Can Be Injected from DI Container
**Given**: IUserContext.cs interface defined  
**When**: IUserContext is injected into handler constructor  
**Then**: Injection succeeds (Phase 1 implements, Phase 0 verifies interface exists)  
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
- **Critical for**: Phase 1 (Auth) depends on IUserContext abstraction, DI setup

### External Dependencies
- .NET Core 10 SDK
- Supabase account (for connection string)
- GitHub Actions (for CI/CD)
- NuGet packages: MediatR, FluentValidation, xUnit, Moq

---

## Implementation Notes

### Key Files for Architecture

**Entity.cs** - Base class for all domain entities:
```csharp
public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
    protected Entity(TId id) => Id = id;
    protected Entity() { }
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => new List<IDomainEvent>();
}
```

**ValueObject.cs** - Base class for immutable value objects:
```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    public override bool Equals(object? obj) => obj is ValueObject vo && 
        GetEqualityComponents().SequenceEqual(vo.GetEqualityComponents());
}
```

**ScopedQueryBehavior.cs** - **CRITICAL** for multi-tenancy enforcement:
```csharp
public class ScopedQueryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Validates response doesn't contain cross-tenant data
    // Throws if current user ID doesn't match response owner ID
}
```

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
- [ ] Application: IUserContext.cs interface
- [ ] Application: Behaviors (Validation, Logging, ScopedQuery)
- [ ] Application: Example command (CreateCategoryCommand + handler)
- [ ] Application: Example query (GetCategoriesQuery + handler)
- [ ] Application: MockRepositoryFactory test helper
- [ ] Infrastructure: SupabaseContext.cs
- [ ] Infrastructure: Migrations folder + 001_InitialSchema.sql template
- [ ] Frontend: Program.cs with MediatR + DI config
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
