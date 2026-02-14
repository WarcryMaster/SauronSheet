# Phase 1: Authentication & Multi-Tenancy - PLAN

**Status:** Ready for Implementation  
**Duration:** 3-4 weeks  
**Dependencies:** Phase 0 complete  
**Feature Branch:** `001-authentication-multitenant`  
**Version:** 1.0.2 (Aligned with phase-1-auth-spec.md)  

---

## Executive Summary

**Objective**: Implement user authentication via Supabase Auth + real multi-tenancy enforcement

**What We Build**:
- User entity + database migrations
- Supabase Auth native integration (passwords + JWT handled by Supabase)
- Real IUserContext from JWT tokens
- User registration + login flows
- Tenant isolation validation in queries
- Session management + logout
- Security audit logging

**Success Criteria**:
- ✅ User can register via email/password
- ✅ User can login + receive JWT token (from Supabase)
- ✅ JWT token validated on each request
- ✅ Queries filtered by current user's tenant
- ✅ Cross-tenant access blocked + logged
- ✅ 8/8 tests passing
- ✅ Code coverage ≥80% (Domain & Application layers)

---

## ✅ Critical Decisions RESOLVED

| Decision | Answer | Rationale |
|----------|--------|-----------|
| **Auth Strategy** | Use **Supabase Auth Native API** | Reduces custom security code, bcrypt + password reset handled by Supabase |
| **JWT Generation** | **Supabase issues JWT natively** | Proven approach, no custom token generation needed, secure key management |
| **Password Storage** | **Supabase Auth table only** (NOT in users table) | Separation of concerns, Supabase handles crypto |
| **Password Hashing** | **Supabase bcrypt** | Delegated to IdP, no local bcrypt |
| **Token Lifetime** | **3600 seconds (1 hour)** | Configurable in Supabase, shorter for security |
| **Multi-user/Roles** | **Phase 1: Single-user only** | Phase 5 will add sharing/multi-user |
| **Token Refresh** | **Phase 2+** | Phase 1: No refresh, logout after expiry |
| **Email Verification** | **Out of scope Phase 1** | Phase 3+ if needed |

---

## Objectives
- Integrate Supabase Auth for user registration, login, logout
- Secure Razor Pages (require authentication)
- Extract userId from session/JWT for MediatR commands/queries
- Enforce tenant boundaries in all queries/commands
- Provide basic UI for login/logout
- Implement comprehensive security & audit logging
- Test authentication flows and tenant enforcement

---

## Constitution Compliance
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

**Gates:** All principles compliant. No violations detected.

---

## Key Entities

### User Entity (Domain)
- **Properties:**
  - Id (UUID Primary Key)
  - Email (VARCHAR 255, UNIQUE, NOT NULL)
  - CreatedAt (TIMESTAMP DEFAULT NOW())
  - UpdatedAt (TIMESTAMP DEFAULT NOW())
- **Invariants:**
  - Email must be non-empty and valid (RFC 5322)
  - Email must be unique across all users
  - Id cannot be changed after creation
- **Note:** Password stored in Supabase Auth service, NOT in users table

### JWT Token (Issued by Supabase)
- **Claims:**
  - `sub` (UserId - subject)
  - `exp` (expiry timestamp)
  - `iat` (issued at timestamp)
  - `email` (user email)
  - `aud` (audience)
- **Algorithm:** HS256 (HMAC SHA256)
- **Lifetime:** 3600 seconds (1 hour - configurable in Supabase)
- **Signing:** Supabase JWT Secret (environment variable)
- **Storage:** HttpOnly cookie (prevents XSS theft)

