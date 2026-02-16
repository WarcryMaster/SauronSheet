# Phase 4: Analytics & Dashboard (MVP)

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Features)
- **Phase Type**: Full-Stack (Features)
- **Duration**: Weeks 14–18
- **Goal**: Analytics queries, dashboard UI with charts, transaction search & filtering — this completes the **MVP ✅**
- **Depends On**: Phase 0 (foundation), Phase 1 (auth + tenant scoping), Phase 2 (domain model), Phase 3 (transaction CRUD, category management, PDF import, Supabase repositories)
- **Unlocks**: Phase 5 (Budget Management & Alerts), Phase 6 (Polish & Production Deployment)
- **Milestone**: 🏁 **MVP Launch — all core functionality operational after this phase**

> ✅ **MVP COMPLETION PHASE**: After Phase 4, users can register, import PDFs, manage transactions/categories, and view analytics dashboards with charts. The application is functionally complete for basic expense tracking.

---

## Critical Decisions

|
 ID     
|
 Decision                                                           
|
 Rationale                                                                                  
|
 Date       
|
|
--------
|
--------------------------------------------------------------------
|
--------------------------------------------------------------------------------------------
|
------------
|
|
 CD-4.1 
|
 Chart.js via CDN for frontend charts                               
|
 Lightweight, free, well-documented, CDN available; no npm build pipeline needed             
|
 2026-02-15 
|
|
 CD-4.2 
|
 Aggregation queries in Application layer (not DB views/functions)  
|
 Keeps logic testable with mocked repos; Supabase free tier limits server-side views         
|
 2026-02-15 
|
|
 CD-4.3 
|
 Dashboard is the new default authenticated landing page            
|
 Users expect summary on login; replaces Phase 1 stub dashboard                             
|
 2026-02-15 
|
|
 CD-4.4 
|
 Date range filter defaults to current month                        
|
 Most common use case; configurable via date picker                                         
|
 2026-02-15 
|
|
 CD-4.5 
|
 All analytics queries use domain specifications for filtering      
|
 Consistent with architecture; filtering expressed in domain language                       
|
 2026-02-15 
|
|
 CD-4.6 
|
 Transaction search uses server-side filtering (not client-side)    
|
 Handles large datasets; specifications compose filters at query level                      
|
 2026-02-15 
|
|
 CD-4.7 
|
 Chart data fetched via PageModel (server-rendered JSON)            
|
 Simplifies architecture; no separate API endpoints needed for MVP                          
|
 2026-02-15 
|
|
 CD-4.8 
|
 Spending amounts aggregated as absolute values for analytics       
|
 Expenses shown as positive numbers in charts for readability; sign used only in net calc   
|
 2026-02-15 
|
|
 CD-4.9 
|
 No user preferences persistence in this phase                      
|
 Filter state maintained via query parameters (URL); persistence deferred to Phase 6        
|
 2026-02-15 
|
|
 CD-4.10
|
 Recent transactions widget shows last 10 transactions              
|
 Quick glance without navigating to full list; links to full transaction page                
|
 2026-02-15 
|

---

## Executive Summary

### In Scope

|
 Area           
|
 Deliverable                                                                                              
|
|
----------------
|
----------------------------------------------------------------------------------------------------------
|
|
 Application    
|
`GetSpendingByCategoryQuery`
 + handler (category breakdown for date range)                               
|
|
 Application    
|
`GetMonthlyTrendsQuery`
 + handler (monthly spending totals for a given year)                             
|
|
 Application    
|
`GetYearlyComparisonQuery`
 + handler (compare two years side-by-side)                                    
|
|
 Application    
|
`GetTransactionSummaryQuery`
 + handler (total income, total expenses, net, transaction count)            
|
|
 Application    
|
`GetRecentTransactionsQuery`
 + handler (last N transactions for dashboard widget)                        
|
|
 Application    
|
`SearchTransactionsQuery`
 + handler (multi-filter search: keyword, date range, category, amount range)   
|
|
 Application    
|
 DTOs: 
`CategorySpendingDto`
, 
`MonthlyTrendDto`
, 
`YearlyComparisonDto`
, 
`TransactionSummaryDto`
, 
`TransactionSearchResultDto`
|
|
 Domain         
|
`TransactionByDescriptionKeywordSpecification`
 (new specification for text search)                       
|
|
 Domain         
|
`CompositeSpecification<T>`
 (combines multiple specifications with AND logic)                            
|
|
 Infrastructure 
|
 Repository query methods optimized for analytics aggregations                                            
|
|
 Frontend       
|
 Dashboard page (complete replacement of Phase 1 stub) with summary cards, charts, recent transactions    
|
|
 Frontend       
|
 Chart.js integration: pie chart (category breakdown), line chart (monthly trends), bar chart (yearly comparison) 
|
|
 Frontend       
|
 Date range filter component (reusable across dashboard and transaction list)                              
|
|
 Frontend       
|
 Transaction search page with multi-filter UI (
`/Transactions/Search`
)                                    
|
|
 Frontend       
|
 Updated transaction list page with filter controls                                                        
|
|
 Frontend       
|
 Updated 
`_Layout.cshtml`
 with dashboard as default landing page                                           
|
|
 Tests          
|
 ≥32 tests (application handler tests + domain specification tests)                                        
|

### Deferred (NOT in this phase)

|
 Item                                  
|
 Target Phase 
|
 Reason                                           
|
|
---------------------------------------
|
--------------
|
--------------------------------------------------
|
|
 Budget vs. actual comparison          
|
 Phase 5      
|
 Budget Management phase                          
|
|
 Budget status indicators on dashboard 
|
 Phase 5      
|
 Requires budget CRUD first                       
|
|
 CSV/Excel data export                 
|
 Post-MVP     
|
 Export feature; not core analytics                
|
|
 Scheduled/automated reports           
|
 Post-MVP     
|
 Requires background jobs infrastructure          
|
|
 Anomaly detection / spending alerts   
|
 Phase 5      
|
 Tied to budget overage system                    
|
|
 User preferences persistence          
|
 Phase 6      
|
 Polish; filter state via URL params for now       
|
|
 Advanced chart interactivity          
|
 Phase 6      
|
 Click-through, drill-down; polish concern         
|
|
 Dashboard widget customization        
|
 Post-MVP     
|
 Users choose which widgets to display             
|
|
 Comparison with previous period       
|
 Post-MVP     
|
 "vs. last month" percentage changes               
|
|
 Spending forecast / projections       
|
 Post-MVP     
|
 ML/statistical analysis                           
|

---

## User Scenarios & Testing

### Scenario 4.1: View Analytics Dashboard

**As a** user
**I want to** see an analytics dashboard with spending summary and charts
**So that** I can understand my spending habits at a glance

**Acceptance Criteria:**
- Dashboard is the default landing page after login (replaces Phase 1 stub)
- **Summary cards** at the top:
  - Total Income (positive transactions) for current period
  - Total Expenses (negative transactions) for current period
  - Net Amount (income - expenses)
  - Transaction Count for current period
- **Pie chart**: spending breakdown by category (top categories + "Other" if > 6)
- **Line chart**: monthly spending trends for the current year
- **Bar chart**: yearly comparison (current year vs. previous year)
- **Recent transactions widget**: last 10 transactions with date, description, amount, category
- Date range filter at the top (defaults to current month; options: This Month, Last Month, Last 3 Months, This Year, Custom Range)
- All data scoped to current authenticated user
- Charts update when date range filter changes
- Responsive layout: cards in grid, charts stack vertically on mobile
- Empty state: "No spending data yet. Import a PDF or add transactions to see analytics." with action links

### Scenario 4.2: Spending by Category Analysis

**As a** user
**I want to** see how much I spend in each category
**So that** I can identify where my money goes

**Acceptance Criteria:**
- Pie chart shows percentage and absolute amount per category
- Categories with spending shown; categories with zero spending omitted
- If more than 6 categories have spending, group smallest into "Other"
- Clicking a pie slice (or legend item) could highlight/filter (basic interactivity)
- Chart legend shows category name, color, amount, percentage
- Uncategorized transactions grouped under "Uncategorized" label
- Data matches the selected date range filter

### Scenario 4.3: Monthly Spending Trends

**As a** user
**I want to** see my spending trends over months
**So that** I can identify seasonal patterns or changes in spending

