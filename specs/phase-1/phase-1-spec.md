
# Phase 1: Authentication & Multi-Tenancy

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Auth)
- **Phase Type**: Full-Stack (Auth)
- **Duration**: Weeks 3–5
- **Goal**: Secure multi-user authentication with JWT, tenant-scoped data access, login/register UI
- **Depends On**: Phase 0 (Foundation — solution structure, base abstractions, MediatR, IUserContext interface)
- **Unlocks**: Phase 2 (Domain Model — `UserId` value object formalized), Phase 3+ (all tenant-scoped features)

---

## Critical Decisions

| ID     | Decision                                         | Rationale                                                               | Date       |
|--------|--------------------------------------------------|------------------------------------------------------------------------|------------|
| CD-1.1 | Supabase Auth as identity provider               | Free tier, built-in JWT, email/password ready, social login extensible  | 2026-02-15 |
| CD-1.2 | JWT stored in secure HTTP-only cookies           | Prevents XSS token theft; standard web security practice; SameSite=Strict| 2026-02-15 |
| CD-1.3 | Tenant scoping enforced in Application handlers  | Constitution: "enforced in handler, not UI" — prevents UI bypass        | 2026-02-15 |
| CD-1.4 | `UserId` strong-typed value object for all user refs | Constitution: raw string for entity IDs is a compliance violation   | 2026-02-15 |
| CD-1.5 | Auth middleware in Frontend extracts JWT claims  | Single point of authentication; all downstream layers receive `IUserContext`| 2026-02-15 |
| CD-1.6 | `IAuthService` interface defined in Domain layer | Infrastructure implements it; Application depends on abstraction only    | 2026-02-15 |
| CD-1.7 | Email/password authentication only for MVP       | Simplifies scope; social login deferred to post-MVP                      | 2026-02-15 |
| CD-1.8 | Password minimum 8 characters                    | Industry standard minimum; Supabase enforces configurable policy         | 2026-02-15 |
| CD-1.9 | Return URL support after login redirect          | Standard UX: user returns to originally requested page after authentication| 2026-02-15 |

---

## Executive Summary

### In Scope

| Area         | Deliverable                                                                 |
|--------------|----------------------------------------------------------------------------|
| Domain       | `UserId` value object (validation, equality, empty guard)                  |
| Domain       | `IAuthService` interface (contract for auth operations)                    |
| Application  | Auth commands: `RegisterUserCommand`, `LoginUserCommand`, `LogoutUserCommand`, `RefreshTokenCommand` |
| Application  | Auth query: `GetCurrentUserQuery`                                          |
| Application  | `IUserContext` implementation (resolves from HTTP context)                 |
| Application  | `TenantScopingBehavior` MediatR pipeline behavior                          |
| Application  | `UserProfileDto` data transfer object                                      |
| Application  | Auth result types: `AuthTokenResult`, `RegistrationResult`                 |
| Infrastructure | `SupabaseAuthService` (implements `IAuthService`)                        |
| Infrastructure | `JwtCookieMiddleware` (reads JWT from cookie, validates, sets ClaimsPrincipal) |
| Infrastructure | `HttpUserContext` (implements `IUserContext` from HTTP context)          |
| Infrastructure | `users` profile table migration with Row Level Security policies         |
| Frontend     | Login page (`/Login`)                                                      |
| Frontend     | Register page (`/Register`)                                                |
| Frontend     | Logout flow (navigation button + cookie clearing)                          |
| Frontend     | Dashboard stub page (`/Dashboard` — protected, shows welcome message)      |
| Frontend     | Auth-protected page routing (redirect unauthenticated → `/Login`)          |
| Frontend     | Updated `_Layout.cshtml` with auth-aware navigation                        |
| Tests        | ≥18 tests (domain + application + infrastructure validation)               |

### Deferred

| Item                              | Target Phase | Reason                                    |
|------------------------------------|--------------|-------------------------------------------|
| Social login (Google, GitHub)      | Post-MVP     | Email/password sufficient for MVP         |
| Password reset flow                | Phase 6      | Polish phase; Supabase has built-in support|
| Role-based authorization (admin)   | Post-MVP     | Single role (user) sufficient for MVP     |
| Multi-factor authentication (MFA)  | Post-MVP     | Not required for expense tracking         |
| Session management UI              | Phase 6      | Polish phase (list active sessions, logout all) |
| Email verification flow            | Phase 6      | Polish phase; Supabase supports it built-in|
| Account deletion                   | Post-MVP     | GDPR consideration; deferred              |
| User profile editing               | Phase 6      | Display name, avatar — polish             |
| Rate limiting middleware           | Phase 6      | Supabase has built-in; custom middleware deferred |
| Remember me / persistent sessions  | Phase 6      | Cookie expiration tuning                  |

---

## User Scenarios & Testing

### Scenario 1.1: New User Registration

**As a** new user
**I want to** create an account with email and password
**So that** I can track my personal expenses securely

**Acceptance Criteria:**
- Registration page accessible at `/Register`
- Form fields: Email, Password, Confirm Password
- Client-side validation: all fields required, email format, passwords match
- Server-side validation:
  - Email must be valid format
  - Password minimum 8 characters
  - Password and Confirm Password must match
  - Email must not already be registered
- On success:
  - User created in Supabase Auth (`auth.users`)
  - Profile record created in `public.users` table
  - JWT token set in HTTP-only secure cookie
  - Redirect to `/Dashboard`
