# Phase 1: Authentication & Multi-Tenancy

**Feature Branch**: `001-authentication-multitenant`  
**Version**: 1.0.0  
**Status**: Ready for speckit.tasks generation  
**Duration**: 3-4 weeks  
**Depends On**: Phase 0 Complete

---

## Executive Summary

**Objective**: Implement user authentication via Supabase Auth + real multi-tenancy enforcement

**What We Build**:
- User entity + database migrations
- Supabase authentication integration
- Real IUserContext from JWT tokens
- User registration + login flows
- Tenant isolation validation in queries
- Session management + logout

**Success Criteria**:
- ✅ User can register via email/password
- ✅ User can login + receive JWT token
- ✅ JWT token validated on each request
- ✅ Queries filtered by current user's tenant
- ✅ Cross-tenant access blocked + logged
- ✅ 8/8 tests passing
- ✅ Code coverage ≥80% (Domain & Application layers)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - User Registration (Priority: P1)

A new user wants to create an account using email and password so they can access the expense tracking system securely.

**Why this priority**: Authentication is the foundation for multi-tenancy. No other features can proceed without user registration.

**Independent Test**: Can be fully tested by registration flow alone - system accepts email/password, creates user record, returns success response.

**Acceptance Scenarios**:

1. **Given** user is on Register page, **When** user enters valid email and password, **Then** user is created and redirected to login page with success message
2. **Given** user is on Register page, **When** user enters email that already exists, **Then** system shows "Email already registered" error
3. **Given** user is on Register page, **When** user enters weak password (<8 chars), **Then** system shows "Password must be at least 8 characters" error
4. **Given** user is on Register page, **When** user enters invalid email format, **Then** system shows "Invalid email format" error

---

### User Story 2 - User Login (Priority: P1)

A registered user wants to log in with their email and password to access their expense data.

**Why this priority**: Login is required to authenticate and establish session. Core to multi-tenancy enforcement.

**Independent Test**: Can be fully tested by login flow - system validates credentials, issues JWT token, user can access authenticated pages.

**Acceptance Scenarios**:

1. **Given** registered user is on Login page, **When** user enters correct email and password, **Then** user receives JWT token and is redirected to Dashboard
2. **Given** registered user is on Login page, **When** user enters incorrect password, **Then** system shows "Invalid credentials" error without revealing if email exists
3. **Given** registered user is on Login page, **When** user enters unregistered email, **Then** system shows "Invalid credentials" error
4. **Given** user is on authenticated page, **When** user's JWT token expires, **Then** user is redirected to login page

---

### User Story 3 - Tenant Isolation (Priority: P1)

The system must ensure that users only see data belonging to their tenant (userId) and cannot access other users' data.

**Why this priority**: Multi-tenancy is non-negotiable per Constitution. Data isolation prevents security breaches.

**Independent Test**: Can be tested by attempting cross-tenant queries - system must reject unauthorized access and log attempt.

**Acceptance Scenarios**:

1. **Given** User A is logged in, **When** User A attempts to query User B's categories, **Then** system returns UnauthorizedAccessException and logs security event
2. **Given** User A is logged in, **When** User A requests categories, **Then** only User A's categories are returned (TenantId filtered)
3. **Given** User A has categories, **When** User B logs in, **Then** User B's category list is empty (User A's data invisible)

---

### User Story 4 - User Profile View (Priority: P2)

A logged-in user wants to view their profile information (email, account created date) on a Profile page.

**Why this priority**: Enhances UX by showing user context. Not blocking other features.

**Independent Test**: Can be tested independently - fetch current user from context, display profile page.

**Acceptance Scenarios**:

1. **Given** user is logged in, **When** user navigates to /profile, **Then** user sees their email and account creation date
2. **Given** user is not logged in, **When** user navigates to /profile, **Then** user is redirected to login page

---

### User Story 5 - User Logout (Priority: P2)

A logged-in user wants to log out and end their session so others cannot access their account.

**Why this priority**: Standard security best practice. Enables device sharing scenarios.

**Independent Test**: Can be tested independently - clear session/token, redirect to login.

**Acceptance Scenarios**:

