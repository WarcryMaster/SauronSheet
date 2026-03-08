# Task Specifications: Feature 3 - System Categories Global Scope Refactoring

**Branch**: `003-system-categories-global-scope` | **Date**: 2026-02-19  
**Total Tasks**: 29 | **Total Effort**: 20.75 hours | **Status**: Ready for Assignment  
**Prerequisite**: Review [plan.md](plan.md) and [spec.md](spec.md)

---

## CRITICAL DECISION GATE

**⚠️ DECISION REQUIRED BEFORE STARTING TASKS**

This entire plan assumes **Option A: Migration-Only Seeding Strategy**:
- ✅ Migration 004 inserts 24 system categories
- ✅ SeedSystemDefaultsCommandHandler REMOVED (Task 6.5)
- ✅ GetCategoriesQueryHandler call REMOVED

**If your team chooses Option B (Hybrid):**
- Task 6.5 becomes: "Refactor SeedSystemDefaultsCommandHandler" instead of removal
- Effort remains: +0.5h

**Confirmation**: ☐ Option A (Recommended) | ☐ Option B (Hybrid)

---

## Task Organization & Dependencies

```
DOMAIN LAYER (Tasks 1.1-2.10)
  └─ Blocking all other layers
  └─ Must be 100% complete before Infrastructure tasks start

INFRASTRUCTURE LAYER (Tasks 3.1-5.4)
  └─ Depends on: Domain Layer complete
  └─ Can parallelize: Tests (5.1-5.4) while implementing (3.1-4.4)
  └─ Blocking: Application Layer

APPLICATION LAYER (Tasks 6.1-7.1)
  └─ Depends on: Infrastructure Layer complete
  └─ Can parallelize after 4.4 complete

TESTING & VERIFICATION (Tasks 8.1-8.4)
  └─ Parallel: Throughout implementation
  └─ Final: Full regression & staging validation
```

---

## PHASE 1: DOMAIN LAYER (29 hours estimated)

### Task 1.1: Make Category.UserId Nullable

**Effort**: 0.5 hours | **Assigned to**: Senior Engineer  
**Depends on**: None  
**Blocks**: Tasks 1.2, 1.3, 4.1, 6.1-6.3

**Description**:
Update `Category` entity to support nullable `UserId`. This is the foundational change that all other layers depend on.

**Files to Create/Modify**:
- `src/SauronSheet.Domain/Entities/Category.cs` (MODIFY)

**Acceptance Criteria**:
- [ ] `UserId` property changed from `UserId` to `UserId?`
- [ ] Private constructor accepts `UserId? userId` parameter
- [ ] Public constructor keeps `UserId userId` (non-nullable)
- [ ] Add `CreateSystemDefault()` static factory method (no userId param)
- [ ] Domain invariant added: `if (userId == null && !isSystemDefault) throw DomainException`
- [ ] Code compiles without errors
- [ ] No breaking changes to public API (public constructor untouched)

**Implementation Notes**:
```csharp
// Pattern:
// Private constructor: Category(id, userId?, name, type, color, icon, isSystemDefault)
// Public constructor: Category(id, userId, name, type, color, icon) → calls private with userId, false
// Factory: CreateSystemDefault(id, name, type, color, icon) → calls private with null, true
```

**Testing**:
- Compile-time verification only (unit tests in Task 2.x)

---

### Task 1.2: Add Category Helper Methods

**Effort**: 0.25 hours | **Assigned to**: Senior Engineer  
**Depends on**: Task 1.1  
**Blocks**: Tasks 6.1-6.3

**Description**:
Add semantic helper methods to Category entity for clearer intent in handlers.

**Files to Create/Modify**:
- `src/SauronSheet.Domain/Entities/Category.cs` (MODIFY - same file as 1.1)

**Methods to Add**:
```csharp
public bool IsGlobal => UserId is null;
public bool IsUserScoped => UserId is not null;
public bool IsOwnedByUser(UserId userId)
{
    return UserId != null && UserId.Value == userId.Value;
}
public bool IsAccessibleToUser(UserId userId)
{
    return IsSystemDefault || IsOwnedByUser(userId);
}
```

**Acceptance Criteria**:
- [ ] All 4 methods added to Category entity
- [ ] Methods are public
- [ ] `IsGlobal` and `IsUserScoped` are properties (not methods)
- [ ] Logic is correct (see above)
- [ ] No external dependencies
- [ ] Code compiles
- [ ] Methods documented (inline XML comments optional)

**Testing**:
- Compile-time verification only (unit tests in Task 2.x)

---

### Task 1.3: Update CategoryService (Nullable Strategy)

**Effort**: 1.5 hours | **Assigned to**: Senior Engineer  
**Depends on**: Task 1.1  
**Blocks**: Tasks 4.4, 5.3, 6.4

**Description**:
Update CategoryService to remove userId parameter and add caching. This includes cascading updates to SeedSystemDefaultsCommandHandler (if following Option A).

**Files to Create/Modify**:
- `src/SauronSheet.Domain/Services/CategoryService.cs` (MODIFY)
- `src/SauronSheet.Application/Features/Categories/Commands/SeedSystemDefaultsCommandHandler.cs` (MODIFY - cascade)

**Changes to CategoryService**:
1. Add static cache field and lock:
   ```csharp
   private static IReadOnlyList<Category>? _cachedSystemDefaults;
   private static readonly object _cacheLock = new();
   ```

2. Update `GetSystemDefaults()` signature:
   ```csharp
   // BEFORE: public IReadOnlyList<Category> GetSystemDefaults(UserId userId)
   // AFTER: public IReadOnlyList<Category> GetSystemDefaults()
   ```

3. Update `CreateDefault()` helper:
   ```csharp
   // BEFORE: private static Category CreateDefault(UserId userId, string name, ...)
   // AFTER: private static Category CreateDefault(string name, ...)
   //        - Remove userId parameter
   //        - Call: Category.CreateSystemDefault(...)
   ```

4. Implement lazy caching in `GetSystemDefaults()`:
   ```csharp
   if (_cachedSystemDefaults != null) return _cachedSystemDefaults;
   lock (_cacheLock) { ... build categories ... }
   ```

**Cascade Changes to SeedSystemDefaultsCommandHandler**:
- Update line ~38: `await _categoryRepo.GetSystemDefaultsAsync(userId);` → `await _categoryRepo.GetSystemDefaultsAsync();`
- Update idempotency check: `if (existingDefaults.Count == 4)` → `if (existingDefaults.Count == 24)`
- Add comment: "NOTE: Task 6.5 may remove this handler entirely (Option A)"

