# Phase 1: Authentication & Multi-Tenancy - DETAILED TASKS

**Status:** Ready for Implementation
**Source Plan:** specs/phase-1-auth-plan.md  
**Constitutional Compliance:** All tasks mapped to Clean Architecture, CQRS, DDD, Test-First, Spec-Driven principles
**Total Tasks:** 30 (organized in 3 phases)
**Estimated Duration:** 3-4 weeks

---

## PHASE I: User Entity & Database Foundation (10 tasks, ~8.5 hours)

### Task I-1: Create User Entity in Domain Layer
**Duration:** 1h | **Dependencies:** None | **Blocks:** I-3, I-6, I-7, I-8

**Description:**
Create the `User` entity in the Domain layer with all required properties and invariants. This is a pure domain class with no external dependencies.

**Acceptance Criteria:**
- [ ] File created: `src/Domain/User.cs`
- [ ] Properties: `Id` (Guid), `Email` (string), `CreatedAt` (DateTime), `UpdatedAt` (DateTime?)
- [ ] Invariants enforced via constructor validation:
  - Email must be non-empty and valid (regex: RFC 5322 basic)
  - Email must be unique (database constraint, not code)
  - Id cannot be changed after creation (protected setter)
- [ ] Inherits from `Entity<Guid>` base class
- [ ] No external dependencies (no Supabase, no Infrastructure references)
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Domain/User.cs` (new)

**Constitutional Compliance:**
- ✅ Clean Architecture: Domain layer is pure, no external deps
- ✅ DDD: Entity with invariants and state enforcement
- ✅ No Infrastructure references

---

### Task I-2: Create UserCreatedDomainEvent in Domain Layer
**Duration:** 45m | **Dependencies:** I-1 | **Blocks:** I-8, II-14, II-15

**Description:**
Create domain event published when a user is created. This enables event sourcing patterns and decoupled application behavior.

**Acceptance Criteria:**
- [ ] File created: `src/Domain/Events/UserCreatedDomainEvent.cs`
- [ ] Inherits from `IDomainEvent`
- [ ] Contains: `UserId` (Guid), `Email` (string), `OccurredOn` (DateTime)
- [ ] Can be published and subscribed to (for future event handlers)
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Domain/Events/UserCreatedDomainEvent.cs` (new)

**Constitutional Compliance:**
- ✅ DDD: Domain events enable event-driven patterns
- ✅ Spec-Driven: Event specified in acceptance criteria

---

### Task I-3: Write Unit Test T01-001 (User Entity)
**Duration:** 1h | **Dependencies:** I-1, I-2 | **Blocks:** I-10

**Description:**
Write test-first specification for User entity. This test captures the invariants and expected behavior.

**Acceptance Criteria:**
- [ ] File created: `tests/Domain.Tests/UserTests.cs`
- [ ] Test: User entity has Email property
- [ ] Test: Email invariant: non-empty
- [ ] Test: Email invariant: valid format (basic regex)
- [ ] Test: Id is immutable (cannot be set after creation)
- [ ] Test: CreatedAt is set automatically
- [ ] All tests RED initially (test-first principle)
- [ ] Run with `dotnet test tests/Domain.Tests` → All tests pass after I-1 complete

**Files Affected:**
- `tests/Domain.Tests/UserTests.cs` (new)

**Constitutional Compliance:**
- ✅ Test-First Development: Tests written before implementation
- ✅ Spec-Driven: Tests define required behavior

---

### Task I-4: Create Supabase Migration - CreateUsersTable
**Duration:** 45m | **Dependencies:** None | **Blocks:** I-5, I-7

**Description:**
Create SQL migration for users table in Supabase PostgreSQL. NO password column (delegated to Supabase Auth).

**Acceptance Criteria:**
- [ ] File created: `src/Infrastructure/Migrations/001_CreateUsersTable.sql`
- [ ] Table name: `users`
- [ ] Columns:
  - `id` (UUID PRIMARY KEY, DEFAULT gen_random_uuid())
  - `email` (VARCHAR(255) UNIQUE NOT NULL)
  - `created_at` (TIMESTAMP DEFAULT NOW())
  - `updated_at` (TIMESTAMP DEFAULT NOW())
- [ ] Constraints:
  - UNIQUE constraint on email
  - CHECK constraint: email <> ''
- [ ] Indexes:
  - `idx_users_email` on email (for login queries)
  - `idx_users_created_at` on created_at DESC (for sorting)
