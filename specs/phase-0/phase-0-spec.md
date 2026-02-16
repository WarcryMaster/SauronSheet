# Phase 0: Foundation & Infrastructure Setup

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Foundation phase type)
- **Phase Type**: Foundation
- **Duration**: Weeks 1–2
- **Goal**: Solution structure, base abstractions, CI/CD, testing infrastructure
- **Depends On**: Nothing (first phase)
- **Unlocks**: Phase 1 (Authentication), Phase 2 (Domain Model)

---

## Critical Decisions

| ID | Decision | Rationale | Date |
|--------|-------|---------|----------|
| CD-0.1 | .NET 10 multi-project solution structure | Enforces Clean Architecture physical boundaries; prevents accidental cross-layer references | 2026-02-15 |
| CD-0.2 | MediatR 12+ integrated from day one | Avoids retrofit; all use cases flow through pipeline from the start | 2026-02-15 |
| CD-0.3 | xUnit + Moq as testing foundation | Industry standard for .NET; aligns with constitution testing pyramid | 2026-02-15 |
| CD-0.4 | Supabase client setup in Infrastructure only | Constitution mandate: no Supabase references in Domain or Application | 2026-02-15 |
| CD-0.5 | Tailwind CSS via CDN for initial setup | Simplifies Phase 0; full build pipeline deferred to Phase 6 (Polish) | 2026-02-15 |
| CD-0.6 | Strong-typed ID base pattern in Domain.Common | All future entities must use this; prevents raw Guid/string compliance violations | 2026-02-15 |
| CD-0.7 | Domain events (`IDomainEvent`) NOT included | Not needed until aggregate roots emit events; documented for future extension | 2026-02-15 |
| CD-0.8 | Alpine.js NOT included | No interactive UI until Phase 3+; reduces initial complexity | 2026-02-15 |

---

## Executive Summary

### In Scope

| Area | Deliverable |
|------|----------|
| Solution Structure | 6 .NET projects with enforced dependency graph |
| Domain.Common | `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject` base abstractions |
| Domain.Exceptions | `DomainException`, `EntityNotFoundException` |
| Domain.Repositories | `ISpecification<T>` base interface (MaxResults = 1000) |
| Application.Common | MediatR pipeline registration, `IUserContext` interface |
| Infrastructure | Supabase client DI configuration (connection validation, no tables) |
| Frontend | Minimal Razor Pages app: `Program.cs`, shared layout, Tailwind CDN, health check page |
| Testing | xUnit test projects for Domain and Application layers with ≥10 seed tests |
| CI/CD | `dotnet build` + `dotnet test` validation script |

### Deferred

| Item | Target Phase | Reason |
|------|-------------|----------|
| Domain entities (Transaction, etc.) | Phase 2 | Domain-Only phase scope |
| Authentication & JWT | Phase 1 | Full-Stack (Auth) phase scope |
| Supabase tables / migrations | Phase 1+ | No entities to persist yet |
| PDF parsing library | Phase 3 | Transaction Import phase scope |
| Analytics queries | Phase 4 | Analytics & Dashboard phase scope |
| Budget management | Phase 5 | Budget Management phase scope |
| Vercel deployment config | Phase 6 | Polish phase scope |
| Domain events (`IDomainEvent`) | Future | Not needed in foundation |
| Alpine.js | Phase 3+ | No interactive UI yet |
| Tailwind build pipeline (PostCSS) | Phase 6 | CDN sufficient for development |

---

## User Scenarios & Testing

### Scenario 0.1: Developer Clones and Builds Successfully

**As a** developer joining the project
**I want to** clone the repo, run `dotnet build`, and have zero errors
**So that** I can start contributing immediately

**Acceptance Criteria:**
- `dotnet build` completes with zero warnings and zero errors
- Solution contains 6 projects: `Domain`, `Application`, `Infrastructure`, `Frontend`, `Domain.Tests`, `Application.Tests`
- Project references enforce Clean Architecture dependency rules (no upward references)
- `global.json` pins the .NET SDK version