**Acceptance Criteria**:
- [ ] GetSystemDefaults() has no parameters
- [ ] Caching implemented with thread-safety (lock)
- [ ] All 24 system categories created using CreateSystemDefault()
- [ ] CreateDefault() helper updated (no userId)
- [ ] SeedSystemDefaultsCommandHandler updated (cascade)
- [ ] Code compiles
- [ ] Caching logic verified (static field properly initialized)
- [ ] No breaking changes to CategoryService public API except GetSystemDefaults signature

**Testing**:
- Unit tests in Task 2.x (T-3.13, T-3.14, idempotency check)

---

### Task 1.4: Domain Layer Unit Tests (8 tests)

**Effort**: 1 hour | **Assigned to**: QA Engineer  
**Depends on**: Tasks 1.1, 1.2, 1.3  
**Blocks**: Code Review Checkpoint 1

**Description**:
Implement unit tests for Category entity and CategoryService changes.

**Files to Create**:
- `tests/SauronSheet.Domain.Tests/Entities/CategoryNullableUserIdTests.cs` (NEW)
- `tests/SauronSheet.Domain.Tests/Services/CategoryServiceCachingTests.cs` (NEW)

**Test Cases (CategoryNullableUserIdTests.cs)**:

**T-3.01**: `Category_SystemDefault_HasNullUserId`
```csharp
var category = Category.CreateSystemDefault(...);
Assert.Null(category.UserId);
Assert.True(category.IsSystemDefault);
Assert.True(category.IsGlobal);
Assert.False(category.IsUserScoped);
```

**T-3.02**: `Category_UserScoped_HasNonNullUserId`
```csharp
var userId = new UserId("test-user");
var category = new Category(id, userId, name, type, color, icon);
Assert.NotNull(category.UserId);
Assert.Equal("test-user", category.UserId.Value);
Assert.False(category.IsSystemDefault);
Assert.False(category.IsGlobal);
Assert.True(category.IsUserScoped);
```

**T-3.03**: `Category_IsGlobal_ReturnsTrueForNull`
```csharp
var systemCat = Category.CreateSystemDefault(...);
Assert.True(systemCat.IsGlobal);
var userCat = new Category(id, userId, ...);
Assert.False(userCat.IsGlobal);
```

**T-3.04**: `Category_IsUserScoped_ReturnsTrueForNonNull`
```csharp
var userCat = new Category(id, userId, ...);
Assert.True(userCat.IsUserScoped);
var systemCat = Category.CreateSystemDefault(...);
Assert.False(systemCat.IsUserScoped);
```

**T-3.05**: `Category_NullUserIdWithSystemDefaultFalse_ThrowsDomainException`
```csharp
Assert.Throws<DomainException>(() =>
{
    // Use reflection to call private constructor with invalid state
    var ctor = typeof(Category).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];
    ctor.Invoke(new object[] { id, null, name, type, color, icon, false }); // ← isSystemDefault=false
});
```

**T-3.06**: `Category_IsOwnedByUser_ReturnsTrueForOwner`
```csharp
var userId = new UserId("user-123");
var otherUserId = new UserId("user-456");
var category = new Category(id, userId, ...);

Assert.True(category.IsOwnedByUser(userId));
Assert.False(category.IsOwnedByUser(otherUserId));

var systemCat = Category.CreateSystemDefault(...);
Assert.False(systemCat.IsOwnedByUser(userId)); // System categories not "owned"
```

**T-3.07**: `Category_IsAccessibleToUser_AllowsSystemDefault`
```csharp
var userId = new UserId("user-123");
var otherUserId = new UserId("user-456");

var userCat = new Category(id, userId, ...);
var systemCat = Category.CreateSystemDefault(...);

// User can access own category
Assert.True(userCat.IsAccessibleToUser(userId));
// User CANNOT access others' categories
Assert.False(userCat.IsAccessibleToUser(otherUserId));
// User CAN access system categories
Assert.True(systemCat.IsAccessibleToUser(userId));
Assert.True(systemCat.IsAccessibleToUser(otherUserId));
```

**Test Cases (CategoryServiceCachingTests.cs)**:

**T-3.13**: `GetSystemDefaults_CreatesWithNullUserId`
```csharp
var service = new CategoryService();
var defaults = service.GetSystemDefaults();

Assert.Equal(24, defaults.Count);
Assert.All(defaults, cat => 
{
    Assert.Null(cat.UserId);
    Assert.True(cat.IsSystemDefault);
});
```

**T-3.14**: `GetSystemDefaults_CachedAfterFirstCall`
```csharp
var service = new CategoryService();
var defaults1 = service.GetSystemDefaults();
var defaults2 = service.GetSystemDefaults();

// Same instance (proves caching)
Assert.Same(defaults1, defaults2);
```

**Acceptance Criteria**:
- [ ] 8 tests created and passing
- [ ] Tests use xUnit assertions
- [ ] Tests verify NULL semantics correctly
- [ ] Tests verify caching behavior (Same reference check)
- [ ] Tests verify helper methods work as expected
- [ ] 100% of modified domain code covered
- [ ] No external dependencies in tests (no mocking needed for domain)
- [ ] `dotnet test --filter "Category=Domain"` passes

**Running the Tests**:
```bash
cd tests/SauronSheet.Domain.Tests
dotnet test --filter "T-3.01 | T-3.02 | T-3.03 | T-3.04 | T-3.05 | T-3.06 | T-3.07 | T-3.13 | T-3.14"
```

**Code Review Checkpoint 1** (after Task 1.4 complete):
- ☐ Nullable UserId properly constrained
- ☐ Domain invariant validation in place
- ☐ Helper methods semantically clear
- ☐ 8 domain tests all green
- ☐ 100% domain coverage maintained

---

## PHASE 2: INFRASTRUCTURE LAYER (7.5 hours)

### Task 3.1: Create Database Migration SQL (004)

**Effort**: 0.75 hours | **Assigned to**: Senior Engineer (Infrastructure)  
**Depends on**: Domain Layer complete (Task 1.4)  
**Blocks**: Tasks 4.1-4.4, 5.1-5.4

**Description**:
Create the database migration to make user_id nullable, add indexes, CHECK constraint, and insert 24 system categories.

**Files to Create**:
- `src/SauronSheet.Infrastructure/Persistence/Migrations/004_SystemCategoriesGlobalScope.sql` (NEW)

**Migration Content**:

