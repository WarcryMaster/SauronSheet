## Exploration: Complete Redesign of Annual Report Dashboard

### Current State

The `/Analysis/Annual` page was already redesigned in a previous change (archived `2026-06-27-annual-analysis-redesign`) from a raw 15-column table to a dashboard with:

**Frontend** (`Annual.cshtml`, 522 lines):
- Year selector (dropdown `<select>`)
- 4 KPI cards (Income Total, Expense Total, Net, Fixed Cost %) with animated counters
- Monthly trend line chart (Chart.js) via `initAnnualTrendChart()`
- Fixed/Variable donut chart via `initAnnualDistributionChart()`
- YoY comparison section with 5 compact cards (Income Fixed/Variable, Expense Fixed/Variable, Net)
- Collapsible detail tables (income + expense) with row-level expansion showing mini-bars
- Empty state for years without data
- All E2E testids preserved

**PageModel** (`Annual.cshtml.cs`, 202 lines):
- `MonthlyIncomeTotals[12]`, `MonthlyExpenseTotals[12]` — aggregates from rows
- `ChartDataJson`, `FixedVariableChartJson` — serialized JSON for Chart.js
- `FixedCostPercentage` — computed ratio
- `FormatVariationPct()`, `GetVariationBadgeClass()`, `GetVariationArrow()` — display helpers

**Backend** (`GetAnnualAnalysisQueryHandler`, 186 lines):
- Loads all user transactions for the requested year via `FindBySpecificationAsync` (in-memory filter after fetching ALL user transactions)
- Classifies into fixed/variable income/expense via `AnnualClassificationEngine`
- Computes YoY variation by re-running the same classification for `year - 1`
- Returns `AnnualAnalysisResultDto` with rows + summary

**Existing separate queries** (already available but NOT integrated into the Annual page):
- `GetYearlyComparisonQuery` — month-by-month comparison between 2 arbitrary years
- `GetMonthlyTrendsQuery` — monthly income/expense/net for a date range
- `GetSpendingByCategoryQuery` — spending grouped by category (top 6 + "Other")
- `GetMonthlySpendingByCategoryQuery` — category spending per month
- `GetTransactionSummaryQuery` — basic summary stats

**Classification Engine** (`AnnualClassificationEngine`):
- Pure domain logic, no external dependencies
- Rule-based: known mappings + heuristic (coefficient of variation ≤ 10% for fixed)
- Static sets of fixed/variable expense mappings (Spanish subcategory names)

**Testing**:
- Handler tests: 10 tests (empty data, correct rows, monthsWithData, tenant isolation, 4 YoY scenarios)
- DTO tests: 8 tests for `YearOverYearVariationDto`, `AnnualAnalysisSummaryDto`, `AnnualAnalysisRowDto`
- Frontend model tests: 10 tests for `MonthlyIncomeTotals`, `MonthlyExpenseTotals`, `ChartDataJson`, `FixedVariableChartJson`, `FixedCostPercentage`
- Page rendering integration tests: 4 tests with mocked mediator
- E2E tests: 6 tests covering dashboard rendering, empty state, detail toggle, row expansion, year selector, YoY fallback

### What the Current Implementation Lacks (for the Redesign Vision)

The user wants 17+ new features. Current status:

| Feature | Status | Notes |
|---------|--------|-------|
| Executive KPI dashboard | ✅ Done | 4 KPIs with YoY badges |
| Savings rate | ❌ Missing | Needs new computation |
| Historical ranking | ❌ Missing | Needs multi-year data |
| Smart generated summary | ❌ Missing | Text generation, rule-based |
| Multi-year comparison charts | ⚠️ Partial | Separate query exists but NOT on the page |
| Monthly evolution best/worst months | ❌ Missing | Needs detection logic |
| Category distribution with rankings | ⚠️ Partial | Separate category query exists |
| Category comparison tables (YoY) | ❌ Missing | Needs new aggregation |
| Anomaly/exceptional transaction detection | ❌ Missing | Statistical outlier detection |
| Annual timeline of key events | ❌ Missing | Event detection from transactions |
| Top movements (biggest/frequent) | ❌ Missing | Simple query missing |
| Financial ratios | ❌ Missing | Monthly/daily/per-transaction |
| Financial health score (0-100, rule-based) | ❌ Missing | New domain service |
| Auto-generated discoveries/insights | ❌ Missing | Rule-based text generation |
| Achievement badges | ❌ Missing | Gamification logic |
| Trend detection (growing/stable/declining) | ❌ Missing | Multi-year category analysis |
| Predictions (simple models) | ❌ Missing | Requires 2+ years data |
| Historical year-to-year comparison | ⚠️ Partial | Separate query exists |
| Year navigation (prev/next buttons) | ❌ Missing | Currently uses `<select>` |
| Export to PDF/image | ❌ Missing | Nice-to-have |

