## Exploration: fix-transaction-category-retrieval

### Current State
The archived change was only partially completed. The import flow already resolves category and subcategory IDs and persists `BankCategory`, `BankSubcategory`, `SubcategoryId`, and `CategorySource`, but three gaps remain in current code:
- Read-side transaction query handlers expose `SubcategoryId` but never populate `SubcategoryName`, even though `TransactionDto` includes it.
- `SupabaseBankCategoryTranslationRepository` returns the generic `(bank_category, bank_subcategory IS NULL)` translation before checking the exact `(bank_category, bank_subcategory)` pair, which is the opposite of the archived design.
- `ImportTransactionsFromPdfCommandHandler` persists `row.Category` and `row.SubCategory` as-is. Trimming happens for resolution only, not for stored raw values, so the spec contract of whitespace trim only is not enforced at the write boundary.

Additional risk found during code review: the current tests cover DTO shape and import mapping, but they do not assert read-side `SubcategoryName` population or exact translation precedence, which is why these gaps could survive archive.

### Affected Areas
- `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` - maps `TransactionDto` without `SubcategoryName`.
- `src/SauronSheet.Application/Features/Transactions/Queries/GetRecentTransactionsQueryHandler.cs` - same omission on recent transactions.
- `src/SauronSheet.Application/Features/Transactions/Queries/SearchTransactionsQueryHandler.cs` - same omission on search results.
- `src/SauronSheet.Application/Features/Transactions/DTOs/TransactionDto.cs` - DTO contract already expects `SubcategoryName`, exposing the read-side mismatch.
- `src/SauronSheet.Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` - generic translation is queried before exact category plus subcategory translation.
- `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs` - raw bank values are persisted without trim.
- `tests/SauronSheet.Application.Tests/Services/BankCategoryResolutionServiceTests.cs` - should gain regression coverage for exact-over-generic translation precedence.
- `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetRecentTransactionsQueryTests.cs` - currently does not verify `SubcategoryName` mapping.
- `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/SearchTransactionsQueryTests.cs` - currently does not verify `SubcategoryName` mapping.

### Approaches
1. **Surgical corrective patch**
   - Pros: Smallest change set; aligns directly with archived spec and design; likely stays within the review budget.
   - Cons: Read-side mapping logic remains duplicated across three handlers unless a tiny shared helper is introduced.
   - Effort: Low

2. **Corrective patch plus read-model consolidation**
   - Pros: Fixes the same bugs while centralizing transaction-to-DTO enrichment, reducing future drift.
   - Cons: Broader scope for a corrective change; higher regression surface; more likely to push review size upward.
   - Effort: Medium

### Recommendation
Use approach 1. Fix translation lookup order, trim raw bank values before transaction creation, and enrich the three transaction query handlers with subcategory name lookup plus regression tests. Keep the change corrective and narrow; if duplication becomes noisy, use a small shared helper in Application rather than a larger refactor.

### Risks
- Adding subcategory-name enrichment can introduce extra repository calls or N+1 behavior if the handler fetch strategy is not batched.
- The import trim fix may change persisted literals for future imports; tests should lock the intended contract clearly.
- Because the archived change is already marked complete, missing regression tests are the main risk for reintroducing these gaps later.

### Ready for Proposal
Yes - the orchestrator should tell the user that the corrective proposal can stay tightly scoped to three confirmed defects (`SubcategoryName` read mapping, exact translation precedence, trim-on-write) plus regression tests, with no broader redesign required.
