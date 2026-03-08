# Implementation Plan: Feature 3 - System Categories Global Scope Refactoring

**Branch**: `003-system-categories-global-scope` | **Date**: 2026-02-19 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification with 23 clarifications resolved | **Status**: Ready for Task Generation

---

## Summary

**System Categories Global Scope Refactoring** transforms how system default categories are managed — from in-memory generation per-user to persisted global records with NULL user_id. This is an internal architectural improvement with zero user-facing changes.

**Current (Feature 2):**
- System categories generated in-memory by `CategoryService.GetSystemDefaults(UserId userId)`
- Called for each user, creates new instances every time
- NOT persisted in database

**Target (Feature 3):**
- System categories persisted once in database with `user_id = NULL`
- Shared across all users via `WHERE user_id = @userId OR user_id IS NULL` union query
- CategoryService maintains cached hardcoded reference for validation

**Technical Approach:**
- Domain: Nullable `UserId` in Category entity + helper methods + domain validation
- Repository: Union query + new `FindByNameAsync()` method
- Handlers: Safe null-checking (3 minimal updates)
- Database: Single migration + CHECK constraint + composite indexes
- Tests: 20 test specs covering NULL semantics, RLS policies, caching

---

## Technical Context

**Language/Version**: C# / .NET 10 (LTS)  
**Primary Dependencies**: MediatR 12+, Postgrest Client (Supabase), xUnit, Moq  
**Storage**: Supabase PostgreSQL with database triggers (CreatedAt/UpdatedAt)  
**Testing**: xUnit (Domain 100%, Application 70%, Infrastructure integration)  
**Target Platform**: Web (.NET Razor Pages backend)  
**Project Type**: Full-Stack refactoring (all layers minimal changes)  
**Performance Goals**: System defaults cached (1 instance per app lifetime)  
**Constraints**:
- System categories immutable (NULL rows protected by RLS)
- Feature 2 system categories NOT persisted (simplifies migration)
- Zero downtime deployment via blue-green strategy
- Backward compatibility with existing handlers

**Scale/Scope**:
- 24 system default categories (hardcoded, cached)
- 3 handlers affected (safe null-checking)
- 20 test cases (domain + infrastructure + application)
- ~150 lines code changes (net positive)

---

## Constitution Check ✅ PASS

**GATE: All 5 Core Principles verified**

| Principle | Status | Evidence |
|-----------|--------|----------|
| **I. Clean Architecture** | ✅ PASS | Domain (nullable UserId) isolated; Repository handles complexity; Handlers unchanged |
| **II. CQRS + MediatR** | ✅ PASS | Union query transparent to handlers; GetByUserIdAsync() returns same DTOs |
| **III. Domain-Driven Design** | ✅ PASS | Strong-typed IDs (UserId?), invariant validation, domain service caching, helper methods |
| **IV. Test-First Development** | ✅ PASS | 20 test specs defined before implementation; 100% domain coverage target |
| **V. Spec-Driven Development** | ✅ PASS | Single spec.md file; 14 deliverables; scope boundaries clear |

**No violations detected. Feature ready for implementation.**

---

## Project Structure Context

### Current Implementation (Feature 2 Reference)

**Domain Layer:**
- `Category.cs` — Entity with non-nullable UserId
- `CategoryService.cs` — Generates system defaults per-user (in-memory)
- `ICategoryRepository.cs` — Interface with GetSystemDefaultsAsync(UserId)

**Infrastructure Layer:**
- `SupabaseCategoryRepository.cs` — Postgrest implementation
- `002_CreateCategoriesTable.sql` — Migration (NO system category inserts)

**Application Layer:**
- `GetCategoriesQueryHandler.cs` — Returns categories via repository
- `CreateCategoryCommandHandler.cs` — Validates name against GetSystemDefaults()

**Tests:**
- Domain: ~40 tests (Category, CategoryService, ValueObjects)
- Application: ~25 tests (handlers, validation)

---

## Phase 0: Analysis & Design

### Code Audit Results

**Finding 1: System categories NOT persisted in Feature 2**
- `CategoryService.GetSystemDefaults(UserId userId)` generates NEW instances each call
- Database migration has NO INSERT statements for system categories
- Application initialization has NO seeding code
- **Implication**: Feature 3 migration can simply INSERT 24 rows (no UPDATE)

