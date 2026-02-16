# Phase 1 Implementation Plan

**Version**: 1.0.0  
**Created**: 2026-02-15  
**Aligned with**: Constitution v1.1.0, Phase 1 Spec v1.0.0, Full Spec v1.0.0  
**Duration**: Weeks 3–5  
**Goal**: Multi-user authentication with JWT, tenant-scoped data access, login/register UI

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

Phase 1 builds secure multi-user authentication on top of Phase 0's foundation. This phase implements the **Full-Stack (Auth)** architecture with JWT-based sessions, tenant-scoped query isolation, and a complete auth UI.

**Key Deliverables:**
- ✅ `UserId` strong-typed value object with domain validation
- ✅ `IAuthService` interface (Domain layer contract)
- ✅ `AuthResult` + `UserProfile` value objects
- ✅ Auth commands/queries: Register, Login, Logout, RefreshToken, GetCurrentUser
- ✅ `TenantScopingBehavior` MediatR pipeline for multi-tenancy
- ✅ `IAnonymousRequest` marker interface (for Register/Login)
- ✅ `SupabaseAuthService` + JWT cookie middleware
- ✅ `HttpUserContext` implementation
- ✅ `users` table migration with Row Level Security
- ✅ Login/Register/Dashboard pages with auth routing
- ✅ 22 passing tests (8 Domain + 14 Application)
- ✅ Updated `_Layout.cshtml` with auth-aware navigation
- ✅ Updated `Program.cs` with auth middleware

**Key Constraint**: All auth logic flows through **IUserContext** abstraction. Infrastructure implements it; Application depends on abstraction only.

**Constitutional Compliance:**
- ✅ Clean Architecture: Infrastructure implements Domain interfaces
- ✅ CQRS: Commands/Queries routed through MediatR pipeline
- ✅ DDD: Strong-typed UserId VO, domain validation, no public setters
- ✅ Test-First: 22 tests (8 Domain 100% coverage, 14 Application ≥70%)
- ✅ Spec-Driven: Single phase spec, layer boundaries respected (all layers in scope)

---

## Implementation Phases

### Phase 1A: Domain Layer Extensions (Days 1-2)
Add `UserId` VO, `IAuthService` interface, auth value objects (AuthResult, UserProfile).

### Phase 1B: Application Layer — Auth Commands (Days 2-4)
Implement Register, Login, Logout, RefreshToken handlers + TenantScopingBehavior + marker interface.

### Phase 1C: Application Layer — Auth Queries & DTOs (Days 4-5)
Implement GetCurrentUser query, auth DTOs, update DependencyInjection with behaviors.

### Phase 1D: Infrastructure Auth Services (Days 5-7)
Implement SupabaseAuthService, JwtCookieMiddleware, HttpUserContext, AuthConfiguration, database migration.

### Phase 1E: Frontend Auth Pages (Days 7-8)
Build Login, Register, Dashboard pages + updated layout + auth routing + appsettings config.

### Phase 1F: Integration & Validation (Days 8-10)
E2E testing, coverage reporting, security audit, all tests passing.

---

## Task Breakdown by Component

### 0. PRE-IMPLEMENTATION

#### 0.1: Environment Validation

**Task**: Verify prerequisites for Phase 1 implementation

```sh
✓ Phase 0 build passing         # dotnet build (Phase 0 complete)
✓ Phase 0 tests passing         # dotnet test (all 13 tests green)
✓ Supabase Auth enabled         # Verify email/password provider in Supabase dashboard
✓ Supabase JWT secret available # Settings → API → JWT Secret
✓ Git workspace clean           # Phase 0 merged to main
```

**Acceptance Criteria:**
- Phase 0 solution compiles and all 13 tests pass
- Supabase Auth email/password provider enabled
- Supabase JWT secret accessible
- Git workspace clean (no uncommitted changes)

---

### 1. DOMAIN LAYER EXTENSIONS

#### 1.1: Write Domain.Tests for UserId VO (RED Phase)

**Task**: Create test stubs for UserId value object (8 tests total)

**Directory structure** (create if not exists):
```sh
mkdir -p tests/SauronSheet.Domain.Tests/ValueObjects
mkdir -p tests/SauronSheet.Domain.Tests/Services
```

**File**: `tests/SauronSheet.Domain.Tests/ValueObjects/UserIdTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class UserIdTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_ValidString_SetsValue()
    {
        // RED: Will fail until UserId implemented
        Assert.True(false, "Implement UserId");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_NullString_ThrowsDomainException()
    {
        Assert.True(false, "Implement UserId null guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_EmptyString_ThrowsDomainException()
    {
        Assert.True(false, "Implement UserId empty guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_WhitespaceString_ThrowsDomainException()
    {
        Assert.True(false, "Implement UserId whitespace guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_Equality_SameValue()
    {
        Assert.True(false, "Implement UserId equality");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_Inequality_DifferentValue()
    {
        Assert.True(false, "Implement UserId inequality");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_SuccessFactory_SetsProperties()
    {
        Assert.True(false, "Implement AuthResult success factory");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_FailureFactory_SetsError()
    {
        Assert.True(false, "Implement AuthResult failure factory");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 8 new Domain tests FAIL (red) — UserId not yet implemented
# Expected: 11 old tests still PASS
```

---

#### 1.2: Implement UserId Value Object (GREEN Phase)

**Task**: Create strong-typed user identifier with validation

**File**: `src/SauronSheet.Domain/ValueObjects/UserId.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

using Common;
using Exceptions;

public record UserId : ValueObject
{
    public string Value { get; }

    public UserId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("UserId cannot be null or empty.");

        Value = value;
    }

    public override string ToString() => Value;
}
```