- On failure:
  - Display specific error message on the form:
    - "Email is already registered" (duplicate email)
    - "Password must be at least 8 characters" (weak password)
    - "Passwords do not match" (mismatch)
    - "Please enter a valid email address" (invalid format)
  - Form retains entered email (not password)

### Scenario 1.2: Existing User Login

**As a** registered user
**I want to** log in with my email and password
**So that** I can access my expense data

**Acceptance Criteria:**
- Login page accessible at `/Login`
- Form fields: Email, Password
- Client-side validation: both fields required
- Server-side validation:
  - Credentials verified against Supabase Auth
- On success:
  - JWT stored in HTTP-only secure cookie (Secure, SameSite=Strict)
  - Refresh token stored separately (HTTP-only cookie or secure storage)
  - Redirect to `/Dashboard` (or return URL if redirected from protected page)
- On failure:
  - Display generic error: "Invalid email or password" (no information leakage)
  - Form retains entered email (not password)
- Rate limiting handled by Supabase Auth (built-in brute force protection)

### Scenario 1.3: User Logout

**As a** logged-in user
**I want to** log out
**So that** my session is terminated securely

**Acceptance Criteria:**
- Logout button visible in navigation bar when user is authenticated
- On click:
  - Supabase session revoked via `IAuthService.LogoutAsync()`
  - JWT cookie cleared (expired)
  - Refresh token cookie cleared (expired)
  - Redirect to `/Login`
- After logout:
  - Accessing any protected page redirects to `/Login`
  - Browser back button does not show cached protected pages (Cache-Control headers)

### Scenario 1.4: Tenant Isolation

**As a** user
**I want** my data to be completely isolated from other users
**So that** no one else can see my expenses

**Acceptance Criteria:**
- Every Application handler receives `IUserContext` with current `UserId`
- `TenantScopingBehavior` MediatR pipeline behavior:
  - Intercepts all commands/queries
  - Injects `UserId` from `IUserContext` into handler context
  - Throws `UnauthorizedAccessException` if `IUserContext.IsAuthenticated` is false
- All queries automatically scoped to current user's `UserId`
- Attempting to access another user's data returns "not found" (not "forbidden" — prevents enumeration)
- No UI element ever displays cross-user data
- Supabase RLS policies provide belt-and-suspenders DB-level isolation

### Scenario 1.5: Unauthenticated Access Redirect

**As an** unauthenticated visitor
**I want to** be redirected to the login page when accessing protected pages
**So that** the application is secure by default

**Acceptance Criteria:**
- **Public pages** (no auth required): `/`, `/Login`, `/Register`, `/Error`
- **Protected pages** (auth required): all other pages (`/Dashboard`, future pages)
- Unauthenticated requests to protected pages:
  - HTTP 302 redirect to `/Login?returnUrl={originalPath}`
  - Original path preserved as query parameter
- After successful login:
  - Redirect to `returnUrl` if present and valid (same-origin only)
  - Redirect to `/Dashboard` if no return URL
- Expired JWT:
  - Attempt automatic refresh via refresh token
  - If refresh succeeds: continue to requested page
  - If refresh fails: redirect to `/Login`

### Scenario 1.6: JWT Token Refresh

**As a** logged-in user
**I want** my session to automatically refresh before the JWT expires
**So that** I don't get logged out unexpectedly during active use

**Acceptance Criteria:**
- JWT access token has configurable expiration (default: 1 hour)
- Refresh token has longer expiration (default: 7 days)
- Middleware checks JWT expiration on each request:
  - If expired: attempt refresh using refresh token
  - If refresh succeeds: new JWT + refresh token set in cookies, request proceeds
  - If refresh fails: redirect to `/Login`
- Token refresh is transparent to the user (no visible interruption)

---

## Functional Requirements

### FR-1.01: Domain Layer Additions

#### UserId Value Object