- [ ] NO password column (Supabase Auth handles it)
- [ ] SQL syntax valid (can be executed in Supabase console)

**Files Affected:**
- `src/Infrastructure/Migrations/001_CreateUsersTable.sql` (new)

**Constitutional Compliance:**
- ✅ Clean Architecture: Separation of concerns (auth in Supabase, data in app)
- ✅ Infrastructure layer responsibility: persistence schema

---

### Task I-5: Apply Supabase Migration & Verify Schema
**Duration:** 30m | **Dependencies:** I-4 | **Blocks:** I-7

**Description:**
Execute migration in Supabase and verify schema is correct.

**Acceptance Criteria:**
- [ ] Migration executed in Supabase SQL console
- [ ] Table `users` exists with correct columns
- [ ] Indexes created successfully
- [ ] UNIQUE constraint on email verified
- [ ] CHECK constraint on email verified
- [ ] Can insert test record: INSERT INTO users (email) VALUES ('test@example.com')
- [ ] Duplicate email rejected (unique constraint)
- [ ] Query execution: SELECT * FROM users returns correct result

**Files Affected:**
- (Database only, no code changes)

**Constitutional Compliance:**
- ✅ Infrastructure: Migration verified before implementation

---

### Task I-6: Verify IRepository<User> Interface Exists in Domain
**Duration:** 15m | **Dependencies:** I-1 | **Blocks:** I-7

**Description:**
Check that generic `IRepository<T>` interface exists in Domain (created in Phase 0). If not, create it.

**Acceptance Criteria:**
- [ ] File exists: `src/Domain/IRepository.cs`
- [ ] Generic interface `IRepository<T> where T : Entity<Guid>`
- [ ] Methods: AddAsync, UpdateAsync, DeleteAsync, GetByIdAsync, GetAllAsync, GetBySpecificationAsync
- [ ] Used as: `IRepository<User>`

**Files Affected:**
- `src/Domain/IRepository.cs` (should already exist from Phase 0)

**Constitutional Compliance:**
- ✅ DDD: Repository pattern abstracts persistence
- ✅ Clean Architecture: Infrastructure implements this interface

---

### Task I-7: Implement UserRepository in Infrastructure
**Duration:** 1.5h | **Dependencies:** I-4, I-5, I-6 | **Blocks:** I-8

**Description:**
Implement `IRepository<User>` in Infrastructure layer. This is the concrete Supabase implementation.

**Acceptance Criteria:**
- [ ] File created: `src/Infrastructure/Repositories/UserRepository.cs`
- [ ] Implements `IRepository<User>`
- [ ] Uses Supabase Postgrest client for CRUD operations:
  - `AddAsync`: INSERT new user
  - `UpdateAsync`: UPDATE user
  - `DeleteAsync`: DELETE user
  - `GetByIdAsync`: SELECT by id
  - `GetAllAsync`: SELECT all users
