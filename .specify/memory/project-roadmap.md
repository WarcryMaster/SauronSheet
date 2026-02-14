# SauronSheet: Project Roadmap & Incremental Execution Plan

**Created**: 2026-02-14  
**Version**: 1.0.0  
**Constitution Alignment**: All phases follow 5 core principles (Clean Arch, CQRS, DDD, Test-First, Spec-Driven)

---

## Executive Summary

SauronSheet is a multi-user expense tracking application with PDF import, analytics, and budget management. Execution follows **Test-First + Spec-Driven** development with incremental delivery of MVPs. Each phase produces a **fully testable, independently deployable slice** of functionality.

**Total Estimated Duration**: 16-20 weeks  
**Team Size**: 1 developer (async-friendly phases)  
**Release Strategy**: Phase-by-phase deployments to staging; production releases after Phase 3+

---

## Execution Phases

### Phase 0: Foundation & Infrastructure Setup
**Duration**: 2-3 weeks  
**Goal**: Establish layered architecture, tooling, CI/CD baseline  
**Status**: ⏳ BLOCKED — Awaiting project initiation

#### Objectives
1. Initialize .NET Core 10 solution with 4-layer structure (Frontend, Application, Domain, Infrastructure)
2. Configure Supabase project, migrations framework, connection pooling
3. Setup MediatR pipeline with handlers, validators, behaviors
4. Configure xUnit + Moq test infrastructure with test helpers
5. Establish GitHub Actions CI/CD pipeline (build, test, deploy to staging)
6. Document base architecture patterns in code examples

#### Deliverables
- ✅ Solution structure: `Frontend/`, `Application/`, `Domain/`, `Infrastructure/`
- ✅ `Domain/Common/` with base Entity, ValueObject abstractions
- ✅ `Application/Common/` with base handlers, DTOs, pipeline behaviors
- ✅ Supabase project configuration, migrations folder structure
- ✅ MediatR registration in `Program.cs` with pipeline
- ✅ Test project setup: `Domain.Tests/`, `Application.Tests/`, `Infrastructure.Tests/`
- ✅ CI/CD pipeline: `.github/workflows/build-test-deploy.yml`
- ✅ README: Architecture diagram, folder structure explanation

#### Tests (TDD)
- [ ] T00-001: Verify Domain Entity constructor enforces immutability
- [ ] T00-002: Verify MediatR handler resolves correctly from DI container
- [ ] T00-003: Verify Supabase connection string configurable from appsettings
- [ ] T00-004: Verify test helpers (FakeRepository, FakeUserContext) work
- [ ] T00-005: Verify GitHub Actions runs tests on push

#### Dependency Chain
- Nothing blocks Phase 0 (foundation)

#### Definition of Done
- [ ] All 4 layers compile without warnings (nullable enabled)
- [ ] MediatR pipeline passes 5 unit tests
- [ ] Supabase migrations run successfully
- [ ] CI/CD pipeline green on all tests
- [ ] Documentation includes architecture diagrams

---

### Phase 1: Authentication & Multi-Tenancy Foundation
**Duration**: 3-4 weeks  
**Goal**: Implement Supabase Auth integration, user context, authorization layer  
**Dependencies**: Phase 0 complete  
**Status**: ⏳ BLOCKED

#### Objectives
1. Integrate Supabase Auth (signup, login, logout, JWT refresh)
2. Create User domain entity with tenant isolation rules
3. Implement `IUserContext` abstraction for DI-injected user claims
4. Build authorization specifications (IsUserOwner, IsAdmin)
5. Create Razor Page layout with auth UI (login form, user menu)
6. Write integration tests: Auth pipeline, JWT token validation, tenant isolation

#### User Stories (P1 Priority)
- **US1.1**: User can sign up with email/password via Supabase Auth
- **US1.2**: User can log in and receive JWT token stored in secure cookie
- **US1.3**: Authenticated user's ID automatically passed to all queries/commands
- **US1.4**: User profile stored in Supabase after first login (auto-provisioning)
- **US1.5**: System prevents user A from accessing user B's data

