# Design: fix-transaction-category-retrieval

## Technical Approach

Three surgical corrections to existing code with zero schema changes. Each fix maps to one autonomous PR slice following the feature-branch-chain strategy.

## Architecture Decisions

| Decision | Alternatives | Choice | Rationale |
|----------|-------------|--------|-----------|
| Trim at constructor call site | Trim inside Transaction entity, Trim in domain service | Trim in import handler before passing to `Transaction` ctor | Keeps domain entity pure (stores what it receives); the handler is responsible for input normalization |
| SubcategoryName via batch lookup | N+1 per-transaction `GetByIdAsync`, JOIN at DB level | Collect distinct `SubcategoryId`s → single `GetByUserIdAsync` → in-memory dictionary | Existing `ISubcategoryRepository.GetByUserIdAsync` returns all subcats for user; avoids N+1 and needs no new repo method |
| Translation precedence swap | New repo method with ordering, Fetch both + pick in service | Swap query order in `SupabaseBankCategoryTranslationRepository` (exact first, generic second) | Minimal change, single responsibility stays in infra; service logic unchanged |

## Data Flow

```
[Import Handler] ─trim→ Transaction ctor ─persist→ DB
                                                      
[Query Handler] ─fetch transactions─→ collect SubcategoryId set
                ─fetch subcategories─→ build Dictionary<SubcategoryId, string>
                ─map TransactionDto─→ lookup SubcategoryName from dict
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/.../Commands/ImportTransactionsFromPdfCommandHandler.cs` | Modify | Trim `row.Category` and `row.SubCategory` before passing to Transaction ctor (lines 175-176) |
| `src/.../Persistence/SupabaseBankCategoryTranslationRepository.cs` | Modify | Swap query order: exact (bank_category + bank_subcategory) runs FIRST; generic (bank_subcategory=null) runs as fallback |
| `src/.../Queries/GetRecentTransactionsQueryHandler.cs` | Modify | Inject `ISubcategoryRepository`, batch-fetch subcategories, populate `SubcategoryName` in DTO mapping |
| `src/.../Queries/GetTransactionsQueryHandler.cs` | Modify | Same pattern: inject repo, batch-fetch, populate |
| `src/.../Queries/SearchTransactionsQueryHandler.cs` | Modify | Same pattern: inject repo, batch-fetch, populate |

## Interfaces / Contracts

No new interfaces. Existing `ISubcategoryRepository.GetByUserIdAsync(UserId)` is sufficient.

Enrichment pattern for all three query handlers:

```csharp
// After fetching transactions and category lookup...
var subcategoryLookup = (await _subcategoryRepo.GetByUserIdAsync(userId))
    .ToDictionary(s => s.Id, s => s.Name.Value);

// In DTO mapping:
SubcategoryName: t.SubcategoryId != null && subcategoryLookup.ContainsKey(t.SubcategoryId)
    ? subcategoryLookup[t.SubcategoryId]
    : null
```

Translation precedence fix pattern:

```csharp
// Query 1: Exact match (bank_category + bank_subcategory) — FIRST
if (!string.IsNullOrEmpty(bankSubcategory))
{
    var exactResponse = await _client.From<BankCategoryTranslationRow>()
        .Where(x => x.BankCategory == bankCat)
        .Where(x => x.BankSubcategory == bankSub)
        .Get();
    if (exactResponse.Models.Count > 0)
        return exactResponse.Models.First().ToDomain();
}

// Query 2: Generic fallback (bank_subcategory IS NULL) — SECOND
var nullSubResponse = await _client.From<BankCategoryTranslationRow>()
    .Where(x => x.BankCategory == bankCat)
    .Where(x => x.BankSubcategory == null)
    .Get();
if (nullSubResponse.Models.Count > 0)
    return nullSubResponse.Models.First().ToDomain();
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Trim applied before persist (CR-1c) | Mock `ITransactionRepository.AddAsync` → assert stored `BankCategory`/`BankSubcategory` are trimmed |
| Unit | Exact-before-generic translation (CR-2e) | Integration test on `SupabaseBankCategoryTranslationRepository` OR unit test with mocked Postgrest client verifying query order |
| Unit | SubcategoryName populated (DT-1b) | Mock `ISubcategoryRepository.GetByUserIdAsync` → assert DTO has `SubcategoryName` when `SubcategoryId` is non-null |
| Unit | SubcategoryName null when no subcat (DT-1c) | Same mock → assert null when `SubcategoryId` is null |

## Migration / Rollout

No migration required. No schema changes. All fixes are code-only corrections to existing behavior.

## Open Questions

None — all three corrections are deterministic and do not require external decisions.
