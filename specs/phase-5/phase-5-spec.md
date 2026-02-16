# Phase 5: Budget Management & Alerts

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Features)
- **Phase Type**: Full-Stack (Features)
- **Duration**: Weeks 19–21
- **Goal**: Budget CRUD, overage detection, visual alerts on dashboard, budget vs. actual reporting
- **Depends On**: Phase 0 (foundation), Phase 1 (auth + tenant scoping), Phase 2 (domain model — Budget entity, BudgetId, Money, DateRange, IBudgetRepository), Phase 3 (transaction CRUD, category management, Supabase repositories), Phase 4 (analytics dashboard, date range filter, Chart.js integration)
- **Unlocks**: Phase 6 (UI Polish, Performance & Production Deployment)

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
---------
|
----------------------------------------------------------------------
|
----------------------------------------------------------------------------------------------
|
------------
|
|
 CD-5.1  
|
 Budget alerts via visual indicators only (no push/email)             
|
 Immediate dashboard feedback; push notifications deferred to post-MVP                        
|
 2026-02-15 
|
|
 CD-5.2  
|
 Budget status thresholds: green < 60%, yellow 60–80%, red > 80%     
|
 Industry-standard visual cues; overage (> 100%) gets distinct styling                        
|
 2026-02-15 
|
|
 CD-5.3  
|
 Budget vs. actual comparison as dashboard widget + dedicated page    
|
 Quick glance on dashboard; full detail on separate page                                      
|
 2026-02-15 
|
|
 CD-5.4  
|
 Budget uniqueness enforced at handler level + DB constraint          
|
 Belt-and-suspenders: handler validates before insert; DB 
`UNIQUE`
 prevents race conditions   
|
 2026-02-15 
|
|
 CD-5.5  
|
 Current spend calculated from transactions (not denormalized)        
|
 Source of truth is transactions table; avoids stale cache issues                             
|
 2026-02-15 
|
|
 CD-5.6  
|
 Budget month represented as DateRange (1st to last day of month)     
|
 Consistent with domain model; allows flexible month boundaries                               
|
 2026-02-15 
|
|
 CD-5.7  
|
 Budget management page separate from dashboard                       
|
 Dashboard shows status summary; management page for CRUD operations                          
|
 2026-02-15 
|
|
 CD-5.8  
|
 Budget status widget on dashboard replaces placeholder content       
|
 Phase 1 stub "Your dashboard will appear here" fully replaced by Phase 4+5 content           
|
 2026-02-15 
|
|
 CD-5.9  
|
 Overage percentage capped at display level (not domain)              
|
 Domain returns raw percentage (can be > 1.0); UI caps visual bar at 100% with overflow label 
|
 2026-02-15 
|
|
 CD-5.10 
|
 Budget deletion has no cascading effects on transactions             
|
 Deleting a budget doesn't affect categorized transactions; budget is a tracking overlay       
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
`CreateBudgetCommand`
 + handler (create budget with uniqueness check)                                    
|
|
 Application    
|
`UpdateBudgetCommand`
 + handler (update budget limit)                                                    
|
|
 Application    
|
`DeleteBudgetCommand`
 + handler (delete budget)                                                          
|
|
 Application    
|
`GetBudgetsQuery`
 + handler (list budgets for user, optional month filter)                               
|
|
 Application    
|
`GetBudgetByIdQuery`
 + handler (single budget with current spend)                                        
|
|
 Application    
|
`GetBudgetStatusQuery`
 + handler (budget + current spend + percentage + status level)                    
|
|
 Application    
|
`GetBudgetVsActualQuery`
 + handler (all budgets vs. actual spending for a month)                         
|
|
 Application    
|
`GetBudgetSummaryForDashboardQuery`
 + handler (aggregated budget health for dashboard widget)             
|
|
 Application    
|
 DTOs: 
`BudgetDto`
, 
`BudgetStatusDto`
, 
`BudgetVsActualDto`
, 
`BudgetDashboardSummaryDto`
|
|
 Application    
|
`BudgetStatusLevel`
 enum (Green, Yellow, Red, Overage)                                                   
|
|
 Domain         
|
`BudgetService`
 domain service (uniqueness validation, spend calculation coordination)                    
|
|
 Infrastructure 
|
`SupabaseBudgetRepository`
 (implements 
`IBudgetRepository`
)                                              
|
|
 Infrastructure 
|
 Database migration: 
`budgets`
 table with indexes, unique constraint, and RLS                             
|
|
 Frontend       
|
 Budget management page (
`/Budgets`
) — create, edit, delete                                               
|
|
 Frontend       
|
 Budget detail page (
`/Budgets/{id}`
) — single budget with spend progress                                
|
|
 Frontend       
|
 Budget vs. actual page (
`/Budgets/Comparison`
) — all budgets for a month                                 
|
|
 Frontend       
|
 Dashboard budget status widget (green/yellow/red indicators)                                             
|
|
 Frontend       
|
 Updated 
`_Layout.cshtml`
 navigation with budget links                                                    
|
|
 Tests          
|
 ≥36 tests (application handler tests + domain service tests)                                             
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
------------------------------------------------
|
--------------
|
-----------------------------------------------------
|
|
 Budget alerts via email/push notifications     
|
 Post-MVP     
|
 Requires notification infrastructure                
|
|
 Recurring/auto-renewing budgets                
|
 Post-MVP     
|
 Monthly auto-creation adds complexity               
|
|
 Budget templates (copy from previous month)    
|
 Post-MVP     
|
 UX convenience; not core functionality              
|
|
 Budget history / trend analysis                
|
 Post-MVP     
|
 "How has my grocery budget changed over 6 months?"  
|
|
 Multi-category budgets (e.g., "Food" group)    
|
 Post-MVP     
|
 Category grouping not yet supported                 
|
|
 Budget sharing between users                   
|
 Post-MVP     
|
 Multi-user budget collaboration                     
|
|
 CSV/PDF export of budget reports               
|
 Post-MVP     
|
 Export feature                                       
|
|
 Budget rollover (unused amount carries forward)
|
 Post-MVP     
|
 Complex financial logic                              
|
|
 Animated progress bars                         
|
 Phase 6      
|
 Polish concern; static bars in this phase           
|

---

## User Scenarios & Testing

### Scenario 5.1: Create a Budget

**As a** user
**I want to** create a monthly budget for a specific category
**So that** I can set spending limits and track my progress

**Acceptance Criteria:**
- Budget creation form with fields: Category (dropdown), Month (month picker), Limit (amount input)
- Category dropdown shows all user categories (system defaults + user-defined)
- Month picker defaults to current month
- Limit must be a positive amount (> 0)
- One budget per user-category-month enforced:
  - Attempting to create a duplicate shows error: "A budget for {category} in {month} already exists."
- On success: redirect to budget list with success message
- On failure: display specific validation error, form retains values
- Currency defaults to EUR (consistent with system)

### Scenario 5.2: View and Manage Budgets

**As a** user
**I want to** see all my budgets in a list with current status
**So that** I can manage my spending limits

**Acceptance Criteria:**
- Budget list page shows all budgets for the selected month (default: current month)
- Month selector allows switching between months
- Each budget row shows: Category name, Limit amount, Current Spend, Remaining, Percentage Used, Status indicator
- Status indicator: color-coded (green/yellow/red/overage) based on percentage used
- Actions per row: Edit (change limit), Delete (with confirmation)
- Empty state: "No budgets set for {month}. Create one to start tracking." with create link
- Sort: by category name alphabetically

### Scenario 5.3: Edit a Budget

**As a** user
**I want to** change the spending limit on an existing budget
**So that** I can adjust my budget as circumstances change

**Acceptance Criteria:**
- Edit action opens inline edit or modal with current limit pre-filled
- Only the limit amount can be changed (category and month are immutable after creation)
- New limit must be positive (> 0)
- On save: budget updated, status recalculated immediately
- On cancel: no changes made

### Scenario 5.4: Delete a Budget

**As a** user
**I want to** delete a budget I no longer need
**So that** my budget list stays clean

**Acceptance Criteria:**
- Delete button on each budget row
- Confirmation dialog: "Are you sure you want to delete the budget for {category} in {month}?"
- On confirm: budget removed from database and list
- On cancel: no action
- Deleting a budget does NOT affect transactions or categories
- Scoped to current user only

### Scenario 5.5: View Budget Status on Dashboard

**As a** user
**I want to** see my budget health at a glance on the dashboard
**So that** I can quickly identify overspending