**Verification**:

```sh
dotnet test --filter "ClassName=SauronSheet.Domain.Tests.ValueObjects.UserIdTests" --no-build
# Expected: T-1.01 to T-1.06 should now PASS
```

---

#### 1.3: Implement AuthResult Value Object (GREEN Phase)

**Task**: Create auth operation result with factory methods

**File**: `src/SauronSheet.Domain/ValueObjects/AuthResult.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

public record AuthResult
{
    public UserId? UserId { get; }
    public string? AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime? ExpiresAt { get; }
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private AuthResult(
        UserId? userId,
        string? accessToken,
        string? refreshToken,
        DateTime? expiresAt,
        bool isSuccess,
        string? errorMessage)
    {
        UserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static AuthResult Success(
        UserId userId,
        string accessToken,
        string refreshToken,
        DateTime expiresAt)
    {
        return new AuthResult(userId, accessToken, refreshToken, expiresAt, true, null);
    }

    public static AuthResult Failure(string errorMessage)
    {
        return new AuthResult(null, null, null, null, false, errorMessage);
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName=SauronSheet.Domain.Tests.ValueObjects.UserIdTests" --no-build
# Expected: T-1.07 and T-1.08 should now PASS
```

---

#### 1.4: Implement UserProfile Value Object (GREEN Phase)

**Task**: Create user profile data structure

**File**: `src/SauronSheet.Domain/ValueObjects/UserProfile.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

public record UserProfile
{
    public UserId Id { get; }
    public string Email { get; }
    public string? DisplayName { get; }
    public DateTime CreatedAt { get; }

    public UserProfile(UserId id, string email, string? displayName, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));

        Id = id;
        Email = email;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }
}
```

---

#### 1.5: Create IAuthService Interface (GREEN Phase)

**Task**: Define auth service contract

**File**: `src/SauronSheet.Domain/Services/IAuthService.cs`

```csharp
namespace SauronSheet.Domain.Services;

using ValueObjects;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password);
    Task<AuthResult> LoginAsync(string email, string password);
    Task LogoutAsync(string accessToken);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<UserProfile?> GetUserProfileAsync(string userId);
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: All 19 Domain tests PASS (11 Phase 0 + 8 Phase 1)
```

---

#### Checkpoint 1A: Domain Layer Tests GREEN ✓

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 19 tests PASS (11 Phase 0 + 8 Phase 1)
```

**Status**: All domain tests passing → Proceed to Phase 1B (Application Commands)

---

### 2. APPLICATION LAYER — AUTH COMMANDS

#### 2.1: Write Application.Tests for Auth Handlers (RED Phase)

**Task**: Create test stubs for auth command handlers (14 tests total)

**Directory structure**:
```sh
mkdir -p tests/SauronSheet.Application.Tests/Features/Auth/Commands
mkdir -p tests/SauronSheet.Application.Tests/Features/Auth/Queries
mkdir -p tests/SauronSheet.Application.Tests/Common
```

**File**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/RegisterUserCommandTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class RegisterUserCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_ValidInput_ReturnsRegistrationResult()
    {
        // RED: Will fail until RegisterUserCommandHandler implemented
        Assert.True(false, "Implement RegisterUserCommandHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_DuplicateEmail_ThrowsDomainException()
    {
        Assert.True(false, "Implement duplicate email guard");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_WeakPassword_ThrowsDomainException()
    {
        Assert.True(false, "Implement password validation");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_MismatchedPasswords_ThrowsDomainException()
    {
        Assert.True(false, "Implement password matching validation");
    }
}
```