- [ ] Async/await all operations
- [ ] Proper error handling (handle Supabase exceptions)
- [ ] No business logic (persistence only)
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Infrastructure/Repositories/UserRepository.cs` (new)

**Constitutional Compliance:**
- ✅ Clean Architecture: Infrastructure implements Domain interface
- ✅ CQRS: Repository used by Command handlers

---

### Task I-8: Write Integration Test T01-002 (User Creation + Event)
**Duration:** 1h | **Dependencies:** I-1, I-2, I-7 | **Blocks:** I-10

**Description:**
Write integration test for user creation via Repository and domain event publishing.

**Acceptance Criteria:**
- [ ] File created or updated: `tests/Application.Tests/UserCommandTests.cs`
- [ ] Test: CreateUserCommand creates user in repository
- [ ] Test: UserCreatedDomainEvent published after creation
- [ ] Test: User properties saved correctly (email, createdAt)
- [ ] Mock `IRepository<User>` for unit testing (not database)
- [ ] Use `ApplicationTestFixture` for DI
- [ ] All tests RED initially
- [ ] Run with `dotnet test tests/Application.Tests` → All tests pass after I-7 complete

**Files Affected:**
- `tests/Application.Tests/UserCommandTests.cs` (new)

**Constitutional Compliance:**
- ✅ Test-First Development: Integration tests with mocked repositories
- ✅ Clean Architecture: Tests don't depend on infrastructure

---

### Task I-9: Create UserDto in Application Layer
**Duration:** 30m | **Dependencies:** I-1 | **Blocks:** II-14, II-15

**Description:**
Create DTO for returning user data from application layer.

**Acceptance Criteria:**
- [ ] File created: `src/Application/Users/UserDto.cs`
- [ ] Inherits from `BaseDto` (which implements `ITenantScoped`)
- [ ] Properties:
  - `Id` (Guid) - from BaseDto
  - `Email` (string)
  - `CreatedAt` (DateTime) - from BaseDto
  - `TenantId` (Guid) - from ITenantScoped (set to UserId for single-user)
- [ ] Read-only properties (no setters, or private setters)
- [ ] Maps from `User` entity

**Files Affected:**
- `src/Application/Users/UserDto.cs` (new)

**Constitutional Compliance:**
- ✅ CQRS: DTO for returning query results
- ✅ Clean Architecture: Application layer converts domain entities to DTOs

---

### Task I-10: Gate - Verify Phase I Tests Pass
**Duration:** 30m | **Dependencies:** I-3, I-8, I-7 | **Blocks:** Phase II start

**Description:**
Run all Phase I tests and verify they pass. Gate keeper before Phase II.

**Acceptance Criteria:**
- [ ] Run: `dotnet test tests/Domain.Tests`
- [ ] Result: Test T01-001 PASSES
- [ ] Run: `dotnet test tests/Application.Tests`
- [ ] Result: Test T01-002 PASSES
- [ ] Run: `dotnet build`
- [ ] Result: 0 warnings, 0 errors
- [ ] Code coverage: Domain layer ≥80%
- [ ] All domain files compile without upward dependencies

**Verification Commands:**
```bash
dotnet test tests/Domain.Tests --verbosity normal
dotnet test tests/Application.Tests --verbosity normal
dotnet build --no-restore
```

**Files Affected:**
- (No new files, verification only)

**Constitutional Compliance:**
- ✅ Test-First: All required tests pass before Phase II
- ✅ Code Review: Verify no upward dependencies

---

## PHASE II: Authentication Services (10 tasks, ~11 hours)

### Task II-1: Implement SupabaseAuthClient in Infrastructure
**Duration:** 1.5h | **Dependencies:** None | **Blocks:** II-4, II-5

**Description:**
Create Supabase Auth API wrapper for SignUp and SignIn operations.

**Acceptance Criteria:**
- [ ] File created: `src/Infrastructure/Authentication/SupabaseAuthClient.cs`
- [ ] Method: `SignUpAsync(email, password)` → calls POST /auth/v1/signup
  - Returns: `{ user_id: Guid, email: string }`
  - Handles errors: duplicate email, weak password, invalid email
- [ ] Method: `SignInAsync(email, password)` → calls POST /auth/v1/token
  - Returns: `{ access_token: string, user_id: Guid, email: string, expires_in: int }`
  - Handles errors: invalid credentials, user not found
- [ ] Uses HttpClient to call Supabase endpoints
- [ ] Reads SUPABASE_URL, SUPABASE_KEY from configuration
- [ ] Proper exception handling (map Supabase errors to domain exceptions)
- [ ] Async/await all operations
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Infrastructure/Authentication/SupabaseAuthClient.cs` (new)

**Constitutional Compliance:**
- ✅ Clean Architecture: Infrastructure implements auth client
- ✅ Dependency Injection: Registered in Infrastructure DI
- ✅ No Domain/Application references to Supabase

---

### Task II-2: Create RegisterUserValidator in Application
**Duration:** 45m | **Dependencies:** None | **Blocks:** II-4

**Description:**
Create FluentValidation validator for RegisterUserCommand.

**Acceptance Criteria:**
- [ ] File created: `src/Application/Users/Validators/RegisterUserValidator.cs`
- [ ] Inherits from `AbstractValidator<RegisterUserCommand>`
- [ ] Rule: Email must be valid (EmailAddress validator + custom regex for RFC 5322)
- [ ] Rule: Email must not be empty
- [ ] Rule: Password must be at least 8 characters
- [ ] Rule: Password must not be empty
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Application/Users/Validators/RegisterUserValidator.cs` (new)

**Constitutional Compliance:**
- ✅ CQRS: Validator for Command
- ✅ Clean Architecture: Application layer responsibility

---

### Task II-3: Create LoginUserValidator in Application
**Duration:** 45m | **Dependencies:** None | **Blocks:** II-5

**Description:**
Create FluentValidation validator for LoginUserCommand.

**Acceptance Criteria:**
- [ ] File created: `src/Application/Users/Validators/LoginUserValidator.cs`
- [ ] Inherits from `AbstractValidator<LoginUserCommand>`
- [ ] Rule: Email must be valid and not empty
- [ ] Rule: Password must not be empty
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Application/Users/Validators/LoginUserValidator.cs` (new)