```sql
-- Feature 3: System Categories Global Scope Refactoring
-- Migrates system default categories from in-memory to persisted with NULL user_id

-- Step 1: Make user_id nullable (non-breaking for existing user categories)
ALTER TABLE public.categories ALTER COLUMN user_id DROP NOT NULL;

-- Step 2: Update UNIQUE constraint to exclude NULL rows
-- This allows multiple NULL user_ids (one per system category)
DROP INDEX IF EXISTS idx_categories_user_id_name;
CREATE UNIQUE INDEX idx_categories_user_name_unique 
  ON public.categories(user_id, name) 
  WHERE user_id IS NOT NULL;

-- Step 3: Add CHECK constraint (enforce domain invariant)
-- Ensures NULL user_id can only be used for system categories
ALTER TABLE public.categories 
ADD CONSTRAINT chk_null_user_implies_system_default
  CHECK ((user_id IS NOT NULL) OR (is_system_default = true));

-- Step 4: Insert 24 system default categories with NULL user_id
-- Use ON CONFLICT to make migration idempotent
INSERT INTO public.categories (id, user_id, name, type, color, icon_name, is_system_default, created_at)
VALUES 
  (gen_random_uuid(), NULL, 'Salary', 'Income', '#27AE60', 'building-dollar', true, NOW()),
  (gen_random_uuid(), NULL, 'Sales', 'Income', '#27AE60', 'shopping-bag', true, NOW()),
  (gen_random_uuid(), NULL, 'Investments', 'Income', '#27AE60', 'trending-up', true, NOW()),
  (gen_random_uuid(), NULL, 'Gifts', 'Income', '#27AE60', 'gift', true, NOW()),
  (gen_random_uuid(), NULL, 'Other Income', 'Income', '#27AE60', 'coins', true, NOW()),
  (gen_random_uuid(), NULL, 'Housing', 'Expense', '#E74C3C', 'house', true, NOW()),
  (gen_random_uuid(), NULL, 'Utilities', 'Expense', '#E74C3C', 'lightbulb', true, NOW()),
  (gen_random_uuid(), NULL, 'Groceries', 'Expense', '#E74C3C', 'shopping-cart', true, NOW()),
  (gen_random_uuid(), NULL, 'Transportation', 'Expense', '#E74C3C', 'car', true, NOW()),
  (gen_random_uuid(), NULL, 'Entertainment', 'Expense', '#E74C3C', 'popcorn', true, NOW()),
  (gen_random_uuid(), NULL, 'Dining Out', 'Expense', '#E74C3C', 'utensils', true, NOW()),
  (gen_random_uuid(), NULL, 'Coffee', 'Expense', '#E74C3C', 'coffee', true, NOW()),
  (gen_random_uuid(), NULL, 'Subscription', 'Expense', '#E74C3C', 'bell', true, NOW()),
  (gen_random_uuid(), NULL, 'Insurance', 'Expense', '#E74C3C', 'shield', true, NOW()),
  (gen_random_uuid(), NULL, 'Healthcare', 'Expense', '#E74C3C', 'heart', true, NOW()),
  (gen_random_uuid(), NULL, 'Fitness', 'Expense', '#E74C3C', 'dumbbell', true, NOW()),
  (gen_random_uuid(), NULL, 'Education', 'Expense', '#E74C3C', 'book', true, NOW()),
  (gen_random_uuid(), NULL, 'Shopping', 'Expense', '#E74C3C', 'shopping-bag', true, NOW()),
  (gen_random_uuid(), NULL, 'Gas', 'Expense', '#E74C3C', 'gas-pump', true, NOW()),
  (gen_random_uuid(), NULL, 'Phone', 'Expense', '#E74C3C', 'phone', true, NOW()),
  (gen_random_uuid(), NULL, 'Internet', 'Expense', '#E74C3C', 'wifi', true, NOW()),
  (gen_random_uuid(), NULL, 'Hobbies', 'Expense', '#E74C3C', 'palette', true, NOW()),
  (gen_random_uuid(), NULL, 'Gifts Given', 'Expense', '#E74C3C', 'gift', true, NOW()),
  (gen_random_uuid(), NULL, 'Other Expense', 'Expense', '#E74C3C', 'dots-horizontal', true, NOW())
ON CONFLICT DO NOTHING;

-- Step 5: Add composite index for system category queries
-- Improves performance when filtering by is_system_default=true
CREATE INDEX idx_categories_is_system_default 
  ON public.categories(is_system_default, name)
  WHERE is_system_default = true;
```

**Acceptance Criteria**:
- [ ] Migration file created at correct location
- [ ] SQL syntax valid (can be tested with `psql` if needed)
- [ ] All 5 steps present and correct
- [ ] 24 system categories inserted with correct names/icons/colors
- [ ] CHECK constraint enforces domain invariant
- [ ] UNIQUE index allows NULL user_id
- [ ] Composite index added for performance
- [ ] ON CONFLICT makes migration idempotent
- [ ] Migration can be executed multiple times without error

**Testing**:
- Manual: `psql -f 004_SystemCategoriesGlobalScope.sql` on dev DB
- Integration tests in Tasks 5.3-5.4

---

### Task 3.2: Create Rollback Migration (005)

**Effort**: 0.5 hours | **Assigned to**: Senior Engineer (Infrastructure)  
**Depends on**: Task 3.1  
**Blocks**: Deployment planning

**Description**:
Create the rollback migration to revert Feature 3 changes if needed.

**Files to Create**:
- `src/SauronSheet.Infrastructure/Persistence/Migrations/005_RevertSystemCategoriesGlobalScope.sql` (NEW)

**Migration Content**:

```sql
-- Rollback Feature 3: System Categories Global Scope Refactoring
-- Reverts database to pre-Feature 3 state
-- Safe: Only deletes system categories (NULL user_id), preserves user categories

-- Step 1: Delete system categories (NULL user_id)
DELETE FROM public.categories 
WHERE user_id IS NULL AND is_system_default = true;

-- Step 2: Remove new indexes
DROP INDEX IF EXISTS public.idx_categories_is_system_default;
DROP INDEX IF EXISTS public.idx_categories_user_name_unique;

-- Step 3: Drop CHECK constraint
ALTER TABLE public.categories 
DROP CONSTRAINT IF EXISTS chk_null_user_implies_system_default;

-- Step 4: Recreate old UNIQUE index (user_id, name)
-- Assumes user_id is NOT NULL for user categories
CREATE UNIQUE INDEX idx_categories_user_id_name 
ON public.categories(user_id, name);

-- Step 5: Make user_id NOT NULL again
-- Safe: System categories already deleted in Step 1
ALTER TABLE public.categories 
ALTER COLUMN user_id SET NOT NULL;
```

**Acceptance Criteria**:
- [ ] Rollback migration created at correct location
- [ ] SQL syntax valid
- [ ] All 5 steps present and correctly ordered
- [ ] DELETE statement only removes system categories (safe)
- [ ] User categories preserved (no deletion)
- [ ] Constraints and indexes restored
- [ ] Schema matches pre-Feature 3 state after rollback
- [ ] No data loss for user categories

**Testing**:
- Dry-run: Can review logic without executing
- Full test in Task 5.4

---

### Task 4.1: Update CategoryRow Mapping (Nullable Handling)

**Effort**: 0.5 hours | **Assigned to**: Mid Engineer  
**Depends on**: Task 1.1  
**Blocks**: Task 4.2-4.4, 5.1

