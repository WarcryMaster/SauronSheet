# Exploration: dashboard-filter-charts-legend-order

## Topic

Two related issues on `/dashboard`:

1. The pill-style date filter (All Time / This Month / Last 3 Months / This Year) only affects the summary cards. The three charts are locked to a full calendar year resolved from `AnalyticsYear`.
2. Chart.js legend order does not always match the dataset order rendered in the chart. The user wants a single, explicit rule applied to every chart in the app, and that rule documented in the appropriate `AGENTS.md`-linked instructions file.

## Current State

### Filter flow

`Dashboard.cshtml.cs` exposes a `[BindProperty(SupportsGet = true)] public string DateFilter { get; set; } = "all";`. `CalculateDateRange()` produces a `(FromDate, ToDate)` pair:

- `all` -> `DateTime.MinValue, now.Date`
- `this-month` -> first day of current month .. now.Date
- `last-3-months` (default fallback) -> three months back .. now.Date
- `this-year` -> `DateTime(now.Year, 1, 1)` .. now.Date
- `last-month` and `custom` are also defined but **not exposed in the pill UI**.

`ResolveAnalyticsYearAsync()` returns `currentYear` for any non-`all` filter, or the year of the latest transaction for `all`. This single year is the only thing fed to the chart queries.

### Query contract today

| Query | Signature | Locks to | DTO ordering |
|---|---|---|---|
| `GetTransactionSummaryQuery` | `(DateTime FromDate, DateTime ToDate)` | date range | n/a (scalar) |
| `GetSpendingByCategoryQuery` | `(DateTime FromDate, DateTime ToDate)` | date range | `OrderByDescending(c => c.Amount)` then top-6 + "Other" |
| `GetMonthlySpendingByCategoryQuery` | `(int Year)` | full calendar year `Jan 1 - Dec 31 23:59:59` | `GroupBy` iteration order — **no explicit sort** |
| `GetMonthlyTrendsQuery` | `(int Year)` | full calendar year | months 1..12 in order (deterministic) |
| `GetYearlyComparisonQuery` | `(int Year1, int Year2)` | two full calendar years | months 1..12 in order (deterministic) |

`GetMonthlySpendingByCategoryQueryHandler` has no test coverage yet (`tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlySpendingByCategoryQueryTests.cs` does not exist). The other three chart queries are tested but the assertions are scoped to a fixed year.

### Dashboard wiring (`Dashboard.cshtml`)

- Three canvas elements: `categoryStackedChart`, `monthlyTrendsChart`, `yearlyComparisonChart`.
- Data flows as `<script type="application/json" id="...">` blocks (ADR-0002 compliant), parsed by `initCharts()` in the `x-data` Alpine component.
- `initCategoryStackedChart`, `initMonthlyTrendsChart`, `initYearlyComparisonChart` all live in `wwwroot/js/charts.js`.
- `destroyAllCharts()` is wired into `htmx:beforeSwap` so the pill selector can re-render without leaking Chart.js instances.
- `dashboard-category-data` (the pie `SpendingByCategory` payload) is serialized but never read by any init function. **Dead/legacy payload** — worth removing as part of this change.

### Legend / dataset ordering today

- `initCategoryStackedChart` (charts.js L107-172) builds the category list with `[...new Set(monthlyCategoryData.map(d => d.categoryName))]`. Because `new Set` preserves insertion order, the legend order equals the order in which categories first appear in the data array. The data array comes from `GroupBy` over the LINQ-to-objects pipeline in the handler — **insertion order is not deterministic against PostgreSQL/Supabase ordering, and is not sorted by total amount**. The user has reported that "Compras" is not always first even when it is the largest category.
- `initMonthlyTrendsChart` (charts.js L180-228) and `initYearlyComparisonChart` (charts.js L237-318) have a fixed, hand-coded `datasets` array. Legend order is whatever the developer wrote — already consistent with the rendering order, but the rule is implicit, not enforced.
- `Budgets/Comparison.cshtml` L66-99 inlines a `new Chart(canvas, {...})` with two datasets (`Budget`, `Actual`). Legend order is fixed; the only "order risk" is that the rule isn't documented anywhere.