#### Deliverables
- ✅ `Domain/Entities/User.cs` with AggregateRoot, invariants (email, userId uniqueness)
- ✅ `Domain/Specifications/UserSpecifications.cs` (IsUserOwner, IsAdmin)
- ✅ `Application/Auth/` commands: `RegisterUserCommand`, `LoginUserCommand`
- ✅ `Application/Common/IUserContext.cs` + implementation
- ✅ `Infrastructure/Auth/SupabaseAuthService.cs` with JWT handling
- ✅ `Frontend/Pages/Auth/` (Login.cshtml, Register.cshtml, Logout handler)
- ✅ `Frontend/Shared/` layout with user menu, auth status
- ✅ Database migrations: `users` table with auth metadata

#### Tests (TDD)
- [ ] T01-001: User can register with valid email/password
- [ ] T01-002: Login returns JWT token in secure cookie
- [ ] T01-003: IUserContext extracts user ID from JWT claims correctly
- [ ] T01-004: Query handler scopes results to current user tenant
- [ ] T01-005: User A cannot read User B's data (cross-tenant isolation)
- [ ] T01-006: User profile auto-provisioned on first login
- [ ] T01-007: Logout clears JWT cookie
- [ ] T01-008: Expired JWT triggers re-authentication

#### Dependency Chain
- **Blocks**: Phase 2, 3, 4, 5 (all features depend on auth context)

#### Definition of Done
- [ ] Auth workflow tested end-to-end (register → login → authenticated request)
- [ ] All user context queries scoped to current tenant
- [ ] Integration tests pass (JWT validation, tenant isolation)
- [ ] Deployment to staging: Users can authenticate
- [ ] Security review: CORS, HTTPS enforced, secure cookies

---

### Phase 2: Core Data Model & Domain Entities
**Duration**: 2-3 weeks  
**Goal**: Define Transaction, Category, Budget entities with business rules  
**Dependencies**: Phase 0, Phase 1  
**Status**: ⏳ BLOCKED

#### Objectives
1. Design Transaction, Category, Budget domain entities with value objects (Money, TransactionId, etc.)
2. Implement domain logic: Budget.IsOverBudget(), Transaction.Validate(), Category hierarchy
3. Create domain specifications for filtering (TransactionsByDateRange, TransactionsByCategory)
4. Write domain unit tests: Entity invariants, value object equality, state transitions
5. Define repository interfaces (ITransactionRepository, ICategoryRepository)
6. Document DDD patterns: Aggregate roots, bounded contexts, ubiquitous language

#### User Stories (P1 Priority)
- **US2.1**: System stores transaction records with (date, amount, category, description, user)
- **US2.2**: System maintains category list per user (custom + default categories)
- **US2.3**: System tracks budget limits per category per user
- **US2.4**: Transaction amount cannot be negative or exceed decimal precision
- **US2.5**: Budget must have positive limit and valid category reference

#### Entities & Value Objects
- **Transaction** (AggregateRoot)
  - Properties: Id, UserId, Amount (Money), Date, Category, Description, ImportedFrom
  - Invariants: Amount > 0, Date ≤ today, required Category
  - Methods: IsOutlier(), MatchesPattern()

- **Category** (AggregateRoot)
  - Properties: Id, UserId, Name, Color, IsDefault
  - Invariants: Name unique per user, Name max 50 chars
  - Methods: CanDelete() (not system default)

- **Budget** (AggregateRoot)
  - Properties: Id, UserId, CategoryId, MonthlyLimit (Money), Month
  - Invariants: Limit > 0, unique per user-category-month
  - Methods: IsOverBudget(currentSpend), RemainingAmount()

- **Money** (ValueObject)
  - Properties: Amount (decimal), Currency (string)
  - Invariants: Amount ≥ 0, precision 2 decimals
  - Methods: Plus(), Minus(), CompareTo()

- **TransactionId, UserId, CategoryId** (ValueObjects)