**Description**:
Update the CategoryRow ORM mapping class to handle NULL user_id from database, and update the ToDomain() method to correctly map to Category entity.

**Files to Create/Modify**:
- `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs` (MODIFY - CategoryRow inner class + ToDomain method)

**Changes**:

1. **Update CategoryRow property** (inner class):
```csharp
[Column("user_id")]
public string? UserId { get; set; } = null;  // Changed from: = ""
```

2. **Update ToDomain() mapping method**:
```csharp
private Category ToDomain(CategoryRow row)
{
    var categoryType = row.Type == "Income" ? CategoryType.Income : CategoryType.Expense;
    
    // Handle nullable UserId from database
    UserId? userId = string.IsNullOrEmpty(row.UserId) 
        ? null 
        : new UserId(row.UserId);
    
    if (row.IsSystemDefault)
    {
        return Category.CreateSystemDefault(
            new CategoryId(Guid.Parse(row.Id)),
            CategoryName.Create(row.Name),
            categoryType,
            ColorHex.Create(row.Color),
            row.IconName);
    }
    
    return new Category(
        new CategoryId(Guid.Parse(row.Id)),
        userId ?? throw new DomainException($"User category {row.Id} missing user_id"),
        CategoryName.Create(row.Name),
        categoryType,
        ColorHex.Create(row.Color),
        row.IconName);
}
```

**Acceptance Criteria**:
- [ ] CategoryRow.UserId property is nullable (string?)
- [ ] Default value is null (not empty string)
- [ ] ToDomain() handles NULL correctly
- [ ] System categories (IsSystemDefault=true) use CreateSystemDefault()
- [ ] User categories require non-null UserId (throw if missing)
- [ ] Code compiles
- [ ] No runtime errors when mapping NULL values

**Testing**:
- Integration tests in Tasks 5.1, 5.2

---

### Task 4.2: Update GetByUserIdAsync() Union Query

**Effort**: 0.5 hours | **Assigned to**: Mid Engineer  
**Depends on**: Task 4.1, Task 3.1 (migration)  
**Blocks**: Task 5.1, Task 6.1-6.3

**Description**:
Update the GetByUserIdAsync() repository method to return a union of user-specific and system categories in a single query.

**Files to Create/Modify**:
- `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs` (MODIFY - GetByUserIdAsync method)

**Changes**:

```csharp
public async Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId)
{
    var response = await _client.From<CategoryRow>()
        .Where(x => x.UserId == userId.Value || x.UserId == null)  // UNION: user + system
        .Order("name", Constants.Ordering.Ascending)
        .Get();

    return response.Models
        .Select(r => r.ToDomain())
        .ToList()
        .AsReadOnly();
}
```

**Key Points**:
- Single query (no extra calls)
- Filters for user's own categories (x.UserId == userId.Value)
- AND system categories (x.UserId == null)
- Order by name for consistent results

**Acceptance Criteria**:
- [ ] Method signature unchanged (still takes UserId userId)
- [ ] Query uses OR condition: `user_id = @userId OR user_id IS NULL`
- [ ] Results sorted by name
- [ ] Single query execution (verified via logging)
- [ ] Returns both user and system categories
- [ ] No duplicates in results
- [ ] Code compiles

**Testing**:
- Integration test T-3.10 in Task 5.1

---

### Task 4.3: Add FindByNameAsync() Method

**Effort**: 0.25 hours | **Assigned to**: Mid Engineer  
**Depends on**: Task 4.1  
**Blocks**: Task 6.4

**Description**:
Add new repository method to search for categories by name across all scopes (user + system). Used for validation in CreateCategoryCommand.

**Files to Create/Modify**:
- `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs` (MODIFY - add new method)
- `src/SauronSheet.Domain/Repositories/ICategoryRepository.cs` (MODIFY - add interface)

**New Method**:

```csharp
// In SupabaseCategoryRepository
public async Task<Category?> FindByNameAsync(string name)
{
    var response = await _client.From<CategoryRow>()
        .Where(x => x.Name == name)  // NO user_id filter - global search
        .Limit(1)
        .Get();

    return response.Models.FirstOrDefault()?.ToDomain();
}

// In ICategoryRepository interface
Task<Category?> FindByNameAsync(string name);
```

**Acceptance Criteria**:
- [ ] Method added to both repository class and interface
- [ ] Query searches across all categories (no user_id filter)
- [ ] Returns first match or null
- [ ] Works for both system (NULL user_id) and user categories
- [ ] Code compiles
- [ ] Method signature matches interface

**Testing**:
- Integration tests T-3.11, T-3.12 in Task 5.1

---

### Task 4.4: Update GetSystemDefaultsAsync() Signature

**Effort**: 1 hour | **Assigned to**: Mid Engineer  
**Depends on**: Task 1.3 (cascade update), Task 4.1  
**Blocks**: Task 5.3

**Description**:
Update GetSystemDefaultsAsync() to remove userId parameter (breaking change). This method is now less useful (domain service caching primary source), but kept for potential future use.

**Files to Create/Modify**:
- `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs` (MODIFY - GetSystemDefaultsAsync)
- `src/SauronSheet.Domain/Repositories/ICategoryRepository.cs` (MODIFY - interface)
- `src/SauronSheet.Application/Features/Categories/Commands/SeedSystemDefaultsCommandHandler.cs` (CASCADE - verify call site)

**Changes**:

```csharp
// BEFORE
public async Task<IReadOnlyList<Category>> GetSystemDefaultsAsync(UserId userId)
{
    var response = await _client.From<CategoryRow>()
        .Where(x => x.UserId == userId.Value && x.IsSystemDefault == true)
        .Get();
    return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
}

// AFTER
public async Task<IReadOnlyList<Category>> GetSystemDefaultsAsync()
{
    var response = await _client.From<CategoryRow>()
        .Where(x => x.UserId == null && x.IsSystemDefault == true)  // System cats only
        .Get();
    return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
}
```

**Interface Update**:
```csharp
// ICategoryRepository
// BEFORE: Task<IReadOnlyList<Category>> GetSystemDefaultsAsync(UserId userId);
// AFTER: Task<IReadOnlyList<Category>> GetSystemDefaultsAsync();
```

**Cascade Verification**:
- Verify SeedSystemDefaultsCommandHandler has been updated (Task 1.3)
- Line ~38 should call: `await _categoryRepo.GetSystemDefaultsAsync();` (no userId)
- No other call sites should exist

**Acceptance Criteria**:
- [ ] Method signature changed (remove userId) in both class and interface
- [ ] Query changed: WHERE user_id = NULL (not == userId.Value)
- [ ] Call site in SeedSystemDefaultsCommandHandler updated
- [ ] No other call sites found (grep/search for old signature)
- [ ] Code compiles
- [ ] Breaking change documented in comments