### Other charts in the application

Only one: `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml` (horizontal bar chart for Budget vs Actual). It does not use the `charts.js` helpers and does not currently exhibit the bug, but the new legend-ordering rule should apply to it too.

### Documentation surface

`AGENTS.md` lists six auto-loaded instruction files. The one that covers `.cshtml` and frontend JavaScript — i.e., where a Chart.js rule belongs — is:

- `.github/instructions/razor-frontend.instructions.md` (`applyTo: "**/*.cshtml"`)

It already has dedicated sections for MDBootstrap, Alpine.js, Flatpickr and the `<template x-for>` `<select>` workaround, so a "Charts" section fits the existing pattern.

## Affected Areas

- `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlySpendingByCategoryQuery.cs` — record signature must change to accept a date range.
- `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlySpendingByCategoryQueryHandler.cs` — bucketize by month, sort categories by total descending, return DTOs in the desired legend order.
- `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlyTrendsQuery.cs` — record signature must change to accept a date range.
- `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlyTrendsQueryHandler.cs` — bucketize by month within the range; pad missing months with zeros so the chart x-axis stays continuous.
- `src/SauronSheet.Application/Features/Analytics/Queries/GetYearlyComparisonQuery.cs` — keep two-year semantics, but anchor the year selection to the filter so the title still makes sense.
- `src/SauronSheet.Application/Features/Analytics/Queries/GetYearlyComparisonQueryHandler.cs` — unchanged behaviour for the data, but document the assumption.
- `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs` — pass `FromDate/ToDate` to the chart queries; compute chart titles from the active filter; remove the dead `SpendingByCategory` payload path if decided in scope.
- `src/SauronSheet.Frontend/Pages/Dashboard.cshtml` — update chart titles; pass the new period label to the init functions; remove dead `dashboard-category-data` block.
- `src/SauronSheet.Frontend/wwwroot/js/charts.js` — `initCategoryStackedChart` must trust the order of the data array (it already does) and not re-shuffle; add a defensive sort fallback so a buggy handler cannot regress the legend. Document the contract via JSDoc.
- `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml` — minor: align the `new Chart(...)` block with the documented legend-ordering rule. (Two fixed datasets; very low risk.)
- `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlySpendingByCategoryQueryTests.cs` — **new** test file (none exists today). Must cover: (a) date range buckets correctly, (b) categories returned in total-amount-descending order so the legend matches the chart, (c) empty input returns empty list.
- `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlyTrendsQueryTests.cs` — update for new date-range signature; add cases for ranges shorter than 12 months.
- `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetYearlyComparisonQueryTests.cs` — confirm the year-pair behaviour is preserved after refactor.
- `.github/instructions/razor-frontend.instructions.md` — new "Charts" section: legend-ordering rule, required data ordering, when/how to extend `charts.js` vs. inline `new Chart(...)`, link to `DESIGN.md` for color tokens.

## Approaches

### A. Filter scope (which signature to use for the chart queries)

1. **Replace `Year` with `(FromDate, ToDate)` in all three chart queries** (recommend).
   - Pros: one contract; mirrors `GetSpendingByCategoryQuery` and `GetTransactionSummaryQuery`; natural extension for any future "Last 30 days" filter; one place to test.
   - Cons: `GetYearlyComparisonQuery` is semantically a year-comparison; the handler must either still anchor to two years (computed from the filter) or accept that the range is clamped to full years. The PageModel can do the year selection and pass two dates; the handler stays a pure year-pair comparison.
   - Effort: Medium.

