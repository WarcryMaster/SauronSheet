# Design: Fix Transaction Category Warnings

## Technical Approach

Two surgical fixes targeting documented warnings from the `fix-transaction-category-retrieval` archive. No new abstractions, no interface changes, no domain modifications. Both fixes reuse patterns already present in sibling code.

## Architecture Decisions

| Decision | Alternatives | Rationale |
|----------|-------------|-----------|
| `protected internal virtual` seam methods in repository | (a) Interface method split (b) Protected-only without internal | `protected internal` is visible to test assembly via existing `InternalsVisibleTo`, avoids interface bloat, and keeps seam invisible to DI consumers |
| Test subclass in Infrastructure.Tests (`TestableSupabaseBankCategoryTranslationRepository`) | (a) Moq + partial mock (b) Separate in-memory implementation | Subclass directly overrides the seam methods with in-memory data; Moq partial-mock requires `virtual` already set, and a full separate impl duplicates logic. Subclass is cheapest. |
| Pass `null!` as `Supabase.Client` in test subclass constructor | (a) Create real client with mock HTTP | Virtual methods short-circuit before `_client` is used. `null!` is safe because overridden methods never call base. Avoids complex HTTP mocking. |
| Batch via `GetByUserIdAsync` in `GetTransactionsQueryHandler` | (a) Selective batch only for page IDs (b) Cache layer | Sibling handler (`GetRecentTransactionsQueryHandler` L51-52) already uses this exact pattern. Consistency trumps micro-optimization for small category sets. |

## Data Flow

```
GetTransactionsQueryHandler.Handle()
    │
    ├─ _transactionRepo.FindBySpecificationAsync(spec) → transactions
    ├─ _categoryRepo.GetByUserIdAsync(userId)          → all categories (1 call)
    ├─ categories.ToDictionary(Id → Name)              → categoryLookup
    ├─ _subcategoryRepo.GetByUserIdAsync(userId)       → subcategoryLookup (existing)
    └─ paginated.Select(t → TransactionDto using lookup)
```

```
SupabaseBankCategoryTranslationRepository.FindByBankCategoryAsync()
    │
    ├─ if (subcategory != null): ExecuteExactMatchQueryAsync(bankCat, bankSub)
    │     └─ if result → return early
    └─ ExecuteGenericMatchQueryAsync(bankCat)
          └─ return result or null
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/SauronSheet.Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` | Modify | Extract lines 43-46 → `ExecuteExactMatchQueryAsync`, lines 53-56 → `ExecuteGenericMatchQueryAsync` as `protected internal virtual` |
| `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` | Modify | Replace lines 85-97 (N+1 loop) with batch `GetByUserIdAsync` + `ToDictionary` pattern |
| `tests/SauronSheet.Infrastructure.Tests/Persistence/SupabaseBankCategoryTranslationRepositoryTests.cs` | Modify | Add `TestableSupabaseBankCategoryTranslationRepository` subclass and CR-2e-infra tests |
| `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetTransactionsQueryHandlerTests.cs` | Modify | Add DT-1d test: verify `GetByUserIdAsync` Once, `GetByIdAsync` Never |

## Interfaces / Contracts

No new interfaces. The seam methods are implementation details:

```csharp
// SupabaseBankCategoryTranslationRepository — new virtual seam methods
protected internal virtual Task<BankCategoryTranslationRow?> ExecuteExactMatchQueryAsync(
    string bankCategory, string bankSubcategory)

protected internal virtual Task<BankCategoryTranslationRow?> ExecuteGenericMatchQueryAsync(
    string bankCategory)
```

Test subclass contract:

```csharp
// In Infrastructure.Tests — overrides short-circuit Supabase.Client entirely
internal class TestableSupabaseBankCategoryTranslationRepository
    : SupabaseBankCategoryTranslationRepository
{
    private readonly List<BankCategoryTranslationRow> _rows;
    public List<string> CallOrder { get; } = new(); // tracks "exact"/"generic"

    public TestableSupabaseBankCategoryTranslationRepository(
        List<BankCategoryTranslationRow> rows) : base(null!) => _rows = rows;

    protected internal override Task<BankCategoryTranslationRow?> ExecuteExactMatchQueryAsync(...)
    protected internal override Task<BankCategoryTranslationRow?> ExecuteGenericMatchQueryAsync(...)
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Infrastructure | CR-2e-infra: exact executed before generic | `TestableSupabaseBankCategoryTranslationRepository` with `CallOrder` list; assert "exact" before "generic" |
| Infrastructure | CR-2e-infra-fallback: fallback when no exact | Same subclass; only generic row seeded; assert exact returns null, generic returns row |
| Application | DT-1d: batch category loading | Moq `ICategoryRepository`; verify `GetByUserIdAsync` called `Times.Once()`, `GetByIdAsync` called `Times.Never()` |
| Application | DT-1d: category name mapped correctly | Moq returns known categories; assert DTO `CategoryName` matches expected lookup |

## Migration / Rollout

No migration required. Pure code change; no schema, no feature flag.

## Open Questions

None — all decisions are informed by existing patterns in the codebase.
