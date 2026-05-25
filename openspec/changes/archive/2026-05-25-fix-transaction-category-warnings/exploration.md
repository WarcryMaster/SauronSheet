## Exploration: fix-transaction-category-warnings

### Current State
- Warning 1 is confirmed from real code: `src/SauronSheet.Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` already does exact-before-generic lookup (lines 40-56), but `tests/SauronSheet.Infrastructure.Tests/Persistence/SupabaseBankCategoryTranslationRepositoryTests.cs` only verifies interface signatures and constructor wiring. There is no automated repository-level behavior test for CR-2e. The existing `BankCategoryResolutionServiceTests` mock `IBankCategoryTranslationRepository`, so an infra regression would not fail there.
- Warning 2 is also confirmed: `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` still builds distinct category IDs and then loops with repeated `_categoryRepo.GetByIdAsync(...)` calls (lines 85-97). That is a pre-existing N+1 category lookup pattern. Its sibling handlers already batch category loading with `GetByUserIdAsync(userId)`.

### Affected Areas
- `src/SauronSheet.Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` — repository behavior under test; may need a tiny seam only if the current `Supabase.Client` chain is too hard to exercise directly.
- `tests/SauronSheet.Infrastructure.Tests/Persistence/SupabaseBankCategoryTranslationRepositoryTests.cs` — currently contract-only; must gain behavior coverage for exact-before-generic fallback.
- `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` — replace per-ID category fetch loop with a single batched category load.
- `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetTransactionsQueryHandlerTests.cs` — add regression coverage for category-name mapping plus batching/no-`GetByIdAsync` behavior.

### Approaches
1. **Single bounded corrective change** — close both archived warnings together.
   - Pros: same reviewer context; still a small corrective scope; likely well under the single-PR budget.
   - Cons: crosses Application and Infrastructure; repository coverage may need a small test seam because there is no existing real Supabase behavioral harness in the repo.
   - Effort: Medium

2. **Split by layer** — one change for repository coverage, one for query batching.
   - Pros: isolates any infrastructure testability work.
   - Cons: adds process overhead for two small leftovers; delays closure of the archived warnings.
   - Effort: Medium

### Recommendation
Use one bounded change with slug `fix-transaction-category-warnings`. Both items are explicitly tracked warnings from the same archived change, and the expected code delta is small. Testing approach: add an infrastructure-focused regression around `SupabaseBankCategoryTranslationRepository` for exact-before-generic precedence, and add an application unit test for `GetTransactionsQueryHandler` that verifies batched category lookup via `GetByUserIdAsync` and no repeated `GetByIdAsync` calls.

### Risks
- The repository warning is not just “write one more test”: current Infrastructure tests are mostly signature/contract checks, and the repo has no established live-Supabase test fixture. If the fluent `Supabase.Client` API cannot be exercised cleanly, a tiny seam may be needed to keep the test bounded.
- The N+1 fix must stay inside existing Clean Architecture boundaries. Reuse `ICategoryRepository.GetByUserIdAsync(userId)`; do not introduce Infrastructure leakage or a broader query refactor.

### Ready for Proposal
Yes — propose a single corrective change focused on closing the repository-test gap and removing the `GetTransactionsQueryHandler` category N+1 pattern under strict TDD.