### Scenario 0.2: All Seed Tests Pass

**As a** developer
**I want to** run `dotnet test` and see all foundation tests pass
**So that** the testing infrastructure is proven working

**Acceptance Criteria:**
- `dotnet test` discovers and runs ≥10 tests
- All tests pass (green)
- Tests are categorized: `[Trait("Category", "Domain")]` and `[Trait("Category", "Application")]`
- Coverage report can be generated via coverlet

### Scenario 0.3: Frontend Health Check Renders

**As a** developer
**I want to** run the Frontend project and see a health check page
**So that** the Razor Pages pipeline is proven working

**Acceptance Criteria:**
- `dotnet run --project Frontend/` starts without errors
- Navigating to `/` shows a styled page with "SauronSheet — System OK" message
- Page uses the shared layout with Tailwind CSS styling (CDN)
- Page renders correctly in Chrome, Firefox, and Edge

### Scenario 0.4: Dependency Rules Are Physically Enforced

**As an** architect
**I want** the project references to enforce Clean Architecture boundaries
**So that** no layer can accidentally reference a layer it shouldn't

**Acceptance Criteria:**
- `Domain.csproj` has ZERO `<ProjectReference>` entries and ZERO `<PackageReference>` entries
- `Application.csproj` references ONLY `Domain`
- `Infrastructure.csproj` references ONLY `Domain`
- `Frontend.csproj` references `Application` and `Infrastructure` (for DI registration only)
- `Domain.Tests.csproj` references ONLY `Domain`
- `Application.Tests.csproj` references `Application` and `Domain`
- Adding a forbidden reference (e.g., Domain → Infrastructure) causes a build error by convention

### Scenario 0.5: Supabase Configuration Validates on Startup

**As a** developer
**I want** missing Supabase configuration to fail fast on startup
**So that** configuration issues are caught immediately

**Acceptance Criteria:**
- Missing `Supabase:Url` throws descriptive exception on startup
- Missing `Supabase:Key` throws descriptive exception on startup
- Valid configuration allows application to start successfully
- Configuration read from `appsettings.json` and environment variables

---

## Functional Requirements

### FR-0.01: Solution Structure

The solution MUST contain the following projects with the exact dependency graph:

```
SauronSheet.sln
├── src/
│   ├── SauronSheet.Domain/ → references: NOTHING
│   ├── SauronSheet.Application/ → references: Domain
│   ├── SauronSheet.Infrastructure/ → references: Domain
│   └── SauronSheet.Frontend/ → references: Application, Infrastructure
└── tests/
    ├── SauronSheet.Domain.Tests/ → references: Domain
    └── SauronSheet.Application.Tests/ → references: Application, Domain
```

**Dependency Graph (Visual):**

```
┌─────────────┐ ┌──────────────┐ ┌────────┐
│  Frontend   │────►│ Application │────►│ Domain │
└─────────────┘ └──────────────┘ └────────┘
      │ ▲
      │ ┌──────────────────┐ │
      └───────────►│ Infrastructure │─────┘
                  └──────────────────┘
```

> Frontend references Infrastructure **solely** for dependency injection registration in `Program.cs`. It MUST NOT call Infrastructure services directly.

**Additional Files:**

| File               | Location    | Purpose                                       |
|--------------------|-------------|-----------------------------------------------|
| `global.json`      | Root        | Pin .NET SDK version                          |
| `.gitignore`       | Root        | Standard .NET + IDE ignores                   |
| `Directory.Build.props` | Root   | Shared build properties (nullable, implicit usings) |

### FR-0.02: Domain.Common Base Abstractions

```
Domain/
├── Common/
│   ├── Entity.cs                    # Base entity with Id, CreatedAt, UpdatedAt
│   ├── AggregateRoot.cs             # Inherits Entity; marker for aggregate boundaries
│   └── ValueObject.cs               # Base record with value-based equality helper
├── Exceptions/
│   ├── DomainException.cs           # Base domain exception
│   └── EntityNotFoundException.cs    # Typed "not found" exception
└── Repositories/
    └── ISpecification.cs            # ISpecification with MaxResults = 1000 default
```

