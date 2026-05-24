# Design: Bank Category Resolution

## Technical Approach

Replace the empty category pipeline on import with a three-step flow: **save raw bank values → name-match against user categories → assign resolution source**. The existing 24 English system defaults are removed — only user-created categories participate. Resolution is entirely heuristics + optional override table, with no AI and no auto-creation.

## Architecture Decisions

| Option | Tradeoffs | Decision |
|--------|-----------|----------|
| Resolution in Domain vs Application | Domain: pure logic, testable without infra. Application: access to both repos and services in one place. | **Application service** — needs `ICategoryRepository` + `ISubcategoryRepository` + `IBankCategoryTranslationRepository`. A domain service would force all three repos as dependencies. |
| `CategorySource` as enum vs string VO | Enum: type-safe, serializable to string. String VO: more flexible for future values. | **Enum** in Domain. 4 fixed values. Postgrest DTO maps to/from string. |
| Subcategory as AggregateRoot vs Value Object | AR: independent lifecycle, repository, FK from transactions. VO: embedded in transaction. | **AggregateRoot** — DB already has a `subcategories` table with FK relationships. Must match existing schema. |
| Name matching in Postgrest vs in-memory | Postgrest: case-insensitive depends on column collation. In-memory: explicit `OrdinalIgnoreCase`. | **In-memory** — established pattern in `SupabaseCategoryRepository.FindByNameAndUserAsync`. Postgrest OR limitation makes two queries + in-memory filter safer. |

## Data Flow

```
PDF IngBankPdfParser
    │
    ▼
RawTransactionRow { Category="Compras", SubCategory="Ropa y complementos" }
    │
    ▼
Import Handler (after duplicate check)
    │
    ├─ 1. Save raw: transaction.BankCategory = row.Category
    │              transaction.BankSubcategory = row.SubCategory
    │
    ├─ 2. Call ResolutionService.Resolve(userId, row.Category, row.SubCategory)
    │        │
    │        ├─ Normalize input (trim, null-guard)
    │        ├─ Query bank_category_translations for exact match
    │        │     └─ Found? Use resolved_category_name as resolvedName
    │        ├─ Fetch user's categories
    │        │     └─ Match resolvedName (case-insensitive, in-memory)
    │        │           ├─ Found → CategoryId = match.Id, source = AutoMatched
    │        │           └─ Not found → (null, null, RawOnly)
    │        ├─ If category matched AND bankSubcategory present:
    │        │     └─ Query subcategories within matched category
    │        │           ├─ Found → SubcategoryId = match.Id
    │        │           └─ Not found → null
    │        └─ Return ResolutionResult
    │
    ├─ 3. new Transaction(..., categoryId, subcategoryId, bankCategory, bankSubcategory, source)
    │
    └─ 4. repo.AddAsync(transaction)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Domain/ValueObjects/CategorySource.cs` | Create | Enum: Legacy, RawOnly, AutoMatched, UserOverride |
| `Domain/ValueObjects/SubcategoryId.cs` | Create | Strong-typed ID record wrapping Guid |
| `Domain/ValueObjects/SubcategoryName.cs` | Create | Validated name VO (1-50 chars) |
| `Domain/Entities/Subcategory.cs` | Create | AggregateRoot<SubcategoryId> with CategoryId FK |
| `Domain/Repositories/ISubcategoryRepository.cs` | Create | 5 methods: CRUD + find by name |
| `Domain/Repositories/IBankCategoryTranslationRepository.cs` | Create | Read-only: FindByBankCategoryAsync |
| `Domain/Services/CategoryService.cs` | Modify | Remove GetSystemDefaults(), cached list (_cachedSystemDefaults + _cacheLock), CreateDefault helper, and the system-default name check from ValidateUniqueName |
| `Domain/Entities/Transaction.cs` | Modify | Add 4 properties, new constructor overload, Categorize override |
| `Domain/Entities/Category.cs` | No change | `IsSystemDefault` property stays (column kept in DB). Only system default DATA is removed. |
| `Application/Commands/ImportTransactionsFromPdfCommandHandler.cs` | Modify | Inject resolution service, call after parse, remove SeedSystemDefaultsCommand call |
| `Application/Queries/GetCategoriesQueryHandler.cs` | Modify | Remove SeedSystemDefaultsCommand call (line 41), remove IsSystemDefault sort — just order alphabetically |
| `Application/Commands/SeedSystemDefaultsCommand.cs` | Delete | No longer needed |
| `Application/Commands/SeedSystemDefaultsCommandHandler.cs` | Delete | No longer needed |
| `Application/DTOs/TransactionDto.cs` | Modify | Add BankCategory, BankSubcategory, SubcategoryId, CategorySource fields |
| `Application/Services/IBankCategoryResolutionService.cs` | Create | Interface with Resolve(userId, bankCategory, bankSubcategory) |
| `Application/Services/BankCategoryResolutionService.cs` | Create | Resolution algorithm implementation |
| `Infrastructure/Persistence/TransactionRow.cs` | Modify | Add 4 columns, update ToDomain/FromDomain/FromDomainForInsert |
| `Infrastructure/Persistence/SupabaseTransactionRepository.cs` | Modify | No structural change — uses updated TransactionRow |
| `Infrastructure/Persistence/SubcategoryRow.cs` | Create | Postgrest DTO for subcategories table |
| `Infrastructure/Persistence/SupabaseSubcategoryRepository.cs` | Create | ISubcategoryRepository implementation |
| `Infrastructure/Persistence/TranslationRow.cs` | Create | Postgrest DTO for bank_category_translations (read-only) |
| `Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` | Create | Read-only implementation |
| `Infrastructure/DependencyInjection.cs` | Modify | Register new repositories, services; remove old |
| `Infrastructure/Persistence/Migrations/` | Create | SQL migration: delete system defaults, ensure columns |
| `Domain/Repositories/ICategoryRepository.cs` | Modify | Remove GetSystemDefaultsAsync only. Keep FindByNameAsync (used by CreateCategoryCommandHandler for global duplicate check — now just searches user categories) |