#### Deliverables
- ✅ `Domain/Entities/Transaction.cs`, `Category.cs`, `Budget.cs`
- ✅ `Domain/ValueObjects/Money.cs`, `TransactionId.cs`, `UserId.cs`, `CategoryId.cs`
- ✅ `Domain/Specifications/TransactionSpecifications.cs` (ByDateRange, ByCategory, ByAmount)
- ✅ `Domain/Repositories/ITransactionRepository.cs`, `ICategoryRepository.cs`, `IBudgetRepository.cs`
- ✅ Unit tests: Entity invariants, value object operations (20+ tests)
- ✅ Documentation: `Domain/README.md` with DDD patterns & entity diagrams

#### Tests (TDD)
- [ ] T02-001: Transaction with negative amount raises DomainException
- [ ] T02-002: Money value object equality works correctly
- [ ] T02-003: Budget.IsOverBudget() calculates correctly
- [ ] T02-004: Category.CanDelete() prevents system default deletion
- [ ] T02-005: Transaction date cannot be in future
- [ ] T02-006: Category name uniqueness per user enforced at domain level
- [ ] T02-007: Specification filters transactions by date range correctly
- [ ] T02-008: Budget with negative limit raises DomainException

#### Dependency Chain
- **Blocks**: Phase 3, 4, 5 (all business features depend on these entities)

#### Definition of Done
- [ ] All domain entities compile with nullable reference types enabled
- [ ] 100% unit test coverage for domain entities & value objects
- [ ] Domain invariants enforced (impossible states prevented)
- [ ] Repository interface contracts defined
- [ ] Architecture decision record (ADR) in docs: Why Money as value object

---

### Phase 3: Transaction Import Pipeline (PDF Parsing & CRUD)
**Duration**: 3-4 weeks  
**Goal**: Implement PDF parsing, transaction validation, and CRUD via MediatR commands  
**Dependencies**: Phase 0, 1, 2  
**Status**: ⏳ BLOCKED

#### Objectives
1. Build PDF parsing service (iTextSharp/PdfSharp) to extract bank statement transactions
2. Implement `ImportTransactionsFromPdfCommand` + handler with validation
3. Create transaction creation/update/delete commands via MediatR
4. Build Supabase repository implementations for Transaction, Category, Budget
5. Write integration tests: PDF parsing, transaction persistence, rollback on validation
6. Create Razor page for PDF upload with progress feedback

#### User Stories (P1 MVP)
- **US3.1**: User uploads bank PDF; system extracts transaction rows
- **US3.2**: System validates extracted transactions against domain rules
- **US3.3**: System persists valid transactions to Supabase, skips invalid ones with error
- **US3.4**: User receives upload summary (N imported, M skipped with reasons)
- **US3.5**: User can manually create/edit/delete transactions via UI
- **US3.6**: System prevents duplicate transaction imports (via date+amount hash)

#### Deliverables
- ✅ `Domain/Services/PdfParsingService.cs` interface (bank-agnostic)
- ✅ `Infrastructure/PDF/BankPdfParser.cs` implementation (iTextSharp)
- ✅ `Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommand.cs` + handler
- ✅ `Application/Features/Transactions/Commands/CreateTransactionCommand.cs` + handler
- ✅ `Application/Features/Transactions/Commands/UpdateTransactionCommand.cs` + handler
- ✅ `Application/Features/Transactions/Commands/DeleteTransactionCommand.cs` + handler
- ✅ `Infrastructure/Persistence/TransactionRepository.cs` (Supabase)
- ✅ `Infrastructure/Persistence/CategoryRepository.cs`, `BudgetRepository.cs`
- ✅ Database migrations: `transactions`, `categories`, `budgets`, `pdf_imports` tables
- ✅ `Frontend/Pages/Transactions/Upload.cshtml`, `Create.cshtml`, `Edit.cshtml`
- ✅ Integration tests: PDF parsing, transaction CRUD, error scenarios

#### Tests (TDD)
- [ ] T03-001: PDF parser extracts transaction rows correctly
- [ ] T03-002: ImportTransactionsFromPdfCommand validates all extracted rows
- [ ] T03-003: Invalid transactions fail with descriptive error
- [ ] T03-004: Duplicate transactions detected and skipped
- [ ] T03-005: Valid transactions persisted to Supabase
- [ ] T03-006: Create transaction command generates valid TransactionId
- [ ] T03-007: Update command respects transaction immutability rules
- [ ] T03-008: Delete command cascades to budget calculations
- [ ] T03-009: User cannot import PDF with malformed structure
- [ ] T03-010: PDF import transaction scoped to current user