```csharp
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
Rules:

Inherits from ValueObject (Phase 0 base)
Null or empty string → throws DomainException
Whitespace-only string → throws DomainException
Value-based equality (two UserId with same string are equal)
ToString() returns the raw value (for logging/debugging)
IAuthService Interface
csharp
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password);
    Task<AuthResult> LoginAsync(string email, string password);
    Task LogoutAsync(string accessToken);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<UserProfile?> GetUserProfileAsync(string userId);
}
Rules:

Defined in Domain/Services/ (contract, not implementation)
AuthResult is a Domain value object containing token info + user id
Implementation lives in Infrastructure/Auth/
Application layer depends only on this interface
AuthResult Value Object
csharp
public record AuthResult
{
    public UserId UserId { get; }
    public string AccessToken { get; }
    public string RefreshToken { get; }
    public DateTime ExpiresAt { get; }
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    // Factory methods
    public static AuthResult Success(UserId userId, string accessToken, string refreshToken, DateTime expiresAt);
    public static AuthResult Failure(string errorMessage);
}
UserProfile Value Object
csharp
public record UserProfile
{
    public UserId Id { get; }
    public string Email { get; }
    public string? DisplayName { get; }
    public DateTime CreatedAt { get; }
}
File Structure:

text
Domain/
├── ValueObjects/
│   └── UserId.cs                # Strong-typed user identifier
├── Services/
│   ├── IAuthService.cs          # Auth service contract
│   ├── AuthResult.cs            # Auth operation result
│   └── UserProfile.cs           # User profile data
└── (existing from Phase 0)
FR-1.02: Application Layer — Auth Commands & Queries
text
Application/
├── Features/
│   └── Auth/
│       ├── Commands/
│       │   ├── RegisterUserCommand.cs
│       │   ├── RegisterUserCommandHandler.cs
│       │   ├── LoginUserCommand.cs
│       │   ├── LoginUserCommandHandler.cs
│       │   ├── LogoutUserCommand.cs
│       │   ├── LogoutUserCommandHandler.cs
│       │   ├── RefreshTokenCommand.cs
│       │   └── RefreshTokenCommandHandler.cs
│       ├── Queries/
│       │   ├── GetCurrentUserQuery.cs
│       │   └── GetCurrentUserQueryHandler.cs
│       └── DTOs/
│           ├── UserProfileDto.cs
│           ├── AuthTokenDto.cs
│           └── RegistrationResultDto.cs
├── Common/
│   ├── IUserContext.cs            # Interface (from Phase 0)
│   └── Behaviors/
│       └── TenantScopingBehavior.cs
└── DependencyInjection.cs         # Updated: register behaviors
RegisterUserCommand
Property	Type	Validation
Email	string	Required, valid email format
Password	string	Required, minimum 8 characters
ConfirmPassword	string	Required, must match Password
Handler Flow:

Validate input (email format, password length, passwords match)
Call IAuthService.RegisterAsync(email, password)
If success: return RegistrationResultDto with UserId
If failure (duplicate email, weak password): throw appropriate DomainException
Returns: RegistrationResultDto

csharp
public record RegistrationResultDto(string UserId, string Email);
LoginUserCommand
Property	Type	Validation
Email	string	Required
Password	string	Required
Handler Flow:

Call IAuthService.LoginAsync(email, password)
If success: return AuthTokenDto with access token, refresh token, expiration
If failure: throw UnauthorizedAccessException (generic message)
Returns: AuthTokenDto

csharp
public record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId
);
LogoutUserCommand
Property	Type	Validation
AccessToken	string	Required (current token)
Handler Flow:

Call IAuthService.LogoutAsync(accessToken)
Return success
Returns: Unit (void/success)

RefreshTokenCommand
Property	Type	Validation
RefreshToken	string	Required
Handler Flow:

Call IAuthService.RefreshTokenAsync(refreshToken)
If success: return new AuthTokenDto
If failure: throw UnauthorizedAccessException
Returns: AuthTokenDto

GetCurrentUserQuery
Property	Type	Validation
(none)		Uses IUserContext internally
Handler Flow:

Get UserId from IUserContext
If not authenticated: throw UnauthorizedAccessException
Call IAuthService.GetUserProfileAsync(userId)
If profile found: return UserProfileDto
If not found: throw EntityNotFoundException
Returns: UserProfileDto

csharp
public record UserProfileDto(
    string UserId,
    string Email,
    string? DisplayName,
    DateTime CreatedAt
);
TenantScopingBehavior
csharp
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
        // Skip auth check for auth-related commands (Register, Login)
        if (request is IAnonymousRequest)
            return await next();

        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await next();
    }
}
Rules:

Registered as open generic MediatR pipeline behavior
Intercepts ALL commands/queries
Skips auth check for commands marked with IAnonymousRequest interface
Throws UnauthorizedAccessException for unauthenticated requests on protected endpoints
RegisterUserCommand and LoginUserCommand implement IAnonymousRequest
IAnonymousRequest Marker Interface
csharp
public interface IAnonymousRequest { }
Rules:

Marker interface; no members
Applied to commands/queries that don't require authentication
Only RegisterUserCommand and LoginUserCommand should implement this
FR-1.03: Infrastructure Layer — Auth Implementation
text
Infrastructure/
├── Auth/
│   ├── SupabaseAuthService.cs     # Implements IAuthService
│   ├── JwtCookieMiddleware.cs     # JWT extraction from cookie + validation
│   ├── HttpUserContext.cs         # Implements IUserContext from HttpContext
│   └── AuthConfiguration.cs      # Auth config options (cookie names, expiration)
├── Persistence/
│   └── Migrations/
│       └── 001_CreateUsersTable.sql  # users profile table + RLS
└── DependencyInjection.cs         # Updated: register auth services
SupabaseAuthService
Responsibilities:

Implements IAuthService interface
Calls Supabase Auth REST API endpoints:
POST /auth/v1/signup for registration
POST /auth/v1/token?grant_type=password for login
POST /auth/v1/logout for logout
POST /auth/v1/token?grant_type=refresh_token for token refresh
GET /auth/v1/user for profile retrieval
Maps Supabase responses to domain AuthResult / UserProfile objects
Handles Supabase error responses and maps to appropriate exceptions
Creates public.users profile record on successful registration
Error Mapping:

Supabase Error	Domain Mapping
Email already registered	DomainException("Email is already registered")
Invalid credentials	UnauthorizedAccessException("Invalid email or password")
Weak password	DomainException("Password must be at least 8 characters")
Invalid refresh token	UnauthorizedAccessException("Session expired")
Network/server error	InfrastructureException (logged, generic user message)
JwtCookieMiddleware
csharp
public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthConfiguration _config;

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Read JWT from cookie
        var accessToken = context.Request.Cookies[_config.AccessTokenCookieName];

        if (!string.IsNullOrEmpty(accessToken))
        {
            // 2. Validate JWT (check signature, expiration)
            // 3. If valid: set ClaimsPrincipal on HttpContext.User
            // 4. If expired: attempt refresh using refresh token cookie
            // 5. If refresh succeeds: set new cookies + ClaimsPrincipal
            // 6. If refresh fails: clear cookies (user will be redirected to login)
        }

        await _next(context);
    }
}
#### Cookie Configuration

| Cookie | Name (default) | Flags |
|--------|---|---|
| Access Token | sb-access-token | HttpOnly, Secure, SameSite=Strict, Path=/ |
| Refresh Token | sb-refresh-token | HttpOnly, Secure, SameSite=Strict, Path=/ |

**Rules:**

- Registered as middleware in Program.cs (before UseAuthorization)
- Reads JWT from named cookie (not Authorization header)
- Validates JWT signature using Supabase project's JWT secret
- Extracts sub claim as UserId
- Sets ClaimsPrincipal with claims: sub (UserId), email, exp (expiration)
- Transparent token refresh on expiration

#### HttpUserContext
HttpUserContext
csharp
public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("User is not authenticated.");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
Rules:

Registered as scoped service (per-request lifetime)
Extracts sub claim from ClaimsPrincipal set by JwtCookieMiddleware
UserId property throws if no authenticated user (fail-fast)
IsAuthenticated returns false gracefully (no exception)
AuthConfiguration
csharp
public class AuthConfiguration
{
    public string AccessTokenCookieName { get; set; } = "sb-access-token";
    public string RefreshTokenCookieName { get; set; } = "sb-refresh-token";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string JwtSecret { get; set; } = string.Empty;
}
Configuration Binding (appsettings.json):

json
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
  }
}
FR-1.04: Database Migration
sql
-- Migration: 001_CreateUsersTable.sql
-- Purpose: User profiles linked to Supabase Auth

-- Users profile table
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

-- Note: DELETE policy intentionally omitted (account deletion deferred to post-MVP)
Rules:

id is a foreign key to auth.users(id) (Supabase managed)
ON DELETE CASCADE ensures profile cleanup when auth user is deleted
RLS policies enforce that users can only read/update their own profile
Insert policy allows users to create their own profile (during registration)
No delete policy (account deletion is post-MVP)
FR-1.05: Frontend Pages
text
Frontend/
├── Pages/
│   ├── Auth/
│   │   ├── Login.cshtml           # Login form
│   │   ├── Login.cshtml.cs        # Login PageModel
│   │   ├── Register.cshtml        # Registration form
│   │   └── Register.cshtml.cs     # Register PageModel
│   ├── Dashboard.cshtml           # Protected stub dashboard
│   ├── Dashboard.cshtml.cs        # Dashboard PageModel
│   ├── Index.cshtml               # Health check (updated, public)
│   ├── Index.cshtml.cs
│   ├── Error.cshtml
│   └── Error.cshtml.cs
├── Shared/
│   └── _Layout.cshtml             # Updated: auth-aware navigation
└── Program.cs                     # Updated: auth middleware
Login Page (/Auth/Login)
PageModel:

csharp
public class LoginModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

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
            var result = await _mediator.Send(new LoginUserCommand(Input.Email, Input.Password));
            // Set JWT cookies
            SetAuthCookies(result);
            return LocalRedirect(ReturnUrl);
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }
    }
}
View Requirements:

Form: Email input (type=email), Password input (type=password)
Submit button: "Sign In"
Link to Register page: "Don't have an account? Sign up"
Error message display area
Tailwind CSS styling (centered card layout)
Hidden field for ReturnUrl
Register Page (/Auth/Register)
PageModel:

csharp
public class RegisterModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

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
            var result = await _mediator.Send(
                new RegisterUserCommand(Input.Email, Input.Password, Input.ConfirmPassword));
            // Auto-login: set JWT cookies
            var loginResult = await _mediator.Send(
                new LoginUserCommand(Input.Email, Input.Password));
            SetAuthCookies(loginResult);
            return RedirectToPage("/Dashboard");
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
View Requirements:

Form: Email input, Password input, Confirm Password input
Submit button: "Create Account"
Link to Login page: "Already have an account? Sign in"
Error message display area
Client-side validation (required fields, email format, password match)
Tailwind CSS styling (centered card layout, consistent with Login)
Dashboard Stub Page (/Dashboard)
PageModel:

csharp
[Authorize]
public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public UserProfileDto? UserProfile { get; set; }

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
View Requirements:

Heading: "Welcome, {email}" (or display name if available)
Placeholder content: "Your dashboard will appear here in Phase 4."
Shows current date
Navigation to future features (disabled/placeholder links)
Tailwind CSS styling
Updated _Layout.cshtml
Requirements:

Navigation bar shows:
Authenticated user: SauronSheet logo, Dashboard link, Logout button, user email/name
Unauthenticated user: SauronSheet logo, Login link, Register link
Auth state determined by User.Identity?.IsAuthenticated
Logout button triggers POST to logout endpoint (not GET — CSRF safe)
Responsive navigation (hamburger menu on mobile)
Updated Program.cs
csharp
var builder = WebApplication.CreateBuilder(args);

// Layer registrations
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Auth services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

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

// Auth middleware
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
FR-1.06: Infrastructure DI Updates
csharp
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Supabase client (from Phase 0)
    var supabaseUrl = configuration["Supabase:Url"]
        ?? throw new InvalidOperationException("Supabase:Url is not configured.");
    var supabaseKey = configuration["Supabase:Key"]
        ?? throw new InvalidOperationException("Supabase:Key is not configured.");

    // Auth configuration
    services.Configure<AuthConfiguration>(configuration.GetSection("Auth"));

    // Auth services
    services.AddScoped<IAuthService, SupabaseAuthService>();

    // Register Supabase client as singleton
    // ... (existing from Phase 0)

    return services;
}
FR-1.07: Application DI Updates
csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantScopingBehavior<,>));
    });

    return services;
}
Architecture Notes
Auth Request Flow
text
                          ┌──────────────────┐
User clicks "Login" ─────►│   Login.cshtml   │
                          │   (Frontend)      │
                          └────────┬─────────┘
                                   │ OnPostAsync()
                                   ▼
                          ┌──────────────────┐
                          │   IMediator.Send  │
                          │   (Application)   │
                          └────────┬─────────┘
                                   │ LoginUserCommand
                                   ▼
                          ┌──────────────────┐
                          │ TenantScoping     │  ← Skipped (IAnonymousRequest)
                          │ Behavior          │
                          └────────┬─────────┘
                                   │
                                   ▼
                          ┌──────────────────┐
                          │ LoginUserCommand  │
                          │ Handler           │
                          │ (Application)     │
                          └────────┬─────────┘
                                   │ IAuthService.LoginAsync()
                                   ▼
                          ┌──────────────────┐
                          │ SupabaseAuth      │
                          │ Service           │
                          │ (Infrastructure)  │
                          └────────┬─────────┘
                                   │ Supabase REST API
                                   ▼
                          ┌──────────────────┐
                          │ Supabase Auth     │
                          │ (External)        │
                          └────────┬─────────┘
                                   │ JWT + Refresh Token
                                   ▼
                          ┌──────────────────┐
                          │ Set HTTP-only     │
                          │ Cookies           │
                          │ (Frontend)        |
                          └────────┬─────────┘
                                   │ Redirect
                                   ▼
                          ┌──────────────────┐
                          │  /Dashboard       │
                          └──────────────────┘
Authenticated Request Flow
text
Browser request ──► JwtCookieMiddleware
                         │
                    Read cookie ──► Validate JWT
                         │              │
                    ┌────┴────┐    ┌────┴────┐
                    │ Valid   │    │ Expired │
                    │         │    │         │
                    │ Set     │    │ Refresh │──► Supabase
                    │ Claims  │    │ Token?  │
                    │ Principal│    └────┬────┘
                    └────┬────┘         │
                         │         ┌────┴────┐
                         │         │ Success │──► Set new cookies + Claims
                         │         │ Failure │──► Clear cookies
                         │         └─────────┘
                         ▼
                    UseAuthentication()
                         │
                         ▼
                    UseAuthorization()
                         │
                         ▼
                    [Authorize] PageModel
                         │
                         ▼
                    IMediator.Send(query)
                         │
                         ▼
                    TenantScopingBehavior
                         │
                    Check IUserContext.IsAuthenticated
                         │
                         ▼
                    Handler executes (scoped to UserId)
### Layer Dependencies (Phase 1 Additions)

| Layer | New Dependencies |
|-------|---|
| Domain | None (still zero external deps) — adds VOs + interface |
| Application | Domain (IAuthService, UserId, AuthResult) |
| Infrastructure | Domain (implements IAuthService), Supabase client |
| Frontend | Application (MediatR), Infrastructure (DI registration) |

## Test Specifications

### Domain.Tests — UserId & Auth Contracts

```
TEST: UserId_ValidString_SetsValue
  GIVEN a valid non-empty string "user-123"
  WHEN UserId is constructed
  THEN Value equals "user-123"
  AND ToString() returns "user-123"

