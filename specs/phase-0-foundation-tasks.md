# Phase 0: Foundation & Infrastructure Setup - TASKS

**Generated from**: phase-0-foundation-spec.md  
**Version**: 1.0.0  
**Total Tasks**: 43 (organized in 7 phases)  
**Estimated Duration**: 48 hours over 2-3 weeks  
**Status**: Ready for implementation

---

## Task Summary by Phase

| Phase | Name | Tasks | Hours | Prerequisites |
|-------|------|-------|-------|---|
| I | Initialization | 10 | 2.75h | None |
| II | Domain Foundation | 10 | 8.75h | Phase I complete |
| III | Application Infrastructure | 13 | 11h | Phase II complete |
| IV | CQRS Pattern Examples | 8 | 7h | Phase III complete |
| V | Testing Infrastructure | 8 | 8.75h | Phase IV complete |
| VI | Frontend Scaffolding | 7 | 4.75h | Phase III-9 complete |
| VII | CI/CD & Documentation | 6 | 5.25h | All previous phases |

---

## PHASE I: Initialization (2h 45m, 10 tasks)

**Phase Goal**: Set up project structure, projects, NuGet packages, and DI foundation  
**Gate Criteria**: Solution compiles with `dotnet build` (0 warnings)  
**Blocking**: All other phases depend on I-10

### Task I-1: Create Solution File
- **Duration**: 15m
- **File Paths**: SauronSheet.sln
- **Command**: `dotnet new sln -n SauronSheet`
- **Verification**: `ls SauronSheet.sln` (file exists)
- **Dependencies**: None

### Task I-2: Create Domain Project
- **Duration**: 10m
- **File Paths**: src/Domain/Domain.csproj
- **Commands**:
  ```bash
  mkdir -p src/Domain
  dotnet new classlib -n Domain -o src/Domain --framework net10.0
  ```
- **Verification**: Domain.csproj exists
- **Dependencies**: I-1

### Task I-3: Create Application Project
- **Duration**: 10m
- **File Paths**: src/Application/Application.csproj
- **Commands**:
  ```bash
  mkdir -p src/Application
  dotnet new classlib -n Application -o src/Application --framework net10.0
  ```
- **Verification**: Application.csproj exists
- **Dependencies**: I-1

### Task I-4: Create Infrastructure Project
- **Duration**: 10m
- **File Paths**: src/Infrastructure/Infrastructure.csproj
- **Commands**:
  ```bash
  mkdir -p src/Infrastructure
  dotnet new classlib -n Infrastructure -o src/Infrastructure --framework net10.0
  ```
- **Verification**: Infrastructure.csproj exists
- **Dependencies**: I-1

### Task I-5: Create Frontend (Razor Pages) Project
- **Duration**: 15m
- **File Paths**: src/Frontend/Frontend.csproj
- **Commands**:
  ```bash
  mkdir -p src/Frontend
  dotnet new webapp -n Frontend -o src/Frontend --framework net10.0
  ```
- **Verification**: Frontend.csproj exists with Razor Pages template
- **Dependencies**: I-1

### Task I-6: Add Project References
- **Duration**: 10m
- **File Paths**: Application.csproj, Infrastructure.csproj, Frontend.csproj
- **Commands**:
  ```bash
  dotnet sln SauronSheet.sln add src/Domain/Domain.csproj
  dotnet sln SauronSheet.sln add src/Application/Application.csproj
  dotnet sln SauronSheet.sln add src/Infrastructure/Infrastructure.csproj
  dotnet sln SauronSheet.sln add src/Frontend/Frontend.csproj
  
  # Add project references
  cd src/Application && dotnet add reference ../Domain/Domain.csproj
  cd ../Infrastructure && dotnet add reference ../Domain/Domain.csproj
  cd ../Frontend && dotnet add reference ../Application/Application.csproj
  cd ../..
  ```
- **Verification**: All projects in solution, no circular references
- **Dependencies**: I-2, I-3, I-4, I-5

### Task I-7: Install NuGet Packages
- **Duration**: 20m
- **File Paths**: *.csproj files (all projects)
- **Packages to Install**:
  ```
  Domain: (no external packages)
  Application: MediatR (v12.0+)
  Infrastructure: Postgrest (Supabase), FluentValidation
  Frontend: MediatR, MediatR.Extensions.Microsoft.DependencyInjection, Tailwind CSS
  All: xUnit, Moq (for tests)
  ```
- **Commands**:
  ```bash
  # Application
  cd src/Application
  dotnet add package MediatR --version 12.0.0
  
  # Infrastructure
  cd ../Infrastructure
  dotnet add package Postgrest
  
  # Frontend
  cd ../Frontend
  dotnet add package MediatR
  dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
  
  # Test projects (will create in Phase V)
  cd ../..
  ```
- **Verification**: `dotnet restore` succeeds
- **Dependencies**: I-6

### Task I-8: Create DI Container Setup (Program.cs)
- **Duration**: 15m
- **File Paths**: src/Frontend/Program.cs
- **Implementation**:
  ```csharp
  var builder = WebApplication.CreateBuilder(args);
  
  // Add MediatR
  builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
      typeof(Program).Assembly,
      typeof(Application.DependencyInjection).Assembly
  ));
  
  var app = builder.Build();
  app.MapDefaultEndpoints();
  app.Run();
  ```
