# Tasks: budgets-follow-up-quality-gaps

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~150–250 total (PR 1: ~90–130 · PR 2: ~100–120) |
| 400-line budget risk | Low |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 (stacked to main) |
| Delivery strategy | auto-chain |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain unit tests → coverage ≥ 80 % | PR 1 | Base = main; self-contained; zero prod changes |
| 2 | E2E fixture + remove `test.skip` guards | PR 2 | Base = PR 1 branch; depends on PR 1 merged |

---

## Phase 1: Baseline Domain (PR 1 — RED)

- [x] 1.1 Run `dotnet test tests/SauronSheet.Domain.Tests --collect:"XPlat Code Coverage"` — confirm coverage is **below 80 %** (expected ~78.11 %, 432/553 lines).
- [x] 1.2 Inspect `tests/SauronSheet.Domain.Tests/Exceptions/EntityNotFoundExceptionTests.cs` and `Specifications/TransactionByImportedFromSpecificationTests.cs` — note naming/trait conventions to follow.

## Phase 2: Domain Test Files (PR 1 — GREEN)

- [x] 2.1 Create `tests/SauronSheet.Domain.Tests/Exceptions/DuplicateEntityExceptionTests.cs` — cover 3 constructors (message, type+message, type+message+inner), message format assertion, and inner exception chaining. Use `[Trait("Category","Domain")]`.
- [x] 2.2 Create `tests/SauronSheet.Domain.Tests/Records/BankCategoryTranslationTests.cs` *(new folder)* — cover construction, value equality between two identical instances, and `with`-expression mutation.
- [x] 2.3 Create `tests/SauronSheet.Domain.Tests/Specifications/TransactionByMultipleImportedFromsSpecificationTests.cs` — cover multi-source match, case-insensitive filter, and null/empty input guard.

## Phase 3: Coverage Gate (PR 1 — VERIFY)

- [x] 3.1 Run `dotnet test tests/SauronSheet.Domain.Tests --collect:"XPlat Code Coverage"` — assert **≥ 446/553 lines (> 80 %)**.  If short, add backup class (`TransactionByMultipleImportedFromsSpec`) tests or similar until threshold is confirmed.
- [x] 3.2 Run `dotnet build` — zero new warnings (TreatWarningsAsErrors in Domain project).

---

## Phase 4: E2E Baseline (PR 2 — RED)

- [ ] 4.1 On the PR 2 branch (stacked on PR 1), run `npx playwright test --project=chromium e2e/tests/03-budgets.spec.ts` — confirm TC-B01/B02/B03 are skipped or fail without deterministic data provisioning.

## Phase 5: Fixture (PR 2 — GREEN)

- [ ] 5.1 Create `e2e/fixtures/budget-data.fixture.ts` using `test.extend<{ budgetReadyPage: Page }>` — follow `e2e/fixtures/auth.fixture.ts` pattern. Implement these idempotent steps in order:
  1. Login via existing auth dual-path
  2. Navigate `/categories` → create `"E2E-Budget-Cat-A"` via modal if missing (DOM check)
  3. Create `"E2E-Budget-Cat-B"` if missing
  4. Navigate `/transactions/add` → add expense `−25 €` on `"E2E-Budget-Cat-B"`, current-month date, if missing
  5. Yield `budgetReadyPage` to the suite
- [ ] 5.2 Export `{ test, expect }` from the fixture so tests import from `../fixtures/budget-data.fixture` instead of `@playwright/test`.

## Phase 6: Wire Suite (PR 2 — GREEN)

- [ ] 6.1 Modify `e2e/tests/03-budgets.spec.ts` — replace `@playwright/test` import with `../fixtures/budget-data.fixture`.
- [ ] 6.2 Replace all conditional `test.skip` guards in TC-B01, TC-B02, TC-B03 — none should remain.
- [ ] 6.3 Replace any dynamic category name references with `"E2E-Budget-Cat-B"` where the budget scenarios depend on it.

## Phase 7: E2E Verification (PR 2 — VERIFY / REFACTOR)

- [ ] 7.1 Run `npx playwright test --project=chromium e2e/tests/03-budgets.spec.ts` — all tests green, **zero skipped**.
- [ ] 7.2 Re-run the suite immediately (second consecutive run) — fixture must detect existing data and create no duplicates (idempotency check).
- [ ] 7.3 Run `dotnet build` — confirm zero new warnings.