**Acceptance Criteria:**
- Dashboard shows a "Budget Status" widget/section
- Widget shows all budgets for the current month with mini progress bars
- Each progress bar: category name, spent/limit label, colored bar
  - **Green** (< 60%): On track
  - **Yellow** (60–80%): Caution
  - **Red** (80–100%): Warning
  - **Overage** (> 100%): Over budget — distinct styling (red with strikethrough or exclamation icon)
- Summary line: "X of Y budgets on track" or "Z budgets over limit"
- Click on any budget → navigates to budget detail page
- If no budgets: "No budgets set. Create budgets to track spending." with link
- Widget responds to dashboard date range filter (shows budgets for the selected month)

### Scenario 5.6: View Budget vs. Actual Comparison

**As a** user
**I want to** see a detailed comparison of budgeted vs. actual spending per category
**So that** I can analyze where I'm over or under budget

**Acceptance Criteria:**
- Dedicated page at `/Budgets/Comparison`
- Month selector (defaults to current month)
- Table showing per-category comparison:
  - Category name
  - Budget limit (or "No budget" if none set)
  - Actual spending in that category for the month
  - Difference (limit - actual): positive = under budget, negative = over
  - Status indicator (green/yellow/red/overage)
- Includes categories without budgets (shows actual spending with "No budget" in limit column)
- Summary row at bottom: Total budgeted, Total actual, Total difference
- Bar chart: horizontal bars showing budget limit vs. actual per category (Chart.js)
- Categories sorted: over budget first, then by percentage used descending

### Scenario 5.7: View Budget Detail

**As a** user
**I want to** see detailed information about a single budget
**So that** I can understand my spending progress in that category

**Acceptance Criteria:**
- Budget detail page at `/Budgets/{id}`
- Shows: Category name, Month, Limit, Current Spend, Remaining, Percentage Used
- Visual progress bar (large, prominent) with color coding
- List of transactions in this category for this month (reuses transaction list component)
- Edit limit button (inline or modal)
- Back to budget list link

---

## Functional Requirements

### FR-5.01: Domain Layer Additions

#### BudgetService