**Acceptance Criteria:**
- Line chart shows 12 months (January–December) for the selected year
- Y-axis: total spending amount; X-axis: month names
- Months with no transactions show 0
- Default year: current year
- Year selector allows switching between available years
- Hover tooltip shows month name and exact amount
- Line color consistent with app theme

### Scenario 4.4: Yearly Spending Comparison

**As a** user
**I want to** compare my spending between two years
**So that** I can track year-over-year changes

**Acceptance Criteria:**
- Bar chart with grouped bars: Year 1 vs. Year 2, per month
- Default: current year vs. previous year
- Year selectors for both years (dropdowns)
- Months with no data show 0-height bars
- Different colors for each year
- Legend shows year labels with colors
- Hover tooltip shows month, year, and amount

### Scenario 4.5: Transaction Search and Filtering

**As a** user
**I want to** search and filter my transactions with multiple criteria
**So that** I can find specific transactions or analyze spending patterns

**Acceptance Criteria:**
- Search page accessible at `/Transactions/Search` (or integrated into transaction list)
- **Filter controls:**
  - Keyword search (searches in description — partial match, case-insensitive)
  - Date range picker (from date, to date)
  - Category dropdown (single select or "All Categories")
  - Amount range (min amount, max amount)
- Filters are combinable (AND logic: all active filters applied simultaneously)
- Results displayed in paginated table (same format as transaction list)
- Filter values preserved in URL query parameters (shareable, bookmarkable)
- Clear all filters button
- Result count shown: "Showing N of M transactions matching filters"
- Empty result: "No transactions match your filters." with suggestion to adjust

### Scenario 4.6: Transaction Summary Statistics

**As a** user
**I want to** see summary statistics for my transactions
**So that** I have a quick numeric overview of my finances

**Acceptance Criteria:**
- Summary shows:
  - Total Income: sum of all positive-amount transactions in period
  - Total Expenses: sum of all negative-amount transactions in period (displayed as positive)
  - Net Amount: income - expenses (can be negative — displayed in red if negative)
  - Transaction Count: total number of transactions in period
- Summary responds to date range filter
- Summary displayed as cards with icons and formatted currency amounts
- Card colors: Income (green), Expenses (red), Net (green if positive, red if negative), Count (blue/neutral)

### Scenario 4.7: Dashboard Responsive Design

**As a** user accessing the app on mobile
**I want** the dashboard to be usable on small screens
**So that** I can check my finances on the go

**Acceptance Criteria:**
- Summary cards: 2x2 grid on mobile, 4-column row on desktop
- Charts: full-width stacked vertically on mobile, side-by-side where possible on desktop
- Recent transactions: card layout on mobile, table on desktop
- Date range filter: collapsible on mobile (tap to expand)
- Touch-friendly chart interactions (tap instead of hover)
- No horizontal scroll required on any viewport width ≥ 320px

---

## Functional Requirements

### FR-4.01: Domain Layer Additions

#### TransactionByDescriptionKeywordSpecification