**Constitutional Compliance:**
- ✅ CQRS: Validator for Command
- ✅ Clean Architecture: Application layer responsibility

---

### Task II-4: Create RegisterUserCommand & Handler in Application
**Duration:** 1h | **Dependencies:** II-2, I-1, I-9 | **Blocks:** II-8

**Description:**
Create RegisterUserCommand and its MediatR handler.

**Acceptance Criteria:**
- [ ] File created: `src/Application/Users/Commands/RegisterUserCommand.cs`
  - Record type: `RegisterUserCommand(string Email, string Password) : IRequest<RegisterUserResponse>`
  - `RegisterUserResponse(Guid UserId, string Email, string Message)`
- [ ] File created: `src/Application/Users/Commands/RegisterUserCommandHandler.cs`
  - Implements: `IRequestHandler<RegisterUserCommand, RegisterUserResponse>`
  - Injects: `IUserContext`, `SupabaseAuthClient`, `IRepository<User>`
  - Flow:
    1. Validate email/password via ValidationBehavior (will be added)
    2. Call `SupabaseAuthClient.SignUpAsync(email, password)`
    3. Create `User` entity with returned user_id
    4. Save user via `IRepository<User>.AddAsync(user)`
    5. Publish `UserCreatedDomainEvent`
    6. Return `RegisterUserResponse` with 201 Created
  - Handle errors: duplicate email (409), validation errors (400)
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Application/Users/Commands/RegisterUserCommand.cs` (new)
- `src/Application/Users/Commands/RegisterUserCommandHandler.cs` (new)

**Constitutional Compliance:**
- ✅ CQRS: Command handler is state-changing operation
- ✅ MediatR: Routed through MediatR pipeline
- ✅ DDD: Uses User entity and publishes domain events

---

### Task II-5: Create LoginUserCommand & Handler in Application
**Duration:** 1h | **Dependencies:** II-3, I-1, I-9 | **Blocks:** II-8

**Description:**
Create LoginUserCommand and its MediatR handler.

**Acceptance Criteria:**
- [ ] File created: `src/Application/Users/Commands/LoginUserCommand.cs`
  - Record type: `LoginUserCommand(string Email, string Password) : IRequest<LoginUserResponse>`
  - `LoginUserResponse(string JwtToken, Guid UserId, string Email)`
- [ ] File created: `src/Application/Users/Commands/LoginUserCommandHandler.cs`
  - Implements: `IRequestHandler<LoginUserCommand, LoginUserResponse>`
  - Injects: `SupabaseAuthClient`
  - Flow:
    1. Validate email/password via ValidationBehavior
    2. Call `SupabaseAuthClient.SignInAsync(email, password)`
    3. Return JWT token from Supabase response
    4. Return `LoginUserResponse` with 200 OK
  - Handle errors: invalid credentials (401), service unavailable (503)
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Application/Users/Commands/LoginUserCommand.cs` (new)
- `src/Application/Users/Commands/LoginUserCommandHandler.cs` (new)

**Constitutional Compliance:**
- ✅ CQRS: Command handler for login operation
- ✅ MediatR: Routed through MediatR pipeline
- ✅ No business logic: Delegation to Supabase Auth

---

### Task II-6: Implement SupabaseUserContext in Application
**Duration:** 1h | **Dependencies:** None | **Blocks:** II-8, III-24

**Description:**
Create real `SupabaseUserContext` that extracts UserId from JWT claims. Replaces MockUserContext.

**Acceptance Criteria:**
- [ ] File created: `src/Application/Authentication/SupabaseUserContext.cs`
- [ ] Implements: `IUserContext`
- [ ] Property: `UserId` (Guid)
- [ ] Injects: `IHttpContextAccessor`
- [ ] Extracts UserId from JWT claim `sub` (subject claim)
- [ ] Handles missing/invalid token gracefully (returns Guid.Empty or throws)
- [ ] Lifecycle: Scoped per HTTP request
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Application/Authentication/SupabaseUserContext.cs` (new)

**Constitutional Compliance:**
- ✅ Clean Architecture: Application layer, not Infrastructure
- ✅ Dependency Injection: Registered as scoped service

---

### Task II-7: Create AuthenticationBehavior in Application
**Duration:** 1h | **Dependencies:** None | **Blocks:** II-8

**Description:**
Create MediatR behavior to validate JWT token on every request.

**Acceptance Criteria:**
- [ ] File created: `src/Application/Behaviors/AuthenticationBehavior.cs`
- [ ] Implements: `IPipelineBehavior<TRequest, TResponse>`
- [ ] Validates: JWT token present in HttpContext
- [ ] Validates: Token not expired (checks `exp` claim)
- [ ] Throws: `UnauthorizedAccessException` if token missing (401)
- [ ] Throws: `UnauthorizedAccessException` if token expired (401)
- [ ] Logs: Security event if unauthorized attempt
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Application/Behaviors/AuthenticationBehavior.cs` (new)