**Finding 2: Current UserId is non-nullable**
- All Category constructors require non-null UserId
- All repository queries use `x.UserId == userId.Value` (no null handling)
- 3 handlers use category.UserId.Value directly (no null checks)
- **Implication**: Changing to nullable UserId requires careful null-checking updates

**Finding 3: GetSystemDefaultsAsync(UserId) signature mismatch**
- Repository method filters `WHERE user_id == userId.Value AND is_system_default = true`
- But semantically, system defaults are same for all users
- **Implication**: Signature should change to remove userId parameter

### Design Decisions

**Decision 1: Nullable UserId Implementation**
- ✅ Private constructor accepts nullable UserId
- ✅ Public constructor requires non-null UserId (user-scoped)
- ✅ Static factory method for system defaults (no userId)
- ✅ Domain invariant: NULL user_id requires IsSystemDefault=true
- ❌ NOT using Union types or discriminated unions (keep it simple)

**Decision 2: Repository Union Query**
- ✅ Single query: `WHERE user_id = @userId OR user_id IS NULL`
- ✅ New method `FindByNameAsync(name)` for global name search
- ✅ Keep `FindByNameAndUserAsync(userId, name)` for backward compatibility
- ✅ UpdatedAt managed by database trigger (no app logic)

**Decision 3: Caching Strategy**
- ✅ Lazy singleton in CategoryService (lock-protected)
- ✅ Alternatives: IMemoryCache (more complex, not needed)
- ✅ Thread-safe initialization pattern (double-check locking)

**Decision 4: Database Migration**
- ✅ Make user_id nullable (ALTER COLUMN DROP NOT NULL)
- ✅ Add CHECK constraint (NULL user_id ⟹ IsSystemDefault=true)
- ✅ Insert 24 system categories with NULL user_id
- ✅ Update UNIQUE index (exclude NULL rows from uniqueness)
- ✅ Add composite index for system category queries

**Decision 5: Deployment Strategy**
- ✅ Blue-green: Deploy code → Run migration → Verify
- ✅ Rollback: Delete NULL rows, revert code
- ✅ Zero downtime (safe null-checking allows coexistence)

---

## Phase 1: Detailed Design & Task Breakdown

### Domain Layer Changes

**Task Set 1: Category Entity Updates**

**Task 1.1: Make UserId nullable**
- Location: `src/SauronSheet.Domain/Entities/Category.cs`
- Change: `public UserId UserId` → `public UserId? UserId`
- Private constructor: Accept `UserId? userId` parameter
- Public constructor: Keep `UserId userId` (non-nullable for user-scoped)
- Factory method: Add `CreateSystemDefault()` with no userId
- Validation: Add domain invariant check (NULL ⟹ IsSystemDefault)
- Effort: 0.5 hours
- Tests: Unit tests T-3.01, T-3.02, T-3.05

**Task 1.2: Add helper methods**
- Location: `src/SauronSheet.Domain/Entities/Category.cs`
- Methods:
  - `public bool IsGlobal => UserId is null`
  - `public bool IsUserScoped => UserId is not null`
  - `public bool IsOwnedByUser(UserId userId)`
  - `public bool IsAccessibleToUser(UserId userId)`
- Effort: 0.25 hours
- Tests: Unit tests T-3.03, T-3.04, T-3.06, T-3.07

**Task 1.3: Update CategoryService**
- Location: `src/SauronSheet.Domain/Services/CategoryService.cs`
- Changes:
  - Remove `userId` parameter from `CreateDefault()` helper
  - Remove `userId` parameter from `GetSystemDefaults()`
  - Add lazy singleton caching with lock
  - Update all 24 category definitions to use `CreateSystemDefault()`
- Effort: 1 hour
- Tests: Unit tests T-3.13, T-3.14

**Task Set 2: Domain Tests**
- Location: `tests/SauronSheet.Domain.Tests/Categories/`
- Files: CategoryNullableUserIdTests.cs, CategoryServiceCachingTests.cs
- Test count: 8 tests (T-3.01 through T-3.07, T-3.13, T-3.14)
- Coverage: NULL semantics, caching, helper methods, invariants
- Effort: 1 hour
- Acceptance: All 8 tests green, 100% domain coverage maintained