```csharp
public class TransactionByDescriptionKeywordSpecification : BaseSpecification<Transaction>
{
    public TransactionByDescriptionKeywordSpecification(string keyword)
        : base(t => t.Description.ToLower().Contains(keyword.ToLower()))
    {
        if (string.IsNullOrWhiteSpace(keyword))
            throw new DomainException("Search keyword cannot be empty.");
    }
}
CompositeSpecification<T>
csharp
public class CompositeSpecification<T> : BaseSpecification<T>
{
    private CompositeSpecification(Expression<Func<T, bool>> criteria)
        : base(criteria)
    { }

    public static CompositeSpecification<T> And(
        ISpecification<T> left,
        ISpecification<T> right)
    {
        // Combine left.Criteria AND right.Criteria using Expression.AndAlso
        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.AndAlso(
            Expression.Invoke(left.Criteria, parameter),
            Expression.Invoke(right.Criteria, parameter));
        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);

        return new CompositeSpecification<T>(lambda);
    }
}
File Structure:

text
Domain/
├── Specifications/                                    # Additions
│   ├── (existing from Phase 2)
│   ├── TransactionByDescriptionKeywordSpecification.cs  # NEW
│   └── CompositeSpecification.cs                        # NEW
FR-4.02: Application Layer — Analytics Queries
text
Application/
├── Features/
│   ├── Analytics/
│   │   ├── Queries/
│   │   │   ├── GetSpendingByCategoryQuery.cs
│   │   │   ├── GetSpendingByCategoryQueryHandler.cs
│   │   │   ├── GetMonthlyTrendsQuery.cs
│   │   │   ├── GetMonthlyTrendsQueryHandler.cs
│   │   │   ├── GetYearlyComparisonQuery.cs
│   │   │   ├── GetYearlyComparisonQueryHandler.cs
│   │   │   ├── GetTransactionSummaryQuery.cs
│   │   │   └── GetTransactionSummaryQueryHandler.cs
│   │   └── DTOs/
│   │       ├── CategorySpendingDto.cs
│   │       ├── MonthlyTrendDto.cs
│   │       ├── YearlyComparisonDto.cs
│   │       └── TransactionSummaryDto.cs
│   └── Transactions/
│       ├── Queries/
│       │   ├── (existing from Phase 3)
│       │   ├── GetRecentTransactionsQuery.cs              # NEW
│       │   ├── GetRecentTransactionsQueryHandler.cs       # NEW
│       │   ├── SearchTransactionsQuery.cs                 # NEW
│       │   └── SearchTransactionsQueryHandler.cs          # NEW
│       └── DTOs/
│           └── (existing from Phase 3)
GetSpendingByCategoryQuery
csharp
public record GetSpendingByCategoryQuery(
    DateTime FromDate,
    DateTime ToDate
) : IRequest<List<CategorySpendingDto>>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Build DateRange from FromDate/ToDate
3. Load all transactions for user within date range
   (TransactionByUserSpecification AND TransactionByDateRangeSpecification)
4. Filter to expenses only (Amount.IsNegative)
5. Group by CategoryId
6. For each group:
   a. Sum absolute amounts
   b. Lookup category name (or "Uncategorized" for null CategoryId)
   c. Calculate percentage of total spending
7. Sort by amount descending
8. If more than 6 categories: group remaining into "Other"
9. Return List<CategorySpendingDto>
CategorySpendingDto
csharp
public record CategorySpendingDto(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount,
    string Currency,
    decimal Percentage
);
GetMonthlyTrendsQuery
csharp
public record GetMonthlyTrendsQuery(
    int Year
) : IRequest<List<MonthlyTrendDto>>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Build DateRange for full year (Jan 1 – Dec 31)
3. Load all transactions for user within year
4. Group by month (1–12)
5. For each month:
   a. Sum expenses (absolute negative amounts)
   b. Sum income (positive amounts)
   c. Calculate net (income - expenses)
6. Fill missing months with zero values
7. Return List<MonthlyTrendDto> (always 12 entries, Jan–Dec)
MonthlyTrendDto
csharp
public record MonthlyTrendDto(
    int Month,
    string MonthName,
    decimal TotalExpenses,
    decimal TotalIncome,
    decimal NetAmount,
    string Currency,
    int TransactionCount
);
GetYearlyComparisonQuery
csharp
public record GetYearlyComparisonQuery(
    int Year1,
    int Year2
) : IRequest<List<YearlyComparisonDto>>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Load all transactions for user in Year1 and Year2
3. Group each year's transactions by month
4. For each month (1–12):
   a. Year1 total expenses
   b. Year2 total expenses
   c. Difference (Year2 - Year1)
   d. Percentage change ((Year2 - Year1) / Year1 * 100)
5. Fill missing months with zero values
6. Return List<YearlyComparisonDto> (always 12 entries)
YearlyComparisonDto
csharp
public record YearlyComparisonDto(
    int Month,
    string MonthName,
    decimal Year1Amount,
    decimal Year2Amount,
    decimal Difference,
    decimal? PercentageChange,
    string Currency
);
Note: PercentageChange is null when Year1Amount is 0 (division by zero protection).

GetTransactionSummaryQuery
csharp
public record GetTransactionSummaryQuery(
    DateTime FromDate,
    DateTime ToDate
) : IRequest<TransactionSummaryDto>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Build specifications: user + date range
3. Load all matching transactions
4. Calculate:
   a. TotalIncome: sum of positive amounts
   b. TotalExpenses: sum of absolute negative amounts
   c. NetAmount: TotalIncome - TotalExpenses
   d. TransactionCount: total number of transactions
5. Return TransactionSummaryDto
TransactionSummaryDto
csharp
public record TransactionSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount,
    int TransactionCount,
    string Currency,
    DateTime FromDate,
    DateTime ToDate
);
GetRecentTransactionsQuery
csharp
public record GetRecentTransactionsQuery(
    int Count = 10
) : IRequest<List<TransactionDto>>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Load transactions for user, sorted by date descending
3. Take first N (default 10)
4. Map to TransactionDto list
5. Return list
SearchTransactionsQuery
csharp
public record SearchTransactionsQuery(
    string? Keyword = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? CategoryId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PaginatedResultDto<TransactionDto>>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Start with TransactionByUserSpecification(userId)
3. If Keyword provided: AND with TransactionByDescriptionKeywordSpecification
4. If FromDate/ToDate provided: AND with TransactionByDateRangeSpecification
5. If CategoryId provided: AND with TransactionByCategorySpecification
6. If MinAmount/MaxAmount provided: AND with TransactionByAmountRangeSpecification
7. Compose all specs using CompositeSpecification.And()
8. Query via ITransactionRepository.FindBySpecificationAsync()
9. Apply pagination
10. Return PaginatedResultDto<TransactionDto>
FR-4.03: Frontend — Dashboard Page
text
Frontend/
├── Pages/
│   ├── Dashboard.cshtml               # Complete replacement of Phase 1 stub
│   ├── Dashboard.cshtml.cs
│   ├── Transactions/
│   │   ├── Index.cshtml               # Updated with filter controls
│   │   ├── Index.cshtml.cs
│   │   ├── Search.cshtml              # NEW: multi-filter search
│   │   ├── Search.cshtml.cs
│   │   ├── Upload.cshtml              # (from Phase 3 — unchanged)
│   │   ├── Upload.cshtml.cs
│   │   ├── Add.cshtml                 # (from Phase 3 — unchanged)
│   │   └── Add.cshtml.cs
│   └── Categories/
│       ├── Index.cshtml               # (from Phase 3 — unchanged)
│       └── Index.cshtml.cs
├── Shared/
│   ├── _Layout.cshtml                 # Updated: Chart.js CDN, dashboard as default
│   └── Components/
│       └── _DateRangeFilter.cshtml    # NEW: reusable date range filter partial
Dashboard PageModel
csharp
[Authorize]
public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public TransactionSummaryDto Summary { get; set; } = default!;
    public List<CategorySpendingDto> SpendingByCategory { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
    public List<YearlyComparisonDto> YearlyComparison { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string DateFilter { get; set; } = "this-month";

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomFromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomToDate { get; set; }

    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public async Task OnGetAsync()
    {
        CalculateDateRange();

        Summary = await _mediator.Send(
            new GetTransactionSummaryQuery(FromDate, ToDate));

        SpendingByCategory = await _mediator.Send(
            new GetSpendingByCategoryQuery(FromDate, ToDate));

        MonthlyTrends = await _mediator.Send(
            new GetMonthlyTrendsQuery(DateTime.UtcNow.Year));

        YearlyComparison = await _mediator.Send(
            new GetYearlyComparisonQuery(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Year));

        RecentTransactions = await _mediator.Send(
            new GetRecentTransactionsQuery(10));
    }

    private void CalculateDateRange()
    {
        var now = DateTime.UtcNow;
        (FromDate, ToDate) = DateFilter switch
        {
            "this-month" => (new DateTime(now.Year, now.Month, 1),
                            new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month))),
            "last-month" => (new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                            new DateTime(now.Year, now.Month, 1).AddDays(-1)),
            "last-3-months" => (now.AddMonths(-3).Date, now.Date),
            "this-year" => (new DateTime(now.Year, 1, 1), now.Date),
            "custom" when CustomFromDate.HasValue && CustomToDate.HasValue
                => (CustomFromDate.Value, CustomToDate.Value),
            _ => (new DateTime(now.Year, now.Month, 1),
                 new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)))
        };
    }
}
Dashboard View Requirements
Summary Cards Section:

Card	Value	Icon	Color	Format
Total Income	Summary.TotalIncome	📈	Green (text-green-600)	€1,234.56
Total Expenses	Summary.TotalExpenses	📉	Red (text-red-600)	€1,234.56
Net Amount	Summary.NetAmount	💰	Green if ≥ 0, Red if < 0	€1,234.56
Transactions	Summary.TransactionCount	📊	Blue (text-blue-600)	123
Charts Section:

html
<!-- Pie Chart: Category Breakdown -->
<div class="bg-white rounded-lg shadow p-6">
    <h3 class="text-lg font-semibold mb-4">Spending by Category</h3>
    <canvas id="categoryPieChart"></canvas>
</div>

<!-- Line Chart: Monthly Trends -->
<div class="bg-white rounded-lg shadow p-6">
    <h3 class="text-lg font-semibold mb-4">Monthly Trends</h3>
    <canvas id="monthlyTrendsChart"></canvas>
</div>

<!-- Bar Chart: Yearly Comparison -->
<div class="bg-white rounded-lg shadow p-6">
    <h3 class="text-lg font-semibold mb-4">Year over Year</h3>
    <canvas id="yearlyComparisonChart"></canvas>
</div>
Chart.js Configuration Pattern:

javascript
// Data passed from server via JSON in a <script> block
const categoryData = @Html.Raw(Json.Serialize(Model.SpendingByCategory));
const monthlyData = @Html.Raw(Json.Serialize(Model.MonthlyTrends));
const yearlyData = @Html.Raw(Json.Serialize(Model.YearlyComparison));

// Pie Chart
new Chart(document.getElementById('categoryPieChart'), {
    type: 'pie',
    data: {
        labels: categoryData.map(c => c.categoryName),
        datasets: [{
            data: categoryData.map(c => c.amount),
            backgroundColor: categoryData.map(c => c.categoryColor || defaultColors[i])
        }]
    },
    options: {
        responsive: true,
        plugins: {
            legend: { position: 'bottom' },
            tooltip: {
                callbacks: {
                    label: (ctx) => `${ctx.label}: €${ctx.parsed.toFixed(2)} (${categoryData[ctx.dataIndex].percentage.toFixed(1)}%)`
                }
            }
        }
    }
});

// Line Chart
new Chart(document.getElementById('monthlyTrendsChart'), {
    type: 'line',
    data: {
        labels: monthlyData.map(m => m.monthName),
        datasets: [
            {
                label: 'Expenses',
                data: monthlyData.map(m => m.totalExpenses),
                borderColor: '#EF4444',
                tension: 0.3
            },
            {
                label: 'Income',
                data: monthlyData.map(m => m.totalIncome),
                borderColor: '#10B981',
                tension: 0.3
            }
        ]
    },
    options: { responsive: true, scales: { y: { beginAtZero: true } } }
});

// Bar Chart
new Chart(document.getElementById('yearlyComparisonChart'), {
    type: 'bar',
    data: {
        labels: yearlyData.map(y => y.monthName),
        datasets: [
            {
                label: `${year1}`,
                data: yearlyData.map(y => y.year1Amount),
                backgroundColor: '#3B82F6'
            },
            {
                label: `${year2}`,
                data: yearlyData.map(y => y.year2Amount),
                backgroundColor: '#8B5CF6'
            }
        ]
    },
    options: { responsive: true, scales: { y: { beginAtZero: true } } }
});
Recent Transactions Widget:

Table with columns: Date, Description, Amount, Category
Maximum 10 rows
"View all transactions →" link at bottom
Amount formatted with color (green positive, red negative)
Date Range Filter Component (_DateRangeFilter.cshtml):

html
<!-- Reusable partial view -->
<div x-data="{ showCustom: false }" class="flex flex-wrap gap-2 items-center mb-6">
    <select name="DateFilter" class="rounded-md border-gray-300 ..."
            x-on:change="showCustom = ($event.target.value === 'custom')">
        <option value="this-month">This Month</option>
        <option value="last-month">Last Month</option>
        <option value="last-3-months">Last 3 Months</option>
        <option value="this-year">This Year</option>
        <option value="custom">Custom Range</option>
    </select>

    <div x-show="showCustom" class="flex gap-2">
        <input type="date" name="CustomFromDate" class="rounded-md border-gray-300 ..." />
        <input type="date" name="CustomToDate" class="rounded-md border-gray-300 ..." />
    </div>

    <button type="submit" class="bg-blue-600 text-white px-4 py-2 rounded-md">Apply</button>
</div>
Search Transactions Page (/Transactions/Search)
csharp
[Authorize]
public class SearchTransactionsModel : PageModel
{
    private readonly IMediator _mediator;

    public PaginatedResultDto<TransactionDto> Results { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public List<CategoryDto> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());

        Results = await _mediator.Send(new SearchTransactionsQuery(
            Keyword, FromDate, ToDate, CategoryId, MinAmount, MaxAmount, Page));
    }
}
View Requirements:

Filter panel at top:
Keyword text input with search icon
Date range: From and To date pickers
Category dropdown (all categories + "All Categories" option)
Amount range: Min and Max number inputs
Search button + Clear filters button
Results table (same format as transaction list)
Result count: "Showing N of M transactions"
Filters preserved in URL query parameters
Pagination controls
FR-4.04: Updated Navigation
Authenticated Navigation Items (Updated):

Label	Route	Icon	Notes
Dashboard	/Dashboard	📊	Default landing page
Transactions	/Transactions	💳	Transaction list
Search	/Transactions/Search	🔍	Multi-filter search
Upload PDF	/Transactions/Upload	📄	PDF import
Categories	/Categories	🏷️	Category management
Logout	(POST action)	🚪	Clears session
FR-4.05: Layout Updates
Chart.js CDN Addition:

html
<!-- In _Layout.cshtml <head> section -->
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.x.x/dist/chart.umd.min.js"></script>
Default Redirect Update:

csharp
// In Program.cs or via convention
// After login: redirect to /Dashboard (already configured in Phase 1)
// Root "/" health check remains public
FR-4.06: Infrastructure Optimizations
Repository Enhancements for Analytics:

csharp
// ITransactionRepository additions (or new methods in existing interface)
// These may use optimized Supabase queries

// Option A: Use existing FindBySpecificationAsync with composed specs
// Option B: Add specific analytics methods if performance requires

// For MVP: Option A is preferred (reuse existing spec-based queries)
// Post-MVP: Optimize with Supabase RPC functions if needed
Supabase Query Optimization Notes:

Analytics queries load all user transactions for the period, then aggregate in memory
For MVP, this is acceptable given the 1000-row MaxResults default
For large datasets (post-MVP), consider Supabase RPC functions for server-side aggregation
Index idx_transactions_user_date supports efficient date-range queries
Architecture Notes
Analytics Query Flow
text
┌─────────────┐     ┌───────────────────────────────────┐     ┌──────────────────┐
│  Dashboard   │     │         Application Layer          │     │  Infrastructure  │
│  PageModel   │     │                                     │     │                  │
└──────┬───────┘     └───────────────────────────────────┘     └──────────────────┘
       │                                                              
       │ OnGetAsync()                                                 
       │                                                              
       ├─► _mediator.Send(GetTransactionSummaryQuery)                 
       │        │                                                     
       │        ▼                                                     
       │   ┌─────────────────────────────────────┐                    
       │   │ GetTransactionSummaryQueryHandler    │                    
       │   │                                       │                    
       │   │ 1. Get UserId from IUserContext        │                    
       │   │ 2. Compose specs (user + date range)  │                    
       │   │ 3. Query repository ──────────────────┼──► ITransactionRepository
       │   │ 4. Aggregate: income, expenses, net   │    .FindBySpecificationAsync()
       │   │ 5. Return TransactionSummaryDto       │                    
       │   └─────────────────────────────────────┘                    
       │                                                              
       ├─► _mediator.Send(GetSpendingByCategoryQuery)                 
       │        │ (similar flow: load → group by category → sum)      
       │        ▼                                                     
       │   ... handler → repo → aggregate → DTO                      
       │                                                              
       ├─► _mediator.Send(GetMonthlyTrendsQuery)                      
       │        │ (load year → group by month → sum per month)        
       │        ▼                                                     
       │   ... handler → repo → aggregate → DTO                      
       │                                                              
       ├─► _mediator.Send(GetYearlyComparisonQuery)                   
       │        │ (load 2 years → group each by month → compare)      
       │        ▼                                                     
       │   ... handler → repo → aggregate → DTO                      
       │                                                              
       └─► _mediator.Send(GetRecentTransactionsQuery)                 
                │ (load user txns → sort by date desc → take 10)      
                ▼                                                     
           ... handler → repo → map → DTO list                       
Specification Composition Pattern
text
SearchTransactionsQuery(keyword="coffee", fromDate=Jan 1, categoryId=X)
    │
    ▼
Build specifications:
    spec1 = TransactionByUserSpecification(userId)
    spec2 = TransactionByDescriptionKeywordSpecification("coffee")
    spec3 = TransactionByDateRangeSpecification(Jan 1 – today)
    spec4 = TransactionByCategorySpecification(X)
    │
    ▼
Compose:
    composed = CompositeSpecification.And(spec1, spec2)
    composed = CompositeSpecification.And(composed, spec3)
    composed = CompositeSpecification.And(composed, spec4)
    │
    ▼
Query: ITransactionRepository.FindBySpecificationAsync(composed)
NuGet Packages (Phase 4 Additions)
Project	New Packages	Notes
SauronSheet.Domain	None	Still zero dependencies
SauronSheet.Application	None	No new packages needed
SauronSheet.Infrastructure	None	Existing Supabase client handles analytics queries
SauronSheet.Frontend	None	Chart.js added via CDN in layout
SauronSheet.Domain.Tests	None	Existing xUnit + Moq
SauronSheet.Application.Tests	None	Existing xUnit + Moq
File Structure (Phase 4 Additions)
text
Domain/
├── Specifications/
│   ├── (existing from Phase 2)
│   ├── TransactionByDescriptionKeywordSpecification.cs    # NEW
│   └── CompositeSpecification.cs                          # NEW

Application/
├── Features/
│   ├── Analytics/                                          # NEW (entire folder)
│   │   ├── Queries/
│   │   │   ├── GetSpendingByCategoryQuery.cs
│   │   │   ├── GetSpendingByCategoryQueryHandler.cs
│   │   │   ├── GetMonthlyTrendsQuery.cs
│   │   │   ├── GetMonthlyTrendsQueryHandler.cs
│   │   │   ├── GetYearlyComparisonQuery.cs
│   │   │   ├── GetYearlyComparisonQueryHandler.cs
│   │   │   ├── GetTransactionSummaryQuery.cs
│   │   │   └── GetTransactionSummaryQueryHandler.cs
│   │   └── DTOs/
│   │       ├── CategorySpendingDto.cs
│   │       ├── MonthlyTrendDto.cs
│   │       ├── YearlyComparisonDto.cs
│   │       └── TransactionSummaryDto.cs
│   └── Transactions/
│       ├── Queries/
│       │   ├── (existing from Phase 3)
│       │   ├── GetRecentTransactionsQuery.cs               # NEW
│       │   ├── GetRecentTransactionsQueryHandler.cs        # NEW
│       │   ├── SearchTransactionsQuery.cs                  # NEW
│       │   └── SearchTransactionsQueryHandler.cs           # NEW

Frontend/
├── Pages/
│   ├── Dashboard.cshtml                                     # REPLACED (complete rewrite)
│   ├── Dashboard.cshtml.cs
│   ├── Transactions/
│   │   ├── Search.cshtml                                    # NEW
│   │   └── Search.cshtml.cs
├── Shared/
│   ├── _Layout.cshtml                                       # UPDATED (Chart.js CDN, nav)
│   └── Components/
│       └── _DateRangeFilter.cshtml                          # NEW (reusable partial)
├── wwwroot/
│   └── js/
│       └── charts.js                                        # NEW (Chart.js initialization)
text

Ahora va la **Parte 2/2** — pégalo justo después:

```markdown