**Testing**:
- Compile verification
- Integration test in Task 5.3

---

### Task 5.1: Repository NULL Handling Tests (5 tests)

**Effort**: 1 hour | **Assigned to**: QA Engineer  
**Depends on**: Tasks 4.1, 4.2, 4.3  
**Blocks**: Code Review Checkpoint 2

**Description**:
Implement integration tests for repository changes with NULL user_id handling.

**Files to Create**:
- `tests/SauronSheet.Application.Tests/Infrastructure/Persistence/SupabaseCategoryRepositoryNullableUserIdTests.cs` (NEW)

**Test Cases**:

**T-3.08**: `GetByUserIdAsync_ReturnsSystemCategories`
```csharp
// Setup: Insert 1 user category + assume 24 system categories exist (from migration)
// Act: Call GetByUserIdAsync(userId)
// Assert: Result includes system categories (NULL user_id)
var result = await _repository.GetByUserIdAsync(userId);
Assert.Contains(result, c => c.UserId == null && c.IsSystemDefault);
```

**T-3.09**: `GetByUserIdAsync_ReturnsUserCategories`
```csharp
// Setup: Insert 2 user categories
// Act: Call GetByUserIdAsync(userId)
// Assert: Result includes user's categories
var result = await _repository.GetByUserIdAsync(userId);
Assert.Contains(result, c => c.UserId != null && c.UserId.Value == userId.Value);
```

**T-3.10**: `GetByUserIdAsync_UnionQuerySingleFetch`
```csharp
// Verify single query (no extra round trips)
// Log: Verify only ONE query executed against Supabase
var result = await _repository.GetByUserIdAsync(userId);
// Count queries or use timing - single call should be < 50ms
```

**T-3.11**: `FindByNameAsync_FindsSystemCategory`
```csharp
// Act: Call FindByNameAsync("Salary") [system category]
// Assert: Found, has NULL user_id, isSystemDefault=true
var result = await _repository.FindByNameAsync("Salary");
Assert.NotNull(result);
Assert.Null(result.UserId);
Assert.True(result.IsSystemDefault);
```

**T-3.12**: `FindByNameAsync_FindsUserCategory`
```csharp
// Setup: Create user category
// Act: Call FindByNameAsync(userCategoryName)
// Assert: Found, has non-null user_id
var result = await _repository.FindByNameAsync("My Coffee");
Assert.NotNull(result);
Assert.NotNull(result.UserId);
```

**Acceptance Criteria**:
- [ ] 5 tests created and passing
- [ ] Tests use mocked/in-memory Supabase (xUnit with test double)
- [ ] Union query functionality verified
- [ ] NULL user_id handling verified
- [ ] Single query verified (T-3.10)
- [ ] Both global and user-scoped searches work
- [ ] No errors or null reference exceptions
- [ ] `dotnet test --filter "T-3.08 | T-3.09 | T-3.10 | T-3.11 | T-3.12"` passes

---

### Task 5.2: RLS Policy Verification Tests (3 tests)

**Effort**: 2 hours | **Assigned to**: QA Engineer  
**Depends on**: Task 3.1 (migration applied)  
**Blocks**: Code Review Checkpoint 2

**Description**:
Test Supabase RLS policies with NULL user_id to ensure system categories are protected while remaining visible.

**Files to Create**:
- `tests/SauronSheet.Application.Tests/Infrastructure/RLS/CategoryRLSPolicyTests.cs` (NEW)

**Test Setup** (required for all 3 tests):
```csharp
[TestInitialize] / [SetUp]
public async Task Setup()
{
    // 1. Create test user with JWT token
    // 2. Insert test data:
    //    - 1 system category: (uuid, NULL, 'Salary', 'Income', ..., true)
    //    - 2 user categories: (uuid, 'test-user', 'Coffee', 'Expense', ..., false)
    // 3. Sign in as test-user
    // 4. Note: Migration 004 provides 24 system categories
}
```

**Test Cases**:

**T-3.18**: `RLS_UserCanViewOwnCategories`
```csharp
// Execute: SELECT FROM categories WHERE user_id = current_user_id
// Assert: Returns user's categories (Coffee, etc.) with user_id = test-user
```

**T-3.19**: `RLS_UserCanViewSystemCategories`
```csharp
// Execute: SELECT FROM categories WHERE user_id = NULL
// Assert: Returns system categories (Salary, etc.) with user_id = NULL
// Verify: Both user AND system categories returned together
```

**T-3.20**: `RLS_UserCannotModifySystemCategories`
```csharp
// Execute: UPDATE categories SET name='Modified' WHERE user_id IS NULL
// Assert: UPDATE fails (permission denied or 0 rows affected)
// Execute: DELETE FROM categories WHERE user_id IS NULL
// Assert: DELETE fails (permission denied or 0 rows affected)
```

**Acceptance Criteria**:
- [ ] 3 tests created
- [ ] Tests use actual Supabase test instance (or mock RLS behavior)
- [ ] SELECT policy verified (SELECT returns system + user categories)
- [ ] UPDATE policy verified (UPDATE blocked for NULL user_id)
- [ ] DELETE policy verified (DELETE blocked for NULL user_id)
- [ ] No errors or permission exceptions on valid operations
- [ ] Permission errors on invalid operations
- [ ] Test data setup explicit and reusable
- [ ] `dotnet test --filter "T-3.18 | T-3.19 | T-3.20"` passes

---

### Task 5.3: Migration Verification Tests (3 tests)

**Effort**: 1 hour | **Assigned to**: QA Engineer  
**Depends on**: Task 3.1, 3.2  
**Blocks**: Code Review Checkpoint 2

**Description**:
Verify that migration 004 applies successfully, is idempotent, and maintains data integrity.

**Files to Create**:
- `tests/SauronSheet.Application.Tests/Infrastructure/Migrations/SystemCategoriesMigrationTests.cs` (NEW)

**Test Cases**:

**Preamble**:
```csharp
[SetUp]
public async Task Setup()
{
    // Fresh database schema (no prior migrations)
    // Apply all migrations up to 003
}
```

**Test 1**: `Migration004_Applies_Successfully`
```csharp
// Act: Apply migration 004_SystemCategoriesGlobalScope.sql
// Assert: No errors
// Assert: user_id column is nullable
// Assert: idx_categories_user_name_unique exists
// Assert: chk_null_user_implies_system_default exists
// Assert: 24 system categories inserted
```

**Test 2**: `Migration004_Creates_24_Categories_Correctly`
```csharp
// Query: SELECT * FROM categories WHERE user_id IS NULL AND is_system_default = true
// Assert: Exactly 24 rows
// Assert: All have correct names (Salary, Housing, Coffee, etc.)
// Assert: All have correct colors (#27AE60 for income, #E74C3C for expense)
// Assert: All have correct icons (building-dollar, house, coffee, etc.)
```