- **Verification**: Program.cs compiles, MediatR registered
- **Dependencies**: I-7

### Task I-9: Verify Solution Compiles
- **Duration**: 10m
- **Commands**: `dotnet build`
- **Expected Output**: 0 warnings, 0 errors
- **Deliverables**: Solution builds cleanly
- **Dependencies**: I-8

### Task I-10: Create Test Project Structure
- **Duration**: 10m
- **File Paths**: 
  - tests/Domain.Tests/Domain.Tests.csproj
  - tests/Application.Tests/Application.Tests.csproj
  - tests/Integration.Tests/Integration.Tests.csproj
- **Commands**:
  ```bash
  mkdir -p tests/Domain.Tests
  mkdir -p tests/Application.Tests
  mkdir -p tests/Integration.Tests
  
  dotnet new xunit -n Domain.Tests -o tests/Domain.Tests
  dotnet new xunit -n Application.Tests -o tests/Application.Tests
  dotnet new xunit -n Integration.Tests -o tests/Integration.Tests
  
  # Add references
  dotnet sln SauronSheet.sln add tests/Domain.Tests/Domain.Tests.csproj
  dotnet sln SauronSheet.sln add tests/Application.Tests/Application.Tests.csproj
  dotnet sln SauronSheet.sln add tests/Integration.Tests/Integration.Tests.csproj
  
  cd tests/Domain.Tests && dotnet add reference ../../src/Domain/Domain.csproj
  cd ../Application.Tests && dotnet add reference ../../src/Application/Application.csproj ../../src/Domain/Domain.csproj
  cd ../Integration.Tests && dotnet add reference ../../src/Frontend/Frontend.csproj
  ```
- **Verification**: All test projects added to solution, projects compile
- **Dependencies**: I-9
- **Blocks**: Phase II (domain tests), Phase III (app tests)

---

## PHASE II: Domain Foundation (8h 45m, 10 tasks)

**Phase Goal**: Implement domain layer (Entity<TId>, ValueObject, exceptions, repositories)  
**Gate Criteria**: Domain compiles with 0 warnings, T00-001 through T00-007 pass  
**Dependencies**: Phase I complete (I-10)

### Task II-1: Create Entity<TId> Base Class
- **Duration**: 45m
- **File Paths**: src/Domain/Entity.cs
- **Implementation**:
  ```csharp
  public abstract class Entity<TId> where TId : notnull
  {
      public TId Id { get; protected set; }
      public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
      public DateTime? UpdatedAt { get; protected set; }
      
      public List<IDomainEvent> DomainEvents { get; } = new();
      
      protected Entity(TId id)
      {
          Id = id;
      }
      
      protected Entity() { }
      
      public override bool Equals(object? obj)
      {
          return obj is Entity<TId> entity && Id.Equals(entity.Id);
      }
      
      public override int GetHashCode()
      {
          return Id.GetHashCode();
      }
  }
  ```
- **Deliverables**: Entity<TId> class with ID, CreatedAt, UpdatedAt, DomainEvents
- **Verification**: Compiles, has all required properties
- **Dependencies**: II-3 (IDomainEvent interface)

### Task II-2: Create ValueObject Base Class
- **Duration**: 45m
- **File Paths**: src/Domain/ValueObject.cs
- **Implementation**:
  ```csharp
  public abstract class ValueObject : IEquatable<ValueObject>
  {
      public abstract IEnumerable<object?> GetEqualityComponents();
      
      public override bool Equals(object? obj)
      {
          if (obj == null || obj.GetType() != GetType())
              return false;
              
          var valueObject = (ValueObject)obj;
          return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
      }
      
      public override int GetHashCode()
      {
          return GetEqualityComponents()
              .Aggregate(0, (a, b) => HashCode.Combine(a, b?.GetHashCode() ?? 0));
      }
      
      public bool Equals(ValueObject? other) => Equals((object?)other);
  }
  ```
- **Deliverables**: ValueObject base class with value-based equality
- **Verification**: Compiles, equality works correctly
- **Dependencies**: None

### Task II-3: Create IDomainEvent Stub Interface
- **Duration**: 15m
- **File Paths**: src/Domain/IDomainEvent.cs
- **Implementation**:
  ```csharp
  public interface IDomainEvent
  {
      DateTime OccurredOn { get; }
  }
  ```
- **Deliverables**: IDomainEvent interface (stub, will be populated in Phase 2+)
- **Verification**: Compiles
- **Dependencies**: None

### Task II-4: Create DomainException Hierarchy
- **Duration**: 30m
- **File Paths**: 
  - src/Domain/DomainException.cs
  - src/Domain/EntityNotFoundException.cs
  - src/Domain/ValueObjectValidationException.cs
- **Implementation**:
  ```csharp
  // DomainException.cs
  public class DomainException : Exception
  {
      public DomainException(string message) : base(message) { }
  }
  
  // EntityNotFoundException.cs
  public class EntityNotFoundException : DomainException
  {
      public EntityNotFoundException(string entityName, object id) 
          : base($"{entityName} with ID {id} not found") { }
  }
  
  // ValueObjectValidationException.cs
  public class ValueObjectValidationException : DomainException
  {
      public ValueObjectValidationException(string message) : base(message) { }
  }
  ```
- **Deliverables**: 3 exception classes with proper inheritance
- **Verification**: All compile
- **Dependencies**: None

