# SauronSheet Architecture Documentation

## Overview

SauronSheet follows a **Clean Architecture** with a layered approach, using **CQRS** (Command Query Responsibility Segregation) and **MediatR** for orchestration. The architecture enforces strict separation of concerns and multi-tenancy at the application level.

## Architecture Layers

### 1. **Domain Layer** (`src/Domain`)
The core business logic and domain model.

**Responsibilities:**
- Define entity models with `Entity<TId>` base class
- Define value objects with `ValueObject` base class
- Specify domain rules through entities
- Publish domain events (Phase 1+)

**Key Types:**
- `Entity<TId>`: Base class for all domain entities with ID, timestamps, and domain events
- `ValueObject`: Base class for immutable value objects with structural equality
- `IRepository<T>`: Interface defining persistence contracts (6 methods: Add, Update, Delete, GetById, GetAll, GetBySpec)
- `ISpecification<T>`: Interface for query criteria and limitations
- `IDomainEvent`: Marker interface for domain events (stub in Phase 0)
- Domain Exceptions: `DomainException`, `EntityNotFoundException`, `ValueObjectValidationException`

**Dependencies:** None - Zero external dependencies

### 2. **Application Layer** (`src/Application`)
CQRS handlers, orchestration, and DTOs.

**Responsibilities:**
- Handle commands (state-changing operations)
- Handle queries (read-only operations)
- Implement business workflows
- Enforce multi-tenancy boundaries
- Validation and logging

**Key Components:**

#### CQRS Pattern
- **Commands**: Requests that modify state (e.g., `CreateCategoryCommand`)
  ```csharp
  public record CreateCategoryCommand(Guid UserId, string Name, string? Description) 
      : IRequest<CategoryDto>;
  ```

- **Queries**: Requests that read state (e.g., `GetCategoriesQuery`)
  ```csharp
  public record GetCategoriesQuery(Guid UserId) 
      : IRequest<GetCategoriesQueryDto>;
  ```

- **Handlers**: Implement command/query logic
  ```csharp
  public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
  ```

#### MediatR Behaviors Pipeline
Behaviors are executed in order for each request:

1. **LoggingBehavior**: Logs all requests and responses
2. **ValidationBehavior**: Validates requests using FluentValidation
3. **ScopedQueryBehavior**: Enforces multi-tenancy by validating response TenantId matches current UserId

```
Request
  ↓
[LoggingBehavior] - Log request
  ↓
[ValidationBehavior] - Validate
  ↓
[ScopedQueryBehavior] - Enforce tenant boundary
  ↓
[Handler] - Execute business logic
  ↓
Response validated and logged
```

#### Multi-Tenancy Design
- `IUserContext`: Provides current user's ID
- `ITenantScoped`: Marker interface for resources with tenant ownership
- `BaseDto`: Base DTO implementing ITenantScoped (required TenantId property)
- `ScopedQueryBehavior`: Throws `UnauthorizedAccessException` if response.TenantId != UserContext.UserId

#### Dependency Injection
All services registered in `Application.DependencyInjection.cs`:
```csharp
services.AddApplicationServices();
  → Registers MediatR with all behaviors
  → Registers MockUserContext (Phase 0) or real IUserContext (Phase 1+)
```

**Dependencies:** Domain, MediatR, FluentValidation, Microsoft.Extensions.Logging, Microsoft.Extensions.DependencyInjection

### 3. **Infrastructure Layer** (`src/Infrastructure`)
Data access and external service implementations. (Stubbed in Phase 0)

**Responsibilities:**
- Implement repository pattern
- Database context setup
- Supabase integration
- External service clients

**Current Status (Phase 0):**
- Empty - all persistence stubbed in Application layer
- Will implement `IRepository<T>` with Supabase in Phase 1+

**Dependencies:** Domain, Application

### 4. **Frontend Layer** (`src/Frontend`)
Razor Pages ASP.NET Core web application.

**Responsibilities:**
- User interface and forms
- HTTP request handling via PageModels
- MediatR dispatch from UI
- Razor templates for rendering

**Current Structure (Phase 0):**
```
Frontend/
├── Pages/
│   ├── Index.cshtml (.razor template)
│   ├── Index.cshtml.cs (PageModel)
│   └── Shared/
│       └── Layout.cshtml (main layout)
├── wwwroot/
│   ├── css/ (Tailwind CSS)
│   ├── js/ (Alpine.js)
├── Program.cs (DI setup)
└── appsettings.json
```

**Dependencies:** Application, MediatR

## Dependency Rules (Clean Architecture)

```
Frontend → Application → Domain ← Infrastructure
  ↑          ↑            ↑           ↑
  └──────────┴────────────┴───────────┘
  (NO upward dependencies allowed)
```

**Enforcement:**
- Domain: No external package dependencies (only System.*)
- Application: References Domain only (no Infrastructure/Frontend)
- Infrastructure: References Domain and Application only
- Frontend: References Application only (not Infrastructure directly)

## CQRS Example: Creating a Category

### Command Flow
```
1. User submits form → Frontend PageModel
2. PageModel calls: await _mediator.Send(new CreateCategoryCommand(userId, name, description))
3. MediatR invokes pipeline:
   - LoggingBehavior: Log "CreateCategoryCommand" request
   - ValidationBehavior: Validate name not empty
   - ScopedQueryBehavior: No-op (commands don't validate response)
   - CreateCategoryCommandHandler: Create and return CategoryDto
   - ScopedQueryBehavior: Check CategoryDto.TenantId == UserId ✓
   - LoggingBehavior: Log success
4. Result returned to PageModel
5. PageModel redirects to success page
```