**Constitutional Compliance:**
- ✅ CQRS: MediatR behavior for cross-cutting concern
- ✅ Clean Architecture: Application layer responsibility

---

### Task II-8: Update Application DependencyInjection.cs
**Duration:** 45m | **Dependencies:** II-4, II-5, II-6, II-7 | **Blocks:** II-9, III-24

**Description:**
Register all new authentication services in DependencyInjection.

**Acceptance Criteria:**
- [ ] File updated: `src/Application/DependencyInjection.cs`
- [ ] Register: `IUserContext` → `SupabaseUserContext` (Scoped)
- [ ] Register: `SupabaseAuthClient` (Scoped)
- [ ] Register validators: `RegisterUserValidator`, `LoginUserValidator`
- [ ] Add behavior: `AuthenticationBehavior` to MediatR pipeline
- [ ] All handlers auto-registered via `RegisterServicesFromAssemblies`
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Application/DependencyInjection.cs` (update existing)

**Constitutional Compliance:**
- ✅ CQRS: All services registered for MediatR
- ✅ Dependency Injection: Centralized configuration

---

### Task II-9: Write Integration Tests T01-003 to T01-005
**Duration:** 1.5h | **Dependencies:** II-4, II-5, II-6, II-7 | **Blocks:** II-10

**Description:**
Write integration tests for login, UserId extraction, and authentication.

**Acceptance Criteria:**
- [ ] File created or updated: `tests/Application.Tests/AuthenticationTests.cs`
- [ ] Test T01-003: LoginUserCommand validates credentials + returns JWT
  - Mock `SupabaseAuthClient.SignInAsync`
  - Verify response contains JWT token
  - Verify response contains UserId and Email
- [ ] Test T01-004: SupabaseUserContext extracts UserId from JWT claims
  - Create JWT with `sub` claim = known Guid
  - Inject into `SupabaseUserContext`
  - Verify `UserId` property returns correct Guid
- [ ] Test T01-005: AuthenticationBehavior blocks unauthenticated requests
  - Create request without JWT token
  - Send through MediatR pipeline
  - Verify `UnauthorizedAccessException` thrown
- [ ] All tests RED initially
- [ ] Run with `dotnet test tests/Application.Tests` → All tests pass

**Files Affected:**
- `tests/Application.Tests/AuthenticationTests.cs` (new)

**Constitutional Compliance:**
- ✅ Test-First: Tests written before implementation
- ✅ Integration Tests: Use mocked repositories, test behaviors

---

### Task II-10: Gate - Verify Phase II Tests Pass
**Duration:** 30m | **Dependencies:** II-9, II-8 | **Blocks:** Phase III start

**Description:**
Run all Phase II tests and verify they pass.

**Acceptance Criteria:**
- [ ] Run: `dotnet test tests/Application.Tests`
- [ ] Result: Tests T01-003, T01-004, T01-005 PASS
- [ ] Run: `dotnet build`
- [ ] Result: 0 warnings, 0 errors
- [ ] Code coverage: Application layer ≥70%
- [ ] No upward dependencies (Domain/Application don't reference Infrastructure)

**Verification Commands:**
```bash
dotnet test tests/Application.Tests --verbosity normal
dotnet build --no-restore
```

**Files Affected:**
- (No new files, verification only)

**Constitutional Compliance:**
- ✅ Test-First: All required tests pass before Phase III
- ✅ Code Review: Verify architecture compliance

---

## PHASE III: Frontend & Integration (10 tasks, ~9.5 hours)

### Task III-1: Create Register.cshtml & Register.cshtml.cs
**Duration:** 1h | **Dependencies:** II-4, II-8 | **Blocks:** III-7

**Description:**
Create Razor Pages for user registration.

**Acceptance Criteria:**
- [ ] File created: `src/Frontend/Pages/Auth/Register.cshtml`
- [ ] File created: `src/Frontend/Pages/Auth/Register.cshtml.cs` (PageModel)
- [ ] PageModel:
  - Injects: `IMediator`
  - Property: `Email` (string)
  - Property: `Password` (string)
  - Property: `Message` (string) - for success/error
  - Method: `OnPostAsync()` → calls `RegisterUserCommand` via MediatR
  - Handles success: redirect to Login page
  - Handles errors: display error message
- [ ] Razor template (HTML):
  - Form with Email and Password fields
  - Submit button
  - Display error messages
  - Use Tailwind CSS for styling
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Frontend/Pages/Auth/Register.cshtml` (new)
- `src/Frontend/Pages/Auth/Register.cshtml.cs` (new)