TEST: UserId_NullString_ThrowsDomainException
  GIVEN a null string
  WHEN UserId is constructed
  THEN throws DomainException with message containing "cannot be null or empty"

TEST: UserId_EmptyString_ThrowsDomainException
  GIVEN an empty string ""
  WHEN UserId is constructed
  THEN throws DomainException with message containing "cannot be null or empty"

TEST: UserId_WhitespaceString_ThrowsDomainException
  GIVEN a whitespace-only string "   "
  WHEN UserId is constructed
  THEN throws DomainException with message containing "cannot be null or empty"

TEST: UserId_Equality_SameValue
  GIVEN two UserId instances with value "user-123"
  WHEN compared for equality
  THEN they are equal

TEST: UserId_Inequality_DifferentValue
  GIVEN UserId("user-123") and UserId("user-456")
  WHEN compared for equality
  THEN they are NOT equal

TEST: AuthResult_SuccessFactory_SetsProperties
  GIVEN valid parameters (UserId, tokens, expiration)
  WHEN AuthResult.Success() is called
  THEN IsSuccess is true
  AND all properties are set correctly
  AND ErrorMessage is null

TEST: AuthResult_FailureFactory_SetsError
  GIVEN an error message "Invalid credentials"
  WHEN AuthResult.Failure() is called
  THEN IsSuccess is false
  AND ErrorMessage equals "Invalid credentials"
  AND AccessToken is null/empty
