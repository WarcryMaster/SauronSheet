# SauronSheet — Full Project Specification (Phases 0-6)

> **Version**: 1.0.0 | **Created**: 2026-02-15 | **Constitution**: v1.1.0
> **MVP Launch**: End of Phase 4 (Week 18) | **Production Release**: End of Phase 6 (Week 24)

---

## Table of Contents

- [Phase Overview Map](#phase-overview-map)
- [Phase 0: Foundation & Infrastructure Setup](#phase-0-foundation--infrastructure-setup)
- [Phase 1: Authentication & Multi-Tenancy](#phase-1-authentication--multi-tenancy)
- [Phase 2: Core Data Model & Domain Entities](#phase-2-core-data-model--domain-entities)
- [Phase 3: Transaction Import Pipeline](#phase-3-transaction-import-pipeline)
- [Phase 4: Analytics & Dashboard (MVP)](#phase-4-analytics--dashboard-mvp)
- [Phase 5: Budget Management & Alerts](#phase-5-budget-management--alerts)
- [Phase 6: UI Polish, Performance & Production Deployment](#phase-6-ui-polish-performance--production-deployment)
- [Cross-Phase Dependency Map](#cross-phase-dependency-map)

---

## Phase Overview Map

| Phase | Name | Type | Layers In Scope | Weeks | Cumulative |
|-------|-------------------------------------|-----------------|------------------------------|-------|------------|
| 0     | Foundation & Infrastructure Setup   | Foundation      | All layers                   | 1-2   | Week 2     |
| 1     | Authentication & Multi-Tenancy      | Full-Stack (Auth)| All layers                  | 3-5   | Week 5     |
| 2     | Core Data Model & Domain Entities   | Domain-Only     | Domain                       | 6-8   | Week 8     |
| 3     | Transaction Import Pipeline         | Full-Stack      | All layers                   | 9-13  | Week 13    |
| 4     | Analytics & Dashboard               | Full-Stack      | All layers                   | 14-18 | Week 18 ✅ MVP |
| 5     | Budget Management & Alerts          | Full-Stack      | All layers                   | 19-21 | Week 21    |
| 6     | UI Polish, Performance & Deployment | Polish          | Frontend + Infrastructure    | 22-24 | Week 24 🚀 Production |

---

# Phase 0: Foundation & Infrastructure Setup

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Foundation)
- **Duration**: Weeks 1-2
- **Phase Type**: Foundation
- **Goal**: Solution structure, base abstractions, CI/CD, testing infrastructure

---

## Critical Decisions

| ID    | Decision                                   | Rationale                                                      |
|-------|--------------------------------------------|----------------------------------------------------------------|
| CD-0.1| .NET 10 multi-project solution structure    | Enforces Clean Architecture physical boundaries                |
| CD-0.2| MediatR 12+ integrated from day one        | Avoids retrofit; all use cases flow through pipeline           |
| CD-0.3| xUnit + Moq as testing foundation          | Industry standard for .NET; aligns with constitution           |
| CD-0.4| Supabase client setup in Infrastructure    | Constitution mandate: no Supabase references in Domain/App     |
| CD-0.5| Tailwind CSS via CDN for initial setup     | Simplifies Phase 0; full build pipeline deferred to Phase 6    |
| CD-0.6| Strong-typed ID base pattern in Domain     | All future entities must use this; prevents raw Guid/string    |

---

## Executive Summary

### In Scope

- **Solution structure**: 6 .NET projects with correct dependency graph
- **Domain.Common**: Base `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject` abstractions
- **Domain.Exceptions**: `DomainException`, `EntityNotFoundException`
- **Domain.Repositories**: `ISpecification<T>` base interface (MaxResults = 1000)
- **Application.Common**: MediatR pipeline registration, `IUserContext` interface
- **Infrastructure**: Supabase client configuration (connection only, no tables)
- **Frontend**: Minimal Razor Pages app with `Program.cs`, layout, Tailwind CSS CDN, health check page
- **Testing**: xUnit test projects for Domain and Application layers with seed tests
- **CI/CD**: `dotnet build` + `dotnet test` validation

### Deferred

| Item                        | Target Phase | Reason                        |
|-----------------------------|--------------|-------------------------------|
| Domain entities             | Phase 2      | Domain-Only phase             |
| Authentication & JWT        | Phase 1      | Full-Stack (Auth) phase       |
| Supabase tables/migrations  | Phase 1+     | No entities to persist yet    |
| PDF parsing library         | Phase 3      | Transaction Import phase      |
| Analytics queries           | Phase 4      | Analytics & Dashboard phase   |
| Budget management           | Phase 5      | Budget Management phase       |
| Vercel deployment           | Phase 6      | Polish phase                  |
| Domain events (IDomainEvent)| Future       | Not needed in foundation      |
| Alpine.js                   | Phase 3+     | No interactive UI yet         |

---

## User Scenarios & Testing

### Scenario 0.1: Developer Clones and Builds

**As a** developer joining the project
**I want to** clone the repo, run `dotnet build`, and have zero errors
**So that** I can start contributing immediately

**Acceptance Criteria:**
- `dotnet build` completes with zero warnings and zero errors
- Solution contains 6 projects: Domain, Application, Infrastructure, Frontend, Domain.Tests, Application.Tests
- Project references enforce dependency rules

### Scenario 0.2: All Seed Tests Pass

**As a** developer
**I want to** run `dotnet test` and see all foundation tests pass
**So that** the testing infrastructure is proven working

**Acceptance Criteria:**
- `dotnet test` discovers and runs ≥9 tests
- All tests pass (green)
- Tests categorized with `[Trait("Category", "Domain")]` or `[Trait("Category", "Application")]`

### Scenario 0.3: Frontend Health Check Renders

**As a** developer
**I want to** run the Frontend project and see a health check page
**So that** the Razor Pages pipeline is proven working

**Acceptance Criteria:**
- `dotnet run --project Frontend/` starts without errors
- Navigating to `/` shows "SauronSheet — System OK"
- Page uses Tailwind CSS styling via shared layout

### Scenario 0.4: Dependency Rules Enforced

**As an** architect
**I want** the project references to enforce Clean Architecture boundaries
**So that** no layer can accidentally reference a forbidden layer

**Acceptance Criteria:**
- `Domain.csproj`: ZERO `<ProjectReference>` entries
- `Application.csproj`: references ONLY Domain
- `Infrastructure.csproj`: references ONLY Domain
- `Frontend.csproj`: references Application + Infrastructure (DI only)
- `Domain.Tests.csproj`: references ONLY Domain
- `Application.Tests.csproj`: references Application + Domain

---

## Functional Requirements

### FR-0.01: Solution Structure

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

### FR-0.02: Domain.Common Base Abstractions

| Abstraction        | Requirements                                                                           |
|--------------------|----------------------------------------------------------------------------------------|
| `Entity<TId>`      | Generic Id, CreatedAt (DateTime, set on construction), UpdatedAt (DateTime?, null initially), protected constructor, no public setters |
| `AggregateRoot<TId>`| Inherits Entity<TId>; marker base class (domain events deferred)                       |
| `ValueObject`      | Abstract record base; value-based equality via C# records; ToString() override          |

### FR-0.03: Domain Exceptions

| Exception                  | Inherits From     | Constructor                                         | Message Format                                           |
|----------------------------|-------------------|-----------------------------------------------------|----------------------------------------------------------|
| `DomainException`          | `Exception`       | `(string message)`, `(string message, Exception inner)` | Passed message                                           |
| `EntityNotFoundException`  | `DomainException` | `(string entityName, object entityId)`              | `"Entity '{entityName}' with id '{entityId}' was not found."` |

### FR-0.04: ISpecification<T>

- `Expression<Func<T, bool>> Criteria { get; }`
- `int MaxResults { get; }` → default `1000`
- `List<Expression<Func<T, object>>> Includes { get; }`
- `List<string> IncludeStrings { get; }`

### FR-0.05: Application.Common

- `IUserContext`: `string UserId { get; }`, `bool IsAuthenticated { get; }`
- `DependencyInjection.cs`: `AddApplicationServices()` extension → registers MediatR from Application assembly

### FR-0.06: Infrastructure Configuration

- `DependencyInjection.cs`: `AddInfrastructureServices(IConfiguration config)` extension
- Reads `Supabase:Url` and `Supabase:Key` from config
- Validates on startup (throws if missing)
- Registers Supabase client as singleton

### FR-0.07: Frontend Minimal Setup

- `Program.cs`: calls `AddApplicationServices()` + `AddInfrastructureServices(config)`
- `_Layout.cshtml` with Tailwind CDN
- `Index.cshtml`: "SauronSheet — System OK" health check page
- Standard middleware: HTTPS redirect, static files, routing, Razor Pages

---

## Test Specifications

| Test ID | Test Name                                       | Category    | Assert                                                    |
|---------|------------------------------------------------|-------------|-----------------------------------------------------------|
| T-0.01  | Entity_SetsCreatedAtOnConstruction              | Domain      | CreatedAt ≈ UtcNow (±1s), UpdatedAt is null               |
| T-0.02  | Entity_EqualityByIdAndType                      | Domain      | Same type + same Id → equal                               |
| T-0.03  | Entity_InequalityByDifferentId                  | Domain      | Same type + different Id → not equal                      |
| T-0.04  | ValueObject_EqualityByProperties                | Domain      | Identical properties → equal                              |
| T-0.05  | ValueObject_InequalityByDifferentProperties     | Domain      | Different properties → not equal                          |
| T-0.06  | DomainException_CarriesMessage                  | Domain      | Message equals input string                               |
| T-0.07  | DomainException_CarriesInnerException           | Domain      | InnerException is set                                     |
| T-0.08  | EntityNotFoundException_FormatsMessage           | Domain      | Message matches format template                           |
| T-0.09  | Specification_DefaultMaxResultsIs1000           | Domain      | MaxResults == 1000                                        |
| T-0.10  | MediatR_ResolvesFromServiceProvider             | Application | IMediator resolved is not null                            |

---

## Deliverables

| #   | Deliverable                             | Layer          | Acceptance                                              |
|-----|-----------------------------------------|----------------|---------------------------------------------------------|
| D-0.01 | `SauronSheet.sln` with 6 projects    | All            | `dotnet build` succeeds; dependency graph validated     |
| D-0.02 | `Entity<TId>`, `AggregateRoot<TId>`  | Domain         | Unit tests prove construction, immutability             |
| D-0.03 | `ValueObject` abstract record         | Domain         | Unit tests prove value-based equality                   |
| D-0.04 | `DomainException` + `EntityNotFoundException` | Domain  | Unit tests prove message formatting                     |
| D-0.05 | `ISpecification<T>` interface         | Domain         | Unit test proves MaxResults default = 1000              |
| D-0.06 | `IUserContext` interface              | Application    | Compiles; test verifies contract                        |
| D-0.07 | MediatR DI registration              | Application    | Integration test: mediator resolves                     |
| D-0.08 | Supabase client DI registration       | Infrastructure | Missing config → startup exception                      |
| D-0.09 | Frontend health check page            | Frontend       | Renders with Tailwind styling at localhost               |
| D-0.10 | Domain.Tests (≥9 tests)              | Tests          | `dotnet test --filter Category=Domain` all green        |
| D-0.11 | Application.Tests (≥1 test)          | Tests          | `dotnet test --filter Category=Application` all green   |

---

## Success Criteria

| #     | Criterion                                                         | Metric                                |
|-------|-------------------------------------------------------------------|---------------------------------------|
| SC-0.1 | Project builds from clean clone with zero errors                 | `dotnet build` exit code 0            |
| SC-0.2 | All foundation tests pass                                        | `dotnet test` exit code 0, ≥10 tests  |
| SC-0.3 | Dependency rules physically enforced                             | Audit of 6 `.csproj` files            |
| SC-0.4 | Frontend renders health check page                               | Visual verification at localhost       |
| SC-0.5 | Domain has ZERO NuGet dependencies                               | No `<PackageReference>` in Domain.csproj |
| SC-0.6 | Domain test coverage ≥ 80%                                       | coverlet report                        |
| SC-0.7 | MediatR registers successfully                                   | Integration test passes                |
| SC-0.8 | Infrastructure validates config on startup                       | Missing config throws descriptive error |

---

## Assumptions

1. .NET 10 SDK is available on the development machine.
2. Supabase account is already created with URL and Key available.
3. Tailwind CSS CDN is acceptable for Phase 0 (build pipeline in Phase 6).
4. Domain events (`IDomainEvent`) are NOT included — `AggregateRoot` extended later if needed.
5. Alpine.js NOT included — added when interactive UI is needed (Phase 3+).
6. No authentication in Phase 0 — health check page is public.
7. `supabase-csharp` NuGet package is used as the Supabase client library.

---

## Risks & Mitigations

| Risk                                  | Impact | Mitigation                                                |
|---------------------------------------|--------|-----------------------------------------------------------|
| .NET 10 SDK not yet stable            | High   | Pin SDK version in `global.json`; fallback to .NET 9      |
| Supabase C# client lacks features     | Medium | Wrap in Infrastructure abstraction; swap if needed         |
| Tailwind CDN performance              | Low    | Temporary; full build pipeline in Phase 6                 |
| MediatR 12+ breaking changes          | Low    | Pin exact version in `Application.csproj`                 |

---

# Phase 1: Authentication & Multi-Tenancy

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Auth)
- **Duration**: Weeks 3-5
- **Phase Type**: Full-Stack (Auth)
- **Goal**: Secure multi-user authentication with tenant-scoped data access
- **Depends On**: Phase 0 (Foundation)

---

## Critical Decisions

| ID     | Decision                                               | Rationale                                                                  |
|--------|--------------------------------------------------------|----------------------------------------------------------------------------|
| CD-1.1 | Supabase Auth as identity provider                     | Free tier, built-in JWT, email/password + social login ready               |
| CD-1.2 | JWT stored in secure HTTP-only cookies                 | Prevents XSS token theft; standard web security practice                   |
| CD-1.3 | Tenant scoping enforced in Application handlers        | Constitution: "enforced in handler, not UI" — prevents UI bypass           |
| CD-1.4 | `UserId` strong-typed value object for all user references | Constitution: raw string for entity IDs is a compliance violation       |
| CD-1.5 | Auth middleware in Frontend extracts JWT claims         | Single point of authentication; all downstream layers receive `IUserContext` |

---

## Executive Summary

### In Scope

- **Domain**: `UserId` value object (already in common plan, formalized here)
- **Application**: `IUserContext` implementation, auth pipeline behavior, auth commands/queries
- **Infrastructure**: `SupabaseAuthService` implementing auth interfaces, JWT validation middleware
- **Frontend**: Login page, Register page, Logout flow, auth-protected page routing
- **Database**: Supabase Auth tables (managed by Supabase), `users` profile table migration

### Deferred

| Item                           | Target Phase | Reason                              |
|--------------------------------|-------------|--------------------------------------|
| Social login (Google, GitHub)  | Post-MVP    | Email/password sufficient for MVP    |
| Password reset flow            | Phase 6     | Polish phase                         |
| Role-based authorization       | Post-MVP    | Single role (user) sufficient for MVP |
| Multi-factor authentication    | Post-MVP    | Not required for expense tracking    |
| Session management UI          | Phase 6     | Polish phase                         |

---

## User Scenarios & Testing

### Scenario 1.1: New User Registration

**As a** new user
**I want to** create an account with email and password
**So that** I can track my personal expenses securely

**Acceptance Criteria:**
- Registration page with email, password, confirm password fields
- Password minimum 8 characters
- Email must be valid format
- On success: redirect to dashboard (or health check page in Phase 1)
- On failure: display specific error message (email taken, weak password, etc.)
- User profile record created in `users` table

### Scenario 1.2: Existing User Login

**As a** registered user
**I want to** log in with my email and password
**So that** I can access my expense data

**Acceptance Criteria:**
- Login page with email and password fields
- On success: JWT stored in HTTP-only secure cookie, redirect to dashboard
- On failure: display "Invalid email or password" (no information leakage)
- Login attempts are rate-limited (Supabase built-in)

### Scenario 1.3: User Logout

**As a** logged-in user
**I want to** log out
**So that** my session is terminated securely

**Acceptance Criteria:**
- Logout button visible in navigation when authenticated
- On click: JWT cookie cleared, Supabase session revoked, redirect to login page
- Subsequent requests to protected pages redirect to login

### Scenario 1.4: Tenant Isolation

**As a** user
**I want** my data to be completely isolated from other users
**So that** no one else can see my expenses

**Acceptance Criteria:**
- Every Application handler receives `IUserContext` with current `UserId`
- All queries automatically scoped to current user's `UserId`
- Attempting to access another user's data returns "not found" (not "forbidden")
- No UI element ever displays cross-user data

### Scenario 1.5: Unauthenticated Access Redirect

**As an** unauthenticated visitor
**I want to** be redirected to the login page when accessing protected pages
**So that** the application is secure

**Acceptance Criteria:**
- All pages except `/Login`, `/Register`, and `/` (health check) require authentication
- Unauthenticated requests to protected pages redirect to `/Login`
- After login, user is redirected to originally requested page (return URL)

---

## Functional Requirements

### FR-1.01: Auth Commands & Queries

| Command / Query               | Type    | Input                                | Output                           |
|-------------------------------|---------|--------------------------------------|----------------------------------|
| `RegisterUserCommand`         | Command | Email, Password, ConfirmPassword     | `UserId` on success              |
| `LoginUserCommand`            | Command | Email, Password                      | JWT token + refresh token        |
| `LogoutUserCommand`           | Command | (current user context)               | Success/failure                  |
| `GetCurrentUserQuery`         | Query   | (current user context)               | `UserProfileDto`                 |
| `RefreshTokenCommand`         | Command | Refresh token                        | New JWT token + refresh token    |

### FR-1.02: IUserContext Implementation
Application/Common/
├── IUserContext.cs # Interface (from Phase 0)
└── Behaviors/
└── TenantScopingBehavior.cs # Pipeline behavior injecting UserId into handlers

text

- `IUserContext` resolved from HTTP context via middleware
- Extracts `sub` claim from JWT as `UserId`
- Throws `UnauthorizedAccessException` if no valid JWT present on protected routes

### FR-1.03: Infrastructure Auth Service
Infrastructure/Auth/
├── SupabaseAuthService.cs # Implements IAuthService (Domain interface)
├── JwtCookieMiddleware.cs # Reads JWT from cookie, validates, sets ClaimsPrincipal
└── AuthConfiguration.cs # Supabase Auth config binding

text

- `IAuthService` interface defined in Domain (contract)
- `SupabaseAuthService` calls Supabase Auth REST API
- JWT validation using Supabase public key
- Cookie: `HttpOnly`, `Secure`, `SameSite=Strict`, expiration = JWT expiration

### FR-1.04: Frontend Auth Pages

| Page              | Route       | Auth Required | Description                        |
|-------------------|-------------|---------------|------------------------------------|
| Login             | `/Login`    | No            | Email + password form              |
| Register          | `/Register` | No            | Email + password + confirm form    |
| Dashboard (stub)  | `/Dashboard`| Yes           | Placeholder: "Welcome, {email}"    |

### FR-1.05: Database Migration

```sql
-- Supabase manages auth.users automatically
-- Custom profile table:
CREATE TABLE public.users (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT NOT NULL,
    display_name TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- Row Level Security
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Users can view own profile"
    ON public.users FOR SELECT USING (auth.uid() = id);
CREATE POLICY "Users can update own profile"
    ON public.users FOR UPDATE USING (auth.uid() = id);
```

---

## Test Specifications

| Test ID | Test Name                                       | Category    | Assert                                                    |
|---------|------------------------------------------------|-------------|-----------------------------------------------------------|
| T-1.01  | RegisterUser_ValidInput_CreatesUser            | Application | Handler returns UserId; no exception                      |
| T-1.02  | RegisterUser_DuplicateEmail_ThrowsException    | Application | DomainException with "email already registered"            |
| T-1.03  | RegisterUser_WeakPassword_ThrowsException      | Application | DomainException with "password too weak"                  |
| T-1.04  | RegisterUser_MismatchedPasswords_ThrowsException| Application | Validation error before reaching auth service              |
| T-1.05  | LoginUser_ValidCredentials_ReturnsToken         | Application | JWT token returned, not null/empty                        |
| T-1.06  | LoginUser_InvalidCredentials_ThrowsException    | Application | UnauthorizedAccessException                                |
| T-1.07  | LogoutUser_ClearsSession                        | Application | Auth service revoke called                                |
| T-1.08  | GetCurrentUser_Authenticated_ReturnsProfile    | Application | UserProfileDto with correct email                        |
| T-1.09  | GetCurrentUser_Unauthenticated_ThrowsException | Application | UnauthorizedAccessException                                |
| T-1.10  | TenantScoping_InjectsUserIdIntoHandler         | Application | Handler receives correct UserId from IUserContext        |
| T-1.11  | UserId_ValueObject_Equality                    | Domain      | Same string value → equal; different → not equal          |
| T-1.12  | UserId_ValueObject_EmptyThrows                  | Domain      | Empty/null string → DomainException                        |

---

## Deliverables

| #   | Deliverable                             | Layer          | Acceptance                                              |
|-----|-----------------------------------------|----------------|---------------------------------------------------------|
| D-1.01 | `IAuthService` interface              | Domain         | Contract for auth operations                            |
| D-1.02 | `UserId` value object                  | Domain         | Unit tests: equality, empty throws                     |
| D-1.03 | Auth commands + handlers               | Application    | Integration tests: register, login, logout             |
| D-1.04 | `TenantScopingBehavior`                | Application    | Integration test: UserId injected                      |
| D-1.05 | `IUserContext` implementation          | Application    | Resolves from HTTP context                            |
| D-1.06 | `SupabaseAuthService`                  | Infrastructure | Implements IAuthService; calls Supabase Auth API      |
| D-1.07 | `JwtCookieMiddleware`                  | Infrastructure | JWT validation, cookie management                      |
| D-1.08 | Login page                              | Frontend       | Form, validation, redirect                            |
| D-1.09 | Register page                          | Frontend       | Form, validation, redirect                            |
| D-1.10 | Dashboard stub (protected)            | Frontend       | Auth required, shows welcome message                  |
| D-1.11 | `users` table migration                | Infrastructure | SQL migration with RLS policies                        |
| D-1.12 | Auth tests (≥12)                      | Tests          | All green                                              |

---

## Success Criteria

| #     | Criterion                                                         | Metric                                |
|-------|-------------------------------------------------------------------|---------------------------------------|
| SC-1.1 | Users can register with email/password	E2E: register → login → see dashboard |
| SC-1.2 | Users can log in and receive secure JWT cookie	Cookie: HttpOnly, Secure, SameSite |
| SC-1.3 | Protected pages redirect unauthenticated users	/Dashboard → /Login redirect |
| SC-1.4 | Tenant isolation verified	User A cannot see User B's data |
| SC-1.5 | All auth tests pass	dotnet test ≥12 auth tests green |
| SC-1.6 | Application layer test coverage ≥ 70%	coverlet report |

---

# Phase 2: Core Data Model & Domain Entities

## Quick Reference

- **Status**: Draft
- **Layer Scope**: Domain ONLY ⚠️
- **Duration**: Weeks 6-8
- **Phase Type**: Domain-Only
- **Goal**: Complete domain model with entities, value objects, services, specifications, and repository interfaces
- **Depends On**: Phase 0 (base abstractions), Phase 1 (UserId value object)
- **Coverage Requirement**: 100% domain layer (domain-only phase rule)

---

## Critical Decisions

| ID    | Decision                                   | Rationale                                                      |
|-------|--------------------------------------------|----------------------------------------------------------------|
| CD-2.1| Domain-Only phase: NO Application/Infrastructure code	| Constitution: "Domain Layer ONLY = no commands, queries, implementations"	|
| CD-2.2| All entity IDs are strong-typed value objects	| Constitution: raw Guid/string for entity IDs is compliance violation	|
| CD-2.3| Money value object supports single currency (EUR)	| Simplifies MVP; multi-currency deferred to post-MVP	|
| CD-2.4| 4 system default categories (immutable)	| Groceries, Transport, Utilities, Other — can't be deleted/renamed	|
| CD-2.5| Budget uniqueness: one per user-category-month	| Prevents conflicting budget definitions	|
| CD-2.6| Specification pattern for all query filtering	| Domain language for filtering; MaxResults 1000 default	|

---

## Executive Summary

### In Scope (Domain Layer ONLY)

- Entities: Transaction, Category, Budget
- Value Objects: TransactionId, CategoryId, BudgetId, Money, DateRange
- Domain Services: CategoryService (cross-entity logic)
- Specifications: TransactionByDateRangeSpec, TransactionByCategorySpec, TransactionByAmountRangeSpec
- Repository Interfaces: ITransactionRepository, ICategoryRepository, IBudgetRepository
- Domain Exceptions: Any additional domain-specific exceptions
- Unit Tests: 100% coverage of all domain code

### Deferred (NOT in this phase)

| Item	| Target Phase	| Reason	|
|-----------------------------|-------------|----------------------------------|
| MediatR commands/queries	| Phase 3+	| Application layer — out of scope	|
| Supabase table migrations	| Phase 3	| Infrastructure — out of scope	|
| Repository implementations	| Phase 3	| Infrastructure — out of scope	|
| UI for transactions/categories	| Phase 3+	| Frontend — out of scope	|
| PDF parsing	| Phase 3	| Infrastructure — out of scope	|
| Analytics aggregations	| Phase 4	| Application layer — out of scope	|

---

## User Scenarios & Testing

### Scenario 2.1: Transaction Creation with Validation
As a domain model
I want to enforce business rules on transaction creation
So that invalid transactions can never exist in the system

Acceptance Criteria:

- Transaction requires: Id, UserId, Amount (Money), Date, Description
- Date cannot be in the future → throws DomainException
- Description cannot be empty/whitespace → throws DomainException
- Amount can be positive (income) or negative (expense)
- Optional: ImportedFrom (PDF source reference), CategoryId
- Transaction is immutable after creation (modification via explicit methods only)

### Scenario 2.2: Category Management with System Defaults
As a domain model
I want to support user-defined and system-default categories
So that transactions can be categorized with protected defaults

Acceptance Criteria:

- Category requires: Id, UserId, Name, optional Color, optional Icon
- 4 system defaults: Groceries, Transport, Utilities, Other
- System defaults have IsSystemDefault = true
- CanDelete() returns false for system defaults
- CanDelete() returns false if category has active transactions (via parameter)
- Category names must be unique per user (validated by CategoryService)

### Scenario 2.3: Budget Tracking with Overage Detection
As a domain model
I want to track budgets per category per month
So that users can detect overspending

Acceptance Criteria:

- Budget requires: Id, UserId, CategoryId, Month (DateRange), Limit (Money)
- IsOverBudget(Money currentSpend) returns true when spend > limit
- PercentageUsed(Money currentSpend) returns decimal (e.g., 0.75 = 75%)
- RemainingAmount(Money currentSpend) returns Money (can be negative)
- One budget per user-category-month (invariant checked by domain service or specification)

### Scenario 2.4: Money Value Object Arithmetic
As a domain model
I want Money to encapsulate currency-safe arithmetic
So that financial calculations are always correct

Acceptance Criteria:

- Money(decimal amount, string currency = "EUR")
- Plus(Money other) → adds amounts (same currency or throws)
- Minus(Money other) → subtracts amounts (same currency or throws)
- IsPositive, IsNegative, IsZero properties
- Cross-currency operations throw DomainException ("Currency mismatch")
- Equality: same amount AND same currency

### Scenario 2.5: Specifications Filter Transactions
As a domain model
I want specifications to express filtering in domain language
So that queries are encapsulated without SQL knowledge

Acceptance Criteria:

- TransactionByDateRangeSpecification(DateRange range) → filters by date
- TransactionByCategorySpecification(CategoryId categoryId) → filters by category
- TransactionByAmountRangeSpecification(Money min, Money max) → filters by amount
- All specs inherit from ISpecification<Transaction>
- Default MaxResults = 1000

---

## Functional Requirements

### FR-2.01: Transaction Entity

```csharp
public class Transaction : AggregateRoot<TransactionId>
{
    public TransactionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public string? ImportedFrom { get; private set; }

    // Invariants enforced in constructor
    // Methods: Categorize(CategoryId), UpdateDescription(string)
}
```

### FR-2.02: Category Entity

```csharp
public class Category : AggregateRoot<CategoryId>
{
    public CategoryId Id { get; private set; }
    public UserId UserId { get; private set; }
    public string Name { get; private set; }
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public bool IsSystemDefault { get; private set; }

    // Guard: CanDelete(bool hasActiveTransactions)
    // Guard: CanRename() → false if IsSystemDefault
    // Method: Rename(string newName)
    // Static: CreateSystemDefault(CategoryId, UserId, string name)
}
```

### FR-2.03: Budget Entity

```csharp
public class Budget : AggregateRoot<BudgetId>
{
    public BudgetId Id { get; private set; }
    public UserId UserId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public DateRange Month { get; private set; }
    public Money Limit { get; private set; }

    // Methods: IsOverBudget(Money currentSpend), PercentageUsed(Money), RemainingAmount(Money)
    // Method: UpdateLimit(Money newLimit)
}
```

### FR-2.04: Value Objects

| Value Object       | Properties                              | Validation                                                 |
|--------------------|-----------------------------------------|------------------------------------------------------------|
| `TransactionId`    | Guid Value                              | Empty Guid → DomainException                                |
| `CategoryId`       | Guid Value                              | Empty Guid → DomainException                                |
| `BudgetId`         | Guid Value                              | Empty Guid → DomainException                                |
| `UserId`           | string Value                            | Null/empty → DomainException (from Phase 1)                |
| `Money`            | decimal Amount, string Currency        | Currency must be non-empty; arithmetic respects currency    |
| `DateRange`        | DateTime Start, DateTime End          | End must be ≥ Start; both required                          |

### FR-2.05: CategoryService

```csharp
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepo;

    public async Task ValidateUniqueName(UserId userId, string name);
    public bool CanDeleteCategory(Category category, bool hasActiveTransactions);
    public IReadOnlyList<Category> GetSystemDefaults(UserId userId);
}
```

### FR-2.06: Repository Interfaces

| Interface               | Key Methods                                                  |
|-------------------------|--------------------------------------------------------------|
| `ITransactionRepository`  | Add, GetById, FindBySpecification, GetByUserId, Delete, Update |
| `ICategoryRepository`     | Add, GetById, FindByNameAndUser, GetByUserId, GetSystemDefaults, Delete |
| `IBudgetRepository`      | Add, GetById, GetByUserAndCategoryAndMonth, GetByUserId, Update, Delete |

### FR-2.07: Specifications

| Specification                      | Criteria                                                              |
|------------------------------------|-----------------------------------------------------------------------|
| `TransactionByDateRangeSpecification`    | `t => t.Date >= range.Start && t.Date <= range.End`              |
| `TransactionByCategorySpecification`    | `t => t.CategoryId == categoryId`                                 |
| `TransactionByAmountRangeSpecification` | `t => t.Amount.Amount >= min && t.Amount.Amount <= max`             |
| `TransactionByUserSpecification`      | `t => t.UserId == userId`                                        |
| Default MaxResults = 1000          |

---

## Test Specifications

| Transaction Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.01  | Transaction_ValidConstruction_SetsAllProperties	| All properties match constructor args                    |
| T-2.02  | Transaction_FutureDate_ThrowsDomainException	| DomainException "cannot be in the future"                |
| T-2.03  | Transaction_EmptyDescription_ThrowsDomainException	| DomainException "Description is required"                |
| T-2.04  | Transaction_Categorize_UpdatesCategoryId		| CategoryId changes to new value                          |
| T-2.05  | Transaction_UpdateDescription_ChangesDescription	| Description updated; UpdatedAt set                        |
| T-2.06  | Transaction_NullUserId_ThrowsDomainException		| DomainException on null UserId                            |

| Category Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.07  | Category_ValidConstruction_SetsProperties		| All properties match                                      |
| T-2.08  | Category_SystemDefault_CanDeleteReturnsFalse		| CanDelete(false) → false                                  |
| T-2.09  | Category_WithActiveTransactions_CanDeleteReturnsFalse	| CanDelete(true) → false (even if not system default)    |
| T-2.10  | Category_UserDefined_NoTransactions_CanDeleteReturnsTrue	| CanDelete(false) → true                                    |
| T-2.11  | Category_SystemDefault_CanRenameReturnsFalse		| CanRename() → false                                      |
| T-2.12  | Category_UserDefined_RenameChangesName		| Name updated; UpdatedAt set                              |
| T-2.13  | Category_EmptyName_ThrowsDomainException		| DomainException "Name is required"                       |
| T-2.14  | Category_CreateSystemDefault_SetsFlag		| IsSystemDefault == true                                  |

| Budget Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.15  | Budget_ValidConstruction_SetsProperties		| All properties match                                      |
| T-2.16  | Budget_IsOverBudget_SpendExceedsLimit_ReturnsTrue	| Spend 150, Limit 100 → true                              |
| T-2.17  | Budget_IsOverBudget_SpendUnderLimit_ReturnsFalse	| Spend 50, Limit 100 → false                              |
| T-2.18  | Budget_PercentageUsed_CalculatesCorrectly		| Spend 75, Limit 100 → 0.75                              |
| T-2.19  | Budget_RemainingAmount_CalculatesCorrectly		| Limit 100 - Spend 75 → Money(25)                        |
| T-2.20  | Budget_RemainingAmount_Negative_WhenOverBudget		| Limit 100 - Spend 150 → Money(-50)                      |
| T-2.21  | Budget_UpdateLimit_ChangesLimit				| Limit updated; UpdatedAt set                              |
| T-2.22  | Budget_ZeroLimit_ThrowsDomainException		| DomainException "Limit must be positive"                 |

| Money Value Object Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.23  | Money_Plus_SameCurrency_AddsAmounts		| 100 EUR + 50 EUR = 150 EUR                               |
| T-2.24  | Money_Minus_SameCurrency_SubtractsAmounts		| 100 EUR - 30 EUR = 70 EUR                               |
| T-2.25  | Money_Plus_DifferentCurrency_ThrowsDomainException	| EUR + USD → DomainException "Currency mismatch"          |
| T-2.26  | Money_IsPositive_PositiveAmount_ReturnsTrue		| Money(50) → IsPositive = true                            |
| T-2.27  | Money_IsNegative_NegativeAmount_ReturnsTrue		| Money(-50) → IsNegative = true                            |
| T-2.28  | Money_IsZero_ZeroAmount_ReturnsTrue		| Money(0) → IsZero = true                                |
| T-2.29  | Money_Equality_SameAmountAndCurrency		| Money(100, "EUR") == Money(100, "EUR")                  |
| T-2.30  | Money_Inequality_DifferentAmount			| Money(100) != Money(200)                                |
| T-2.31  | Money_Inequality_DifferentCurrency		| Money(100, "EUR") != Money(100, "USD")                  |

| DateRange Value Object Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.32  | DateRange_ValidConstruction_SetsProperties		| Start and End match                                      |
| T-2.33  | DateRange_EndBeforeStart_ThrowsDomainException		| DomainException "End must be >= Start"                   |
| T-2.34  | DateRange_Equality_SameValues			| Same start+end → equal                                  |

| Strong-Typed ID Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.35  | TransactionId_EmptyGuid_ThrowsDomainException		| Guid.Empty → DomainException                             |
| T-2.36  | TransactionId_ValidGuid_SetsValue			| Value matches input Guid                                |
| T-2.37  | CategoryId_EmptyGuid_ThrowsDomainException		| Guid.Empty → DomainException                             |
| T-2.38  | BudgetId_EmptyGuid_ThrowsDomainException			| Guid.Empty → DomainException                             |

| CategoryService Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.39  | CategoryService_ValidateUniqueName_Duplicate_Throws	| DomainException "already exists"                         |
| T-2.40  | CategoryService_ValidateUniqueName_Unique_NoException	| No exception thrown                                       |
| T-2.41  | CategoryService_CanDeleteCategory_SystemDefault_False	| Returns false                                            |
| T-2.42  | CategoryService_CanDeleteCategory_ActiveTxns_False	| Returns false                                            |
| T-2.43  | CategoryService_CanDeleteCategory_Eligible_True	| Returns true                                             |
| T-2.44  | CategoryService_GetSystemDefaults_ReturnsFourCategories	| List.Count == 4                                       |

| Specification Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-2.45  | DateRangeSpec_MatchesTransactionsInRange		| Criteria evaluates true for in-range                      |
| T-2.46  | DateRangeSpec_ExcludesTransactionsOutOfRange		| Criteria evaluates false for out-of-range                 |
| T-2.47  | CategorySpec_MatchesTransactionsWithCategory		| Criteria evaluates true for matching CategoryId           |
| T-2.48  | AmountRangeSpec_MatchesTransactionsInRange		| Criteria evaluates true for in-range amounts              |
| T-2.49  | UserSpec_MatchesTransactionsForUser			| Criteria evaluates true for matching UserId                |
| T-2.50  | AllSpecs_DefaultMaxResults_1000			| MaxResults == 1000 for each spec                        |

---

## Deliverables

| #   | Deliverable                             | Layer          | Acceptance                                              |
|-----|-----------------------------------------|----------------|---------------------------------------------------------|
| D-2.01 | Transaction aggregate root            | Domain         | 6 unit tests passing (T-2.01 to T-2.06)                |
| D-2.02 | Category aggregate root               | Domain         | 8 unit tests passing (T-2.07 to T-2.14)                 |
| D-2.03 | Budget aggregate root                 | Domain         | 8 unit tests passing (T-2.15 to T-2.22)                 |
| D-2.04 | Money value object                    | Domain         | 9 unit tests passing (T-2.23 to T-2.31)                 |
| D-2.05 | DateRange value object                | Domain         | 3 unit tests passing (T-2.32 to T-2.34)                 |
| D-2.06 | TransactionId, CategoryId, BudgetId  | Domain         | 4 unit tests passing (T-2.35 to T-2.38)                 |
| D-2.07 | CategoryService                       | Domain         | 6 unit tests passing (T-2.39 to T-2.44)                 |
| D-2.08 | 4 Specifications                      | Domain         | 6 unit tests passing (T-2.45 to T-2.50)                 |
| D-2.09 | 3 Repository interfaces               | Domain         | Compile check; no implementation                        |
| D-2.10 | All Domain.Tests (50 tests)          | Tests          | 100% domain coverage; all green                        |

---

## Success Criteria

| #     | Criterion                                                   | Metric                                |
|-------|-------------------------------------------------------------|---------------------------------------|
| SC-2.1 | All 50 domain tests pass                                    | `dotnet test --filter Category=Domain` |
| SC-2.2 | Domain test coverage = 100%                                | coverlet report (domain-only phase)   |
| SC-2.3 | No Application/Infrastructure code created                  | Manual audit of project files          |
| SC-2.4 | All entities use strong-typed IDs                          | Code review: no raw Guid/string       |
| SC-2.5 | All value objects are immutable                            | No public setters; record types       |
| SC-2.6 | CategoryService uses mocked repository interfaces            | Test inspection: Moq used             |
| SC-2.7 | Domain project has ZERO NuGet dependencies                  | No <PackageReference> in Domain.csproj |

---

# Phase 3: Transaction Import Pipeline

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Features)
- **Duration**: Weeks 9-13
- **Phase Type**: Full-Stack (Features)
- **Goal**: PDF import pipeline, transaction CRUD, category management, Supabase persistence
- **Depends On**: Phase 0 (foundation), Phase 1 (auth), Phase 2 (domain model)

---

## Critical Decisions

| ID    | Decision                                   | Rationale                                                      |
|-------|--------------------------------------------|----------------------------------------------------------------|
| CD-3.1| PDF parsing library: iTextSharp or PdfPig	| Open-source, .NET compatible; decision finalized at implementation	|
| CD-3.2| Bank-specific parser strategy pattern	    | Different banks have different PDF formats; extensible design	|
| CD-3.3| Bulk import with per-row error reporting	| Users need to know which rows failed and why					|
| CD-3.4| Duplicate detection via date + amount + description hash	| Prevents re-importing same transactions						|
| CD-3.5| Supabase RLS policies for all tables		| Belt-and-suspenders: tenant isolation at DB level AND handler level|

---

## Executive Summary

### In Scope

- Application: Transaction commands/queries, category commands/queries, PDF import command
- Infrastructure: Supabase repository implementations, PDF parser, database migrations
- Frontend: Upload page, transaction list page, category management page
- Domain: Minor additions (duplicate detection, ImportBatch value object)

### Deferred

| Item                           | Target Phase | Reason                              |
|--------------------------------|-------------|--------------------------------------|
| Analytics/charts                | Phase 4      | Analytics phase                      |
| Budget CRUD                     | Phase 5      | Budget phase                        |
| Bulk edit/delete               | Post-MVP    | Not critical for MVP                |
| Multi-bank format support       | Post-MVP    | Start with one bank format          |
| Transaction search              | Phase 4      | Part of analytics                    |

---

## User Scenarios & Testing

### Scenario 3.1: Upload PDF Bank Statement
As a user
I want to upload a PDF bank statement
So that my transactions are automatically imported

Acceptance Criteria:

- Upload page accepts PDF files only (file type validation)
- Max file size: 10MB
- On upload: parse PDF → extract transaction rows → validate → persist
- Response shows: N transactions imported, M skipped (with reasons)
- Imported transactions appear in transaction list immediately
- Each imported transaction has ImportedFrom set to PDF filename
- Duplicate transactions are detected and skipped (not duplicated)

### Scenario 3.2: View Transaction List
As a user
I want to see all my transactions in a list
So that I can review my spending

Acceptance Criteria:

- Paginated list of transactions (default 50 per page)
- Sorted by date (newest first)
- Shows: date, description, amount, category (or "Uncategorized")
- Scoped to current user only
- Responsive design (mobile + desktop)

### Scenario 3.3: Manually Add Transaction
As a user
I want to manually add a transaction
So that I can track cash expenses or unimported items

Acceptance Criteria:

- Form: date, description, amount, category (dropdown)
- Validates domain rules (date not future, description required)
- Category optional (defaults to "Other" if not selected)
- On success: redirect to transaction list

### Scenario 3.4: Categorize Transaction
As a user
I want to assign a category to a transaction
So that my spending is organized

Acceptance Criteria:

- Transaction list shows category dropdown per row (or edit button)
- Category selection includes all user categories + system defaults
- On change: update transaction category via command

### Scenario 3.5: Manage Categories
As a user
I want to create, rename, and delete custom categories
So that I can organize spending my way

Acceptance Criteria:

- Category management page lists all categories (system + user-defined)
- System defaults are displayed but edit/delete disabled (visual indication)
- Create: form with name, optional color, optional icon
- Rename: inline edit (only user-defined)
- Delete: confirmation dialog; blocked if category has transactions
- Name uniqueness enforced (per user)

### Scenario 3.6: Delete Transaction
As a user
I want to delete a transaction
So that I can remove incorrect entries

Acceptance Criteria:

- Delete button on each transaction row
- Confirmation dialog before deletion
- On delete: transaction removed from list and database
- Scoped to current user only

---

## Functional Requirements

### FR-3.01: Application Commands & Queries

| Command / Query               | Type    | Input                                | Output                           |
|-------------------------------|---------|--------------------------------------|----------------------------------|
| `ImportTransactionsFromPdfCommand`   | Command | PDF file stream, UserId                        | `ImportResultDto`          |
| `CreateTransactionCommand`           | Command | UserId, Amount, Date, Description, CategoryId?   | `TransactionId`            |
| `UpdateTransactionCategoryCommand`   | Command | TransactionId, CategoryId, UserId         | Success                    |
| `UpdateTransactionDescriptionCommand`| Command | TransactionId, Description, UserId        | Success                    |
| `DeleteTransactionCommand`           | Command | TransactionId, UserId                     | Success                    |
| `CreateCategoryCommand`              | Command | UserId, Name, Color?, Icon?               | `CategoryId`               |
| `RenameCategoryCommand`              | Command | CategoryId, NewName, UserId               | Success                    |
| `DeleteCategoryCommand`              | Command | CategoryId, UserId                        | Success                    |
| `SeedSystemDefaultsCommand`          | Command | UserId                                    | `List<CategoryId>`         |

### FR-3.02: PDF Parsing Pipeline
Infrastructure/PDF/
├── IPdfParser.cs                  # Interface (defined in Domain or Application)
├── PdfParserFactory.cs            # Strategy pattern for bank-specific parsers
├── Parsers/
│   └── GenericBankPdfParser.cs    # Default parser implementation
└── Models/
    └── RawTransactionRow.cs       # Intermediate parsed row before domain validation

Pipeline Steps:

- Receive PDF stream
- Extract text/table data from PDF
- Parse rows into RawTransactionRow objects
- Validate each row against domain rules
- Check for duplicates (hash: date + amount + description)
- Create Transaction entities for valid rows
- Persist via ITransactionRepository
- Return ImportResultDto with counts and error details

### FR-3.03: Supabase Migrations

```sql
-- transactions table
CREATE TABLE public.transactions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    amount DECIMAL(15,2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    date TIMESTAMPTZ NOT NULL,
    description TEXT NOT NULL,
    category_id UUID REFERENCES public.categories(id) ON DELETE SET NULL,
    imported_from TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- categories table
CREATE TABLE public.categories (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    color VARCHAR(7),
    icon VARCHAR(50),
    is_system_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(user_id, name)
);

-- pdf_imports table
CREATE TABLE public.pdf_imports (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    filename TEXT NOT NULL,
    imported_count INT NOT NULL,
    skipped_count INT NOT NULL,
    imported_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_transactions_user_date ON public.transactions(user_id, date DESC);
CREATE INDEX idx_transactions_user_category ON public.transactions(user_id, category_id);
CREATE INDEX idx_categories_user ON public.categories(user_id);

-- RLS Policies
ALTER TABLE public.transactions ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Users access own transactions" ON public.transactions
    FOR ALL USING (auth.uid() = user_id);

ALTER TABLE public.categories ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Users access own categories" ON public.categories
    FOR ALL USING (auth.uid() = user_id);

ALTER TABLE public.pdf_imports ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Users access own imports" ON public.pdf_imports
    FOR ALL USING (auth.uid() = user_id);
```

### FR-3.04: Frontend Pages

| Page              | Route       | Auth Required | Description                        |
|-------------------|-------------|---------------|------------------------------------|
| Upload PDF        | /Transactions/Upload	| Yes	| File upload form + import results	|
| Transaction List  | /Transactions	| Yes	| Paginated list with category filters	|
| Add Transaction   | /Transactions/Add	| Yes	| Manual transaction form	|
| Categories        | /Categories	| Yes	| Create, rename, delete categories	|

---

## Test Specifications

| Application Handler Tests (Integration)
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-3.01  | ImportPdf_ValidPdf_ImportsTransactions             | ImportResultDto.ImportedCount > 0                         |
| T-3.02  | ImportPdf_DuplicateTransactions_SkipsDuplicates	| SkippedCount > 0; reason = "Duplicate"                    |
| T-3.03  | ImportPdf_InvalidRows_ReportsErrors             | SkippedCount > 0 with per-row error messages               |
| T-3.04  | CreateTransaction_ValidInput_ReturnsTransactionId	| TransactionId is not empty                                |
| T-3.05  | CreateTransaction_FutureDate_ThrowsDomainException	| DomainException propagated from domain                     |
| T-3.06  | UpdateTransactionCategory_ValidInput_Updates		| GetById returns updated CategoryId                      |
| T-3.07  | DeleteTransaction_ValidInput_Removes			| GetById throws EntityNotFoundException                    |
| T-3.08  | DeleteTransaction_WrongUser_ThrowsException		| UnauthorizedAccessException or EntityNotFound              |
| T-3.09  | GetTransactions_ReturnsOnlyUserTransactions	    | Result contains only current user's transactions            |
| T-3.10  | GetTransactions_Paginated_RespectsPageSize		| Result.Count ≤ pageSize                                   |
| T-3.11  | CreateCategory_ValidInput_ReturnsCategoryId		| CategoryId is not empty                                   |
| T-3.12  | CreateCategory_DuplicateName_ThrowsDomainException	| DomainException "already exists"                          |
| T-3.13  | RenameCategory_SystemDefault_ThrowsDomainException	| DomainException "cannot rename system default"            |
| T-3.14  | DeleteCategory_WithTransactions_ThrowsDomainException	| DomainException "has active transactions"                 |
| T-3.15  | SeedSystemDefaults_CreatesExactlyFour	        | Result count == 4                                        |
| T-3.16  | GetCategories_IncludesSystemDefaults	        | Result contains 4 system defaults                          |

---

## Deliverables

| #   | Deliverable                             | Layer          | Acceptance                                              |
|-----|-----------------------------------------|----------------|---------------------------------------------------------|
| D-3.01 | Import pipeline command + handler    | Application    | Tests T-3.01 to T-3.03 pass                           |
| D-3.02 | Transaction CRUD commands + handlers   | Application    | Tests T-3.04 to T-3.10 pass                           |
| D-3.03 | Category commands + handlers           | Application    | Tests T-3.11 to T-3.16 pass                           |
| D-3.04 | PDF parser implementation              | Infrastructure | Parses sample PDF correctly                            |
| D-3.05 | Supabase repository implementations    | Infrastructure | All 3 repositories (Transaction, Category, PdfImport) |
| D-3.06 | Database migrations (3 tables + RLS)  | Infrastructure | Applied to Supabase; RLS verified                      |
| D-3.07 | Upload PDF page                        | Frontend       | File upload → results display                          |
| D-3.08 | Transaction list page (paginated)    | Frontend       | Paginated, sorted, category shown                      |
| D-3.09 | Manual add transaction page            | Frontend       | Form → validation → redirect                          |
| D-3.10 | Category management page              | Frontend       | CRUD with system default protection                    |
| D-3.11 | All Phase 3 tests (≥16)              | Tests          | All green                                              |

---

## Success Criteria

| #     | Criterion                                                         | Metric                                |
|-------|-------------------------------------------------------------------|---------------------------------------|
| SC-3.1 | User can upload a PDF and see imported transactions	| E2E: upload → transaction list updated |
| SC-3.2 | Duplicate transactions are detected and not re-imported	| Import same PDF twice → second time 0 new	|
| SC-3.3 | Manual transaction creation works end-to-end		| Add form → appears in list			|
| SC-3.4 | Categories can be created, renamed, and deleted (with guards)	| System defaults protected			|
| SC-3.5 | All data scoped to current user				| User A never sees User B's data		|
| SC-3.6 | Application layer test coverage ≥ 70%			| coverlet report					|
| SC-3.7 | Domain test coverage ≥ 80%				| coverlet report					|
| SC-3.8 | All 16+ handler tests pass				| dotnet test all green				|

---

# Phase 4: Analytics & Dashboard (MVP)

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Features)
- **Duration**: Weeks 14-18
- **Phase Type**: Full-Stack (Features)
- **Goal**: Analytics queries, dashboard UI, charts — this completes the MVP ✅
- **Depends On**: Phase 0-3 (all prior phases)

---

## Critical Decisions

| ID    | Decision                                   | Rationale                                                      |
|-------|--------------------------------------------|----------------------------------------------------------------|
| CD-4.1| Chart.js for frontend charts	            | Lightweight, free, CDN available, widely documented			|
| CD-4.2| Aggregation queries in Application layer (not DB views)| Keeps logic testable; Supabase free tier limits DB-side views|
| CD-4.3| Dashboard is the new default authenticated landing page	| Users expect to see their summary on login					|
| CD-4.4| Date range filter defaults to current month	| Most common use case; configurable							|
| CD-4.5| All analytics queries use domain specifications	| Consistent with architecture; filtering in Domain Language		|

---

## Executive Summary

### In Scope

- Analytics queries: `GetSpendingByCategoryQuery`, `GetMonthlyTrendsQuery`, `GetYearlyComparisonQuery`
- `GetTransactionSummaryQuery` (total income, total expenses, net, count)
- Dashboard page (default authenticated landing page)
- Chart.js integration: pie chart (category breakdown), line chart (monthly trends), bar chart (comparison)
- Date range filter (defaults to current month)
- Transaction search & advanced filtering (by category, amount range, date range, description keyword)
- DTOs: `CategorySpendingDto`, `MonthlyTrendDto`, `YearlyComparisonDto`, `TransactionSummaryDto`
- ≥14 integration tests

### Deferred

| Item                           | Target Phase | Reason                              |
|--------------------------------|-------------|--------------------------------------|
| Detailed spending reports      | Phase 5      | Budget vs actual feature             |
| CSV/Excel data export          | Post-MVP    | Export feature                       |
| Scheduled reports              | Post-MVP    | Background job setup                  |
| Anomaly detection alerts       | Phase 5     | Budget overage alerts                |

---

## User Scenarios & Testing

### Scenario 4.1: View Analytics Dashboard
As a user
I want to see an analytics dashboard with spending charts and trends
So that I can understand my spending habits

Acceptance Criteria:

- Dashboard shows pie chart (category breakdown), line chart (monthly trends), bar chart (yearly comparison)
- Default date range: current month
- Data updates dynamically based on date range and category filters
- Responsive design (mobile + desktop)

### Scenario 4.2: Transaction Search and Filtering
As a user
I want to search and filter my transactions
So that I can find specific transactions or patterns

Acceptance Criteria:

- Search bar on transaction list page
- Filters for date range, category, amount range, description keyword
- Filter settings saved/restored across sessions
- Clear separation of search results and analytics data

---

## Functional Requirements

### FR-4.01: Analytics Queries

| Query                            | Phase | Input                              | Output                         |
|----------------------------------|-------|------------------------------------|--------------------------------|
| `GetSpendingByCategoryQuery`     | 4     | UserId, DateRange                  | `List<CategorySpendingDto>`    |
| `GetMonthlyTrendsQuery`          | 4     | UserId, year                       | `List<MonthlyTrendDto>`        |
| `GetYearlyComparisonQuery`       | 4     | UserId, year1, year2               | `List<YearlyComparisonDto>`    |
| `GetTransactionSummaryQuery`     | 4     | UserId, DateRange                  | `TransactionSummaryDto`        |

### FR-4.02: Dashboard Page

- Default route: `/Dashboard`
- Displays user summary, charts, recent transactions, budget status
- Chart.js integration for pie, line, and bar charts
- Date range filter defaults to current month, allows custom range
- Transaction search bar above the transaction list

---

## Test Specifications

| Analytics Queries Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-4.01  | GetSpendingByCategoryQuery_Valid             | Returns correct spending by category                       |
| T-4.02  | GetMonthlyTrendsQuery_Valid                  | Returns correct monthly trends                             |
| T-4.03  | GetYearlyComparisonQuery_Valid               | Returns correct yearly comparison                          |
| T-4.04  | GetTransactionSummaryQuery_Valid             | Returns correct transaction summary                        |
| T-4.05  | Dashboard Page_LoadsSuccessfully               | Dashboard loads with no errors                             |
| T-4.06  | Dashboard Page_ChartDataIsValid              | Charts render with valid data                              |

---

## Deliverables

| #   | Deliverable                             | Layer          | Acceptance                                              |
|-----|-----------------------------------------|----------------|---------------------------------------------------------|
| D-4.01 | Analytics queries                     | Application    | Tests T-4.01 to T-4.04 pass                           |
| D-4.02 | Dashboard page                        | Frontend       | Page loads, displays charts, filters work              |
| D-4.03 | Chart.js integration                   | Frontend       | Charts render correctly (pie, line, bar)               |
| D-4.04 | Transaction search & filtering         | Frontend       | Search and filters work as expected                    |
| D-4.05 | User settings (date range, categories) | Application    | User preferences are saved and applied                 |
| D-4.06 | All Phase 4 tests (≥14)              | Tests          | All green                                              |

---

## Success Criteria

| #     | Criterion                                                         | Metric                                |
|-------|-------------------------------------------------------------------|---------------------------------------|
| SC-4.1 | Dashboard shows correct spending data and trends	| E2E: dashboard displays correct charts |
| SC-4.2 | Transaction search and filtering work as expected	| Search and filters return expected results |
| SC-4.3 | All analytics queries pass	| dotnet test --filter Category=Analytics |
| SC-4.4 | Chart.js integration works	| Charts render correctly in dashboard page |
| SC-4.5 | Date range filter works across dashboard and transaction list	| Date range applies to all displayed data |
| SC-4.6 | MVP complete: all Phase 4 features work together	| User can seamlessly navigate and use dashboard features |

---

# Phase 5: Budget Management & Alerts

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Features)
- **Duration**: Weeks 19-21
- **Phase Type**: Full-Stack (Features)
- **Goal**: Budget CRUD, overage detection, visual alerts, budget vs. actual reporting
- **Depends On**: Phase 0-4 (all prior phases)

---

## Critical Decisions

| ID    | Decision                                   | Rationale                                                      |
|-------|--------------------------------------------|----------------------------------------------------------------|
| CD-5.1| Budget alerts via visual indicators        | Immediate feedback on budget status; no push notifications yet  |
| CD-5.2| Budget vs actual reporting in dashboard	  | Compare planned vs actual spending at a glance                |
| CD-5.3| Supabase scheduled functions for alerts    | Use built-in Supabase feature for simplicity                   |
| CD-5.4| Rollback protection for budgets            | Prevents loss of budget data on duplicate import               |

---

## Executive Summary

### In Scope

- Budget commands: `CreateBudgetCommand`, `UpdateBudgetCommand`, `DeleteBudgetCommand`
- Budget queries: `GetBudgetsQuery`, `GetBudgetStatusQuery` (with current spend calculation)
- `GetBudgetVsActualQuery` (per-category budget comparison)
- Budget management page (create/edit/delete per category per month)
- Budget status indicators on dashboard (green/yellow/red by percentage used)
- Budget alerts: visual warnings on dashboard when ≥80% used, overage state highlighted
- Unique constraint enforcement: one budget per user-category-month
- ≥12 integration tests

### Deferred

| Item                           | Target Phase | Reason                              |
|--------------------------------|-------------|--------------------------------------|
| Visual budget alerts (green/yellow/red indicators)     | Phase 6      | Polish phase                        |
| Automated budget reports (PDF/CSV exports)     | Post-MVP    | Reporting feature                   |
| Integration with external APIs (e.g., for exchange rates)     | Post-MVP    | External integrations                |

---

## User Scenarios & Testing

### Scenario 5.1: Create and Manage Budgets
As a user
I want to create, update, and delete budgets
So that I can manage my spending limits

Acceptance Criteria:

- Budget management page with form: category, month, limit
- Validate: one budget per category per month, limit > 0
- On save: budget appears in budget list, with edit/delete options
- Edit: change month, category, limit
- Delete: remove budget from list and database

### Scenario 5.2: View Budget Status and Alerts
As a user
I want to see my budget status at a glance
So that I can quickly assess my spending health

Acceptance Criteria:

- Dashboard shows budget status indicators (green/yellow/red)
- Green: under 60% used, Yellow: 60-80% used, Red: over 80% used, Overage: over 100%
- Visual alert on dashboard when budget limit exceeded
- Tooltip with budget details on hover over status indicator

---

## Functional Requirements

### FR-5.01: Budget Commands & Queries

| Command / Query               | Type    | Input                                | Output                           |
|-------------------------------|---------|--------------------------------------|----------------------------------|
| `CreateBudgetCommand`         | Command | UserId, CategoryId, Month, Limit    | `BudgetId`                       |
| `UpdateBudgetCommand`         | Command | BudgetId, NewLimit, UserId          | Success                          |
| `DeleteBudgetCommand`         | Command | BudgetId, UserId                    | Success                          |
| `GetBudgetsQuery`             | Query   | UserId, month?                     | `List<BudgetDto>`                |
| `GetBudgetStatusQuery`        | Query   | BudgetId, UserId                   | `BudgetStatusDto`                |
| `GetBudgetVsActualQuery`      | Query   | UserId, month                      | `List<BudgetVsActualDto>`        |

---

## Test Specifications

| Budget Management Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-5.01  | CreateBudgetCommand_ValidInput_CreatesBudget	| Budget is created with correct values                      |
| T-5.02  | CreateBudgetCommand_Duplicate_ThrowsException	| DomainException "already exists"                          |
| T-5.03  | UpdateBudgetCommand_ValidInput_UpdatesBudget	| Budget is updated with new values                          |
| T-5.04  | UpdateBudgetCommand_NonExisting_ThrowsException	| EntityNotFoundException                                    |
| T-5.05  | DeleteBudgetCommand_ValidInput_RemovesBudget		| GetById throws EntityNotFoundException                      |
| T-5.06  | DeleteBudgetCommand_NonExisting_ThrowsException	| EntityNotFoundException                                    |
| T-5.07  | GetBudgetsQuery_ReturnsUserBudgets            | Returns only budgets for the user                          |
| T-5.08  | GetBudgetStatusQuery_Valid                     | Returns correct budget status                              |
| T-5.09  | GetBudgetVsActualQuery_Valid                  | Returns correct budget vs actual spending comparison      |
| T-5.10  | Dashboard_BudgetStatusIndicators_AreCorrect   | Status indicators reflect correct budget status          |
| T-5.11  | Dashboard_BudgetAlerts_Working                 | Alerts trigger visually on dashboard when over budget     |

---

## Deliverables

| #   | Deliverable                             | Layer          | Acceptance                                              |
|-----|-----------------------------------------|----------------|---------------------------------------------------------|
| D-5.01 | Budget commands                       | Application    | Tests T-5.01 to T-5.04 pass                           |
| D-5.02 | Budget queries                        | Application    | Tests T-5.07 to T-5.09 pass                           |
| D-5.03 | Dashboard budget status indicators     | Frontend       | Status indicators show correct budget status            |
| D-5.04 | Budget alerts (visual warnings)       | Frontend       | Visual alerts trigger on dashboard when budget limit exceeded |
| D-5.05 | All Phase 5 tests (≥12)              | Tests          | All green                                              |

---

## Success Criteria

| #     | Criterion                                                   | Metric                                |
|-------|-------------------------------------------------------------|---------------------------------------|
| SC-5.1 | Users can create, update, and delete budgets	| E2E: budget CRUD operations work       |
| SC-5.2 | Dashboard shows correct budget status and alerts	| Budget status indicators are accurate  |
| SC-5.3 | All budget tests pass	| dotnet test --filter Category=Budget     |
| SC-5.4 | Application layer test coverage ≥ 70%			| coverlet report					|
| SC-5.5 | Domain test coverage ≥ 80%				| coverlet report					|

---

# Phase 6: UI Polish, Performance & Production Deployment

## Quick Reference

- **Status**: Draft
- **Layer Scope**: Frontend + Infrastructure (Polish)
- **Duration**: Weeks 22-24
- **Phase Type**: Polish
- **Goal**: UI refinements, performance optimization, Vercel deployment, security hardening

> ⚠️ **Polish Phase**: Only Frontend + Infrastructure layers in scope. No new domain entities or application commands.

---

## Critical Decisions

| ID    | Decision                                   | Rationale                                                      |
|-------|--------------------------------------------|----------------------------------------------------------------|
| CD-6.1| Tailwind CSS build pipeline                | Purge unused classes, minify output                           |
| CD-6.2| Alpine.js for interactive components      | Lightweight, integrates well with Tailwind                     |
| CD-6.3| Vercel for hosting                        | Easy deployment, CI/CD integration, free tier available        |
| CD-6.4| Sentry for error monitoring                | Track and fix errors in production                             |

---

## Executive Summary

### In Scope

- Tailwind CSS build pipeline: PostCSS/CLI, purge unused classes, minified output
- Alpine.js integration for interactive components (dropdowns, modals, filters)
- Responsive design audit and fixes (mobile-first)
- Loading states, error states, empty states for all pages
- Password reset flow (Supabase Auth built-in)
- Session management UI (active sessions, logout all)
- Performance: database indexes audit, query optimization, lazy loading, caching headers
- Vercel deployment: `vercel.json`, environment variables, auto-deploy on push to main
- CORS configuration for Supabase ↔ Vercel domain
- Error logging setup (Sentry or similar)
- Custom domain configuration (optional)
- Security: HTTPS enforcement, CSP headers, rate limiting review
- Accessibility audit (WCAG 2.1 AA baseline)
- ≥8 tests (performance benchmarks, deployment smoke tests)

### Deferred

| Item                                        | Target Phase | Reason                              |
|---------------------------------------------|-------------|--------------------------------------|
| New features or major changes                | Post-MVP    | Focus on polish and production readiness |

---

## User Scenarios & Testing

### Scenario 6.1: UI Refinements
As a user
I want the application to have a polished and responsive UI
So that I have a good user experience

Acceptance Criteria:

- Consistent padding, margins, font sizes across pages
- Responsive design: works on mobile, tablet, desktop
- No console errors or warnings
- Fast loading times (meets performance benchmarks)

### Scenario 6.2: Performance Optimization
As a user
I want the application to load and respond quickly
So that I can use it efficiently

Acceptance Criteria:

- Indexes and queries optimized for performance
- No unnecessary API calls or data fetching
- Images and assets are optimized
- Time to interactive (TTI) under 3 seconds on mobile 3G

### Scenario 6.3: Production Deployment
As a developer
I want to deploy the application to production
So that users can access the latest features and fixes

Acceptance Criteria:

- Vercel deployment is configured and successful
- Environment variables set correctly in Vercel
- Automatic deployment on push to main branch
- Manual deployment works (if needed)

---

## Functional Requirements

### FR-6.01: UI Polish

- Tailwind CSS build pipeline integrated
- Alpine.js components for interactivity
- Responsive design pass for all pages
- Accessibility audit and remediation (WCAG 2.1 AA)

### FR-6.02: Performance

- Query and database performance optimization
- Error logging and monitoring setup (Sentry)

### FR-6.03: Deployment

- Vercel configuration: `vercel.json`, environment variables
- CI/CD integration with GitHub for auto-deploy on main branch push

---

## Test Specifications

| Polish and Deployment Tests
| Test ID | Test Name                                       | Assert                                                    |
|---------|------------------------------------------------|-----------------------------------------------------------|
| T-6.01  | UI_ConsistentStyling                          | No visual regressions, consistent styles                   |
| T-6.02  | UI_ResponsiveDesign                           | Layouts work on mobile/tablet/desktop                     |
| T-6.03  | Performance_TTIUnder3Seconds                   | TTI under 3 seconds on mobile 3G                          |
| T-6.04  | Vercel_DeploysSuccessfully                    | Production deployment succeeds                             |
| T-6.05  | Sentry_ErrorMonitoringActive                  | Errors are logged in Sentry                              |
| T-6.06  | Tailwind_PurgeUnusedClasses                   | Unused CSS classes are purged, file size reduced         |
| T-6.07  | Alpine.js_InteractiveComponents                | Dropdowns, modals, filters work as expected              |

---

## Deliverables

| #   | Deliverable                             | Layer          | Acceptance                                              |
|-----|-----------------------------------------|----------------|---------------------------------------------------------|
| D-6.01 | Polished UI                           | Frontend       | Tests T-6.01 to T-6.03 pass                           |
| D-6.02 | Performance optimizations              | Infrastructure | Performance benchmarks met                             |
| D-6.03 | Sentry error monitoring                 | Infrastructure | Errors logged in Sentry                              |
| D-6.04 | Vercel production deployment            | Infrastructure | Successful deployment to production                    |

---

## Success Criteria

| #     | Criterion                                                   | Metric                                |
|-------|-------------------------------------------------------------|---------------------------------------|
| SC-6.1 | UI is polished, responsive, and consistent	| No visual regressions, TTI under 3s  |
| SC-6.2 | Performance benchmarks are met	| Query optimization, error logging active |
| SC-6.3 | Successful deployment to Vercel production	| Live on Vercel, auto-deploy works   |