### Task II-5: Create IRepository<T> Interface
- **Duration**: 45m
- **File Paths**: src/Domain/IRepository.cs
- **Implementation**:
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
- **Deliverables**: IRepository<T> with 6 methods
- **Verification**: Compiles, 6 methods present
- **Dependencies**: None

### Task II-6: Create ISpecification<T> Interface
- **Duration**: 30m
- **File Paths**: src/Domain/ISpecification.cs
- **Implementation**:
  ```csharp
  public interface ISpecification<T> where T : Entity<Guid>
  {
      Expression<Func<T, bool>>? Criteria { get; }
      int MaxResults { get; }
  }
  
  public abstract class Specification<T> : ISpecification<T> where T : Entity<Guid>
  {
      public Expression<Func<T, bool>>? Criteria { get; protected set; }
      public virtual int MaxResults => 1000;
  }
  ```
- **Deliverables**: ISpecification<T> and base Specification<T> class
- **Verification**: Compiles, MaxResults defaults to 1000
- **Dependencies**: None

### Task II-7: Write Tests T00-001 to T00-003
- **Duration**: 1h
- **File Paths**: tests/Domain.Tests/EntityTests.cs, ValueObjectTests.cs, ExceptionTests.cs
- **Tests**:
  ```
  T00-001: Entity<TId> base class has ID property (type: Guid)
  T00-002: ValueObject implements value-based equality
  T00-003: DomainException + EntityNotFoundException + ValueObjectValidationException inherit correctly
  ```
- **Commands**: `dotnet test tests/Domain.Tests`
- **Expected**: 3/3 tests pass
- **Dependencies**: II-1, II-2, II-4

### Task II-8: Write Tests T00-004 to T00-006
- **Duration**: 1h
- **File Paths**: tests/Domain.Tests/RepositoryTests.cs, SpecificationTests.cs
- **Tests**:
  ```
  T00-004: IRepository<T> interface has 6 methods (Add, Update, Delete, GetById, GetAll, GetBySpec)
  T00-005: ISpecification<T> has Criteria property + MaxResults = 1000
  T00-006: IDomainEvent stub interface exists
  ```
- **Commands**: `dotnet test tests/Domain.Tests`
- **Expected**: 3/3 tests pass (total 6/6)
- **Dependencies**: II-5, II-6, II-3

### Task II-9: Verify Domain Layer Compiles
- **Duration**: 15m
- **Commands**: 
  ```bash
  dotnet build src/Domain
  dotnet test tests/Domain.Tests
  ```
- **Expected**: 0 warnings, 6/6 tests pass
- **Dependencies**: II-7, II-8

### Task II-10: Verify No Cross-Layer Dependencies
- **Duration**: 30m
- **Verification**:
  - Domain project has NO references to Application/Infrastructure/Frontend
  - No NuGet packages except system packages
  - Code review: Check all imports