### Affected Areas

**Frontend (Presentation Layer):**
- `src/SauronSheet.Frontend/Pages/Analysis/Annual.cshtml` — REWRITE: from 522-line dashboard to 1000+ line full report
- `src/SauronSheet.Frontend/Pages/Analysis/Annual.cshtml.cs` — REWRITE: from 202-line model to multi-query orchestrator
- `src/SauronSheet.Frontend/wwwroot/js/charts.js` — EXTEND: add 4-6 new chart init functions (multi-year bar, category radar, timeline, etc.)
- `src/SauronSheet.Frontend/wwwroot/css/` — POSSIBLE NEW: report-specific print/PDF styles

**Application Layer (Backend):**
- `src/SauronSheet.Application/Features/Analytics/Queries/` — NEW: multiple new queries or a composite dashboard handler
- `src/SauronSheet.Application/Features/Analytics/DTOs/` — NEW: 5-10 new DTOs for health score, anomalies, timeline, etc.
- `src/SauronSheet.Application/Features/Analytics/Services/` — NEW: dedicated services for health score, trend detection, anomaly detection
- `src/SauronSheet.Application/Features/Analytics/Classification/` — EXTEND: possibly add more classification rules

**Domain Layer:**
- `src/SauronSheet.Domain/ValueObjects/` — POSSIBLE: new VOs (HealthScore, SavingsRate, etc.)
- `src/SauronSheet.Domain/Entities/Transaction.cs` — No changes (already has all needed data)
- `src/SauronSheet.Domain/Repositories/ITransactionRepository.cs` — MAYBE: new query methods if perf requires

**Infrastructure:**
- `src/SauronSheet.Infrastructure/Persistence/SupabaseTransactionRepository.cs` — POSSIBLE: add more efficient query methods (avoid in-memory filtering for large datasets)

**Testing:**
- `tests/SauronSheet.Application.Tests/Features/Analytics/` — NEW: tests for all new services/handlers
- `tests/SauronSheet.Frontend.Tests/Pages/Analysis/AnnualModelTests.cs` — REWRITE: adapt to new model properties
- `tests/SauronSheet.Frontend.Tests/Pages/Analysis/AnnualPageRenderingTests.cs` — EXTEND: test new sections
- `e2e/tests/07-annual-analysis.spec.ts` — REWRITE: adapt to new layout, add new test flows

### Technical Constraints (Must Preserve)

1. **Auth decorator**: `[Authorize]` on PageModel
2. **Data-testid attributes**: All existing E2E testids must be preserved or migrated systematically
3. **Clean Architecture layering**: Domain → Application (CQRS) → Infrastructure → Frontend
4. **Supabase only in Infrastructure**: No raw SQL in Application layer
5. **Sentry-only observability**: No Console.WriteLine, Debug, or Trace
6. **DDD patterns**: Strong-typed IDs, Value Objects, Specifications
7. **Chart.js + Alpine.js + MDBootstrap**: No new frontend frameworks
8. **Spain-local timezone**: All date operations use `ToSpainLocal()` extension
9. **TDD workflow**: `strict_tdd: true`, RED-GREEN-REFACTOR mandatory
10. **No `var`**: Explicit type declarations everywhere
11. **Explicit types**: No `var` in C# code
12. **Performance boundary**: `FindBySpecificationAsync` loads ALL user transactions and filters in-memory — avoid calling it N times
13. **Archived change path**: `openspec/changes/archive/2026-06-27-annual-analysis-redesign/`

### Approaches

**1. Monolithic Handler Extension** — Keep the existing `GetAnnualAnalysisQueryHandler`, add all new logic there
- Pros: Single MediatR call, simplest frontend integration
- Cons: Handler would grow to 800+ lines, violates Single Responsibility, hard to test, impossible to maintain
- Effort: Low implementation but High technical debt
- **Verdict: Rejected** — this is exactly the opposite of Clean Architecture

