# Feature Specification: System Categories - Global Scope Refactoring

**Feature Branch**: `003-system-categories-global-scope`  
**Created**: 2026-02-19  
**Status**: Ready for Clarification  
**Type**: Infrastructure/Domain Refactoring  

---

## Quick Reference

- **Scope Layers**: Domain + Infrastructure + Application (Minimal Changes)
- **Current State**: System categories belong to `user_id = 'system'` sentinel value
- **Target State**: System categories are global (NULL user_id) and automatically available to all users
- **Impact**: Database schema change, Repository query logic, Category entity behavior
- **Breaking Change**: YES — affects how categories are queried and persisted

---

## Critical Problem Statement

### Current Architecture (Feature 2)

```sql
-- Current: system categories have user_id = 'system'
INSERT INTO categories (id, user_id, name, type, color, icon_name, is_system_default)
VALUES ('uuid', 'system', 'Salary', 'Income', '#27AE60', 'building-dollar', true);

-- Current: query returns both user categories AND system categories
SELECT * FROM categories 
WHERE user_id = @userId OR user_id = 'system';
```

**Issues with Current Design:**
1. `'system'` is a magic string sentinel value — violates DDD (domain concepts should be explicit)
2. Every query must check `WHERE user_id = @userId OR user_id = 'system'` — error-prone
3. System categories are "owned" by nobody — confusing semantics
4. Row-level security (RLS) becomes complex: "users can view own categories OR system categories"
5. Supabase RLS policies harder to reason about (two conditions instead of one)

### Target Architecture (This Feature)

```sql
-- Target: system categories have NULL user_id (indicating global scope)
INSERT INTO categories (id, user_id, name, type, color, icon_name, is_system_default)
VALUES ('uuid', NULL, 'Salary', 'Income', '#27AE60', 'building-dollar', true);

-- Target: queries are cleaner
-- For user_id = @userId:
--   User-specific: WHERE user_id = @userId
--   System: WHERE user_id IS NULL
-- Combined: WHERE user_id = @userId OR user_id IS NULL

-- Target: RLS is cleaner
CREATE POLICY "Users see own + system categories" ON categories
    FOR SELECT USING (auth.uid() = user_id OR user_id IS NULL);
```

**Advantages:**
1. ✅ Explicit NULL semantics: "no owner = system/global"
2. ✅ Queries clearer: `IS NULL` vs magic string `'system'`
3. ✅ DDD-compliant: domain model uses proper abstractions
4. ✅ RLS simpler and more secure
5. ✅ Scales better: adding more "scopes" (e.g., team categories) in future uses same pattern

---

## Executive Summary (In Scope / Deferred)

### In Scope

1. **Database Migration**
   - Update `categories` table: change `user_id` from non-nullable to nullable
   - Migrate existing `user_id = 'system'` records to `user_id = NULL`
   - Update RLS policies to reflect new logic

2. **Domain Layer Changes**
   - `Category` entity: support `UserId` as nullable (optional property)
   - `CategoryService.GetSystemDefaults()`: return categories with null UserId
   - Repository interface: update signatures if needed (minimal change)

3. **Application Layer Changes**
   - `SupabaseCategoryRepository.GetByUserIdAsync()`: union query (user-specific + system)
   - Handler logic: no changes needed (repository handles the union transparently)

4. **Infrastructure Queries**
   - `GetAllCategoriesQuery`: filter results to remove duplicates (if returned by repository)
   - All existing handlers: no changes (backward compatible)

5. **Tests**
   - Update tests to verify NULL user_id semantics
   - Verify RLS policies work correctly
   - Verify system categories are globally visible

### Deferred

| Item | Target | Reason |
|------|--------|--------|
| Team categories (team_id instead of user_id) | Post-MVP | Not needed for MVP |
| Shared categories between users | Post-MVP | Not needed for MVP |
| Category inheritance/hierarchies | Post-MVP | Out of scope |

---

## Architecture & Implementation Details

### Domain Layer

**Current Category Entity:**
```csharp
public class Category : AggregateRoot<CategoryId>
{
    public UserId UserId { get; private set; }  // Required, always set
    public bool IsSystemDefault { get; private set; }
    // ... other properties
}
```

**Target Category Entity:**
```csharp
public class Category : AggregateRoot<CategoryId>
{
    public UserId? UserId { get; private set; }  // Nullable: null = system/global
    public bool IsSystemDefault { get; private set; }
    
    // New helper methods:
    public bool IsGlobal => UserId is null;      // True if system/global
    public bool IsUserScoped => UserId is not null;  // True if user-specific
}
```

