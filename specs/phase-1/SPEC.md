# SauronSheet Phase 1: Authentication & Multi-Tenancy Foundation

**Version**: 1.0.0  
**Duration**: 3-4 weeks  
**Status**: ⏳ Blocked by Phase 0  
**Depends**: Phase 0 (complete)

---

## Goal

Implement Supabase authentication + user management, and establish multi-tenancy enforcement at the database layer. This phase enables Phase 2 (domain entities) to be tenant-aware.

---

## Requirements

### Functional Requirements

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| **FR-001** | User registration via Supabase Auth | Email verified, JWT token issued |
| **FR-002** | User login flow | Credentials validated, session stored |
| **FR-003** | User logout clears session | User context invalidated |
| **FR-004** | Multi-tenancy: User sees only own data | Query results filtered by UserId |
| **FR-005** | Admin flag support | Admin users can bypass query filters (Phase 5+) |

### Non-Functional Requirements
- NF-001: All queries default-scoped to current tenant (ScopedQueryBehavior active)
- NF-002: 8 integration tests verifying auth + tenancy
- NF-003: Zero unauthenticated data access
- NF-004: JWT token expiry enforced (default 1 hour)

---

## Architecture Changes

### New Components
- `User` entity (Domain layer)
- `IUserContext` implementation (Infrastructure: SupabaseUserContext)
- Supabase Auth client configuration
- JWT middleware for Razor Pages
- User repository (generic IRepository<User>)

### Database Schema
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    supabase_auth_id UUID NOT NULL UNIQUE,
    is_admin BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- RLS Policy: Users can only see their own record
CREATE POLICY "Users see own record" ON users
FOR SELECT USING (id = auth.uid());
```

---

## Deliverables

### Domain Layer
- [ ] `Domain/Entities/User.cs` - User aggregate root with email, isAdmin
- [ ] `Domain/ValueObjects/Email.cs` - Email value object with validation

### Infrastructure Layer
- [ ] `Infrastructure/Authentication/SupabaseUserContext.cs` - IUserContext implementation
- [ ] `Infrastructure/Persistence/Repositories/UserRepository.cs`
- [ ] `Infrastructure/Persistence/Migrations/002_CreateUsersTable.sql`
- [ ] `Infrastructure/Authentication/JwtTokenProvider.cs`

### Application Layer
- [ ] `Application/Features/Auth/RegisterUserCommand.cs` + handler
- [ ] `Application/Features/Auth/LoginUserQuery.cs` + handler
- [ ] `Application/Tests/Features/Auth/RegisterUserTests.cs` (4 tests)
- [ ] `Application/Tests/Features/Auth/MultiTenancyTests.cs` (4 tests)

### Frontend Layer
- [ ] `Frontend/Pages/Auth/Login.cshtml` + `Login.cshtml.cs`
- [ ] `Frontend/Pages/Auth/Register.cshtml` + `Register.cshtml.cs`
- [ ] `Frontend/Pages/Auth/Logout.cshtml.cs`

---

## Test Specifications

### T01-001: User Registration Creates User and Returns JWT
**When**: RegisterUserCommand executed with valid email  
**Then**: User entity created in DB + JWT token returned

### T01-002: Duplicate Email Rejected in Registration
**When**: RegisterUserCommand with existing email  
**Then**: ValueObjectValidationException thrown

### T01-003: Login Returns JWT for Valid Credentials
**When**: LoginUserQuery with correct email + password  
**Then**: JWT token returned

### T01-004: Login Fails for Invalid Credentials
**When**: LoginUserQuery with wrong password  
**Then**: EntityNotFoundException or custom AuthException thrown

### T01-005: ScopedQueryBehavior Enforces Tenant Isolation
**When**: User queries another user's data  
**Then**: Security exception thrown (cross-tenant blocked)

### T01-006: IUserContext Returns Correct User ID
**When**: IUserContext injected in query handler  
**Then**: CurrentUserId matches authenticated user

### T01-007: JWT Token Expiry Enforced
**When**: Expired JWT token used in request  
**Then**: HTTP 401 Unauthorized returned

### T01-008: Admin User Can Override Query Filters (Future)
**When**: Admin user executes query  
**Then**: Query handler recognizes admin flag (test passes, enforcement Phase 5+)

---

## Success Criteria

✅ Phase 1 is complete when:

1. `dotnet test` shows **8/8 Phase 1 tests passing**
2. User entity created with email + isAdmin properties
3. Supabase Auth configured in Infrastructure
4. IUserContext implementation functional
5. Login/Register pages render without errors
6. JWT token stored in session after login
7. ScopedQueryBehavior blocks cross-tenant queries
8. All Phase 0 tests still passing (11/11)

---

## Dependencies

- **Blocks**: Phase 2 (domain entities need user context)
- **Incoming**: Phase 0 (complete)

---

## Timeline

- **Week 1**: User entity + Email value object + Supabase setup
- **Week 2**: Auth commands/queries + IUserContext implementation
- **Week 3**: Login/Register pages + 8 tests
- **Week 4**: JWT middleware + multi-tenancy enforcement

Target: 8 tests green + Supabase Auth working locally

---

**Specification Version**: 1.0.0  
**Last Updated**: 2026-02-14
