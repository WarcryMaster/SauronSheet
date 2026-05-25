# Proposal: Fix Transaction Category Retrieval

## Intent

Three spec gaps survived the archived `bank-category-resolution` change:
1. `TransactionDto.SubcategoryName` is never populated by read-side query handlers.
2. Translation lookup returns a generic `(bank_category, null)` row before the exact `(bank_category + bank_subcategory)` pair — opposite of the archived design.
3. Raw bank values are stored without whitespace trim, violating CR-1.

These are implementation defects against requirements CR-1, CR-2a, and DT-1 already committed to `openspec/specs/category-resolution/spec.md`. No new behaviour is designed; only the existing spec is enforced.

## Scope

### In Scope
- Populate `TransactionDto.SubcategoryName` in all three transaction query handlers
- Fix `SupabaseBankCategoryTranslationRepository` so exact `(bank_category + bank_subcategory)` match wins over generic `(bank_category, null)` fallback
- Trim `row.Category` / `row.SubCategory` before persisting raw bank values in the import handler
- Add targeted regression tests for each gap (translation precedence, SubcategoryName mapping, trim contract)

### Out of Scope
- Data migration for already-stored untrimmed values (no evidence of meaningful whitespace in production data)
- Read-model consolidation or shared enrichment helper (DX improvement, not correctness fix — deferred)
- Subcategory management UI

## Capabilities

### New Capabilities
- None

### Modified Capabilities
- `category-resolution`: Corrects three implementation gaps that violate CR-1, CR-2a, and DT-1. No spec text changes expected; implementation is aligned to the existing spec.

## Approach

**Surgical corrective patch** (Exploration Approach 1):

1. **Import handler** — apply `.Trim()` to `row.Category` and `row.SubCategory` before constructing the `Transaction` (CR-1).
2. **Translation repository** — query exact `(bank_category, bank_subcategory)` first; fall back to generic `(bank_category, null)` only if no exact row exists (CR-2a).
3. **Three query handlers** — enrich `TransactionDto.SubcategoryName` by calling `ISubcategoryRepository.GetByIdAsync(transaction.SubcategoryId)` when `SubcategoryId != null`. Use an in-memory map keyed on `SubcategoryId` to avoid N+1 per query batch.

**Chain strategy** (force-chained / 400-line budget):

| PR | Scope | Risk |
|----|-------|------|
| 1 | Import trim fix + CR-1 regression test | Trivial |
| 2 | Translation precedence fix + CR-2a regression test | Low |
| 3 | SubcategoryName enrichment in three handlers + DT-1 tests | Medium (N+1 guard) |

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs` | Modified | Trim raw bank values before persist |
| `Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` | Modified | Exact-before-generic lookup order |
| `Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` | Modified | Populate `SubcategoryName` |
| `Application/Features/Transactions/Queries/GetRecentTransactionsQueryHandler.cs` | Modified | Populate `SubcategoryName` |
| `Application/Features/Transactions/Queries/SearchTransactionsQueryHandler.cs` | Modified | Populate `SubcategoryName` |
| `Application.Tests/Services/BankCategoryResolutionServiceTests.cs` | Modified | Exact-over-generic precedence regression |
| `Application.Tests/Features/Transactions/Queries/*Tests.cs` | Modified | `SubcategoryName` mapping assertions |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| N+1 on `SubcategoryName` resolution per row | Medium | Pre-fetch subcategories by ID set; resolve in-memory per query invocation |
| Trim fix breaks existing import snapshot tests | Low | Update test expectations to trimmed literals explicitly |
| Translation fix changes resolution for any row with both category + subcategory | Low | Correct behavior per spec; no data loss |

## Rollback Plan

1. Revert import handler — stored raw values revert to un-trimmed form; no row loss.
2. Revert translation repository — generic-first lookup restored; subcategory-specific translations stop winning.
3. Revert handler enrichment — `SubcategoryName` returns to `null`; `SubcategoryId` still intact.

No schema change. No migration required.

## Dependencies

- None external. All within the current project stack.

## Success Criteria

- [ ] All three transaction query handlers populate `SubcategoryName` when `SubcategoryId != null`
- [ ] Exact `(bank_category + bank_subcategory)` translation wins over `(bank_category, null)` in a unit test
- [ ] Import handler stores trimmed raw bank values; CR-1a scenario passes as unit test
- [ ] All existing tests continue to pass after each chained PR (`dotnet test` green)
