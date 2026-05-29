# Design: Consolidar presupuestos mensuales por categoria

## Technical Approach

Realign existing budget code with the new `monthly-budgets` spec by correcting two threshold/logic mismatches and adding missing E2E coverage. No new entities, no schema migration, no new CQRS endpoints. This is consolidation, not expansion.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|--------------|-----------|
| Status thresholds location | Domain (`BudgetService.GetStatusLevel`) | Application handler; enum with [Range] | Single source of truth in domain service; handlers consume, never duplicate |
| "On track" semantics | Green or Yellow only (< 100%) | Not-Overage (current impl) | Spec defines Red (= 100%) as NOT on track; current code treats Red as on-track incorrectly |
| Threshold values | Green < 75%, Yellow 75%-<100%, Red = 100%, Overage > 100% | Keep current 60/80/100 | Spec-driven — spec is the contract, code aligns |
| Dashboard widget period | Always `DateTime.UtcNow` month (already implemented) | Respect DateFilter | Spec says "no period selector in widget"; code is already correct, just needs explicit test |
| Unbudgeted categories in comparison | Keep current implementation (working) | Add "No budget" VO | `GetBudgetVsActualQueryHandler` already includes them with null limits — matches spec |
| E2E test infrastructure | Extend existing `auth.fixture.ts` + new spec file | Separate fixture | Reuse existing pattern; single worker, sequential budget CRUD |

## Data Flow

```
Dashboard.cshtml.cs (OnGetAsync)
       │
       ├─ GetBudgetSummaryForDashboardQuery(now.Year, now.Month)
       │         │
       │         ▼
       │  DashboardQueryHandler
       │    ├─ IBudgetRepository.GetByUserIdAsync() → filter by period
       │    ├─ ITransactionRepository.FindBySpec() → expenses in period
       │    ├─ Budget.PercentageUsed(spend) → domain calc
       │    ├─ BudgetService.GetStatusLevel(%) → Green/Yellow/Red/Overage
       │    └─ Aggregate: OnTrack = count(Green + Yellow)  ← FIX HERE
       │
       ▼
Budgets/Comparison.cshtml.cs
       │
       ├─ GetBudgetVsActualQuery(year, month)
       │         │
       │         ▼
       │  VsActualQueryHandler
       │    ├─ same repos + domain calcs
       │    └─ unbudgeted cats → BudgetLimit=null (already correct)
       │
       ▼
  Domain Layer (no changes to entity/repo/VO contracts)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/SauronSheet.Domain/Services/BudgetService.cs` | Modify | Change thresholds: Green < 0.75, Yellow 0.75–<1.0, Red == 1.0, Overage > 1.0 |
| `src/SauronSheet.Domain/ValueObjects/BudgetStatusLevel.cs` | Modify | Update XML doc comments to match new thresholds |
| `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandler.cs` | Modify | Fix "on track" count: include only Green + Yellow (exclude Red and Overage) |
| `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs` | Modify | Update threshold assertions to new spec boundaries (75/100) |
| `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandlerTests.cs` | Modify | Add/fix test: Red status is NOT on-track |
| `e2e/tests/03-budgets.spec.ts` | Create | E2E: create budget, view comparison, verify dashboard widget |

## Interfaces / Contracts

No new interfaces. Only the static method signature stays the same with new internal logic:

```csharp
// Domain — threshold change only (signature unchanged)
public static BudgetStatusLevel GetStatusLevel(decimal percentageUsed)
{
    return percentageUsed switch
    {
        > 1.0m => BudgetStatusLevel.Overage,
        1.0m   => BudgetStatusLevel.Red,
        >= 0.75m => BudgetStatusLevel.Yellow,
        _ => BudgetStatusLevel.Green
    };
}
```

Dashboard handler "on track" fix (line 120):
```csharp
// Before: periodBudgets.Count - overBudgetCount
// After:  count where status is Green or Yellow
var onTrackCount = budgetStatuses.Count(s =>
    s.StatusLevel == "Green" || s.StatusLevel == "Yellow");
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (Domain) | `GetStatusLevel` boundary values at 0.74, 0.75, 0.99, 1.0, 1.01 | Modify existing `BudgetServiceTests` — red-green-refactor |
| Application | Dashboard handler returns Red as NOT on-track | Update `GetBudgetSummaryForDashboardQueryHandlerTests` |
| Application | VsActual handler — verify unbudgeted cat labeled correctly | Verify existing `GetBudgetVsActualQueryHandlerTests` covers it |
| E2E | Create budget → navigate Comparison → check dashboard widget | New `03-budgets.spec.ts` using `auth.fixture.ts` |

## Migration / Rollout

No migration required. The `budgets` table schema is unchanged. All changes are logic-only in Domain and Application layers. Rollback is `git revert`.

## Open Questions

- None. All decisions are resolved by the spec.