---

## Test Specifications

### Analytics Query Tests
TEST T-4.01: GetSpendingByCategory_WithTransactions_ReturnsGroupedData
GIVEN 5 transactions: 2 in "Groceries" (€50+€30), 2 in "Transport" (€20+€10), 1 uncategorized (€15)
AND date range covers all transactions
WHEN GetSpendingByCategoryQueryHandler handles the query
THEN returns 3 CategorySpendingDto entries
AND "Groceries" has Amount = 80, Percentage ≈ 64%
AND "Transport" has Amount = 30, Percentage ≈ 24%
AND "Uncategorized" has Amount = 15, Percentage ≈ 12%

TEST T-4.02: GetSpendingByCategory_NoTransactions_ReturnsEmptyList
GIVEN no transactions for user in the date range
WHEN GetSpendingByCategoryQueryHandler handles the query
THEN returns empty list

TEST T-4.03: GetSpendingByCategory_OnlyIncomeTransactions_ReturnsEmptyList
GIVEN only positive-amount transactions (income) for user in date range
WHEN GetSpendingByCategoryQueryHandler handles the query
THEN returns empty list (only expenses are analyzed)

TEST T-4.04: GetSpendingByCategory_MoreThanSixCategories_GroupsIntoOther
GIVEN transactions in 8 different categories
WHEN GetSpendingByCategoryQueryHandler handles the query
THEN returns 7 entries (top 6 + "Other")
AND "Other" amount equals sum of categories 7 and 8