### Query Flow
```
1. User loads categories page → Frontend PageModel
2. PageModel calls: await _mediator.Send(new GetCategoriesQuery(userId))
3. MediatR invokes pipeline:
   - LoggingBehavior: Log "GetCategoriesQuery" request
   - ValidationBehavior: No validators (query is simple)
   - ScopedQueryBehavior: No-op (results not yet available)
   - GetCategoriesQueryHandler: Query database, return GetCategoriesQueryDto
   - ScopedQueryBehavior: Check GetCategoriesQueryDto.TenantId == UserId ✓
   - LoggingBehavior: Log success
4. Result returned to PageModel
5. PageModel passes Categories to view for rendering
```

## Testing Strategy

### Unit Tests (Domain Layer)
- Test entities and value objects in isolation
- Example: `Entity_Should_Have_Guid_Id` (T00-001)
- Target Coverage: ≥80%
- File: `tests/Domain.Tests/`

### Integration Tests (Application Layer)
- Test handlers with mocked dependencies
- Example: `ScopedQueryBehavior_Should_Block_Cross_Tenant_Access` (T00-008)
- Target Coverage: ≥70%
- File: `tests/Application.Tests/`

### End-to-End Tests (Integration Layer)
- Test full request pipeline through Razor Pages
- Future: Phase 1+
- File: `tests/Integration.Tests/`

## Multi-Tenancy Implementation

### Enforcement Points

1. **Query Level (ScopedQueryBehavior)**
   - Every query response is validated for tenant ownership
   - If `response is ITenantScoped` and `response.TenantId != currentUserId` → Throw `UnauthorizedAccessException`

2. **Database Level (Phase 1+)**
   - Repository queries automatically filtered by tenant
   - Example: `WHERE transactions.user_id = current_user_id`

3. **Authentication (Phase 1+)**
   - JWT token contains user ID
   - IUserContext extracted from token

## Phase 0 vs Phase 1+

### Phase 0 (Current)
- Mock repositories (no database)
- MockUserContext with fixed user
- No persistence layer
- Foundation for all future phases
- Tests via in-memory handlers

### Phase 1+ (Future)
- Real Supabase repository implementations
- Authentication with JWT
- Actual database persistence
- Event publishing and handlers
- Real user context from HTTP requests

## File Structure

```
SauronSheet/
├── src/
│   ├── Domain/                          # Entities, ValueObjects, Interfaces
│   │   ├── Entity.cs
│   │   ├── ValueObject.cs
│   │   ├── IRepository.cs
│   │   ├── ISpecification.cs
│   │   ├── IDomainEvent.cs
│   │   └── DomainException.cs
│   │
│   ├── Application/                     # CQRS Handlers, DTOs, Behaviors
│   │   ├── IUserContext.cs
│   │   ├── ITenantScoped.cs
│   │   ├── MockUserContext.cs
│   │   ├── DependencyInjection.cs
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs
│   │   │   ├── LoggingBehavior.cs
│   │   │   └── ScopedQueryBehavior.cs
│   │   ├── Common/
│   │   │   └── BaseDto.cs
│   │   └── Categories/
│   │       ├── CategoryDto.cs
│   │       ├── Commands/
│   │       │   ├── CreateCategoryCommand.cs
│   │       │   └── CreateCategoryCommandHandler.cs
│   │       └── Queries/
│   │           ├── GetCategoriesQuery.cs
│   │           └── GetCategoriesQueryHandler.cs
│   │
│   ├── Infrastructure/                  # Repositories, Data Access
│   │   └── (Stubbed for Phase 1+)
│   │
│   └── Frontend/                        # Razor Pages, UI
│       ├── Pages/
│       ├── wwwroot/css/
│       ├── wwwroot/js/
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── Domain.Tests/                    # 14 unit tests (T00-001 through T00-006+)
│   ├── Application.Tests/               # 7+ integration tests (T00-007 through T00-009+)
│   └── Integration.Tests/               # (Phase 1+)
│
└── SauronSheet.sln
```

## Key Patterns

### Specification Pattern
```csharp
var activeCategories = new Specification<Category>
{
    Criteria = c => c.IsActive == true,
    MaxResults = 100
};
var results = await _repository.GetBySpecificationAsync(activeCategories);
```

### DTOs for API/Response Contracts
```csharp
public class CategoryDto : BaseDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
}
```

### MediatR Command/Query Pattern
```csharp
// Command (Write)
public record CreateCategoryCommand(...) : IRequest<CategoryDto>;

// Query (Read)
public record GetCategoriesQuery(...) : IRequest<IEnumerable<CategoryDto>>;

// Dispatch
var result = await _mediator.Send(command);
```

## Testing Patterns

### Mock Repository Factory
```csharp
var mockRepo = MockRepositoryFactory.CreateMockRepository<Category>();
mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
```

### DI Container Setup in Tests
```csharp
var services = new ServiceCollection();
services.AddApplicationServices();
var sp = services.BuildServiceProvider();
var mediator = sp.GetRequiredService<IMediator>();
```

## Deployment

### Vercel (Frontend Hosting)
- Deploy `Frontend` project to Vercel
- Environment variables: `Supabase__Url`, `Supabase__AnonKey`
- Auto-deploy on main branch push

### Supabase (Database)
- PostgreSQL database
- Row-level security for tenancy
- Migrations tracked in `Infrastructure/Migrations/`

## Continuous Integration

GitHub Actions workflow (`.github/workflows/build-test-deploy.yml`):
1. dotnet build - Compile all projects
2. dotnet test - Run all 22+ tests
3. Auto-deploy to Vercel on main branch

Success criteria:
- ✓ 0 compiler warnings
- ✓ All tests pass
- ✓ CI/CD green

---

**Phase 0 Foundation Complete** - Ready for Phase 1: Authentication & Multi-Tenancy Persistence