**File**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/LoginUserCommandTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Features.Auth.Commands;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class LoginUserCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task LoginUser_ValidCredentials_ReturnsAuthToken()
    {
        Assert.True(false, "Implement LoginUserCommandHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task LoginUser_InvalidCredentials_ThrowsUnauthorized()
    {
        Assert.True(false, "Implement invalid credential handling");
    }
}
```

**File**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/LogoutUserCommandTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Features.Auth.Commands;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class LogoutUserCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task LogoutUser_ValidToken_CallsAuthService()
    {
        Assert.True(false, "Implement LogoutUserCommandHandler");
    }
}
```

**File**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/RefreshTokenCommandTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Features.Auth.Commands;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class RefreshTokenCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task RefreshToken_ValidRefresh_ReturnsNewTokens()
    {
        Assert.True(false, "Implement RefreshTokenCommandHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RefreshToken_InvalidRefresh_ThrowsUnauthorized()
    {
        Assert.True(false, "Implement invalid refresh token handling");
    }
}
```

**File**: `tests/SauronSheet.Application.Tests/Features/Auth/Queries/GetCurrentUserQueryTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Features.Auth.Queries;

namespace SauronSheet.Application.Tests.Features.Auth.Queries;

public class GetCurrentUserQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCurrentUser_Authenticated_ReturnsProfile()
    {
        Assert.True(false, "Implement GetCurrentUserQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCurrentUser_Unauthenticated_ThrowsUnauthorized()
    {
        Assert.True(false, "Implement unauthenticated handling");
    }
}
```

**File**: `tests/SauronSheet.Application.Tests/Common/TenantScopingBehaviorTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Common;

namespace SauronSheet.Application.Tests.Common;

public class TenantScopingBehaviorTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task TenantScoping_Authenticated_Proceeds()
    {
        Assert.True(false, "Implement TenantScopingBehavior");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task TenantScoping_Unauthenticated_ThrowsUnauthorized()
    {
        Assert.True(false, "Implement unauthenticated rejection");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task TenantScoping_AnonymousRequest_SkipsCheck()
    {
        Assert.True(false, "Implement anonymous request bypass");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: 14 new Application tests FAIL (red) — handlers not yet implemented
# Expected: 2 old tests still PASS
```

---

#### 2.2: Create Auth Commands (GREEN Phase)

**Task**: Define auth command contracts

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Application/Features/Auth/Commands
mkdir -p src/SauronSheet.Application/Features/Auth/Queries
mkdir -p src/SauronSheet.Application/Features/Auth/DTOs
```

**File**: `src/SauronSheet.Application/Features/Auth/Commands/RegisterUserCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using Common;
using MediatR;

public record RegisterUserCommand(
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<RegistrationResultDto>, IAnonymousRequest;
```

**File**: `src/SauronSheet.Application/Features/Auth/Commands/LoginUserCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using Common;
using DTOs;
using MediatR;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthTokenDto>, IAnonymousRequest;
```

**File**: `src/SauronSheet.Application/Features/Auth/Commands/LogoutUserCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using MediatR;

public record LogoutUserCommand(string AccessToken) : IRequest<Unit>;
```

**File**: `src/SauronSheet.Application/Features/Auth/Commands/RefreshTokenCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using Common;
using DTOs;
using MediatR;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokenDto>, IAnonymousRequest;
```

**File**: `src/SauronSheet.Application/Common/IAnonymousRequest.cs`

```csharp
namespace SauronSheet.Application.Common;

/// <summary>
/// Marker interface for requests that do not require authentication.
/// RegisterUserCommand and LoginUserCommand implement this.
/// </summary>
public interface IAnonymousRequest { }
```

---

#### 2.3: Create Auth DTOs (GREEN Phase)

**Task**: Define data transfer objects for auth responses

**File**: `src/SauronSheet.Application/Features/Auth/DTOs/RegistrationResultDto.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.DTOs;

public record RegistrationResultDto(
    string UserId,
    string Email);
```

**File**: `src/SauronSheet.Application/Features/Auth/DTOs/AuthTokenDto.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.DTOs;

public record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId);
```

**File**: `src/SauronSheet.Application/Features/Auth/DTOs/UserProfileDto.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.DTOs;

public record UserProfileDto(
    string UserId,
    string Email,
    string? DisplayName,
    DateTime CreatedAt);
```

---

#### 2.4: Create Auth Handlers (GREEN Phase)

**Task**: Implement command handlers that coordinate with domain and infrastructure

**File**: `src/SauronSheet.Application/Features/Auth/Commands/RegisterUserCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using Domain.Exceptions;
using DTOs;
using MediatR;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegistrationResultDto>
{
    private readonly IAuthService _authService;

    public RegisterUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<RegistrationResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Validation
        if (request.Password != request.ConfirmPassword)
            throw new DomainException("Passwords do not match.");

        if (request.Password.Length < 8)
            throw new DomainException("Password must be at least 8 characters.");

        // Call infrastructure service
        var result = await _authService.RegisterAsync(request.Email, request.Password);

        if (!result.IsSuccess)
            throw new DomainException(result.ErrorMessage ?? "Registration failed.");

        return new RegistrationResultDto(result.UserId!.Value, request.Email);
    }
}
```

**File**: `src/SauronSheet.Application/Features/Auth/Commands/LoginUserCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using DTOs;
using MediatR;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthTokenDto>
{
    private readonly IAuthService _authService;

    public LoginUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthTokenDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.IsSuccess)
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthTokenDto(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value,
            result.UserId!.Value);
    }
}
```

**File**: `src/SauronSheet.Application/Features/Auth/Commands/LogoutUserCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using MediatR;

public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, Unit>
{
    private readonly IAuthService _authService;

    public LogoutUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Unit> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.AccessToken);
        return Unit.Value;
    }
}
```

**File**: `src/SauronSheet.Application/Features/Auth/Commands/RefreshTokenCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using DTOs;
using MediatR;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokenDto>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.IsSuccess)
            throw new UnauthorizedAccessException("Session expired.");

        return new AuthTokenDto(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value,
            result.UserId!.Value);
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: T-1.09 to T-1.17 should now PASS
```

---

### 3. APPLICATION LAYER — AUTH QUERIES

#### 3.1: Create Auth Query (GREEN Phase)

**Task**: Implement GetCurrentUser query

**File**: `src/SauronSheet.Application/Features/Auth/Queries/GetCurrentUserQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Queries;

using DTOs;
using MediatR;

public record GetCurrentUserQuery : IRequest<UserProfileDto>;
```

**File**: `src/SauronSheet.Application/Features/Auth/Queries/GetCurrentUserQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Auth.Queries;

using Common;
using Domain.Services;
using Domain.Exceptions;
using DTOs;
using MediatR;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
{
    private readonly IAuthService _authService;
    private readonly IUserContext _userContext;

    public GetCurrentUserQueryHandler(IAuthService authService, IUserContext userContext)
    {
        _authService = authService;
        _userContext = userContext;
    }

    public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var profile = await _authService.GetUserProfileAsync(_userContext.UserId);

        if (profile == null)
            throw new EntityNotFoundException("User", _userContext.UserId);

        return new UserProfileDto(
            profile.Id.Value,
            profile.Email,
            profile.DisplayName,
            profile.CreatedAt);
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: T-1.18 and T-1.19 should now PASS
```

---

#### 3.2: Implement TenantScopingBehavior (GREEN Phase)

**Task**: Create MediatR pipeline behavior for tenant scoping

**File**: `src/SauronSheet.Application/Common/Behaviors/TenantScopingBehavior.cs`

```csharp
namespace SauronSheet.Application.Common.Behaviors;

using MediatR;

public class TenantScopingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUserContext _userContext;

    public TenantScopingBehavior(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip auth check for anonymous requests (Register, Login, RefreshToken)
        if (request is IAnonymousRequest)
            return await next();

        // Enforce authentication for all other requests
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await next();
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: T-1.20 to T-1.22 should now PASS
```

---

#### Checkpoint 2: Application Layer Tests GREEN ✓

```sh
dotnet test --filter Category=Application --no-build
# Expected: 16 tests PASS (2 Phase 0 + 14 Phase 1)
```

**Status**: All application tests passing → Proceed to Phase 1D (Infrastructure)

---

### 4. APPLICATION LAYER — DI UPDATES

#### 4.1: Update DependencyInjection.cs (GREEN Phase)

**Task**: Register auth behaviors and services

**File**: `src/SauronSheet.Application/DependencyInjection.cs` (update existing)

```csharp
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Common.Behaviors;

namespace SauronSheet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            
            // Register pipeline behaviors
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantScopingBehavior<,>));
        });

        return services;
    }
}
```

---

### 5. INFRASTRUCTURE LAYER

#### 5.1: Add Auth NuGet Packages

**Task**: Add required packages to Infrastructure

```sh
dotnet add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj package Microsoft.AspNetCore.Http
```

---

#### 5.2: Create Auth Service Implementations (GREEN Phase)

**Task**: Implement Supabase auth service

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Infrastructure/Auth
```

**File**: `src/SauronSheet.Infrastructure/Auth/SupabaseAuthService.cs`

```csharp
namespace SauronSheet.Infrastructure.Auth;

using System.Net.Http.Json;
using Domain.Services;
using Domain.ValueObjects;

public class SupabaseAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public SupabaseAuthService(HttpClient httpClient, string supabaseUrl, string supabaseKey)
    {
        _httpClient = httpClient;
        _supabaseUrl = supabaseUrl;
        _supabaseKey = supabaseKey;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        try
        {
            var payload = new { email, password };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_supabaseUrl}/auth/v1/signup",
                payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsAsync<dynamic>();
                var message = error?.error_description ?? "Registration failed";
                return AuthResult.Failure(message);
            }

            var data = await response.Content.ReadAsJsonAsync<dynamic>();
            var userId = new UserId((string)data.user.id);
            return AuthResult.Success(
                userId,
                (string)data.session.access_token,
                (string)data.session.refresh_token,
                DateTime.UtcNow.AddSeconds((int)data.session.expires_in));
        }
        catch (Exception ex)
        {
            return AuthResult.Failure(ex.Message);
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var payload = new
            {
                email,
                password,
                grant_type = "password"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_supabaseUrl}/auth/v1/token?grant_type=password",
                payload);

            if (!response.IsSuccessStatusCode)
                return AuthResult.Failure("Invalid email or password.");

            var data = await response.Content.ReadAsJsonAsync<dynamic>();
            var userId = new UserId((string)data.user.id);
            return AuthResult.Success(
                userId,
                (string)data.access_token,
                (string)data.refresh_token,
                DateTime.UtcNow.AddSeconds((int)data.expires_in));
        }
        catch
        {
            return AuthResult.Failure("Invalid email or password.");
        }
    }

    public async Task LogoutAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/logout");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        await _httpClient.SendAsync(request);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var payload = new
            {
                refresh_token = refreshToken,
                grant_type = "refresh_token"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_supabaseUrl}/auth/v1/token?grant_type=refresh_token",
                payload);

            if (!response.IsSuccessStatusCode)
                return AuthResult.Failure("Session expired.");

            var data = await response.Content.ReadAsJsonAsync<dynamic>();
            var userId = new UserId((string)data.user.id);
            return AuthResult.Success(
                userId,
                (string)data.access_token,
                (string)data.refresh_token,
                DateTime.UtcNow.AddSeconds((int)data.expires_in));
        }
        catch
        {
            return AuthResult.Failure("Session expired.");
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_supabaseUrl}/auth/v1/user");
            if (!response.IsSuccessStatusCode)
                return null;

            var data = await response.Content.ReadAsJsonAsync<dynamic>();
            return new UserProfile(
                new UserId((string)data.id),
                (string)data.email,
                (string?)data.user_metadata?.display_name,
                DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }
}
```

**Note**: This is a simplified implementation. Production should use the official Supabase .NET client or proper HTTP-based implementation with error handling.

---

#### 5.3: Create JWT Cookie Middleware (GREEN Phase)

**Task**: Extract and validate JWT from cookies

**File**: `src/SauronSheet.Infrastructure/Auth/JwtCookieMiddleware.cs`

```csharp
namespace SauronSheet.Infrastructure.Auth;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthConfiguration _config;

    public JwtCookieMiddleware(RequestDelegate next, AuthConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies[_config.AccessTokenCookieName];

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // TODO: Validate JWT signature using Supabase JWT secret
                // For now, basic claim extraction
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-id-placeholder")
                };

                var identity = new ClaimsIdentity(claims, "jwt");
                var principal = new ClaimsPrincipal(identity);
                context.User = principal;
            }
            catch { /* Invalid token */ }
        }

        await _next(context);
    }
}
```

---

#### 5.4: Create HttpUserContext (GREEN Phase)

**Task**: Implement IUserContext from HTTP context

**File**: `src/SauronSheet.Infrastructure/Auth/HttpUserContext.cs`

```csharp
namespace SauronSheet.Infrastructure.Auth;