```csharp
public class BudgetService
{
    private readonly IBudgetRepository _budgetRepo;

    public BudgetService(IBudgetRepository budgetRepo)
    {
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
    }

    public async Task ValidateUniqueBudget(UserId userId, CategoryId categoryId, DateRange month)
    {
        var existing = await _budgetRepo.GetByUserAndCategoryAndMonthAsync(userId, categoryId, month);
        if (existing is not null)
            throw new DomainException(
                $"A budget for this category in {month.StartDate:MMMM yyyy} already exists.");
    }

    public BudgetStatusLevel CalculateStatusLevel(decimal percentageUsed)
    {
        return percentageUsed switch
        {
            > 1.0m => BudgetStatusLevel.Overage,
            > 0.8m => BudgetStatusLevel.Red,
            > 0.6m => BudgetStatusLevel.Yellow,
            _ => BudgetStatusLevel.Green
        };
    }
}
BudgetStatusLevel (Domain Enum)
csharp
public enum BudgetStatusLevel
{
    Green,    // < 60% used — on track
    Yellow,   // 60–80% used — caution
    Red,      // 80–100% used — warning
    Overage   // > 100% used — over budget
}
File Structure:

text
Domain/
├── Services/
│   ├── (existing from Phase 1/2)
│   ├── BudgetService.cs                # NEW
│   └── BudgetStatusLevel.cs            # NEW (enum)
FR-5.02: Application Layer — Budget Commands & Queries
text
Application/
├── Features/
│   └── Budgets/
│       ├── Commands/
│       │   ├── CreateBudgetCommand.cs
│       │   ├── CreateBudgetCommandHandler.cs
│       │   ├── UpdateBudgetCommand.cs
│       │   ├── UpdateBudgetCommandHandler.cs
│       │   ├── DeleteBudgetCommand.cs
│       │   └── DeleteBudgetCommandHandler.cs
│       ├── Queries/
│       │   ├── GetBudgetsQuery.cs
│       │   ├── GetBudgetsQueryHandler.cs
│       │   ├── GetBudgetByIdQuery.cs
│       │   ├── GetBudgetByIdQueryHandler.cs
│       │   ├── GetBudgetStatusQuery.cs
│       │   ├── GetBudgetStatusQueryHandler.cs
│       │   ├── GetBudgetVsActualQuery.cs
│       │   ├── GetBudgetVsActualQueryHandler.cs
│       │   ├── GetBudgetSummaryForDashboardQuery.cs
│       │   └── GetBudgetSummaryForDashboardQueryHandler.cs
│       └── DTOs/
│           ├── BudgetDto.cs
│           ├── BudgetStatusDto.cs
│           ├── BudgetVsActualDto.cs
│           └── BudgetDashboardSummaryDto.cs
CreateBudgetCommand
csharp
public record CreateBudgetCommand(
    Guid CategoryId,
    int Year,
    int Month,
    decimal LimitAmount,
    string Currency = "EUR"
) : IRequest<Guid>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Validate category exists and belongs to user (ICategoryRepository.GetByIdAsync)
3. Build DateRange for the month (1st to last day)
4. Create Money value object from LimitAmount + Currency
5. Validate uniqueness via BudgetService.ValidateUniqueBudget(userId, categoryId, month)
6. Create BudgetId (new Guid)
7. Create Budget entity (invariants enforced by constructor: limit > 0)
8. Persist via IBudgetRepository.AddAsync()
9. Return BudgetId.Value
UpdateBudgetCommand
csharp
public record UpdateBudgetCommand(
    Guid BudgetId,
    decimal NewLimitAmount,
    string Currency = "EUR"
) : IRequest<Unit>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Load Budget by Id; throw EntityNotFoundException if not found
3. Verify Budget.UserId matches current user (tenant isolation)
4. Create Money value object from NewLimitAmount + Currency
5. Call Budget.UpdateLimit(newLimit) (invariant enforced by entity: limit > 0)
6. Persist via IBudgetRepository.UpdateAsync()
DeleteBudgetCommand
csharp
public record DeleteBudgetCommand(
    Guid BudgetId
) : IRequest<Unit>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Load Budget by Id; throw EntityNotFoundException if not found
3. Verify Budget.UserId matches current user (tenant isolation)
4. Delete via IBudgetRepository.DeleteAsync()
GetBudgetsQuery
csharp
public record GetBudgetsQuery(
    int? Year = null,
    int? Month = null
) : IRequest<List<BudgetStatusDto>>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Load budgets for user (optionally filtered by month)
3. For each budget:
   a. Calculate current spend from transactions in the budget's category + month
      (ITransactionRepository.FindBySpecificationAsync with user + category + date range specs)
   b. Calculate percentage used (Budget.PercentageUsed)
   c. Calculate remaining (Budget.RemainingAmount)
   d. Determine status level (BudgetService.CalculateStatusLevel)
4. Map to List<BudgetStatusDto>
5. Sort by category name alphabetically
GetBudgetByIdQuery
csharp
public record GetBudgetByIdQuery(
    Guid BudgetId
) : IRequest<BudgetStatusDto>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Load Budget by Id; throw EntityNotFoundException if not found
3. Verify Budget.UserId matches current user
4. Calculate current spend (same as GetBudgetsQuery logic)
5. Map to BudgetStatusDto with full details
GetBudgetStatusQuery
csharp
public record GetBudgetStatusQuery(
    Guid BudgetId
) : IRequest<BudgetStatusDto>;
Handler Flow: Same as GetBudgetByIdQuery (alias for semantic clarity in different UI contexts).

GetBudgetVsActualQuery
csharp
public record GetBudgetVsActualQuery(
    int Year,
    int Month
) : IRequest<List<BudgetVsActualDto>>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Build DateRange for the requested month
3. Load all budgets for user in that month
4. Load all transactions for user in that month, grouped by category
5. Build comparison list:
   a. For each budget: category name, limit, actual spend, difference, status
   b. For categories with spending but no budget: include with limit = null
6. Sort: over budget first, then by percentage used descending
7. Calculate totals: total budgeted, total actual, total difference
8. Return List<BudgetVsActualDto>
GetBudgetSummaryForDashboardQuery
csharp
public record GetBudgetSummaryForDashboardQuery(
    int Year,
    int Month
) : IRequest<BudgetDashboardSummaryDto>;
Handler Flow:

text
1. Get UserId from IUserContext
2. Load all budgets for user in the requested month
3. For each budget: calculate current spend + status level
4. Aggregate:
   a. TotalBudgets: count of budgets
   b. OnTrackCount: budgets with status Green or Yellow
   c. WarningCount: budgets with status Red
   d. OverageCount: budgets with status Overage
   e. BudgetItems: list of mini status items (category, percentage, status level)
5. Return BudgetDashboardSummaryDto
FR-5.03: Application DTOs
BudgetDto
csharp
public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal LimitAmount,
    string Currency,
    DateTime MonthStart,
    DateTime MonthEnd,
    DateTime CreatedAt
);
BudgetStatusDto
csharp
public record BudgetStatusDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal LimitAmount,
    decimal CurrentSpend,
    decimal RemainingAmount,
    decimal PercentageUsed,
    BudgetStatusLevel StatusLevel,
    string Currency,
    DateTime MonthStart,
    DateTime MonthEnd
);
BudgetVsActualDto
csharp
public record BudgetVsActualDto(
    Guid? BudgetId,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal? BudgetLimit,
    decimal ActualSpend,
    decimal? Difference,
    decimal? PercentageUsed,
    BudgetStatusLevel? StatusLevel,
    string Currency,
    bool HasBudget
);
BudgetDashboardSummaryDto
csharp
public record BudgetDashboardSummaryDto(
    int TotalBudgets,
    int OnTrackCount,
    int WarningCount,
    int OverageCount,
    decimal TotalBudgeted,
    decimal TotalActualSpend,
    string Currency,
    List<BudgetDashboardItemDto> Items
);

public record BudgetDashboardItemDto(
    Guid BudgetId,
    string CategoryName,
    string? CategoryColor,
    decimal LimitAmount,
    decimal CurrentSpend,
    decimal PercentageUsed,
    BudgetStatusLevel StatusLevel
);
FR-5.04: Infrastructure — Budget Repository & Migration
text
Infrastructure/
├── Persistence/
│   ├── (existing repositories from Phase 3)
│   ├── SupabaseBudgetRepository.cs                # NEW
│   └── Migrations/
│       ├── (existing: 001–004 from Phase 1/3)
│       └── 005_CreateBudgetsTable.sql             # NEW
SupabaseBudgetRepository
csharp
public class SupabaseBudgetRepository : IBudgetRepository
{
    private readonly Supabase.Client _client;

    public SupabaseBudgetRepository(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<Budget?> GetByIdAsync(BudgetId id)
    {
        // Query Supabase: SELECT * FROM budgets WHERE id = @id
        // Map to Budget entity; return null if not found
    }

    public async Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId)
    {
        // Query Supabase: SELECT * FROM budgets WHERE user_id = @userId
        // Map to Budget entities
    }

    public async Task<Budget?> GetByUserAndCategoryAndMonthAsync(
        UserId userId, CategoryId categoryId, DateRange month)
    {
        // Query: WHERE user_id = @userId AND category_id = @categoryId
        //        AND month_start = @month.StartDate AND month_end = @month.EndDate
        // Return null if not found (used for uniqueness check)
    }

    public async Task AddAsync(Budget budget)
    {
        // Map Budget entity to Supabase row model
        // INSERT into budgets table
    }

    public async Task UpdateAsync(Budget budget)
    {
        // Map Budget entity to Supabase row model
        // UPDATE budgets SET limit_amount = @limit, updated_at = @updatedAt WHERE id = @id
    }

    public async Task DeleteAsync(BudgetId id)
    {
        // DELETE FROM budgets WHERE id = @id
    }
}
Entity ↔ Supabase Mapping:

Entity Property	Supabase Column	Type	Notes
Id.Value	id	UUID	Primary key
UserId.Value	user_id	UUID	FK to users.id
CategoryId.Value	category_id	UUID	FK to categories.id
Month.StartDate	month_start	TIMESTAMPTZ	First day of budget month
Month.EndDate	month_end	TIMESTAMPTZ	Last day of budget month
Limit.Amount	limit_amount	DECIMAL(15,2)	Budget limit
Limit.Currency	currency	VARCHAR(3)	Currency code (default EUR)
CreatedAt	created_at	TIMESTAMPTZ	Auto-set
UpdatedAt	updated_at	TIMESTAMPTZ	Nullable; set on update
005_CreateBudgetsTable.sql
sql
-- Migration: 005_CreateBudgetsTable.sql
-- Purpose: Monthly budgets per category per user

CREATE TABLE IF NOT EXISTS public.budgets (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES public.categories(id) ON DELETE CASCADE,
    month_start TIMESTAMPTZ NOT NULL,
    month_end TIMESTAMPTZ NOT NULL,
    limit_amount DECIMAL(15,2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    CONSTRAINT uq_budget_user_category_month UNIQUE (user_id, category_id, month_start)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_budgets_user ON public.budgets(user_id);
CREATE INDEX IF NOT EXISTS idx_budgets_user_month ON public.budgets(user_id, month_start);
CREATE INDEX IF NOT EXISTS idx_budgets_user_category ON public.budgets(user_id, category_id);

-- Row Level Security
ALTER TABLE public.budgets ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own budgets"
    ON public.budgets FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own budgets"
    ON public.budgets FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own budgets"
    ON public.budgets FOR UPDATE
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own budgets"
    ON public.budgets FOR DELETE
    USING (auth.uid() = user_id);
FR-5.05: Frontend Pages
text
Frontend/
├── Pages/
│   ├── Budgets/
│   │   ├── Index.cshtml               # Budget list with status indicators
│   │   ├── Index.cshtml.cs
│   │   ├── Create.cshtml              # Create budget form
│   │   ├── Create.cshtml.cs
│   │   ├── Detail.cshtml              # Single budget detail with progress bar
│   │   ├── Detail.cshtml.cs
│   │   ├── Comparison.cshtml          # Budget vs actual comparison
│   │   └── Comparison.cshtml.cs
│   ├── Dashboard.cshtml               # UPDATED: add budget status widget
│   └── Dashboard.cshtml.cs
├── Shared/
│   ├── _Layout.cshtml                 # UPDATED: budget nav links
│   └── Components/
│       ├── _DateRangeFilter.cshtml    # (from Phase 4 — reused)
│       ├── _BudgetProgressBar.cshtml  # NEW: reusable progress bar partial
│       └── _BudgetStatusBadge.cshtml  # NEW: reusable status badge partial
Budget List Page (/Budgets)
csharp
[Authorize]
public class BudgetListModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetStatusDto> Budgets { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [BindProperty(SupportsGet = true)]
    public int Month { get; set; } = DateTime.UtcNow.Month;

    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    public async Task OnGetAsync()
    {
        Budgets = await _mediator.Send(new GetBudgetsQuery(Year, Month));
    }
}
View Requirements:

Month selector (previous/next arrows + dropdown)
Budget table:
Category name (with color swatch)
Limit amount (formatted with currency)
Current spend (formatted)
Remaining (formatted; negative in red)
Progress bar (colored by status level)
Percentage label (e.g., "75%")
Actions: Edit (pencil icon), Delete (trash icon with confirmation)
Create button: "Add Budget" → link to /Budgets/Create
Empty state: "No budgets for {month}. Create one to start tracking."
Responsive: table on desktop, cards on mobile
Create Budget Page (/Budgets/Create)
csharp
[Authorize]
public class CreateBudgetModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public CreateBudgetInputModel Input { get; set; } = new();

    public List<CategoryDto> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());
        Input.Year = DateTime.UtcNow.Year;
        Input.Month = DateTime.UtcNow.Month;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _mediator.Send(new CreateBudgetCommand(
                Input.CategoryId,
                Input.Year,
                Input.Month,
                Input.LimitAmount));

            return RedirectToPage("/Budgets/Index",
                new { Year = Input.Year, Month = Input.Month });
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}

public class CreateBudgetInputModel
{
    public Guid CategoryId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal LimitAmount { get; set; }
}
View Requirements:

Form fields:
Category dropdown (all user categories)
Month picker (month + year)
Limit amount (number input with currency label "EUR")
Submit button: "Create Budget"
Cancel link → back to budget list
Validation error display area
Tailwind-styled card layout
Budget Detail Page (/Budgets/{id})
csharp
[Authorize]
public class BudgetDetailModel : PageModel
{
    private readonly IMediator _mediator;

    public BudgetStatusDto Budget { get; set; } = default!;
    public List<TransactionDto> Transactions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Budget = await _mediator.Send(new GetBudgetByIdQuery(id));

            // Load transactions for this category in this month
            Transactions = (await _mediator.Send(new SearchTransactionsQuery(
                CategoryId: Budget.CategoryId,
                FromDate: Budget.MonthStart,
                ToDate: Budget.MonthEnd))).Items;

            return Page();
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }
}
View Requirements:

Large progress bar at top with percentage and status badge
Budget details card: Category, Month, Limit, Current Spend, Remaining
Transaction list for this category + month (table format)
Edit limit button (opens inline edit or modal)
Back to budget list link
Budget vs. Actual Comparison Page (/Budgets/Comparison)
csharp
[Authorize]
public class BudgetComparisonModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetVsActualDto> Comparison { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [BindProperty(SupportsGet = true)]
    public int Month { get; set; } = DateTime.UtcNow.Month;

    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public decimal TotalBudgeted { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalDifference { get; set; }

    public async Task OnGetAsync()
    {
        Comparison = await _mediator.Send(
            new GetBudgetVsActualQuery(Year, Month));

        TotalBudgeted = Comparison.Where(c => c.HasBudget).Sum(c => c.BudgetLimit ?? 0);
        TotalActual = Comparison.Sum(c => c.ActualSpend);
        TotalDifference = TotalBudgeted - TotalActual;
    }
}
View Requirements:

Month selector (same as budget list page)
Comparison table:
Category name
Budget limit (or "No budget" badge)
Actual spending
Difference (colored: green if positive/under, red if negative/over)
Status badge (green/yellow/red/overage)
Summary row: Total Budgeted | Total Actual | Total Difference
Horizontal bar chart (Chart.js): budget limit bars vs. actual spend bars per category
Responsive layout
Dashboard Budget Widget
csharp
// Added to DashboardModel.OnGetAsync()
public BudgetDashboardSummaryDto BudgetSummary { get; set; } = default!;

// In OnGetAsync:
BudgetSummary = await _mediator.Send(
    new GetBudgetSummaryForDashboardQuery(FromDate.Year, FromDate.Month));
Widget View Requirements:

Section heading: "Budget Status" with month label
If no budgets: "No budgets set. [Create one →]"
If budgets exist:
Summary line: "X of Y budgets on track" / "Z over budget"
Mini progress bars per budget:
Category name (left), Percentage (right)
Colored bar (green/yellow/red)
Click → navigates to /Budgets/{id}
"View all budgets →" link to /Budgets
Reusable Components
_BudgetProgressBar.cshtml:

html
@model BudgetProgressBarViewModel
<div class="w-full bg-gray-200 rounded-full h-4 overflow-hidden">
    <div class="h-4 rounded-full transition-all duration-300"
         style="width: @(Math.Min(Model.PercentageUsed * 100, 100))%"
         class="@Model.StatusLevel switch {
             BudgetStatusLevel.Green => 'bg-green-500',
             BudgetStatusLevel.Yellow => 'bg-yellow-500',
             BudgetStatusLevel.Red => 'bg-red-500',
             BudgetStatusLevel.Overage => 'bg-red-700',
         }">
    </div>
</div>
@if (Model.StatusLevel == BudgetStatusLevel.Overage)
{
    <span class="text-red-700 text-sm font-semibold">
        ⚠️ Over budget by @((Model.PercentageUsed - 1) * 100):F0)%
    </span>
}
_BudgetStatusBadge.cshtml:

html
@model BudgetStatusLevel
<span class="px-2 py-1 rounded-full text-xs font-semibold
    @Model switch {
        BudgetStatusLevel.Green => 'bg-green-100 text-green-800',
        BudgetStatusLevel.Yellow => 'bg-yellow-100 text-yellow-800',
        BudgetStatusLevel.Red => 'bg-red-100 text-red-800',
        BudgetStatusLevel.Overage => 'bg-red-200 text-red-900 border border-red-500',
    }">
    @Model switch {
        BudgetStatusLevel.Green => "On Track",
        BudgetStatusLevel.Yellow => "Caution",
        BudgetStatusLevel.Red => "Warning",
        BudgetStatusLevel.Overage => "Over Budget",
    }
</span>
FR-5.06: Updated Navigation
Authenticated Navigation Items (Updated):

Label	Route	Icon	Notes
Dashboard	/Dashboard	📊	Default landing page
Transactions	/Transactions	💳	Transaction list
Search	/Transactions/Search	🔍	Multi-filter search
Upload PDF	/Transactions/Upload	📄	PDF import
Categories	/Categories	🏷️	Category management
Budgets	/Budgets	💰	NEW: Budget management
Comparison	/Budgets/Comparison	📊	NEW: Budget vs actual
Logout	(POST action)	🚪	Clears session
FR-5.07: Infrastructure DI Updates
csharp
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... (existing from Phase 0/1/3)

    // Budget repository (NEW in Phase 5)
    services.AddScoped<IBudgetRepository, SupabaseBudgetRepository>();

    // Budget domain service (NEW in Phase 5)
    services.AddScoped<BudgetService>();

    return services;
}
text

Ahora va la **Parte 2/2** — pégalo justo después:

```markdown

