# Proposal: Dashboard Filter Charts & Legend Order

## Intent

The dashboard date filter (All Time / This Month / Last 3 Months / This Year) only affects summary cards. Charts ignore it, locked to a full calendar year. Additionally, Chart.js legend order does not match dataset order in the Spending by Category chart. Both issues make the dashboard inconsistent and misleading.

## Scope

### In Scope
- Extend date filter to all three dashboard charts (Spending by Category, Monthly Trends, Year over Year)
- Enforce deterministic legend ordering (category total descending) in all application charts
- Document legend-ordering rule in `.github/instructions/razor-frontend.instructions.md`
- Remove dead `dashboard-category-data` payload (serialized but never consumed)
- Write missing tests for `GetMonthlySpendingByCategoryQuery`

### Out of Scope
- Custom date range UI (exists in `CalculateDateRange` but not exposed in pill selector)
- New chart types or dashboard widgets
- Backend database schema changes

## Capabilities

### New Capabilities
- `dashboard-analytics-filter`: Date-range filtering for all dashboard charts, replacing year-only queries with `FromDate/ToDate` parameters
- `chart-legend-ordering`: Deterministic legend ordering rule (total descending) applied to all Chart.js instances in the application

### Modified Capabilities
None

## Approach

1. Replace `int Year` with `(DateTime FromDate, DateTime ToDate)` in `GetMonthlySpendingByCategoryQuery` and `GetMonthlyTrendsQuery`
2. Sort categories by total amount descending in handler (single source of truth for legend order)
3. Keep `GetYearlyComparisonQuery` as year-pair; PageModel computes `(Year1, Year2)` from filter
4. Remove dead `SpendingByCategory` payload path in `Dashboard.cshtml`/`.cs`
5. Add JSDoc contract in `charts.js` documenting data-array ordering expectation
6. Add "Charts" section to `razor-frontend.instructions.md` with legend-ordering rule
7. Write new `GetMonthlySpendingByCategoryQueryTests`; update existing tests for new signatures

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/.../Analytics/Queries/GetMonthlySpendingByCategoryQuery.cs` | Modified | Signature: `Year` → `FromDate/ToDate` |
| `src/.../Analytics/Queries/GetMonthlySpendingByCategoryQueryHandler.cs` | Modified | Date-range bucketing + category sort by total desc |
| `src/.../Analytics/Queries/GetMonthlyTrendsQuery.cs` | Modified | Signature: `Year` → `FromDate/ToDate` |
| `src/.../Analytics/Queries/GetMonthlyTrendsQueryHandler.cs` | Modified | Date-range iteration with zero-padding |
| `src/.../Pages/Dashboard.cshtml.cs` | Modified | Pass `FromDate/ToDate` to chart queries; compute YoY year pair |
| `src/.../Pages/Dashboard.cshtml` | Modified | Remove dead payload; update chart titles |
| `src/.../wwwroot/js/charts.js` | Modified | JSDoc contract for data-array ordering |
| `src/.../Pages/Budgets/Comparison.cshtml` | Modified | Align inline Chart with legend-ordering rule |
| `.github/instructions/razor-frontend.instructions.md` | Modified | New "Charts" section |
| `tests/.../GetMonthlySpendingByCategoryQueryTests.cs` | New | RED-GREEN-REFACTOR from scratch |
| `tests/.../GetMonthlyTrendsQueryTests.cs` | Modified | Update for date-range signature |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Breaking change to query records | Low | Only caller is `Dashboard.cshtml.cs` (verified); comprehensive tests |
| Application layer couples to UI ordering | Low | Explicit JSDoc + instructions doc; single sort key |
| YoY semantics under sub-year filters | Medium | PageModel computes years deterministically; chart title clarifies pair |
| Empty data under "All Time" with sparse months | Low | Zero-pad monthly trend series |

## Rollback Plan

Revert the merge commit. No database migrations involved. Single deployable unit — clean git revert restores previous behavior.

## Dependencies

- Existing `CalculateDateRange()` in `Dashboard.cshtml.cs` (already computes `FromDate/ToDate`)
- Chart.js CDN + `charts.js` init functions (already wired with `destroyAllCharts()` for HTMX)

## Success Criteria

- [ ] All three dashboard charts respond to the pill date filter
- [ ] Chart legend order matches dataset order in every chart instance
- [ ] `GetMonthlySpendingByCategoryQuery` has unit test coverage (new file)
- [ ] Existing chart query tests pass with updated signatures
- [ ] Legend-ordering rule documented in `razor-frontend.instructions.md`
- [ ] Dead `dashboard-category-data` payload removed
- [ ] `dotnet test` passes; `dotnet build` clean
