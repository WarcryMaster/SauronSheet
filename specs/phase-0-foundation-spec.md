# Phase 0: Foundation & Infrastructure Setup

**Version**: 1.0.0  
**Status**: Ready for speckit.tasks generation  
**Duration**: 2-3 weeks (48 hours estimated)

---

## Executive Summary

**Objective**: Establish 4-layer Clean Architecture + CQRS pattern + CI/CD foundation for SauronSheet

**What We Build**: 
- Domain layer: Entity<TId>, ValueObject, exception hierarchy, repository interfaces
- Application layer: CQRS pattern with MediatR, ScopedQueryBehavior, DI configuration
- Infrastructure layer: Supabase context (abstract, schema in Phase 1+)
- Frontend layer: Razor Pages scaffolding + Index page
- Testing: 11 unit/integration tests covering domain + application

**Success Criteria**:
- ✅ `dotnet build` → 0 warnings
- ✅ `dotnet test` → 11/11 passing
- ✅ GitHub Actions workflow green
- ✅ All 38 deliverables completed
- ✅ ScopedQueryBehavior enforces multi-tenancy

---

## Timeline

| Week | Duration | Key Tasks | Gate Criteria |
|------|----------|-----------|--------------|
| **Week 1** | 20h | I (Initialization) + II (Domain) + III (App Infra start) | Domain compiles, 0 warnings |
| **Week 2** | 17h | III (App Infra finish) + IV (CQRS Examples) + V (Testing) + VI (Frontend start) | CQRS working, MediatR resolves |
| **Week 3** | 6h | VI (Frontend finish) + VII (CI/CD + Documentation) | All 11 tests pass, CI/CD green |

**Critical Path**: I → II → III → IV → V = 38h 15m  
**With Parallelization**: VI + VII can run concurrent = ~5h savings possible

---

## Architecture Overview

### 4-Layer Clean Architecture

```
Frontend (Razor Pages + JS)
    ↓
Application (MediatR Queries/Commands)
    ↓
Domain (Entities, Value Objects, Specifications)
    ↓
Infrastructure (Supabase, Repositories)
```

**Dependency Rules**:
- Frontend → Application → Domain ← Infrastructure
- NO upward dependencies (Domain never references Infrastructure)
- NO cross-layer references (Frontend never directly uses Domain)

### Core Patterns

**CQRS + MediatR**:
- Commands: State-changing operations (CreateCategoryCommand)
- Queries: Read-only operations (GetCategoriesQuery)
- All routed through MediatR (`await _mediator.Send(command)`)

**Multi-Tenancy**:
- Marker interface: `ITenantScoped`
- ScopedQueryBehavior validates: Response.TenantId == CurrentUser.UserId
- All queries implement ITenantScoped (enforced + tested)

**Domain-Driven Design**:
- Entity<TId>: Base class with ID + DomainEvents
- ValueObject: Immutable, value-based equality
- Specifications: ISpecification<T> with MaxResults = 1000

---

## Deliverables

### By Component (38 total items)

**Domain Layer** (8 deliverables):
- [ ] IDomainEvent.cs (stub interface)
- [ ] Entity<TId>.cs (base class with ID + Events)
- [ ] ValueObject.cs (base class for immutable objects)
- [ ] DomainException.cs (base exception)
- [ ] EntityNotFoundException.cs (specific exception)
- [ ] ValueObjectValidationException.cs (specific exception)
- [ ] IRepository<T>.cs (6 methods: Add, Update, Delete, GetById, GetAll, GetBySpecification)
- [ ] ISpecification<T>.cs (filtering interface with MaxResults = 1000)

**Application Layer** (13 deliverables):
- [ ] IUserContext.cs (interface with UserId property)
- [ ] MockUserContext.cs (test/Phase 0 implementation)
- [ ] ITenantScoped.cs (marker interface for queries)
- [ ] BaseDto.cs (base DTO with TenantId)
- [ ] ValidationBehavior.cs (MediatR pipeline behavior)
- [ ] LoggingBehavior.cs (MediatR pipeline behavior for logging)
- [ ] ScopedQueryBehavior.cs (enforces ITenantScoped validation)
- [ ] DependencyInjection.cs (MediatR registration)
- [ ] CreateCategoryCommand.cs + handler (example command)
- [ ] GetCategoriesQuery.cs (example query)
- [ ] CategoryDto.cs (example DTO)
- [ ] CreateCategoryCommandHandler.cs (example handler)
- [ ] GetCategoriesQueryHandler.cs (example handler)