**Domain Layer Total Effort: 2.75 hours**

---

### Infrastructure Layer Changes

**Task Set 3: Database Migration**

**Task 3.1: Create migration SQL file**
- Location: `src/SauronSheet.Infrastructure/Persistence/Migrations/004_SystemCategoriesGlobalScope.sql`
- Steps:
  1. ALTER TABLE categories: DROP NOT NULL on user_id
  2. DROP old UNIQUE index (idx_categories_user_id_name)
  3. CREATE new UNIQUE index (exclude NULL rows)
  4. ADD CHECK constraint (NULL ⟹ IsSystemDefault)
  5. INSERT 24 system categories with NULL user_id + NOW()
  6. CREATE composite index for system queries
- Effort: 0.75 hours
- Acceptance: Migration applies without errors, idempotent

**Task 3.2: Create rollback migration**
- Location: `src/SauronSheet.Infrastructure/Persistence/Migrations/005_RevertSystemCategoriesGlobalScope.sql`
- Steps:
  1. DELETE FROM categories WHERE user_id IS NULL AND is_system_default = true
  2. DROP UNIQUE index (idx_categories_user_name_unique)
  3. CREATE old UNIQUE index (user_id, name)
  4. ALTER TABLE categories: SET user_id NOT NULL
  5. DROP composite index (idx_categories_is_system_default)
  6. DROP CHECK constraint (chk_null_user_implies_system_default)
- Effort: 0.5 hours
- Acceptance: Rollback executes successfully, user categories preserved

**Task Set 4: Repository Updates**

**Task 4.1: Update CategoryRow mapping**
- Location: `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs`
- Changes:
  - Update `CategoryRow.UserId` property to `string?` (nullable)
  - Update `ToDomain()` method to handle NULL user_id
  - Create `UserId?` conditionally: `string.IsNullOrEmpty(UserId) ? null : new UserId(UserId)`
  - Use `Category.CreateSystemDefault()` for system categories
- Effort: 0.5 hours
- Tests: Integration tests T-3.08, T-3.09, T-3.10, T-3.11, T-3.12

**Task 4.2: Update GetByUserIdAsync()**
- Location: `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs`
- Changes:
  - Update query: `.Where(x => x.UserId == userId.Value || x.UserId == null)`
  - Add `.Order("name", Ascending)` for consistent results
  - Update mapping to handle both user + system categories
- Effort: 0.5 hours
- Tests: Integration test T-3.10 (single query verification)

**Task 4.3: Add FindByNameAsync()**
- Location: `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs`
- New method:
  - Signature: `public async Task<Category?> FindByNameAsync(string name)`
  - Query: `.Where(x => x.Name == name)` (NO user_id filter)
  - Returns first match (system or user category)
  - Used in CreateCategoryCommand validation
- Effort: 0.25 hours
- Tests: Integration tests T-3.11, T-3.12

**Task 4.4: Update GetSystemDefaultsAsync()**
- Location: `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs`
- Changes:
  - Remove `userId` parameter from method signature
  - Change query: `.Where(x => x.UserId == null && x.IsSystemDefault == true)`
  - Note: This is now redundant with CategoryService caching, but kept for potential future use
- Effort: 0.25 hours
- Tests: Could be removed or kept for backward compatibility

**Task Set 5: Infrastructure Tests**

**Task 5.1: Repository NULL handling tests**
- Location: `tests/SauronSheet.Application.Tests/Infrastructure/Persistence/SupabaseCategoryRepositoryNullableUserIdTests.cs`
- Tests: T-3.08, T-3.09, T-3.10, T-3.11, T-3.12
- Coverage:
  - GetByUserIdAsync() returns system + user categories
  - Single query (no extra calls)
  - FindByNameAsync() finds both scopes
- Effort: 1 hour
- Acceptance: All 5 tests green