**Test 3**: `Migration004_Is_Idempotent`
```csharp
// Act: Apply migration 004
// Assert: 24 categories inserted
// Act: Apply migration 004 again (simulating re-deployment)
// Assert: No errors
// Assert: Still 24 categories (no duplicates)
// Assert: Existing user categories unchanged
```

**Acceptance Criteria**:
- [ ] 3 tests created and passing
- [ ] Migration applies without errors
- [ ] Schema changes correct (nullable, indexes, constraint)
- [ ] 24 system categories inserted with correct data
- [ ] Migration is idempotent (safe to run twice)
- [ ] User categories preserved if present
- [ ] All indexes and constraints properly created
- [ ] `dotnet test --filter "Migration004"` passes

---

### Task 5.4: Migration Idempotency Tests (3 new tests)

**Effort**: 1.5 hours | **Assigned to**: QA Engineer  
**Depends on**: Task 3.1, 3.2  
**Blocks**: Code Review Checkpoint 2

**Description**:
Deep-dive tests specifically for idempotency and edge cases with existing user data.

**Files to Create**:
- `tests/SauronSheet.Application.Tests/Infrastructure/Migrations/MigrationIdempotencyTests.cs` (NEW)

**Test Cases**:

**T-3.21**: `Migration_RunTwice_NoDuplicates`
```csharp
// Act: Run migration 004 once
// Assert: 24 system categories inserted
// Count: SELECT COUNT(*) FROM categories WHERE is_system_default = true
// Assert: count == 24
// Act: Run migration 004 again
// Assert: No errors
// Count: SELECT COUNT(*) FROM categories WHERE is_system_default = true
// Assert: count == 24 (not 48)
```

**T-3.22**: `SystemCategories_Correct_AfterIdempotentMigration`
```csharp
// Setup: Insert migration data (run 004 once)
// Verify: 24 categories, correct names/colors/icons
// Act: Run migration 004 again
// Verify: Data unchanged (same names, colors, icons)
// Verify: No data corruption
```

**T-3.23**: `Migration_WithExistingUserCategories_PreservedOnSecondRun`
```csharp
// Setup: Run migration 004
// Act: Insert user category: (uuid, 'user-123', 'My Coffee', 'Expense', ...)
// Assert: Row count = 25 (24 system + 1 user)
// Act: Run migration 004 again
// Assert: No errors
// Assert: User category still exists and unchanged
// Assert: Row count still = 25
```

**Acceptance Criteria**:
- [ ] 3 tests created and passing
- [ ] Idempotency verified (can run migration 2+ times)
- [ ] No duplicates created
- [ ] User data preserved
- [ ] System data unchanged on re-run
- [ ] Edge cases handled (existing data + migration)
- [ ] No data corruption
- [ ] `dotnet test --filter "T-3.21 | T-3.22 | T-3.23"` passes

---

## PHASE 3: APPLICATION LAYER (3.5 hours)

### Task 6.1: CreateTransactionCommandHandler Null-Checking

**Effort**: 0.25 hours | **Assigned to**: Mid Engineer  
**Depends on**: Tasks 1.1-1.4 (Domain complete)  
**Blocks**: Task 7.1

**Description**:
Update CreateTransactionCommandHandler to use safe null-checking for nullable UserId.

**Files to Create/Modify**:
- `src/SauronSheet.Application/Features/Transactions/Commands/CreateTransactionCommandHandler.cs` (MODIFY)

**Change Pattern**:
```csharp
// BEFORE (unsafe):
var category = await _categoryRepository.GetByIdAsync(categoryId);
if (category.UserId.Value != currentUserId.Value)
    throw new UnauthorizedAccessException("You don't own this category");

// AFTER (safe):
var category = await _categoryRepository.GetByIdAsync(categoryId);
if (category.UserId != null && category.UserId.Value != currentUserId.Value)
    throw new UnauthorizedAccessException("You don't own this category");
// If UserId is null (system category), allow access

// OR use helper method:
if (!category.IsAccessibleToUser(currentUserId))
    throw new UnauthorizedAccessException("You don't have access to this category");
```

**Acceptance Criteria**:
- [ ] Null-checking added (either pattern acceptable)
- [ ] System categories (UserId==null) allow access
- [ ] User categories require ownership check
- [ ] No NullReferenceException possible
- [ ] Code compiles
- [ ] Behavior unchanged for existing user categories

**Testing**:
- Integration test T-3.16 in Task 7.1

---

### Task 6.2: UpdateTransactionCategoryCommandHandler Null-Checking

**Effort**: 0.25 hours | **Assigned to**: Mid Engineer  
**Depends on**: Task 1.1-1.4  
**Blocks**: Task 7.1

**Description**:
Update UpdateTransactionCategoryCommandHandler with safe null-checking (same pattern as 6.1).

**Files to Create/Modify**:
- `src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCategoryCommandHandler.cs` (MODIFY)

**Change Pattern**:
Same as Task 6.1 (find category ownership check, add safe null-checking).

**Acceptance Criteria**:
- [ ] Same as Task 6.1
- [ ] Pattern consistent with 6.1

**Testing**:
- Covered by T-3.17 in Task 7.1

---

### Task 6.3: DeleteTransactionCommandHandler Null-Checking

**Effort**: 0.25 hours | **Assigned to**: Mid Engineer  
**Depends on**: Task 1.1-1.4  
**Blocks**: Task 7.1

**Description**:
Update DeleteTransactionCommandHandler with safe null-checking (same pattern as 6.1, 6.2).

**Files to Create/Modify**:
- `src/SauronSheet.Application/Features/Transactions/Commands/DeleteTransactionCommandHandler.cs` (MODIFY)

**Change Pattern**:
Same as Tasks 6.1-6.2.

**Acceptance Criteria**:
- [ ] Same as Tasks 6.1-6.2
- [ ] Pattern consistent

**Testing**:
- Covered by integration tests in Task 7.1

---

### Task 6.4: CreateCategoryCommand Validation Update

**Effort**: 0.5 hours | **Assigned to**: Mid Engineer  
**Depends on**: Tasks 4.3, 1.3  
**Blocks**: Task 7.1

**Description**:
Update CreateCategoryCommandHandler to validate category names against both hardcoded system defaults and database.

**Files to Create/Modify**:
- `src/SauronSheet.Application/Features/Categories/Commands/CreateCategoryCommandHandler.cs` (MODIFY)

**Changes**:
```csharp
public async Task<CategoryId> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
{
    var userId = new UserId(_userContext.UserId);

    // NEW: Check database for duplicate (across all scopes)
    var duplicate = await _categoryRepository.FindByNameAsync(request.Name);
    if (duplicate != null)
        throw new DomainException($"Category name '{request.Name}' is already in use (system or custom)");

    // Existing: Check hardcoded system defaults
    var systemDefaults = _categoryService.GetSystemDefaults();
    if (systemDefaults.Any(c => c.Name.Value.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        throw new DomainException($"Category name '{request.Name}' is reserved for system defaults");

    // ... rest of handler unchanged
}
```