**Infrastructure Layer** (3 deliverables):
- [ ] SupabaseContext.cs (abstract, no schema implementation)
- [ ] DependencyInjection.cs (infrastructure registration)
- [ ] Migrations/README.md (folder structure + instruction for Phase 1+)

**Frontend Layer** (3 deliverables):
- [ ] Layout.cshtml (main layout with navigation)
- [ ] Index.cshtml + Index.cshtml.cs (home page + PageModel)
- [ ] Program.cs (updated with MediatR DI)

**CI/CD** (1 deliverable):
- [ ] .github/workflows/build-test-deploy.yml (GitHub Actions)

**Documentation** (10 deliverables):
- [ ] ARCHITECTURE.md (layer descriptions + dependency rules)
- [ ] PROJECT_STRUCTURE.md (folder organization)
- [ ] SETUP.md (developer setup instructions)
- [ ] appsettings.json (configuration template)
- [ ] README (project overview)

---

## Test Specifications (11 Tests)

**T00-001**: Entity<TId> base class compiles + has ID property  
**T00-002**: ValueObject base class compiles + value equality works  
**T00-003**: DomainException + subclasses inherit correctly  
**T00-004**: IRepository<T> interface has 6 methods  
**T00-005**: ISpecification<T> interface with MaxResults = 1000  
**T00-006**: IDomainEvent stub interface exists  
**T00-007**: MockUserContext returns mocked UserId  
**T00-008**: ScopedQueryBehavior blocks cross-tenant queries (ITenantScoped marker validation)  
**T00-009**: MediatR DI container resolves correctly  
**T00-010**: CreateCategoryCommand handler works + test passes  
**T00-011**: GetCategoriesQuery handler works + respects tenant boundary  

**Success**: All 11/11 tests pass via `dotnet test`

---

## Implementation Strategy

### Phase I: Initialization (10 tasks, 2h 45m)
- I-1: Create solution file (dotnet new sln)
- I-2: Create Domain project (class library)
- I-3: Create Application project (class library)
- I-4: Create Infrastructure project (class library)
- I-5: Create Frontend project (Razor Pages)
- I-6: Add project references (Application → Domain, Infrastructure → Domain)
- I-7: Install NuGet packages (MediatR, Supabase, xUnit, Moq)
- I-8: Create DI container setup (MediatR registration in Program.cs)
- I-9: Verify solution compiles (dotnet build)
- I-10: Create test project structure

### Phase II: Domain Foundation (10 tasks, 8h 45m)
- II-1: Create Entity<TId> base class
- II-2: Create ValueObject base class
- II-3: Create IDomainEvent interface (empty stub)
- II-4: Create DomainException + subclasses
- II-5: Create IRepository<T> interface (6 methods)
- II-6: Create ISpecification<T> interface
- II-7: Write tests T00-001, T00-002, T00-003
- II-8: Write tests T00-004, T00-005, T00-006
- II-9: Domain layer compiles + all domain tests pass
- II-10: Verify no cross-layer dependencies

### Phase III: Application Infrastructure (13 tasks, 11h)
- III-1: Create IUserContext interface + MockUserContext
- III-2: Create ITenantScoped marker interface
- III-3: Create BaseDto with TenantId
- III-4: Create ValidationBehavior for MediatR
- III-5: Create LoggingBehavior for MediatR
- III-6: Create ScopedQueryBehavior (tenant validation)
- III-7: Create DependencyInjection.cs (register all behaviors)
- III-8: Write test T00-007 (MockUserContext injection)
- III-9: MediatR configuration complete + DI container wired
- III-10: Write test T00-008 (ScopedQueryBehavior blocks cross-tenant)
- III-11: Write test T00-009 (MediatR resolves from container)
- III-12: Application layer compiles + 0 warnings
- III-13: Verify all application infrastructure tested