---

## Architecture Notes

### Budget Status Calculation Flow
┌─────────────────────┐ ┌────────────────────────────────────┐ ┌──────────────────┐
│ Dashboard / Budget │ │ Application Layer │ │ Infrastructure │
│ Page (Frontend) │ │ │ │ │
└──────────┬───────────┘ └────────────────────────────────────┘ └──────────────────┘
│
│ _mediator.Send(GetBudgetStatusQuery(budgetId))
▼
┌──────────────────────────────────────────────────────┐
│ GetBudgetStatusQueryHandler │
│ │
│ 1. Get UserId from IUserContext │
│ 2. Load Budget ──────────────────────────────────────┼──► IBudgetRepository
│ │ .GetByIdAsync()
│ 3. Build specs: user + category + date range │
│ 4. Load transactions ────────────────────────────────┼──► ITransactionRepository
│ │ .FindBySpecificationAsync()
│ 5. Sum expenses → currentSpend (Money) │
│ 6. Budget.PercentageUsed(currentSpend) │ ← Domain logic
│ 7. Budget.RemainingAmount(currentSpend) │ ← Domain logic
│ 8. Budget.IsOverBudget(currentSpend) │ ← Domain logic
│ 9. BudgetService.CalculateStatusLevel(percentage) │ ← Domain service
│ 10. Map to BudgetStatusDto │
└──────────────────────────────────────────────────────┘
│
▼
┌──────────────────┐
│ Render progress │
│ bar + badge │
│ (Frontend) │
└──────────────────┘

text

### NuGet Packages (Phase 5 Additions)

| Project                          | New Packages | Notes                             |
|----------------------------------|--------------|-----------------------------------|
| `SauronSheet.Domain`            | **None**     | Still zero dependencies           |
| `SauronSheet.Application`       | None         | No new packages needed            |
| `SauronSheet.Infrastructure`    | None         | Existing Supabase client used     |
| `SauronSheet.Frontend`          | None         | Chart.js already in layout        |
| `SauronSheet.Domain.Tests`      | None         | Existing xUnit + Moq             |
| `SauronSheet.Application.Tests` | None         | Existing xUnit + Moq             |

### Layer Dependencies (Phase 5 Additions)

| Layer          | New Dependencies                                                                |
|----------------|---------------------------------------------------------------------------------|
| Domain         | None — adds `BudgetService` + `BudgetStatusLevel` enum                         |
| Application    | Domain (BudgetService, Budget entity, IBudgetRepository, ITransactionRepository)|
| Infrastructure | Domain (implements IBudgetRepository)                                           |
| Frontend       | Application (MediatR budget commands/queries)                                   |

---

## Test Specifications

### Budget Command Tests
TEST T-5.01: CreateBudget_ValidInput_ReturnsBudgetId
GIVEN valid categoryId (exists, belongs to user), year = 2026, month = 3, limit = 500
AND IBudgetRepository.GetByUserAndCategoryAndMonthAsync returns null (no duplicate)
AND ICategoryRepository.GetByIdAsync returns the category
WHEN CreateBudgetCommandHandler handles the command
THEN returns a non-empty Guid (BudgetId)
AND IBudgetRepository.AddAsync called once

TEST T-5.02: CreateBudget_DuplicateBudget_ThrowsDomainException
GIVEN valid categoryId, year = 2026, month = 3
AND IBudgetRepository.GetByUserAndCategoryAndMonthAsync returns an existing budget
WHEN CreateBudgetCommandHandler handles the command
THEN throws DomainException with message containing "already exists"
AND IBudgetRepository.AddAsync NOT called

TEST T-5.03: CreateBudget_InvalidCategory_ThrowsEntityNotFound
GIVEN categoryId that does not exist
AND ICategoryRepository.GetByIdAsync returns null
WHEN CreateBudgetCommandHandler handles the command
THEN throws EntityNotFoundException

TEST T-5.04: CreateBudget_ZeroLimit_ThrowsDomainException
GIVEN valid categoryId, year, month, limit = 0
WHEN CreateBudgetCommandHandler handles the command
THEN throws DomainException with message containing "must be positive"

TEST T-5.05: CreateBudget_NegativeLimit_ThrowsDomainException
GIVEN valid categoryId, year, month, limit = -100
WHEN CreateBudgetCommandHandler handles the command
THEN throws DomainException with message containing "must be positive"