**Acceptance Criteria**:
- [ ] FindByNameAsync() called first (database check)
- [ ] Throws DomainException if duplicate exists
- [ ] Hardcoded validation still present (defensive)
- [ ] Error message clear
- [ ] Code compiles
- [ ] No breaking changes to public API

**Testing**:
- Integration test T-3.15 in Task 7.1

---

### Task 6.5: Remove SeedSystemDefaultsCommandHandler (OPTION A ONLY)

**Effort**: 0.5 hours | **Assigned to**: Senior Engineer  
**Depends on**: Task 1.3 (cascade complete), Task 3.1 (migration in place)  
**Blocks**: Task 7.1, final build

**Description**:
Remove SeedSystemDefaultsCommandHandler and SeedSystemDefaultsCommand, and clean up GetCategoriesQueryHandler.

**⚠️ CONDITIONAL**: Only execute if Option A (Migration-Only) is chosen.

**Files to Delete**:
- `src/SauronSheet.Application/Features/Categories/Commands/SeedSystemDefaultsCommandHandler.cs` (DELETE)
- `src/SauronSheet.Application/Features/Categories/Commands/SeedSystemDefaultsCommand.cs` (DELETE)

**Files to Modify**:
- `src/SauronSheet.Application/Features/Categories/Queries/GetCategoriesQueryHandler.cs` (MODIFY)

**Changes to GetCategoriesQueryHandler**:
```csharp
public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
{
    var userId = new UserId(_userContext.UserId);

    // REMOVE: await _mediator.Send(new SeedSystemDefaultsCommand(), cancellationToken);
    // System categories now persisted in database via migration 004

    var categories = await _categoryRepo.GetByUserIdAsync(userId);
    
    // ... rest unchanged (still returns system + user categories via union query)
}
```

**Acceptance Criteria**:
- [ ] Handler file deleted
- [ ] Command file deleted
- [ ] MediatR.Send(SeedSystemDefaultsCommand) line removed from GetCategoriesQueryHandler
- [ ] No compilation errors
- [ ] System categories still available (via GetByUserIdAsync() union query)
- [ ] No other references to SeedSystemDefaultsCommand found (grep search)
- [ ] Tests for GetCategoriesQueryHandler still pass (verify system cats visible)

**Testing**:
- Verify in Task 7.1 (GetCategoriesQueryHandler tests)
- Full build test to confirm no orphaned references

---

### Task 7.1: Handler Integration Tests (3 tests)

**Effort**: 1.5 hours | **Assigned to**: QA Engineer  
**Depends on**: Tasks 6.1-6.5 (all handlers complete)  
**Blocks**: Code Review Checkpoint 3

**Description**:
Implement integration tests for handler changes with nullable UserId.

**Files to Create**:
- `tests/SauronSheet.Application.Tests/Features/Categories/CreateCategoryCommandHandlerNullableUserIdTests.cs` (NEW)
- `tests/SauronSheet.Application.Tests/Features/Transactions/TransactionHandlerNullableUserIdTests.cs` (NEW)

**Test Cases**:

**T-3.15**: `CreateCategoryCommand_RejectsSystemDefaultName`
```csharp
// Setup: System defaults include "Salary"
// Act: Send CreateCategoryCommand with name="Salary"
// Assert: DomainException thrown with message about reserved name
```

**T-3.16**: `CreateTransactionCommandHandler_AllowsSystemCategory`
```csharp
// Setup: Insert system category (NULL user_id, isSystemDefault=true)
// Act: Send CreateTransactionCommand with system category ID
// Assert: Transaction created successfully (no ownership error)
```

**T-3.17**: `UpdateTransactionCategoryCommandHandler_SafeNullChecking`
```csharp
// Setup: Insert system category + user category
// Act: Try to update transaction with system category → Should succeed
// Act: Try to update transaction with other user's category → Should fail
// Assert: No NullReferenceException in any case
```

**Acceptance Criteria**:
- [ ] 3 tests created and passing
- [ ] System default names rejected
- [ ] System categories accessible to all users
- [ ] Safe null-checking prevents exceptions
- [ ] User ownership still enforced for user categories
- [ ] No NullReferenceException possible
- [ ] `dotnet test --filter "T-3.15 | T-3.16 | T-3.17"` passes

---

## PHASE 4: TESTING & VERIFICATION (4.5 hours)

### Task 8.1: Full Test Suite Execution

**Effort**: 1.5 hours | **Assigned to**: QA Engineer  
**Depends on**: All implementation tasks complete  
**Blocks**: Staging deployment

**Description**:
Run complete test suite to ensure all 23 tests pass and no regressions.

**Execution**:
```bash
# Run all Feature 3 tests
dotnet test --filter "T-3.01 | T-3.02 | T-3.03 | T-3.04 | T-3.05 | T-3.06 | T-3.07 | T-3.08 | T-3.09 | T-3.10 | T-3.11 | T-3.12 | T-3.13 | T-3.14 | T-3.15 | T-3.16 | T-3.17 | T-3.18 | T-3.19 | T-3.20 | T-3.21 | T-3.22 | T-3.23"

# Run domain tests
dotnet test --filter "Category=Domain"

# Run infrastructure tests
dotnet test --filter "Category=Infrastructure"

# Run application tests
dotnet test --filter "Category=Application"

# Full regression suite
dotnet test
```

**Acceptance Criteria**:
- [ ] 23 Feature 3 tests all passing
- [ ] 100% domain coverage maintained
- [ ] No new failing tests in existing test suite
- [ ] Build succeeds: `dotnet build`
- [ ] No warnings in build output
- [ ] All test output reviewed (no flaky tests)

**Report**:
Generate summary:
```
Feature 3 Test Results:
- Domain: 8/8 passing ✅
- Infrastructure: 12/12 passing ✅
- Application: 3/3 passing ✅
- Idempotency: 3/3 passing ✅
- Total: 23/23 passing ✅
- Regression: 0 failures ✅
```

---

### Task 8.2: Performance Profiling (Caching)

**Effort**: 1 hour | **Assigned to**: Senior Engineer  
**Depends on**: Task 1.3 (caching implemented)  
**Blocks**: Success metric verification

**Description**:
Profile GetSystemDefaults() to verify caching works as expected.