1. **Given** user is logged in, **When** user clicks "Logout", **Then** session is cleared and user is redirected to login page
2. **Given** user is logged out, **When** user attempts to access protected page, **Then** user is redirected to login

---

### Edge Cases

- What happens when user registers with email containing non-ASCII characters (é, ñ, etc.)? → Should be rejected or normalized
- What happens when JWT token is tampered with? → Should be rejected with 403 Forbidden
- What happens when Supabase is temporarily unavailable during login? → Should show "Service unavailable" error with retry option
- What happens if user tries to register twice with same email in quick succession? → Second request should be idempotent or show duplicate error
- What happens when user is deleted from Supabase but still has valid JWT? → Next query should fail and redirect to login

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to register with unique email and password (minimum 8 characters)
- **FR-002**: System MUST hash passwords using bcrypt (or Supabase Auth default) before storing
- **FR-003**: System MUST issue JWT token on successful login containing UserId claim (tenant identifier)
- **FR-004**: System MUST validate JWT token on every authenticated request using Supabase public key
- **FR-005**: System MUST enforce tenant isolation: all queries must filter by current user's UserId
- **FR-006**: System MUST return 401 Unauthorized if request lacks valid JWT token
- **FR-007**: System MUST return 403 Forbidden if user attempts cross-tenant access (with security logging)
- **FR-008**: System MUST allow users to log out, clearing session/cookie and invalidating token
- **FR-009**: System MUST prevent duplicate registrations (email must be unique)
- **FR-010**: System MUST validate email format during registration (RFC 5322 compliant)
- **FR-011**: System MUST extract UserId from JWT claim and inject into IUserContext for each request
- **FR-012**: System MUST maintain audit log of failed login attempts (for future monitoring/alerts)

### Key Entities

- **User**: Represents authenticated user with Id (UUID), Email (unique), CreatedAt, UpdatedAt
  - Invariant: Email must be non-empty and valid
  - Invariant: Email must be unique across all users
  - Invariant: Id cannot be changed after creation

- **JWT Token**: Contains claims: UserId (sub), exp (expiry), iat (issued at), email (context)
  - Lifetime: 24 hours (configurable)
  - Signing: HS256 with Supabase JWT secret

- **IUserContext**: Provides current authenticated user's UserId (tenant identifier)
  - Implementation: SupabaseUserContext extracts from JWT claim
  - Lifecycle: Scoped per HTTP request

---

## Architecture Changes from Phase 0

**New Components**:
- User entity (Id, Email, CreatedAt, UpdatedAt, Password hash stored in Supabase)
- Authentication middleware (JWT validation)
- Real SupabaseUserContext (replaces MockUserContext in production)
- Register + Login page models (Razor Pages)
- Supabase Auth API integration (OAuth via Supabase)

**Dependency Graph**:
```
Frontend (Register.cshtml, Login.cshtml, Profile.cshtml)
  ↓ uses
Application (RegisterUserCommand, LoginUserCommand, SupabaseUserContext)
  ↓ uses
Domain (User entity, UserCreatedDomainEvent)
  ↓ uses
Infrastructure (UserRepository, Supabase Auth Client, JWT validation)
```

**Database Schema**:
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMP DEFAULT NOW() NOT NULL,
    -- Password handled by Supabase Auth, not stored in this table
    CONSTRAINT email_not_empty CHECK (email <> '')
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_created_at ON users(created_at DESC);
```

---

## API Contracts & Integration

### Registration Endpoint

**Command**: `RegisterUserCommand`

```csharp
public record RegisterUserCommand(
    string Email, 
    string Password
) : IRequest<RegisterUserResponse>;

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string Message
);
```

**Flow**:
1. Frontend POST /register with { email, password }
2. Application validates email format + password strength
3. Infrastructure calls Supabase Auth API to create user
4. Domain publishes UserCreatedDomainEvent
5. Response: 201 Created with UserId + success message

**Status Codes**:
- 201 Created: User successfully registered
- 400 Bad Request: Invalid email/password format
- 409 Conflict: Email already exists

---

### Login Endpoint

**Command**: `LoginUserCommand`

```csharp
public record LoginUserCommand(
    string Email,
    string Password
) : IRequest<LoginUserResponse>;