**2. PageModel Orchestration (N+1 Queries)** — Keep existing handler as-is, call additional separate queries from the PageModel
- Pros: Reuses existing focused queries (`GetYearlyComparison`, `GetMonthlyTrends`, `GetSpendingByCategory`), clean separation
- Cons: N+1 MediatR calls per page load, harder to test the PageModel orchestration, no transactional consistency, may be slow
- Effort: Medium
- **Verdict: Feasible for small features but won't scale** — 6+ MediatR calls per page load is too many

**3. Composite Dashboard Handler** — Create a new `GetAnnualDashboardQuery` with its own handler that internally delegates to focused sub-handlers, returning a single `AnnualDashboardDto` containing all data sections
- Pros: Single MediatR call from frontend, clean internal separation, easy to test each sub-handler independently, can add caching, data dependency management (predictions only with 2+ years)
- Cons: New composite DTO (large but cohesive), sub-handler injection requires care, need to decide what goes in the composite vs. what's HTMX-fetched
- Effort: High (new handler, new DTOs, new services)
- **Verdict: Recommended** — best architectural fit for Clean Architecture + CQRS

**4. Hybrid: Composite + HTMX Progressive Loading** — Composite dashboard handler returns core data (KPIs, summary, charts) while heavy features (anomalies, timeline, predictions) are fetched via HTMX partials on demand
- Pros: Perceived performance is excellent, users see KPIs instantly, heavy computation is deferred
- Cons: More frontend complexity, HTMX partial endpoints needed, state management across partial loads, harder E2E testing
- Effort: High (highest complexity)
- **Verdict: Good for the "Export" and "Predictions" features, overkill for core data**

### Recommendation

**Approach 3 (Composite Dashboard Handler) as the primary architecture, with Approach 4 (HTMX Progressive Loading) only for export predictions and the PDF/nice-to-have features.**

Rationale:
1. The existing `GetAnnualAnalysisQueryHandler` already loads ALL transactions and filters in-memory. Adding more queries that also load ALL transactions is a performance disaster. A composite handler can share the transaction load across sub-computations.
2. The feature list has clear dependency tiers:
   - **Tier 1 (always):** KPIs, summary, monthly trend, category distribution, savings rate, ratios
   - **Tier 2 (needs multi-year):** YoY comparison, trend detection, historical ranking, predictions
   - **Tier 3 (needs computational):** Anomaly detection, health score, discoveries, achievements, timeline
3. The composite handler can load transactions ONCE and pass them to each internal sub-handler/service.
4. The composite DTO can be built incrementally — Tier 1 first, then Tier 2, then Tier 3 in subsequent SDD cycles.

### Proposed Architecture

```
GetAnnualDashboardQuery (single IRequest<AnnualDashboardDto>)
  └── GetAnnualDashboardQueryHandler
       ├── TransactionLoaderService (loads transactions for N years ONCE)
       ├── AnnualSummaryService (existing classification logic extracted)
       ├── MultiYearComparisonService (YoY, historical ranking, trend detection)
       ├── CategoryAnalysisService (ranking, YoY changes, category comparison)
       ├── AnomalyDetectionService (statistical outlier detection, CV, Z-score)
       ├── FinancialRatiosService (avg monthly, daily, per-transaction)
       ├── HealthScoreService (0-100 rule-based, no AI)
       ├── InsightsService (auto-generated discoveries from data)
       ├── AchievementsService (badge rules)
       ├── TimelineService (key event detection)
       ├── PredictionService (simple linear projection, requires 2+ years)
       └── TopMovementsService (biggest, most frequent, by category)
```