### IUserContext Implementation
- **Interface:** IUserContext (extracts current user's UserId)
- **Implementation:** SupabaseUserContext
- **Lifecycle:** Scoped per HTTP request
- **Source:** JWT claims from HttpOnly cookie

---

## Architecture Changes from Phase 0

**New Components**:
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

## Functional Requirements

### Core Authentication (FR-001 to FR-008)
- **FR-001:** System MUST allow users to register with unique email and password (minimum 8 characters)
- **FR-002:** Supabase Auth MUST hash passwords using bcrypt before storing (delegated to Supabase)
- **FR-003:** Supabase Auth MUST issue JWT token on successful login containing UserId claim
- **FR-004:** System MUST validate JWT token on every authenticated request using Supabase public key
- **FR-005:** System MUST enforce tenant isolation: all queries must filter by current user's UserId
- **FR-006:** System MUST return 401 Unauthorized if request lacks valid JWT token
- **FR-007:** System MUST return 403 Forbidden if user attempts cross-tenant access (with security logging)
- **FR-008:** System MUST allow users to log out, clearing session/cookie

### Data Integrity & Security (FR-009 to FR-013)
- **FR-009:** System MUST prevent duplicate registrations (email must be unique) - enforced by Supabase
- **FR-010:** System MUST validate email format during registration (RFC 5322 compliant)
- **FR-011:** System MUST extract UserId from JWT claim and inject into IUserContext for each request
- **FR-012:** System MUST maintain audit log of failed login attempts (for future monitoring/alerts)
- **FR-013:** Supabase Auth MUST handle password hashing + storage (no custom implementation)

---

## API Contracts

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
2. ValidationBehavior validates email + password (8+ chars minimum)
3. Infrastructure: SupabaseAuthClient.SignUp(email, password)
4. SupabaseAuthClient calls POST /auth/v1/signup on Supabase
5. Supabase hashes password with bcrypt, stores in Auth table
6. Domain: User entity created in users table
7. Domain publishes UserCreatedDomainEvent
8. Response: 201 Created with UserId + success message

**Status Codes**: 201 Created | 400 Bad Request | 409 Conflict

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
2. ValidationBehavior validates input format
3. Infrastructure: SupabaseAuthClient.SignIn(email, password)
4. SupabaseAuthClient calls POST /auth/v1/token on Supabase
5. Supabase verifies credentials, returns JWT + user info
6. Response: 200 OK { JwtToken, UserId, Email }
7. Frontend stores JwtToken in HttpOnly cookie

**Status Codes**: 200 OK | 401 Unauthorized | 503 Service Unavailable

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

### Environment Variables

```env
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_KEY=your-anon-key
SUPABASE_JWT_SECRET=your-jwt-secret
JWT_ALGORITHM=HS256
ALLOWED_ORIGINS=https://localhost:7001,https://example.com
```

### Startup Configuration (Program.cs)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new() {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["SUPABASE_JWT_SECRET"])
            )
        };
    });