#### Entity<TId>

| Member         | Type          | Access           | Notes                                           |
|----------------|---------------|------------------|-------------------------------------------------|
| `Id`           | `TId`         | `get; protected set;` | Set in constructor only                    |
| `CreatedAt`    | `DateTime`    | `get; protected set;` | Set to `DateTime.UtcNow` on construction  |
| `UpdatedAt`    | `DateTime?`   | `get; protected set;` | Null initially; set on mutation methods   |

**Rules:**
- Generic `TId` type parameter allows strong-typed IDs
- Protected constructor for derived classes
- No public setters
- Equality based on `Id` and type (two entities with same Id and type are equal)
- `GetHashCode()` based on `Id`

#### AggregateRoot<TId>

| Member         | Type          | Notes                                           |
|----------------|---------------|-------------------------------------------------|
| (inherits all) | Entity<TId>  | Marker base class                               |

**Rules:**
- Inherits from `Entity<TId>`
- No additional behavior in Phase 0
- Domain events collection (`IDomainEvent`) explicitly deferred
- Comment in code: `// TODO: Add domain events collection in future phase`

#### ValueObject

| Member         | Type          | Notes                                           |
|----------------|---------------|-------------------------------------------------|
| (abstract)     | record        | C# record provides value-based equality         |

**Rules:**
- Abstract record base type
- Value-based equality via C# record semantics (compiler-generated)
- Override `ToString()` for debugging/logging
- Immutable by design (records are immutable)

### FR-0.03: Domain Exceptions

#### DomainException

| Constructor                               | Behavior                          |
|-------------------------------------------|-----------------------------------|
| `DomainException(string message)`         | Sets Message                      |
| `DomainException(string message, Exception innerException)` | Sets Message + InnerException |

**Rules:**
- Inherits from `Exception`
- Base class for all domain-specific exceptions
- Caught in Application layer (not in Domain)

#### EntityNotFoundException

| Constructor                                        | Message Format                                              |
|----------------------------------------------------|-------------------------------------------------------------|
| `EntityNotFoundException(string entityName, object entityId)` | `"Entity '{entityName}' with id '{entityId}' was not found."` |

**Rules:**
- Inherits from `DomainException`
- Stores `EntityName` and `EntityId` as properties for programmatic access
- Used when repository lookups return null for expected entities

### FR-0.04: ISpecification<T>

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    int MaxResults => 1000; // Default interface implementation
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
}
```

**Rules:**

- Generic interface for type-safe specifications
- MaxResults defaults to 1000 (pagination required for larger datasets)
- Includes for eager loading navigation properties
- IncludeStrings for string-based include paths
- Concrete specifications created in Phase 2

### FR-0.05: Application.Common
```
Application/
├── Common/
│   ├── IUserContext.cs         # Interface: UserId, IsAuthenticated
│   └── Behaviors/
│       └── (empty — pipeline behaviors deferred to Phase 1)
└── DependencyInjection.cs     # Extension method to register MediatR
```

#### IUserContext

| Member | Type | Notes |
|--------|------|-------|
| UserId | string | Current authenticated user's ID |
| IsAuthenticated | bool | Whether user has valid JWT token |

**Rules:**

- Interface only in Phase 0 (implementation in Phase 1)
- Used by handlers to scope queries to current tenant
- Resolved from HTTP context via middleware (Phase 1)

#### DependencyInjection.cs

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
```

**Rules:**

- Static extension method on IServiceCollection
- Registers MediatR from the Application assembly
- Future pipeline behaviors (validation, tenant scoping) registered here in later phases

### FR-0.06: Infrastructure Configuration

```
Infrastructure/
├── DependencyInjection.cs      # Extension method to register Supabase client + services
└── Persistence/
    └── (empty — no repositories until Phase 3)
```