**Task 5.2: RLS policy verification**
- Location: `tests/SauronSheet.Application.Tests/Infrastructure/RLS/CategoryRLSPolicyTests.cs`
- Tests: T-3.18, T-3.19, T-3.20
- Coverage:
  - SELECT works for user categories (auth.uid() = user_id)
  - SELECT works for system categories (user_id IS NULL)
  - UPDATE/DELETE blocked for system categories
- Effort: 1.5 hours
- Acceptance: All 3 tests pass (may require test Supabase instance)

**Task 5.3: Migration verification**
- Location: `tests/SauronSheet.Application.Tests/Infrastructure/Migrations/SystemCategoriesMigrationTests.cs`
- Tests:
  - Migration applies successfully
  - 24 system categories inserted correctly
  - Migration is idempotent
  - CHECK constraint enforced
  - Indexes created
- Effort: 1 hour
- Acceptance: All tests pass, migration idempotent

**Infrastructure Layer Total Effort: 7 hours**

---

### Application Layer Changes

**Task Set 6: Handler Updates**

**Task 6.1: CreateTransactionCommandHandler null-checking**
- Location: `src/SauronSheet.Application/Features/Transactions/Commands/CreateTransactionCommandHandler.cs`
- Changes:
  - Find: `if (category.UserId.Value != currentUserId.Value)`
  - Replace: `if (category.UserId != null && category.UserId.Value != currentUserId.Value)`
  - Or use helper: `if (!category.IsAccessibleToUser(currentUserId))`
- Effort: 0.25 hours
- Tests: Integration test T-3.16

**Task 6.2: UpdateTransactionCategoryCommandHandler null-checking**
- Location: `src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCategoryCommandHandler.cs` (if exists)
- Same pattern as 6.1
- Effort: 0.25 hours
- Tests: Integration test T-3.17

**Task 6.3: DeleteTransactionCommandHandler null-checking**
- Location: `src/SauronSheet.Application/Features/Transactions/Commands/DeleteTransactionCommandHandler.cs` (if exists)
- Same pattern as 6.1
- Effort: 0.25 hours
- Tests: Same as 6.1, 6.2

**Task 6.4: CreateCategoryCommand validation**
- Location: `src/SauronSheet.Application/Features/Categories/Commands/CreateCategoryCommandHandler.cs`
- Changes:
  - Add new validation: `var duplicate = await _categoryRepository.FindByNameAsync(request.Name);`
  - Check both hardcoded system defaults AND database
  - Throw DomainException if duplicate found
- Effort: 0.5 hours
- Tests: Integration test T-3.15

**Task Set 7: Application Tests**

**Task 7.1: Handler integration tests**
- Location: `tests/SauronSheet.Application.Tests/Features/Categories/`
- Files: CreateTransactionCommandHandlerNullableUserIdTests.cs, CreateCategoryCommandValidationTests.cs
- Tests: T-3.15, T-3.16, T-3.17
- Coverage:
  - CreateCategoryCommand rejects system default names
  - Handlers allow access to system categories
  - Safe null-checking prevents NullReferenceException
- Effort: 1.5 hours
- Acceptance: All 3 tests green

**Application Layer Total Effort: 3 hours**

---

## Phase 2: Implementation Order & Dependencies

### Execution Sequence (Dependency-ordered)

```
┌─────────────────────────────────────────────────────┐
│ DOMAIN LAYER (2.75 hours)                           │
│ ├─ Task 1.1: Make UserId nullable                   │
│ ├─ Task 1.2: Add helper methods                     │
│ ├─ Task 1.3: Update CategoryService caching         │
│ └─ Task 2.x: Domain tests (8 tests)                 │
│                                                      │
│ INFRASTRUCTURE LAYER (7 hours)                      │
│ ├─ Task 3.1: Create migration SQL                   │
│ ├─ Task 3.2: Create rollback migration              │
│ ├─ Task 4.1: Update CategoryRow mapping             │
│ ├─ Task 4.2: Update GetByUserIdAsync() union query  │
│ ├─ Task 4.3: Add FindByNameAsync()                  │
│ ├─ Task 4.4: Update GetSystemDefaultsAsync()        │
│ └─ Task 5.x: Infrastructure tests (9 tests)         │
│                                                      │
│ APPLICATION LAYER (3 hours)                         │
│ ├─ Task 6.1-6.3: Handler null-checking              │
│ ├─ Task 6.4: CreateCategoryCommand validation       │
│ └─ Task 7.x: Application tests (3 tests)            │
│                                                      │
│ TESTING & VERIFICATION (4 hours)                    │
│ ├─ Full test suite run (20 tests)                   │
│ ├─ Performance profiling (caching)                  │
│ ├─ Regression testing (Feature 2 functionality)     │
│ └─ Staging validation (blue-green strategy)         │
│                                                      │
│ TOTAL: 19.75 hours (~5 days with 2 engineers)       │
└─────────────────────────────────────────────────────┘
```