### Phase IV: CQRS Pattern Examples (8 tasks, 7h)
- IV-1: Create CategoryDto
- IV-2: Create CreateCategoryCommand class
- IV-3: Create CreateCategoryCommandHandler (example handler)
- IV-4: Create GetCategoriesQuery class
- IV-5: Create GetCategoriesQueryHandler (example handler)
- IV-6: Wire handlers into DI (DependencyInjection.cs)
- IV-7: Write test T00-010 (CreateCategoryCommand works)
- IV-8: Write test T00-011 (GetCategoriesQuery respects tenant)

### Phase V: Testing Infrastructure (8 tasks, 8h 45m)
- V-1: Create MockRepositoryFactory
- V-2: Create test fixtures for Domain + Application
- V-3: Create test base class template
- V-4: Add integration test harness
- V-5: Verify all unit tests pass isolated
- V-6: Verify all integration tests pass with DI
- V-7: Measure code coverage (target: 80%+ Domain, 70%+ Application)
- V-8: Document testing patterns in ARCHITECTURE.md

### Phase VI: Frontend Scaffolding (7 tasks, 4h 45m)
- VI-1: Create Layout.cshtml (main layout)
- VI-2: Create Index.cshtml + Index.cshtml.cs
- VI-3: Install Tailwind CSS + Alpine.js
- VI-4: Configure Program.cs (MediatR DI)
- VI-5: Create appsettings.json (config template)
- VI-6: Frontend starts without errors (dotnet run)
- VI-7: Verify frontend can call MediatR queries

### Phase VII: CI/CD & Documentation (6 tasks, 5h 25m)
- VII-1: Create GitHub Actions workflow (.github/workflows/build-test-deploy.yml)
- VII-2: Create ARCHITECTURE.md (layer descriptions)
- VII-3: Create PROJECT_STRUCTURE.md (folder organization)
- VII-4: Create SETUP.md (developer setup)
- VII-5: Verify CI/CD pipeline runs + all tests pass on push
- VII-6: Final validation: all 38 deliverables complete + 11/11 tests passing

---

## Task Sequence & Dependencies

**Critical Path** (Must run sequentially):
```
I-10 (project refs) → II-4 (Entity) → III-9 (DI) → IV-2 (CQRS handler) → V-8 (final test)
```

**Can Parallelize**:
- II + III can run parallel after I-10
- VI can start after III-9 (DI ready)
- VII can start after IV complete

**Estimated Time Savings with Parallelization**: ~5 hours (from 48h to 43h elapsed)

---

## Pre-Flight Checklist

Before starting Phase I:

- [ ] .NET 10 SDK installed: `dotnet --version` shows 10.x
- [ ] Git installed: `git --version`
- [ ] VS Code installed + C# extension (or Visual Studio)
- [ ] GitHub repo created + cloned locally
- [ ] Supabase account created (for Phase 1)
- [ ] Calendar blocked: 2-3 weeks @ 14-21 hours/week

---

## Clarifications (Resolved During Planning)

**Q1: IDomainEvent in Phase 0 - Stub or Implementation?**  
A: Stub interface (empty in Phase 0). Phase 2+ populates domain events when entities are created.

**Q2: IRepository<T> Methods - What Should They Be?**  
A: 6 methods: AddAsync, UpdateAsync, DeleteAsync, GetByIdAsync, GetAllAsync, GetBySpecificationAsync

**Q3: IUserContext - Phase 0 or Phase 1?**  
A: MockUserContext in Phase 0 (used in tests + DI). Real SupabaseUserContext in Phase 1.

**Q4: ScopedQueryBehavior - How to Validate Tenant?**  
A: Marker interface `ITenantScoped`. Behavior checks if response implements ITenantScoped, then validates response.TenantId == currentUser.UserId.

**Q5: Database Migrations - Phase 0 or Later?**  
A: Phase 1+ (one migration per phase when entities implemented). Phase 0 creates folder structure only.

---

## Success Criteria (Definition of Done)

Phase 0 is **COMPLETE** when:

✅ Code Quality:
- [ ] Zero compiler warnings (`dotnet build` clean)
- [ ] No StyleCop violations
- [ ] Architecture dependency rules verified (no upward deps)

✅ Testing:
- [ ] All 11 tests pass (`dotnet test` = 11/11)
- [ ] 100% coverage of Domain entities
- [ ] 70%+ coverage of Application handlers

✅ Build & Deploy:
- [ ] `dotnet build` succeeds
- [ ] `dotnet run --project src/Frontend` runs
- [ ] GitHub Actions workflow green

✅ Documentation:
- [ ] ARCHITECTURE.md complete
- [ ] PROJECT_STRUCTURE.md complete
- [ ] SETUP.md verified (developer can follow it)

✅ Git:
- [ ] All changes committed
- [ ] Commit message: "feat: phase 0 foundation setup complete"
- [ ] Code pushed to main branch

---

## Daily Execution Roadmap (14 Days)

**Monday (Day 1)**: Phase I Initialization (2h 45m)
- Tasks: I-1 through I-10
- Checkpoint: Solution compiles, `dotnet build` passes

**Tuesday (Day 2)**: Phase II Domain Part 1 (2h)
- Tasks: II-1, II-2, II-3, II-4
- Checkpoint: Entity<TId>.cs compiles

**Wednesday (Day 3)**: Phase II Domain Part 2 + Phase III App Infra Part 1 (7h)
- Tasks: II-5 through II-10 + III-1 through III-3
- Gate: Domain 100% complete + tests passing
- Checkpoint: IRepository<T> interface + MockUserContext

**Thursday (Day 4)**: Phase III App Infra Part 2 (2h 45m)
- Tasks: III-4 through III-9
- Checkpoint: MediatR DI resolves without errors

**Friday (Day 5)**: Phase III App Infra Part 3 + Phase IV CQRS Start (4h 15m)
- Tasks: III-10 through III-13 + IV-1, IV-2
- Gate 1 Passed: ScopedQueryBehavior validated
- Checkpoint: CreateCategoryCommand created

**Monday (Day 6)**: Phase IV CQRS Examples (7h)
- Tasks: IV-3 through IV-8
- Checkpoint: Example handler + query working

**Tuesday (Day 7)**: Phase V Testing Infrastructure (3h)
- Tasks: V-1 through V-3
- Gate 2 Passed: CQRS pattern working + DI verified
- Checkpoint: Test fixtures ready

**Wednesday (Day 8)**: Phase V Testing Part 2 + Phase VI Frontend Part 1 (2h)
- Tasks: V-4 through V-6 + VI-1, VI-2
- Checkpoint: Tests passing in isolation

**Thursday (Day 9)**: Phase VI Frontend Part 2 (3h)
- Tasks: VI-3 through VI-7
- Checkpoint: Frontend starts without errors

**Friday (Day 10)**: Phase V Testing Part 3 (2h 45m)
- Tasks: V-7, V-8
- Gate 3 Passed: Examples complete + testing infrastructure ready
- Checkpoint: Coverage measured, patterns documented

**Monday (Day 11)**: Phase VII CI/CD Part 1 (3h)
- Tasks: VII-1, VII-2, VII-3
- Checkpoint: GitHub Actions workflow created

**Tuesday (Day 12)**: Phase VII CI/CD Part 2 (2h)
- Tasks: VII-4, VII-5
- Checkpoint: CI/CD pipeline green

**Wednesday (Day 13)**: Phase VII Final Validation (2h 25m)
- Tasks: VII-6 + final verification
- Checkpoint: All 38 deliverables complete

**Thursday (Day 14)**: Buffer + Final Testing (0h)
- Gate 4 Passed: All 11 tests passing + CI/CD green ✅
- Phase 0 COMPLETE

---

## Risk Assessment & Mitigations