2. **Keep year-based queries, add a `DateFilter` parameter**.
   - Pros: each handler keeps a single shape; less work in the YoY handler.
   - Cons: two ways of expressing time windows; handlers grow branching logic; harder to compose with custom date ranges later.
   - Effort: Medium.

3. **Add a parallel "range" query variant for each year-based query**.
   - Pros: zero risk of regressing existing year-based callers.
   - Cons: surface area doubles; tests double; orphaned variants are likely.
   - Effort: Medium-High.

### B. Legend ordering — where the rule lives

1. **Enforce order in the handler** (recommend).
   - The handler returns `MonthlyCategorySpendingDto`s grouped by month, but every row also carries a `CategoryName`. The handler can sort the per-month groupings so that for each month the rows are emitted in total-amount-descending order — and Chart.js will then paint the bottom of the stack and the first legend entry from the same ordered list.
   - Pros: single source of truth; deterministic; unit-testable; matches the existing `GetSpendingByCategoryQuery` precedent (`OrderByDescending(c => c.Amount)`).
   - Cons: requires the handler to know the desired chart order, which couples the Application layer to a UI rule. Mitigation: phrase the contract as "category order is total-amount descending across the full range" and document it.
   - Effort: Low.

2. **Enforce order in `charts.js`**.
   - Compute totals in JS, sort categories, build datasets in that order.
   - Pros: keeps Application layer pure of presentation rules.
   - Cons: a second sort in JS duplicates the intent; harder to unit-test; needs a fixture for at least one chart.
   - Effort: Low-Medium.

3. **Both — handler sorts and JS is defensive**.
   - The handler sorts; the JS keeps its current behaviour (which already trusts the data order) but adds a JSDoc warning and an optional `assertOrderConsistency` flag in tests.
   - Pros: belt and suspenders; defends against future regressions.
   - Cons: tiny duplication; mostly documentation.
   - Effort: Low.

### C. YoY chart under a date filter

1. **Keep two full years, derive the years from the filter** (recommend). The PageModel maps the filter to `(Year1, Year2)`:
   - `all` -> (max-year-of-data - 1, max-year-of-data)
   - `this-month` / `this-year` -> (currentYear - 1, currentYear)
   - `last-3-months` -> (currentYear - 1, currentYear) (only two years; "Year over Year" loses its meaning under sub-year filters; title clarifies).
   - The YoY chart's purpose is to compare two years — it should never silently degrade to a single year.
2. **Replace YoY with a single-year trend when the filter is sub-year**. Not recommended; it would shrink the dashboard's value.

## Recommendation

Adopt approach **A1** (replace `Year` with `FromDate/ToDate` in `GetMonthlySpendingByCategoryQuery` and `GetMonthlyTrendsQuery`; keep `GetYearlyComparisonQuery` as a year-pair but compute the years in the PageModel) and **B1** (handler enforces category total-amount-descending order, with a JSDoc contract in `charts.js` and a test that proves the order). The user explicitly asked for the legend order to match the chart data order — that is exactly the contract "the Application layer hands the data to the Frontend already in legend order; the Frontend trusts and renders".

Specifically:

1. `GetMonthlySpendingByCategoryQuery(DateTime FromDate, DateTime ToDate)` — handler buckets by month, then for each month emits rows sorted by amount descending; the overall per-category total is also computed so the chart can re-order categories globally if needed.
2. `GetMonthlyTrendsQuery(DateTime FromDate, DateTime ToDate)` — handler iterates each month in the range, returns `MonthlyTrendDto` for each (pad missing months with zeros). This keeps the x-axis continuous.
3. `GetYearlyComparisonQuery` — keep two-year semantics; the PageModel decides the year pair from the filter; the title reflects the active filter ("This year vs last year", "2025 vs 2026", etc.).
4. `initCategoryStackedChart` — keep current behaviour (trust the array order); add JSDoc warning that the handler must order categories. Add an optional `assertOrder = false` debug hook for tests.
5. `Dashboard.cshtml.cs` — compute `FromDate/ToDate` for charts; remove dead `SpendingByCategory` path; pass filter context to chart titles.
6. `.github/instructions/razor-frontend.instructions.md` — new "Charts" section: legend-ordering rule, when to extend `charts.js`, and a pointer that all charts in the app must follow the rule.
7. `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlySpendingByCategoryQueryTests.cs` — **new** test file with three tests at minimum: grouping correctness, descending-order contract, empty-input safety.
8. Update existing `GetMonthlyTrendsQueryTests.cs` and `GetYearlyComparisonQueryTests.cs` to match the new signature/anchor logic.