TEST T-4.05: GetMonthlyTrends_FullYear_Returns12Entries
GIVEN transactions spread across Jan, Mar, Jun, Dec of 2026
WHEN GetMonthlyTrendsQueryHandler handles the query for year 2026
THEN returns exactly 12 MonthlyTrendDto entries (Jan–Dec)
AND months with transactions have non-zero amounts
AND months without transactions have zero amounts

TEST T-4.06: GetMonthlyTrends_NoTransactions_Returns12ZeroEntries
GIVEN no transactions for user in the requested year
WHEN GetMonthlyTrendsQueryHandler handles the query
THEN returns 12 entries all with TotalExpenses = 0, TotalIncome = 0, NetAmount = 0

TEST T-4.07: GetMonthlyTrends_SeparatesIncomeAndExpenses
GIVEN January has: €500 income, -€300 expense, -€100 expense
WHEN GetMonthlyTrendsQueryHandler handles the query
THEN January entry has TotalIncome = 500, TotalExpenses = 400, NetAmount = 100

TEST T-4.08: GetYearlyComparison_TwoYears_ReturnsMonthlyComparison
GIVEN Year 2025: Jan €100, Feb €200; Year 2026: Jan €150, Feb €180
WHEN GetYearlyComparisonQueryHandler handles the query for 2025 vs 2026
THEN returns 12 entries
AND January: Year1Amount = 100, Year2Amount = 150, Difference = 50
AND February: Year1Amount = 200, Year2Amount = 180, Difference = -20

TEST T-4.09: GetYearlyComparison_NoDataForOneYear_ReturnsZeros
GIVEN Year 2025 has transactions; Year 2024 has none
WHEN GetYearlyComparisonQueryHandler handles the query for 2024 vs 2025
THEN Year1Amount (2024) is 0 for all months
AND Year2Amount (2025) has actual data

TEST T-4.10: GetYearlyComparison_PercentageChange_ZeroDivision
GIVEN Year1 January = €0, Year2 January = €150
WHEN GetYearlyComparisonQueryHandler handles the query
THEN January PercentageChange is null (not infinity or error)

TEST T-4.11: GetTransactionSummary_CalculatesCorrectly
GIVEN transactions: +€500, +€200 (income), -€300, -€100, -€50 (expenses)
AND all within the date range
WHEN GetTransactionSummaryQueryHandler handles the query
THEN TotalIncome = 700
AND TotalExpenses = 450
AND NetAmount = 250
AND TransactionCount = 5

TEST T-4.12: GetTransactionSummary_NoTransactions_ReturnsZeros
GIVEN no transactions for user in date range
WHEN GetTransactionSummaryQueryHandler handles the query
THEN TotalIncome = 0, TotalExpenses = 0, NetAmount = 0, TransactionCount = 0

TEST T-4.13: GetTransactionSummary_OnlyExpenses_NetIsNegative
GIVEN transactions: -€300, -€200 (expenses only)
WHEN GetTransactionSummaryQueryHandler handles the query
THEN TotalIncome = 0
AND TotalExpenses = 500
AND NetAmount = -500

TEST T-4.14: GetTransactionSummary_RespectsDateRange
GIVEN transactions in January (€100) and February (€200)
AND date range covers only January
WHEN GetTransactionSummaryQueryHandler handles the query
THEN TotalExpenses = 100 (February excluded)
AND TransactionCount = 1

text

### Recent Transactions & Search Tests
TEST T-4.15: GetRecentTransactions_ReturnsLastN
GIVEN 20 transactions for user
WHEN GetRecentTransactionsQueryHandler handles the query with count = 10
THEN returns exactly 10 TransactionDto items
AND sorted by date descending (newest first)

TEST T-4.16: GetRecentTransactions_FewerThanN_ReturnsAll
GIVEN 3 transactions for user
WHEN GetRecentTransactionsQueryHandler handles the query with count = 10
THEN returns 3 TransactionDto items

TEST T-4.17: GetRecentTransactions_NoTransactions_ReturnsEmptyList
GIVEN no transactions for user
WHEN GetRecentTransactionsQueryHandler handles the query
THEN returns empty list

TEST T-4.18: SearchTransactions_ByKeyword_FiltersCorrectly
GIVEN transactions: "Coffee shop", "Grocery store", "Coffee beans", "Gas station"
WHEN SearchTransactionsQueryHandler handles query with keyword = "coffee"
THEN returns 2 transactions ("Coffee shop", "Coffee beans")
AND search is case-insensitive

TEST T-4.19: SearchTransactions_ByDateRange_FiltersCorrectly
GIVEN transactions: Jan 5, Jan 15, Feb 1, Feb 20
WHEN SearchTransactionsQueryHandler handles query with fromDate = Jan 10, toDate = Feb 5
THEN returns 2 transactions (Jan 15, Feb 1)

TEST T-4.20: SearchTransactions_ByCategory_FiltersCorrectly
GIVEN transactions: 3 in "Groceries", 2 in "Transport"
WHEN SearchTransactionsQueryHandler handles query with categoryId = GroceriesId
THEN returns 3 transactions

TEST T-4.21: SearchTransactions_ByAmountRange_FiltersCorrectly
GIVEN transactions with amounts: €10, €50, €100, €200, €500
WHEN SearchTransactionsQueryHandler handles query with minAmount = 50, maxAmount = 200
THEN returns 3 transactions (€50, €100, €200)

TEST T-4.22: SearchTransactions_CombinedFilters_AppliesAll
GIVEN various transactions
AND query with keyword = "coffee", categoryId = GroceriesId, fromDate = Jan 1
WHEN SearchTransactionsQueryHandler handles the query
THEN only transactions matching ALL criteria are returned

TEST T-4.23: SearchTransactions_NoFilters_ReturnsAllUserTransactions
GIVEN 10 transactions for user
WHEN SearchTransactionsQueryHandler handles query with no filters
THEN returns all 10 transactions (paginated)

TEST T-4.24: SearchTransactions_NoResults_ReturnsEmptyPage
GIVEN transactions exist but none match filters
WHEN SearchTransactionsQueryHandler handles the query
THEN returns PaginatedResultDto with Items = empty, TotalCount = 0

TEST T-4.25: SearchTransactions_Paginated_RespectsPageSize
GIVEN 100 matching transactions
AND query with page = 2, pageSize = 25
WHEN SearchTransactionsQueryHandler handles the query
THEN returns 25 items (page 2)
AND TotalCount = 100
AND TotalPages = 4

text

### Domain Specification Tests
TEST T-4.26: DescriptionKeywordSpec_MatchesPartialKeyword
GIVEN a Transaction with description = "Morning Coffee at Starbucks"
AND TransactionByDescriptionKeywordSpecification("coffee")
WHEN Criteria is compiled and evaluated
THEN returns true (case-insensitive partial match)

TEST T-4.27: DescriptionKeywordSpec_NoMatch_ReturnsFalse
GIVEN a Transaction with description = "Grocery shopping"
AND TransactionByDescriptionKeywordSpecification("coffee")
WHEN Criteria is compiled and evaluated
THEN returns false

