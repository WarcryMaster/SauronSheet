# Tasks: Rediseño del Dashboard de Análisis Anual

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~860 (additions + deletions) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | force-chained |
| Chain strategy | pending (to be decided by orchestrator) |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | PageModel computed properties + Chart.js init functions + unit tests | PR 1 | Backend prep; no UI changes; independently verifiable via `dotnet test` |
| 2 | Full UI rewrite: KPIs + charts + YoY + collapsible tables with row expansion | PR 2 | Depends on PR 1; replaces entire Annual.cshtml |
| 3 | E2E test rewrite + accessibility polish | PR 3 | Depends on PR 2; validates new layout end-to-end |

## Phase 1: Backend Prep + Chart Functions (PR #1)

- [x] **T-ANN-001** Add computed properties to `Annual.cshtml.cs`
  - **Files**: `src/SauronSheet.Frontend/Pages/Analysis/Annual.cshtml.cs`
  - **Description**: Add 5 computed properties: `MonthlyIncomeTotals` (decimal[12]), `MonthlyExpenseTotals` (decimal[12]), `ChartDataJson` (string — JSON with labels+income+expense), `FixedVariableChartJson` (string — JSON with 4 donut segments), `FixedCostPercentage` (decimal — ExpenseFixed/ExpenseTotal*100 with zero guard). Use `JsonSerializer.Serialize` with anonymous objects. Follow design.md code exactly.
  - **Dependencies**: None
  - **Estimated lines**: ~50 added
  - **Test**: Unit tests in `tests/SauronSheet.Frontend.Tests/Pages/Analysis/AnnualModelTests.cs` — verify monthly aggregation correctness, JSON structure with/without data, FixedCostPercentage zero guard.
  - **PR slice**: PR #1

- [x] **T-ANN-002** Add chart init functions to `charts.js`
  - **Files**: `src/SauronSheet.Frontend/wwwroot/js/charts.js`
  - **Description**: Add `initAnnualTrendChart(canvas, data)` — line chart with 2 datasets (income green, expense red), uses `tokens.success`/`tokens.danger`, `hexToRgba`, `chartDefaults`, `interaction: { mode: 'index', intersect: false }`. Add `initAnnualDistributionChart(canvas, data)` — doughnut chart with 4 segments, legend at bottom, tooltip with `€` formatting. Both: guard null canvas, destroy existing instance. JSDoc with `@param` for canvas element and parsed data object. Signatures accept element ref (not ID string) — different from existing functions.
  - **Dependencies**: None (parallel with T-ANN-001)
  - **Estimated lines**: ~60 added
  - **Test**: Manual — functions are inert without invocation; verified in PR #2 integration.
  - **PR slice**: PR #1

## Phase 2: UI Rewrite (PR #2)

- [ ] **T-ANN-003** Rewrite `Annual.cshtml` — KPI cards, charts, YoY section
  - **Files**: `src/SauronSheet.Frontend/Pages/Analysis/Annual.cshtml`
  - **Description**: Replace entire Razor view. Structure: (1) Year selector form (preserve existing). (2) Empty state with `data-testid="annual-empty-state"` when `!HasData`. (3) KPI row: 4 cards (`annual-kpi-income`, `annual-kpi-expense`, `annual-kpi-net`, `annual-kpi-fixed-pct`) with animated counters and YoY badges using existing `GetVariationBadgeClass`/`GetVariationArrow`/`FormatVariationPct`. (4) Chart section: trend line chart + distribution donut in `card border-0 shadow-sm p-3` containers with `height: 380px`. JSON data blocks `<script type="application/json" id="annual-chart-data">` and `<script id="annual-distribution-data">`. Alpine.js `x-data` with `JSON.parse` + `$nextTick` calling `initAnnualTrendChart($refs.trendCanvas, trendData)` and `initAnnualDistributionChart($refs.distCanvas, distData)`. (5) YoY section: 5 compact cards with `border-start border-3` color indicators, or "Sin datos del año anterior" message when `!hasVariation`. All `data-testid` attributes per design migration plan.
  - **Dependencies**: T-ANN-001, T-ANN-002
  - **Estimated lines**: ~200 (full rewrite)
  - **Test**: Visual verification + E2E in PR #3.
  - **PR slice**: PR #2

- [ ] **T-ANN-004** Rewrite `Annual.cshtml` — collapsible detail tables with row expansion
  - **Files**: `src/SauronSheet.Frontend/Pages/Analysis/Annual.cshtml`
  - **Description**: Add collapsible income/expense tables section. Toggle button (`annual-detail-toggle`) shows/hides both tables simultaneously via Alpine.js `x-show`. Tables use `template x-for` (NOT `@foreach`) with Alpine.js model including `expanded: false` per row. Each row: 4 columns (toggle arrow, Movement, TypeLabel badge, Average, Annual Total). Click/Enter/Space toggles `row.expanded`. Expansion row: 12 mini-bars CSS (`height: X%` based on `amt/annualMax*100`), month labels (E,D,M,A,M,J,J,A,S,O,N,D). Preserve `data-testid="annual-income-table"` and `data-testid="annual-expense-table"`. Row data serialized as JSON in `x-data` via `@Json.Serialize(Model.IncomeRows.Select(...))`. Use `@@click`, `@@keydown.enter`, `@@keydown.space.prevent` (double-@ for Razor).
  - **Dependencies**: T-ANN-003
  - **Estimated lines**: ~150
  - **Test**: E2E in PR #3 — verify toggle, row expansion, mini-bars render.
  - **PR slice**: PR #2

## Phase 3: E2E Tests + Accessibility (PR #3)

- [ ] **T-ANN-005** Rewrite E2E test spec for annual analysis
  - **Files**: `e2e/tests/07-annual-analysis.spec.ts`
  - **Description**: Full rewrite. Test cases: (1) KPI cards visible with correct `data-testid` and content. (2) Charts render (canvas elements present with `role="img"`). (3) Table toggle — tables hidden by default, visible after click. (4) Row expansion — click row shows 12 mini-bars. (5) Empty state preserved. (6) Year change reloads. (7) YoY section visible or "Sin datos" fallback. Map new testids: `annual-kpi-income`, `annual-kpi-expense`, `annual-kpi-net`, `annual-kpi-fixed-pct`, `annual-trend-chart`, `annual-distribution-chart`, `annual-yoy-section`, `annual-yoy-no-data`, `annual-detail-toggle`, `annual-income-table`, `annual-expense-table`, `annual-empty-state`. Use authenticatedPage fixture.
  - **Dependencies**: T-ANN-004
  - **Estimated lines**: ~200
  - **Test**: `npx playwright test --config=e2e/playwright.config.ts --project=chromium`
  - **PR slice**: PR #3

- [ ] **T-ANN-006** Accessibility and responsive polish
  - **Files**: `src/SauronSheet.Frontend/Pages/Analysis/Annual.cshtml`
  - **Description**: Add `role="img"` and descriptive `aria-label` on both `<canvas>` elements. Add `visually-hidden` fallback tables for each chart (12-row monthly table for trend, 4-row table for distribution). Ensure toggle buttons are keyboard-navigable (`tabindex="0"`, `@@keydown.enter`, `@@keydown.space.prevent`). Verify responsive breakpoints: KPIs `row-cols-1 row-cols-md-2 row-cols-xl-4`, charts stack on mobile, tables allow horizontal scroll.
  - **Dependencies**: T-ANN-004
  - **Estimated lines**: ~50
  - **Test**: E2E — verify `role="img"` on canvases, keyboard navigation on toggle.
  - **PR slice**: PR #3