TEST T-5.06: CreateBudget_CategoryBelongsToDifferentUser_ThrowsException
GIVEN categoryId that belongs to a different user
AND ICategoryRepository.GetByIdAsync returns category with different UserId
WHEN CreateBudgetCommandHandler handles the command
THEN throws EntityNotFoundException (tenant isolation)

TEST T-5.07: UpdateBudget_ValidInput_UpdatesLimit
GIVEN an existing budget owned by current user with limit = 500
AND new limit = 800
WHEN UpdateBudgetCommandHandler handles the command
THEN IBudgetRepository.UpdateAsync called once
AND the budget's Limit is Money(800, "EUR")

TEST T-5.08: UpdateBudget_NonExistent_ThrowsEntityNotFound
GIVEN a BudgetId that does not exist
AND IBudgetRepository.GetByIdAsync returns null
WHEN UpdateBudgetCommandHandler handles the command
THEN throws EntityNotFoundException

TEST T-5.09: UpdateBudget_WrongUser_ThrowsException
GIVEN a budget owned by a different user
WHEN UpdateBudgetCommandHandler handles the command
THEN throws EntityNotFoundException (tenant isolation)

TEST T-5.10: UpdateBudget_ZeroLimit_ThrowsDomainException
GIVEN existing budget, new limit = 0
WHEN UpdateBudgetCommandHandler handles the command
THEN throws DomainException with message containing "must be positive"

TEST T-5.11: DeleteBudget_ValidInput_RemovesBudget
GIVEN an existing budget owned by current user
WHEN DeleteBudgetCommandHandler handles the command
THEN IBudgetRepository.DeleteAsync called once

TEST T-5.12: DeleteBudget_NonExistent_ThrowsEntityNotFound
GIVEN a BudgetId that does not exist
WHEN DeleteBudgetCommandHandler handles the command
THEN throws EntityNotFoundException

TEST T-5.13: DeleteBudget_WrongUser_ThrowsException
GIVEN a budget owned by a different user
WHEN DeleteBudgetCommandHandler handles the command
THEN throws EntityNotFoundException (tenant isolation)

text

### Budget Query Tests
TEST T-5.14: GetBudgets_ReturnsOnlyUserBudgets
GIVEN 3 budgets for user A, 2 budgets for user B
AND current user is user A
WHEN GetBudgetsQueryHandler handles the query
THEN returns 3 BudgetStatusDto items

TEST T-5.15: GetBudgets_FilteredByMonth_ReturnsMonthOnly
GIVEN budgets for Jan, Feb, Mar 2026
AND query with Year = 2026, Month = 2
WHEN GetBudgetsQueryHandler handles the query
THEN returns only February budgets

TEST T-5.16: GetBudgets_CalculatesCurrentSpendCorrectly
GIVEN budget for "Groceries" in March with limit = 500
AND 3 grocery transactions in March: -€100, -€150, -€75 (total = €325)
WHEN GetBudgetsQueryHandler handles the query
THEN BudgetStatusDto.CurrentSpend == 325
AND PercentageUsed == 0.65
AND RemainingAmount == 175
AND StatusLevel == Yellow

TEST T-5.17: GetBudgets_NoBudgets_ReturnsEmptyList
GIVEN no budgets for current user
WHEN GetBudgetsQueryHandler handles the query
THEN returns empty list

TEST T-5.18: GetBudgets_OverBudget_StatusIsOverage
GIVEN budget for "Transport" with limit = 100
AND transport transactions totaling €150
WHEN GetBudgetsQueryHandler handles the query
THEN StatusLevel == Overage
AND PercentageUsed == 1.5
AND RemainingAmount == -50

TEST T-5.19: GetBudgetById_Exists_ReturnsDetailedStatus
GIVEN an existing budget owned by current user
WHEN GetBudgetByIdQueryHandler handles the query
THEN returns BudgetStatusDto with all fields populated

TEST T-5.20: GetBudgetById_WrongUser_ThrowsException
GIVEN a budget owned by a different user
WHEN GetBudgetByIdQueryHandler handles the query
THEN throws EntityNotFoundException

TEST T-5.21: GetBudgetById_NonExistent_ThrowsException
GIVEN a BudgetId that does not exist
WHEN GetBudgetByIdQueryHandler handles the query
THEN throws EntityNotFoundException

TEST T-5.22: GetBudgetVsActual_ReturnsComparisonForMonth
GIVEN 3 budgets (Groceries €500, Transport €200, Utilities €150) for March
AND transactions: Groceries €400, Transport €250, Utilities €100, Entertainment €50 (no budget)
WHEN GetBudgetVsActualQueryHandler handles the query for March
THEN returns 4 items (3 with budgets + 1 without)
AND Groceries: limit=500, actual=400, difference=100 (under), Green
AND Transport: limit=200, actual=250, difference=-50 (over), Overage
AND Utilities: limit=150, actual=100, difference=50 (under), Green
AND Entertainment: HasBudget=false, actual=50

TEST T-5.23: GetBudgetVsActual_NoTransactions_ShowsZeroActual
GIVEN budgets for March but no transactions in March
WHEN GetBudgetVsActualQueryHandler handles the query
THEN all items have ActualSpend = 0, StatusLevel = Green

TEST T-5.24: GetBudgetVsActual_NoBudgets_ShowsUnbudgetedSpending
GIVEN no budgets for March but transactions exist in categories
WHEN GetBudgetVsActualQueryHandler handles the query
THEN returns items with HasBudget = false for each category with spending

TEST T-5.25: GetBudgetVsActual_SortedOverBudgetFirst
GIVEN budgets with mixed status (some over, some under)
WHEN GetBudgetVsActualQueryHandler handles the query
THEN over-budget items appear before under-budget items

text

### Dashboard Budget Summary Tests
TEST T-5.26: GetBudgetSummaryForDashboard_CalculatesAggregates
GIVEN 5 budgets: 2 Green, 1 Yellow, 1 Red, 1 Overage
WHEN GetBudgetSummaryForDashboardQueryHandler handles the query
THEN TotalBudgets == 5
AND OnTrackCount == 3 (Green + Yellow)
AND WarningCount == 1 (Red)
AND OverageCount == 1
AND Items contains 5 BudgetDashboardItemDto entries

TEST T-5.27: GetBudgetSummaryForDashboard_NoBudgets_ReturnsZeros
GIVEN no budgets for current user in the requested month
WHEN GetBudgetSummaryForDashboardQueryHandler handles the query
THEN TotalBudgets == 0, OnTrackCount == 0, WarningCount == 0, OverageCount == 0
AND Items is empty list

TEST T-5.28: GetBudgetSummaryForDashboard_AllOnTrack
GIVEN 3 budgets all under 60% spending
WHEN GetBudgetSummaryForDashboardQueryHandler handles the query
THEN OnTrackCount == 3, WarningCount == 0, OverageCount == 0

text

### Domain Service Tests
TEST T-5.29: BudgetService_ValidateUniqueBudget_Duplicate_Throws
GIVEN IBudgetRepository.GetByUserAndCategoryAndMonthAsync returns existing budget
WHEN ValidateUniqueBudget is called
THEN throws DomainException with message containing "already exists"

TEST T-5.30: BudgetService_ValidateUniqueBudget_NoDuplicate_NoException
GIVEN IBudgetRepository.GetByUserAndCategoryAndMonthAsync returns null
WHEN ValidateUniqueBudget is called
THEN no exception is thrown

TEST T-5.31: BudgetService_CalculateStatusLevel_Under60_Green
GIVEN percentageUsed = 0.45
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Green

TEST T-5.32: BudgetService_CalculateStatusLevel_60To80_Yellow
GIVEN percentageUsed = 0.70
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Yellow

TEST T-5.33: BudgetService_CalculateStatusLevel_80To100_Red
GIVEN percentageUsed = 0.90
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Red

TEST T-5.34: BudgetService_CalculateStatusLevel_Over100_Overage
GIVEN percentageUsed = 1.25
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Overage

TEST T-5.35: BudgetService_CalculateStatusLevel_Exactly60_Yellow
GIVEN percentageUsed = 0.60
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Green (threshold is > 0.6, not >=)

TEST T-5.36: BudgetService_CalculateStatusLevel_Exactly80_Yellow
GIVEN percentageUsed = 0.80
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Yellow (threshold is > 0.8, not >=)

TEST T-5.37: BudgetService_CalculateStatusLevel_Exactly100_Red
GIVEN percentageUsed = 1.0
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Red (threshold is > 1.0 for Overage)

TEST T-5.38: BudgetService_CalculateStatusLevel_Zero_Green
GIVEN percentageUsed = 0.0
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Green