### Critical Path Analysis

**Blocker 1: Domain changes**
- All other layers depend on nullable UserId
- Must complete before infrastructure/application changes
- Effort: 2.75 hours

**Blocker 2: Database migration**
- Repository changes depend on migration being designed
- Must be finalized before repository implementation
- Effort: 1.25 hours

**Blocker 3: Repository updates**
- Application handlers depend on union query implementation
- Must complete before handler updates
- Effort: 1.5 hours

**Non-blocking:**
- Domain tests can run in parallel with domain implementation
- Infrastructure tests can run in parallel with repository changes
- Application tests depend only on handlers (no infrastructure mocking needed)

---

## Phase 3: Quality Gates & Verification

### Pre-Implementation Checklist

- [ ] Spec.md reviewed and approved by 2+ stakeholders
- [ ] CLARIFICATION_ANSWERS.md reviewed (23 Q&A pairs)
- [ ] Architecture diagram created (before/after)
- [ ] Rollback plan verified with DBA

### Code Review Checkpoints

**Checkpoint 1: Domain Layer (after Task 2.x)**
- [ ] Nullable UserId properly constrained
- [ ] Domain invariant validation in place
- [ ] Helper methods semantically clear
- [ ] 8 domain tests all green
- [ ] 100% domain coverage maintained

**Checkpoint 2: Infrastructure Layer (after Task 5.x)**
- [ ] Migration SQL idempotent + reversible
- [ ] Repository union query tested
- [ ] CategoryRow mapping handles NULL correctly
- [ ] 9 infrastructure tests all green
- [ ] RLS policies verified

**Checkpoint 3: Application Layer (after Task 7.x)**
- [ ] Safe null-checking pattern consistent
- [ ] Validation logic validates both scopes
- [ ] 3 application tests all green
- [ ] No breaking changes to handler signatures

### Pre-Deployment Testing

**Test Execution Sequence:**
1. Unit tests (Domain): `dotnet test --filter "Category=Domain"` → PASS
2. Integration tests (Infrastructure): `dotnet test --filter "Category=Infrastructure"` → PASS
3. Integration tests (Application): `dotnet test --filter "Category=Application"` → PASS
4. Full regression suite: `dotnet test` → PASS (all existing tests still pass)
5. Performance profiling:
   - GetSystemDefaults() called 100× → Should use cached instance only
   - GetByUserIdAsync() query plan reviewed → Index utilization checked

### Staging Deployment

**Blue-Green Validation:**
1. Deploy new application code to staging
2. Run migration 004_SystemCategoriesGlobalScope.sql
3. Verify system categories visible to all test users
4. Run full smoke test suite
5. Monitor error rates (expect ZERO errors)
6. ✅ If all pass: Approve for production

### Rollback Trigger

If any of the following occur, execute rollback immediately:

- NullReferenceException logged (safe null-checking failed)
- System categories duplicated in UI (union query failed)
- RLS policy blocks legitimate queries (auth issue)
- Performance regression > 20% (query plan changed)
- User reports data loss (migrations issue)

**Rollback procedure:**
1. Execute 005_RevertSystemCategoriesGlobalScope.sql
2. Redeploy previous application code
3. Verify Feature 2 behavior restored
4. Post-mortem analysis

---

## Phase 4: Documentation & Handoff

### Developer Documentation

**Document 1: Architecture Update**
- Location: `docs/architecture/domain-layer-system-categories.md`
- Content:
  - Before/after architecture diagrams
  - Nullable UserId pattern explanation
  - Union query semantics
  - Caching strategy