Each sub-service is:
- **Pure C#** — no external dependencies (except repositories via DI)
- **Testable** — unit tests with mocked data
- **Optional** — can be enabled/disabled per feature flag
- **Sentry-instrumented** — each service logs its computation time

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `FindBySpecificationAsync` performance degradation with N years of data | High | High | New `GetByUserIdAndDateRangeAsync` method on repository to filter at Postgrest level; or load once and cache |
| DTO bloat — `AnnualDashboardDto` becomes 50+ fields | Medium | Medium | Group fields into nested DTOs (`DashboardSummaryDto`, `CategoryAnalysisDto`, etc.) |
| Feature creep — 17 features is a lot for one change | High | High | Slice into SDD task phases: Tier 1 (core) → Tier 2 (multi-year) → Tier 3 (advanced) |
| Health score rules need domain expert validation | Medium | Low | Start with simple rules, iterate. Score is 0-100, rule-based, no AI |
| Prediction accuracy with sparse data (1-2 years) | Medium | Low | Feature shows warning "requires 3+ years of data" when insufficient |
| Chart.js performance with 12+ datasets (multi-year) | Low | Medium | Use canvas pooling, limit visible datasets to 5, add toggle |
| Existing testids migration | Medium | Medium | Systematic mapping table in spec; preserve `annual-income-table`, `annual-expense-table`, `annual-empty-state` |
| Export to PDF introduces new dependencies | Low | Low | Defer to "nice to have" — use browser print or jsPDF |
| Frontend bundle size (Alpine + Chart.js + new JS) | Low | Low | Lazy load chart init functions, code-split chart types |
| `AnnualClassificationEngine` is stateful in test pattern | Low | Medium | Keep it pure; extract to shared service if classification reuse is needed |

### Missing Data/Queries

The following backend logic is **entirely new** and needs to be built:

| New Service/Query | What It Computes | Data Required |
|-------------------|-----------------|---------------|
| `SavingsRateService` | `(Net Income / Total Income) * 100` | Summary totals |
| `HistoricalRankingService` | Rank current year among all user years by net income, savings rate, etc. | All years' summaries |
| `SummaryGeneratorService` | Rule-based text: "Your income grew 12%, best month was May..." | All metrics |
| `BestWorstMonthDetection` | Finds highest/lowest income/expense/net month | Monthly aggregates |
| `CategoryRankingService` | Rank categories by total + YoY change per category | Multi-year category data |
| `AnomalyDetectionService` | Z-score or IQR-based outlier detection per category | Transactions (detail needed) |
| `TimelineService` | Detect "first expense in category X", "largest ever purchase Y" | Chronological transactions |
| `TopMovementsService` | Top 5 largest expense/income transactions, most frequent merchants | Transaction detail |
| `FinancialRatiosService` | Avg monthly spending, daily spending, per-transaction avg | Summary + transaction count |
| `HealthScoreService` | Rule-based score (0-100): savings rate, debt ratio, emergency fund, etc. | All metrics |
| `InsightsService` | Pattern detection: "You spend 40% more on weekends", recurring charges | Transaction detail |
| `AchievementService` | Badges: "Full year tracked", "Saved 12 months straight", "Under budget Q1" | Milestone detection |
| `TrendDetectionService` | Linear regression or moving average: growing/stable/declining categories | Multi-year per-category |
| `PredictionService` | Simple linear projection: "Based on current trend, December will be..." | Multi-year monthly data |

### Performance Considerations

**Critical finding**: `SupabaseTransactionRepository.FindBySpecificationAsync` (line 222-242) fetches **ALL user transactions** from Supabase and filters in-memory via `specification.Criteria.Compile()`. For the current page, this means:
- Current year: loads ALL transactions (expensive)
- Previous year (YoY): loads ALL transactions AGAIN
- If we add 3-year comparison: loads ALL transactions 3 times

**Mitigation approaches**:
1. Add `GetByUserIdAndDateRangeAsync` to `ITransactionRepository` that uses Postgrest date filtering (server-side)
2. Load all user transactions ONCE in the composite handler, then partition in-memory
3. Add server-side Postgrest `Where(x => x.Date >= start && x.Date <= end)` filter

Option 2 is simplest for MVP. Option 3 is best for performance at scale.

### Ready for Proposal

**Yes** — the codebase has been thoroughly explored. We know:
- Exactly what exists (both frontend and backend)
- What new features are needed (17 items, mapped against current state)
- The architectural constraints (Clean Architecture, CQRS, DDD, Supabase-only infrastructure)
- The performance bottlenecks (in-memory transaction filtering)
- The test coverage gaps
- The existing queries that can be reused
- The correct architectural approach (Composite Dashboard Handler with internal sub-services)

The orchestrator should proceed to `sdd-propose` with:
- The composite handler approach as the recommended architecture
- A clear 3-tier slicing plan for the 17 features
- The performance mitigation strategy (transaction loading optimization)
- The testid migration plan