```

### Application.Tests — Auth Handlers

```
TEST: RegisterUser_ValidInput_ReturnsRegistrationResult
  GIVEN valid email "test@example.com", password "securepass123", matching confirm
  AND IAuthService.RegisterAsync returns success AuthResult
  WHEN RegisterUserCommandHandler handles the command
  THEN returns RegistrationResultDto with correct UserId and Email
  AND IAuthService.RegisterAsync was called once with correct params

TEST: RegisterUser_DuplicateEmail_ThrowsDomainException
  GIVEN email "existing@example.com"
  AND IAuthService.RegisterAsync throws DomainException("Email is already registered")
  WHEN RegisterUserCommandHandler handles the command
  THEN throws DomainException with message containing "already registered"

TEST: RegisterUser_WeakPassword_ThrowsDomainException
  GIVEN password "short"
  WHEN RegisterUserCommandHandler handles the command
  THEN throws DomainException with message containing "at least 8 characters"

TEST: RegisterUser_MismatchedPasswords_ThrowsDomainException
  GIVEN password "securepass123" and confirmPassword "differentpass"
  WHEN RegisterUserCommandHandler handles the command
  THEN throws DomainException with message containing "do not match"
  AND IAuthService.RegisterAsync was NOT called

TEST: LoginUser_ValidCredentials_ReturnsAuthToken
  GIVEN valid email and password
  AND IAuthService.LoginAsync returns success AuthResult
  WHEN LoginUserCommandHandler handles the command
  THEN returns AuthTokenDto with non-empty AccessToken, RefreshToken
  AND ExpiresAt is in the future