#### DependencyInjection.cs

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is not configured.");
        var supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Supabase:Key is not configured.");

        // Register Supabase client as singleton
        // Register repository implementations (Phase 3+)

        return services;
    }
}
```

**Rules:**

- Static extension method on IServiceCollection
- Reads Supabase:Url and Supabase:Key from IConfiguration
- Throws InvalidOperationException with descriptive message if either is missing
- Registers Supabase client as singleton
- No repository registrations until Phase 3

### FR-0.07: Frontend Minimal Setup

```
Frontend/
├── Pages/
│   ├── Index.cshtml           # Health check page
│   ├── Index.cshtml.cs        # PageModel (no MediatR calls yet)
│   ├── Error.cshtml           # Error page
│   ├── Error.cshtml.cs        # Error PageModel
│   ├── _ViewImports.cshtml    # Tag helpers, namespaces
│   └── _ViewStart.cshtml      # Layout assignment
├── Shared/
│   └── _Layout.cshtml         # Base layout with Tailwind CDN, navigation shell
├── wwwroot/
│   ├── css/
│   │   └── site.css           # Minimal custom styles (if any beyond Tailwind)
│   └── js/
│       └── site.js            # Placeholder (empty or minimal)
├── Program.cs                 # Startup configuration
├── appsettings.json           # Supabase config placeholders
└── appsettings.Development.json # Development overrides
```

#### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Layer registrations
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
// app.UseAuthentication(); // Phase 1
// app.UseAuthorization();  // Phase 1
app.MapRazorPages();

app.Run();
```

**Rules:**

- Calls AddApplicationServices() and AddInfrastructureServices(config)
- Authentication/authorization middleware commented out (Phase 1)
- Standard middleware: HTTPS redirect, static files, routing, Razor Pages
- Error page for unhandled exceptions

#### Index.cshtml (Health Check)

**Requirements:**

- Displays "SauronSheet" as page heading (h1)
- Shows "System OK" status badge/message
- Displays current date/time
- Styled with Tailwind CSS utility classes
- No authentication required
- Responsive layout (looks good on mobile and desktop)

#### _Layout.cshtml

**Requirements:**

- HTML5 document structure
- Tailwind CSS CDN link in `<head>`
- Navigation bar shell (SauronSheet logo/title, placeholder nav links)
- `@RenderBody()` content area
- Footer with version info
- Responsive meta viewport tag

#### appsettings.json

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your-anon-key"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### FR-0.08: Testing Infrastructure

```
tests/
├── SauronSheet.Domain.Tests/
│   ├── Common/
│   │   ├── EntityBaseTests.cs              # Tests Entity<TId> base behavior
│   │   └── ValueObjectBaseTests.cs         # Tests ValueObject equality
│   ├── Exceptions/
│   │   ├── DomainExceptionTests.cs         # Tests exception construction
│   │   └── EntityNotFoundExceptionTests.cs # Tests formatted message
│   └── Specifications/
│       └── SpecificationBaseTests.cs       # Tests ISpecification defaults
│
└── SauronSheet.Application.Tests/
    └── Common/
        └── MediatRRegistrationTests.cs     # Tests MediatR DI registration
```

**Rules:**

- Each test class has `[Trait("Category", "Domain")]` or `[Trait("Category", "Application")]`
- Test naming convention: MethodName_Scenario_ExpectedResult
- Arrange-Act-Assert pattern
- Test helper classes use concrete stubs (e.g., TestEntity : Entity<Guid>) to test abstract bases

## Architecture Notes

### NuGet Packages Per Project

| Project | Packages |
|---------|----------|
| SauronSheet.Domain | None (zero dependencies — constitution mandate) |
| SauronSheet.Application | MediatR (12+), MediatR.Extensions.Microsoft.DependencyInjection |
| SauronSheet.Infrastructure | supabase-csharp (or Postgrest) |
| SauronSheet.Frontend | Microsoft.AspNetCore.App (implicit SDK reference) |
| SauronSheet.Domain.Tests | xUnit, xUnit.runner.visualstudio, Moq, coverlet.collector |
| SauronSheet.Application.Tests | xUnit, xUnit.runner.visualstudio, Moq, coverlet.collector |