## Interfaces / Contracts

```csharp
// Domain
public enum CategorySource { Legacy = 0, RawOnly = 1, AutoMatched = 2, UserOverride = 3 }

public record SubcategoryId(Guid Value);
public record SubcategoryName(string Value)
{
    public static SubcategoryName Create(string name); // validated 1-50 chars
}

public class Subcategory : AggregateRoot<SubcategoryId>
{
    public UserId? UserId { get; }
    public CategoryId CategoryId { get; }
    public SubcategoryName Name { get; }
    public bool IsAutoCreated { get; }
}

public interface ISubcategoryRepository
{
    Task<Subcategory?> GetByIdAsync(SubcategoryId id);
    Task<IReadOnlyList<Subcategory>> GetByUserIdAsync(UserId userId);
    Task<IReadOnlyList<Subcategory>> GetByCategoryIdAsync(CategoryId categoryId);
    Task<Subcategory?> FindByNameAsync(UserId userId, CategoryId categoryId, string name);
    Task AddAsync(Subcategory subcategory);
}

public interface IBankCategoryTranslationRepository
{
    Task<BankCategoryTranslation?> FindByBankCategoryAsync(string bankCategory, string? bankSubcategory);
}

// Application
public record ResolutionResult(CategoryId? CategoryId, SubcategoryId? SubcategoryId, CategorySource Source);

public interface IBankCategoryResolutionService
{
    Task<ResolutionResult> ResolveAsync(UserId userId, string? bankCategory, string? bankSubcategory);
}
```

## Transaction Entity Changes

```csharp
// New properties on Transaction
public string? BankCategory { get; private set; }
public string? BankSubcategory { get; private set; }
public SubcategoryId? SubcategoryId { get; private set; }
public CategorySource CategorySource { get; private set; }

// New constructor overload (backward-compatible defaults)
public Transaction(
    TransactionId id, UserId userId, Money amount, DateTime date,
    string description,
    CategoryId? categoryId = null,
    string? importedFrom = null,
    string? bankCategory = null,
    string? bankSubcategory = null,
    SubcategoryId? subcategoryId = null,
    CategorySource categorySource = CategorySource.Legacy)
    : base(id)
{
    // ... existing validation ...
    BankCategory = bankCategory;
    BankSubcategory = bankSubcategory;
    SubcategoryId = subcategoryId;
    CategorySource = categorySource;
}

// Modified Categorize — user action from UI
// When assigning a category → source is UserOverride
// When clearing (null) → keep existing source (transaction didn't become "raw" again)
public void Categorize(CategoryId? categoryId)
{
    CategoryId = categoryId;
    if (categoryId != null)
        CategorySource = CategorySource.UserOverride;
    UpdatedAt = DateTime.UtcNow;
}

// New overload
public void Categorize(CategoryId? categoryId, SubcategoryId? subcategoryId, CategorySource source)
{
    CategoryId = categoryId;
    SubcategoryId = subcategoryId;
    CategorySource = source;
    UpdatedAt = DateTime.UtcNow;
}
```

## Resolution Algorithm