public record LoginUserResponse(
    string JwtToken,
    Guid UserId,
    string Email
);
```

**Flow**:
1. Frontend POST /login with { email, password }
2. Infrastructure calls Supabase Auth API (verify credentials)
3. JwtTokenService generates JWT token if credentials valid
4. Response: 200 OK with token + user info

**Status Codes**:
- 200 OK: Login successful
- 401 Unauthorized: Invalid credentials
- 503 Service Unavailable: Supabase down

---

### Protected Queries

All queries decorated with `[Authorize]` or check:

```csharp
public record GetCategoriesQuery(Guid UserId) 
    : IRequest<List<CategoryDto>>;
```

**Enforcement**:
- MediatR ScopedQueryBehavior validates: response.TenantId == UserContext.UserId
- Throws UnauthorizedAccessException if mismatch
- Middleware logs all unauthorized attempts

---

## Error Handling & Validations

### Registration Errors

| Error | Status | Message | Logging |
|-------|--------|---------|---------|
| Invalid Email Format | 400 | "Email must be valid (example@domain.com)" | Info |
| Password Too Short | 400 | "Password must be at least 8 characters" | Info |
| Email Already Exists | 409 | "Email already registered. Try login." | Info |
| Weak Password | 400 | "Password must include upper, lower, digit, special char" | Info |

### Login Errors

| Error | Status | Message | Logging |
|-------|--------|---------|---------|
| Invalid Credentials | 401 | "Invalid email or password" | Security (no details leaked) |
| User Not Found | 401 | "Invalid email or password" | Info |
| Account Disabled | 403 | "Account is disabled" | Security |
| Rate Limit Exceeded | 429 | "Too many login attempts. Try again in 15 min." | Security |

### Multi-Tenancy Violations

| Error | Status | Message | Logging |
|-------|--------|---------|---------|
| Cross-Tenant Query | 403 | "Unauthorized access" | **CRITICAL SECURITY** |
| Expired Token | 401 | "Session expired. Please login again." | Info |
| Invalid Token | 403 | "Invalid token. Please login again." | Security |
| Missing Token | 401 | "Authentication required" | Info |

---

## Security Considerations

### Password Security
- Passwords hashed via Supabase Auth (bcrypt with salt)
- Password minimum 8 characters
- Complexity optional but recommended (upper + lower + digit + special)
- No plaintext passwords in logs or responses
- Supabase Auth handles password reset (out of scope Phase 1)

### JWT Token Security
- **Algorithm**: HS256 (HMAC SHA256)
- **Lifetime**: 24 hours (configurable in .env)
- **Claims**: `sub` (UserId), `exp` (expiry), `iat` (issued at), `email`
- **Signing Key**: Supabase JWT Secret (environment variable)
- **Validation**: Public key verification on each request
- **Storage**: HttpOnly cookie (not localStorage) - prevents XSS theft

### CORS & Origin Policy
- Only allow requests from whitelisted origins
- No credentials in cross-origin requests without explicit CORS header
- Set SameSite=Strict on session cookies

### Audit Logging
- Log all login attempts (success + failure)
- Log all failed authentication attempts (with rate limiting check)
- Log ALL cross-tenant access attempts with user/resource context
- Log token generation + expiry events
- Audit table structure: event_type, user_id, timestamp, details, severity

### Rate Limiting
- 5 failed login attempts per email → 15 minute lockout
- 10 register attempts per IP → 1 hour lockout
- Implement via middleware or Supabase rate limit policies

### HTTPS & TLS
- All auth endpoints must use HTTPS (enforced in production)
- No redirect from HTTPS to HTTP
- HSTS headers enabled

---

## Dependencies & Configuration

### NuGet Packages Required

```xml
<!-- JWT Token Handling -->
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />

<!-- Supabase Integration -->
<PackageReference Include="Supabase.Postgrest" Version="0.8.0" />
<PackageReference Include="Supabase.Core" Version="0.8.0" />

<!-- Validation -->
<PackageReference Include="FluentValidation" Version="12.1.1" /> <!-- Already added -->