builder.Services.AddAuthorization();
app.UseAuthentication();
app.UseAuthorization();
```

---

## Security Considerations

### Password Security
- ✅ Delegated to Supabase Auth: Passwords hashed with bcrypt + salt
- Password minimum: 8 characters
- No plaintext passwords in logs or responses
- No passwords stored in users table

### JWT Token Security
- **Algorithm:** HS256 (HMAC SHA256)
- **Lifetime:** 3600 seconds (1 hour)
- **Claims:** sub, exp, iat, email, aud
- **Storage:** HttpOnly cookie (prevents XSS theft)
- **Validation:** Public key verification on each request

### Audit Logging
- Log all login attempts (success + failure) with timestamp + IP
- Log ALL cross-tenant access attempts with user/resource context
- Log token expiry events
- Severity levels: INFO, SECURITY, CRITICAL

### Rate Limiting
- 5 failed login attempts per email → 15 minute lockout (via Supabase Auth)
- 10 register attempts per IP → 1 hour lockout (implement in middleware)

### HTTPS & TLS
- All auth endpoints must use HTTPS (enforced in production)
- No redirect from HTTPS to HTTP
- HSTS headers enabled

---

## Deliverables (18 items)

### Domain Layer (2)
- [ ] User entity (Id, Email, CreatedAt, UpdatedAt) with invariants
- [ ] UserCreatedDomainEvent for event publishing

### Application Layer (8)
- [ ] SupabaseUserContext (implements IUserContext, extracts UserId from JWT)
- [ ] RegisterUserCommand + RegisterUserCommandHandler
- [ ] LoginUserCommand + LoginUserCommandHandler (returns Supabase JWT)
- [ ] UserDto (Email, CreatedAt read-only)
- [ ] AuthenticationBehavior (MediatR pipeline validation)
- [ ] DependencyInjection.cs (register auth services)
- [ ] RegisterUserValidator (FluentValidation)
- [ ] LoginUserValidator (FluentValidation)

### Infrastructure Layer (5)
- [ ] SupabaseAuthClient (Supabase Auth API wrapper for Sign Up + Sign In)
- [ ] UserRepository (implements IRepository<User>)
- [ ] Migration: 001_CreateUsersTable.sql (NO password column)
- [ ] DependencyInjection.cs (register Supabase services)
- [ ] SupabaseJwtTokenProvider (validate JWT tokens from Supabase)

### Frontend Layer (3)
- [ ] Register.cshtml + Register.cshtml.cs (PageModel)
- [ ] Login.cshtml + Login.cshtml.cs (PageModel)
- [ ] Profile.cshtml + Profile.cshtml.cs (display current user)

### Middleware & Configuration (2)
- [ ] JwtAuthenticationMiddleware (validate token on each request)
- [ ] SessionManagement (HttpOnly cookie setup)

---

## Test Specifications (8 tests - T01-001 to T01-008)

**T01-001:** User entity compiles + has Email property with invariants  
**T01-002:** RegisterUserCommand creates user + publishes UserCreatedDomainEvent  
**T01-003:** LoginUserCommand validates credentials + returns JWT  
**T01-004:** SupabaseUserContext extracts UserId from JWT claims correctly  
**T01-005:** AuthenticationBehavior blocks unauthenticated requests with 401  
**T01-006:** User repository CRUD operations work (Add, Get, Update, Delete)  
**T01-007:** Supabase migration creates users table with correct schema  
**T01-008:** Multi-tenant query filtering respects UserId + TenantId enforcement  

---

## Task Breakdown (~30 tasks, organized in 3 phases)

### Phase I: User Entity & Database (10 tasks)
- T01 Create User entity in Domain
- T02 Create UserCreatedDomainEvent in Domain
- T03 Write test T01-001 (User entity)
- T04 Create Supabase migration (001_CreateUsersTable.sql)
- T05 Verify schema in Supabase
- T06 Create IRepository<User> interface in Domain
- T07 Create UserRepository implementation in Infrastructure
- T08 Write test T01-002 (User creation + event)
- T09 Create UserDto in Application
- T10 Verify tests pass (T01-001, T01-002)

### Phase II: Authentication Services (10 tasks)
- T11 Create SupabaseAuthClient in Infrastructure
- T12 Create RegisterUserValidator in Application
- T13 Create LoginUserValidator in Application
- T14 Create RegisterUserCommand + handler in Application
- T15 Create LoginUserCommand + handler in Application
- T16 Create SupabaseUserContext in Application
- T17 Create AuthenticationBehavior in Application
- T18 Update DependencyInjection.cs (register auth services)
- T19 Write tests T01-003 through T01-005
- T20 Verify tests pass (T01-003 to T01-005)

### Phase III: Frontend & Integration (10 tasks)
- T21 Create Register.cshtml + Register.cshtml.cs (PageModel)
- T22 Create Login.cshtml + Login.cshtml.cs (PageModel)
- T23 Create Profile.cshtml + Profile.cshtml.cs (PageModel)
- T24 Wire authentication middleware in Program.cs
- T25 Implement SessionManagement (HttpOnly cookies)
- T26 Update Layout.cshtml (show login/logout links)
- T27 Write tests T01-006 through T01-008
- T28 Verify end-to-end authentication flow
- T29 Update ARCHITECTURE.md with authentication patterns
- T30 Final validation: All tests passing, coverage ≥80%

---

## Success Criteria

✅ User can register via email + password  
✅ User receives JWT token on login  
✅ All queries filtered by current user  
✅ Cross-tenant requests rejected + logged  
✅ All 8 tests passing (T01-001 to T01-008)  
✅ Code coverage ≥80% (Domain + Application)  
✅ All Razor Pages require authentication  
✅ Audit logging for security events  

---

## Next Phase

**Phase 2: Core Data Model & Domain Entities** (2-3 weeks)
- Create Category entity
- Create Transaction entity
- Create Budget entity
- Implement domain validation rules
- Transaction persistence & queries