```csharp
public async Task<ResolutionResult> ResolveAsync(UserId userId, string? bankCategory, string? bankSubcategory)
{
    if (string.IsNullOrWhiteSpace(bankCategory))
        return new ResolutionResult(null, null, CategorySource.RawOnly);

    var normalized = bankCategory.Trim();

    // Step 1: Check bank_category_translations for override
    var translation = await _translationRepo.FindByBankCategoryAsync(normalized, bankSubcategory?.Trim());
    var resolvedName = translation?.ResolvedCategoryName ?? normalized;

    // Step 2: Fetch user categories, match by name
    var userCategories = await _categoryRepo.GetByUserIdAsync(userId);
    var match = userCategories.FirstOrDefault(
        c => c.Name.Value.Equals(resolvedName, StringComparison.OrdinalIgnoreCase));

    if (match == null)
        return new ResolutionResult(null, null, CategorySource.RawOnly);

    // Step 3: Match subcategory within category
    SubcategoryId? subcategoryId = null;
    if (!string.IsNullOrWhiteSpace(bankSubcategory))
    {
        var subcats = await _subcategoryRepo.GetByCategoryIdAsync(match.Id);
        var subMatch = subcats.FirstOrDefault(
            s => s.Name.Value.Equals(bankSubcategory.Trim(), StringComparison.OrdinalIgnoreCase));
        if (subMatch != null)
            subcategoryId = subMatch.Id;
    }

    return new ResolutionResult(match.Id, subcategoryId, CategorySource.AutoMatched);
}
```

## Postgrest Query Patterns

```csharp
// bank_category_translations lookup (exact match on both)
await _client.From<TranslationRow>()
    .Where(x => x.BankCategory == bankCategory)
    .Where(x => x.BankSubcategory == bankSubcategory)
    .Get();

// Subcategories within category (two filters, AND is supported)
await _client.From<SubcategoryRow>()
    .Where(x => x.UserId == userId)
    .Where(x => x.CategoryId == categoryId)
    .Get();
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | CategorySource enum serialization | Values map as-expected to strings |
| Unit | Subcategory entity invariants | Null/empty name, missing CategoryId |
| Unit | SubcategoryId/SubcategoryName creation | Validation on empty, boundary values |
| Unit | Transaction new constructor | Valid params, null defaults, backward compat |
| Unit | Transaction.Categorize overloads | Sets CategorySource correctly |
| Application | ResolutionService no-match → RawOnly | Mock repos, verify output |
| Application | ResolutionService exact name match → AutoMatched | Mock repo returns matching category |
| Application | ResolutionService translation override | Mock translation repo overrides resolvedName |
| Application | ResolutionService subcategory match | Mock subcategory repo returns match |
| Application | ImportHandler saves raw bank values | Verify Transaction has BankCategory/BankSubcategory |
| Application | ImportHandler calls resolution service | Verify service called once per row |
| Application | ImportHandler no SeedSystemDefaults | Verify mediator NOT called for that command |
| Integration | ResolutionService with real mock chain | All repos coordinated |
| DB | Migration removes system defaults | DELETE + verify FK integrity |
| DB | New columns on transactions | SELECT on migrated row |

## Migration / Rollout

1. **SQL migration** (prerequisite, applied first):
   ```sql
   -- NOTE: bank_category, bank_subcategory, subcategory_id, category_source
   -- already exist on public.transactions. No ALTER needed.

   -- Remove all system default categories (user_id IS NULL, is_system_default = true)
   -- SAFE: 0 existing transactions reference them (all have category_id = null)
   DELETE FROM public.categories WHERE user_id IS NULL AND is_system_default = true;

   -- Keep the is_system_default column on categories table (NOT NULL DEFAULT false).
   -- It doesn't hurt and avoids a heavy column-drop migration.
   ```

2. **Code deploy**: All new files + modifications deployed together.

3. **Remove `SeedSystemDefaultsCommand` calls** from both callers:
   - `ImportTransactionsFromPdfCommandHandler.cs` line 71
   - `GetCategoriesQueryHandler.cs` line 41
   
   Then delete the command and handler files entirely.

4. **No data migration**: 569 existing transactions keep `category_source='Legacy'`. The raw `bank_category`/`bank_subcategory` columns may be null for existing rows — that's acceptable (Legacy source means resolution didn't run).

## Open Questions (Resolved in Review)

- [x] `bank_category_translations` RLS: policy `"Authenticated users can view bank category translations"` already grants SELECT. Read-only is fine for resolution service.
- [x] Subcategory FK constraint: `transactions.subcategory_id → subcategories.id` already has `ON DELETE SET NULL`. Safe.
- [x] `Category.IsSystemDefault`: keeping both the column and the property. Only the DATA (24 system default rows) is removed. The entity property stays — unused for now, but harmless.