TEST: LoginUser_InvalidCredentials_ThrowsUnauthorized
  GIVEN invalid email/password combination
  AND IAuthService.LoginAsync returns failure AuthResult
  WHEN LoginUserCommandHandler handles the command
  THEN throws UnauthorizedAccessException

TEST: LogoutUser_ValidToken_CallsAuthService
  GIVEN a valid access token
  WHEN LogoutUserCommandHandler handles the command
  THEN IAuthService.LogoutAsync was called once with the token
  AND no exception is thrown

TEST: RefreshToken_ValidRefresh_ReturnsNewTokens
  GIVEN a valid refresh token
  AND IAuthService.RefreshTokenAsync returns success AuthResult
  WHEN RefreshTokenCommandHandler handles the command
  THEN returns AuthTokenDto with new AccessToken and RefreshToken

TEST: RefreshToken_InvalidRefresh_ThrowsUnauthorized
  GIVEN an expired/invalid refresh token
  AND IAuthService.RefreshTokenAsync returns failure AuthResult
  WHEN RefreshTokenCommandHandler handles the command
  THEN throws UnauthorizedAccessException

TEST: GetCurrentUser_Authenticated_ReturnsProfile
  GIVEN IUserContext.IsAuthenticated = true, UserId = "user-123"
  AND IAuthService.GetUserProfileAsync returns UserProfile
  WHEN GetCurrentUserQueryHandler handles the query
  THEN returns UserProfileDto with correct email and UserId

TEST: GetCurrentUser_Unauthenticated_ThrowsUnauthorized
  GIVEN IUserContext.IsAuthenticated = false
  WHEN GetCurrentUserQueryHandler handles the query
  THEN throws UnauthorizedAccessException

TEST: TenantScoping_AuthenticatedRequest_ProceedsToHandler
  GIVEN IUserContext.IsAuthenticated = true
  AND a non-anonymous MediatR request
  WHEN TenantScopingBehavior processes the request
  THEN the next handler in the pipeline is invoked
  AND no exception is thrown

TEST: TenantScoping_UnauthenticatedRequest_ThrowsUnauthorized
  GIVEN IUserContext.IsAuthenticated = false
  AND a non-anonymous MediatR request
  WHEN TenantScopingBehavior processes the request
  THEN throws UnauthorizedAccessException
  AND the next handler is NOT invoked

TEST: TenantScoping_AnonymousRequest_SkipsAuthCheck
  GIVEN IUserContext.IsAuthenticated = false
  AND a request implementing IAnonymousRequest (e.g., LoginUserCommand)
  WHEN TenantScopingBehavior processes the request
  THEN the next handler in the pipeline IS invoked
  AND no exception is thrown