- **Commands**: Review csproj files, verify file structure
- **Expected**: No upward dependencies found
- **Dependencies**: II-9
- **Blocks**: Phase III (can't start until domain layer is isolated)

---

## PHASE III: Application Infrastructure (11h, 13 tasks)

**Phase Goal**: Implement CQRS infrastructure (MediatR, behaviors, DI)  
**Gate Criteria**: MediatR DI resolves correctly, T00-007 through T00-009 pass  
**Dependencies**: Phase II complete (II-10)

### Task III-1: Create IUserContext Interface + MockUserContext
- **Duration**: 45m
- **File Paths**: 
  - src/Application/IUserContext.cs
  - src/Application/MockUserContext.cs
- **Implementation**:
  ```csharp
  // IUserContext.cs
  public interface IUserContext
  {
      Guid UserId { get; }
  }
  
  // MockUserContext.cs
  public class MockUserContext : IUserContext
  {
      public Guid UserId { get; set; } = Guid.NewGuid();
  }
  ```
- **Deliverables**: IUserContext interface + MockUserContext implementation
- **Verification**: Both compile
- **Dependencies**: None

### Task III-2: Create ITenantScoped Marker Interface
- **Duration**: 15m
- **File Paths**: src/Application/ITenantScoped.cs
- **Implementation**:
  ```csharp
  public interface ITenantScoped
  {
      Guid TenantId { get; }
  }
  ```
- **Deliverables**: ITenantScoped marker interface
- **Verification**: Compiles
- **Dependencies**: None

### Task III-3: Create BaseDto with TenantId
- **Duration**: 30m
- **File Paths**: src/Application/Common/BaseDto.cs
- **Implementation**:
  ```csharp
  public abstract class BaseDto : ITenantScoped
  {
      public Guid Id { get; set; }
      public Guid TenantId { get; set; }
      public DateTime CreatedAt { get; set; }
  }
  ```
- **Deliverables**: BaseDto class implementing ITenantScoped
- **Verification**: Compiles, all properties present
- **Dependencies**: III-2

### Task III-4: Create ValidationBehavior for MediatR
- **Duration**: 45m
- **File Paths**: src/Application/Behaviors/ValidationBehavior.cs
- **Implementation**:
  ```csharp
  public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
      where TRequest : IRequest<TResponse>
  {
      private readonly IEnumerable<IValidator<TRequest>> _validators;
      
      public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
      {
          _validators = validators;
      }
      
      public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
      {
          var context = new ValidationContext<TRequest>(request);
          var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
          var failures = results.Where(r => r.Errors.Any()).SelectMany(r => r.Errors).ToList();
          
          if (failures.Any())
              throw new ValidationException(failures);
              
          return await next();
      }
  }
  ```
- **Deliverables**: ValidationBehavior class
- **Verification**: Compiles, implements IPipelineBehavior
- **Dependencies**: FluentValidation NuGet
- **Note**: Will integrate in next task

### Task III-5: Create LoggingBehavior for MediatR
- **Duration**: 30m
- **File Paths**: src/Application/Behaviors/LoggingBehavior.cs
- **Implementation**:
  ```csharp
  public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
      where TRequest : IRequest<TResponse>
  {
      private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
      
      public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
      {
          _logger = logger;
      }
      
      public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
      {
          _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
          var response = await next();
          _logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
          return response;
      }
  }
  ```
- **Deliverables**: LoggingBehavior class
- **Verification**: Compiles, logs request/response
- **Dependencies**: None

### Task III-6: Create ScopedQueryBehavior (Tenant Validation)
- **Duration**: 1h
- **File Paths**: src/Application/Behaviors/ScopedQueryBehavior.cs
- **Implementation**:
  ```csharp
  public class ScopedQueryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
      where TRequest : IRequest<TResponse>
  {
      private readonly IUserContext _userContext;
      
      public ScopedQueryBehavior(IUserContext userContext)
      {
          _userContext = userContext;
      }
      
      public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
      {
          var response = await next();
          
          if (response is ITenantScoped tenantScoped)
          {
              if (tenantScoped.TenantId != _userContext.UserId)
                  throw new UnauthorizedAccessException($"Tenant boundary violation: {_userContext.UserId} accessed {tenantScoped.TenantId}");
          }
          
          return response;
      }
  }
  ```
- **Deliverables**: ScopedQueryBehavior with tenant validation
- **Verification**: Compiles, validation logic correct
- **Dependencies**: III-1, III-2
- **Test**: T00-008 will verify this blocks cross-tenant access

### Task III-7: Create DependencyInjection.cs (Register Behaviors)
- **Duration**: 45m
- **File Paths**: src/Application/DependencyInjection.cs
- **Implementation**:
  ```csharp
  public static class DependencyInjection
  {
      public static IServiceCollection AddApplicationServices(this IServiceCollection services)
      {
          services.AddMediatR(cfg =>
          {
              cfg.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly);
              cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
              cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
              cfg.AddOpenBehavior(typeof(ScopedQueryBehavior<,>));
          });
          
          services.AddScoped<IUserContext, MockUserContext>();
          
          return services;
      }
  }
  ```
- **Deliverables**: DependencyInjection static class with AddApplicationServices
- **Verification**: Compiles, registers all behaviors
- **Dependencies**: III-4, III-5, III-6

### Task III-8: Write Test T00-007 (MockUserContext Injection)
- **Duration**: 45m
- **File Paths**: tests/Application.Tests/IUserContextTests.cs
- **Test**:
  ```
  T00-007: MockUserContext injects correctly from DI container + returns mocked UserId
  ```
- **Verification**: Test compiles + passes
- **Dependencies**: III-1, III-7

### Task III-9: Wire MediatR DI Configuration in Frontend Program.cs
- **Duration**: 45m
- **File Paths**: src/Frontend/Program.cs (update)
- **Update**:
  ```csharp
  builder.Services.AddApplicationServices(); // Call from Application.DependencyInjection
  ```
- **Verification**: Frontend program compiles, MediatR resolves from container
- **Commands**: `dotnet build src/Frontend`, verify no errors
- **Dependencies**: III-7
- **Blocks**: Phase IV (handlers need DI), Phase VI (frontend needs DI)

### Task III-10: Write Test T00-008 (ScopedQueryBehavior Blocks Cross-Tenant)
- **Duration**: 1h
- **File Paths**: tests/Application.Tests/ScopedQueryBehaviorTests.cs
- **Test**:
  ```
  T00-008: ScopedQueryBehavior blocks cross-tenant queries
  - Create ITenantScoped response with TenantId = Guid A
  - Set IUserContext.UserId = Guid B
  - Verify UnauthorizedAccessException thrown
  ```
- **Verification**: Test compiles + passes
- **Dependencies**: III-6, III-7

### Task III-11: Write Test T00-009 (MediatR DI Container Resolves)
- **Duration**: 45m
- **File Paths**: tests/Application.Tests/DependencyInjectionTests.cs
- **Test**:
  ```
  T00-009: MediatR DI container resolves correctly
  - Set up IServiceProvider from DependencyInjection.AddApplicationServices
  - Resolve IMediator
  - Verify not null + behaviors registered
  ```
- **Verification**: Test compiles + passes
- **Dependencies**: III-7, III-9

### Task III-12: Verify Application Layer Compiles
- **Duration**: 15m
- **Commands**:
  ```bash
  dotnet build src/Application
  dotnet test tests/Application.Tests
  ```
- **Expected**: 0 warnings, 3/3 tests pass (total 9/9)
- **Dependencies**: III-10, III-11

### Task III-13: Verify All Application Infrastructure Tested
- **Duration**: 30m
- **Verification**:
  - All Behaviors tested (ValidationBehavior, LoggingBehavior, ScopedQueryBehavior)
  - IUserContext tested
  - DependencyInjection tested
  - No compiler warnings
- **Commands**: `dotnet test tests/Application.Tests --verbosity detailed`
- **Expected**: 9/9 tests pass
- **Dependencies**: III-12
- **Blocks**: Phase IV (CQRS handlers depend on behaviors)

---

## PHASE IV: CQRS Pattern Examples (7h, 8 tasks)

**Phase Goal**: Create example command + query handlers (archetype for Phase 1+)  
**Gate Criteria**: Example handler + query working, T00-010, T00-011 pass  
**Dependencies**: Phase III complete (III-13)

### Task IV-1: Create CategoryDto
- **Duration**: 30m
- **File Paths**: src/Application/Categories/CategoryDto.cs
- **Implementation**:
  ```csharp
  public class CategoryDto : BaseDto
  {
      public string Name { get; set; } = string.Empty;
      public string? Description { get; set; }
  }
  ```
- **Deliverables**: CategoryDto extending BaseDto
- **Verification**: Compiles, all properties present
- **Dependencies**: III-3

### Task IV-2: Create CreateCategoryCommand
- **Duration**: 30m
- **File Paths**: src/Application/Categories/Commands/CreateCategoryCommand.cs
- **Implementation**:
  ```csharp
  public record CreateCategoryCommand(Guid UserId, string Name, string? Description) : IRequest<CategoryDto>;
  ```
- **Deliverables**: CreateCategoryCommand as MediatR IRequest<CategoryDto>
- **Verification**: Compiles, is record type
- **Dependencies**: IV-1

### Task IV-3: Create CreateCategoryCommandHandler
- **Duration**: 1h 15m
- **File Paths**: src/Application/Categories/Commands/CreateCategoryCommandHandler.cs
- **Implementation**:
  ```csharp
  public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
  {
      private readonly IUserContext _userContext;
      
      public CreateCategoryCommandHandler(IUserContext userContext)
      {
          _userContext = userContext;
      }
      
      public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
      {
          // Validate input
          if (string.IsNullOrWhiteSpace(request.Name))
              throw new ValueObjectValidationException("Category name cannot be empty");
          
          // Create DTO (no persistence in Phase 0)
          var dto = new CategoryDto
          {
              Id = Guid.NewGuid(),
              Name = request.Name,
              Description = request.Description,
              TenantId = request.UserId,
              CreatedAt = DateTime.UtcNow
          };
          
          return await Task.FromResult(dto);
      }
  }
  ```
- **Deliverables**: CreateCategoryCommandHandler implementing IRequestHandler
- **Verification**: Compiles, handles command + returns DTO
- **Dependencies**: IV-2, III-1
- **Note**: Handler is example only (no persistence yet, Phase 1+)

### Task IV-4: Create GetCategoriesQuery
- **Duration**: 30m
- **File Paths**: src/Application/Categories/Queries/GetCategoriesQuery.cs
- **Implementation**:
  ```csharp
  public record GetCategoriesQuery(Guid UserId) : IRequest<IEnumerable<CategoryDto>>;
  ```
- **Deliverables**: GetCategoriesQuery as MediatR IRequest<IEnumerable<CategoryDto>>
- **Verification**: Compiles, is record type, implements ITenantScoped (see IV-5)
- **Dependencies**: IV-1

### Task IV-5: Create GetCategoriesQueryHandler
- **Duration**: 1h 15m
- **File Paths**: src/Application/Categories/Queries/GetCategoriesQueryHandler.cs
- **Implementation**:
  ```csharp
  public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, GetCategoriesQueryDto>
  {
      private readonly IUserContext _userContext;
      
      public GetCategoriesQueryHandler(IUserContext userContext)
      {
          _userContext = userContext;
      }
      
      public async Task<GetCategoriesQueryDto> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
      {
          // Return empty list in Phase 0 (no persistence yet)
          var categories = new List<CategoryDto>();
          
          var result = new GetCategoriesQueryDto
          {
              Categories = categories,
              TenantId = request.UserId // Implemented via ITenantScoped
          };
          
          return await Task.FromResult(result);
      }
  }
  
  public class GetCategoriesQueryDto : ITenantScoped
  {
      public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
      public Guid TenantId { get; set; }
  }
  ```
- **Deliverables**: GetCategoriesQueryHandler + GetCategoriesQueryDto (ITenantScoped)
- **Verification**: Compiles, returns ITenantScoped result
- **Dependencies**: IV-4, III-2
- **Note**: Handler returns empty categories in Phase 0

### Task IV-6: Wire Handlers into DI (DependencyInjection.cs)
- **Duration**: 30m
- **File Paths**: src/Application/DependencyInjection.cs (update)
- **Update**:
  ```csharp
  cfg.RegisterServicesFromAssemblies(typeof(CreateCategoryCommandHandler).Assembly);
  // This auto-registers all handlers in the assembly
  ```
- **Verification**: Handlers auto-registered, no manual registration needed
- **Commands**: `dotnet build src/Application`
- **Dependencies**: IV-3, IV-5

### Task IV-7: Write Test T00-010 (CreateCategoryCommand Works)
- **Duration**: 1h
- **File Paths**: tests/Application.Tests/Categories/CreateCategoryCommandTests.cs
- **Test**:
  ```
  T00-010: CreateCategoryCommand handler works
  - Create command with name "Groceries"
  - Send via IMediator
  - Verify CategoryDto returned with name set
  - Verify TenantId = UserId (multi-tenancy)
  ```
- **Verification**: Test compiles + passes
- **Dependencies**: IV-3, IV-6

### Task IV-8: Write Test T00-011 (GetCategoriesQuery Respects Tenant)
- **Duration**: 1h
- **File Paths**: tests/Application.Tests/Categories/GetCategoriesQueryTests.cs
- **Test**:
  ```
  T00-011: GetCategoriesQuery respects tenant boundary
  - Query with UserId = Guid A
  - ScopedQueryBehavior validates TenantId = Guid A
  - If TenantId ≠ UserId, UnauthorizedAccessException thrown
  ```
- **Verification**: Test compiles + passes
- **Dependencies**: IV-5, IV-6, III-6
- **Note**: Tests multi-tenancy enforcement via ScopedQueryBehavior

---

## PHASE V: Testing Infrastructure (8h 45m, 8 tasks)

**Phase Goal**: Create test fixtures, mocks, patterns  
**Gate Criteria**: All 11 tests pass, coverage ≥80% Domain, ≥70% Application  
**Dependencies**: Phase IV complete (all handlers implemented)

### Task V-1: Create MockRepositoryFactory
- **Duration**: 1h
- **File Paths**: tests/Integration.Tests/Fixtures/MockRepositoryFactory.cs
- **Implementation**:
  ```csharp
  public class MockRepositoryFactory
  {
      public static Mock<IRepository<T>> CreateMockRepository<T>(List<T>? initialData = null) 
          where T : Entity<Guid>
      {
          var mockRepo = new Mock<IRepository<T>>();
          var data = initialData ?? new List<T>();
          
          mockRepo.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
              .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
              .Returns(Task.CompletedTask);
          
          mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(d => d.Id == id));
          
          mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(data);
          
          return mockRepo;
      }
  }
  ```
- **Deliverables**: MockRepositoryFactory with factory methods for common setup
- **Verification**: Compiles, mocks IRepository<T>
- **Dependencies**: None

### Task V-2: Create Test Fixtures for Domain + Application
- **Duration**: 1h
- **File Paths**: 
  - tests/Domain.Tests/Fixtures/DomainTestFixture.cs
  - tests/Application.Tests/Fixtures/ApplicationTestFixture.cs
- **Implementation**:
  ```csharp
  // DomainTestFixture
  public class DomainTestFixture
  {
      public static Entity<Guid> CreateTestEntity() => new TestEntity(Guid.NewGuid());
  }
  
  // ApplicationTestFixture
  public class ApplicationTestFixture
  {
      private readonly ServiceCollection _services;
      private readonly IServiceProvider _provider;
      
      public ApplicationTestFixture()
      {
          _services = new ServiceCollection();
          _services.AddApplicationServices();
          _provider = _services.BuildServiceProvider();
      }
      
      public IMediator GetMediator() => _provider.GetRequiredService<IMediator>();
  }
  ```
- **Deliverables**: Test fixtures for both layers
- **Verification**: Both compile
- **Dependencies**: None

### Task V-3: Create Test Base Class Template
- **Duration**: 45m
- **File Paths**: tests/Application.Tests/Base/ApplicationTestBase.cs
- **Implementation**:
  ```csharp
  public abstract class ApplicationTestBase : IDisposable
  {
      protected readonly ApplicationTestFixture _fixture;
      protected readonly IMediator _mediator;
      protected readonly IUserContext _userContext;
      
      public ApplicationTestBase()
      {
          _fixture = new ApplicationTestFixture();
          _mediator = _fixture.GetMediator();
          _userContext = new MockUserContext { UserId = Guid.NewGuid() };
      }
      
      public void Dispose()
      {
          _fixture?.Dispose();
      }
  }
  ```
- **Deliverables**: Base test class with common setup
- **Verification**: Compiles, can be inherited
- **Dependencies**: V-2

### Task V-4: Add Integration Test Harness
- **Duration**: 1h
- **File Paths**: tests/Integration.Tests/IntegrationTestBase.cs
- **Implementation**:
  ```csharp
  public abstract class IntegrationTestBase : IDisposable
  {
      protected readonly IServiceProvider _serviceProvider;
      protected readonly IMediator _mediator;
      
      public IntegrationTestBase()
      {
          var services = new ServiceCollection();
          services.AddApplicationServices();
          _serviceProvider = services.BuildServiceProvider();
          _mediator = _serviceProvider.GetRequiredService<IMediator>();
      }
      
      public void Dispose()
      {
          _serviceProvider?.Dispose();
      }
  }
  ```
- **Deliverables**: Integration test base with full DI container
- **Verification**: Compiles
- **Dependencies**: None

### Task V-5: Verify All Unit Tests Pass Isolated
- **Duration**: 30m
- **Commands**:
  ```bash
  dotnet test tests/Domain.Tests
  dotnet test tests/Application.Tests
  ```
- **Expected**: All tests pass independently
- **Verification**: No cross-test dependencies, no flaky tests
- **Dependencies**: V-1, V-2, V-3

### Task V-6: Verify All Integration Tests Pass with DI
- **Duration**: 30m
- **Commands**: `dotnet test tests/Integration.Tests`
- **Expected**: All integration tests pass
- **Verification**: DI container works, behaviors chain correctly
- **Dependencies**: V-4

### Task V-7: Measure Code Coverage
- **Duration**: 45m
- **Commands**:
  ```bash
  dotnet test tests/Domain.Tests --collect:"XPlat Code Coverage"
  dotnet test tests/Application.Tests --collect:"XPlat Code Coverage"
  ```
- **Target**:
  - Domain: ≥80% coverage
  - Application: ≥70% coverage
- **Verification**: Coverage report shows target met
- **Dependencies**: V-5, V-6

### Task V-8: Document Testing Patterns in ARCHITECTURE.md
- **Duration**: 1h
- **File Paths**: src/ARCHITECTURE.md (new section: "Testing Patterns")
- **Documentation**:
  - MockRepositoryFactory pattern
  - Test fixture pattern
  - IUserContext mocking
  - ScopedQueryBehavior testing
- **Verification**: Document complete, examples clear
- **Dependencies**: All previous test tasks
- **Blocks**: Phase VI, VII (documentation needed before code review)

---

## PHASE VI: Frontend Scaffolding (4h 45m, 7 tasks)

**Phase Goal**: Create Razor Pages UI scaffolding  
**Gate Criteria**: Frontend starts without errors, can instantiate Pages  
**Current Prerequisites**: Phase III-9 complete (DI wired)  
**Can run in parallel with**: Phase IV, V

### Task VI-1: Create Layout.cshtml
- **Duration**: 45m
- **File Paths**: src/Frontend/Pages/Shared/Layout.cshtml
- **Template**: 
  ```html
  <!DOCTYPE html>
  <html>
  <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1.0" />
      <title>@ViewData["Title"] - SauronSheet</title>
      <link href="~/css/site.css" rel="stylesheet" />
  </head>
  <body>
      <nav>
          <a href="/">Home</a>
      </nav>
      <main>
          @RenderBody()
      </main>
      <script src="~/js/site.js"></script>
  </body>
  </html>
  ```
- **Deliverables**: Layout.cshtml with navigation + RenderBody
- **Verification**: Compiles, valid HTML
- **Dependencies**: None

### Task VI-2: Create Index.cshtml + Index.cshtml.cs
- **Duration**: 1h
- **File Paths**: 
  - src/Frontend/Pages/Index.cshtml
  - src/Frontend/Pages/Index.cshtml.cs
- **Implementation**:
  ```csharp
  // Index.cshtml.cs
  public class IndexModel : PageModel
  {
      private readonly IMediator _mediator;
      
      public IndexModel(IMediator mediator)
      {
          _mediator = mediator;
      }
      
      public async Task OnGetAsync()
      {
          // Phase 0: Just load page, no queries yet
          ViewData["Title"] = "SauronSheet - Expense Tracking";
      }
  }
  ```
  ```html
  <!-- Index.cshtml -->
  @page
  @model IndexModel
  
  <h1>Welcome to SauronSheet</h1>
  <p>Expense tracking dashboard</p>
  <a href="/Transactions">View Transactions</a>
  ```
- **Deliverables**: Home page with PageModel + Razor template
- **Verification**: Compiles, page loads
- **Dependencies**: VI-1

### Task VI-3: Install Tailwind CSS + Alpine.js
- **Duration**: 30m
- **File Paths**: src/Frontend/Program.cs (update)
- **Steps**:
  ```bash
  cd src/Frontend
  npm install -D tailwindcss postcss autoprefixer
  npm install alpinejs
  dotnet add package TailwindCSS
  ```
- **Configuration**:
  - tailwind.config.js
  - postcss.config.js
  - src/Frontend/wwwroot/css/site.css (Tailwind imports)
- **Deliverables**: Tailwind + Alpine.js configured
- **Verification**: CSS builds, Alpine loads
- **Dependencies**: None

### Task VI-4: Update Program.cs (MediatR DI Already Done)
- **Duration**: 15m
- **File Paths**: src/Frontend/Program.cs (verify)
- **Verification**: MediatR already registered in Phase III-9
- **Check**: `builder.Services.AddApplicationServices()` is present
- **Dependencies**: III-9

### Task VI-5: Create appsettings.json (Config Template)
- **Duration**: 20m
- **File Paths**: src/Frontend/appsettings.json
- **Content**:
  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information"
      }
    },
    "Supabase": {
      "Url": "YOUR_SUPABASE_URL",
      "AnonKey": "YOUR_ANON_KEY"
    }
  }
  ```
- **Deliverables**: appsettings.json with environment placeholders
- **Verification**: Valid JSON
- **Dependencies**: None

### Task VI-6: Verify Frontend Starts Without Errors
- **Duration**: 30m
- **Commands**:
  ```bash
  cd src/Frontend
  dotnet run
  # Open http://localhost:5000 in browser
  # Verify home page loads
  ```
- **Expected**: App starts, no exceptions, home page displays
- **Verification**: Browser shows "Welcome to SauronSheet"
- **Dependencies**: VI-2, VI-3, VI-5

### Task VI-7: Verify Frontend Can Call MediatR Queries
- **Duration**: 45m
- **File Paths**: src/Frontend/Pages/Test.cshtml.cs (temporary test page)
- **Test**:
  ```csharp
  public class TestModel : PageModel
  {
      private readonly IMediator _mediator;
      
      public TestModel(IMediator mediator) => _mediator = mediator;
      
      public async Task OnGetAsync()
      {
          var query = new GetCategoriesQuery(User.GetUserId());
          var result = await _mediator.Send(query);
          // Verify result returned (empty list in Phase 0)
      }
  }
  ```
- **Verification**: Query executes via frontend, result returned
- **Dependencies**: VI-6, IV-5 (query handler)
- **Note**: Remove test page after verification

---

## PHASE VII: CI/CD & Documentation (5h 25m, 6 tasks)

**Phase Goal**: GitHub Actions pipeline + complete documentation  
**Gate Criteria**: CI/CD green, all documentation complete  
**Dependencies**: All previous phases complete

### Task VII-1: Create GitHub Actions Workflow
- **Duration**: 1h 15m
- **File Paths**: .github/workflows/build-test-deploy.yml
- **Content**:
  ```yaml
  name: Build & Test
  on: [push, pull_request]
  jobs:
    build:
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v3
        - uses: actions/setup-dotnet@v3
          with:
            dotnet-version: '10.0.x'
        - run: dotnet build
        - run: dotnet test
  ```
- **Deliverables**: GitHub Actions workflow for CI
- **Verification**: Workflow syntax valid, triggers on push
- **Dependencies**: None
- **Blocks**: VII-5 (need workflow to verify)

### Task VII-2: Create ARCHITECTURE.md
- **Duration**: 1h 15m
- **File Paths**: ARCHITECTURE.md (root)
- **Sections**:
  - 4-Layer architecture diagram
  - Dependency rules (no upward deps)
  - CQRS pattern explanation
  - ITenantScoped validation flow
  - Entity<TId> + ValueObject
  - MediatR behaviors pipeline
  - Testing approach
- **Verification**: Document complete, examples clear
- **Dependencies**: V-8 (testing patterns)

### Task VII-3: Create PROJECT_STRUCTURE.md
- **Duration**: 45m
- **File Paths**: PROJECT_STRUCTURE.md (root)
- **Content**:
  ```
  SauronSheet/
  ├── src/
  │   ├── Domain/              (Entities, Value Objects, Specifications)
  │   ├── Application/         (Commands, Queries, Handlers, DTOs)
  │   ├── Infrastructure/      (Repositories, External Services)
  │   └── Frontend/            (Razor Pages, UI)
  ├── tests/
  │   ├── Domain.Tests/
  │   ├── Application.Tests/
  │   └── Integration.Tests/
  └── .github/workflows/
  ```
- **Deliverables**: Folder structure documented
- **Verification**: Document matches actual structure
- **Dependencies**: None

### Task VII-4: Create SETUP.md
- **Duration**: 1h
- **File Paths**: SETUP.md (root)
- **Content**:
  - Prerequisites (.NET 10, Git, etc.)
  - Clone & build steps
  - Run tests
  - Run frontend
  - Debug guide
- **Verification**: Developer can follow & reproduce steps
- **Dependencies**: All previous phases (write from experience)

### Task VII-5: Verify CI/CD Pipeline Runs + All Tests Pass
- **Duration**: 45m
- **Steps**:
  - Commit all code
  - Push to GitHub
  - Verify workflow runs
  - Check: Build passes, all tests pass
- **Commands**: 
  ```bash
  git add .
  git commit -m "feat: phase 0 foundation setup complete"
  git push origin main
  ```
- **Expected**: GitHub Actions green ✅
- **Verification**: CI/CD dashboard shows 0 failures
- **Dependencies**: VII-1

### Task VII-6: Final Validation - All 38 Deliverables Complete + 11/11 Tests
- **Duration**: 1h
- **Verification Checklist**:
  - [ ] 8 Domain files (Entity, ValueObject, 3 exceptions, IRepository, ISpecification, IDomainEvent)
  - [ ] 13 Application files (IUserContext, MockUserContext, ITenantScoped, BaseDto, 3 behaviors, DI, 2 DTOs, 2 handlers)
  - [ ] 3 Infrastructure files (SupabaseContext, DI, Migrations/README.md)
  - [ ] 3 Frontend files (Layout, Index page + PageModel, Program.cs)
  - [ ] 1 CI/CD file (.github/workflows/build-test-deploy.yml)
  - [ ] 3 Documentation files (ARCHITECTURE.md, PROJECT_STRUCTURE.md, SETUP.md)
  - [ ] All 11 tests passing (T00-001 through T00-011)
  - [ ] 0 compiler warnings
  - [ ] Code reviewed + approved
- **Commands**:
  ```bash
  dotnet build                    # 0 warnings
  dotnet test                     # 11/11 passing
  git log --oneline | head -1     # "feat: phase 0 foundation setup complete"
  ```
- **Final Gate**: Phase 0 COMPLETE ✅
- **Next**: Proceed to Phase 1 (Authentication & Multi-Tenancy)

---

## Summary

**Total Tasks**: 43  
**Total Duration**: 48 hours (2-3 weeks)  
**Total Tests**: 11 (all unit/integration)  
**Total Deliverables**: 38 files  
**Success Criteria**: All 11/11 tests passing + 0 warnings + CI/CD green  

**Execution Order**: Phases I → II → III → (IV+V+VI in parallel after III-9) → VII → Final Validation

---

**Phase 0 Tasks Complete - Ready for Implementation**