**Category Service Changes:**
```csharp
public class CategoryService
{
    // GetSystemDefaults now creates categories with NULL UserId
    public IReadOnlyList<Category> GetSystemDefaults() => new List<Category>
    {
        new Category(
            new CategoryId(Guid.NewGuid()),
            userId: null,  // ← KEY CHANGE
            new CategoryName("Salary"),
            CategoryType.Income,
            new ColorHex("#27AE60"),
            "building-dollar",
            isSystemDefault: true,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        ),
        // ... 23 more
    };
}
```

### Infrastructure Layer

**Database Migration:**
```sql
-- Step 1: Migrate system categories
UPDATE public.categories
SET user_id = NULL
WHERE user_id = 'system';

-- Step 2: Make user_id nullable
ALTER TABLE public.categories 
ALTER COLUMN user_id DROP NOT NULL;

-- Step 3: Update UNIQUE constraint (exclude NULL)
-- Current: UNIQUE(user_id, name)
-- Target: Allow multiple NULL user_ids (system categories)
DROP CONSTRAINT categories_user_id_name_key;
CREATE UNIQUE INDEX idx_categories_user_name_unique 
  ON public.categories(user_id, name) 
  WHERE user_id IS NOT NULL;  -- Only enforce uniqueness for user-scoped

-- Step 4: Update RLS policies
DROP POLICY "Users see own categories" ON public.categories;
CREATE POLICY "Users see own + system categories" ON public.categories
    FOR SELECT USING (auth.uid() = user_id OR user_id IS NULL);

CREATE POLICY "Users can modify own categories only" ON public.categories
    FOR UPDATE USING (auth.uid() = user_id);

CREATE POLICY "Users can delete own categories only" ON public.categories
    FOR DELETE USING (auth.uid() = user_id);
```

**Repository Implementation:**
```csharp
public class SupabaseCategoryRepository : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId)
    {
        // Return both user-specific AND system categories
        var categories = await _supabaseClient
            .From<CategoryRow>("categories")
            .Where(c => c.UserId == userId.Value || c.UserId == null)  // ← KEY CHANGE
            .Get();
        
        return categories
            .Select(ToDomain)
            .ToList()
            .AsReadOnly();
    }

    // Other methods unchanged:
    // - GetByIdAsync: works as-is
    // - FindByNameAndUserAsync: filter by user AND unique user-scoped names
    // - AddAsync: accepts null UserId
    // - UpdateAsync: only if user_id matches or null (for system categories)
    // - DeleteAsync: only if user_id matches
}
```

**Key Query Change:**
```sql
-- Repository generates this query:
SELECT * FROM categories
WHERE user_id = @userId 
   OR user_id IS NULL
ORDER BY (CASE WHEN user_id IS NULL THEN 0 ELSE 1 END), name;
```

### Application Layer

**Minimal Changes:**
- All existing handlers work unchanged (repository handles the logic)
- `GetAllCategoriesQuery`: repository returns union of user + system, no additional logic needed
- `CreateCategoryCommand`: can no longer create system categories (checks IsSystemDefault)

**Validation in CreateCategoryCommand:**
```csharp
public async Task<CategoryId> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
{
    // Existing validation
    await _categoryService.ValidateUniqueName(request.UserId, request.Name);
    
    // NEW: Prevent duplicate names with system categories
    var systemDefaults = _categoryService.GetSystemDefaults();
    if (systemDefaults.Any(c => c.Name.Value.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        throw new DomainException($"Category name '{request.Name}' is reserved for system defaults.");
    
    // ... rest unchanged
}
```

---

## User Impact

### Before (Current Feature 2)
```
User A views categories:
├── Salary (System)
├── Housing (System)
├── My Coffee Shop (Custom)
└── ...

Database Query:
WHERE user_id = 'user-a-uuid' OR user_id = 'system'
```

### After (This Refactoring)
```
User A views categories:
├── Salary (System)      ← from NULL user_id
├── Housing (System)     ← from NULL user_id
├── My Coffee Shop (Custom) ← from user_id = 'user-a-uuid'
└── ...

Database Query:
WHERE user_id = 'user-a-uuid' OR user_id IS NULL
```

**User-facing change:** ❌ ZERO — behavior identical, UX unchanged

---

## Test Specifications