#### Dependency Chain
- **Blocks**: Phase 4 (analytics needs transaction data)
- **Enables MVP**: Upload transactions → See them in dashboard

#### Definition of Done
- [ ] End-to-end test: Upload PDF → Parse → Validate → Persist → Query results
- [ ] Integration tests pass (pdf upload, transaction CRUD)
- [ ] Error messages actionable (which rows failed, why)
- [ ] Duplicate detection works across re-uploads
- [ ] Deployment to staging: Users can upload PDFs and create transactions
- [ ] Security: PDF uploads scanned for malware (optional Supabase integration)

---

### Phase 4: Analytics & Dashboard (Queries & Reporting)
**Duration**: 3-4 weeks  
**Goal**: Implement MediatR queries for analytics, build dashboard UI with charts  
**Dependencies**: Phase 0, 1, 2, 3  
**Status**: ⏳ BLOCKED

#### Objectives
1. Create MediatR queries: `GetSpendingByCategoryQuery`, `GetMonthlyTrendsQuery`, `GetBudgetStatusQuery`
2. Implement query handlers with Supabase aggregations (GROUP BY, SUM)
3. Add pagination, filtering (date range, category) to queries
4. Build Razor pages with Chart.js/Plotly for visualization
5. Write integration tests: Query performance, pagination, data accuracy
6. Optimize database queries with indexes (userId, date, category)

#### User Stories (P1 MVP)
- **US4.1**: Dashboard displays total spending for current month
- **US4.2**: Dashboard shows spending breakdown by category (pie chart)
- **US4.3**: Dashboard displays monthly spending trend (line chart, last 12 months)
- **US4.4**: User can filter transactions by date range
- **US4.5**: Dashboard shows budget status per category (progress bar)
- **US4.6**: User can export report as CSV

#### Deliverables
- ✅ `Application/Features/Analytics/Queries/GetSpendingByCategoryQuery.cs` + handler
- ✅ `Application/Features/Analytics/Queries/GetMonthlyTrendsQuery.cs` + handler
- ✅ `Application/Features/Analytics/Queries/GetBudgetStatusQuery.cs` + handler
- ✅ `Application/Features/Analytics/Queries/GetTransactionListQuery.cs` + handler (paginated)
- ✅ DTOs: `SpendingByCategoryDto`, `MonthlyTrendDto`, `BudgetStatusDto`
- ✅ `Frontend/Pages/Dashboard/Index.cshtml` + handler
- ✅ `Frontend/Pages/Reports/` (monthly, yearly, category breakdowns)
- ✅ Database indexes: `transactions(userId, date)`, `transactions(userId, categoryId)`
- ✅ Integration tests: Query pagination, filtering, chart data accuracy

#### Tests (TDD)
- [ ] T04-001: GetSpendingByCategoryQuery returns accurate totals
- [ ] T04-002: Query filters by date range correctly
- [ ] T04-003: Query respects pagination (limit, offset)
- [ ] T04-004: Monthly trends query spans 12 months
- [ ] T04-005: Budget status compares spending vs. limit
- [ ] T04-006: Queries scoped to current user only
- [ ] T04-007: Query performance < 500ms for 10k transactions
- [ ] T04-008: CSV export includes all filtered transactions

#### Dependency Chain
- **Depends on**: Phase 3 (needs transaction data)
- **Enables**: Budget alerts (Phase 5)

#### Definition of Done
- [ ] Dashboard displays all 3 charts without errors
- [ ] Queries execute < 500ms with 10k records
- [ ] Pagination works (limit, cursor-based or offset)
- [ ] CSV export includes correct data
- [ ] Deployment to staging: Users see analytics dashboard
- [ ] Performance benchmark: 1000 concurrent dashboard loads

---

### Phase 5: Budget Management & Alerts
**Duration**: 2-3 weeks  
**Goal**: Implement budget tracking, over-budget detection, notifications  
**Dependencies**: Phase 0, 1, 2, 3, 4  
**Status**: ⏳ BLOCKED