## Risks

- **Breaking change to three query records.** Any other caller of these queries breaks at compile time. The only known caller is `Dashboard.cshtml.cs` (verified by grep across `src/` and `tests/`), so the blast radius is small — but a `grep "GetMonthlySpendingByCategoryQuery\|GetMonthlyTrendsQuery\|GetYearlyComparisonQuery"` should be the first sanity step in `sdd-apply`.
- **Coupling Application to UI order.** Having the handler return categories in chart-legend order is a small cross-layer compromise. Mitigate with explicit JSDoc + docs and a single sort key ("total amount descending across the full range").
- **YoY semantics under sub-year filters.** Picking the wrong year pair makes the chart misleading. The PageModel must compute years deterministically and the chart title must show the chosen pair (e.g., "This year vs last year" or "2024 vs 2025").
- **HTMX re-render of charts.** `destroyAllCharts()` is already wired; if the JSON blocks change id or shape, the Alpine `init()` must still pick them up. No code in `initCharts()` references `DateFilter` today, so the JS does not need to know the active filter — only the chart title and the payload do.
- **Empty/edge states.** "All Time" with only one month of data should still produce a non-broken chart. Padding the months trend with zeros handles this; the stacked chart naturally degenerates to one month.
- **Test coverage gap.** `GetMonthlySpendingByCategoryQuery` has no tests today. RED-GREEN-REFACTOR (required by the project rules) makes this the perfect moment to write them.
- **Documentation drift.** If the rule lives only in `razor-frontend.instructions.md`, future `new Chart(...)` blocks may forget it. Cross-link the rule from the `Charts` section in `AGENTS.md`'s "Common Pitfalls" or a new bullet in the Frontend Rules table so the discoverability is high.

## Ready for Proposal

Yes. The change has a clear scope, a single dominant approach (A1 + B1), a concrete file-level impact list, and a small but well-defined test surface. Next step is `sdd-propose` to draft the proposal, then `sdd-spec` for the deltas (most likely under a new `openspec/specs/analytics/` domain or extending an existing one), then `sdd-design` to lock the chart-titling and YoY anchor logic, then `sdd-tasks` for the implementation slices, then `sdd-apply` under TDD. The 400-line PR budget is comfortable: the touched files total well under 400 lines, so a single PR is acceptable.

## Open Clarifications for the Orchestrator / User

1. **Custom date range** is defined in `CalculateDateRange` but not exposed in the pill UI. Should this change also surface a fifth pill "Custom" backed by two Flatpickr fields, or stay scoped to the four existing pills? The task description says "pill selector: All Time, This Month, Last 3 Months, This Year" — so I will **not** add a Custom pill unless the user asks.
2. **Dead pie payload (`dashboard-category-data` / `SpendingByCategory`)** is serialized on the page but not consumed by any init function. Removing it is a small bonus cleanup, but if the user wants to keep the door open to a pie chart later, it should stay. Default recommendation: remove, since it adds bytes and a misleading "Spending by Category" payload to the JSON inspector.
3. **YoY chart title under `last-3-months`.** The natural title is "Year over Year — {year-1} vs {year}". Should the YoY chart keep showing two full years regardless, or should it shrink to a quarter-over-quarter view? The task description does not call this out; default recommendation is to keep two full years (A1 + C1) and let the title clarify.
