# SauronSheet: Execution Checklist by Phase

**Purpose**: Tactical, actionable checklist for each phase. Print & tick off as you progress.  
**Constitution Alignment**: Every item follows Test-First + Spec-Driven principles  
**Update Frequency**: After each completed work session

---

## Phase 0: Foundation & Infrastructure Setup (2-3 weeks)

### Pre-Start Checklist
- [ ] Clone repo and checkout `main` branch
- [ ] .NET 10 SDK installed (`dotnet --version` shows 10.x.x)
- [ ] Supabase account created and project initialized
- [ ] GitHub Actions secrets configured (if needed)
- [ ] Local postgres or Supabase connection tested

### Solution Structure
- [ ] Create `src/` directory with 4-layer structure:
  - [ ] `src/Domain/` (no dependencies except System)
  - [ ] `src/Application/` (depends only on Domain)
  - [ ] `src/Infrastructure/` (depends on Domain, Application)
  - [ ] `src/Frontend/` (Razor Pages, depends on Application)
- [ ] Create `tests/` directory:
  - [ ] `tests/Domain.Tests/`
  - [ ] `tests/Application.Tests/`
  - [ ] `tests/Infrastructure.Tests/`
- [ ] Create `.github/workflows/` for CI/CD

### Core Project Setup
- [ ] `dotnet new sln -n SauronSheet`
- [ ] Domain project: `dotnet new classlib -n Domain` → `src/Domain/`
- [ ] Application: `dotnet new classlib -n Application` → `src/Application/`
- [ ] Infrastructure: `dotnet new classlib -n Infrastructure` → `src/Infrastructure/`
- [ ] Frontend: `dotnet new web -n Frontend` → `src/Frontend/` (Razor Pages template)
- [ ] Add projects to solution: `dotnet sln add ...`
- [ ] Set nullable reference types in all .csproj: `<Nullable>enable</Nullable>`
- [ ] Verify build: `dotnet build` (0 warnings)

### Domain Layer Foundation
- [ ] Create `Domain/Common/Entity.cs` abstract base
  - [ ] Property: `Id` (generic)
  - [ ] Method: `GetDomainEvents()` for event sourcing (future)
- [ ] Create `Domain/Common/ValueObject.cs` abstract base
  - [ ] Abstract method: `GetEqualityComponents()` for value equality
- [ ] Create `Domain/Common/DomainException.cs` custom exception (base)
- [ ] Create `Domain/Exceptions/EntityNotFoundException.cs` (subclass)
- [ ] Create `Domain/Exceptions/ValueObjectValidationException.cs` (subclass)
- [ ] Create `Domain/Common/IRepository.cs` interface (generic)
- [ ] Create `Domain/Specifications/ISpecification<T>` base class
  - [ ] Property: `MaxResults = 1000` (default)
- [ ] First domain unit test: `tests/Domain.Tests/Common/ValueObjectTests.cs`
  - [ ] Test: Two value objects with same properties are equal