TEST T-4.28: DescriptionKeywordSpec_EmptyKeyword_ThrowsDomainException
GIVEN keyword = ""
WHEN TransactionByDescriptionKeywordSpecification is constructed
THEN throws DomainException with message containing "keyword cannot be empty"

TEST T-4.29: DescriptionKeywordSpec_CaseInsensitive
GIVEN a Transaction with description = "COFFEE BEANS"
AND TransactionByDescriptionKeywordSpecification("coffee")
WHEN Criteria is compiled and evaluated
THEN returns true

TEST T-4.30: CompositeSpec_And_CombinesTwoSpecs
GIVEN TransactionByUserSpecification(userA) AND TransactionByCategorySpecification(catX)
AND a Transaction belonging to userA with categoryId = catX
WHEN composed spec Criteria is compiled and evaluated
THEN returns true

TEST T-4.31: CompositeSpec_And_RejectsMismatch
GIVEN TransactionByUserSpecification(userA) AND TransactionByCategorySpecification(catX)
AND a Transaction belonging to userA with categoryId = catY (different)
WHEN composed spec Criteria is compiled and evaluated
THEN returns false (user matches but category doesn't)

TEST T-4.32: CompositeSpec_And_MultipleSpecs
GIVEN specs: user + dateRange + category
AND a Transaction matching user and dateRange but NOT category
WHEN all three composed with And
THEN returns false (all conditions must be true)

text

---

## Test Summary

| Test ID | Test Name                                                       | Category    | Area                  |
|---------|-----------------------------------------------------------------|-------------|-----------------------|
| T-4.01  | GetSpendingByCategory_WithTransactions_ReturnsGroupedData       | Application | Analytics - Category  |
| T-4.02  | GetSpendingByCategory_NoTransactions_ReturnsEmptyList           | Application | Analytics - Category  |
| T-4.03  | GetSpendingByCategory_OnlyIncomeTransactions_ReturnsEmptyList   | Application | Analytics - Category  |
| T-4.04  | GetSpendingByCategory_MoreThanSixCategories_GroupsIntoOther     | Application | Analytics - Category  |
| T-4.05  | GetMonthlyTrends_FullYear_Returns12Entries                      | Application | Analytics - Trends    |
| T-4.06  | GetMonthlyTrends_NoTransactions_Returns12ZeroEntries            | Application | Analytics - Trends    |
| T-4.07  | GetMonthlyTrends_SeparatesIncomeAndExpenses                     | Application | Analytics - Trends    |
| T-4.08  | GetYearlyComparison_TwoYears_ReturnsMonthlyComparison           | Application | Analytics - Comparison|
| T-4.09  | GetYearlyComparison_NoDataForOneYear_ReturnsZeros               | Application | Analytics - Comparison|
| T-4.10  | GetYearlyComparison_PercentageChange_ZeroDivision               | Application | Analytics - Comparison|
| T-4.11  | GetTransactionSummary_CalculatesCorrectly                       | Application | Analytics - Summary   |
| T-4.12  | GetTransactionSummary_NoTransactions_ReturnsZeros               | Application | Analytics - Summary   |
| T-4.13  | GetTransactionSummary_OnlyExpenses_NetIsNegative                | Application | Analytics - Summary   |
| T-4.14  | GetTransactionSummary_RespectsDateRange                         | Application | Analytics - Summary   |
| T-4.15  | GetRecentTransactions_ReturnsLastN                              | Application | Recent Transactions   |
| T-4.16  | GetRecentTransactions_FewerThanN_ReturnsAll                     | Application | Recent Transactions   |
| T-4.17  | GetRecentTransactions_NoTransactions_ReturnsEmptyList           | Application | Recent Transactions   |
| T-4.18  | SearchTransactions_ByKeyword_FiltersCorrectly                   | Application | Search                |
| T-4.19  | SearchTransactions_ByDateRange_FiltersCorrectly                 | Application | Search                |
| T-4.20  | SearchTransactions_ByCategory_FiltersCorrectly                  | Application | Search                |
| T-4.21  | SearchTransactions_ByAmountRange_FiltersCorrectly               | Application | Search                |
| T-4.22  | SearchTransactions_CombinedFilters_AppliesAll                   | Application | Search                |
| T-4.23  | SearchTransactions_NoFilters_ReturnsAllUserTransactions         | Application | Search                |
| T-4.24  | SearchTransactions_NoResults_ReturnsEmptyPage                   | Application | Search                |
| T-4.25  | SearchTransactions_Paginated_RespectsPageSize                   | Application | Search                |
| T-4.26  | DescriptionKeywordSpec_MatchesPartialKeyword                    | Domain      | Specification         |
| T-4.27  | DescriptionKeywordSpec_NoMatch_ReturnsFalse                     | Domain      | Specification         |
| T-4.28  | DescriptionKeywordSpec_EmptyKeyword_ThrowsDomainException       | Domain      | Specification         |
| T-4.29  | DescriptionKeywordSpec_CaseInsensitive                          | Domain      | Specification         |
| T-4.30  | CompositeSpec_And_CombinesTwoSpecs                              | Domain      | Specification         |
| T-4.31  | CompositeSpec_And_RejectsMismatch                               | Domain      | Specification         |
| T-4.32  | CompositeSpec_And_MultipleSpecs                                 | Domain      | Specification         |

**Total: 32 tests (25 Application + 7 Domain)**

**Tests by Area:**

| Area                  | Test Count | Test IDs                     |
|-----------------------|------------|------------------------------|
| Analytics - Category  | 4          | T-4.01–T-4.04               |
| Analytics - Trends    | 3          | T-4.05–T-4.07               |
| Analytics - Comparison| 3          | T-4.08–T-4.10               |
| Analytics - Summary   | 4          | T-4.11–T-4.14               |
| Recent Transactions   | 3          | T-4.15–T-4.17               |
| Search                | 8          | T-4.18–T-4.25               |
| Specification         | 7          | T-4.26–T-4.32               |

---

## Deliverables

| #      | Deliverable                                                           | Layer          | Acceptance                                                             |
|--------|-----------------------------------------------------------------------|----------------|------------------------------------------------------------------------|
| D-4.01 | `GetSpendingByCategoryQuery` + handler                                | Application    | Tests T-4.01–T-4.04 pass                                              |
| D-4.02 | `GetMonthlyTrendsQuery` + handler                                     | Application    | Tests T-4.05–T-4.07 pass                                              |
| D-4.03 | `GetYearlyComparisonQuery` + handler                                  | Application    | Tests T-4.08–T-4.10 pass                                              |
| D-4.04 | `GetTransactionSummaryQuery` + handler                                | Application    | Tests T-4.11–T-4.14 pass                                              |
| D-4.05 | `GetRecentTransactionsQuery` + handler                                | Application    | Tests T-4.15–T-4.17 pass                                              |
| D-4.06 | `SearchTransactionsQuery` + handler                                   | Application    | Tests T-4.18–T-4.25 pass                                              |
| D-4.07 | Analytics DTOs (CategorySpendingDto, MonthlyTrendDto, YearlyComparisonDto, TransactionSummaryDto) | Application | Compile; used by handlers and frontend |
| D-4.08 | `TransactionByDescriptionKeywordSpecification`                        | Domain         | Tests T-4.26–T-4.29 pass                                              |
| D-4.09 | `CompositeSpecification<T>`                                           | Domain         | Tests T-4.30–T-4.32 pass                                              |
| D-4.10 | Dashboard page (complete rewrite)                                     | Frontend       | Summary cards + 3 charts + recent transactions + date filter           |
| D-4.11 | Chart.js integration (pie, line, bar)                                 | Frontend       | Charts render correctly with real data                                 |
| D-4.12 | Date range filter component (`_DateRangeFilter.cshtml`)               | Frontend       | Reusable; works on dashboard and search page                           |
| D-4.13 | Transaction search page (`/Transactions/Search`)                      | Frontend       | Multi-filter search with pagination                                    |
| D-4.14 | Updated transaction list page with filter controls                    | Frontend       | Filters integrated from search component                               |
| D-4.15 | Updated `_Layout.cshtml` (Chart.js CDN, navigation)                   | Frontend       | Chart.js loaded; Search link added to nav                              |
| D-4.16 | `charts.js` static JavaScript file                                    | Frontend       | Chart initialization extracted to reusable file                        |
| D-4.17 | Domain.Tests for specifications (7 tests)                             | Tests          | `dotnet test --filter Category=Domain` all green                       |
| D-4.18 | Application.Tests for analytics + search (25 tests)                   | Tests          | `dotnet test --filter Category=Application` all green                  |

---

## Success Criteria

| #      | Criterion                                                                           | Metric                                                                 |
|--------|-------------------------------------------------------------------------------------|------------------------------------------------------------------------|
| SC-4.1 | Dashboard shows correct spending summary cards                                      | E2E: import PDF → dashboard shows accurate income/expense/net/count    |
| SC-4.2 | Pie chart displays spending by category correctly                                   | Category amounts and percentages match raw transaction data            |
| SC-4.3 | Line chart shows monthly trends for the year                                        | 12 data points; months with no data show zero                          |
| SC-4.4 | Bar chart compares two years correctly                                              | Side-by-side bars with correct amounts per month                       |
| SC-4.5 | Date range filter works across dashboard                                            | Changing filter updates all cards and charts                           |
| SC-4.6 | Transaction search with keyword finds matching transactions                         | Search "coffee" → returns transactions with "coffee" in description    |
| SC-4.7 | Combined search filters work correctly (AND logic)                                  | Keyword + category + date range → only matching transactions returned  |
| SC-4.8 | Search results are paginated                                                        | 100+ results → pagination controls functional                          |
| SC-4.9 | Filter values preserved in URL parameters                                           | Refresh page → filters retained; URL is shareable                      |
| SC-4.10| Recent transactions widget shows last 10                                            | Widget displays correct 10 most recent transactions                    |
| SC-4.11| Dashboard is responsive (mobile, tablet, desktop)                                   | Visual verification on 320px, 768px, 1024px+ viewports                |
| SC-4.12| Charts render without JavaScript errors                                             | Browser console has no errors on dashboard page                        |
| SC-4.13| All Phase 4 tests pass (32 tests)                                                   | `dotnet test` all green                                                |
| SC-4.14| All prior phase tests still pass (no regressions)                                   | `dotnet test` → Phase 0 + 1 + 2 + 3 + 4 all green                    |
| SC-4.15| Application layer test coverage ≥ 70%                                               | coverlet report on Application project                                 |
| SC-4.16| Domain test coverage ≥ 80%                                                          | coverlet report on Domain project (cumulative)                         |
| SC-4.17| **MVP COMPLETE**: User can register, import PDF, manage transactions, view analytics | Full workflow E2E test passes                                          |

---

## Assumptions

1. **Phases 0–3 are fully implemented and tested.** All foundation, auth, domain model, transaction CRUD, category management, PDF import, and Supabase repositories are stable.
2. **Chart.js 4.x is available via CDN** and does not require npm/node for installation.
3. **Analytics aggregation is performed in memory** in Application handlers after loading transactions from the repository. This is acceptable for MVP given the 1000-row MaxResults default. Server-side aggregation (Supabase RPC) is a post-MVP optimization.
4. **Date calculations use `DateTime.UtcNow`** consistently. No timezone-aware date handling in this phase.
5. **"This Month" date range** means from the 1st of the current month through the last day of the current month (not "last 30 days").
6. **Currency is EUR for all analytics.** Multi-currency aggregation is deferred to post-MVP.
7. **The "Other" grouping in pie chart** triggers when more than 6 categories have spending. The threshold is hardcoded for MVP.
8. **Search keyword is a simple `Contains` match** on the description field. Full-text search (PostgreSQL `tsvector`) is a post-MVP optimization.
9. **`CompositeSpecification<T>` uses `Expression.Invoke`** which may not translate directly to all ORM query providers. For Supabase Postgrest (in-memory evaluation), this is fine. Document as known limitation.
10. **No caching of analytics results.** Each dashboard load re-queries the database. Caching is a Phase 6 optimization.

---

## Risks & Mitigations

| ID    | Risk                                                                     | Impact | Probability | Mitigation                
|
 ID    
|
 Risk                                                                     
|
 Impact 
|
 Probability 
|
 Mitigation                                                                                       
|
|
-------
|
--------------------------------------------------------------------------
|
--------
|
-------------
|
--------------------------------------------------------------------------------------------------
|
|
 R-4.1 
|
 In-memory aggregation slow for users with many transactions              
|
 Medium 
|
 Medium      
|
 1000-row MaxResults limits data; post-MVP: Supabase RPC for server-side aggregation              
|
|
 R-4.2 
|
 Chart.js CDN unavailable or slow                                         
|
 Low    
|
 Low         
|
 Fallback: bundle Chart.js locally; CDN has high availability                                     
|
|
 R-4.3 
|
`Expression.Invoke`
 in CompositeSpecification not compatible with Supabase 
|
 Medium 
|
 Medium      
|
 Specs evaluated in-memory after repo loads data; document as known limitation                     
|
|
 R-4.4 
|
 Date range filter edge cases (timezone, month boundaries)                
|
 Medium 
|
 Medium      
|
 Use 
`DateTime.UtcNow`
 consistently; test boundary dates explicitly                               
|
|
 R-4.5 
|
 Dashboard loads slowly (multiple sequential queries)                     
|
 Medium 
|
 Medium      
|
 Queries are independent — can parallelize with 
`Task.WhenAll`
 if needed; optimize post-MVP       
|
|
 R-4.6 
|
 Pie chart unreadable with many small categories                          
|
 Low    
|
 Medium      
|
 Group into "Other" when > 6 categories; tested in T-4.04                                         
|
|
 R-4.7 
|
 Search keyword injection (malicious input)                               
|
 Low    
|
 Low         
|
 Specifications use parameterized expressions; Supabase Postgrest handles escaping                
|
|
 R-4.8 
|
 Yearly comparison meaningless for new users (no prior year data)         
|
 Low    
|
 High        
|
 Show zero bars for year with no data; informational message in UI                                
|
|
 R-4.9 
|
 Chart.js version mismatch with documentation examples                    
|
 Low    
|
 Low         
|
 Pin specific Chart.js version in CDN URL (e.g., 
`chart.js@4.4.0`
)                               
|

---

## Implementation Notes

### Recommended Implementation Order
Step 1: Write Domain.Tests for new specifications (RED phase)
└── Tests T-4.26–T-4.32
└── TransactionByDescriptionKeywordSpecification tests
└── CompositeSpecification tests
└── Verify: tests FAIL (red)

Step 2: Implement Domain specifications (GREEN phase)
└── Domain/Specifications/TransactionByDescriptionKeywordSpecification.cs
└── Domain/Specifications/CompositeSpecification.cs
└── Verify: dotnet test --filter Category=Domain — new tests GREEN

Step 3: Write Application.Tests for GetTransactionSummaryQuery (RED phase)
└── Tests T-4.11–T-4.14
└── Mock: ITransactionRepository, IUserContext
└── Verify: tests FAIL (red)

Step 4: Implement GetTransactionSummaryQuery + handler (GREEN phase)
└── TransactionSummaryDto
└── Verify: dotnet test --filter Category=Application — summary tests GREEN

Step 5: Write Application.Tests for GetSpendingByCategoryQuery (RED phase)
└── Tests T-4.01–T-4.04
└── Mock: ITransactionRepository, ICategoryRepository, IUserContext
└── Verify: tests FAIL (red)

Step 6: Implement GetSpendingByCategoryQuery + handler (GREEN phase)
└── CategorySpendingDto
└── Verify: tests GREEN

Step 7: Write Application.Tests for GetMonthlyTrendsQuery (RED phase)
└── Tests T-4.05–T-4.07
└── Verify: tests FAIL (red)

Step 8: Implement GetMonthlyTrendsQuery + handler (GREEN phase)
└── MonthlyTrendDto
└── Verify: tests GREEN

Step 9: Write Application.Tests for GetYearlyComparisonQuery (RED phase)
└── Tests T-4.08–T-4.10
└── Verify: tests FAIL (red)

Step 10: Implement GetYearlyComparisonQuery + handler (GREEN phase)
└── YearlyComparisonDto
└── Verify: tests GREEN

Step 11: Write Application.Tests for GetRecentTransactionsQuery (RED phase)
└── Tests T-4.15–T-4.17
└── Verify: tests FAIL (red)

Step 12: Implement GetRecentTransactionsQuery + handler (GREEN phase)
└── Verify: tests GREEN

Step 13: Write Application.Tests for SearchTransactionsQuery (RED phase)
└── Tests T-4.18–T-4.25
└── Mock: ITransactionRepository, IUserContext
└── Verify: tests FAIL (red)

Step 14: Implement SearchTransactionsQuery + handler (GREEN phase)
└── Uses CompositeSpecification to compose filters
└── Verify: dotnet test --filter Category=Application — all search tests GREEN

Step 15: Implement Frontend — Dashboard page
└── Dashboard.cshtml + Dashboard.cshtml.cs (complete rewrite)
└── Summary cards (income, expenses, net, count)
└── Recent transactions widget
└── Verify: page loads with real data from Supabase

Step 16: Implement Chart.js integration
└── Add Chart.js CDN to _Layout.cshtml
└── Create wwwroot/js/charts.js
└── Pie chart (category breakdown)
└── Line chart (monthly trends)
└── Bar chart (yearly comparison)
└── Verify: charts render correctly with data

Step 17: Implement Date range filter component
└── Shared/Components/_DateRangeFilter.cshtml
└── Alpine.js toggle for custom date range
└── Integrate into Dashboard page
└── Verify: filter changes update dashboard data

Step 18: Implement Search page
└── /Transactions/Search
└── Multi-filter form (keyword, dates, category, amounts)
└── Paginated results table
└── URL query parameter preservation
└── Verify: all filter combinations work

Step 19: Update Transaction list page with filter controls
└── Add date range filter and category filter to /Transactions
└── Verify: filters work on existing transaction list

Step 20: Update navigation
└── _Layout.cshtml: add Search link, set Dashboard as primary
└── Verify: navigation works on all pages

Step 21: End-to-end MVP validation
└── Full workflow test:
1. Register new user
2. Import PDF bank statement
3. View imported transactions in list
4. Manually add a transaction
5. Create custom category
6. Categorize transactions
7. View dashboard with charts
8. Change date range filter → data updates
9. Search transactions with multiple filters
10. Verify all charts display correct data
└── Test with two users: verify tenant isolation across analytics
└── Test responsive design on mobile viewport
└── Verify no JavaScript console errors

Step 22: Final test + coverage validation
└── dotnet build → zero errors, zero warnings
└── dotnet test → ALL tests green (Phase 0 + 1 + 2 + 3 + 4)
└── Domain coverage ≥ 80% (cumulative)
└── Application coverage ≥ 70%
└── Audit: no forbidden layer references
└── ✅ MVP CHECKPOINT: All core features operational

text

### Spec-Driven Workflow Compliance

| Step | Workflow Stage           | Phase 4 Action                                                                |
|------|--------------------------|-------------------------------------------------------------------------------|
| 1    | Write Test Spec          | ✅ Tests written first (Steps 1, 3, 5, 7, 9, 11, 13)                        |
| 2    | Define Handler Stub      | ✅ MediatR queries defined (Steps 4, 6, 8, 10, 12, 14)                      |
| 3    | Build Domain             | ✅ New specifications: keyword search + composite (Step 2)                   |
| 4    | Implement Persistence    | ✅ Repository methods reused; optimized queries if needed (existing infra)   |
| 5    | Wire UI                  | ✅ Dashboard, charts, search page, date filter (Steps 15–20)                |
| 6    | End-to-end Test          | ✅ Full MVP validation (Step 21)                                             |

### Testing Patterns Used in This Phase

| Pattern                    | Description                                                                   | Example                                                              |
|----------------------------|-------------------------------------------------------------------------------|----------------------------------------------------------------------|
| Aggregation Test           | Verify grouping, summing, and percentage calculations in handlers             | 5 transactions → group by category → verify amounts and percentages  |
| Zero Data Test             | Verify handlers return sensible defaults when no data exists                  | No transactions → all zeros, empty lists, 12-month array of zeros    |
| Boundary Test              | Verify edge cases (exact date boundaries, zero amounts, division by zero)    | Year1 = 0 → PercentageChange = null (not infinity)                   |
| Filter Composition Test    | Verify multiple specifications combine correctly with AND logic              | Keyword + category + date range → only matching transactions         |
| Pagination Test            | Verify correct page size, total count, total pages calculations              | 100 results, page 2, size 25 → items[25..49], totalPages = 4        |
| Income/Expense Separation  | Verify positive/negative amounts are correctly classified                    | Mixed transactions → income sum, expense sum, net calculation        |

### Dashboard Performance Considerations

| Concern                          | MVP Approach                              | Post-MVP Optimization                          |
|----------------------------------|-------------------------------------------|-------------------------------------------------|
| Multiple sequential queries      | 5 MediatR queries in `OnGetAsync`         | `Task.WhenAll` for parallel execution           |
| In-memory aggregation            | Load transactions → LINQ grouping         | Supabase RPC functions for server-side aggregation |
| Chart data serialization         | `Json.Serialize` in Razor view            | Separate API endpoint with caching              |
| Large transaction datasets       | MaxResults 1000 limits data               | Pagination + incremental loading                |
| Date range recalculation         | Calculated on every request               | Cache per user per date range (Redis/memory)    |

### Chart.js Color Palette

```javascript
// Default colors for pie chart categories (up to 7 including "Other")
const defaultColors = [
    '#3B82F6', // Blue
    '#10B981', // Green
    '#F59E0B', // Amber
    '#EF4444', // Red
    '#8B5CF6', // Purple
    '#EC4899', // Pink
    '#6B7280', // Gray (for "Other")
];

// Line chart colors
const expenseLineColor = '#EF4444'; // Red
const incomeLineColor = '#10B981';  // Green

// Bar chart colors
const year1BarColor = '#3B82F6';    // Blue
const year2BarColor = '#8B5CF6';    // Purple
Security Considerations
☑️ All analytics queries verify UserId from IUserContext (handler-level tenant isolation)
☑️ Search keyword is used in expression lambda (not raw SQL) — no injection risk
☑️ Date range inputs validated (ToDate ≥ FromDate; no future dates beyond reasonable range)
☑️ Amount range inputs validated (MinAmount ≤ MaxAmount; non-negative)
☑️ URL query parameters are server-validated (malicious values rejected gracefully)
☑️ Chart.js CDN loaded via HTTPS with integrity hash (SRI) when available
☑️ JSON data serialized server-side (no client-side API calls for analytics)
☑️ No sensitive data exposed in chart tooltips or labels
Cumulative Test Count (Phases 0–4)
Phase	Domain Tests	Application Tests	Total Phase	Cumulative Total
Phase 0	11	2	13	13
Phase 1	8	14	22	35
Phase 2	81	0	81	116
Phase 3	5	33	38	154
Phase 4	7	25	32	186
MVP Completion Checklist
✅ This checklist validates that the MVP is fully operational after Phase 4.

#	Feature	Phase Delivered	Status
1	User registration (email/password)	Phase 1	✅
2	User login with JWT cookies	Phase 1	✅
3	User logout	Phase 1	✅
4	Tenant isolation (handler + RLS)	Phase 1	✅
5	Domain model (entities, VOs, services)	Phase 2	✅
6	PDF bank statement import	Phase 3	✅
7	Duplicate transaction detection	Phase 3	✅
8	Manual transaction creation	Phase 3	✅
9	Transaction categorization	Phase 3	✅
10	Transaction deletion	Phase 3	✅
11	Category CRUD with system default guards	Phase 3	✅
12	System default categories (4)	Phase 3	✅
13	Analytics dashboard with summary cards	Phase 4	✅
14	Spending by category pie chart	Phase 4	✅
15	Monthly trends line chart	Phase 4	✅
16	Yearly comparison bar chart	Phase 4	✅
17	Transaction search with multi-filter	Phase 4	✅
18	Date range filter (reusable component)	Phase 4	✅
19	Responsive design (mobile + desktop)	Phase 4	✅
20	≥186 automated tests passing	Phase 0–4	✅
Phase Spec Version: 1.0.0 | Created: 2026-02-15 | Aligned with Constitution v1.1.0