#### Objectives
1. Create budget creation/update/delete commands via MediatR
2. Implement budget validation & uniqueness rules (one per user-category-month)
3. Add query for current spending vs. budget by category
4. Implement budget alert logic (email/in-app notification when over budget)
5. Build Razor pages for budget management UI
6. Write integration tests: Budget CRUD, alert generation

#### User Stories (P2 Priority)
- **US5.1**: User can set monthly budget limit per category
- **US5.2**: System calculates current spending vs. budget for each category
- **US5.3**: System triggers alert when spending exceeds 80% of budget
- **US5.4**: User receives email notification on budget threshold breach
- **US5.5**: User can view all budgets and edit/delete them
- **US5.6**: System suggests budget limits based on average historical spending

#### Deliverables
- ✅ `Application/Features/Budgets/Commands/CreateBudgetCommand.cs` + handler
- ✅ `Application/Features/Budgets/Commands/UpdateBudgetCommand.cs` + handler
- ✅ `Application/Features/Budgets/Commands/DeleteBudgetCommand.cs` + handler
- ✅ `Application/Features/Budgets/Queries/GetBudgetsQuery.cs` + handler
- ✅ `Domain/Services/BudgetAlertService.cs` (calculate alerts)
- ✅ `Infrastructure/Notifications/EmailNotificationService.cs` (stub or SendGrid)
- ✅ `Frontend/Pages/Budgets/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`
- ✅ Background job: Check budgets daily, send alerts (hangfire or similar)
- ✅ Integration tests: Budget CRUD, alert calculation, email sending

#### Tests (TDD)
- [ ] T05-001: CreateBudgetCommand creates budget with valid limit
- [ ] T05-002: Duplicate budget per user-category-month rejected
- [ ] T05-003: Budget limit must be positive
- [ ] T05-004: Budget alert triggered at 80% threshold
- [ ] T05-005: Email notification sent on alert
- [ ] T05-006: Budget can be deleted only by owner
- [ ] T05-007: System suggests limits based on 3-month average spending
- [ ] T05-008: Budget calculations respect date range (e.g., Jan 1-31)

#### Dependency Chain
- **Depends on**: Phase 3, 4 (needs transactions, spending queries)
- **Optional**: Can deploy without Phase 5; full MVP works without budgets

#### Definition of Done
- [ ] Budget creation → alert triggered → email sent (end-to-end)
- [ ] All budget CRUD operations tested
- [ ] Email notification service mocked in tests (or stubbed)
- [ ] Deployment to staging: Users can manage budgets
- [ ] Background job scheduling verified (test alert email delivery)

---

### Phase 6: UI Polish, Performance & Deployment to Production
**Duration**: 2-3 weeks  
**Goal**: Optimize frontend, prepare for production deployment  
**Dependencies**: All previous phases (1-5 complete)  
**Status**: ⏳ BLOCKED

#### Objectives
1. Refine Razor Pages UI: Mobile responsive, accessibility (WCAG 2.1)
2. Optimize Tailwind CSS: Unused class removal, minification
3. Add Alpine.js interactivity: Modal dialogs, loading spinners, form validation
4. Implement error pages (404, 500), logging pipeline
5. Prepare production Supabase instance, environment variable management
6. Security audit: CORS, HTTPS, SQL injection, XSS prevention
7. Performance testing: Load testing 1000 concurrent users
8. Deployment to Vercel with auto-scaling

#### Objectives Detail
- **UI/UX**: Responsive design, dark mode toggle, accessibility improvements
- **Performance**: Database query optimization, caching strategy, CDN for static assets
- **Security**: Penetration testing, API rate limiting, input validation
- **Logging**: Sentry/Application Insights for error tracking
- **Documentation**: User guide, API documentation for future integrations

#### Deliverables
- ✅ Responsive Tailwind CSS across all pages (mobile-first)
- ✅ Alpine.js interactivity (budget modals, transaction filters)
- ✅ 404/500 error pages with helpful messages
- ✅ Centralized logging: Sentry or Application Insights
- ✅ Production Supabase environment (separate from staging)
- ✅ Vercel deployment configuration (`vercel.json`)
- ✅ Load test results (1000 users, 500 concurrent)
- ✅ Security audit checklist completed

