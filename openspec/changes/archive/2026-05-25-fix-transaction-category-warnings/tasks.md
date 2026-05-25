# Tasks: Fix Transaction Category Warnings

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~150–200 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | N/A — single PR |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | CR-2e seam + DT-1d batch fix + all tests | PR 1 | targets main; all 4 files; self-contained |

---

## Phase 1: RED — Write Failing Tests (TDD)

- [x] 1.1 `SupabaseBankCategoryTranslationRepositoryTests.cs`: add inner class `TestableSupabaseBankCategoryTranslationRepository` with `List<string> CallOrder` and overrides for `ExecuteExactMatchQueryAsync` / `ExecuteGenericMatchQueryAsync` — will NOT compile until Phase 2.
- [x] 1.2 `SupabaseBankCategoryTranslationRepositoryTests.cs`: add `CR_2e_Infra_ExactExecutedBeforeGeneric` — seeds fila-A (exact) + fila-B (generic), calls `FindByBankCategoryAsync`, asserts `CallOrder[0] == "exact"` and result is fila-A.
- [x] 1.3 `SupabaseBankCategoryTranslationRepositoryTests.cs`: add `CR_2e_Infra_FallbackGenericWhenNoExact` — seeds only fila-B (generic), asserts exact returns null and generic returns fila-B.
- [x] 1.4 `GetTransactionsQueryHandlerTests.cs`: add `DT_1d_CategoryResolution_UsesBatchCall` — Moq `ICategoryRepository`; verify `GetByUserIdAsync` called `Times.Once()` and `GetByIdAsync` called `Times.Never()`.
- [x] 1.5 `GetTransactionsQueryHandlerTests.cs`: add `DT_1d_CategoryResolution_MapsNameCorrectly` — Moq returns known categories; assert `TransactionDto.CategoryName` matches expected lookup value.
- [x] 1.6 Run `dotnet test` — confirm new tests FAIL (RED baseline). ✓ (CS0115 for infra; runtime Moq verification failure for DT-1d)

## Phase 2: GREEN — Infrastructure Seam (CR-2e)

- [x] 2.1 `SupabaseBankCategoryTranslationRepository.cs`: extract the exact-match Supabase query into `internal virtual Task<BankCategoryTranslationRow?> ExecuteExactMatchQueryAsync(string bankCategory, string bankSubcategory)`.
- [x] 2.2 `SupabaseBankCategoryTranslationRepository.cs`: extract the generic-match Supabase query into `internal virtual Task<BankCategoryTranslationRow?> ExecuteGenericMatchQueryAsync(string bankCategory)`.
- [x] 2.3 Update call sites inside `FindByBankCategoryAsync` to call the two new seam methods; preserve existing exact-before-generic conditional logic.
- [x] 2.4 Run `dotnet test --filter "CR_2e_Infra"` — confirm 2 new infra tests go GREEN. ✓ (2/2 passed)

## Phase 3: GREEN — Batch Fix (DT-1d)

- [x] 3.1 `GetTransactionsQueryHandler.cs` lines 85-97: delete the `foreach` N+1 loop that calls `GetByIdAsync` per `categoryId`.
- [x] 3.2 Replace with batch pattern (identical to `GetRecentTransactionsQueryHandler` L51-52): `var categories = await _categoryRepo.GetByUserIdAsync(userId);` then `var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name.Value);`.
- [x] 3.3 Update DTO mapping to use `categoryLookup.TryGetValue(catId, out var catName)` (null-safe); confirm `CategoryName` remains populated when category exists.
- [x] 3.4 Run `dotnet test --filter "DT_1d"` — confirm 2 new handler tests go GREEN. ✓ (2/2 passed)

## Phase 4: Verify — Full Suite + Acceptance Gate

- [x] 4.1 Run `dotnet test` — all 386 tests pass (382 baseline + 4 new; 0 failures, 0 skips). ✓
- [x] 4.2 Grep `GetTransactionsQueryHandler.cs` for `GetByIdAsync` — zero occurrences. ✓
- [x] 4.3 Verify `SupabaseBankCategoryTranslationRepository.cs` exposes exactly 2 `internal virtual` seam methods; exact-before-generic guard preserved at line 50. ✓
- [x] 4.4 Verify no `[Skip]` attributes added anywhere in this change. ✓