text

---

## Test Summary

| Test ID | Test Name                                                         | Category    | Area                   |
|---------|-------------------------------------------------------------------|-------------|------------------------|
| T-5.01  | CreateBudget_ValidInput_ReturnsBudgetId                           | Application | Budget Commands        |
| T-5.02  | CreateBudget_DuplicateBudget_ThrowsDomainException                | Application | Budget Commands        |
| T-5.03  | CreateBudget_InvalidCategory_ThrowsEntityNotFound                 | Application | Budget Commands        |
| T-5.04  | CreateBudget_ZeroLimit_ThrowsDomainException                      | Application | Budget Commands        |
| T-5.05  | CreateBudget_NegativeLimit_ThrowsDomainException                  | Application | Budget Commands        |
| T-5.06  | CreateBudget_CategoryBelongsToDifferentUser_ThrowsException       | Application | Budget Commands        |
| T-5.07  | UpdateBudget_ValidInput_UpdatesLimit                              | Application | Budget Commands        |
| T-5.08  | UpdateBudget_NonExistent_ThrowsEntityNotFound                     | Application | Budget Commands        |
| T-5.09  | UpdateBudget_WrongUser_ThrowsException                            | Application | Budget Commands        |
| T-5.10  | UpdateBudget_ZeroLimit_ThrowsDomainException                      | Application | Budget Commands        |
| T-5.11  | DeleteBudget_ValidInput_RemovesBudget                             | Application | Budget Commands        |
| T-5.12  | DeleteBudget_NonExistent_ThrowsEntityNotFound                     | Application | Budget Commands        |
| T-5.13  | DeleteBudget_WrongUser_ThrowsException                            | Application | Budget Commands        |
| T-5.14  | GetBudgets_ReturnsOnlyUserBudgets                                 | Application | Budget Queries         |
| T-5.15  | GetBudgets_FilteredByMonth_ReturnsMonthOnly                       | Application | Budget Queries         |
| T-5.16  | GetBudgets_CalculatesCurrentSpendCorrectly                        | Application | Budget Queries         |
| T-5.17  | GetBudgets_NoBudgets_ReturnsEmptyList                             | Application | Budget Queries         |
| T-5.18  | GetBudgets_OverBudget_StatusIsOverage                             | Application | Budget Queries         |
| T-5.19  | GetBudgetById_Exists_ReturnsDetailedStatus                        | Application | Budget Queries         |
| T-5.20  | GetBudgetById_WrongUser_ThrowsException                           | Application | Budget Queries         |
| T-5.21  | GetBudgetById_NonExistent_ThrowsException                         | Application | Budget Queries         |
| T-5.22  | GetBudgetVsActual_ReturnsComparisonForMonth                       | Application | Budget vs Actual       |
| T-5.23  | GetBudgetVsActual_NoTransactions_ShowsZeroActual                  | Application | Budget vs Actual       |
| T-5.24  | GetBudgetVsActual_NoBudgets_ShowsUnbudgetedSpending               | Application | Budget vs Actual       |
| T-5.25  | GetBudgetVsActual_SortedOverBudgetFirst                           | Application | Budget vs Actual       |
| T-5.26  | GetBudgetSummaryForDashboard_CalculatesAggregates                 | Application | Dashboard Summary      |
| T-5.27  | GetBudgetSummaryForDashboard_NoBudgets_ReturnsZeros               | Application | Dashboard Summary      |
| T-5.28  | GetBudgetSummaryForDashboard_AllOnTrack                           | Application | Dashboard Summary      |
| T-5.29  | BudgetService_ValidateUniqueBudget_Duplicate_Throws               | Domain      | BudgetService          |
| T-5.30  | BudgetService_ValidateUniqueBudget_NoDuplicate_NoException        | Domain      | BudgetService          |
| T-5.31  | BudgetService_CalculateStatusLevel_Under60_Green                  | Domain      | BudgetService          |
| T-5.32  | BudgetService_CalculateStatusLevel_60To80_Yellow                  | Domain      | BudgetService          |
| T-5.33  | BudgetService_CalculateStatusLevel_80To100_Red                    | Domain      | BudgetService          |
| T-5.34  | BudgetService_CalculateStatusLevel_Over100_Overage                | Domain      | BudgetService          |
| T-5.35  | BudgetService_CalculateStatusLevel_Exactly60_Yellow               | Domain      | BudgetService          |
| T-5.36  | BudgetService_CalculateStatusLevel_Exactly80_Yellow               | Domain      | BudgetService          |
| T-5.37  | BudgetService_CalculateStatusLevel_Exactly100_Red                 | Domain      | BudgetService          |
| T-5.38  | BudgetService_CalculateStatusLevel_Zero_Green                     | Domain      | BudgetService          |

**Total: 38 tests (28 Application + 10 Domain)**

**Tests by Area:**

| Area               | Test Count | Test IDs                     |
|--------------------|------------|------------------------------|
| Budget Commands    | 13         | T-5.01–T-5.13               |
| Budget Queries     | 8          | T-5.14–T-5.21               |
| Budget vs Actual   | 4          | T-5.22–T-5.25               |
| Dashboard Summary  | 3          | T-5.26–T-5.28               |
| BudgetService      | 10         | T-5.29–T-5.38               |

---

## Deliverables

| #      | Deliverable                                                          | Layer          | Acceptance                                                            |
|--------|----------------------------------------------------------------------|----------------|-----------------------------------------------------------------------|
| D-5.01 | `CreateBudgetCommand` + handler                                      | Application    | Tests T-5.01–T-5.06 pass                                             |
| D-5.02 | `UpdateBudgetCommand` + handler                                      | Application    | Tests T-5.07–T-5.10 pass                                             |
| D-5.03 | `DeleteBudgetCommand` + handler                                      | Application    | Tests T-5.11–T-5.13 pass                                             |
| D-5.04 | `GetBudgetsQuery` + handler                                          | Application    | Tests T-5.14–T-5.18 pass                                             |
| D-5.05 | `GetBudgetByIdQuery` + handler                                       | Application    | Tests T-5.19–T-5.21 pass                                             |
| D-5.06 | `GetBudgetVsActualQuery` + handler                                   | Application    | Tests T-5.22–T-5.25 pass                                             |
| D-5.07 | `GetBudgetSummaryForDashboardQuery` + handler                        | Application    | Tests T-5.26–T-5.28 pass                                             |
| D-5.08 | Budget DTOs (BudgetDto, BudgetStatusDto, BudgetVsActualDto, BudgetDashboardSummaryDto) | Application | Compile; used by handlers and frontend |
| D-5.09 | `BudgetStatusLevel` enum                                             | Domain         | Used by BudgetService and DTOs                                        |
| D-5.10 | `BudgetService` domain service                                       | Domain         | Tests T-5.29–T-5.38 pass                                             |
| D-5.11 | `SupabaseBudgetRepository`                                           | Infrastructure | Implements all `IBudgetRepository` methods                            |
| D-5.12 | Database migration: `005_CreateBudgetsTable.sql`                     | Infrastructure | Table + unique constraint + indexes + RLS policies applied            |
| D-5.13 | Budget list page (`/Budgets`)                                        | Frontend       | Lists budgets with status, edit/delete actions                        |
| D-5.14 | Create budget page (`/Budgets/Create`)                               | Frontend       | Form → validation → redirect                                         |
| D-5.15 | Budget detail page (`/Budgets/{id}`)                                 | Frontend       | Progress bar + transactions in category                               |
| D-5.16 | Budget vs. actual comparison page (`/Budgets/Comparison`)            | Frontend       | Table + bar chart + month selector                                    |
| D-5.17 | Dashboard budget status widget                                       | Frontend       | Mini progress bars + summary line on dashboard                        |
| D-5.18 | `_BudgetProgressBar.cshtml` reusable component                       | Frontend       | Color-coded progress bar used across pages                            |
| D-5.19 | `_BudgetStatusBadge.cshtml` reusable component                       | Frontend       | Status badge (Green/Yellow/Red/Overage) used across pages             |
| D-5.20 | Updated `_Layout.cshtml` navigation                                  | Frontend       | Budgets + Comparison links added                                      |
| D-5.21 | Updated Infrastructure `DependencyInjection.cs`                      | Infrastructure | BudgetRepository + BudgetService registered                          |
| D-5.22 | Domain.Tests for BudgetService (10 tests)                            | Tests          | `dotnet test --filter Category=Domain` all green                      |
| D-5.22 | Domain.Tests for BudgetService (10 tests)                            | Tests          | `dotnet test --filter Category=Domain` all green                      |
| D-5.23 | Application.Tests for budget handlers (28 tests)                     | Tests          | `dotnet test --filter Category=Application` all green                 |