### Directory.Build.props (Root)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### global.json

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

## Test Specifications

### Domain.Tests

#### EntityBaseTests
TEST: Entity_SetsCreatedAtOnConstruction
  GIVEN a concrete entity derived from Entity<Guid>
  WHEN constructed with a valid Id
  THEN CreatedAt is set to approximately DateTime.UtcNow (within 1 second)
  AND UpdatedAt is null

TEST: Entity_EqualityByIdAndType
  GIVEN two entities of the same type with the same Id
  WHEN compared for equality
  THEN they are considered equal
  AND their hash codes are equal

TEST: Entity_InequalityByDifferentId
  GIVEN two entities of the same type with different Ids
  WHEN compared for equality
  THEN they are NOT equal

TEST: Entity_InequalityByDifferentType
  GIVEN two entities of different types with the same Id
  WHEN compared for equality
  THEN they are NOT equal
```

#### ValueObjectBaseTests
TEST: ValueObject_EqualityByProperties
  GIVEN two value objects with identical property values
  WHEN compared for equality
  THEN they are equal (value-based equality via record semantics)

TEST: ValueObject_InequalityByDifferentProperties
  GIVEN two value objects with different property values
  WHEN compared for equality
  THEN they are NOT equal
```

#### DomainExceptionTests
TEST: DomainException_CarriesMessage
  GIVEN a message string "Invalid state detected"
  WHEN DomainException is constructed with that message
  THEN Message equals "Invalid state detected"

TEST: DomainException_CarriesInnerException
  GIVEN a message and an inner exception (e.g., ArgumentException)
  WHEN DomainException is constructed with both
  THEN InnerException is the provided ArgumentException
  AND Message equals the provided message
```

#### EntityNotFoundExceptionTests
TEST: EntityNotFoundException_FormatsMessage
  GIVEN entityName = "Transaction" and entityId = "550e8400-e29b-41d4-a716-446655440000"
  WHEN EntityNotFoundException is constructed
  THEN Message equals "Entity 'Transaction' with id '550e8400-e29b-41d4-a716-446655440000' was not found."

TEST: EntityNotFoundException_StoresProperties
  GIVEN entityName = "Category" and entityId = some-guid
  WHEN EntityNotFoundException is constructed
  THEN EntityName equals "Category"
  AND EntityId equals the provided guid
```

#### SpecificationBaseTests
TEST: Specification_DefaultMaxResultsIs1000
  GIVEN a concrete specification implementing ISpecification<T>
  WHEN MaxResults is read (using default interface implementation)
  THEN it equals 1000
```

### Application.Tests

#### MediatRRegistrationTests
TEST: MediatR_ResolvesFromServiceProvider
  GIVEN services registered via AddApplicationServices()
  WHEN IMediator is resolved from the built service provider
  THEN it is not null
  AND it is an instance of IMediator

TEST: AddApplicationServices_RegistersWithoutException
  GIVEN a new ServiceCollection
  WHEN AddApplicationServices() is called
  THEN no exception is thrown
  AND the service collection contains MediatR registrations
```

## Test Summary

| Test ID | Test Name | Category | Assert |
|---------|-----------|----------|--------|

T-0.01	Entity_SetsCreatedAtOnConstruction	Domain	CreatedAt ≈ UtcNow (±1s), UpdatedAt is null
T-0.02	Entity_EqualityByIdAndType	Domain	Same type + same Id → equal + same hash code
T-0.03	Entity_InequalityByDifferentId	Domain	Same type + different Id → not equal
T-0.04	Entity_InequalityByDifferentType	Domain	Different type + same Id → not equal
T-0.05	ValueObject_EqualityByProperties	Domain	Identical properties → equal
T-0.06	ValueObject_InequalityByDifferentProperties	Domain	Different properties → not equal
T-0.07	DomainException_CarriesMessage	Domain	Message equals input string
T-0.08	DomainException_CarriesInnerException	Domain	InnerException is set correctly
T-0.09	EntityNotFoundException_FormatsMessage	Domain	Message matches template format
T-0.10	EntityNotFoundException_StoresProperties	Domain	EntityName and EntityId accessible
T-0.11	Specification_DefaultMaxResultsIs1000	Domain	MaxResults == 1000
T-0.12	MediatR_ResolvesFromServiceProvider	Application	IMediator resolved is not null
| T-0.13 | AddApplicationServices_RegistersWithoutException | Application | No exception; services registered |