**Execution**:
```csharp
// In a test or profiling harness:
var service = new CategoryService();
var stopwatch = Stopwatch.StartNew();

// First call (uncached): Should create 24 objects
var defaults1 = service.GetSystemDefaults();
stopwatch.Stop();
var firstCallTime = stopwatch.ElapsedMilliseconds;

stopwatch.Restart();

// Second call (cached): Should return same instance, much faster
for (int i = 0; i < 100; i++)
{
    var defaults = service.GetSystemDefaults();
    Assert.Same(defaults1, defaults); // Same reference
}
stopwatch.Stop();
var subsequentTime = stopwatch.ElapsedMilliseconds;

// Assert: Second call is significantly faster (same object returned)
Assert.True(subsequentTime < firstCallTime * 10, "Caching not working");
```

**Acceptance Criteria**:
- [ ] Caching verified (Same reference returned)
- [ ] Performance improvement measured (100+ calls faster than 1 initial)
- [ ] No memory leaks (static cache doesn't grow)
- [ ] Thread-safety verified (concurrent calls don't corrupt cache)
- [ ] Report generated with timing

---

### Task 8.3: Regression Testing (Feature 2 Functionality)

**Effort**: 1.5 hours | **Assigned to**: QA Engineer  
**Depends on**: All implementation complete  
**Blocks**: Staging deployment approval

**Description**:
Verify that Feature 2 functionality still works without changes. User experience should be identical.

**Test Scenarios**:
1. **GetCategories**
   - User sees own categories + system defaults
   - Same count as before Feature 3
   - Same order (system first, then alphabetical)
   
2. **CreateCategory**
   - User can create custom category
   - Cannot use system default names
   - Same validation as before

3. **CreateTransaction**
   - Can assign transaction to system category
   - Can assign transaction to own category
   - Cannot assign to other user's category
   - Same behavior as before

4. **UpdateTransaction**
   - Can change category to system default
   - Can change category to own category
   - Cannot change to other user's category
   - Same behavior as before

5. **DeleteTransaction**
   - Still works with system categories
   - Still works with own categories
   - Same behavior as before

**Acceptance Criteria**:
- [ ] All regression scenarios pass
- [ ] User-facing behavior identical
- [ ] No breaking changes detected
- [ ] Existing tests still pass
- [ ] Category count/names correct

---

### Task 8.4: Staging Deployment & Blue-Green Validation

**Effort**: 1 hour | **Assigned to**: Senior Engineer + Devops  
**Depends on**: All tasks complete  
**Blocks**: Production deployment approval

**Description**:
Deploy Feature 3 to staging environment following blue-green strategy and validate.

**Deployment Steps**:

**Step 1: Deploy Application Code**
```bash
# Tag: feature/003-system-categories-global-scope
git checkout -b 003-system-categories-global-scope
# Apply all changes
git push origin 003-system-categories-global-scope

# On staging:
git pull origin 003-system-categories-global-scope
dotnet build -c Release
dotnet publish -c Release -o ./publish
# Blue deployment (old code still running)
```

**Step 2: Run Database Migration**
```bash
# Execute migration 004
psql -h staging-db -U admin -d sauronsheet -f 004_SystemCategoriesGlobalScope.sql

# Verify: Check 24 system categories inserted
SELECT COUNT(*) FROM categories WHERE user_id IS NULL AND is_system_default = true;
-- Expected: 24
```

**Step 3: Switch to Green (New Code)**
```bash
# Switch load balancer / restart app with new code
# Old blue code still available for rollback
```

**Step 4: Smoke Test**
```bash
# Test user login
# Test get categories (verify 24 system + user categories)
# Test create transaction with system category
# Test create category
# Monitor error rates (expect 0 new errors)
```

**Step 5: Monitoring**
```
- Error rate: 0
- Response time: Normal
- System categories visible to all users
- No NullReferenceException in logs
- No RLS permission errors
```

**Acceptance Criteria**:
- [ ] Deployment successful (no errors)
- [ ] Migration successful (24 system categories inserted)
- [ ] Application started with new code
- [ ] All smoke tests pass
- [ ] Error rates normal
- [ ] Users see system + own categories
- [ ] No performance regression
- [ ] Rollback ready (005 migration available)

**Rollback Plan** (if issues):
```bash
# Step 1: Switch back to blue (old code) - instant
# Step 2: Run rollback migration 005
psql -h staging-db -U admin -d sauronsheet -f 005_RevertSystemCategoriesGlobalScope.sql
# Step 3: Verify: user_id not NULL again, system categories gone
SELECT COUNT(*) FROM categories WHERE user_id IS NULL;
-- Expected: 0 (if rollback successful)
```

---

## Code Review Checkpoints

### Checkpoint 1: Domain Layer (after Task 1.4)
- [ ] Nullable UserId properly constrained
- [ ] Domain invariant validation in place
- [ ] Helper methods semantically clear
- [ ] 8 domain tests all green
- [ ] 100% domain coverage maintained
- **Decision**: ✅ Approved to proceed to Infrastructure Layer

### Checkpoint 2: Infrastructure Layer (after Task 5.4)
- [ ] Migration SQL idempotent + reversible
- [ ] Repository union query tested (5 tests)
- [ ] CategoryRow mapping handles NULL correctly
- [ ] RLS policies verified (3 tests)
- [ ] Idempotency verified (3 tests)
- [ ] Total: 14 infrastructure tests all green
- **Decision**: ✅ Approved to proceed to Application Layer

### Checkpoint 3: Application Layer (after Task 7.1)
- [ ] Safe null-checking pattern consistent (3 handlers)
- [ ] Validation logic validates both scopes
- [ ] 3 application tests all green
- [ ] No breaking changes to handler signatures
- [ ] GetCategoriesQueryHandler still works (no seeding handler)
- **Decision**: ✅ Approved for staging deployment

---

## Final Acceptance Gates

**Before Production Deployment**:
- [ ] All 23 tests passing
- [ ] 0 regressions in Feature 2 functionality
- [ ] Performance profiling shows caching works
- [ ] Staging blue-green deployment successful
- [ ] Error rates at 0%
- [ ] RLS policies verified with actual users
- [ ] Rollback migration tested
- [ ] Documentation updated
- [ ] Team trained on new patterns

---

## Success Metrics Summary

| Metric | Target | Verification |
|--------|--------|--------------|
| All tests pass | 23/23 green | `dotnet test` exit code 0 |
| No breaking changes | 100% Feature 2 compat | Regression test suite passes |
| System defaults cached | 1 instance/lifetime | Profiling shows single allocation |
| Zero user-facing changes | 0 UX regressions | Manual UI testing on staging |
| Zero downtime deploy | 0 seconds downtime | Blue-green validation passes |
| Migration idempotent | Re-run safe | Migration × 2 test passes |
| RLS policies working | All 3 RLS tests pass | User cannot modify system cats |

---

_Task Specifications Complete: 2026-02-19_  
_Status: Ready for Team Assignment_  
_Total Tasks: 29 | Total Effort: 20.75 hours | Recommended: 2-3 engineers, 5-6 days_
