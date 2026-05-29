# Design: budgets-follow-up-quality-gaps

## Technical Approach

Two stacked PRs, zero production code changes. Slice 1 adds unit tests for three uncovered Domain classes to cross the 80 % threshold. Slice 2 introduces a Playwright fixture that provisions deterministic data via UI flows, then removes the `test.skip` guards from `03-budgets.spec.ts`.

## Architecture Decisions

| Decision | Alternatives | Rationale |
|----------|-------------|-----------|
| Test `DuplicateEntityException` + `BankCategoryTranslation` + `TransactionByMultipleImportedFromsSpec` as coverage targets | Only first two; or `UserProfile` (14 lines) | Three classes yield +19 possible lines (9+5+5), safely clearing the 14-line gap needed. Third is a cheap backup if Coverlet doesn't count all branches. |
| Playwright fixture in `e2e/fixtures/budget-data.fixture.ts` using existing UI flows | Direct Supabase API calls; `supabase/seed.sql`; migration-based seed | UI flows exercise the real stack and don't require `service_role` key exposure. Seed/migration path is unsuitable because the frontend connects to hosted Supabase, not local, and `public.users` depends on `auth.users`. |
| Fixture uses `test.extend<>` Playwright pattern (same as `auth.fixture.ts`) | Global setup script; `beforeAll` in test file | Extends the existing fixture convention in `e2e/fixtures/`; composable, idempotent, and reusable. |
| Idempotency via DOM check before creation | Database query via API; no check | Checking for existing elements on the page (category already in list, transaction already present) keeps the fixture infrastructure-agnostic and consistent with the "no direct DB access" constraint. |

## Data Flow

```
Fixture (budget-data.fixture.ts)
  │
  ├─ 1. Login (reuse auth dual-path pattern)
  ├─ 2. Navigate /categories → check if "E2E-Budget-Cat-A" exists
  │     └─ If missing → create via Categories modal form
  ├─ 3. Check "E2E-Budget-Cat-B" exists → create if missing
  ├─ 4. Navigate /transactions/add → check current-month expense
  │     └─ If missing → add expense (-25€) on "E2E-Budget-Cat-B", today's date
  └─ 5. Yield authenticated page to test
         └─ 03-budgets.spec.ts runs TC-B01/B02/B03 without skips
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `tests/SauronSheet.Domain.Tests/Exceptions/DuplicateEntityExceptionTests.cs` | Create | Tests 3 constructors, message format, inner exception |
| `tests/SauronSheet.Domain.Tests/Records/BankCategoryTranslationTests.cs` | Create | Tests construction, equality, and `with` expression |
| `tests/SauronSheet.Domain.Tests/Specifications/TransactionByMultipleImportedFromsSpecificationTests.cs` | Create | Tests multi-source match, case-insensitivity, null guard |
| `e2e/fixtures/budget-data.fixture.ts` | Create | Idempotent data provisioning fixture via UI flows |
| `e2e/tests/03-budgets.spec.ts` | Modify | Import fixture, remove conditional `test.skip` blocks, use deterministic category names |

## Interfaces / Contracts

No new production interfaces. The fixture exports:

```typescript
// e2e/fixtures/budget-data.fixture.ts
export const test: TestType<{ budgetReadyPage: Page }>;
export { expect } from '@playwright/test';
```

Tests import `{ test, expect }` from the fixture instead of `@playwright/test`.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (Slice 1) | `DuplicateEntityException` constructors; `BankCategoryTranslation` record semantics; `TransactionByMultipleImportedFromsSpec` filter logic | xUnit + `[Trait("Category","Domain")]`, follow `EntityNotFoundExceptionTests` / `TransactionByImportedFromSpecificationTests` patterns |
| E2E (Slice 2) | TC-B01, TC-B02, TC-B03 run green with deterministic data | Playwright chromium, fixture provisions data before suite |

## Migration / Rollout

No migration required. No production code modified.

## Open Questions

- None blocking.