#### New User Stories (P3 Polish)
- **US6.1**: Mobile users can use dashboard and upload PDFs
- **US6.2**: Users receive loading indicators during async operations
- **US6.3**: Users see actionable error messages on failures
- **US6.4**: System logs errors for debugging without exposing stack traces

#### Tests (TDD)
- [ ] T06-001: Dashboard responsive on mobile (375px width)
- [ ] T06-002: Accessibility: Form labels present, keyboard navigation works
- [ ] T06-003: XSS prevention: User input sanitized in all pages
- [ ] T06-004: SQL injection: Parameterized queries used everywhere
- [ ] T06-005: CSRF tokens present on all POST forms
- [ ] T06-006: Load test: 1000 concurrent users, <2s response time

#### Deployment Steps
1. [ ] Create production Supabase instance
2. [ ] Configure environment variables in Vercel dashboard
3. [ ] Run migrations on production database
4. [ ] Deploy to Vercel (auto-deploy from main branch)
5. [ ] Smoke tests: Login → Upload → View dashboard → Check analytics
6. [ ] Monitor error logs (Sentry) for first 2 hours
7. [ ] Enable auto-scaling if needed

#### Definition of Done
- [ ] All Lighthouse scores ≥ 90 (Performance, Accessibility, Best Practices)
- [ ] Load test passes: 1000 concurrent users, p95 < 2s
- [ ] Security audit: OWASP Top 10 addressed
- [ ] Error logging functional (errors captured in Sentry)
- [ ] Production deployment successful with zero data loss
- [ ] User documentation complete (FAQ, screenshot guide)

---

## Cross-Phase Dependencies & Critical Path

```
Phase 0 (Foundation)
    ↓
Phase 1 (Auth) ←── CRITICAL PATH (blocks all others)
    ↓
Phase 2 (Domain) ←── CRITICAL PATH
    ↓
Phase 3 (Transactions) ←── CRITICAL PATH (MVP is here)
    ↓
Phase 4 (Analytics) ←── CRITICAL PATH (MVP enhancement)
    ↓
Phase 5 (Budgets) ←── Optional (nice-to-have for MVP)
    ↓
Phase 6 (Polish & Deploy) ←── Final stretch
```

**Critical Path Phases**: 0 → 1 → 2 → 3 → 4 → 6  
**Minimum MVP**: Phases 0-4 (56% of total scope)  
**Full Release**: Phases 0-6 (100% of total scope)

---

## Timeline & Milestones

| Phase | Duration | Start | End | MVP? | Milestone |
|-------|----------|-------|-----|------|-----------|
| 0 | 2-3w | W1 | W3 | — | "Architecture ready" |
| 1 | 3-4w | W4 | W7 | — | "Auth working" |
| 2 | 2-3w | W8 | W10 | — | "Domain tested" |
| 3 | 3-4w | W11 | W14 | ✅ | "PDF upload works" |
| 4 | 3-4w | W15 | W18 | ✅ | "Dashboard live" |
| 5 | 2-3w | W19 | W21 | ⚪ | "Budgets working" |
| 6 | 2-3w | W22 | W24 | — | "Production ready" |

**MVP Release Target**: End of Week 18 (Phases 0-4 complete)  
**Full Release Target**: End of Week 24 (Phases 0-6 complete)

---

## Risk Management & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|-----------|
| PDF parsing library issues with bank format | High | Medium | Early spike (W3): Test library with real bank PDFs; fallback to manual format support |
| Supabase performance bottleneck at scale | High | Low | Phase 4: Add database indexes; load test early with 10k+ records |
| Auth integration complexity (Supabase JWT + cookies) | Medium | Medium | Phase 1: Spike week dedicated to auth testing; document edge cases |
| CQRS pattern complexity for single developer | Medium | Medium | Phase 0: Invest in base handlers, keep examples in code; reuse patterns |
| Scope creep (feature requests) | Medium | High | Maintain strict Phase separation; track features for Phase 5.x backlog |
| Vercel deployment issues | Low | Low | Phase 6 end: Test deploy 2 weeks before production; have rollback plan |