**Document 2: Developer Guide**
- Location: `docs/guides/system-categories-refactoring.md`
- Content:
  - How to add new system categories (update migration + CategoryService)
  - How to query system categories (GetByUserIdAsync() union)
  - How to validate category names (FindByNameAsync() global search)
  - Safe null-checking pattern

**Document 3: Deployment Runbook**
- Location: `docs/deployment/feature-3-deployment.md`
- Content:
  - Blue-green deployment steps
  - Monitoring checklist
  - Rollback procedure

### Code Comments

**In-code Comments Added:**
- Category.cs: Explain nullable UserId semantics
- CategoryService.cs: Explain caching strategy & thread-safety
- SupabaseCategoryRepository.cs: Explain union query design
- Handlers: Explain safe null-checking pattern

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Migration fails in production | LOW | CRITICAL | Test migration on prod data copy; have rollback |
| NullReferenceException in handlers | MEDIUM | HIGH | Comprehensive null-checking tests + code review |
| System categories duplicated in queries | LOW | MEDIUM | Query plan validation + test verification |
| Performance regression | LOW | MEDIUM | Caching strategy + profiling |
| User data loss | VERY LOW | CRITICAL | Rollback migration uses DELETE (reversible) |

---

## Success Metrics

| Metric | Target | Verification |
|--------|--------|--------------|
| All tests pass | 20/20 green | `dotnet test` exit code 0 |
| No breaking changes | 100% Feature 2 compat | Regression test suite passes |
| System defaults cached | 1 instance/lifetime | Profiling shows single allocation |
| Zero user-facing changes | 0 UX regressions | Manual UI testing |
| Zero downtime deploy | 0 seconds | Staging blue-green test |
| Migration idempotent | Re-run safe | Migration executed twice on staging |

---

## Effort Estimation

| Phase | Hours | FTE Days | Notes |
|-------|-------|----------|-------|
| Domain Layer | 2.75 | 0.35 | Including 8 unit tests |
| Infrastructure Layer | 7 | 0.9 | Including migration + 9 integration tests |
| Application Layer | 3 | 0.4 | Including 3 handler updates + 3 tests |
| Testing & Verification | 4 | 0.5 | Full suite + performance + regression |
| Documentation | 2 | 0.25 | Dev guide + runbook |
| **TOTAL** | **18.75** | **2.4** | **5 days @1 engineer or 2.5 days @2 engineers** |

---

## Team & Assignments

**Recommended Team:**
- **Senior Engineer (Lead)**: Domain layer + Architecture validation
- **Mid Engineer**: Infrastructure layer + Migrations
- **QA**: Test strategy + Regression testing
- **Tech Lead**: Code review + Risk mitigation

**Or alternatively:**
- **Single Engineer (Full-Stack)**: All phases sequentially
- Effort: 5 consecutive days
- Risk: Single point of failure (mitigation: pair on critical sections)

---

## Timeline

| Week | Activity | Status |
|------|----------|--------|
| Feb 19 | Spec finalized + clarifications resolved | ✅ COMPLETE |
| Feb 20 | Plan approved + task generation | ⏳ THIS WEEK |
| Feb 21-22 | Implementation: Domain + Infrastructure | ⏰ NEXT WEEK |
| Feb 23 | Implementation: Application + Testing | ⏰ NEXT WEEK |
| Feb 24 | Staging validation + Documentation | ⏰ NEXT WEEK |
| Feb 25 | Production deployment (blue-green) | ⏰ NEXT WEEK |

---

## Assumptions

1. ✅ Feature 2 system categories NOT persisted (code audit confirmed)
2. ✅ Supabase test instance available for RLS testing
3. ✅ No concurrent migrations (feature branch isolation)
4. ✅ Rollback plan acceptable to stakeholders
5. ✅ Zero downtime deployment possible (safe null-checking)

---

## Next Steps

1. ✅ Review and approve this plan
2. ⏳ Generate detailed task list (`tasks.md`) from this plan
3. ⏳ Assign tasks to team members
4. ⏳ Begin implementation following execution sequence

---

_Implementation Plan Complete: 2026-02-19_  
_Status: Ready for Task Generation via /speckit.tasks_  
_Estimated Effort: 18.75 hours | Recommended Timeline: 5 days_