**Total:** 13 tests (11 Domain + 2 Application)

## Deliverables

| # | Deliverable | Layer | Acceptance |
D-0.01	SauronSheet.sln with 6 projects	All	dotnet build succeeds; dependency graph validated per FR-0.01
D-0.02	global.json + Directory.Build.props	All	SDK pinned; nullable + warnings-as-errors enabled
D-0.03	Entity<TId> base class	Domain	Tests T-0.01 to T-0.04 pass
D-0.04	AggregateRoot<TId> base class	Domain	Inherits Entity; compiles; TODO comment for domain events
D-0.05	ValueObject abstract record	Domain	Tests T-0.05 and T-0.06 pass
D-0.06	DomainException	Domain	Tests T-0.07 and T-0.08 pass
D-0.07	EntityNotFoundException	Domain	Tests T-0.09 and T-0.10 pass
D-0.08	ISpecification<T> interface	Domain	Test T-0.11 passes; MaxResults default = 1000
D-0.09	IUserContext interface	Application	Compiles; used in Phase 1 for implementation
D-0.10	DependencyInjection.cs (Application)	Application	Tests T-0.12 and T-0.13 pass; MediatR resolves
D-0.11	DependencyInjection.cs (Infrastructure)	Infrastructure	Missing config throws InvalidOperationException
D-0.12	Program.cs with layer registration	Frontend	Calls both DI extension methods; app starts
D-0.13	_Layout.cshtml with Tailwind CDN	Frontend	HTML5 structure, nav shell, responsive viewport
D-0.14	Index.cshtml health check page	Frontend	Displays "SauronSheet — System OK" with Tailwind styling
D-0.15	Error.cshtml error page	Frontend	Handles unhandled exceptions gracefully
D-0.16	appsettings.json + appsettings.Development.json	Frontend	Supabase config placeholders
D-0.17	Domain.Tests project (≥11 tests)	Tests	dotnet test --filter Category=Domain all green
| D-0.18 | Application.Tests project (≥2 tests) | Tests | dotnet test --filter Category=Application all green |

## Success Criteria

| # | Criterion | Metric |
SC-0.1	Project builds from clean clone with zero errors and zero warnings	dotnet build exit code 0; TreatWarningsAsErrors
SC-0.2	All foundation tests pass	dotnet test exit code 0; ≥13 tests discovered
SC-0.3	Dependency rules physically enforced via project references	Manual audit of 6 .csproj files per FR-0.01
SC-0.4	Frontend renders health check page with Tailwind styling	Visual verification at https://localhost:{port}/
SC-0.5	Domain project has ZERO NuGet package dependencies	Domain.csproj contains no <PackageReference>
SC-0.6	Domain test coverage ≥ 80%	coverlet report on Domain.Common + Exceptions + Repos
SC-0.7	MediatR registers and resolves successfully	Integration tests T-0.12 and T-0.13 pass
SC-0.8	Infrastructure validates Supabase config on startup	Missing config → InvalidOperationException with message
SC-0.9	global.json pins .NET SDK version	File exists; version specified
| SC-0.10 | Nullable reference types enabled solution-wide | `<Nullable>enable</Nullable>` in Directory.Build.props |