---

## Success Criteria

|
#
|
 Criterion                                                                              
|
 Metric                                                                   
|
|
--------
|
----------------------------------------------------------------------------------------
|
--------------------------------------------------------------------------
|
|
 SC-5.1 
|
 User can create a budget for a category and month                                      
|
 E2E: Create form → submit → appears in budget list                       
|
|
 SC-5.2 
|
 Duplicate budget creation is rejected with descriptive error                            
|
 Same category + month → "already exists" error displayed                 
|
|
 SC-5.3 
|
 User can update a budget's spending limit                                              
|
 Edit limit → save → new limit shown in list                             
|
|
 SC-5.4 
|
 User can delete a budget                                                               
|
 Delete → confirm → removed from list; transactions unaffected            
|
|
 SC-5.5 
|
 Budget list shows correct current spend and status                                     
|
 Import transactions → budget list reflects actual spending               
|
|
 SC-5.6 
|
 Dashboard budget widget shows color-coded status indicators                            
|
 Green/Yellow/Red/Overage badges correctly reflect spending vs. limit     
|
|
 SC-5.7 
|
 Budget vs. actual comparison page shows all categories                                 
|
 Categories with and without budgets both visible                         
|
|
 SC-5.8 
|
 Budget detail page shows transactions in that category for the month                   
|
 Click budget → see progress bar + transaction list                       
|
|
 SC-5.9 
|
 Over-budget state is visually distinct                                                 
|
 Overage styling (red with warning icon) clearly visible                  
|
|
 SC-5.10
|
 Status thresholds are correct (green < 60%, yellow 60-80%, red 80-100%, overage > 100%)
|
 Verified via domain service tests T-5.31–T-5.38                         
|
|
 SC-5.11
|
 All data scoped to current user (tenant isolation)                                     
|
 User A cannot see or modify User B's budgets                            
|
|
 SC-5.12
|
 Budget deletion does not affect transactions                                           
|
 Delete budget → transactions still exist with same categories            
|
|
 SC-5.13
|
 All Phase 5 tests pass (38 tests)                                                      
|
`dotnet test`
 all green                                                  
|
|
 SC-5.14
|
 All prior phase tests still pass (no regressions)                                      
|
`dotnet test`
 → Phase 0 + 1 + 2 + 3 + 4 + 5 all green                  
|
|
 SC-5.15
|
 Application layer test coverage ≥ 70%                                                  
|
 coverlet report on Application project                                   
|
|
 SC-5.16
|
 Domain test coverage ≥ 80%                                                             
|
 coverlet report on Domain project (cumulative)                           
|
|
 SC-5.17
|
 RLS policies verified on budgets table                                                 
|
 Two users cannot see each other's budgets via Supabase                   
|
|
 SC-5.18
|
 Unique constraint prevents duplicate budgets at DB level                               
|
 Direct Supabase insert of duplicate → constraint violation               
|

---

## Assumptions

1. **Phases 0–4 are fully implemented and tested.** All foundation, auth, domain model, transaction CRUD, category management, PDF import, analytics dashboard, and search are stable.
2. **Budget entity from Phase 2 is fully implemented** with `IsOverBudget`, `PercentageUsed`, `RemainingAmount`, and `UpdateLimit` methods tested and working.
3. **`IBudgetRepository` interface from Phase 2 is defined** with all required methods (`GetByIdAsync`, `GetByUserIdAsync`, `GetByUserAndCategoryAndMonthAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`).
4. **Current spend is calculated from transactions at query time** (not cached). For MVP, this is acceptable given the 1000-row MaxResults default per specification query.
5. **Budget month boundaries** are first day of month (00:00:00 UTC) to last day of month (23:59:59 UTC). `DateRange` value object from Phase 2 handles this.
6. **Currency is EUR for all budgets** (consistent with Money value object default). Multi-currency budgets deferred to post-MVP.
7. **Chart.js is already available** in the layout (added in Phase 4). The budget vs. actual page reuses the same Chart.js CDN.
8. **Alpine.js is available** for interactive components (confirmation dialogs, inline edit) — added in Phase 3.
9. **`BudgetService` is a domain service** registered in the DI container and injected into Application handlers. It depends only on `IBudgetRepository` (domain interface).
10. **The `BudgetStatusLevel` enum lives in the Domain layer** because it represents a domain concept (budget health). It is used by both the domain service and application DTOs.
11. **Budget deletion has no cascading effects.** Deleting a budget does not change any transactions. Budgets are purely a tracking/limit overlay.
12. **The "Budgets/Comparison" page includes categories without budgets** to give a complete picture of spending. These rows show actual spending with "No budget" in the limit column.

---

## Risks & Mitigations

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
------------------------------------------------------------------------------
|
--------
|
-------------
|
-----------------------------------------------------------------------------------------------------
|
|
 R-5.1 
|
 Current spend calculation slow for users with many transactions              
|
 Medium 
|
 Medium      
|
 Specification limits to 1000 rows; post-MVP: cache spend or use Supabase aggregate RPC              
|
|
 R-5.2 
|
 Budget status widget slows dashboard load (additional queries)               
|
 Medium 
|
 Medium      
|
 Dashboard already has 5+ queries; budget summary adds 1 more. Parallelize with 
`Task.WhenAll`
 if needed 
|
|
 R-5.3 
|
 Month boundary edge cases (timezone-related)                                 
|
 Low    
|
 Medium      
|
 Use 
`DateTime.UtcNow`
 consistently; DateRange handles boundaries; test month start/end explicitly    
|
|
 R-5.4 
|
 Unique constraint race condition (two simultaneous creates)                  
|
 Low    
|
 Low         
|
 DB 
`UNIQUE`
 constraint catches at DB level; handler validates first for better UX error message      
|
|
 R-5.5 
|
 BudgetStatusLevel threshold confusion (inclusive vs. exclusive boundaries)   
|
 Low    
|
 Medium      
|
 Explicitly tested in T-5.35, T-5.36, T-5.37; documented thresholds use 
`>`
 (not 
`>=`
)              
|
|
 R-5.6 
|
 Budget vs. actual page empty for months with no data                         
|
 Low    
|
 Medium      
|
 Show informational message: "No spending data for this month"                                       
|
|
 R-5.7 
|
 Progress bar visual overflow when > 100%                                     
|
 Low    
|
 Low         
|
 CSS 
`max-width: 100%`
 with overflow label showing actual percentage                                 
|
|
 R-5.8 
|
 Budget comparison bar chart unreadable with many categories                  
|
 Low    
|
 Medium      
|
 Limit to 10 categories in chart; show full data in table                                            
|
|
 R-5.9 
|
`ON DELETE CASCADE`
 on category_id FK deletes budget when category is deleted
|
 Medium 
|
 Low         
|
 Category deletion already blocked if has active transactions (Phase 3); budget deletion is separate  
|

---

## Implementation Notes

### Recommended Implementation Order
Step 1: Write Domain.Tests for BudgetService (RED phase)
└── Tests T-5.29–T-5.38
└── Mock: IBudgetRepository with Moq
└── Verify: tests FAIL (red)

Step 2: Implement BudgetService + BudgetStatusLevel (GREEN phase)
└── Domain/Services/BudgetService.cs
└── Domain/Services/BudgetStatusLevel.cs
└── Verify: dotnet test --filter Category=Domain — new tests GREEN

Step 3: Write Application.Tests for Budget commands (RED phase)
└── Tests T-5.01–T-5.13
└── Mock: IBudgetRepository, ICategoryRepository, IUserContext, BudgetService
└── Verify: tests FAIL (red)

Step 4: Implement Budget commands + handlers (GREEN phase)
└── CreateBudgetCommand, UpdateBudgetCommand, DeleteBudgetCommand
└── Verify: dotnet test --filter Category=Application — command tests GREEN

Step 5: Write Application.Tests for Budget queries (RED phase)
└── Tests T-5.14–T-5.21
└── Mock: IBudgetRepository, ITransactionRepository, IUserContext, BudgetService
└── Verify: tests FAIL (red)

Step 6: Implement Budget queries + handlers (GREEN phase)
└── GetBudgetsQuery, GetBudgetByIdQuery, GetBudgetStatusQuery
└── BudgetDto, BudgetStatusDto
└── Verify: tests GREEN

Step 7: Write Application.Tests for Budget vs Actual + Dashboard summary (RED phase)
└── Tests T-5.22–T-5.28
└── Verify: tests FAIL (red)

Step 8: Implement Budget vs Actual + Dashboard summary handlers (GREEN phase)
└── GetBudgetVsActualQuery, GetBudgetSummaryForDashboardQuery
└── BudgetVsActualDto, BudgetDashboardSummaryDto
└── Verify: dotnet test --filter Category=Application — all budget tests GREEN