### Application Layer Foundation
- [ ] Install NuGet: `MediatR`, `MediatR.Extensions.Microsoft.DependencyInjection`
- [ ] Create `Application/Common/IUserContext.cs` interface
  - [ ] Property: `UserId` (current user's ID)
  - [ ] Method: `IsAuthenticated()` → bool
  - [ ] Method: `IsAdmin()` → bool
- [ ] Create `Application/Common/Behaviors/ValidationBehavior.cs`
  - [ ] Install: `FluentValidation`
- [ ] Create `Application/Common/Behaviors/LoggingBehavior.cs`
- [ ] Create `Application/Common/Behaviors/ScopedQueryBehavior.cs`
  - [ ] Validates every Query has UserId == Current.UserId
  - [ ] Throws exception if cross-tenant data detected
- [ ] Create `Application/Common/Dto/BaseDto.cs`
- [ ] Create `Application/Common/Handlers/ICommandHandler.cs`, `IQueryHandler.cs`
- [ ] Create example command: `Application/Common/Examples/CreateCategoryCommand.cs` + handler
- [ ] Create example query: `Application/Common/Examples/GetCategoriesQuery.cs` + handler
- [ ] Create `Application/Tests/Helpers/MockRepositoryFactory.cs`
  - [ ] Factory: Easily create Moq<IRepository> instances
  - [ ] Provide sample test data builders
- [ ] Configure MediatR in `Frontend/Program.cs`:
  - [ ] Register assemblies
  - [ ] Add behaviors (Validation, Logging, ScopedQuery)
  - [ ] Register IUserContext from DI
- [ ] First application test: `tests/Application.Tests/Common/MediatRTests.cs`
  - [ ] Test: MediatR resolves handler correctly
  - [ ] Test: Example CreateCategoryCommand handler executes
  - [ ] Test: Example GetCategoriesQuery handler with pagination
  - [ ] Test: ScopedQueryBehavior rejects cross-tenant query results

### Infrastructure Layer Foundation
- [ ] Create `Infrastructure/Persistence/SupabaseContext.cs`
  - [ ] Connection string from environment
- [ ] Create `Infrastructure/Persistence/Migrations/` folder
- [ ] Create first migration script: `001_InitialSchema.sql`
- [ ] Test connection: Simple SQL query in test
- [ ] Create `Infrastructure/DependencyInjection.cs` for DI registration
- [ ] First infrastructure test: `tests/Infrastructure.Tests/Persistence/ConnectionTests.cs`
  - [ ] Test: Can connect to Supabase and execute query

### Frontend Layer Foundation
- [ ] Install Tailwind CSS via npm (or use CDN for quick start)
- [ ] Create `Frontend/Pages/Shared/Layout.cshtml` base layout
- [ ] Create `Frontend/Pages/Index.cshtml` landing page
- [ ] Configure `Frontend/Program.cs` with all services (MediatR, Supabase client, auth)
- [ ] Test frontend runs: `dotnet run --project Frontend/`

### CI/CD Setup
- [ ] Create `.github/workflows/build-test-deploy.yml`
  - [ ] Step 1: Checkout repo
  - [ ] Step 2: Setup .NET 10
  - [ ] Step 3: Restore dependencies
  - [ ] Step 4: Build solution
  - [ ] Step 5: Run all tests
  - [ ] Step 6: Report test results
- [ ] Trigger workflow on push to `main`
- [ ] Test workflow: Make small commit, verify pipeline runs green

### Documentation
- [ ] Create `docs/ARCHITECTURE.md`
  - [ ] Diagram: 4-layer dependency flow (ASCII or Mermaid)
  - [ ] Explanation: Each layer's purpose
- [ ] Create `docs/PROJECT_STRUCTURE.md`
  - [ ] Tree of all folders and their meaning
- [ ] Create `docs/SETUP.md`
  - [ ] Local development setup steps
  - [ ] How to run tests

### Phase 0 Exit Criteria
- [ ] `dotnet build` succeeds with 0 warnings
- [ ] `dotnet test` passes all 11 tests (T00-001 through T00-011)
- [ ] `dotnet run --project Frontend/` starts local server
- [ ] GitHub Actions pipeline runs and shows green
- [ ] All 4 layers have test files + passing tests
- [ ] Exception hierarchy tested (DomainException throws/catches)
- [ ] ISpecification<T> enforces 1000-row pagination default
- [ ] ScopedQueryBehavior blocks cross-tenant data attempts
- [ ] IUserContext can be injected from DI container
- [ ] Example CQRS command + query work end-to-end
- [ ] Repository mocking examples documented in test helpers
- [ ] Commit: "feat: phase 0 foundation setup complete"

---

## Phase 1: Authentication & Multi-Tenancy (3-4 weeks)

### Pre-Start Checklist
- [ ] Phase 0 complete and committed
- [ ] Supabase Auth configured in project settings
- [ ] Create Supabase migration for `users` table
- [ ] Install NuGet: `Supabase`, Auth library

### Domain Layer: User Entity
- [ ] Create `Domain/Entities/User.cs` (AggregateRoot)
  - [ ] Properties: `Id` (UserId ValueObject), `Email`, `CreatedAt`, `LastLoginAt`
  - [ ] Constructor: Validates email format
  - [ ] Method: `IsSystemAdmin()` → bool (for later use)
- [ ] Create `Domain/ValueObjects/UserId.cs`
  - [ ] Constructor: Validates V4 UUID format
- [ ] Create `Domain/Specifications/UserSpecifications.cs`
  - [ ] Spec: `IsSystemAdmin()` for authorization
- [ ] Test: `tests/Domain.Tests/Entities/UserTests.cs`
  - [ ] Test: User with invalid email raises DomainException
  - [ ] Test: Two users with same email are different (UserId differs)

### Application Layer: Auth Commands
- [ ] Create `Application/Features/Auth/Commands/RegisterUserCommand.cs`
  - [ ] Input: Email, Password
  - [ ] Output: UserId, Email
- [ ] Create `Application/Features/Auth/Commands/RegisterUserCommandHandler.cs`
  - [ ] Call Supabase Auth API to create user
  - [ ] Auto-provision User entity in database
  - [ ] Return UserId
- [ ] Create `Application/Features/Auth/Commands/LoginUserCommand.cs`
  - [ ] Input: Email, Password
  - [ ] Output: JWT Token, ExpiresAt
- [ ] Create `Application/Features/Auth/Commands/LoginUserCommandHandler.cs`
  - [ ] Call Supabase Auth API to authenticate
  - [ ] Return JWT (don't store server-side, used for client requests)
- [ ] Create `Application/Features/Auth/Commands/LogoutUserCommand.cs`
- [ ] Create validation for all commands using FluentValidation

### Infrastructure Layer: User Context & Auth Service
- [ ] Create `Infrastructure/Auth/SupabaseAuthService.cs`
  - [ ] Method: `RegisterAsync(email, password)` → UserId
  - [ ] Method: `LoginAsync(email, password)` → JWT string
  - [ ] Method: `LogoutAsync(token)` → void
  - [ ] Method: `RefreshTokenAsync(refreshToken)` → JWT
  - [ ] Handle Supabase Auth exceptions
- [ ] Create `Application/Common/IUserContext.cs` interface
  - [ ] Property: `UserId` (current user's ID)
  - [ ] Property: `Email` (current user's email)
  - [ ] Method: `IsAuthenticated()` → bool
- [ ] Create `Infrastructure/Identity/UserContext.cs` implementation
  - [ ] Extract UserId from HttpContext.User claims
  - [ ] Validate JWT expiry (throw if expired)
- [ ] Register UserContext in DI (scoped lifetime)
- [ ] Create middleware `Frontend/Middleware/AuthenticationMiddleware.cs`
  - [ ] Extract JWT from secure cookie
  - [ ] Validate JWT signature & expiry
  - [ ] Populate HttpContext.User from token claims

### Database Migrations
- [ ] Create migration: `002_Users_Table.sql`
  - [ ] Table: `users` (id UUID, email TEXT, created_at, updated_at, auth_id)
  - [ ] Index: `users(email)` for uniqueness

### Frontend Layer: Login/Register Pages
- [ ] Create `Frontend/Pages/Auth/Register.cshtml` + `.cshtml.cs`
  - [ ] Form: Email, Password, Confirm Password
  - [ ] Handler: Calls RegisterUserCommand via MediatR
  - [ ] Success: Redirect to login
  - [ ] Error: Show message (email already exists, password weak, etc.)
- [ ] Create `Frontend/Pages/Auth/Login.cshtml` + `.cshtml.cs`
  - [ ] Form: Email, Password
  - [ ] Handler: Calls LoginUserCommand via MediatR
  - [ ] Success: Store JWT in secure cookie, redirect to dashboard
  - [ ] Error: Show message (invalid credentials)
- [ ] Create `Frontend/Pages/Auth/Logout.cshtml.cs`
  - [ ] Handler: Clear JWT cookie, redirect to login
- [ ] Update `Frontend/Pages/Shared/Layout.cshtml`
  - [ ] Display user email when authenticated
  - [ ] Display login link when not authenticated
- [ ] Create `Frontend/Pages/Account/Profile.cshtml` (stub for future)

### Integration Tests
- [ ] Test: `tests/Application.Tests/Features/Auth/RegisterTests.cs`
  - [ ] Test: RegisterUserCommand with valid email creates user
  - [ ] Test: RegisterUserCommand with duplicate email fails
  - [ ] Test: Registered user can be queried from database
- [ ] Test: `tests/Application.Tests/Features/Auth/LoginTests.cs`
  - [ ] Test: LoginUserCommand with correct password returns JWT
  - [ ] Test: LoginUserCommand with wrong password fails
  - [ ] Test: JWT token can be validated and decoded
- [ ] Test: `tests/Infrastructure.Tests/Identity/UserContextTests.cs`
  - [ ] Test: UserContext extracts UserId from HttpContext.User
  - [ ] Test: UserContext throws if token expired

### Security Checklist
- [ ] Passwords never logged or displayed
- [ ] JWT stored in HttpOnly cookie (not accessible to JavaScript)
- [ ] Secure flag set (HTTPS only in production)
- [ ] SameSite=Strict cookie flag to prevent CSRF
- [ ] CORS configured only for expected origins
- [ ] Password validation: Min 8 chars, includes uppercase + number + special char

### Phase 1 Exit Criteria
- [ ] User can register: Form → Database entry
- [ ] User can login: Email + password → JWT cookie
- [ ] User can logout: Clear cookie, redirect
- [ ] Unauthenticated users redirected to login page
- [ ] All auth tests pass (8+ integration tests)
- [ ] Security review: JWT, cookies, CORS checked
- [ ] Commit: "feat(phase-1): authentication & multi-tenancy complete"

---

## Phase 2: Core Data Model & Domain Entities (2-3 weeks)

### Pre-Start Checklist
- [ ] Phase 1 complete and committed
- [ ] User entity available for foreign key references
- [ ] All tests from Phase 0-1 still passing

### Domain Layer: Value Objects
- [ ] Create `Domain/ValueObjects/Money.cs`
  - [ ] Properties: Amount (decimal), Currency (string, default "USD")
  - [ ] Constructors: Validate Amount >= 0, decimals = 2
  - [ ] Methods: `Plus()`, `Minus()`, `CompareTo()`, `Equals()`
- [ ] Create `Domain/ValueObjects/TransactionId.cs` (UUID ValueObject)
- [ ] Create `Domain/ValueObjects/CategoryId.cs` (UUID ValueObject)
- [ ] Unit tests for all value objects:
  - [ ] Test: Money equality based on amount + currency
  - [ ] Test: Money addition: $10 + $20 = $30
  - [ ] Test: Money with negative amount raises exception
  - [ ] Test: TransactionId format validation (UUID)

### Domain Layer: Entities
- [ ] Create `Domain/Entities/Category.cs` (AggregateRoot)
  - [ ] Properties: `Id` (CategoryId), `UserId`, `Name`, `Color`, `IsSystemDefault`, `CreatedAt`
  - [ ] Invariants: Name unique per user, Name max 50 chars, system defaults immutable
  - [ ] Methods: `CanDelete()` → bool (returns false if IsSystemDefault or has active transactions)
- [ ] Create `Domain/Entities/Transaction.cs` (AggregateRoot)
  - [ ] Properties: `Id` (TransactionId), `UserId`, `Amount` (Money), `Date`, `CategoryId`, `Description`
  - [ ] Invariants: Amount > 0, Date ≤ today, required CategoryId
  - [ ] Immutable after creation (only allow update for soft validation fields)
  - [ ] Methods: `IsOutlier(stdDev)` → bool (for analytics)
- [ ] Create `Domain/Entities/Budget.cs` (AggregateRoot)
  - [ ] Properties: `Id` (BudgetId), `UserId`, `CategoryId`, `MonthlyLimit` (Money), `Month` (int YYYYMM)
  - [ ] Invariants: Limit > 0, unique per (UserId, CategoryId, Month)
  - [ ] Methods: `IsOverBudget(currentSpend)` → bool, `RemainingAmount(currentSpend)` → Money
- [ ] Create `Domain/Services/CategoryService.cs`
  - [ ] Method: `GetSystemDefaultCategories()` → List (Groceries, Transport, Utilities, Other)

### Domain Layer: Specifications
- [ ] Create `Domain/Specifications/TransactionSpecifications.cs`
  - [ ] Spec: `ByDateRange(startDate, endDate)`
  - [ ] Spec: `ByCategory(categoryId)`
  - [ ] Spec: `ByAmount(minAmount, maxAmount)`
  - [ ] Spec: `ByUser(userId)`
- [ ] Create `Domain/Specifications/BudgetSpecifications.cs`
  - [ ] Spec: `ForCurrentMonth(year, month)` & `ForUser(userId)`

### Domain Layer: Repository Interfaces
- [ ] Create `Domain/Repositories/ITransactionRepository.cs`
  - [ ] Method: `AddAsync(transaction)` → Task
  - [ ] Method: `GetByIdAsync(id)` → Task<Transaction>
  - [ ] Method: `FindAsync(spec)` → Task<List<Transaction>>
  - [ ] Method: `UpdateAsync(transaction)` → Task
  - [ ] Method: `DeleteAsync(id)` → Task
- [ ] Create `Domain/Repositories/ICategoryRepository.cs` (same pattern)
- [ ] Create `Domain/Repositories/IBudgetRepository.cs` (same pattern)

### Database Migrations
- [ ] Migration: `003_Categories_Table.sql`
  - [ ] Columns: id, user_id, name, color, is_system_default, created_at
  - [ ] Index: `categories(user_id, name)` UNIQUE
- [ ] Migration: `004_Transactions_Table.sql`
  - [ ] Columns: id, user_id, amount, currency, date, category_id, description, created_at
  - [ ] Index: `transactions(user_id, date)`
  - [ ] FK: category_id → categories(id)
- [ ] Migration: `005_Budgets_Table.sql`
  - [ ] Columns: id, user_id, category_id, monthly_limit, month (YYYYMM), created_at
  - [ ] Unique: `(user_id, category_id, month)`

### Unit Tests (Domain Layer)
- [ ] `tests/Domain.Tests/Entities/CategoryTests.cs`
  - [ ] Test: Category with duplicate name per user raises exception
  - [ ] Test: Cannot delete system default category
- [ ] `tests/Domain.Tests/Entities/TransactionTests.cs`
  - [ ] Test: Transaction with negative amount raises exception
  - [ ] Test: Transaction date cannot be in future
  - [ ] Test: IsOutlier() detects anomalies above 2 std devs
- [ ] `tests/Domain.Tests/Entities/BudgetTests.cs`
  - [ ] Test: Budget.IsOverBudget() returns true when spend > limit
  - [ ] Test: Budget with negative limit raises exception
  - [ ] Test: RemainingAmount() calculates correctly
- [ ] `tests/Domain.Tests/Specifications/TransactionSpecificationTests.cs`
  - [ ] Test: ByDateRange spec filters correctly
  - [ ] Test: ByCategory spec filters correctly
  - [ ] Test: Specifications can be combined

### Documentation
- [ ] Create `docs/DDD_ENTITIES.md`
  - [ ] Diagram: Entity relationships
  - [ ] Bounded contexts: Transactions, Budgets, Reporting
  - [ ] Aggregate roots: Transaction, Category, Budget
- [ ] Create `docs/VALUE_OBJECTS.md`
  - [ ] Explain why Money is a VO, not a property

### Phase 2 Exit Criteria
- [ ] All domain entities compile with nullable enabled
- [ ] 20+ unit tests pass (100% coverage on core entities)
- [ ] Domain invariants prevent invalid states (checked in tests)
- [ ] Repository interfaces defined and documented
- [ ] Database schema created (migrations runnable)
- [ ] Commit: "feat(phase-2): domain entities & value objects complete"

---

## Phase 3: Transaction Import Pipeline (3-4 weeks)

### Pre-Start Checklist
- [ ] Phase 0-2 complete
- [ ] Domain entities and repositories ready
- [ ] Install NuGet: `iTextSharp` or `PdfSharp` for PDF parsing
- [ ] Sample bank PDF obtained for testing

### Domain Layer: PDF Parsing Service Interface
- [ ] Create `Domain/Services/IPdfParser.cs` interface
  - [ ] Method: `ParseAsync(fileStream)` → Task<List<ParsedTransaction>>
  - [ ] ParsedTransaction DTO: Amount, Date, Description, MerchantName
- [ ] Create `Domain/Exceptions/PdfParsingException.cs`

### Infrastructure Layer: PDF Parser Implementation
- [ ] Create `Infrastructure/PDF/BankPdfParser.cs` implementing `IPdfParser`
  - [ ] Extract text/table rows from PDF
  - [ ] Regex patterns to identify: Date, Amount, Description per row
  - [ ] Return list of ParsedTransaction
  - [ ] Log errors per row (invalid format, missing fields)
- [ ] Test: `tests/Infrastructure.Tests/PDF/BankPdfParserTests.cs`
  - [ ] Test: Parse valid bank PDF returns N transactions
  - [ ] Test: Malformed PDF throws PdfParsingException
  - [ ] Test: Missing amount column returns empty list

### Application Layer: Import Command & Handler
- [ ] Create `Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommand.cs`
  - [ ] Input: UserId, FileStream (PDF bytes)
  - [ ] Output: { Imported: int, Skipped: int, Errors: List<string> }
- [ ] Create `Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs`
  - [ ] Step 1: Parse PDF using `IPdfParser` (injected)
  - [ ] Step 2: For each ParsedTransaction:
    - [ ] Validate against domain rules (amount > 0, date valid)
    - [ ] Check for duplicates (same user, date, amount within 1 min)
    - [ ] Create Transaction domain entity
  - [ ] Step 3: Persist valid transactions via `ITransactionRepository`
  - [ ] Step 4: Return summary with errors
- [ ] Create validator: `ImportTransactionsFromPdfCommandValidator.cs`
  - [ ] File size < 10MB
  - [ ] File extension = .pdf

### Application Layer: Transaction CRUD Commands
- [ ] Create `Application/Features/Transactions/Commands/CreateTransactionCommand.cs`
  - [ ] Input: UserId, Amount, Date, CategoryId, Description
  - [ ] Output: TransactionId
- [ ] Create handler: Validate, create domain entity, persist, return ID
- [ ] Create `Application/Features/Transactions/Commands/UpdateTransactionCommand.cs`
  - [ ] Allowed updates: Description, CategoryId (not amount/date)
- [ ] Create `Application/Features/Transactions/Commands/DeleteTransactionCommand.cs`
  - [ ] Soft delete or hard delete (document decision)
- [ ] Create validators for all commands

### Infrastructure Layer: Repository Implementation
- [ ] Create `Infrastructure/Persistence/TransactionRepository.cs`
  - [ ] `AddAsync()`: Insert into Supabase
  - [ ] `GetByIdAsync()`: Query by id + validate user ownership
  - [ ] `FindAsync()`: Execute specification (WHERE clauses)
  - [ ] `UpdateAsync()`: Update allowed fields
  - [ ] `DeleteAsync()`: Delete from DB
- [ ] Create `Infrastructure/Persistence/CategoryRepository.cs` (same pattern)
- [ ] Create `Infrastructure/Persistence/BudgetRepository.cs` (same pattern)
- [ ] All methods: Scoped to current UserId (prevent cross-tenant access)

### Frontend Layer: Upload Page
- [ ] Create `Frontend/Pages/Transactions/Upload.cshtml`
  - [ ] Form: File input, Submit button
  - [ ] Display uploaded file name
- [ ] Create `Frontend/Pages/Transactions/Upload.cshtml.cs`
  - [ ] Method: `OnPostAsync()` → Calls ImportTransactionsFromPdfCommand
  - [ ] Display success: "N transactions imported, M errors"
  - [ ] Display errors: List of skipped rows with reasons
  - [ ] Redirect to transaction list on success

### Frontend Layer: Transaction List
- [ ] Create `Frontend/Pages/Transactions/Index.cshtml`
  - [ ] Table: Date, Amount, Category, Description, Actions
  - [ ] Actions: Edit, Delete buttons
  - [ ] Show loading indicator during sync
- [ ] Create `Frontend/Pages/Transactions/Index.cshtml.cs`
  - [ ] Query: GetTransactionListQuery (paginated)

### Frontend Layer: Create/Edit Transaction
- [ ] Create `Frontend/Pages/Transactions/Create.cshtml` + `.cshtml.cs`
  - [ ] Form: Amount, Date (picker), Category (dropdown), Description
  - [ ] Submit calls CreateTransactionCommand
- [ ] Create `Frontend/Pages/Transactions/Edit.cshtml` + `.cshtml.cs`
  - [ ] Same form, but calls UpdateTransactionCommand

### Database Migrations
- [ ] Migration: `006_PdfImports_Metadata_Table.sql`
  - [ ] Columns: id, user_id, pdf_filename, imported_count, skipped_count, created_at
  - [ ] Track import history for audit trail

### Integration Tests
- [ ] `tests/Application.Tests/Features/Transactions/ImportTransactionsTests.cs`
  - [ ] Test: Valid PDF → transactions persisted
  - [ ] Test: Duplicate transactions → skipped with error
  - [ ] Test: Invalid transaction (negative amount) → skipped
  - [ ] Test: Transactions scoped to importing user
- [ ] `tests/Application.Tests/Features/Transactions/TransactionCrudTests.cs`
  - [ ] Test: CreateTransactionCommand creates and returns ID
  - [ ] Test: UpdateTransactionCommand updates description
  - [ ] Test: DeleteTransactionCommand removes transaction
  - [ ] Test: User cannot delete another user's transaction

### Phase 3 Exit Criteria (MVP Launch)
- [ ] User can upload PDF → Transactions imported
- [ ] Invalid transactions rejected with explanations
- [ ] Duplicate detection working
- [ ] Transactions listed on page with table
- [ ] All transaction CRUD operations working
- [ ] End-to-end test passes: Upload → Import → Query
- [ ] Staging deployment successful
- [ ] Commit: "feat(phase-3): transaction import pipeline complete – MVP launch"

---

## Phase 4: Analytics & Dashboard (3-4 weeks)

### Pre-Start Checklist
- [ ] Phase 0-3 complete with real transaction data
- [ ] Install NuGet: `Chart.js` (client-side) or similar
- [ ] Supabase database has 100+ test transactions per user

### Application Layer: Analytics Queries
- [ ] Create `Application/Features/Analytics/Queries/GetSpendingByCategoryQuery.cs`
  - [ ] Input: UserId, MonthYear (optional filter)
  - [ ] Output: List<{ CategoryName, Amount, Percentage }>
- [ ] Create handler: GROUP BY category, SUM(amount), calculate percentages
- [ ] Create `Application/Features/Analytics/Queries/GetMonthlyTrendsQuery.cs`
  - [ ] Input: UserId, MonthsBack (default 12)
  - [ ] Output: List<{ Month, TotalSpent, CategoryBreakdown }>
- [ ] Create handler: GROUP BY month, aggregate by category
- [ ] Create `Application/Features/Analytics/Queries/GetBudgetStatusQuery.cs`
  - [ ] Input: UserId, Month
  - [ ] Output: List<{ CategoryName, BudgetLimit, CurrentSpent, Status (OnTrack/Warning/Over) }>
- [ ] Create `Application/Features/Analytics/Queries/GetTransactionListQuery.cs` (paginated)
  - [ ] Input: UserId, PageNumber, PageSize, Filters (date, category)
  - [ ] Output: Paged<Transaction>

### Database Optimization
- [ ] Add composite index: `transactions(user_id, date, category_id)`
- [ ] Add index: `budgets(user_id, month)`
- [ ] Test query performance: Queries should execute < 500ms with 10k records
- [ ] Run execution plan analysis on key queries

### Frontend Layer: Dashboard Page
- [ ] Create `Frontend/Pages/Dashboard/Index.cshtml`
  - [ ] Section 1: Total spending this month (large number)
  - [ ] Section 2: Pie chart - Spending by category
  - [ ] Section 3: Line chart - Monthly trends (last 12 months)
  - [ ] Section 4: Budget status cards (per category)
  - [ ] Filter section: Date range picker
- [ ] Create `Frontend/Pages/Dashboard/Index.cshtml.cs`
  - [ ] Call 3 queries: SpendingByCategory, MonthlyTrends, BudgetStatus
  - [ ] Pass data to view for charting

### Frontend Layer: Reports Pages
- [ ] Create `Frontend/Pages/Reports/Monthly.cshtml`
  - [ ] Same data as dashboard, but printer-friendly
  - [ ] Add month selector dropdown
- [ ] Create `Frontend/Pages/Reports/Category.cshtml`
  - [ ] Category breakdown: Total per category (all time, YTD, custom range)
  - [ ] Show trend for selected category

### Charting Library Integration
- [ ] Install Chart.js via npm or CDN
- [ ] Create `Frontend/wwwroot/js/charts.js`
  - [ ] Function: `drawPieChart(elementId, data, labels)`
  - [ ] Function: `drawLineChart(elementId, months, data)`
  - [ ] Function: `drawProgressBars(elementId, budgets)`
- [ ] Update main layout to include Chart.js script

### Export to CSV
- [ ] Create `Application/Features/Reports/ExportTransactionsCsvQuery.cs`
  - [ ] Input: UserId, Filters (date range, category)
  - [ ] Output: CSV byte stream
- [ ] Handler: Query transactions, format as CSV (Date, Amount, Category, Description)
- [ ] Frontend: Add "Export CSV" button on dashboard/transaction list

### Integration Tests
- [ ] `tests/Application.Tests/Features/Analytics/SpendingByCategoryTests.cs`
  - [ ] Test: 3 categories with $100, $50, $25 each → percentages correct
  - [ ] Test: Filter by date range → only included transactions counted
- [ ] `tests/Application.Tests/Features/Analytics/MonthlyTrendsTests.cs`
  - [ ] Test: Query returns 12 months of data
  - [ ] Test: Months with no transactions show $0
- [ ] `tests/Application.Tests/Features/Analytics/BudgetStatusTests.cs`
  - [ ] Test: Status = "OnTrack" when spend < 80% of limit
  - [ ] Test: Status = "Warning" when 80% <= spend < 100%
  - [ ] Test: Status = "Over" when spend >= 100%

### Performance Testing
- [ ] Create test: Load 10,000 transactions for user
- [ ] Benchmark: Dashboard queries execute < 500ms
- [ ] Benchmark: Export CSV < 2s for 10k transactions

### Phase 4 Exit Criteria (MVP Enhanced)
- [ ] Dashboard displays all 3 charts with real data
- [ ] Charts update when date filters change
- [ ] Queries execute < 500ms consistently
- [ ] CSV export includes all filtered transactions
- [ ] Pagination works on transaction list
- [ ] All analytics tests pass (8+ tests)
- [ ] Staging deployment successful
- [ ] Commit: "feat(phase-4): analytics & dashboard complete – Full MVP released"

---

## Phase 5: Budget Management & Alerts (2-3 weeks)

### Pre-Start Checklist
- [ ] Phase 0-4 complete
- [ ] Transaction data available for budget calculations
- [ ] Optional: Email service configured (SendGrid API key or test mode)

### Application Layer: Budget Commands
- [ ] Create `Application/Features/Budgets/Commands/CreateBudgetCommand.cs`
  - [ ] Input: UserId, CategoryId, MonthlyLimit (Money), Month (YYYYMM)
  - [ ] Output: BudgetId
- [ ] Create handler: Validate, create entity, persist
- [ ] Create `Application/Features/Budgets/Commands/UpdateBudgetCommand.cs`
  - [ ] Input: BudgetId, NewMonthlyLimit
- [ ] Create `Application/Features/Budgets/Commands/DeleteBudgetCommand.cs`
- [ ] Create validators for all commands

### Application Layer: Budget Queries
- [ ] Create `Application/Features/Budgets/Queries/GetBudgetsQuery.cs`
  - [ ] Input: UserId, Month (optional)
  - [ ] Output: List<BudgetDto>

### Domain Layer: Budget Alert Service
- [ ] Create `Domain/Services/IBudgetAlertService.cs`
  - [ ] Method: `CheckBudgetStatusAsync(userId, month)` → Task<List<BudgetAlert>>
  - [ ] BudgetAlert: CategoryName, CurrentSpent, Limit, AlertStatus (80%, 100%)
- [ ] Implementation: Compare current spending vs budget limits
  - [ ] Query actual spending from transactions
  - [ ] Compare against budget limit
  - [ ] Generate alerts for 80%+ and 100%+

### Infrastructure Layer: Notification Service
- [ ] Create `Infrastructure/Notifications/INotificationService.cs`
  - [ ] Method: `SendEmailAsync(toEmail, subject, body)` → Task<bool>
- [ ] Create `Infrastructure/Notifications/SendGridNotificationService.cs` (or mock)
  - [ ] Integrate with SendGrid API or use test mode
  - [ ] Log sent emails
- [ ] Create `Infrastructure/Notifications/EmailTemplates.cs`
  - [ ] Template: "You've reached 80% of your {category} budget"
  - [ ] Template: "You've exceeded your {category} budget"

### Background Job: Daily Budget Check
- [ ] Install NuGet: `Hangfire` (or alternative job scheduler)
- [ ] Create `Infrastructure/Jobs/BudgetAlertJob.cs`
  - [ ] Method: `CheckAllBudgetsAsync()` → foreach user, check budgets, send alerts
- [ ] Register recurring job in `Frontend/Program.cs`
  - [ ] Schedule: Daily at 8 AM (configurable)
- [ ] Test job locally: Trigger manually, verify emails sent

### Frontend Layer: Budget Management Pages
- [ ] Create `Frontend/Pages/Budgets/Index.cshtml`
  - [ ] Table: Category, Monthly Limit, Current Spent, Status, Actions
  - [ ] Status color-coded: Green (OnTrack), Yellow (Warning), Red (Over)
  - [ ] Actions: Edit, Delete buttons
  - [ ] Add Budget button
- [ ] Create `Frontend/Pages/Budgets/Create.cshtml` + `.cshtml.cs`
  - [ ] Form: Category dropdown, Monthly limit amount
  - [ ] Month picker (default to current month)
  - [ ] Submit calls CreateBudgetCommand
- [ ] Create `Frontend/Pages/Budgets/Edit.cshtml` + `.cshtml.cs`
  - [ ] Same form as create, calls UpdateBudgetCommand

### Integration Tests
- [ ] `tests/Application.Tests/Features/Budgets/BudgetCrudTests.cs`
  - [ ] Test: CreateBudgetCommand creates budget
  - [ ] Test: Cannot create duplicate budget (same user-category-month)
  - [ ] Test: UpdateBudgetCommand updates limit
- [ ] `tests/Application.Tests/Services/BudgetAlertServiceTests.cs`
  - [ ] Test: Alert generated when spending > 80% of limit
  - [ ] Test: Alert generated when spending >= 100% of limit
  - [ ] Test: No alert when spending < 80%
- [ ] `tests/Infrastructure.Tests/Notifications/NotificationServiceTests.cs`
  - [ ] Test: Email sent successfully
  - [ ] Test: Email contains expected budget information

### Phase 5 Exit Criteria (Optional)
- [ ] User can create budgets per category
- [ ] Budget status displayed on dashboard
- [ ] Daily budget check job runs
- [ ] Email alerts sent on 80% and 100% thresholds
- [ ] User can edit/delete budgets
- [ ] All budget tests pass (8+ tests)
- [ ] Commit: "feat(phase-5): budget management & alerts complete"

---

## Phase 6: UI Polish, Performance & Production Deployment (2-3 weeks)

### Pre-Start Checklist
- [ ] Phase 0-5 complete (or 0-4 for MVP)
- [ ] All tests passing
- [ ] Code review completed
- [ ] No critical issues in backlog

### Frontend UI Polish
- [ ] [ ] Make all pages responsive (375px mobile, 1920px desktop)
  - [ ] Test in Chrome DevTools emulator
  - [ ] Fix Tailwind responsive classes (`sm:`, `md:`, `lg:`)
- [ ] [ ] Add loading spinners/skeleton screens
  - [ ] Show during API calls
  - [ ] Prevent duplicate submissions
- [ ] [ ] Improve form UX
  - [ ] Inline validation (email format, amount > 0)
  - [ ] Clear error messages
  - [ ] Success toast/alert after submission
- [ ] [ ] Add 404/500 error pages
  - [ ] Friendly messages
  - [ ] Link back to dashboard
- [ ] [ ] Dark mode toggle (optional, nice-to-have)

### Accessibility Improvements
- [ ] [ ] Add ARIA labels to all form inputs
- [ ] [ ] Test keyboard navigation (Tab, Enter)
- [ ] [ ] Test screen reader (NVDA or similar on sample pages)
- [ ] [ ] Run Lighthouse accessibility audit (target ≥ 90)

### Performance Optimization
- [ ] [ ] Analyze frontend bundle size
  - [ ] Tree-shake unused CSS classes
  - [ ] Minify JavaScript
  - [ ] Lazy-load Chart.js only on dashboard
- [ ] [ ] Database query optimization
  - [ ] Verify all indexes present
  - [ ] Check query execution plans
  - [ ] Add query result caching (Redis) if needed
- [ ] [ ] Run Lighthouse performance audit
  - [ ] Target: Score ≥ 90 (Performance, Best Practices)

### Logging & Error Tracking
- [ ] [ ] Integrate Sentry or Application Insights
  - [ ] Capture all exceptions
  - [ ] Track error frequency
- [ ] [ ] Configure structured logging
  - [ ] Log level: Information in prod
  - [ ] Exclude sensitive data (passwords, tokens)
- [ ] [ ] Setup dashboards for monitoring
  - [ ] Monitor error rates
  - [ ] Track API response times

### Security Audit
- [ ] [ ] CORS: Verify only expected origins allowed
- [ ] [ ] HTTPS: Enforce in production
- [ ] [ ] CSP headers: Prevent inline script execution
- [ ] [ ] Secrets: No hardcoded API keys (use environment variables)
- [ ] [ ] SQL Injection: Verify all queries parameterized
- [ ] [ ] XSS: Verify all user input sanitized
- [ ] [ ] CSRF tokens on all POST forms
- [ ] [ ] Rate limiting: Implement on auth endpoints

### Production Environment Setup
- [ ] [ ] Create production Supabase instance (separate from staging)
- [ ] [ ] Configure environment variables in Vercel dashboard
  - [ ] Supabase URL
  - [ ] Supabase API Key
  - [ ] Sentry DSN
  - [ ] Email service credentials
- [ ] [ ] Run migrations on production database
- [ ] [ ] Backup strategy: Enable Supabase automated backups
- [ ] [ ] Database secrets: Rotate initial passwords

### Vercel Deployment Configuration
- [ ] [ ] Create `vercel.json` in Frontend/:
  - [ ] Build command: `dotnet publish -c Release -o out`
  - [ ] Output directory: `out`
  - [ ] Framework: `dotnet`
- [ ] [ ] Connect GitHub to Vercel
- [ ] [ ] Set build settings:
  - [ ] Install command: (empty, .NET handles it)
  - [ ] Build command: From vercel.json
  - [ ] Output directory: From vercel.json
- [ ] [ ] Environment variables configured
- [ ] [ ] Custom domain (if applicable)

### Load Testing
- [ ] [ ] Setup load test: 1000 concurrent users
  - [ ] Tool: Apache JMeter or k6
  - [ ] Scenario: 50% dashboard loads, 50% transaction queries
  - [ ] Duration: 5 minutes
- [ ] [ ] Monitor response times:
  - [ ] Target p95 < 2 seconds
  - [ ] Target p99 < 5 seconds
- [ ] [ ] Analyze results: Identify bottlenecks
- [ ] [ ] Optimize if needed (caching, query tuning)

### Documentation for Users
- [ ] [ ] Create user guide (FAQ)
  - [ ] How to upload PDF
  - [ ] Supported bank formats
  - [ ] How to interpret dashboard
- [ ] [ ] Create troubleshooting guide
  - [ ] "My upload fails" → steps to resolve
  - [ ] "Chart not showing" → refresh page, check data

### Smoke Tests Before Production Deploy
- [ ] [ ] Register new account
- [ ] [ ] Login with existing account
- [ ] [ ] Upload sample PDF
- [ ] [ ] View transaction list
- [ ] [ ] View dashboard with charts
- [ ] [ ] Create budget
- [ ] [ ] Export CSV
- [ ] [ ] Logout

### Production Deployment
- [ ] [ ] Verify all tests passing on main branch
- [ ] [ ] Merge from staging to main
- [ ] [ ] Trigger Vercel deployment
- [ ] [ ] Monitor logs for first 1 hour (Sentry, Application Insights)
- [ ] [ ] Verify health check: GET /api/health → 200 OK
- [ ] [ ] Post-deploy checklist:
  - [ ] SSL certificate valid (green lock in browser)
  - [ ] No console errors
  - [ ] Authentication working
  - [ ] Dashboard loading
  - [ ] PDF upload succeeds

### Phase 6 Exit Criteria (Production Ready)
- [ ] Lighthouse score ≥ 90 (all audits)
- [ ] Mobile responsive (375px width works)
- [ ] Load test: 1000 users, p95 < 2s
- [ ] Security audit: OWASP Top 10 addressed
- [ ] Error logging functional (Sentry capturing errors)
- [ ] Production deployment successful
- [ ] Smoke tests all pass
- [ ] User documentation complete
- [ ] Commit: "chore(phase-6): production deployment complete – v1.0.0"

---

## Quick Reference: Commit Messages

Each phase should end with a commit message:

```
Phase 0: feat: phase 0 foundation setup complete
Phase 1: feat(phase-1): authentication & multi-tenancy complete
Phase 2: feat(phase-2): domain entities & value objects complete
Phase 3: feat(phase-3): transaction import pipeline complete – MVP launch
Phase 4: feat(phase-4): analytics & dashboard complete – Full MVP released
Phase 5: feat(phase-5): budget management & alerts complete
Phase 6: chore(phase-6): production deployment complete – v1.0.0
```

---

## Notes

- **TDD First**: For each section, write tests BEFORE implementation
- **Constitution Alignment**: Every feature respects Clean Architecture, CQRS, DDD, Test-First, Spec-Driven principles
- **Incremental Value**: Each phase delivers standalone value (not waiting for later phases)
- **Risk Mitigation**: Early spikes on uncertain areas (PDF parsing in Phase 3 week 1)

**Good luck! 🚀**