| Test ID | Test Name | Category | Assert |
|---------|-----------|----------|--------|
| T-3.01 | Category_SystemDefault_HasNullUserId | Domain | UserId is null for system categories |
| T-3.02 | Category_UserScoped_HasNonNullUserId | Domain | UserId is non-null for user categories |
| T-3.03 | Category_IsGlobal_ReturnsTrueForNull | Domain | IsGlobal returns true when UserId null |
| T-3.04 | Category_IsUserScoped_ReturnsTrueForNonNull | Domain | IsUserScoped returns true when UserId non-null |
| T-3.05 | GetByUserIdAsync_ReturnsSystemCategories | Infrastructure | System categories (NULL user_id) returned |
| T-3.06 | GetByUserIdAsync_ReturnsUserCategories | Infrastructure | User-specific categories returned |
| T-3.07 | GetByUserIdAsync_NoSeparateFetch | Infrastructure | Single query (no extra calls for system) |
| T-3.08 | GetSystemDefaults_CreatesWithNullUserId | Domain | All 24 system categories have null UserId |
| T-3.09 | CreateCategoryCommand_RejectsSystemDefaultName | Application | DomainException when name matches system default |
| T-3.10 | RLS_UserCanViewOwnCategories | Infrastructure | SELECT works for user's own categories |
| T-3.11 | RLS_UserCanViewSystemCategories | Infrastructure | SELECT works for system categories (NULL) |
| T-3.12 | RLS_UserCannotModifySystemCategories | Infrastructure | UPDATE/DELETE fails for system (NULL) categories |

---

## Deliverables

| # | Deliverable | Layer | Acceptance |
|---|-------------|-------|-----------|
| D-3.01 | Database migration | Infrastructure | Migration file created, applies successfully |
| D-3.02 | Category entity (nullable UserId) | Domain | New IsGlobal/IsUserScoped properties |
| D-3.03 | CategoryService.GetSystemDefaults() | Domain | Returns categories with null UserId |
| D-3.04 | SupabaseCategoryRepository updated | Infrastructure | GetByUserIdAsync returns union |
| D-3.05 | RLS policies updated | Infrastructure | Policies enforce null checks |
| D-3.06 | All tests passing | Tests | 12+ tests green |
| D-3.07 | CreateCategoryCommand validation | Application | Rejects system default names |
| D-3.08 | No breaking changes | Application | All existing handlers work unchanged |

---

## Success Criteria

| # | Criterion | Metric |
|---|-----------|--------|
| SC-3.1 | System categories visible to all users without duplication | E2E: user views categories, sees system defaults once |
| SC-3.2 | Database schema is cleaner (no magic strings) | Code review: no 'system' sentinel in queries |
| SC-3.3 | RLS policies simpler and more secure | Audit: `IS NULL` logic verified |
| SC-3.4 | Zero user-facing changes | Regression test: existing behavior preserved |
| SC-3.5 | All domain/infra tests pass | `dotnet test` exit 0 |
| SC-3.6 | Database migration is reversible | Rollback tested |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Migration fails in production | CRITICAL | Test migration on copy of prod data; have rollback plan |
| RLS policies break existing queries | CRITICAL | Test RLS with actual auth users before deploying |
| System categories duplicated in queries | HIGH | Repository test verifies no duplicates |
| Performance regression on GetByUserIdAsync | MEDIUM | Query plan review; index on (user_id, is_system_default) |
| Backward compatibility issue | MEDIUM | All handlers tested against new schema |

---

## Execution Plan

### Phase 1: Specification & Design (CURRENT)
- ✅ Write this spec
- ✅ Design domain/infra changes
- ✅ Review with team

### Phase 2: Implementation (Next)
- Day 1: Update Category entity, CategoryService, tests
- Day 2: Update SupabaseCategoryRepository, RLS policies, migration
- Day 3: Update Application handlers, integration tests, E2E validation
- Day 4: Regression testing, documentation

### Phase 3: Deployment
- Backup production database
- Apply migration to staging
- Run full test suite against staging
- Deploy migration to production
- Verify system categories visible to all users

---

## Architecture Consistency

### Constitution Alignment
- ✅ **Clean Architecture**: Domain change (nullable UserId) isolated; no layer crossings
- ✅ **DDD**: Explicit `IsGlobal` vs `IsUserScoped` properties (not magic strings)
- ✅ **CQRS**: Repository layer handles query complexity transparently
- ✅ **Test-First**: All changes covered by tests before implementation
- ✅ **Spec-Driven**: Clear before/after specification

### Backward Compatibility
- ✅ All existing handlers work unchanged
- ✅ GetAllCategoriesQuery result is identical (same categories returned)
- ✅ UI layer requires zero changes
- ✅ User experience unchanged

---

## Timeline & Effort

| Phase | Duration | Status |
|-------|----------|--------|
| Spec Review | 1 day | 🟡 PENDING |
| Implementation | 2 days | ⏰ NOT STARTED |
| Testing & QA | 1 day | ⏰ NOT STARTED |
| Deployment | 1 day | ⏰ NOT STARTED |
| **TOTAL** | **5 days** | |

---

_Last Updated: 2026-02-19 | Status: Ready for Clarification Review_