<!-- Existing -->
<PackageReference Include="MediatR" Version="12.2.0" /> <!-- Already added -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
```

### Environment Variables (.env)

```
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_KEY=your-anon-key
SUPABASE_JWT_SECRET=your-jwt-secret
JWT_EXPIRY_HOURS=24
JWT_ALGORITHM=HS256
ALLOWED_ORIGINS=https://localhost:7001,https://example.com
```

### Startup Configuration (Program.cs)

```csharp
// In Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new() {
            ValidateIssuer = false, // Supabase handles
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["JWT_SECRET"])
            )
        };
    });

builder.Services.AddAuthorization();
app.UseAuthentication();
app.UseAuthorization();
```

---

## Data Flow Diagrams

### Registration Flow

```
User Registration Page
    ↓ POST { email, password }
Frontend validates (client-side)
    ↓
Application: RegisterUserCommand
    ↓
ValidationBehavior (email format, password strength)
    ↓
RegisterUserCommandHandler
    ↓
Infrastructure: SupabaseAuthClient.CreateUser(email, password)
    ↓ Supabase hashes + stores
Domain: User entity created
    ↓
DomainEvent: UserCreatedDomainEvent published
    ↓
Response: 201 { UserId, Email, "Welcome message" }
    ↓
Frontend: Redirect to Login page
```

### Login Flow

```
User Login Page
    ↓ POST { email, password }
Frontend validates (client-side)
    ↓
Application: LoginUserCommand
    ↓
ValidationBehavior (email format, password not blank)
    ↓
LoginUserCommandHandler
    ↓
Infrastructure: SupabaseAuthClient.VerifyCredentials(email, password)
    ↓ Returns auth response or fails
JwtTokenService.GenerateToken(userId, email)
    ↓
Response: 200 { JwtToken, UserId, Email }
    ↓
Frontend: Store token in HttpOnly cookie
    ↓
Frontend: Redirect to Dashboard
    ↓
ScopedQueryBehavior: Validate token on each query
```

### Tenant Isolation Flow

```
User A queries GetCategoriesQuery(userId=B)
    ↓
ScopedQueryBehavior extracts UserContext.UserId (A)
    ↓
Query handler executes, returns UserB's categories
    ↓
ScopedQueryBehavior checks: response.TenantId != UserContext.UserId
    ↓ Mismatch detected!
Throw UnauthorizedAccessException
    ↓
Logger.LogSecurity("Cross-tenant access attempt: UserA→UserB.Categories")
    ↓