```

## Test Summary

| Test ID | Test Name | Category | Assert |
|---------|-----------|----------|--------|
| T-1.01 | UserId_ValidString_SetsValue | Domain | Value equals input; ToString works |
| T-1.02 | UserId_NullString_ThrowsDomainException | Domain | DomainException "cannot be null or empty" |
| T-1.03 | UserId_EmptyString_ThrowsDomainException | Domain | DomainException "cannot be null or empty" |
| T-1.04 | UserId_WhitespaceString_ThrowsDomainException | Domain | DomainException "cannot be null or empty" |
| T-1.05 | UserId_Equality_SameValue | Domain | Equal + same hash code |
| T-1.06 | UserId_Inequality_DifferentValue | Domain | Not equal |
| T-1.07 | AuthResult_SuccessFactory_SetsProperties | Domain | IsSuccess=true, properties set, ErrorMessage null |
| T-1.08 | AuthResult_FailureFactory_SetsError | Domain | IsSuccess=false, ErrorMessage set |
| T-1.09 | RegisterUser_ValidInput_ReturnsResult | Application | RegistrationResultDto with UserId + Email |
| T-1.10 | RegisterUser_DuplicateEmail_ThrowsDomainException | Application | DomainException "already registered" |
| T-1.11 | RegisterUser_WeakPassword_ThrowsDomainException | Application | DomainException "at least 8 characters" |
| T-1.12 | RegisterUser_MismatchedPasswords_ThrowsDomainException | Application | DomainException "do not match"; auth NOT called |
| T-1.13 | LoginUser_ValidCredentials_ReturnsAuthToken | Application | AuthTokenDto with tokens + future expiration |
| T-1.14 | LoginUser_InvalidCredentials_ThrowsUnauthorized | Application | UnauthorizedAccessException |
| T-1.15 | LogoutUser_ValidToken_CallsAuthService | Application | LogoutAsync called once; no exception |
| T-1.16 | RefreshToken_ValidRefresh_ReturnsNewTokens | Application | New AuthTokenDto |
| T-1.17 | RefreshToken_InvalidRefresh_ThrowsUnauthorized | Application | UnauthorizedAccessException |
| T-1.18 | GetCurrentUser_Authenticated_ReturnsProfile | Application | UserProfileDto with correct data |
| T-1.19 | GetCurrentUser_Unauthenticated_ThrowsUnauthorized | Application | UnauthorizedAccessException |
| T-1.20 | TenantScoping_Authenticated_Proceeds | Application | Next handler invoked; no exception |
| T-1.21 | TenantScoping_Unauthenticated_ThrowsUnauthorized | Application | UnauthorizedAccessException; next NOT invoked |
| T-1.22 | TenantScoping_AnonymousRequest_SkipsCheck | Application | Next handler invoked despite unauthenticated |

**Total:** 22 tests (8 Domain + 14 Application)

## Deliverables

| # | Deliverable | Layer | Acceptance |
|---|---|---|---|
| D-1.01 | UserId value object | Domain | Tests T-1.01 to T-1.06 pass |
| D-1.02 | IAuthService interface | Domain | Compiles; contract for auth operations |
| D-1.03 | AuthResult value object | Domain | Tests T-1.07 and T-1.08 pass |
| D-1.04 | UserProfile value object | Domain | Compiles; used by IAuthService |
| D-1.05 | RegisterUserCommand + handler | Application | Tests T-1.09 to T-1.12 pass |
| D-1.06 | LoginUserCommand + handler | Application | Tests T-1.13 and T-1.14 pass |
| D-1.07 | LogoutUserCommand + handler | Application | Test T-1.15 passes |
| D-1.08 | RefreshTokenCommand + handler | Application | Tests T-1.16 and T-1.17 pass |
| D-1.09 | GetCurrentUserQuery + handler | Application | Tests T-1.18 and T-1.19 pass |
| D-1.10 | TenantScopingBehavior | Application | Tests T-1.20 to T-1.22 pass |
| D-1.11 | IAnonymousRequest marker interface | Application | Used by Register + Login commands |
| D-1.12 | DTOs: UserProfileDto, AuthTokenDto, RegistrationResultDto | Application | Compile; used by handlers and frontend |
| D-1.13 | SupabaseAuthService | Infrastructure | Implements IAuthService; calls Supabase Auth API |
| D-1.14 | JwtCookieMiddleware | Infrastructure | JWT from cookie → ClaimsPrincipal; auto-refresh |
| D-1.15 | HttpUserContext | Infrastructure | Implements IUserContext; extracts sub claim |
| D-1.16 | AuthConfiguration | Infrastructure | Cookie names, expiration, JWT secret config |
| D-1.17 | 001_CreateUsersTable.sql migration | Infrastructure | Users table + RLS policies applied to Supabase |
| D-1.18 | Login page (/Auth/Login) | Frontend | Form, validation, error display, redirect |
| D-1.19 | Register page (/Auth/Register) | Frontend | Form, validation, error display, auto-login |
| D-1.20 | Dashboard stub (/Dashboard) | Frontend | Protected; shows welcome message |
| D-1.21 | Updated _Layout.cshtml | Frontend | Auth-aware navigation (login/register vs logout) |
| D-1.22 | Updated Program.cs | Frontend | Auth middleware, HttpContextAccessor, IUserContext DI |
| D-1.23 | Updated appsettings.json | Frontend | Auth configuration section added |
| D-1.24 | Domain.Tests auth tests (8 tests) | Tests | dotnet test --filter Category=Domain all green |
| D-1.25 | Application.Tests auth tests (14 tests) | Tests | dotnet test --filter Category=Application all green |

## Success Criteria

| # | Criterion | Metric |
|---|---|---|
| SC-1.1 | New user can register with email/password | E2E: /Register → form → submit → /Dashboard |
| SC-1.2 | Registered user can log in | E2E: /Login → form → submit → /Dashboard |
| SC-1.3 | JWT stored in HTTP-only secure cookie | Browser DevTools: cookie flags verified |
| SC-1.4 | Logged-in user can log out | Logout → cookies cleared → /Login redirect |
| SC-1.5 | Protected pages redirect unauthenticated users | /Dashboard → 302 → /Login?returnUrl=/Dashboard |
| SC-1.6 | Return URL works after login | Redirect to /Login?returnUrl=/Dashboard → login → /Dashboard |
| SC-1.7 | Tenant isolation enforced at handler level | TenantScopingBehavior tests pass |
| SC-1.8 | Anonymous requests bypass tenant scoping | Login/Register work without authentication |
| SC-1.9 | Navigation shows auth-appropriate links | Authenticated: logout + email; Guest: login + register |
| SC-1.10 | All Phase 1 tests pass | dotnet test ≥22 new tests green |
| SC-1.11 | Domain test coverage ≥ 80% | coverlet report (cumulative) |
| SC-1.12 | Application test coverage ≥ 70% | coverlet report |
| SC-1.13 | Supabase users table created with RLS | Supabase dashboard: table + policies visible |
| SC-1.14 | Registration creates profile in public.users | Query Supabase after registration: row exists |
| SC-1.15 | Invalid credentials show generic error (no info leakage) | Login with wrong password → "Invalid email or password" |

## Assumptions

- Supabase Auth email/password provider is enabled in the Supabase dashboard (default configuration).
- Supabase JWT secret is available from the Supabase project settings (Settings → API → JWT Secret).
- No email verification required in Phase 1. Users can log in immediately after registration. Email verification deferred to Phase 6.
- Single role: All users have the same permissions. Role-based authorization (admin vs. user) is deferred to post-MVP.
- No CAPTCHA on registration form. Supabase's built-in rate limiting provides brute force protection.
- Session is per-device: Each browser/device has its own JWT cookie. There is no server-side session store.
- Token refresh is handled transparently by the middleware. The frontend does not need to know about token lifecycle.
- Supabase client library handles HTTP communication with Supabase Auth REST API. If the library has limitations, raw HTTP calls via HttpClient are acceptable.
- HTTPS is required in production. Development may use HTTP with app.UseDeveloperExceptionPage().
- Profile creation happens synchronously during registration (not via Supabase triggers/webhooks).

## Risks & Mitigations

| ID | Risk | Impact | Probability | Mitigation |
|---|---|---|---|---|
| R-1.1 | Supabase Auth rate limits during development | Low | Medium | Use separate Supabase project for dev; configure rate limits |
| R-1.2 | JWT cookie not sent on cross-origin requests | Medium | Low | SameSite=Strict is correct for same-origin; CORS not needed for pages |
| R-1.3 | Supabase C# client auth methods incomplete | Medium | Medium | Fall back to raw HTTP calls via HttpClient to Supabase REST API |
| R-1.4 | Token refresh race condition (concurrent requests) | Low | Low | Middleware handles refresh atomically; subsequent requests wait |
| R-1.5 | ClaimsPrincipal not propagated to IUserContext | High | Low | Integration test verifies full pipeline (T-1.20 to T-1.22) |
| R-1.6 | RLS policies misconfigured | High | Medium | Test RLS with two different users; verify isolation at DB level |

## Implementation Notes

### Recommended Implementation Order

```
Step 1: Write Domain.Tests for UserId + AuthResult (RED phase)
        └── Tests T-1.01 to T-1.08
        └── Verify: tests FAIL (red)