**Constitutional Compliance:**
- ✅ Razor Pages: Frontend presentation layer
- ✅ MediatR: Uses command handler via mediator

---

### Task III-2: Create Login.cshtml & Login.cshtml.cs
**Duration:** 1h | **Dependencies:** II-5, II-8 | **Blocks:** III-7

**Description:**
Create Razor Pages for user login.

**Acceptance Criteria:**
- [ ] File created: `src/Frontend/Pages/Auth/Login.cshtml`
- [ ] File created: `src/Frontend/Pages/Auth/Login.cshtml.cs` (PageModel)
- [ ] PageModel:
  - Injects: `IMediator`
  - Property: `Email` (string)
  - Property: `Password` (string)
  - Property: `Message` (string) - for success/error
  - Method: `OnPostAsync()` → calls `LoginUserCommand` via MediatR
  - Handles success: set HttpOnly cookie + redirect to Dashboard
  - Handles errors: display error message
- [ ] Razor template (HTML):
  - Form with Email and Password fields
  - Submit button
  - Display error messages
  - "Register here" link
  - Use Tailwind CSS for styling
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Frontend/Pages/Auth/Login.cshtml` (new)
- `src/Frontend/Pages/Auth/Login.cshtml.cs` (new)

**Constitutional Compliance:**
- ✅ Razor Pages: Frontend presentation layer
- ✅ CQRS: Uses command handler via mediator

---

### Task III-3: Create Profile.cshtml & Profile.cshtml.cs
**Duration:** 1h | **Dependencies:** II-6, II-8 | **Blocks:** III-7

**Description:**
Create Razor Pages for viewing user profile.

**Acceptance Criteria:**
- [ ] File created: `src/Frontend/Pages/Profile.cshtml`
- [ ] File created: `src/Frontend/Pages/Profile.cshtml.cs` (PageModel)
- [ ] PageModel:
  - Injects: `IUserContext`
  - Property: `UserEmail` (string)
  - Property: `CreatedAt` (DateTime)
  - Method: `OnGetAsync()` → extract from IUserContext
  - Requires authentication (redirect to login if not authenticated)
- [ ] Razor template (HTML):
  - Display user email
  - Display account creation date
  - Logout button
  - Use Tailwind CSS for styling
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Frontend/Pages/Profile.cshtml` (new)
- `src/Frontend/Pages/Profile.cshtml.cs` (new)

**Constitutional Compliance:**
- ✅ Razor Pages: Frontend presentation layer
- ✅ Authentication: Requires logged-in user

---

### Task III-4: Wire Authentication Middleware in Program.cs
**Duration:** 45m | **Dependencies:** II-8, III-1, III-2 | **Blocks:** III-5, III-7

**Description:**
Configure JWT authentication middleware in Frontend Program.cs.

**Acceptance Criteria:**
- [ ] File updated: `src/Frontend/Program.cs`
- [ ] Add JWT authentication:
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
  ```
- [ ] Add authorization: `builder.Services.AddAuthorization()`
- [ ] Add middleware: `app.UseAuthentication()` and `app.UseAuthorization()`
- [ ] Add ApplicationServices: `builder.Services.AddApplicationServices()`
- [ ] Compiles with `dotnet build`
- [ ] Frontend runs without errors: `dotnet run --project src/Frontend`

**Files Affected:**
- `src/Frontend/Program.cs` (update existing)

**Constitutional Compliance:**
- ✅ Clean Architecture: Frontend wired to Application services
- ✅ Authentication: JWT validation configured globally

---

### Task III-5: Implement SessionManagement (HttpOnly Cookies)
**Duration:** 1h | **Dependencies:** III-2, III-4 | **Blocks:** III-7

**Description:**
Implement session management with HttpOnly cookies for JWT storage.

**Acceptance Criteria:**
- [ ] File created: `src/Frontend/Services/SessionManager.cs` (utility class)
- [ ] Method: `SetAuthCookie(response, jwtToken, expiresIn)`
  - Sets HttpOnly cookie: `auth_token`
  - Sets SameSite: Strict (CSRF protection)
  - Sets Secure: true (HTTPS only)
  - Sets Expiry: from JWT expiry
  - Stores JWT securely
- [ ] Method: `ClearAuthCookie(response)`
  - Removes `auth_token` cookie
- [ ] Update Login PageModel:
  - After successful login, call `SessionManager.SetAuthCookie()`
- [ ] Update Profile PageModel:
  - Add logout button handler that calls `SessionManager.ClearAuthCookie()`
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Frontend/Services/SessionManager.cs` (new)
- `src/Frontend/Pages/Auth/Login.cshtml.cs` (update)
- `src/Frontend/Pages/Profile.cshtml.cs` (update)