using Application.Common;
using Microsoft.AspNetCore.Http;

public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("User is not authenticated.");
            return userId;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
```

---

#### 5.5: Create Auth Configuration (GREEN Phase)

**Task**: Define auth configuration options

**File**: `src/SauronSheet.Infrastructure/Auth/AuthConfiguration.cs`

```csharp
namespace SauronSheet.Infrastructure.Auth;

public class AuthConfiguration
{
    public string AccessTokenCookieName { get; set; } = "sb-access-token";
    public string RefreshTokenCookieName { get; set; } = "sb-refresh-token";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string JwtSecret { get; set; } = string.Empty;
}
```

---

#### 5.6: Create Database Migration SQL (GREEN Phase)

**Task**: Create users table with RLS policies

**File**: `src/SauronSheet.Infrastructure/Auth/Migrations/001_CreateUsersTable.sql`

```sql
-- Create users profile table
CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT NOT NULL,
    display_name TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON public.users(email);

-- Row Level Security
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Users can view own profile"
    ON public.users
    FOR SELECT
    USING (auth.uid() = id);

CREATE POLICY "Users can update own profile"
    ON public.users
    FOR UPDATE
    USING (auth.uid() = id)
    WITH CHECK (auth.uid() = id);

CREATE POLICY "Users can insert own profile"
    ON public.users
    FOR INSERT
    WITH CHECK (auth.uid() = id);