---

## Success Criteria by Phase

### Phase 0: Successful
- [ ] Solution builds without errors or warnings
- [ ] MediatR pipeline resolves handlers correctly
- [ ] 5 tests pass proving foundation works
- [ ] CI/CD pipeline green on main branch

### Phase 1: Successful
- [ ] User can register → login → receive JWT in secure cookie
- [ ] Authenticated requests scoped to user tenant
- [ ] 8/8 auth tests pass (integration level)
- [ ] Security review: CORS, HTTPS, token expiry checked

### Phase 2: Successful
- [ ] All domain entities compile with nullable enabled
- [ ] 20+ unit tests pass (100% coverage on critical entities)
- [ ] Domain invariants prevent invalid states
- [ ] Repository interfaces documented & ready for implementation

### Phase 3: Successful (MVP Launch)
- [ ] User can upload PDF → extract transactions → see them listed
- [ ] All invalid transactions rejected with reasons
- [ ] Duplicate detection prevents re-imports
- [ ] End-to-end test passes; staging deployment successful
- [ ] **MVP Ready**: Users can track expenses

### Phase 4: Successful (MVP Enhanced)
- [ ] Dashboard displays 3+ charts with real data
- [ ] Queries execute < 500ms with realistic data volume
- [ ] Pagination works; CSV export includes all data
- [ ] Analytics queries scoped to user tenant
- [ ] **Full MVP Released**: Users understand spending patterns

### Phase 5: Successful (Optional)
- [ ] User can create budgets and receive alerts
- [ ] Email notifications sent on threshold breach
- [ ] All budget CRUD operations tested
- [ ] Background job scheduler functional

### Phase 6: Successful (Production Ready)
- [ ] Lighthouse score ≥ 90 across all audits
- [ ] Mobile responsive on 375px width
- [ ] Load test: 1000 users, p95 < 2s
- [ ] Security audit: OWASP Top 10 addressed
- [ ] **Production Release**: Live to public users

---

## Incremental Delivery Strategy

Each phase is designed for **independent deployment**:

1. **Phase 0-1**: Deploy to developer machine → local testing
2. **Phase 2**: Unit test suite runs; no UI changes
3. **Phase 3**: Deploy to staging → actual user testing with PDFs
4. **Phase 4**: MVP 1.0 on staging → gather feedback
5. **Phase 5**: MVP 1.1 on staging → optional features
6. **Phase 6**: Production deployment on Vercel with monitoring

### Demo Checkpoints
- **End of Phase 1**: Show login flow, JWT in browser DevTools
- **End of Phase 3**: Upload sample PDF, show transactions in database
- **End of Phase 4**: Run through dashboard, show all 3 charts with data
- **End of Phase 5**: Set budget, exceed it, check email notification
- **End of Phase 6**: Production URL live, share with early users

---

## Notes for Developer

### Architecture Decisions
- **CQRS**: Separate Commands (ImportTransactionCommand) from Queries (GetSpendingByCategoryQuery) for clarity
- **DDD**: Transaction, Category, Budget are AggregateRoots; Money is ValueObject
- **Test-First**: Write integration tests BEFORE implementation; specs drive design
- **Incremental**: Each phase is a minimum viable slice; earlier phases don't depend on later features

### When to Deviate from Plan
- If Phase 3 PDF parsing proves unexpectedly complex: Spike 1 week, explore library alternatives
- If Supabase performance issues arise in Phase 4: Add database indexes, cache layer
- If security audit finds critical issues in Phase 6: Fix before production deploy

### Backlog for Post-MVP
- **Phase 5.x**: Budget alerts via mobile push notifications
- **Phase 7**: Multi-currency support, exchange rates
- **Phase 8**: Scheduled transaction rules (recurring expenses)
- **Phase 9**: Social features (shared budgets, spending groups)
- **Phase 10**: AI-powered spending suggestions

---

**Approved By**: Developer  
**Last Updated**: 2026-02-14  
**Next Review**: After Phase 0 completion