**Constitutional Compliance:**
- ✅ Clean Architecture: Session management is Frontend responsibility
- ✅ Security: HttpOnly cookies prevent XSS attacks

---

### Task III-6: Update Layout.cshtml with Login/Logout Links
**Duration:** 45m | **Dependencies:** III-3, III-5 | **Blocks:** III-7

**Description:**
Update main layout to show authentication status and logout link.

**Acceptance Criteria:**
- [ ] File updated: `src/Frontend/Pages/Shared/Layout.cshtml`
- [ ] Add navbar with:
  - Logo/Home link
  - Conditional rendering:
    - If authenticated: Show "Profile" link + "Logout" button
    - If not authenticated: Show "Register" link + "Login" link
- [ ] Use C# condition: `User.Identity.IsAuthenticated`
- [ ] Use Tailwind CSS for styling
- [ ] Logout button:
  - POST form to `/auth/logout` endpoint (create in next task)
  - Clears session cookie
  - Redirects to login
- [ ] Compiles with `dotnet build`

**Files Affected:**
- `src/Frontend/Pages/Shared/Layout.cshtml` (update)

**Constitutional Compliance:**
- ✅ Frontend: Presentation logic
- ✅ Authentication: Conditional UI based on User.Identity

---

### Task III-7: Write Integration Tests T01-006 to T01-008
**Duration:** 1.5h | **Dependencies:** I-7, I-4, III-4 | **Blocks:** III-8

**Description:**
Write integration tests for repository, migration, and multi-tenancy.

**Acceptance Criteria:**
- [ ] File created or updated: `tests/Integration.Tests/UserRepositoryTests.cs`
- [ ] Test T01-006: User repository CRUD operations work
  - Test AddAsync: Insert user, verify in database
  - Test GetByIdAsync: Retrieve by Id
  - Test UpdateAsync: Modify user, verify changes
  - Test DeleteAsync: Remove user
  - Use test database or Supabase test instance
- [ ] File created or updated: `tests/Integration.Tests/MigrationTests.cs`
- [ ] Test T01-007: Supabase migration creates users table with correct schema
  - Execute migration
  - Verify table exists
  - Verify columns: id, email, created_at, updated_at
  - Verify constraints: email UNIQUE, email NOT NULL
  - Verify indexes: idx_users_email, idx_users_created_at
- [ ] File created or updated: `tests/Application.Tests/MultiTenancyTests.cs`
- [ ] Test T01-008: Multi-tenant query filtering respects UserId + TenantId enforcement
  - Create User A + User B in test database
  - User A queries categories (should filter by UserId=A)
  - User B queries categories (should filter by UserId=B)
  - Verify User A cannot see User B's data (ScopedQueryBehavior blocks it)
  - Verify cross-tenant access throws UnauthorizedAccessException
- [ ] All tests RED initially
- [ ] Run with `dotnet test` → All tests pass

**Files Affected:**
- `tests/Integration.Tests/UserRepositoryTests.cs` (new)
- `tests/Integration.Tests/MigrationTests.cs` (new)
- `tests/Application.Tests/MultiTenancyTests.cs` (new)

**Constitutional Compliance:**
- ✅ Test-First: Integration tests verify end-to-end behavior
- ✅ Multi-Tenancy: ScopedQueryBehavior enforces tenant isolation

---

### Task III-8: Verify End-to-End Authentication Flow
**Duration:** 1h | **Dependencies:** III-7, III-6 | **Blocks:** III-9

**Description:**
Manual testing of complete authentication flow from user registration to dashboard access.