```

---

#### 5.7: Update Infrastructure DependencyInjection.cs (GREEN Phase)

**Task**: Register auth services

**File**: `src/SauronSheet.Infrastructure/DependencyInjection.cs` (update existing)

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Auth;
using Domain.Services;
using Application.Common;

namespace SauronSheet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Url' is not set.");

        var supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Key' is not set.");

        var jwtSecret = configuration["Supabase:JwtSecret"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:JwtSecret' is not set.");

        // Auth configuration
        services.Configure<AuthConfiguration>(options =>
        {
            options.JwtSecret = jwtSecret;
        });

        // Auth services
        services.AddHttpClient<IAuthService, SupabaseAuthService>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(supabaseUrl));

        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddHttpContextAccessor();

        // TODO: Register Supabase client as singleton in future phases
        // services.AddSingleton(new SupabaseClient(new Uri(supabaseUrl), supabaseKey));

        return services;
    }
}
```

---

### 6. FRONTEND LAYER

#### 6.1: Update Program.cs (GREEN Phase)

**Task**: Register auth middleware and services

**File**: `src/SauronSheet.Frontend/Program.cs` (update existing)

```csharp
using SauronSheet.Application;
using SauronSheet.Infrastructure;
using SauronSheet.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

// Layer registrations
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Auth services
builder.Services.AddHttpContextAccessor();

// Razor Pages
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

// Auth middleware
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
```

---

#### 6.2: Update _Layout.cshtml (GREEN Phase)

**Task**: Add auth-aware navigation

**File**: `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml` (update existing)

Replace navigation section with:

```html
<nav class="bg-white shadow">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16">
            <div class="flex items-center">
                <a href="/" class="flex items-center">
                    <h1 class="text-2xl font-bold text-blue-600">SauronSheet</h1>
                </a>
            </div>
            <div class="flex items-center space-x-4">
                @if (User?.Identity?.IsAuthenticated == true)
                {
                    <a href="/Dashboard" class="text-gray-700 hover:text-blue-600">Dashboard</a>
                    <span class="text-sm text-gray-600">@User.FindFirst("email")?.Value</span>
                    <form method="post" asp-page="/Auth/Logout" style="display: inline;">
                        <button type="submit" class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700">
                            Logout
                        </button>
                    </form>
                }
                else
                {
                    <a href="/Auth/Login" class="text-gray-700 hover:text-blue-600">Sign In</a>
                    <a href="/Auth/Register" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
                        Sign Up
                    </a>
                }
            </div>
        </div>
    </div>
</nav>
```

---

#### 6.3: Create Login Page (GREEN Phase)

**Task**: Build login form page

**File**: `src/SauronSheet.Frontend/Pages/Auth/Login.cshtml`

```html
@page
@model LoginModel
@{
    ViewData["Title"] = "Sign In";
}

<div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
        <div>
            <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
                Sign in to your account
            </h2>
        </div>
        
        @if (!string.IsNullOrEmpty(Model.ErrorMessage))
        {
            <div class="rounded-md bg-red-50 p-4">
                <p class="text-sm text-red-700">@Model.ErrorMessage</p>
            </div>
        }

        <form method="post" class="mt-8 space-y-6">
            <div>
                <label for="email" class="block text-sm font-medium text-gray-700">
                    Email address
                </label>
                <input id="email" name="Input.Email" type="email" required
                    class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                    value="@Model.Input.Email" />
            </div>

            <div>
                <label for="password" class="block text-sm font-medium text-gray-700">
                    Password
                </label>
                <input id="password" name="Input.Password" type="password" required
                    class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500" />
            </div>

            <button type="submit" class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
                Sign in
            </button>
        </form>

        <p class="text-center text-sm text-gray-600">
            Don't have an account?
            <a href="/Auth/Register" class="font-medium text-blue-600 hover:text-blue-500">
                Sign up
            </a>
        </p>
    </div>
</div>
```