## Assumptions
.NET 10 SDK is available on the development machine (preview or GA release). If not available, .NET 9 is an acceptable fallback with global.json updated accordingly.
Supabase account is already created; URL and Key are available for appsettings.json. The free tier is sufficient for all development phases.
Tailwind CSS CDN is acceptable for Phase 0 through Phase 5. A full build pipeline (PostCSS/CLI with purging) will be added in Phase 6 (Polish).
Domain events (IDomainEvent interface and domain event collection on AggregateRoot) are NOT included in Phase 0. The AggregateRoot base class will be extended in a later phase if/when domain events are needed. This is explicitly noted per the copilot-instructions.md pitfall: "Domain events without IDomainEvent base infrastructure (verify Phase 0 first)."
Alpine.js is NOT included in Phase 0. It will be added in Phase 3 or later when interactive UI components are needed (dropdowns, modals, filters).
No authentication in Phase 0. The health check page is publicly accessible. Authentication middleware will be added in Phase 1.
supabase-csharp (or equivalent .NET Supabase client package) is used as the Supabase client library. The specific package will be confirmed during implementation.
- No CI/CD pipeline (GitHub Actions, etc.) in Phase 0 beyond local `dotnet build` + `dotnet test` scripts. CI/CD automation is a Phase 6 concern or can be added opportunistically.

## Risks & Mitigations

| ID | Risk | Impact | Probability | Mitigation |
R-0.1	.NET 10 SDK not yet stable / available	High	Medium	Pin SDK in global.json; fallback to .NET 9 with minimal changes
R-0.2	Supabase C# client lacks needed features	Medium	Low	Wrap in Infrastructure abstraction; swap implementation if needed
R-0.3	Tailwind CDN causes performance issues	Low	Low	Temporary for dev; full build pipeline in Phase 6
R-0.4	MediatR 12+ has breaking changes from 11.x	Low	Low	Pin exact version in Application.csproj; check release notes
| R-0.5 | TreatWarningsAsErrors too strict initially | Low | Medium | Can temporarily suppress specific warnings; track in backlog |

## Implementation Notes

### Recommended Implementation Order

```
Step 1: Create solution and projects
        └── SauronSheet.sln, 6 .csproj files, global.json, Directory.Build.props
        └── Verify: `dotnet build` succeeds with empty projects

Step 2: Write Domain.Tests (RED phase)
        └── Create test stubs for all 11 domain tests
        └── Verify: `dotnet test` runs, all tests FAIL (red)

Step 3: Implement Domain.Common (GREEN phase)
        └── Entity<TId>, AggregateRoot<TId>, ValueObject
        └── DomainException, EntityNotFoundException
        └── ISpecification<T>
        └── Verify: `dotnet test --filter Category=Domain` all GREEN

Step 4: Write Application.Tests (RED phase)
        └── Create test stubs for 2 application tests
        └── Verify: tests FAIL (red)

Step 5: Implement Application.Common (GREEN phase)
        └── IUserContext interface
        └── DependencyInjection.cs with MediatR registration
        └── Verify: `dotnet test --filter Category=Application` all GREEN

Step 6: Implement Infrastructure DI
        └── DependencyInjection.cs with Supabase config validation
        └── Verify: missing config throws; valid config succeeds

Step 7: Implement Frontend
        └── Program.cs, _Layout.cshtml, Index.cshtml, Error.cshtml
        └── appsettings.json
        └── Verify: `dotnet run --project Frontend/` → health check page renders

Step 8: Final validation
        └── `dotnet build` → zero errors, zero warnings
        └── `dotnet test` → all 13 tests green
        └── Coverage report → domain ≥ 80%
        └── Audit .csproj files → dependency rules correct
```

## Spec-Driven Workflow Compliance

This phase follows the Constitution's Spec-Driven Development workflow:

| Step | Workflow Stage | Phase 0 Action |
| 1 | Write Test Spec | ✅ Tests written first (Steps 2 and 4 above) |
| 2 | Define Handler Stub | ⚠️ N/A — no MediatR handlers in Phase 0 |
| 3 | Build Domain | ✅ Base abstractions implemented (Step 3) |
| 4 | Implement Persistence | ⚠️ Partial — config only, no repositories (Step 6) |
| 5 | Wire UI | ✅ Health check page (Step 7) |
Phase Spec Version: 1.0.0 | Created: 2026-02-15 | Aligned with Constitution v1.1.0