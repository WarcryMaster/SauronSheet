# SauronSheet Phase 4: Analytics & Dashboard (FULL MVP RELEASE)

**Version**: 1.0.0  
**Duration**: 3-4 weeks  
**Status**: ⏳ Blocked by Phase 3  
**Depends**: Phase 0, Phase 1, Phase 2, Phase 3

---

## Goal

Build analytics dashboard showing spending trends, category breakdown, comparisons. This marks the **FULL MVP RELEASE** at Week 18.

---

## Requirements

### Functional Requirements

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| **FR-001** | Monthly spending by category pie chart | Visual breakdown of expenses |
| **FR-002** | Monthly trend line graph | Spending trends over 12 months |
| **FR-003** | Category comparison YoY | This year vs last year spending |
| **FR-004** | Budget tracking widget | Show % spent vs limit per category |
| **FR-005** | Top spenders list | List top 5 spending categories |
| **FR-006** | Custom date range filter | Users select arbitrary range |
| **FR-007** | Export reports to PDF | Download monthly/yearly reports |

### Non-Functional Requirements
- NF-001: 8 integration tests for analytics queries
- NF-002: Dashboard loads in < 2 seconds
- NF-003: Charts responsive (mobile-friendly)
- NF-004: Query results cached for 24 hours

---

## Architecture

### Analytics Queries (CQRS Read Models)

```csharp
// Example queries
- GetSpendingByCategoryQuery(userId, month) → List<CategorySpending>
- GetMonthlyCategoryTrendQuery(userId, categoryId, months) → List<MonthlyTrend>
- GetBudgetSummaryQuery(userId, month) → List<BudgetStatus>
- GetYearOverYearComparisonQuery(userId, category) → ComparisonData
```

### New Components
- `GetSpendingByCategoryQuery` (Application)
- `GetMonthlyTrendQuery` (Application)
- `GetBudgetSummaryQuery` (Application)
- `GetYearOverYearComparisonQuery` (Application)
- Dashboard razor page (Frontend)

---

## Deliverables

### Application Layer
- [ ] `Application/Features/Analytics/GetSpendingByCategoryQuery.cs` + handler
- [ ] `Application/Features/Analytics/GetMonthlyTrendQuery.cs` + handler
- [ ] `Application/Features/Analytics/GetBudgetSummaryQuery.cs` + handler
- [ ] `Application/Features/Analytics/GetYearOverYearComparisonQuery.cs` + handler
- [ ] `Application/Features/Analytics/ExportMonthlyReportQuery.cs` + handler
- [ ] `Application/Tests/Features/Analytics/AnalyticsQueryTests.cs` (8 tests)

### Frontend Layer
- [ ] `Frontend/Pages/Dashboard/Index.cshtml` (main analytics page)
- [ ] `Frontend/Pages/Dashboard/Dashboard.cshtml.cs` (page model)
- [ ] `Frontend/wwwroot/js/charts.js` (Chart.js or similar)
- [ ] `Frontend/wwwroot/css/dashboard.css` (dashboard styling)
- [ ] HTML components: pie chart, line graph, budget widget, top spenders list

### Infrastructure Layer
- [ ] Query result caching layer (optional: Redis-like pattern)

---

## Test Specifications

### Analytics Tests (8 tests)

- **T04-001**: GetSpendingByCategoryQuery returns all categories with totals
- **T04-002**: GetSpendingByCategoryQuery respects date range filter
- **T04-003**: GetMonthlyTrendQuery returns 12-month trend
- **T04-004**: GetBudgetSummaryQuery shows % spent for each budget
- **T04-005**: IsOverBudget categories highlighted in budget summary
- **T04-006**: GetYearOverYearComparisonQuery compares current vs prior year
- **T04-007**: ExportMonthlyReportQuery generates PDF with charts
- **T04-008**: All analytics queries respect ScopedQueryBehavior (tenant isolation)

---

## Success Criteria

✅ Phase 4 (MVP Release) is complete when:

1. `dotnet test` shows **8/8 Phase 4 tests passing**
2. Dashboard page loads in < 2 seconds
3. Pie chart renders spending by category
4. Line graph shows 12-month trend
5. Budget widget shows % spent vs limit
6. CSV export working (from Phase 3)
7. Date range filter functional
8. All Phase 0-3 tests still passing (11 + 8 + 20 + 12 = 51)

Total passing tests: 51 (Phase 0-3) + 8 (Phase 4) = **59 tests**

**🚀 FULL MVP RELEASED** at end of Phase 4, Week 18:
- ✅ User authentication
- ✅ Manual transaction entry
- ✅ PDF import
- ✅ Analytics dashboard
- ✅ Category management

---

## UI Layout

### Dashboard Page
```
+------------------------------------------+
| SauronSheet Dashboard                    |
|       [< Month >] [< Year >]             |
+------------------------------------------+
| [Pie Chart: Spending by Category]        |
| [Line Graph: 12-Month Trend]             |
+------------------------------------------+
| [Budget Status: 3 categories at 80%+]    |
| [Top Spenders: Groceries $1,234...]      |
+------------------------------------------+
| [Export PDF] [Download CSV]              |
+------------------------------------------+
```

---

## Timeline

- **Week 1**: Analytics queries + database tuning
- **Week 2**: Dashboard page + Chart.js integration
- **Week 3**: Export to PDF + date range filter
- **Week 4**: Performance tuning + testing

Target: 8 tests green + dashboard responsive + MVP ready to deploy

---

**Specification Version**: 1.0.0  
**Last Updated**: 2026-02-14