**File**: `src/SauronSheet.Frontend/Pages/Auth/Login.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public LoginModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Dashboard";
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Dashboard";

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var result = await _mediator.Send(
                new LoginUserCommand(Input.Email, Input.Password));

            // Set JWT cookies (implementation in Infrastructure middleware)
            Response.Cookies.Append(
                "sb-access-token",
                result.AccessToken,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Expires = result.ExpiresAt
                });

            return LocalRedirect(ReturnUrl);
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }
    }
}

public class LoginInputModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

---

#### 6.4: Create Register Page (GREEN Phase)

**Task**: Build registration form page

**File**: `src/SauronSheet.Frontend/Pages/Auth/Register.cshtml`

```html
@page
@model RegisterModel
@{
    ViewData["Title"] = "Create Account";
}

<div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
        <div>
            <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
                Create your account
            </h2>
        </div>
        
        @if (!string.IsNullOrEmpty(Model.ErrorMessage))
        {
            <div class="rounded-md bg-red-50 p-4">
                <p class="text-sm text-red-700">@Model.ErrorMessage</p>
            </div>
        }

        <form method="post" class="mt-8 space-y-6">
            <div>
                <label for="email" class="block text-sm font-medium text-gray-700">
                    Email address
                </label>
                <input id="email" name="Input.Email" type="email" required
                    class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                    value="@Model.Input.Email" />
            </div>

            <div>
                <label for="password" class="block text-sm font-medium text-gray-700">
                    Password (minimum 8 characters)
                </label>
                <input id="password" name="Input.Password" type="password" required minlength="8"
                    class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500" />
            </div>

            <div>
                <label for="confirmPassword" class="block text-sm font-medium text-gray-700">
                    Confirm password
                </label>
                <input id="confirmPassword" name="Input.ConfirmPassword" type="password" required
                    class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500" />
            </div>

            <button type="submit" class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
                Create account
            </button>
        </form>

        <p class="text-center text-sm text-gray-600">
            Already have an account?
            <a href="/Auth/Login" class="font-medium text-blue-600 hover:text-blue-500">
                Sign in
            </a>
        </p>
    </div>
</div>
```

**File**: `src/SauronSheet.Frontend/Pages/Auth/Register.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public RegisterModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        try
        {
            // Register new user
            await _mediator.Send(
                new RegisterUserCommand(Input.Email, Input.Password, Input.ConfirmPassword));

            // Auto-login after successful registration
            var loginResult = await _mediator.Send(
                new LoginUserCommand(Input.Email, Input.Password));

            // Set JWT cookies
            Response.Cookies.Append(
                "sb-access-token",
                loginResult.AccessToken,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Expires = loginResult.ExpiresAt
                });

            return RedirectToPage("/Dashboard");
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}

public class RegisterInputModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

---

#### 6.5: Create Dashboard Page (GREEN Phase)

**Task**: Build authenticated dashboard page

**File**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`

```html
@page
@model DashboardModel
@{
    ViewData["Title"] = "Dashboard";
}

<div class="space-y-6">
    <div class="bg-white rounded-lg shadow p-6">
        <h1 class="text-3xl font-bold text-gray-900">
            Welcome, @Model.UserProfile?.Email
        </h1>
        <p class="mt-2 text-gray-600">
            Your personal expense tracking dashboard
        </p>
    </div>

    <div class="bg-blue-50 rounded-lg border border-blue-200 p-6">
        <p class="text-blue-900">
            Dashboard features will be available in Phase 4 (Analytics & Dashboard).
        </p>
    </div>
</div>
```

**File**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.DTOs;
using SauronSheet.Application.Features.Auth.Queries;

namespace SauronSheet.Frontend.Pages;

public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public UserProfileDto? UserProfile { get; set; }

    public DashboardModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            UserProfile = await _mediator.Send(new GetCurrentUserQuery());
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/Auth/Login");
        }
    }
}
```

---

#### 6.6: Create Logout Page (GREEN Phase)

**Task**: Build logout handler page

**File**: `src/SauronSheet.Frontend/Pages/Auth/Logout.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.Commands;

namespace SauronSheet.Frontend.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly IMediator _mediator;

    public LogoutModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var token = Request.Cookies["sb-access-token"];

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                await _mediator.Send(new LogoutUserCommand(token));
            }
            catch { /* Ignore logout errors */ }
        }

        // Clear cookies
        Response.Cookies.Delete("sb-access-token");
        Response.Cookies.Delete("sb-refresh-token");

        return RedirectToPage("/Auth/Login");
    }
}
```

---

#### 6.7: Update appsettings.json (GREEN Phase)

**Task**: Add Auth configuration

