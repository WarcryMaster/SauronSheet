# Phase 0 Implementation Plan

**Version**: 1.0.0  
**Created**: 2026-02-15  
**Aligned with**: Constitution v1.1.0, Phase 0 Spec v1.0.0, Full Spec v1.0.0  
**Duration**: Weeks 1–2  
**Goal**: Foundation with 6 projects, base abstractions, testing infrastructure

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Implementation Phases](#implementation-phases)
- [Task Breakdown by Component](#task-breakdown-by-component)
- [Dependency Graph](#dependency-graph)
- [Red-Green-Refactor Workflow](#red-green-refactor-workflow)
- [Validation Checkpoints](#validation-checkpoints)
- [Risk Mitigation](#risk-mitigation)

---

## Executive Summary

Phase 0 establishes the foundation for SauronSheet with **6 interconnected projects** enforcing Clean Architecture. This plan follows **Red-Green-Refactor** (TDD) and **Spec-Driven Development** (SDD) workflows to deliver:

- ✅ Solution structure with dependency enforcement
- ✅ Domain.Common base abstractions (Entity<TId>, ValueObject, etc.)
- ✅ Exception hierarchy (DomainException, EntityNotFoundException)
- ✅ Specification pattern interface (ISpecification<T>)
- ✅ Application DI with MediatR 12+
- ✅ Infrastructure Supabase configuration validation
- ✅ Frontend minimal Razor Pages setup
- ✅ 13+ passing tests with proper categorization
- ✅ CI/CD validation (`dotnet build`, `dotnet test`)

**Key Constraint**: Domain layer MUST have **ZERO NuGet dependencies** and **ZERO project references**.

---

## Implementation Phases

### Phase 0A: Project Scaffolding (Days 1-2)
Create solution structure with correct dependency graph.

### Phase 0B: Domain Layer (Days 3-5)
Implement base abstractions with full unit test coverage.

### Phase 0C: Application Layer (Days 5-6)
Register MediatR and define IUserContext contract.

### Phase 0D: Infrastructure Layer (Days 6-7)
Supabase client configuration with validation.

### Phase 0E: Frontend Layer (Days 7-8)
Razor Pages with health check page.

### Phase 0F: Integration & Validation (Days 8-10)
E2E validation, coverage reporting, documentation.

---

## Task Breakdown by Component

### 0. PRE-IMPLEMENTATION

#### 0.1: Environment Validation
```sh
✓ dotnet --version              # Must be 10.0.100 or later
✓ git status                     # Ensure clean workspace
✓ Supabase account setup         # Obtain URL and Key for appsettings
```

**Acceptance Criteria:**
- .NET SDK version ≥ 10.0.100 (or .NET 9.x if 10 unavailable)
- Git repository clean
- Supabase credentials available

---

### 1. PROJECT SCAFFOLDING

#### 1.1: Create Solution File

**Task**: Create `SauronSheet.sln` with proper folder structure

```sh
dotnet new sln --name SauronSheet

# Create folder structure
mkdir src tests

# Create .csproj files (empty for now)
# Projects will be added via add project commands
```

**Files Created:**
- `SauronSheet.sln` (root)
- `src/` (directory)
- `tests/` (directory)

---

#### 1.2: Create Domain Project

**Task**: Create `SauronSheet.Domain` with ZERO dependencies

```sh
cd src
dotnet new classlib --name SauronSheet.Domain --framework net10.0
cd ../..
dotnet sln add src/SauronSheet.Domain/SauronSheet.Domain.csproj
```

**Verification**:

```sh
# Verify .csproj has NO <ProjectReference> and NO <PackageReference>
cat src/SauronSheet.Domain/SauronSheet.Domain.csproj | grep -E "ProjectReference|PackageReference"
# Result: (empty - should return nothing)
```

**Directory Structure Created:**
```
src/SauronSheet.Domain/
├── SauronSheet.Domain.csproj    (empty)
├── Class1.cs                     (delete)
└── obj/, bin/
```

---

#### 1.3: Create Application Project

**Task**: Create `SauronSheet.Application` → references Domain only

```sh
cd src
dotnet new classlib --name SauronSheet.Application --framework net10.0
cd ../..
dotnet sln add src/SauronSheet.Application/SauronSheet.Application.csproj
dotnet add src/SauronSheet.Application/SauronSheet.Application.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj
```

**Verification**:

```sh
# Verify Application ONLY references Domain
grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj
# Expected: Include="../SauronSheet.Domain/SauronSheet.Domain.csproj"
```

---

#### 1.4: Create Infrastructure Project

**Task**: Create `SauronSheet.Infrastructure` → references Domain only

```sh
cd src
dotnet new classlib --name SauronSheet.Infrastructure --framework net10.0
cd ../..
dotnet sln add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj
dotnet add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj
```

**Verification**:

```sh
# Verify Infrastructure ONLY references Domain
grep "ProjectReference" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj
# Expected: Include="../SauronSheet.Domain/SauronSheet.Domain.csproj"
```

---

#### 1.5: Create Frontend (Razor Pages) Project

**Task**: Create `SauronSheet.Frontend` → references Application + Infrastructure (DI only)

```sh
cd src
dotnet new webapp --name SauronSheet.Frontend --framework net10.0
cd ../..
dotnet sln add src/SauronSheet.Frontend/SauronSheet.Frontend.csproj
dotnet add src/SauronSheet.Frontend/SauronSheet.Frontend.csproj reference src/SauronSheet.Application/SauronSheet.Application.csproj
dotnet add src/SauronSheet.Frontend/SauronSheet.Frontend.csproj reference src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj
```

**Verification**:

```sh
# Verify Frontend references BOTH Application and Infrastructure
grep "ProjectReference" src/SauronSheet.Frontend/SauronSheet.Frontend.csproj | wc -l
# Expected: 2
```

---

#### 1.6: Create Domain.Tests Project

**Task**: Create `SauronSheet.Domain.Tests` → references Domain + test packages

```sh
cd tests
dotnet new xunit --name SauronSheet.Domain.Tests --framework net10.0
cd ../..
dotnet sln add tests/SauronSheet.Domain.Tests/SauronSheet.Domain.Tests.csproj
dotnet add tests/SauronSheet.Domain.Tests/SauronSheet.Domain.Tests.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj

# Add test packages (already in xunit template)
# Verify xunit, xunit.runner.visualstudio, Moq are present
```

**Verification**:

```sh
# Verify dependencies
grep -E "xunit|Moq" tests/SauronSheet.Domain.Tests/SauronSheet.Domain.Tests.csproj
# Expected: xunit, xunit.runner.visualstudio, Moq
```

---

#### 1.7: Create Application.Tests Project

**Task**: Create `SauronSheet.Application.Tests` → references Application + Domain + test packages

```sh
cd tests
dotnet new xunit --name SauronSheet.Application.Tests --framework net10.0
cd ../..
dotnet sln add tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.csproj
dotnet add tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.csproj reference src/SauronSheet.Application/SauronSheet.Application.csproj
dotnet add tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj
```

---

#### 1.8: Create Directory.Build.props (Root)

**Task**: Configure solution-wide build settings (nullable, warnings-as-errors, implicit usings)

**File**: `Directory.Build.props` (root of solution)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

**Verification**:

```sh
dotnet build
# Should compile with zero warnings
```

---

#### 1.9: Create global.json (Root)

**Task**: Pin .NET SDK version for consistency

**File**: `global.json` (root)

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

**Verification**:

```sh
dotnet --version
# Expected: 10.0.100 (or latest minor within 10.x)
```

---

#### 1.10: Create .gitignore

**Task**: Add standard .NET + IDE ignores

**File**: `.gitignore` (root)

```
# Build folders
bin/
obj/
.vs/
.vscode/

# NuGet
*.nupkg

# Test Results
TestResults/
*.trx
*.coverage

# IDE
*.user
*.suo

# Mac
.DS_Store
```

---

#### Checkpoint 1: Project Scaffolding Complete ✓

```sh
# Verify solution structure
dotnet build
# Expected: Build succeeds with zero errors and zero warnings

# Verify 6 projects created
dotnet sln list | grep "csproj"
# Expected: 6 projects listed
```

**Status**: If build succeeds → Proceed to Phase 0B (Domain Layer)

---

### 2. DOMAIN LAYER

#### 2.1: Write Domain.Tests (RED Phase)

**Task**: Create test stubs for all 11 domain tests

**Before creating test files**: Create directory structure
```sh
mkdir -p tests/SauronSheet.Domain.Tests/Common
mkdir -p tests/SauronSheet.Domain.Tests/Exceptions
mkdir -p tests/SauronSheet.Domain.Tests/Repositories
```

**File**: `tests/SauronSheet.Domain.Tests/Common/EntityBaseTests.cs`

```csharp
using Xunit;

namespace SauronSheet.Domain.Tests.Common;

public class EntityBaseTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_SetsCreatedAtOnConstruction()
    {
        // TODO: RED phase - test will fail until Entity<TId> implemented
        Assert.True(false, "Implement Entity<TId>");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_EqualityByIdAndType()
    {
        Assert.True(false, "Implement Entity equality");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_InequalityByDifferentId()
    {
        Assert.True(false, "Implement Entity inequality");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_InequalityByDifferentType()
    {
        Assert.True(false, "Implement Entity type checking");
    }
}
```

**Additional Test Files to Create**:
- `Common/ValueObjectBaseTests.cs` (2 tests: equality, inequality)
- `Exceptions/DomainExceptionTests.cs` (2 tests: message, inner exception)
- `Exceptions/EntityNotFoundExceptionTests.cs` (2 tests: message format, properties)
- `Repositories/SpecificationBaseTests.cs` (1 test: MaxResults default)

**Total Stubs**: 11 tests, all marked `[Trait("Category", "Domain")]`

**Verification**:
```sh
dotnet test --filter Category=Domain --no-build
# Expected: 11 tests FAIL (red)
```

---

#### 2.2: Implement Entity<TId> (GREEN Phase)

**Task**: Create base entity class with Id, CreatedAt, UpdatedAt

**File**: `src/SauronSheet.Domain/Common/Entity.cs`

```csharp
namespace SauronSheet.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected Entity(TId id)
    {
        // For reference types: check null
        // For value types: check default (which is zero-like value)
        if (id == null || EqualityComparer<TId>.Default.Equals(id, default(TId)))
            throw new ArgumentException("Entity ID cannot be null or empty.", nameof(id));

        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}
```

**Rules Applied**:
- `where TId : notnull` constraint enforces non-nullable type parameters (Guid, string, int are all valid)
- `EqualityComparer<TId>.Default` handles null checks uniformly for all TId types
- No more nullable warnings with `TreatWarningsAsErrors=true`

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build -v n
# Expected: T-0.01 to T-0.04 should now PASS
```

---

#### 2.3: Implement AggregateRoot<TId>

**Task**: Create marker base class inheriting from Entity<TId>

**File**: `src/SauronSheet.Domain/Common/AggregateRoot.cs`

```csharp
namespace SauronSheet.Domain.Common;

/// <summary>
/// Marker base class for aggregate roots.
/// Domain events collection will be added in a future phase.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
{
    protected AggregateRoot(TId id) : base(id)
    {
        // TODO: Add domain events collection in future phase
        // protected List<IDomainEvent> _domainEvents = new();
    }
}
```

**Verification**: Compile check only (no test for AggregateRoot itself in Phase 0)

---

#### 2.4: Implement ValueObject

**Task**: Create abstract record base for value objects

**File**: `src/SauronSheet.Domain/Common/ValueObject.cs`

```csharp
namespace SauronSheet.Domain.Common;

public abstract record ValueObject
{
    // C# record provides value-based equality automatically
    // No additional implementation needed
}
```

**Update ValueObjectBaseTests**:

```csharp
namespace SauronSheet.Domain.Tests.Common;

public record TestValueObject(string Name, int Value) : ValueObject;

public class ValueObjectBaseTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void ValueObject_EqualityByProperties()
    {
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("test", 42);

        Assert.Equal(vo1, vo2);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ValueObject_InequalityByDifferentProperties()
    {
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("different", 42);

        Assert.NotEqual(vo1, vo2);
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName=SauronSheet.Domain.Tests.Common.ValueObjectBaseTests" --no-build
# Expected: 2 tests PASS
```

---

#### 2.5: Implement DomainException

**Task**: Create base domain exception

**File**: `src/SauronSheet.Domain/Exceptions/DomainException.cs`

```csharp
namespace SauronSheet.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Update DomainExceptionTests**:

```csharp
namespace SauronSheet.Domain.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void DomainException_CarriesMessage()
    {
        const string message = "Invalid state detected";
        var ex = new DomainException(message);

        Assert.Equal(message, ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DomainException_CarriesInnerException()
    {
        const string message = "Outer error";
        var inner = new ArgumentException("Inner error");
        var ex = new DomainException(message, inner);

        Assert.Equal(message, ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName=SauronSheet.Domain.Tests.Exceptions.DomainExceptionTests" --no-build
# Expected: 2 tests PASS
```

---

#### 2.6: Implement EntityNotFoundException

**Task**: Create typed "not found" exception

**File**: `src/SauronSheet.Domain/Exceptions/EntityNotFoundException.cs`

```csharp
namespace SauronSheet.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId)
        : base($"Entity '{entityName}' with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
```

**Update EntityNotFoundExceptionTests**:

```csharp
namespace SauronSheet.Domain.Tests.Exceptions;

public class EntityNotFoundExceptionTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void EntityNotFoundException_FormatsMessage()
    {
        var ex = new EntityNotFoundException("Transaction", Guid.NewGuid());

        Assert.Contains("Transaction", ex.Message);
        Assert.Contains("was not found", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void EntityNotFoundException_StoresProperties()
    {
        var id = Guid.NewGuid();
        var ex = new EntityNotFoundException("Category", id);

        Assert.Equal("Category", ex.EntityName);
        Assert.Equal(id, ex.EntityId);
    }
}
```

---

#### 2.7: Implement ISpecification<T>

**Task**: Create specification interface with default MaxResults = 1000

**File**: `src/SauronSheet.Domain/Repositories/ISpecification.cs`

```csharp
using System.Linq.Expressions;

namespace SauronSheet.Domain.Repositories;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    int MaxResults => 1000;
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
}
```

**Update SpecificationBaseTests**:

```csharp
namespace SauronSheet.Domain.Tests.Repositories;

public class SpecificationBaseTests
{
    private class TestSpecification : ISpecification<object>
    {
        public Expression<Func<object, bool>> Criteria => x => true;
        public List<Expression<Func<object, object>>> Includes => new();
        public List<string> IncludeStrings => new();
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Specification_DefaultMaxResultsIs1000()
    {
        var spec = new TestSpecification();

        Assert.Equal(1000, spec.MaxResults);
    }
}
```

---

#### Checkpoint 2: Domain Layer Tests GREEN ✓

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 11 tests PASS
```

**Status**: All domain tests passing → Proceed to Phase 0C (Application Layer)

---

### 3. APPLICATION LAYER

#### 3.1: Write Application.Tests (RED Phase)

**Task**: Create test stubs for MediatR registration

**File**: `tests/SauronSheet.Application.Tests/Common/MediatRRegistrationTests.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Xunit;
using SauronSheet.Application.Common;

namespace SauronSheet.Application.Tests.Common;

public class MediatRRegistrationTests
{
    [Fact]
    [Trait("Category", "Application")]
    public void MediatR_ResolvesFromServiceProvider()
    {
        // RED: Will fail until DependencyInjection.AddApplicationServices() implemented
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();

        Assert.NotNull(mediator);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void AddApplicationServices_RegistersWithoutException()
    {
        var services = new ServiceCollection();

        // Should not throw
        services.AddApplicationServices();

        Assert.NotEmpty(services);
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: 2 tests FAIL (red) — MediatR not referenced yet
```

---

#### 3.2: Add MediatR NuGet Packages

**Task**: Add MediatR to Application.csproj only (not Domain)

```sh
dotnet add src/SauronSheet.Application/SauronSheet.Application.csproj package MediatR
dotnet add src/SauronSheet.Application/SauronSheet.Application.csproj package MediatR.Extensions.Microsoft.DependencyInjection
```

**Note**: Do NOT pin exact version here. The `rollForward: latestMinor` in `global.json` handles version strategy consistently. This allows patch updates within MediatR 12.x without manual .csproj edits.

**Verification**:

```sh
# Verify packages in Application.csproj
grep -E "MediatR" src/SauronSheet.Application/SauronSheet.Application.csproj
# Expected: MediatR 12.0.0+ (actual version determined by rollForward)
```

---

#### 3.3: Create IUserContext Interface

**Task**: Define user context contract (implementation in Phase 1)

**File**: `src/SauronSheet.Application/Common/IUserContext.cs`

```csharp
namespace SauronSheet.Application.Common;

public interface IUserContext
{
    string UserId { get; }
    bool IsAuthenticated { get; }
}
```

---

#### 3.4: Implement DependencyInjection.cs

**Task**: Register MediatR from Application assembly

**File**: `src/SauronSheet.Application/DependencyInjection.cs`

```csharp
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace SauronSheet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly)
        );

        return services;
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: 2 tests PASS
```

---

#### Checkpoint 3: Application Layer Tests GREEN ✓

```sh
dotnet test --filter Category=Application --no-build
# Expected: 2 tests PASS

dotnet test --no-build
# Expected: 13 tests total PASS (11 Domain + 2 Application)
```

---

### 4. INFRASTRUCTURE LAYER

#### 4.1: Add Supabase NuGet Package

**Task**: Add Supabase client to Infrastructure.csproj only

```sh
dotnet add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj package supabase-csharp --version 1.0.0
```

**Note**: Exact version TBD at implementation time; use latest stable.

---

#### 4.2: Implement DependencyInjection.cs

**Task**: Validate Supabase config and register client as singleton

**File**: `src/SauronSheet.Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SauronSheet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validation occurs at DI registration time (in Program.cs when AddInfrastructureServices called)
        // If config is missing, InvalidOperationException thrown immediately - not deferred to first use
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Url' is not set.");

        var supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Key' is not set.");

        // TODO: Register Supabase client as singleton in Phase 1+
        // var client = new SupabaseClient(new Uri(supabaseUrl), supabaseKey);
        // services.AddSingleton(client);

        return services;
    }
}
```

**Validation Timing**:
- Configuration keys validated in `Program.cs` → `builder.Services.AddInfrastructureServices(config)`
- If Supabase:Url or Supabase:Key missing → `InvalidOperationException` thrown immediately
- App startup fails fast with clear message
- No deferred validation to first request

**Note**: Supabase client registration deferred until actual package is chosen and tested (Phase 1+).

---

#### Checkpoint 4: Infrastructure Compiles ✓

```sh
dotnet build
# Expected: Zero errors (Supabase client registration commented out for now)

# Test config validation
dotnet run --project src/SauronSheet.Frontend/SauronSheet.Frontend.csproj
# With missing Supabase:Url in appsettings.json:
# Expected: InvalidOperationException thrown during startup (not on first request)
```

---

### 5. FRONTEND LAYER

#### 5.1: Update Program.cs

**Task**: Register Application and Infrastructure services

**File**: `src/SauronSheet.Frontend/Program.cs`

```csharp
using SauronSheet.Application;
using SauronSheet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication and Authorization middleware (Phase 1)
// app.UseAuthentication();
// app.UseAuthorization();

app.MapRazorPages();

app.Run();
```

---

#### 5.2: Update _Layout.cshtml

**Task**: Add Tailwind CSS CDN and navigation shell

**File**: `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - SauronSheet</title>
    
    <!-- Tailwind CSS CDN -->
    <script src="https://cdn.tailwindcss.com"></script>
</head>
<body class="bg-gray-50">
    <nav class="bg-white shadow">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="flex justify-between h-16">
                <div class="flex items-center">
                    <h1 class="text-2xl font-bold text-blue-600">SauronSheet</h1>
                </div>
                <div class="flex items-center space-x-4">
                    <!-- Placeholder nav links -->
                    <a href="/" class="text-gray-700 hover:text-blue-600">Home</a>
                </div>
            </div>
        </div>
    </nav>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        @RenderBody()
    </main>

    <footer class="bg-white border-t mt-12">
        <div class="max-w-7xl mx-auto px-4 py-4 text-center text-gray-600">
            <p>&copy; 2026 SauronSheet. Version 1.0.0 (Phase 0)</p>
        </div>
    </footer>
</body>
</html>
```

---

#### 5.3: Update Index.cshtml

**Task**: Create health check page with Tailwind styling

**File**: `src/SauronSheet.Frontend/Pages/Index.cshtml`

```html
@page
@model IndexModel
@{
    ViewData["Title"] = "System Status";
}

<div class="flex flex-col items-center justify-center min-h-screen">
    <div class="text-center">
        <h1 class="text-4xl font-bold text-gray-900 mb-4">SauronSheet</h1>
        
        <div class="bg-green-100 border-l-4 border-green-500 text-green-700 p-4 mb-6">
            <p class="text-lg font-semibold">System OK</p>
            <p class="text-sm">Foundation Phase 0 is running successfully</p>
        </div>

        <div class="bg-blue-50 rounded-lg p-6 max-w-md">
            <p class="text-gray-600">
                Current Date/Time: <span class="font-mono text-sm">@DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC</span>
            </p>
        </div>
    </div>
</div>
```

**Update Index.cshtml.cs**:

```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SauronSheet.Frontend.Pages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
        // No MediatR calls in Phase 0 — health check only
    }
}
```

---

#### 5.4: Update appsettings.json

**Task**: Add Supabase configuration placeholders

**File**: `src/SauronSheet.Frontend/appsettings.json`

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

**Create appsettings.Development.json** (for local overrides):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

---

#### 5.5: Verify Frontend Builds

```sh
dotnet build src/SauronSheet.Frontend/SauronSheet.Frontend.csproj
# Expected: Build succeeds

dotnet run --project src/SauronSheet.Frontend/SauronSheet.Frontend.csproj
# Expected: App starts at https://localhost:5001 (or similar)
# Navigate to http://localhost:5000/ to see health check page
```

---

#### Checkpoint 5: Frontend Renders ✓

Visual verification:
- ✓ Page displays "SauronSheet" heading
- ✓ Green status badge shows "System OK"
- ✓ Navigation bar visible with Tailwind styling
- ✓ Responsive layout (inspect with DevTools)

---

### 6. INTEGRATION & VALIDATION

#### 6.1: Full Build

**Task**: Verify entire solution builds with zero warnings

```sh
dotnet build
# Expected: Build succeeds
# Expected: Zero errors, zero warnings (TreatWarningsAsErrors=true)
```

---

#### 6.2: Run All Tests

**Task**: Execute all 13 tests with proper categorization

```sh
dotnet test
# Expected: 13 tests PASS
# Expected: 11 tests tagged Category=Domain
# Expected: 2 tests tagged Category=Application
```

---

#### 6.3: Generate Test Coverage Report

**Task**: Generate code coverage report (Domain ≥ 80%)

**Coverage scope**: Only files in `src/SauronSheet.Domain/` → test files EXCLUDED
**Expected coverage**: ≥ 80% of Domain.Common + Exceptions + Repositories

```sh
# Install coverlet globally
dotnet tool install -g coverlet.console

# Run coverage for Domain layer ONLY
coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Domain.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage.xml" \
  --include "[SauronSheet.Domain]*" \
  --exclude "[SauronSheet.Domain.Tests]*"

# View report (if using codecov or similar service)
# Expected: Domain layer files ≥ 80% coverage
# Files measured:
#   - SauronSheet.Domain/Common/Entity.cs
#   - SauronSheet.Domain/Common/AggregateRoot.cs
#   - SauronSheet.Domain/Common/ValueObject.cs
#   - SauronSheet.Domain/Exceptions/DomainException.cs
#   - SauronSheet.Domain/Exceptions/EntityNotFoundException.cs
#   - SauronSheet.Domain/Repositories/ISpecification.cs
```

---

#### 6.4: Verify Dependency Rules

**Task**: Audit .csproj files to ensure Clean Architecture

```sh
# Objective verification: Check Domain has NO dependencies
echo "=== Domain Dependencies ==="
if grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj > /dev/null; then
  echo "❌ FAIL - Domain has dependencies"
  grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj
else
  echo "✓ PASS - Domain has zero dependencies"
fi

# Check Application references ONLY Domain
echo "=== Application Dependencies ==="
APP_REFS=$(grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | grep -c "Domain")
if [ "$APP_REFS" -eq 1 ]; then
  echo "✓ PASS - Application references Domain only"
else
  echo "❌ FAIL - Application has incorrect references"
  grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj
fi

# Check Infrastructure references ONLY Domain
echo "=== Infrastructure Dependencies ==="
INFRA_REFS=$(grep "ProjectReference" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj | grep -c "Domain")
if [ "$INFRA_REFS" -eq 1 ]; then
  echo "✓ PASS - Infrastructure references Domain only"
else
  echo "❌ FAIL - Infrastructure has incorrect references"
  grep "ProjectReference" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj
fi

# Check Frontend references Application + Infrastructure
echo "=== Frontend Dependencies ==="
FRONTEND_REFS=$(grep "ProjectReference" src/SauronSheet.Frontend/SauronSheet.Frontend.csproj | wc -l)
if [ "$FRONTEND_REFS" -eq 2 ]; then
  echo "✓ PASS - Frontend references 2 projects (Application + Infrastructure)"
else
  echo "❌ FAIL - Frontend has incorrect reference count: $FRONTEND_REFS (expected 2)"
  grep "ProjectReference" src/SauronSheet.Frontend/SauronSheet.Frontend.csproj
fi
```

---

#### Checkpoint 6: All Validations Pass ✓

```sh
# Final checklist
✓ dotnet build succeeds (zero errors, zero warnings)
✓ dotnet test: 13 tests PASS
✓ Domain coverage ≥ 80%
✓ Dependency rules enforced
✓ Frontend health check page renders at localhost
✓ Supabase config validates
✓ CI/CD scripts ready (dotnet build, dotnet test, if added)
```

---

## Dependency Graph

```
┌─────────────────────────────────────────────────────┐
│              SauronSheet.sln                         │
└─────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
    ┌───────────┐    ┌────────────┐   ┌──────────┐
    │   src/    │    │   tests/   │   │ Root Cfg │
    └───────────┘    └────────────┘   └──────────┘
        │                   │              │
   ┌────┴──────┐      ┌─────┴─────┐     │
   │            │      │           │     │
┌──────────┐  ┌──────┐┌──────────┐┌──┐ ┌──┐
│ Domain   │  │ App  ││Infra     ││F ││.c│
│ (NO deps)│  │(→D)  ││(→D)      ││r││o│
└──────────┘  └──────┘└──────────┘└──┘ └──┘
   ↑              ↑        ↑           ↑   ↑
   │              │        │       (DI reg) global
Domain.Tests      App.Tests Infrastructure   json
   (→D)            (→A→D)        (→D)       (SDK)
                                Frontend
                              (→A→I)
```

**Key Rules**:
- Domain → ZERO external dependencies
- Application → Domain only
- Infrastructure → Domain only
- Frontend → Application + Infrastructure (DI only, no direct service calls)
- Tests → Respective project + base classes

---

## Red-Green-Refactor Workflow

### Example: Implementing Entity<TId>

**Step 1: RED**
- Write test stub for T-0.01: `Entity_SetsCreatedAtOnConstruction()`
- Test FAILS (Entity<TId> doesn't exist)

**Step 2: GREEN**
- Implement minimal Entity<TId>: constructor, Id, CreatedAt, UpdatedAt properties
- Test PASSES

**Step 3: REFACTOR**
- Add equality methods (GetHashCode, Equals)
- Write tests T-0.02, T-0.03, T-0.04
- Tests FAIL initially, then implement logic → PASS

**Result**: Entity<TId> fully tested, no untested code paths

---

## Validation Checkpoints

### Checkpoint 1: Project Scaffolding (End of Day 2)
```
Status: ✓ PASS
Verification Command: dotnet build && dotnet sln list
Metrics:
  ✓ dotnet build succeeds with zero errors, zero warnings
  ✓ 6 projects created (visible in `dotnet sln list`)
  ✓ Dependency graph verified via 6.4 script (below)
```

### Checkpoint 2: Domain Layer (End of Day 5)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Domain --no-build
Metrics:
  ✓ 11 domain tests PASS (all assertions green)
  ✓ Domain.csproj has ZERO dependencies (audit script in 6.4)
  ✓ Coverage ≥ 80% (coverlet report in 6.3)
```

### Checkpoint 3: Application Layer (End of Day 6)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Application --no-build
Metrics:
  ✓ 2 application tests PASS
  ✓ MediatR resolves (T-0.12 assertion: Assert.NotNull(mediator))
  ✓ Total 13 tests PASS (11 Domain + 2 Application)
```

### Checkpoint 4: Infrastructure Layer (End of Day 7)
```
Status: ✓ PASS
Verification Command: dotnet build && dotnet run --project src/SauronSheet.Frontend/
Metrics:
  ✓ Infrastructure compiles (Supabase client registration commented)
  ✓ Supabase config validation works:
    - Valid config: app starts
    - Missing Supabase:Url: InvalidOperationException on startup (not deferred)
```

### Checkpoint 5: Frontend Layer (End of Day 8)
```
Status: ✓ PASS
Verification Command: dotnet run --project src/SauronSheet.Frontend/
Visual Check (in browser at http://localhost:5000):
  ✓ Page displays "SauronSheet" heading (h1)
  ✓ Green status badge shows "System OK"
  ✓ Navigation bar visible with Tailwind styling
  ✓ Responsive layout (DevTools: resize to mobile, tablet, desktop)
  ✓ Tailwind CDN loaded (inspect: <script src="https://cdn.tailwindcss.com"></script>)
```

### Checkpoint 6: Integration & Validation (End of Day 10)
```
Status: ✓ PASS
Verification Commands (run in order):
  1. dotnet build                    # Exit code 0, zero warnings
  2. dotnet test                     # Output: "13 passed"
  3. coverlet (see 6.3)              # Domain coverage ≥ 80%
  4. Bash script from 6.4            # All assertions PASS
  5. dotnet run + browser            # Frontend renders at http://localhost:5000/

Final Metrics:
  ✓ Full build: zero errors, zero warnings (TreatWarningsAsErrors enforced)
  ✓ All 13 tests: PASS (11 Domain + 2 Application)
  ✓ Coverage reports generated (coverage.xml)
  ✓ Dependency rules verified (Domain=0 refs, App→D, Infra→D, Frontend→A+I)
  ✓ Solution structure correct (6 projects per FR-0.01)
  ✓ Solution ready for Phase 1
```

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| .NET 10 SDK not available | Medium | High | Use global.json; fallback to .NET 9 |
| Tailwind CDN unreliable | Low | Low | Defer to Phase 6; use CDN backup URL |
| MediatR version conflicts | Low | Medium | Pin exact version in .csproj |
| Test flakiness (DateTime.UtcNow) | Medium | Low | Use ±1s tolerance in T-0.01 |
| Coverage tools not available | Low | Medium | Manual code inspection backup |
| Git merge conflicts | Low | Medium | Commit frequently; branch strategy |

---

## Success Criteria Summary

| Criterion | Status | Objective Validation Command |
|-----------|--------|-----------|
| 6 projects created | ✓ | `dotnet sln list \| grep "csproj"` → output shows exactly 6 .csproj files |
| Build succeeds | ✓ | `dotnet build` → exit code 0 (no errors, no warnings) |
| 13 tests pass | ✓ | `dotnet test` → output shows "13 passed" |
| Domain coverage ≥ 80% | ✓ | `coverlet` report (6.3) shows Domain files ≥ 80% |
| Dependency rules enforced | ✓ | Bash script in 6.4 shows all assertions PASS |
| Frontend health check | ✓ | Browser at `http://localhost:5000/` shows "System OK" page |
| Supabase config validates | ✓ | Remove `Supabase:Url` from appsettings → app startup throws `InvalidOperationException` |
| CI/CD scripts ready | ✓ | `dotnet build` + `dotnet test` commands work locally (no GitHub Actions yet) |

---

## Next Steps (Post-Phase 0)

Once Phase 0 is complete and all checkpoints PASS:

1. **Merge to main**: Create PR with all Phase 0 deliverables
2. **Begin Phase 1**: Transition to Authentication & Multi-Tenancy
3. **Phase 1 prep**: Domain tests still passing? ✓ Ready for Phase 1 auth layer

---

**Created**: 2026-02-15  
**Version**: 1.0.0  
**Duration**: 10 days (Weeks 1–2)  
**Status**: Ready for implementation ✅
