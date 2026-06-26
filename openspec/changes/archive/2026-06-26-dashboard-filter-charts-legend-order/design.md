# Design: Dashboard Filter Charts & Legend Order

## Technical Approach

Extend the pill date filter from summary cards to all three dashboard charts. Replace `int Year` with `(DateTime FromDate, DateTime ToDate)` in monthly queries, following the existing `GetSpendingByCategoryQuery` pattern. Enforce category sort (total descending) in the `GetMonthlySpendingByCategoryQueryHandler` so Chart.js legends match dataset order automatically. Add `Year` to monthly DTOs for multi-year range support. Remove dead `dashboard-category-data` payload. Document the legend-ordering rule.

## Architecture Decisions

| # | Decision | Options Considered | Choice | Rationale |
|---|----------|-------------------|--------|-----------|
| D1 | Monthly query signatures | A: `Year` → `FromDate/ToDate` · B: keep `Year`, add filter | A | Follows existing `GetSpendingByCategoryQuery` pattern; single source of truth for date range |
| D2 | DTO year field | A: add `Year` to `MonthlyTrendDto`/`MonthlyCategorySpendingDto` · B: resolve to `AnalyticsYear` in PageModel | A | "All Time" can span years; chart needs per-entry year for correct labels |
| D3 | Legend sort location | A: handler sorts desc · B: frontend sorts before chart init | A | Single source of truth; backend controls data contract; frontend stays dumb |
| D4 | YoY year resolution | A: `ToDate.Year` / `ToDate.Year - 1` · B: complex filter-based logic | A | Deterministic, simple, works for all filter values |
| D5 | Monthly iteration range | A: full calendar months overlapping range · B: exact day boundaries | A | Chart groups by month; partial months still aggregate correctly under their calendar month label |

## Data Flow

```
Pill selector (hx-get ?DateFilter=xxx)
    │
    ▼
DashboardModel.OnGetAsync()
    ├── CalculateDateRange() → FromDate, ToDate
    ├── GetSpendingByCategoryQuery(FromDate, ToDate)           ← already date-range
    ├── GetMonthlySpendingByCategoryQuery(FromDate, ToDate)    ← CHANGED
    ├── GetMonthlyTrendsQuery(FromDate, ToDate)                ← CHANGED
    └── GetYearlyComparisonQuery(ToDate.Year-1, ToDate.Year)  ← CHANGED resolution
            │
            ▼
    Handler: spec date range → group by month+category
            → sort categories by total desc (deterministic legend order)
            → return List<MonthlyCategorySpendingDto> with Year field
            │
            ▼
    <script type="application/json"> → charts.js
            → initCategoryStackedChart() reads categories in array order
            → legend matches chart because data is pre-sorted
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/.../Queries/GetMonthlySpendingByCategoryQuery.cs` | Modify | `int Year` → `DateTime FromDate, DateTime ToDate` |
| `src/.../Queries/GetMonthlySpendingByCategoryQueryHandler.cs` | Modify | Date-range spec; sort categories by total amount desc |
| `src/.../DTOs/MonthlyCategorySpendingDto.cs` | Modify | Add `int Year` field |
| `src/.../Queries/GetMonthlyTrendsQuery.cs` | Modify | `int Year` → `DateTime FromDate, DateTime ToDate` |
| `src/.../Queries/GetMonthlyTrendsQueryHandler.cs` | Modify | Iterate calendar months in range; use entry Year |
| `src/.../DTOs/MonthlyTrendDto.cs` | Modify | Add `int Year` field |
| `src/.../Pages/Dashboard.cshtml.cs` | Modify | Pass `FromDate/ToDate` to all chart queries; YoY uses `ToDate.Year` |
| `src/.../Pages/Dashboard.cshtml` | Modify | Remove dead `dashboard-category-data` JSON block; update chart title logic |
| `src/.../wwwroot/js/charts.js` | Modify | `initMonthlyTrendsChart` uses `d.year`+`d.monthName` for labels; JSDoc data-ordering contract |
| `.github/instructions/razor-frontend.instructions.md` | Modify | New "Charts" section with legend-ordering rule |
| `tests/.../GetMonthlySpendingByCategoryQueryTests.cs` | Create | RED-GREEN: sort order, date-range filtering, empty data |
| `tests/.../GetMonthlyTrendsQueryTests.cs` | Modify | Update for `FromDate/ToDate` signature |

## Interfaces / Contracts

```csharp
// Query signatures after change
public record GetMonthlySpendingByCategoryQuery(
    DateTime FromDate, DateTime ToDate) : IRequest<List<MonthlyCategorySpendingDto>>;

public record GetMonthlyTrendsQuery(
    DateTime FromDate, DateTime ToDate) : IRequest<List<MonthlyTrendDto>>;

// DTOs after change (Year added)
public record MonthlyCategorySpendingDto(
    int Year, int Month, string MonthName, string CategoryName, decimal Amount);

public record MonthlyTrendDto(
    int Year, int Month, string MonthName,
    decimal TotalExpenses, decimal TotalIncome,
    decimal NetAmount, string Currency, int TransactionCount);
```

**Legend-ordering contract (JSDoc in charts.js):**
```
// PRECONDITION: monthlyCategoryData MUST be sorted by handler so that
// categories appear in descending total-amount order.
// Chart.js renders legend in dataset insertion order — no frontend sort needed.
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit (RED first) | `GetMonthlySpendingByCategoryQueryHandler`: categories sorted by total desc; date-range filtering; empty result; multi-year range | xUnit + Moq, following `GetSpendingByCategoryQueryTests` pattern |
| Unit (update) | `GetMonthlyTrendsQueryHandler`: calendar-month iteration; Year field populated; zero-fill for months without data | xUnit + Moq, update existing test file |
| Integration | None required — handlers are thin orchestrators over mocked repos | — |
| E2E | Dashboard pill filter changes chart data; legend order matches visual | Playwright — verify chart JSON blocks change with filter |

## Migration / Rollout

No database migration. Query record signature change is a breaking compile-time change — only caller is `Dashboard.cshtml.cs` (verified). DTO `Year` field addition is additive for JSON consumers. Single deployable unit; clean `git revert` for rollback.

## Open Questions

- None