Step 2: Implement Domain additions (GREEN phase)
        └── UserId, IAuthService, AuthResult, UserProfile
        └── Verify: `dotnet test --filter Category=Domain` all GREEN

Step 3: Write Application.Tests for handlers + behaviors (RED phase)
        └── Tests T-1.09 to T-1.22
        └── Verify: tests FAIL (red)

Step 4: Implement Application handlers + TenantScopingBehavior (GREEN phase)
        └── All commands, queries, handlers, DTOs, behaviors
        └── Update DependencyInjection.cs
        └── Verify: `dotnet test --filter Category=Application` all GREEN

Step 5: Implement Infrastructure auth services
        └── SupabaseAuthService, JwtCookieMiddleware, HttpUserContext
        └── AuthConfiguration
        └── Update DependencyInjection.cs

Step 6: Apply database migration
        └── Run 001_CreateUsersTable.sql on Supabase
        └── Verify: table + RLS policies in Supabase dashboard

Step 7: Implement Frontend pages
        └── Login, Register, Dashboard stub
        └── Update _Layout.cshtml, Program.cs, appsettings.json

Step 8: End-to-end validation
        └── Register new user → login → see dashboard → logout → redirect to login
        └── Test return URL flow
        └── Test invalid credentials
        └── Verify cookies in browser DevTools (HttpOnly, Secure, SameSite)
        └── Verify RLS: create two users, confirm data isolation

Step 9: Final test + coverage validation
        └── `dotnet test` → all tests green (Phase 0 + Phase 1)
        └── Domain coverage ≥ 80%
        └── Application coverage ≥ 70%
Spec-Driven Workflow Compliance
Step	Workflow Stage	Phase 1 Action
1	Write Test Spec	✅ Tests written first (Steps 1 and 3)
2	Define Handler Stub	✅ MediatR commands/queries defined with contracts (Step 4)
3	Build Domain	✅ UserId VO, IAuthService, AuthResult (Step 2)
4	Implement Persistence	✅ SupabaseAuthService, users table migration (Steps 5 and 6)
5	Wire UI	✅ Login, Register, Dashboard pages (Step 7)
6	End-to-end Test	✅ Manual testing of all scenarios
7	Deployment Preparation	✅ Supabase settings configured, code ready for deployment
Security Checklist
 JWT cookie: HttpOnly = true
 JWT cookie: Secure = true
 JWT cookie: SameSite = Strict
 No JWT in URL parameters or localStorage
 Login error: generic "Invalid email or password" (no user enumeration)
 Registration: password ≥ 8 characters enforced server-side
 CSRF protection: logout is POST, not GET
 Return URL validated as local redirect (no open redirect vulnerability)
 RLS policies active on public.users table
 No Supabase service key exposed in frontend (only anon key)
Phase Spec Version: 1.0.0 | Created: 2026-02-15 | Aligned with Constitution v1.1.0