**Acceptance Criteria:**
- [ ] Start Frontend: `dotnet run --project src/Frontend`
- [ ] Open browser: `https://localhost:7001`
- [ ] Navigate to Register page
- [ ] Register new user: email + password (8+ chars)
- [ ] Verify: Redirected to Login page with success message
- [ ] Navigate to Login page
- [ ] Login with registered credentials
- [ ] Verify: Redirected to Dashboard (or home page)
- [ ] Verify: Auth cookie set (check browser DevTools)
- [ ] Navigate to Profile page
- [ ] Verify: Profile shows user email + creation date
- [ ] Click Logout button
- [ ] Verify: Redirected to Login page
- [ ] Verify: Auth cookie cleared
- [ ] Attempt to access Profile without logging in
- [ ] Verify: Redirected to Login page

**Files Affected:**
- (No new files, manual testing only)

**Constitutional Compliance:**
- ✅ Integration: Full flow across all layers
- ✅ Security: Authentication required for protected pages

---

### Task III-9: Update ARCHITECTURE.md with Authentication Patterns
**Duration:** 1.5h | **Dependencies:** III-8, II-10 | **Blocks:** III-10

**Description:**
Document authentication architecture and patterns in ARCHITECTURE.md.

**Acceptance Criteria:**
- [ ] File updated: `ARCHITECTURE.md` (root)
- [ ] Add section: "Authentication & Authorization"
- [ ] Document:
  - Supabase Auth integration (SignUp, SignIn flows)
  - JWT token lifecycle (issuance, validation, expiry)
  - SupabaseUserContext implementation
  - AuthenticationBehavior pipeline
  - Multi-tenancy enforcement via ScopedQueryBehavior
  - HttpOnly cookie storage
  - Security considerations (password hashing, token validation)
- [ ] Include diagrams:
  - Registration flow
  - Login flow
  - Tenant isolation flow
- [ ] Include code examples:
  - RegisterUserCommand/Handler
  - LoginUserCommand/Handler
  - SupabaseUserContext
- [ ] Valid markdown, compiles

**Files Affected:**
- `ARCHITECTURE.md` (update existing)

**Constitutional Compliance:**
- ✅ Documentation: Patterns documented for future maintainers
- ✅ Spec-Driven: Documentation matches implementation

---

### Task III-10: Final Validation - All Tests Passing, Coverage ≥80%
**Duration:** 1h | **Dependencies:** III-9, III-8 | **Blocks:** Phase completion

**Description:**
Final gate: verify all Phase 1 tests pass and code coverage targets met.

**Acceptance Criteria:**
- [ ] Run: `dotnet test` (all projects)
- [ ] Result: All 8 tests PASS (T01-001 through T01-008)
- [ ] Run: `dotnet build`
- [ ] Result: 0 warnings, 0 errors
- [ ] Code coverage:
  - Domain layer: ≥80%
  - Application layer: ≥70%
- [ ] Authentication workflow verified (manual test from III-8)
- [ ] No upward dependencies (Domain/Application don't reference Infrastructure/Frontend)
- [ ] All deliverables checked off:
  - ✅ Domain: User entity, UserCreatedDomainEvent
  - ✅ Application: Commands, handlers, validators, behavior, SupabaseUserContext
  - ✅ Infrastructure: SupabaseAuthClient, UserRepository, migration
  - ✅ Frontend: Register, Login, Profile pages
  - ✅ Tests: T01-001 through T01-008
  - ✅ Documentation: ARCHITECTURE.md updated

**Verification Commands:**
```bash
dotnet test --collect:"XPlat Code Coverage" --logger "trx"
dotnet build --no-restore
git log --oneline -5  # Verify commits
```

**Files Affected:**
- (No new files, verification only)

**Constitutional Compliance:**
- ✅ All 5 principles verified:
  - ✅ Clean Architecture: No upward dependencies
  - ✅ CQRS + MediatR: All use cases via mediator
  - ✅ DDD: Entities with invariants, domain events
  - ✅ Test-First: All required tests pass
  - ✅ Spec-Driven: Tests drive implementation

---

## Summary

**Total Tasks:** 30
**Total Duration:** 28-30 hours (3-4 weeks)
**Test Count:** 8 (T01-001 to T01-008)
**Code Coverage Target:** Domain ≥80%, Application ≥70%
**Success Gate:** All 8 tests passing, 0 build warnings, no upward dependencies

**Next Phase:** Phase 2 - Core Data Model & Domain Entities
