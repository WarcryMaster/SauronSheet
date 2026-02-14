# Phase 1: Authentication & Multi-Tenancy

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

---

## Architecture Changes from Phase 0

**New Components**:
- User entity (Id, Email, CreatedAt, ...)
- Authentication middleware (JWT validation)
- Real SupabaseUserContext (replaces MockUserContext)
- Register + Login page models
- Supabase Auth API integration

**Database Schema**:
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);
```

---

## Deliverables (20 items)

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