| Risk | Severity | Cause | Mitigation |
|------|----------|-------|-----------|
| MediatR DI Configuration Complex | High | First time with MediatR behaviors | Start Task III-9 early; consult official docs; create fixture in V-3 |
| ScopedQueryBehavior Validation Logic | High | Multi-tenancy enforcement critical | Write test T00-008 first; validate cross-tenant rejection |
| Entity Base Class Design | Medium | Must support Phase 2-6 requirements | Review DDD patterns; code review before Phase II-10 gate |
| CQRS Handler Pattern Ambiguity | Medium | First handler = archetype | Document in IV-3; use as template for Phase 1+ handlers |
| Test Scaffolding Overhead | Medium | 11 tests across 3 layers | Use MockRepositoryFactory pattern; create base class in V-3 |

---

## Dependencies Map

```
Blocks Everything: I-10 (project references)
    ↓
Blocks App Layer: II-4 (Entity base class)
    ↓
Blocks CQRS Examples: III-9 (MediatR DI)
    ↓
Blocks Testing: IV-8 (CQRS handlers complete)
    ↓
Blocks CI/CD: VII-1 (GitHub Actions)
```

---

## Code Examples

### Entity<TId> Implementation Pattern
```csharp
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    
    public List<IDomainEvent> DomainEvents { get; } = new();
}
```

### Repository Interface Pattern
```csharp
public interface IRepository<T> where T : Entity<Guid>
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetBySpecificationAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
}
```

### CQRS Handler Pattern
```csharp
public record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Validation, domain logic, repository call
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }
}
```

### ScopedQueryBehavior Pattern
```csharp
public class ScopedQueryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        if (response is ITenantScoped tenantScoped)
        {
            if (tenantScoped.TenantId != _userContext.UserId)
                throw new UnauthorizedAccessException("Tenant boundary violation");
        }
        return response;
    }
}
```

---

## Progress Tracking Template

**Daily Standup Report**:
```
[DATE] Phase 0 Daily Standup

Completed:
- [Task]: [hours] (✅ done)
- Tests passing: __/11
- Build warnings: ___

In Progress:
- [Task]: __% complete
- Blocker(s): [describe if any]

Tomorrow:
- [Task(s)]: [planned hours]

Risk Level: 🟢 Low / 🟡 Medium / 🔴 High
Status: On Track / Slipping / Blocked
```

---

## Verification Commands

```bash
# Build & Compile
dotnet build                                    # Must show 0 warnings
dotnet build --configuration Release           # Release config

# Testing
dotnet test                                     # Must show 11/11 passing
dotnet test --verbosity detailed               # Detailed output
dotnet test --collect:"XPlat Code Coverage"   # Coverage report

# Frontend
dotnet run --project src/Frontend              # Must start without errors

# Git
git status                                      # Should show clean
git commit -m "feat: phase 0 foundation setup complete"
```

---

## Milestone Gates (Pass/Fail Criteria)

**Gate 1: Domain Complete** (End Week 1, Day 3)
- `dotnet build` = 0 warnings ✅
- `dotnet test` = 3/11 passing (T00-001 through T00-007) ✅
- Entity, ValueObject, Exception classes compiled ✅

**Gate 2: CQRS Ready** (End Week 1, Day 5)
- `dotnet build` = 0 warnings ✅
- `dotnet test` = 6/11 passing (domain + DI tests) ✅
- MediatR resolves from container ✅
- ScopedQueryBehavior validates ✅

**Gate 3: Examples Complete** (End Week 2, Day 7)
- `dotnet build` = 0 warnings ✅
- `dotnet test` = 9/11 passing (+ CQRS handlers) ✅
- Example command + query working ✅
- Frontend starts without errors ✅

**Gate 4: Phase 0 Complete** (End Week 3, Day 14)
- `dotnet build` = 0 warnings ✅
- `dotnet test` = 11/11 passing ✅
- GitHub Actions workflow green ✅
- All 38 deliverables completed ✅
- Code committed to main branch ✅

---

## Next Phase (Phase 1)

After Phase 0 passes gate 4, proceed to **Phase 1: Authentication & Multi-Tenancy**

Phase 1 will:
- Implement Supabase authentication (real IUserContext)
- Create User entity + database schema
- Implement real repository pattern
- Add JWT token validation
- Deploy to Vercel

---

**Phase 0 Specification Complete**  
Ready for: `speckit.tasks` command to generate tasks.md  
Then: `speckit.implement` command to generate implement.md