Step 9: Apply database migration to Supabase
└── Run 005_CreateBudgetsTable.sql
└── Verify: table + unique constraint + indexes + RLS policies in Supabase dashboard

Step 10: Implement Infrastructure repository
└── SupabaseBudgetRepository
└── Update DependencyInjection.cs (register repository + BudgetService)
└── Verify: manual CRUD tests against Supabase

Step 11: Implement Frontend — Budget management pages
└── /Budgets — list with status indicators
└── /Budgets/Create — create form
└── /Budgets/{id} — detail with progress bar + transactions
└── Reusable components: _BudgetProgressBar, _BudgetStatusBadge
└── Verify: manual E2E test

Step 12: Implement Frontend — Budget vs. actual comparison page
└── /Budgets/Comparison — table + bar chart
└── Month selector
└── Chart.js horizontal bar chart
└── Verify: manual E2E test

Step 13: Implement Dashboard budget status widget
└── Update Dashboard.cshtml + DashboardModel
└── Add GetBudgetSummaryForDashboardQuery call
└── Mini progress bars + summary line
└── Verify: widget appears on dashboard with correct data

Step 14: Update navigation
└── _Layout.cshtml: add Budgets + Comparison links
└── Verify: navigation works on all pages

Step 15: End-to-end validation
└── Full workflow test:
1. Create budgets for 3 categories for current month
2. Import PDF / add transactions in those categories
3. View budget list → verify spend/remaining/status
4. Check dashboard → budget widget shows correct indicators
5. View budget vs. actual → compare all categories
6. Edit a budget limit → verify recalculation
7. Delete a budget → verify transactions unaffected
8. Create duplicate budget → verify rejected with error
9. Test with two users → verify tenant isolation
└── Test responsive design (budget pages on mobile)
└── Verify Chart.js bar chart renders correctly

Step 16: Final test + coverage validation
└── dotnet build → zero errors, zero warnings
└── dotnet test → ALL tests green (Phase 0 + 1 + 2 + 3 + 4 + 5)
└── Domain coverage ≥ 80% (cumulative)
└── Application coverage ≥ 70%
└── Audit: no forbidden layer references

text

### Spec-Driven Workflow Compliance

| Step | Workflow Stage           | Phase 5 Action                                                                 |
|------|--------------------------|--------------------------------------------------------------------------------|
| 1    | Write Test Spec          | ✅ Tests written first (Steps 1, 3, 5, 7)                                     |
| 2    | Define Handler Stub      | ✅ MediatR commands/queries defined (Steps 4, 6, 8)                           |
| 3    | Build Domain             | ✅ BudgetService + BudgetStatusLevel (Step 2)                                 |
| 4    | Implement Persistence    | ✅ SupabaseBudgetRepository + migration (Steps 9, 10)                         |
| 5    | Wire UI                  | ✅ Budget pages + dashboard widget (Steps 11–14)                              |
| 6    | End-to-end Test          | ✅ Full budget workflow validation (Step 15)                                   |

### Testing Patterns Used in This Phase

| Pattern                       | Description                                                                     | Example                                                                  |
|-------------------------------|---------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| Spend Calculation Test        | Verify handler correctly sums transactions for budget category + month          | 3 groceries txns (€100+€150+€75) → currentSpend = €325                  |
| Status Level Boundary Test    | Verify status thresholds at exact boundary values                               | 60% → Green (not Yellow); 80% → Yellow (not Red)                        |
| Uniqueness Validation Test    | Verify duplicate budget rejected with descriptive error                         | Same category + month → DomainException "already exists"                 |
| Comparison Aggregation Test   | Verify budget vs. actual includes budgeted and unbudgeted categories            | 3 budgets + 1 unbudgeted category → 4 comparison items                  |
| Dashboard Summary Test        | Verify aggregated counts (on track, warning, overage)                           | 5 budgets → 3 on track, 1 warning, 1 overage                            |
| Tenant Isolation Test         | Verify budget operations reject wrong-user access                               | Budget owned by user B → EntityNotFoundException for user A              |
| Cascade Safety Test           | Verify budget deletion doesn't affect transactions                              | Delete budget → transactions still exist                                 |

### Budget Status Visual Reference
┌──────────────────────────────────────────────────────────┐
│ 🟢 Green (< 60%) │
│ ████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░ 45% — On Track │
├──────────────────────────────────────────────────────────┤
│ 🟡 Yellow (60–80%) │
│ █████████████████████████░░░░░░░░░░░░░░░ 70% — Caution │
├──────────────────────────────────────────────────────────┤
│ 🔴 Red (80–100%) │
│ █████████████████████████████████████░░░░ 92% — Warning │
├──────────────────────────────────────────────────────────┤
│ ⚠️ Overage (> 100%) │
│ ████████████████████████████████████████ 125% — OVER! │
│ ⚠️ Over budget by €50.00 │
└──────────────────────────────────────────────────────────┘

text

### Security Considerations

- ☑️ All budget operations verify `UserId` from `IUserContext` (handler-level tenant isolation)
- ☑️ RLS policies on budgets table (belt-and-suspenders with handler-level checks)
- ☑️ `ON DELETE CASCADE` on `user_id` FK — user deletion cleans up all budgets
- ☑️ `ON DELETE CASCADE` on `category_id` FK — category deletion cleans up associated budgets
- ☑️ Unique constraint `(user_id, category_id, month_start)` prevents duplicate budgets at DB level
- ☑️ Budget limit validated as positive in domain entity constructor + `UpdateLimit` method
- ☑️ Alpine.js confirmation dialog prevents accidental budget deletes
- ☑️ Budget comparison page does not leak other users' data (query scoped to current user)
- ☑️ No Supabase service key exposed in frontend (only anon key)

---

## Cumulative Test Count (Phases 0–5)

| Phase   | Domain Tests | Application Tests | Total Phase | Cumulative Total |
|---------|--------------|-------------------|-------------|------------------|
| Phase 0 | 11           | 2                 | 13          | 13               |
| Phase 1 | 8            | 14                | 22          | 35               |
| Phase 2 | 81           | 0                 | 81          | 116              |
| Phase 3 | 5            | 33                | 38          | 154              |
| Phase 4 | 7            | 25                | 32          | 186              |
| Phase 5 | 10           | 28                | 38          | 224              |

---

## Feature Completion Status (After Phase 5)

| #   | Feature                                    | Phase Delivered | Status |
|-----|--------------------------------------------|-----------------|--------|
| 1   | User registration (email/password)         | Phase 1          | ✅     |
| 2   | User login with JWT cookies                | Phase 1          | ✅     |
| 3   | User logout                                | Phase 1          | ✅     |
| 4   | Tenant isolation (handler + RLS)           | Phase 1          | ✅     |
| 5   | Domain model (entities, VOs, services)     | Phase 2          | ✅     |
| 6   | PDF bank statement import                  | Phase 3          | ✅     |
| 7   | Duplicate transaction detection            | Phase 3          | ✅     |
| 8   | Manual transaction creation                | Phase 3          | ✅     |
| 9   | Transaction categorization                 | Phase 3          | ✅     |
| 10  | Transaction deletion                       | Phase 3          | ✅     |
| 11  | Category CRUD with system default guards   | Phase 3          | ✅     |
| 12  | System default categories (4)              | Phase 3          | ✅     |
| 13  | Analytics dashboard with summary cards     | Phase 4          | ✅     |
| 14  | Spending by category pie chart             | Phase 4          | ✅     |
| 15  | Monthly trends line chart                  | Phase 4          | ✅     |
| 16  | Yearly comparison bar chart                | Phase 4          | ✅     |
| 17  | Transaction search with multi-filter       | Phase 4          | ✅     |
| 18  | Date range filter (reusable component)     | Phase 4          | ✅     |
| 19  | **Budget CRUD (create, edit, delete)**     | **Phase 5**      | ✅     |
| 20  | **Budget status indicators (G/Y/R/O)**    | **Phase 5**      | ✅     |
| 21  | **Budget vs. actual comparison**           | **Phase 5**      | ✅     |
| 22  | **Dashboard budget status widget**         | **Phase 5**      | ✅     |
| 23  | Responsive design (mobile + desktop)       | Phase 4+5        | ✅     |
| 24  | ≥224 automated tests passing              | Phase 0–5        | ✅     |

**Remaining for Production (Phase 6):** UI polish, Tailwind build pipeline, performance optimization, Vercel deployment, error monitoring, accessibility audit.

---

_Phase Spec Version: 1.0.0 | Created: 2026-02-15 | Aligned with Constitution v1.1.0_