Middleware catches → 403 Forbidden response
```

## Deliverables (20+ items)

### Domain Layer (2)
- [ ] User entity (Id: UUID, Email, CreatedAt, UpdatedAt) with invariants
- [ ] UserCreatedDomainEvent (for future event publishing)

### Application Layer (8)
- [ ] SupabaseUserContext (implements IUserContext, extracts UserId from JWT)
- [ ] RegisterUserCommand + RegisterUserCommandHandler
- [ ] LoginUserCommand + LoginUserCommandHandler (returns JWT)
- [ ] UserDto (Email, CreatedAt read-only)
- [ ] JwtTokenService (generate token with claims)
- [ ] AuthenticationBehavior (MediatR pipeline validation)
- [ ] DependencyInjection.cs (register auth services)
- [ ] Validators: RegisterUserValidator, LoginUserValidator (FluentValidation)

### Infrastructure Layer (5)
- [ ] UserRepository (implements IRepository<User>)
- [ ] SupabaseAuthClient (Supabase Auth API wrapper)
- [ ] Migration: 001_CreateUsersTable.sql (with indexes)
- [ ] DependencyInjection.cs (register Supabase services)
- [ ] JwtTokenProvider (generate/validate JWT tokens)

### Frontend Layer (3)
- [ ] Register.cshtml + Register.cshtml.cs (PageModel)
- [ ] Login.cshtml + Login.cshtml.cs (PageModel)
- [ ] Profile.cshtml + Profile.cshtml.cs (display current user)

### Middleware & Configuration (2)
- [ ] JwtAuthenticationMiddleware (validate token on each request)
- [ ] SessionManagement (HttpOnly cookie setup)

---

## Compliance with Constitution

This specification adheres to **SauronSheet Constitution v1.0.0**:

### ✅ Clean Architecture & Layered Dependencies
- Domain layer contains ZERO external dependencies (User entity is pure)
- Application layer uses only Domain + MediatR (no Supabase calls)
- Infrastructure layer implements IUserContext + IRepository<User>
- Frontend depends only on Application (MediatR)
- No circular dependencies

### ✅ CQRS + MediatR Pattern
- RegisterUserCommand (state-changing) → handler → domain event
- LoginUserCommand (state-changing) → handler → returns JWT
- AuthenticationBehavior validates token on all requests (pipeline)
- Queries enforce tenant scoping via ScopedQueryBehavior

### ✅ Domain-Driven Design
- User entity encapsulates invariants (email unique, not empty)
- UserCreatedDomainEvent published on registration
- Repository pattern abstracts persistence
- No SQL or Supabase logic in Domain

### ✅ Test-First Development
- 8 tests specified (T01-001 to T01-008)
- Unit tests for User entity + validators
- Integration tests for commands with mocked SupabaseAuthClient
- 80%+ coverage target for Domain + Application

### ✅ Spec-Driven Development
- Tests written before implementation (red-green-refactor)
- Feature spec drives implementation order
- Clear acceptance criteria in user stories

---

**Domain Layer** (2):
- [ ] User entity (Id, Email, CreatedAt, UpdatedAt)
- [ ] UserCreatedDomainEvent

**Application Layer** (8):
- [ ] SupabaseUserContext (replaces MockUserContext)
- [ ] RegisterUserCommand + handler
- [ ] LoginUserCommand + handler (returns JWT)
- [ ] UserDto
- [ ] JwtTokenService (generate/validate tokens)
- [ ] AuthenticationBehavior (MediatR middleware)
- [ ] DependencyInjection.cs (update with new services)
- [ ] RegisteredUserEvent (domain event)

**Infrastructure Layer** (5):
- [ ] User repository implementation
- [ ] Supabase Auth client integration
- [ ] Migration: 001_CreateUsersTable.sql
- [ ] DependencyInjection.cs (Supabase registration)
- [ ] TokenProvider interface + Supabase implementation

**Frontend Layer** (3):
- [ ] Register.cshtml + Register.cshtml.cs
- [ ] Login.cshtml + Login.cshtml.cs
- [ ] Profile.cshtml + Profile.cshtml.cs

**Authentication** (2):
- [ ] Cookie/Session middleware
- [ ] JWT validation middleware

---

## Test Specifications (8 tests)

**T01-001**: User entity compiles + has Email property  
**T01-002**: RegisterUserCommand creates user + publishes event  
**T01-003**: LoginUserCommand validates credentials + returns JWT  
**T01-004**: SupabaseUserContext extracts UserId from JWT  
**T01-005**: AuthenticationBehavior blocks unauthenticated requests  
**T01-006**: User repository CRUD operations work  
**T01-007**: Supabase migration creates users table  
**T01-008**: Multi-tenant query filtering respects UserId + TenantId  

---

## Task Breakdown (Phases I-III, ~40 tasks)

**Phase I: User Entity & Database** (10 tasks)
- Create User entity
- Create UserCreatedDomainEvent
- Create Supabase migration
- Verify schema in Supabase
- Update repository interface for User
- Tests T01-001, T01-002

**Phase II: Authentication Services** (12 tasks)
- Create JwtTokenService
- Create SupabaseUserContext
- Create TokenProvider interface + implementation
- Create AuthenticationBehavior
- Update DependencyInjection.cs
- Create RegisterUserCommand + handler
- Create LoginUserCommand + handler
- Tests T01-003 through T01-005

**Phase III: Frontend & Integration** (8 tasks)
- Create Register.cshtml + PageModel
- Create Login.cshtml + PageModel
- Create Profile.cshtml (display current user)
- Wire authentication middleware
- Add cookie/session management
- Tests T01-006 through T01-008
- Update Layout.cshtml (show login/logout links)
- Verify end-to-end authentication flow

---

## Success Criteria

✅ User can register via email + password  
✅ User receives JWT token on login  
✅ All queries filtered by current user  
✅ Cross-tenant requests rejected  
✅ All 8 tests passing  
✅ Code coverage ≥80%  

---

## Next Phase

Phase 2: Core Data Model & Domain Entities (2-3 weeks)
- Create Category entity
- Create Transaction entity
- Create Budget entity
- Implement domain validation rules