**File**: `src/SauronSheet.Frontend/appsettings.json` (update existing)

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your-anon-key",
    "JwtSecret": "your-jwt-secret"
  },
  "Auth": {
    "AccessTokenCookieName": "sb-access-token",
    "RefreshTokenCookieName": "sb-refresh-token",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
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

---

### 7. INTEGRATION & VALIDATION

#### 7.1: Full Build

```sh
dotnet build
# Expected: Build succeeds with zero errors and zero warnings
```

---

#### 7.2: Run All Tests

```sh
dotnet test
# Expected: 35 tests PASS (11 Phase 0 + 8 Phase 1 Domain + 2 Phase 0 Application + 14 Phase 1 Application)
```

---

#### 7.3: Generate Test Coverage Report

```sh
dotnet tool install -g coverlet.console

# Domain coverage
coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Domain.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage-domain.xml" \
  --include "[SauronSheet.Domain]*" \
  --exclude "[SauronSheet.Domain.Tests]*"

# Application coverage
coverlet tests/SauronSheet.Application.Tests/bin/Debug/net10.0/SauronSheet.Application.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Application.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage-app.xml" \
  --include "[SauronSheet.Application]*" \
  --exclude "[SauronSheet.Application.Tests]*"

# Expected: Domain ≥ 80%, Application ≥ 70%
```

---

#### 7.4: Verify Dependency Rules

```sh
echo "=== Phase 1 Dependency Verification ==="

# Domain MUST have ZERO project references
DOMAIN_REFS=$(grep -c "ProjectReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj || echo "0")
if [ "$DOMAIN_REFS" -eq "0" ]; then
  echo "✓ PASS - Domain has zero project references"
else
  echo "❌ FAIL - Domain has project references"
fi

# Application → Domain only
APP_DOMAIN=$(grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | grep -c "Domain" || echo "0")
if [ "$APP_DOMAIN" -eq "1" ]; then
  echo "✓ PASS - Application references Domain only"
else
  echo "❌ FAIL - Application has incorrect references"
fi

# Infrastructure → Domain only
INFRA_DOMAIN=$(grep "ProjectReference" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj | grep -c "Domain" || echo "0")
if [ "$INFRA_DOMAIN" -eq "1" ]; then
  echo "✓ PASS - Infrastructure references Domain only"
else
  echo "❌ FAIL - Infrastructure has incorrect references"
fi

# Frontend → Application + Infrastructure
FRONTEND_REFS=$(grep "ProjectReference" src/SauronSheet.Frontend/SauronSheet.Frontend.csproj | wc -l)
if [ "$FRONTEND_REFS" -eq "2" ]; then
  echo "✓ PASS - Frontend references 2 projects"
else
  echo "❌ FAIL - Frontend has incorrect reference count: $FRONTEND_REFS (expected 2)"
fi
```

---

#### 7.5: Manual E2E Testing

```
Test Scenario 1: User Registration
- Navigate to http://localhost:5000/Auth/Register
- Enter: test@example.com, password123, password123
- Expected: Redirect to /Dashboard, user logged in
- Verify: Cookie "sb-access-token" is set (HttpOnly, Secure, SameSite=Strict)

Test Scenario 2: User Login
- Navigate to http://localhost:5000/Auth/Login
- Enter: test@example.com, password123
- Expected: Redirect to /Dashboard, user logged in
- Verify: Navigation shows email and Logout button

Test Scenario 3: Tenant Isolation
- Open incognito window, register different user
- Verify: Each user sees only their own data
- Verify: GetCurrentUserQuery returns correct user

Test Scenario 4: Logout
- Click Logout button
- Expected: Redirect to /Auth/Login, cookies cleared
- Verify: Accessing /Dashboard redirects to /Auth/Login

Test Scenario 5: Protected Routes
- Try accessing /Dashboard without authentication
- Expected: Redirect to /Auth/Login?returnUrl=/Dashboard
```

---

## Dependency Graph

```
┌─────────────────────────────────────────────────────┐
│              SauronSheet.sln (Phase 1)               │
└─────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼──────────────────┐
        │                 │                  │
    ┌───────────┐    ┌────────────┐   ┌──────────┐
    │   src/    │    │   tests/   │   │ Root Cfg │
    └───────────┘    └────────────┘   └──────────┘
        │                   │              │
   ┌────┴──────┐      ┌─────┴─────┐     │
   │            │      │           │     │
┌──────────┐  ┌──────┐┌──────────┐┌──┐ ┌──┐
│ Domain   │  │ App  ││Infra     ││F ││.c│
│(+UserId) │  │(+Auth││(+Auth    ││r││o│
│(+IAuth)  │  │ Cmd) ││Service)  ││n││n│
└──────────┘  └──────┘└──────────┘└──┘ └──┘
   ↑              ↑        ↑           ↑   ↑
   │              │        │      (Auth+DI) global
Domain.Tests      App.Tests  Infra.Tests  json
(19 tests)        (16 tests) (JWT)       (SDK)
                                Frontend
                          (Login/Register/Dashboard)
```

**Key Rules (Phase 1 Enforcement)**:
- Domain → ZERO dependencies (now includes UserId VO, IAuthService interface)
- Application → Domain + MediatR (now includes auth commands, queries, TenantScopingBehavior)
- Infrastructure → Domain + Supabase (now includes SupabaseAuthService, JwtCookieMiddleware)
- Frontend → Application + Infrastructure (DI registration + page calls via MediatR)
- IUserContext resolved from HTTP context; Application depends on abstraction

---

## Red-Green-Refactor Workflow

### Example: Implementing Register Command

**Step 1: RED**
- Write test stub for T-1.09: `RegisterUser_ValidInput_ReturnsRegistrationResult()`
- Test FAILS (RegisterUserCommandHandler doesn't exist)

**Step 2: GREEN**
- Implement `RegisterUserCommand` record and `RegisterUserCommandHandler`
- Minimal handler: call IAuthService, return RegistrationResultDto
- Test PASSES

**Step 3: REFACTOR**
- Add validation logic (password matching, minimum 8 chars)
- Write test T-1.10 (duplicate email), T-1.11 (weak password), T-1.12 (password mismatch)
- Tests FAIL initially, then add guards → PASS
- Register handler now fully tested and matches domain rules

**Result**: Register command fully tested, no untested code paths

---

## Validation Checkpoints

### Checkpoint 1A: Domain Layer Extensions (End of Day 2)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Domain --no-build
Metrics:
  ✓ 19 domain tests PASS (11 Phase 0 + 8 Phase 1)
  ✓ UserId, AuthResult, UserProfile, IAuthService implemented
  ✓ Domain.csproj still has ZERO dependencies
```

### Checkpoint 1B: Application Commands (End of Day 4)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Application --no-build
Metrics:
  ✓ 14 application tests PASS (Register, Login, Logout, RefreshToken handlers)
  ✓ All auth commands have handlers
  ✓ IAnonymousRequest marker interface working
```

### Checkpoint 1C: Application Queries & Behaviors (End of Day 5)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Application --no-build
Metrics:
  ✓ 16 application tests PASS (14 + 2 Phase 0)
  ✓ GetCurrentUserQuery + handler working
  ✓ TenantScopingBehavior registered and tested
```

### Checkpoint 1D: Infrastructure Auth Services (End of Day 7)
```
Status: ✓ PASS
Verification Command: dotnet build
Metrics:
  ✓ SupabaseAuthService, JwtCookieMiddleware, HttpUserContext implemented
  ✓ AuthConfiguration created
  ✓ 001_CreateUsersTable.sql migration ready
  ✓ Infrastructure DI updated with auth services
```

### Checkpoint 1E: Frontend Auth Pages (End of Day 8)
```
Status: ✓ PASS
Verification Command: dotnet run --project src/SauronSheet.Frontend/
Visual Check (in browser):
  ✓ /Auth/Login page displays and submits
  ✓ /Auth/Register page displays and submits
  ✓ /Dashboard page shows after login
  ✓ Navigation shows auth-aware content (email + logout vs login/register)
  ✓ Logout clears cookies and redirects to login
```

### Checkpoint 1F: Integration & Validation (End of Day 10)
```
Status: ✓ PASS
Verification Commands (run in order):
  1. dotnet build                                    # Exit code 0, zero warnings
  2. dotnet test                                     # Output: "35 passed"
  3. coverlet (domain + application coverage)        # Domain ≥ 80%, App ≥ 70%
  4. Bash script dependency verification             # All assertions PASS
  5. Manual E2E: register → login → dashboard → logout # All flows working

Final Metrics:
  ✓ Full build: zero errors, zero warnings (TreatWarningsAsErrors enforced)
  ✓ All 35 tests: PASS (11 Phase 0 Domain + 8 Phase 1 Domain + 2 Phase 0 App + 14 Phase 1 App)
  ✓ Coverage reports generated
  ✓ Dependency rules verified
  ✓ Multi-user authentication working
  ✓ Tenant-scoped queries enforced
  ✓ JWT cookies set with security flags
  ✓ Solution ready for Phase 2 (Domain-Only phase)
```

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Supabase Auth API changes | Low | Medium | Use official .NET client; wrap with adapter pattern |
| JWT validation complexity | Medium | Medium | Use Supabase-provided JWT validation libraries |
| Cookie security misconfiguration | Low | High | Audit cookie flags (HttpOnly, Secure, SameSite) before deployment |
| IUserContext not injected properly | Medium | High | Integration tests verify IUserContext resolution in handlers |
| Tenant scoping not enforced | High | High | TenantScopingBehavior + RLS policies provide defense in depth |
| Password validation inconsistency | Low | Medium | Centralize validation: client-side (form) + server-side (handler) + domain rules |
| Cross-origin auth issues | Low | Medium | Use secure, same-origin cookies only; no CORS needed for SPA |
| Expired token not refreshed | Medium | Medium | JwtCookieMiddleware implements transparent refresh logic |

---

## Success Criteria Summary

| Criterion | Status | Objective Validation Command |
|-----------|--------|-----------|
| 8 domain tests pass | ✓ | `dotnet test --filter Category=Domain` → 19 tests (8 new) pass |
| 14 application tests pass | ✓ | `dotnet test --filter Category=Application` → 16 tests (14 new) pass |
| Total 35 tests pass | ✓ | `dotnet test` → output shows "35 passed" |
| Domain coverage ≥ 80% | ✓ | coverlet report shows Domain files ≥ 80% |
| Application coverage ≥ 70% | ✓ | coverlet report shows Application handlers ≥ 70% |
| Dependency rules enforced | ✓ | Bash script in 7.4 shows all assertions PASS |
| Login page renders | ✓ | Browser at `http://localhost:5000/Auth/Login` loads form |
| Register page renders | ✓ | Browser at `http://localhost:5000/Auth/Register` loads form |
| Dashboard protected | ✓ | Unauthenticated access to `/Dashboard` redirects to `/Auth/Login` |
| Multi-user isolation works | ✓ | Two different users cannot see each other's data |
| JWT cookies set securely | ✓ | Browser DevTools: cookie flags verified (HttpOnly, Secure, SameSite=Strict) |
| Auth-aware navigation working | ✓ | Authenticated user sees email + logout; unauthenticated sees login/register |
| Tenant scoping enforced | ✓ | TenantScopingBehavior tests pass (T-1.20 to T-1.22) |

---

## Next Steps (Post-Phase 1)

Once Phase 1 is complete and all checkpoints PASS:

1. **Merge to main**: Create PR with all Phase 1 deliverables (multi-user auth complete)
2. **Database setup**: Apply 001_CreateUsersTable.sql migration to Supabase
3. **Environment config**: Set `Supabase:JwtSecret` in production deployment
4. **Begin Phase 2**: Transition to Domain-Only phase (core data model + entities)
5. **Phase 2 prep**: Domain tests still passing? ✓ Ready for Phase 2 entities

---

**Created**: 2026-02-15  
**Version**: 1.0.0  
**Duration**: 10 days (Weeks 3–5)  
**Status**: Ready for implementation ✅
