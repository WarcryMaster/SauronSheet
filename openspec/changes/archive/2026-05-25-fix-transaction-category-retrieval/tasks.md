# Tasks: fix-transaction-category-retrieval

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 190–260 |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Whitespace trim at import handler (CR-1c) | PR 1 | Base = `feature/fix-transaction-category-retrieval`; trivial; includes regression test |
| 2 | Translation exact-before-generic precedence (CR-2e) | PR 2 | Base = `slice/fix-import-trim`; infra-only; includes regression test |
| 3 | SubcategoryName batch population in 3 query handlers (DT-1b, DT-1c) | PR 3 | Base = `slice/fix-translation-precedence`; includes unit tests |

## Phase 1: Slice 1 — Whitespace Trim (PR 1, CR-1c)

- [x] 1.1 **RED** — Add test in `ImportTransactionsFromPdfCommandHandlerTests.cs`: assert stored `BankCategory`/`BankSubcategory` are trimmed when input has surrounding whitespace (spec scenario CR-1c).
- [x] 1.2 **GREEN** — `ImportTransactionsFromPdfCommandHandler.cs` (~line 175–176): apply `.Trim()` to `row.Category` and `row.SubCategory?.Trim()` before `Transaction` ctor call.
- [x] 1.3 `dotnet test` — verify CR-1a, CR-1b, CR-1c green; no regressions.
- [ ] 1.4 Open PR 1 targeting `feature/fix-transaction-category-retrieval`.

## Phase 2: Slice 2 — Translation Precedence (PR 2, CR-2e)

- [x] 2.1 **RED** — Add test in `BankCategoryResolutionServiceTests.cs` (or `SupabaseBankCategoryTranslationRepositoryTests.cs`): exact (bank_category + bank_subcategory) row wins over generic (bank_subcategory=null) row (spec scenario CR-2e).
- [x] 2.2 **GREEN** — `SupabaseBankCategoryTranslationRepository.cs`: add `if (!string.IsNullOrEmpty(bankSubcategory))` guard → execute exact-match query FIRST; generic null-subcategory query as fallback only.
- [x] 2.3 `dotnet test` — verify CR-2a through CR-2e green; no regressions.
- [ ] 2.4 Open PR 2 targeting `slice/fix-import-trim` branch.

## Phase 3: Slice 3 — SubcategoryName Population (PR 3, DT-1b/DT-1c)

- [x] 3.1 **RED** — Add tests in `GetRecentTransactionsQueryTests.cs`: `SubcategoryName` populated from mocked `ISubcategoryRepository.GetByUserIdAsync` (DT-1b); `SubcategoryName` null when `SubcategoryId=null` (DT-1c). Also added DT-1b/DT-1c tests in new `GetTransactionsQueryHandlerTests.cs` and updated `SearchTransactionsQueryTests.cs`. RED confirmed via CS1729 compilation errors.
- [x] 3.2 **GREEN** — `GetRecentTransactionsQueryHandler.cs`: injected `ISubcategoryRepository` via ctor; batch-fetched via `GetByUserIdAsync(userId)`; built `Dictionary<SubcategoryId, string>`; populated `SubcategoryName` in DTO mapping with `.TryGetValue`. 5/5 tests green.
- [x] 3.3 **GREEN** — `GetTransactionsQueryHandler.cs`: same injection + batch-fetch + DTO mapping pattern; added `GetTransactionsQueryHandlerTests.cs` with parallel DT-1b/DT-1c unit tests. 2/2 new tests green.
- [x] 3.4 **GREEN** — `SearchTransactionsQueryHandler.cs`: same pattern; added DT-1b/DT-1c tests in `SearchTransactionsQueryTests.cs`. 10/10 tests green (8 existing + 2 new).
- [x] 3.5 `dotnet test` — 382/382 passing. All DT-1b and DT-1c scenarios green across all three handlers. Zero regressions.
- [ ] 3.6 Open PR 3 targeting `slice/fix-translation-precedence` branch.
