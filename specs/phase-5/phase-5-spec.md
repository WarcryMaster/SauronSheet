# Phase 5: Budget Management & Alerts

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Features)
- **Phase Type**: Full-Stack (Features)
- **Duration**: Weeks 19–21
- **Goal**: Budget CRUD, overage detection, visual alerts on dashboard, budget vs. actual reporting
- **Depends On**: Phase 0 (foundation), Phase 1 (auth + tenant scoping), Phase 2 (domain model — BudgetId, Money, DateRange value objects), Phase 3 (transaction CRUD, category management, Supabase repositories), Phase 4 (analytics dashboard, date range filter, Chart.js integration)
- **Unlocks**: Phase 6 (UI Polish, Performance & Production Deployment)

> **Note**: The Phase 2 spec planned Budget entity, IBudgetRepository, and 15 Budget entity tests as deliverables, but they were **never implemented**. Phase 5 absorbs those deliverables as prerequisites for budget management.

---

## Critical Decisions

| ID | Decision | Rationale | Date |
|---|---|---|---|
| CD-5.1 | Budget alerts via visual indicators only (no push/email) | Immediate dashboard feedback; push notifications deferred to post-MVP | 2026-02-15 |
| CD-5.2 | Budget status thresholds: green < 60%, yellow 60–80%, red > 80% | Industry-standard visual cues; overage (> 100%) gets distinct styling | 2026-02-15 |
| CD-5.3 | Budget vs. actual comparison as dashboard widget + dedicated page | Quick glance on dashboard; full detail on separate page | 2026-02-15 |
| CD-5.4 | Budget uniqueness enforced at handler level + DB constraint | Belt-and-suspenders: handler validates before insert; DB `UNIQUE` prevents race conditions | 2026-02-15 |
| CD-5.5 | Current spend calculated from transactions (not denormalized) | Source of truth is transactions table; avoids stale cache issues | 2026-02-15 |
| CD-5.6 | Budget month represented as DateRange (1st to last day of month) | Consistent with domain model; allows flexible month boundaries | 2026-02-15 |
| CD-5.7 | Budget management page separate from dashboard | Dashboard shows status summary; management page for CRUD operations | 2026-02-15 |
| CD-5.8 | Budget status widget on dashboard replaces placeholder content | Phase 1 stub "Your dashboard will appear here" fully replaced by Phase 4+5 content | 2026-02-15 |
| CD-5.9 | Overage percentage capped at display level (not domain) | Domain returns raw percentage (can be > 1.0); UI caps visual bar at 100% with overflow label | 2026-02-15 |
| CD-5.10 | Budget deletion has no cascading effects on transactions | Deleting a budget doesn't affect categorized transactions; budget is a tracking overlay | 2026-02-15 |
| CD-5.11 | Phase 5 absorbs undelivered Phase 2 Budget entity + IBudgetRepository + tests | Phase 2 only delivered BudgetId value object; Budget entity and repository interface were planned but never built | 2026-02-20 |

---

## Clarifications

### Session 2026-02-20

- Q: GetBudgetsQuery contract mismatch between spec and plan — spec uses `(int? Year, int? Month) → List<BudgetStatusDto>`, plan uses `(DateTime? MonthFilter) → List<BudgetDto>`. Which is authoritative? → A: Use spec contract `GetBudgetsQuery(int? Year, int? Month) : IRequest<List<BudgetStatusDto>>`. The budget list page requires spend calculations (CurrentSpend, RemainingAmount, PercentageUsed, StatusLevel) to render progress bars and status indicators per Scenario 5.2. Plan must be updated to match.
- Q: `BudgetStatusLevel` file location — spec places it in `Domain/Services/`, plan places it in `Domain/ValueObjects/`. Which location? → A: `Domain/ValueObjects/BudgetStatusLevel.cs`. Enums representing domain value concepts belong alongside other value types (`BudgetId`, `Money`). Spec file structure updated to match plan.
- Q: `Budget.PercentageUsed` and `IsOverBudget` do raw decimal division without currency validation, unlike `Money.Minus`/`Money.Plus` which throw on mismatch. Should currency be validated? → A: Yes. Add `EnsureSameCurrency` validation to `PercentageUsed`, `IsOverBudget`, and `RemainingAmount` for consistency with `Money` arithmetic. The `Limit.Amount == 0` guard in `PercentageUsed` is dead code (constructor rejects limit ≤ 0) but is retained as a defensive measure.
- Q: Category deletion with active budgets — spec says `ON DELETE CASCADE` silently removes budgets, but user gets no warning. Should they be warned? → A: Keep DB cascade (`ON DELETE CASCADE` on `category_id` FK) but add a UI warning on the category deletion confirmation: "This category has X active budget(s). Deleting will also remove them." The delete handler queries `IBudgetRepository` for budget count before proceeding. No domain-level blocking (budgets are tracking overlays).
- Q: `GetBudgetVsActualQuery` parameter type — spec uses `(int Year, int Month)`, plan uses `(DateTime PeriodStart, DateTime PeriodEnd)`. Which is correct? → A: Use spec contract `GetBudgetVsActualQuery(int Year, int Month)`. Handler constructs `DateRange` internally from year+month. Consistent with `GetBudgetsQuery(int?, int?)` and `GetBudgetSummaryForDashboardQuery(int, int)` signatures. Plan must be updated to match.

---

## Executive Summary

### In Scope

| Area | Deliverable |
|---|---|
| Domain | `Budget` aggregate root entity (constructor, `IsOverBudget`, `PercentageUsed`, `RemainingAmount`, `UpdateLimit`) |
| Domain | `IBudgetRepository` interface (GetByIdAsync, GetByUserIdAsync, GetByUserAndCategoryAndMonthAsync, AddAsync, UpdateAsync, DeleteAsync) |
| Domain | `BudgetService` domain service (uniqueness validation, status level calculation) |
| Domain | `BudgetStatusLevel` enum (Green, Yellow, Red, Overage) |
| Application | `CreateBudgetCommand` + handler (create budget with uniqueness check) |
| Application | `UpdateBudgetCommand` + handler (update budget limit) |
| Application | `DeleteBudgetCommand` + handler (delete budget) |
| Application | `GetBudgetsQuery` + handler (list budgets for user, optional month filter) |
| Application | `GetBudgetByIdQuery` + handler (single budget with current spend + status) |
| Application | `GetBudgetVsActualQuery` + handler (all budgets vs. actual spending for a month) |
| Application | `GetBudgetSummaryForDashboardQuery` + handler (aggregated budget health for dashboard widget) |
| Application | DTOs: `BudgetDto`, `BudgetStatusDto`, `BudgetVsActualDto`, `BudgetDashboardSummaryDto` |
| Infrastructure | `SupabaseBudgetRepository` (implements `IBudgetRepository`) |
| Infrastructure | Database migration: `006_CreateBudgetsTable.sql` — budgets table with indexes, unique constraint, and RLS |
| Frontend | Budget management page (`/Budgets`) — create, edit, delete |
| Frontend | Budget detail page (`/Budgets/{id}`) — single budget with spend progress |
| Frontend | Budget vs. actual page (`/Budgets/Comparison`) — all budgets for a month |
| Frontend | Dashboard budget status widget (green/yellow/red indicators) |
| Frontend | Updated `_Layout.cshtml` navigation with budget links |
| Frontend | Reusable components: `_BudgetProgressBar.cshtml`, `_BudgetStatusBadge.cshtml` |
| Tests | ≥55 tests (28 Application handler tests + 10 Domain BudgetService tests + 17 Domain Budget entity tests) |

### Deferred (NOT in this phase)

| Item | Target Phase | Reason |
|---|---|---|
| Budget alerts via email/push notifications | Post-MVP | Requires notification infrastructure |
| Recurring/auto-renewing budgets | Post-MVP | Monthly auto-creation adds complexity |
| Budget templates (copy from previous month) | Post-MVP | UX convenience; not core functionality |
| Budget history / trend analysis | Post-MVP | "How has my grocery budget changed over 6 months?" |
| Multi-category budgets (e.g., "Food" group) | Post-MVP | Category grouping not yet supported |
| Budget sharing between users | Post-MVP | Multi-user budget collaboration |
| CSV/PDF export of budget reports | Post-MVP | Export feature |
| Budget rollover (unused amount carries forward) | Post-MVP | Complex financial logic |
| Animated progress bars | Phase 6 | Polish concern; static bars in this phase |

---

## User Scenarios & Testing

### Scenario 5.1: Create a Budget (Priority: P1)

**As a** user
**I want to** create a monthly budget for a specific category
**So that** I can set spending limits and track my progress

**Why this priority**: Core CRUD — without budget creation, nothing else works.

**Independent Test**: Can be fully tested by navigating to /Budgets/Create, selecting a category, entering a limit, and verifying the budget appears in the list.

**Acceptance Criteria:**
1. **Given** a logged-in user on the budget creation page, **When** they select a category, choose a month, enter a positive limit, and submit, **Then** the budget is created and they are redirected to the budget list with a success message.
2. **Given** a budget already exists for Groceries in February 2026, **When** the user tries to create another budget for Groceries in February 2026, **Then** an error "A budget for Groceries in February 2026 already exists" is shown and the form retains its values.
3. **Given** the user enters a zero or negative limit, **When** they submit the form, **Then** a validation error is displayed.
4. **Given** the category dropdown, **When** the page loads, **Then** all user categories (system defaults + user-defined) are shown, and the month picker defaults to current month.
5. **Given** a currency, **When** creating a budget, **Then** the currency defaults to EUR (consistent with system).

### Scenario 5.2: View and Manage Budgets (Priority: P1)

**As a** user
**I want to** see all my budgets in a list with current status
**So that** I can manage my spending limits

**Why this priority**: Users need to see budget status to derive value from creation.

**Independent Test**: Navigate to /Budgets and verify the list shows budgets with correct spend calculations and status indicators.

**Acceptance Criteria:**
1. **Given** a user with budgets for the current month, **When** they visit /Budgets, **Then** they see a list with columns: Category name, Limit, Current Spend, Remaining, Percentage Used, Status indicator.
2. **Given** no budgets exist for the selected month, **When** visiting /Budgets, **Then** an empty state is shown: "No budgets set for {month}. Create one to start tracking." with a create link.
3. **Given** the budget list, **When** viewing rows, **Then** status indicators are color-coded: green (< 60%), yellow (60–80%), red (80–100%), overage (> 100%).
4. **Given** a month selector, **When** switching months, **Then** the list updates to show budgets for the selected month only.
5. **Given** the budget list, **When** viewing it, **Then** budgets are sorted alphabetically by category name.

### Scenario 5.3: Edit a Budget (Priority: P2)

**As a** user
**I want to** change the spending limit on an existing budget
**So that** I can adjust my budget as circumstances change

**Why this priority**: Editing is secondary to creating; users can delete and recreate as a workaround.

**Independent Test**: Click edit on a budget row, change the limit to a new positive value, save, and verify the new limit is reflected.

**Acceptance Criteria:**
1. **Given** an existing budget with limit €500, **When** the user edits it to €600, **Then** the budget limit is updated and status recalculated immediately.
2. **Given** the edit form, **When** the user enters zero or negative, **Then** a validation error is displayed.
3. **Given** the edit form, **When** the user cancels, **Then** no changes are made.
4. **Given** a budget, **When** editing, **Then** only the limit amount can be changed (category and month are immutable after creation).

### Scenario 5.4: Delete a Budget (Priority: P2)

**As a** user
**I want to** delete a budget I no longer need
**So that** my budget list stays clean

**Why this priority**: Cleanup operation; less critical than CRUD and viewing.

**Independent Test**: Click delete on a budget row, confirm, and verify it disappears from the list while transactions remain unaffected.

**Acceptance Criteria:**
1. **Given** a budget in the list, **When** the user clicks delete and confirms, **Then** the budget is removed from the database and list.
2. **Given** the confirmation dialog, **When** the user cancels, **Then** no action is taken.
3. **Given** a deleted budget, **When** checking the transactions table, **Then** all transactions and categories are unaffected.
4. **Given** a budget owned by User A, **When** User B tries to delete it, **Then** it throws EntityNotFoundException (tenant isolation).

### Scenario 5.5: View Budget Status on Dashboard (Priority: P1)

**As a** user
**I want to** see my budget health at a glance on the dashboard
**So that** I can quickly identify overspending

**Why this priority**: Dashboard integration gives immediate value; users check the dashboard first.

**Independent Test**: Navigate to the dashboard and verify the budget widget shows correct status for current month budgets.

**Acceptance Criteria:**
1. **Given** budgets exist for the current month, **When** visiting the dashboard, **Then** a "Budget Status" widget shows mini progress bars for each budget.
2. **Given** each progress bar, **When** viewing, **Then** it shows category name, spent/limit label, and a colored bar (Green < 60%, Yellow 60–80%, Red 80–100%, Overage > 100%).
3. **Given** the widget, **When** viewing, **Then** a summary line shows "X of Y budgets on track" or "Z budgets over limit".
4. **Given** no budgets, **When** viewing the dashboard, **Then** the widget shows "No budgets set. Create budgets to track spending." with a link to /Budgets/Create.
5. **Given** a budget in the widget, **When** clicking on it, **Then** the user is navigated to the budget detail page.
6. **Given** the dashboard date range filter, **When** the user changes the month, **Then** the budget widget updates accordingly.

### Scenario 5.6: View Budget vs. Actual Comparison (Priority: P2)

**As a** user
**I want to** see a detailed comparison of budgeted vs. actual spending per category
**So that** I can analyze where I'm over or under budget

**Why this priority**: Comparison provides deeper insight but is not needed for basic budget tracking.

**Independent Test**: Navigate to /Budgets/Comparison, select a month, and verify the table and chart show correct budget-vs-actual data.

**Acceptance Criteria:**
1. **Given** budgets and transactions for a month, **When** visiting /Budgets/Comparison, **Then** a table shows per-category comparison: category name, budget limit, actual spending, difference, status indicator.
2. **Given** categories with spending but no budget, **When** viewing, **Then** they appear with "No budget" in the limit column.
3. **Given** the comparison data, **When** viewing, **Then** a summary row at the bottom shows total budgeted, total actual, total difference.
4. **Given** the comparison page, **When** viewing, **Then** a horizontal bar chart (Chart.js) shows budget limit vs. actual per category.
5. **Given** the data, **When** sorting, **Then** categories are sorted by over budget first, then by percentage used descending.

### Scenario 5.7: View Budget Detail (Priority: P3)

**As a** user
**I want to** see detailed information about a single budget
**So that** I can understand my spending progress in that category

**Why this priority**: Detail page is a convenience; budget list already shows key info.

**Independent Test**: Click a budget from the list and verify the detail page shows progress bar and transaction list.

**Acceptance Criteria:**
1. **Given** a budget, **When** navigating to /Budgets/{id}, **Then** the page shows: Category name, Month, Limit, Current Spend, Remaining, Percentage Used with a large color-coded progress bar.
2. **Given** the budget detail page, **When** viewing, **Then** a list of transactions in this category for this month is shown.
3. **Given** the detail page, **When** clicking the edit button, **Then** the user can change the budget limit inline or via modal.
4. **Given** the detail page, **When** clicking "Back to budgets", **Then** the user returns to the budget list.

### Edge Cases

- What happens when a category with an active budget is deleted? → Budget is cascade-deleted at DB level (`ON DELETE CASCADE` on `category_id` FK). Application doesn't block category deletion for budgets, but the UI shows a warning: "This category has X active budget(s). Deleting will also remove them." The category delete handler queries `IBudgetRepository.GetByUserIdAsync` (filtered by category) to determine budget count for the warning message.
- What happens when viewing a budget for a month with zero transactions? → Shows €0.00 current spend, 0% used, status Green.
- What happens when a budget limit is very small (e.g., €0.01)? → Valid; small transactions will quickly trigger overage status.
- What happens when the progress bar exceeds 100%? → CSS caps visual bar at 100% width; an overflow label shows actual percentage (e.g., "125%").
- What happens when switching months and no budgets exist? → Empty state message with create link.
- What happens when two users create budgets for the same category/month simultaneously? → DB unique constraint prevents duplicates; second user gets an error.
- What happens when `currentSpend` has a different currency than `Limit`? → `IsOverBudget` and `PercentageUsed` throw `InvalidOperationException` (consistent with `Money.Minus`/`Money.Plus` currency validation). Single-currency system (EUR) makes this a defensive guard, not a UX concern.

---

## Functional Requirements

### FR-5.01: Domain Layer — Budget Entity, Repository Interface, Domain Service

#### Budget Entity (Aggregate Root)

> **Note**: This entity was planned for Phase 2 but never implemented. Only `BudgetId` value object exists from Phase 2. Phase 5 delivers the full Budget aggregate root.

```csharp
public class Budget : AggregateRoot<BudgetId>
{
    public UserId UserId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public DateRange Month { get; private set; }
    public Money Limit { get; private set; }

    public Budget(
        BudgetId id,
        UserId userId,
        CategoryId categoryId,
        DateRange month,
        Money limit)
        : base(id)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        Month = month ?? throw new ArgumentNullException(nameof(month));

        if (limit == null) throw new ArgumentNullException(nameof(limit));
        if (limit.Amount <= 0) throw new DomainException("Budget limit must be positive.");

        Limit = limit;
    }

    public bool IsOverBudget(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        EnsureSameCurrency(currentSpend);
        return currentSpend.Amount > Limit.Amount;
    }

    public decimal PercentageUsed(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        EnsureSameCurrency(currentSpend);
        if (Limit.Amount == 0) return 0; // Defensive: constructor prevents limit <= 0
        return currentSpend.Amount / Limit.Amount;
    }

    public Money RemainingAmount(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        // Currency validation delegated to Money.Minus (throws InvalidOperationException)
        return Limit.Minus(currentSpend);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Limit.Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot compare budget in {Limit.Currency} with spending in {other.Currency}");
    }

    public void UpdateLimit(Money newLimit)
    {
        if (newLimit == null) throw new ArgumentNullException(nameof(newLimit));
        if (newLimit.Amount <= 0) throw new DomainException("Budget limit must be positive.");
        Limit = newLimit;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

#### IBudgetRepository Interface

> **Note**: This repository interface was planned for Phase 2 but never implemented. Phase 5 delivers it.

```csharp
public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(BudgetId id);
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId);
    Task<Budget?> GetByUserAndCategoryAndMonthAsync(
        UserId userId, CategoryId categoryId, DateRange month);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(BudgetId id);
}
```

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
```

#### BudgetStatusLevel (Domain Enum)

```csharp
public enum BudgetStatusLevel
{
    Green,    // < 60% used — on track
    Yellow,   // 60–80% used — caution
    Red,      // 80–100% used — warning
    Overage   // > 100% used — over budget
}
```

#### File Structure

```text
Domain/
├── Entities/
│   ├── (existing: Transaction.cs, Category.cs, ImportBatch.cs)
│   └── Budget.cs                       # NEW — Budget aggregate root
├── Repositories/
│   ├── (existing: ITransactionRepository.cs, ICategoryRepository.cs, IPdfImportRepository.cs, ISpecification.cs)
│   └── IBudgetRepository.cs            # NEW — Budget repository contract
├── Services/
│   ├── (existing: CategoryService.cs, IAuthService.cs)
│   └── BudgetService.cs                # NEW — Budget domain service
└── ValueObjects/
    ├── (existing: BudgetId.cs, Money.cs, DateRange.cs, etc.)
    └── BudgetStatusLevel.cs            # NEW — Status level enum
```

### FR-5.02: Application Layer — Budget Commands & Queries

```text
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
│       │   ├── GetBudgetVsActualQuery.cs
│       │   ├── GetBudgetVsActualQueryHandler.cs
│       │   ├── GetBudgetSummaryForDashboardQuery.cs
│       │   └── GetBudgetSummaryForDashboardQueryHandler.cs
│       └── DTOs/
│           ├── BudgetDto.cs
│           ├── BudgetStatusDto.cs
│           ├── BudgetVsActualDto.cs
│           └── BudgetDashboardSummaryDto.cs
```

#### CreateBudgetCommand

```csharp
public record CreateBudgetCommand(
    Guid CategoryId,
    int Year,
    int Month,
    decimal LimitAmount,
    string Currency = "EUR"
) : IRequest<Guid>;
```

**Handler Flow:**
1. Get UserId from IUserContext
2. Validate category exists and belongs to user (`ICategoryRepository.GetByIdAsync`)
3. Build DateRange for the month (1st to last day)
4. Create Money value object from LimitAmount + Currency
5. Validate uniqueness via `BudgetService.ValidateUniqueBudget(userId, categoryId, month)`
6. Create BudgetId (new Guid)
7. Create Budget entity (invariants enforced by constructor: limit > 0)
8. Persist via `IBudgetRepository.AddAsync()`
9. Return BudgetId.Value

#### UpdateBudgetCommand

```csharp
public record UpdateBudgetCommand(
    Guid BudgetId,
    decimal NewLimitAmount,
    string Currency = "EUR"
) : IRequest<Unit>;
```

**Handler Flow:**
1. Get UserId from IUserContext
2. Load Budget by Id; throw EntityNotFoundException if not found
3. Verify Budget.UserId matches current user (tenant isolation)
4. Create Money value object from NewLimitAmount + Currency
5. Call `Budget.UpdateLimit(newLimit)` (invariant enforced by entity: limit > 0)
6. Persist via `IBudgetRepository.UpdateAsync()`

#### DeleteBudgetCommand

```csharp
public record DeleteBudgetCommand(
    Guid BudgetId
) : IRequest<Unit>;
```

**Handler Flow:**
1. Get UserId from IUserContext
2. Load Budget by Id; throw EntityNotFoundException if not found
3. Verify Budget.UserId matches current user (tenant isolation)
4. Delete via `IBudgetRepository.DeleteAsync()`

#### GetBudgetsQuery

```csharp
public record GetBudgetsQuery(
    int? Year = null,
    int? Month = null
) : IRequest<List<BudgetStatusDto>>;
```

**Handler Flow:**
1. Get UserId from IUserContext
2. Load budgets for user (optionally filtered by month)
3. For each budget:
   a. Calculate current spend from transactions in the budget's category + month
      (via `ITransactionRepository.FindBySpecificationAsync` with date range + category specs)
   b. Calculate percentage used via `Budget.PercentageUsed(currentSpend)`
   c. Calculate remaining via `Budget.RemainingAmount(currentSpend)`
   d. Determine status level (`BudgetService.CalculateStatusLevel`)
4. Map to `List<BudgetStatusDto>`
5. Sort by category name alphabetically

#### GetBudgetByIdQuery

```csharp
public record GetBudgetByIdQuery(
    Guid BudgetId
) : IRequest<BudgetStatusDto>;
```

**Handler Flow:**
1. Get UserId from IUserContext
2. Load Budget by Id; throw EntityNotFoundException if not found
3. Verify Budget.UserId matches current user
4. Calculate current spend (same as GetBudgetsQuery logic)
5. Map to BudgetStatusDto with full details

#### GetBudgetVsActualQuery

```csharp
public record GetBudgetVsActualQuery(
    int Year,
    int Month
) : IRequest<List<BudgetVsActualDto>>;
```

**Handler Flow:**
1. Get UserId from IUserContext
2. Build DateRange for the requested month
3. Load all budgets for user in that month
4. Load all transactions for user in that month, grouped by category
5. Build comparison list:
   a. For each budget: category name, limit, actual spend, difference, status
   b. For categories with spending but no budget: include with limit = null
6. Sort: over budget first, then by percentage used descending
7. Calculate totals: total budgeted, total actual, total difference
8. Return `List<BudgetVsActualDto>`

#### GetBudgetSummaryForDashboardQuery

```csharp
public record GetBudgetSummaryForDashboardQuery(
    int Year,
    int Month
) : IRequest<BudgetDashboardSummaryDto>;
```

**Handler Flow:**
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

### FR-5.03: Application DTOs

#### BudgetDto

```csharp
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
```

#### BudgetStatusDto

```csharp
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
```

#### BudgetVsActualDto

```csharp
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
```

#### BudgetDashboardSummaryDto

```csharp
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
```

### FR-5.04: Infrastructure — Budget Repository & Migration

```text
Infrastructure/
├── Persistence/
│   ├── (existing: SupabaseTransactionRepository, SupabaseCategoryRepository, SupabasePdfImportRepository)
│   ├── SupabaseBudgetRepository.cs                # NEW
│   └── Migrations/
│       ├── (existing: 001–005 from Phase 1/3)
│       └── 006_CreateBudgetsTable.sql             # NEW
```

#### SupabaseBudgetRepository

```csharp
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
```

#### Entity ↔ Supabase Mapping

| Entity Property | Supabase Column | Type | Notes |
|---|---|---|---|
| Id.Value | id | UUID | Primary key |
| UserId.Value | user_id | UUID | FK to users.id |
| CategoryId.Value | category_id | UUID | FK to categories.id |
| Month.StartDate | month_start | TIMESTAMPTZ | First day of budget month |
| Month.EndDate | month_end | TIMESTAMPTZ | Last day of budget month |
| Limit.Amount | limit_amount | DECIMAL(15,2) | Budget limit |
| Limit.Currency | currency | VARCHAR(3) | Currency code (default EUR) |
| CreatedAt | created_at | TIMESTAMPTZ | Auto-set |
| UpdatedAt | updated_at | TIMESTAMPTZ | Nullable; set on update |

#### 006_CreateBudgetsTable.sql

```sql
-- Migration: 006_CreateBudgetsTable.sql
-- Purpose: Monthly budgets per category per user
-- Note: Migration 005 is 005_CreateUserProfileTrigger.sql (already applied)

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
```

### FR-5.05: Frontend Pages

```text
Frontend/
├── Pages/
│   ├── Budgets/
│   │   ├── Index.cshtml               # Budget list with status indicators
│   │   ├── Index.cshtml.cs
│   │   ├── Create.cshtml              # Create budget form
│   │   ├── Create.cshtml.cs
│   │   ├── Detail.cshtml              # Budget detail with progress bar
│   │   ├── Detail.cshtml.cs
│   │   ├── Comparison.cshtml          # Budget vs. actual comparison
│   │   └── Comparison.cshtml.cs
│   └── Dashboard.cshtml               # UPDATED: add budget status widget
├── Shared/
│   ├── _BudgetProgressBar.cshtml      # NEW: reusable progress bar component
│   └── _BudgetStatusBadge.cshtml      # NEW: reusable status badge component
```

#### Budget List Page (Index)

```csharp
public class BudgetListModel : PageModel
{
    private readonly IMediator _mediator;

    public BudgetListModel(IMediator mediator) => _mediator = mediator;

    public List<BudgetStatusDto> Budgets { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int? Year { get; set; }
    [BindProperty(SupportsGet = true)] public int? Month { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        Year ??= DateTime.UtcNow.Year;
        Month ??= DateTime.UtcNow.Month;
        SuccessMessage = TempData["Success"]?.ToString();
        Budgets = await _mediator.Send(new GetBudgetsQuery(Year, Month));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid budgetId)
    {
        await _mediator.Send(new DeleteBudgetCommand(budgetId));
        TempData["Success"] = "Budget deleted successfully.";
        return RedirectToPage();
    }
}
```

#### Create Budget Page

```csharp
public class CreateBudgetModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateBudgetModel(IMediator mediator) => _mediator = mediator;

    [BindProperty] public Guid CategoryId { get; set; }
    [BindProperty] public int Year { get; set; } = DateTime.UtcNow.Year;
    [BindProperty] public int Month { get; set; } = DateTime.UtcNow.Month;
    [BindProperty] public decimal LimitAmount { get; set; }

    public List<CategoryDto> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            await _mediator.Send(new CreateBudgetCommand(CategoryId, Year, Month, LimitAmount));
            TempData["Success"] = "Budget created successfully.";
            return RedirectToPage("Index");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            Categories = await _mediator.Send(new GetCategoriesQuery());
            return Page();
        }
    }
}
```

#### Budget Detail Page

```csharp
public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailModel(IMediator mediator) => _mediator = mediator;

    public BudgetStatusDto Budget { get; set; } = default!;
    public List<TransactionDto> Transactions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Budget = await _mediator.Send(new GetBudgetByIdQuery(id));
            // Load transactions for this category in budget month
            Transactions = await _mediator.Send(new GetTransactionsQuery
            {
                CategoryId = Budget.CategoryId,
                StartDate = Budget.MonthStart,
                EndDate = Budget.MonthEnd
            });
            return Page();
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }
}
```

#### Budget vs. Actual Comparison Page

```csharp
public class ComparisonModel : PageModel
{
    private readonly IMediator _mediator;

    public ComparisonModel(IMediator mediator) => _mediator = mediator;

    public List<BudgetVsActualDto> ComparisonData { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int Year { get; set; } = DateTime.UtcNow.Year;
    [BindProperty(SupportsGet = true)] public int Month { get; set; } = DateTime.UtcNow.Month;
    public decimal TotalBudgeted => ComparisonData.Where(x => x.HasBudget).Sum(x => x.BudgetLimit ?? 0);
    public decimal TotalActual => ComparisonData.Sum(x => x.ActualSpend);
    public decimal TotalDifference => TotalBudgeted - TotalActual;

    public async Task OnGetAsync()
    {
        ComparisonData = await _mediator.Send(new GetBudgetVsActualQuery(Year, Month));
    }

    // Chart.js data serialized via System.Text.Json in the .cshtml
}
```

#### Dashboard Budget Status Widget

Update `DashboardModel.OnGetAsync()` to include:

```csharp
// Add to DashboardModel
public BudgetDashboardSummaryDto BudgetSummary { get; set; } = default!;

// In OnGetAsync():
BudgetSummary = await _mediator.Send(
    new GetBudgetSummaryForDashboardQuery(year, month));
```

#### Reusable Components

**`_BudgetProgressBar.cshtml`** — Partial view accepting `BudgetStatusDto` or similar model:
- Renders a color-coded progress bar (green/yellow/red/overage)
- Shows percentage label
- CSS caps width at 100%; overflow label shows actual percentage if > 100%

**`_BudgetStatusBadge.cshtml`** — Partial view accepting `BudgetStatusLevel`:
- Renders colored badge: "On Track" (green), "Caution" (yellow), "Warning" (red), "Over Budget" (red with icon)

#### Updated Navigation

| # | Link | Path | Condition |
|---|---|---|---|
| 1 | Dashboard | /Dashboard | Authenticated |
| 2 | Transactions | /Transactions | Authenticated |
| 3 | Add Transaction | /Transactions/Add | Authenticated |
| 4 | Import PDF | /Transactions/Import | Authenticated |
| 5 | Categories | /Categories | Authenticated |
| 6 | **Budgets** | **/Budgets** | **Authenticated (NEW)** |
| 7 | **Budget Comparison** | **/Budgets/Comparison** | **Authenticated (NEW)** |
| 8 | Search | /Transactions/Search | Authenticated |

### FR-5.06: Dependency Injection Updates

```csharp
// In Infrastructure/DependencyInjection.cs — add to AddInfrastructureServices():

// Budget repository (NEW in Phase 5)
services.AddScoped<IBudgetRepository, SupabaseBudgetRepository>();

// Budget domain service (NEW in Phase 5)
services.AddScoped<BudgetService>();
```

### FR-5.07: Architecture Flow — Budget Status Calculation

```text
[Frontend: Budget List Page]
    │
    ▼ await _mediator.Send(new GetBudgetsQuery(year, month))
    │
[Application: GetBudgetsQueryHandler]
    │
    ├──► IUserContext.GetUserId()           → UserId
    ├──► IBudgetRepository.GetByUserIdAsync(userId)  → List<Budget>
    │    (filter by month if provided)
    │
    │    For each Budget:
    │    ├──► ITransactionRepository.FindBySpecificationAsync(
    │    │        CompositeSpecification.And(
    │    │            new TransactionByUserSpecification(userId),
    │    │            new TransactionByDateRangeSpecification(budget.Month),
    │    │            new TransactionByCategorySpecification(budget.CategoryId)
    │    │        ))                          → List<Transaction>
    │    ├──► Sum transaction amounts         → Money currentSpend
    │    ├──► Budget.PercentageUsed(currentSpend) → decimal
    │    ├──► Budget.RemainingAmount(currentSpend) → Money
    │    └──► BudgetService.CalculateStatusLevel(percentageUsed) → BudgetStatusLevel
    │
    └──► Map to List<BudgetStatusDto>
         Sort by category name
         Return to Frontend
```

---

## Test Specifications

### Budget Entity Tests (Domain)

```text
TEST T-5.01: Budget_ValidConstruction_SetsAllProperties
GIVEN valid BudgetId, UserId, CategoryId, DateRange, Money(500, "EUR")
WHEN Budget constructor is called
THEN Id, UserId, CategoryId, Month, Limit are all set correctly

TEST T-5.02: Budget_NullUserId_ThrowsArgumentNullException
GIVEN null UserId
WHEN Budget constructor is called
THEN throws ArgumentNullException

TEST T-5.03: Budget_NullCategoryId_ThrowsArgumentNullException
GIVEN null CategoryId
WHEN Budget constructor is called
THEN throws ArgumentNullException

TEST T-5.04: Budget_NullMonth_ThrowsArgumentNullException
GIVEN null DateRange
WHEN Budget constructor is called
THEN throws ArgumentNullException

TEST T-5.05: Budget_NullLimit_ThrowsArgumentNullException
GIVEN null Money limit
WHEN Budget constructor is called
THEN throws ArgumentNullException

TEST T-5.06: Budget_ZeroLimit_ThrowsDomainException
GIVEN Money(0, "EUR")
WHEN Budget constructor is called
THEN throws DomainException("Budget limit must be positive")

TEST T-5.07: Budget_NegativeLimit_ThrowsDomainException
GIVEN Money(-100, "EUR")
WHEN Budget constructor is called
THEN throws DomainException("Budget limit must be positive")

TEST T-5.08: Budget_IsOverBudget_SpendExceedsLimit_ReturnsTrue
GIVEN Budget with limit €500
WHEN IsOverBudget(Money(600, "EUR")) is called
THEN returns true

TEST T-5.09: Budget_IsOverBudget_SpendBelowLimit_ReturnsFalse
GIVEN Budget with limit €500
WHEN IsOverBudget(Money(300, "EUR")) is called
THEN returns false

TEST T-5.10: Budget_IsOverBudget_SpendEqualsLimit_ReturnsFalse
GIVEN Budget with limit €500
WHEN IsOverBudget(Money(500, "EUR")) is called
THEN returns false (at limit is not over)

TEST T-5.11: Budget_PercentageUsed_CalculatesCorrectly
GIVEN Budget with limit €500
WHEN PercentageUsed(Money(250, "EUR")) is called
THEN returns 0.5

TEST T-5.12: Budget_PercentageUsed_ZeroSpend_ReturnsZero
GIVEN Budget with limit €500
WHEN PercentageUsed(Money(0, "EUR")) is called
THEN returns 0.0

TEST T-5.13: Budget_RemainingAmount_CalculatesCorrectly
GIVEN Budget with limit €500
WHEN RemainingAmount(Money(300, "EUR")) is called
THEN returns Money(200, "EUR")

TEST T-5.14: Budget_RemainingAmount_OverBudget_ReturnsNegative
GIVEN Budget with limit €500
WHEN RemainingAmount(Money(700, "EUR")) is called
THEN returns Money(-200, "EUR")

TEST T-5.15: Budget_UpdateLimit_ValidAmount_UpdatesLimit
GIVEN Budget with limit €500
WHEN UpdateLimit(Money(800, "EUR")) is called
THEN Limit is now Money(800, "EUR") and UpdatedAt is set

TEST T-5.16: Budget_UpdateLimit_ZeroAmount_ThrowsDomainException
GIVEN Budget with limit €500
WHEN UpdateLimit(Money(0, "EUR")) is called
THEN throws DomainException("Budget limit must be positive")

TEST T-5.17: Budget_UpdateLimit_NegativeAmount_ThrowsDomainException
GIVEN Budget with limit €500
WHEN UpdateLimit(Money(-100, "EUR")) is called
THEN throws DomainException("Budget limit must be positive")
```

### Application Handler Tests — Budget Commands

```text
TEST T-5.18: CreateBudget_ValidInput_ReturnsBudgetId
GIVEN valid CreateBudgetCommand with existing category
WHEN CreateBudgetCommandHandler handles the command
THEN IBudgetRepository.AddAsync is called AND returns new BudgetId Guid

TEST T-5.19: CreateBudget_DuplicateBudget_ThrowsDomainException
GIVEN BudgetService.ValidateUniqueBudget throws DomainException
WHEN CreateBudgetCommandHandler handles the command
THEN DomainException propagates with "already exists" message

TEST T-5.20: CreateBudget_InvalidCategory_ThrowsEntityNotFound
GIVEN ICategoryRepository.GetByIdAsync returns null
WHEN CreateBudgetCommandHandler handles the command
THEN throws EntityNotFoundException for the category

TEST T-5.21: CreateBudget_ZeroLimit_ThrowsDomainException
GIVEN CreateBudgetCommand with LimitAmount = 0
WHEN CreateBudgetCommandHandler handles the command
THEN throws DomainException (from Budget constructor)

TEST T-5.22: CreateBudget_NegativeLimit_ThrowsDomainException
GIVEN CreateBudgetCommand with LimitAmount = -100
WHEN CreateBudgetCommandHandler handles the command
THEN throws DomainException (from Budget constructor)

TEST T-5.23: CreateBudget_CategoryBelongsToDifferentUser_ThrowsException
GIVEN category.UserId != current user's UserId
WHEN CreateBudgetCommandHandler handles the command
THEN throws exception (tenant isolation violation)

TEST T-5.24: UpdateBudget_ValidInput_UpdatesLimit
GIVEN existing budget owned by user
WHEN UpdateBudgetCommandHandler handles UpdateBudgetCommand(budgetId, 800)
THEN Budget.UpdateLimit called AND IBudgetRepository.UpdateAsync called

TEST T-5.25: UpdateBudget_NonExistent_ThrowsEntityNotFound
GIVEN IBudgetRepository.GetByIdAsync returns null
WHEN UpdateBudgetCommandHandler handles the command
THEN throws EntityNotFoundException

TEST T-5.26: UpdateBudget_WrongUser_ThrowsException
GIVEN budget.UserId != current user
WHEN UpdateBudgetCommandHandler handles the command
THEN throws EntityNotFoundException (tenant isolation)

TEST T-5.27: UpdateBudget_ZeroLimit_ThrowsDomainException
GIVEN UpdateBudgetCommand with NewLimitAmount = 0
WHEN UpdateBudgetCommandHandler handles the command
THEN throws DomainException (from Budget.UpdateLimit)

TEST T-5.28: DeleteBudget_ValidInput_RemovesBudget
GIVEN existing budget owned by user
WHEN DeleteBudgetCommandHandler handles the command
THEN IBudgetRepository.DeleteAsync called with correct BudgetId

TEST T-5.29: DeleteBudget_NonExistent_ThrowsEntityNotFound
GIVEN IBudgetRepository.GetByIdAsync returns null
WHEN DeleteBudgetCommandHandler handles the command
THEN throws EntityNotFoundException

TEST T-5.30: DeleteBudget_WrongUser_ThrowsException
GIVEN budget.UserId != current user
WHEN DeleteBudgetCommandHandler handles the command
THEN throws EntityNotFoundException (tenant isolation)
```

### Application Handler Tests — Budget Queries

```text
TEST T-5.31: GetBudgets_ReturnsOnlyUserBudgets
GIVEN 3 budgets for user A and 2 for user B
WHEN GetBudgetsQueryHandler handles query for user A
THEN returns exactly 3 budgets

TEST T-5.32: GetBudgets_FilteredByMonth_ReturnsMonthOnly
GIVEN budgets for Jan, Feb, Mar
WHEN GetBudgetsQuery with Month = 2
THEN returns only February budgets

TEST T-5.33: GetBudgets_CalculatesCurrentSpendCorrectly
GIVEN Budget for Groceries €500 and 3 transactions (€100 + €150 + €75)
WHEN GetBudgetsQueryHandler handles the query
THEN CurrentSpend == €325, RemainingAmount == €175, PercentageUsed == 0.65

TEST T-5.34: GetBudgets_NoBudgets_ReturnsEmptyList
GIVEN user has no budgets
WHEN GetBudgetsQueryHandler handles the query
THEN returns empty list

TEST T-5.35: GetBudgets_OverBudget_StatusIsOverage
GIVEN Budget with limit €200 and spend €300
WHEN GetBudgetsQueryHandler handles the query
THEN StatusLevel == BudgetStatusLevel.Overage AND PercentageUsed == 1.5

TEST T-5.36: GetBudgetById_Exists_ReturnsDetailedStatus
GIVEN existing budget with transactions
WHEN GetBudgetByIdQueryHandler handles the query
THEN returns BudgetStatusDto with correct calculations

TEST T-5.37: GetBudgetById_WrongUser_ThrowsException
GIVEN budget owned by different user
WHEN GetBudgetByIdQueryHandler handles the query
THEN throws EntityNotFoundException

TEST T-5.38: GetBudgetById_NonExistent_ThrowsException
GIVEN non-existent budget ID
WHEN GetBudgetByIdQueryHandler handles the query
THEN throws EntityNotFoundException
```

### Application Handler Tests — Budget vs. Actual & Dashboard

```text
TEST T-5.39: GetBudgetVsActual_ReturnsComparisonForMonth
GIVEN 2 budgets and 3 categories with spending
WHEN GetBudgetVsActualQueryHandler handles the query
THEN returns 3 items (2 budgeted + 1 unbudgeted)

TEST T-5.40: GetBudgetVsActual_NoTransactions_ShowsZeroActual
GIVEN budget for Groceries but no grocery transactions
WHEN GetBudgetVsActualQueryHandler handles the query
THEN ActualSpend == 0 for that category

TEST T-5.41: GetBudgetVsActual_NoBudgets_ShowsUnbudgetedSpending
GIVEN no budgets but 2 categories with spending
WHEN GetBudgetVsActualQueryHandler handles the query
THEN returns 2 items with HasBudget == false AND BudgetLimit == null

TEST T-5.42: GetBudgetVsActual_SortedOverBudgetFirst
GIVEN 3 budgets: 1 over (120%), 1 warning (85%), 1 on track (40%)
WHEN GetBudgetVsActualQueryHandler handles the query
THEN items are sorted: over budget first, then by percentage descending

TEST T-5.43: GetBudgetSummaryForDashboard_CalculatesAggregates
GIVEN 5 budgets: 3 green, 1 red, 1 overage
WHEN GetBudgetSummaryForDashboardQueryHandler handles the query
THEN TotalBudgets == 5, OnTrackCount == 3, WarningCount == 1, OverageCount == 1

TEST T-5.44: GetBudgetSummaryForDashboard_NoBudgets_ReturnsZeros
GIVEN user has no budgets for the month
WHEN GetBudgetSummaryForDashboardQueryHandler handles the query
THEN TotalBudgets == 0, OnTrackCount == 0, WarningCount == 0, OverageCount == 0
AND Items is empty list

TEST T-5.45: GetBudgetSummaryForDashboard_AllOnTrack
GIVEN 3 budgets all under 60% spending
WHEN GetBudgetSummaryForDashboardQueryHandler handles the query
THEN OnTrackCount == 3, WarningCount == 0, OverageCount == 0
```

### Domain Service Tests — BudgetService

```text
TEST T-5.46: BudgetService_ValidateUniqueBudget_Duplicate_Throws
GIVEN IBudgetRepository.GetByUserAndCategoryAndMonthAsync returns existing budget
WHEN ValidateUniqueBudget is called
THEN throws DomainException with message containing "already exists"

TEST T-5.47: BudgetService_ValidateUniqueBudget_NoDuplicate_NoException
GIVEN IBudgetRepository.GetByUserAndCategoryAndMonthAsync returns null
WHEN ValidateUniqueBudget is called
THEN no exception is thrown

TEST T-5.48: BudgetService_CalculateStatusLevel_Under60_Green
GIVEN percentageUsed = 0.45
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Green

TEST T-5.49: BudgetService_CalculateStatusLevel_60To80_Yellow
GIVEN percentageUsed = 0.70
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Yellow

TEST T-5.50: BudgetService_CalculateStatusLevel_80To100_Red
GIVEN percentageUsed = 0.90
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Red

TEST T-5.51: BudgetService_CalculateStatusLevel_Over100_Overage
GIVEN percentageUsed = 1.25
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Overage

TEST T-5.52: BudgetService_CalculateStatusLevel_Exactly60_Green
GIVEN percentageUsed = 0.60
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Green (threshold is > 0.6, not >=)

TEST T-5.53: BudgetService_CalculateStatusLevel_Exactly80_Yellow
GIVEN percentageUsed = 0.80
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Yellow (threshold is > 0.8, not >=)

TEST T-5.54: BudgetService_CalculateStatusLevel_Exactly100_Red
GIVEN percentageUsed = 1.0
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Red (threshold is > 1.0 for Overage)

TEST T-5.55: BudgetService_CalculateStatusLevel_Zero_Green
GIVEN percentageUsed = 0.0
WHEN CalculateStatusLevel is called
THEN returns BudgetStatusLevel.Green
```

---

## Test Summary

| Test ID | Test Name | Category | Area |
|---------|-----------|----------|------|
| T-5.01 | Budget_ValidConstruction_SetsAllProperties | Domain | Budget Entity |
| T-5.02 | Budget_NullUserId_ThrowsArgumentNullException | Domain | Budget Entity |
| T-5.03 | Budget_NullCategoryId_ThrowsArgumentNullException | Domain | Budget Entity |
| T-5.04 | Budget_NullMonth_ThrowsArgumentNullException | Domain | Budget Entity |
| T-5.05 | Budget_NullLimit_ThrowsArgumentNullException | Domain | Budget Entity |
| T-5.06 | Budget_ZeroLimit_ThrowsDomainException | Domain | Budget Entity |
| T-5.07 | Budget_NegativeLimit_ThrowsDomainException | Domain | Budget Entity |
| T-5.08 | Budget_IsOverBudget_SpendExceedsLimit_ReturnsTrue | Domain | Budget Entity |
| T-5.09 | Budget_IsOverBudget_SpendBelowLimit_ReturnsFalse | Domain | Budget Entity |
| T-5.10 | Budget_IsOverBudget_SpendEqualsLimit_ReturnsFalse | Domain | Budget Entity |
| T-5.11 | Budget_PercentageUsed_CalculatesCorrectly | Domain | Budget Entity |
| T-5.12 | Budget_PercentageUsed_ZeroSpend_ReturnsZero | Domain | Budget Entity |
| T-5.13 | Budget_RemainingAmount_CalculatesCorrectly | Domain | Budget Entity |
| T-5.14 | Budget_RemainingAmount_OverBudget_ReturnsNegative | Domain | Budget Entity |
| T-5.15 | Budget_UpdateLimit_ValidAmount_UpdatesLimit | Domain | Budget Entity |
| T-5.16 | Budget_UpdateLimit_ZeroAmount_ThrowsDomainException | Domain | Budget Entity |
| T-5.17 | Budget_UpdateLimit_NegativeAmount_ThrowsDomainException | Domain | Budget Entity |
| T-5.18 | CreateBudget_ValidInput_ReturnsBudgetId | Application | Budget Commands |
| T-5.19 | CreateBudget_DuplicateBudget_ThrowsDomainException | Application | Budget Commands |
| T-5.20 | CreateBudget_InvalidCategory_ThrowsEntityNotFound | Application | Budget Commands |
| T-5.21 | CreateBudget_ZeroLimit_ThrowsDomainException | Application | Budget Commands |
| T-5.22 | CreateBudget_NegativeLimit_ThrowsDomainException | Application | Budget Commands |
| T-5.23 | CreateBudget_CategoryBelongsToDifferentUser_ThrowsException | Application | Budget Commands |
| T-5.24 | UpdateBudget_ValidInput_UpdatesLimit | Application | Budget Commands |
| T-5.25 | UpdateBudget_NonExistent_ThrowsEntityNotFound | Application | Budget Commands |
| T-5.26 | UpdateBudget_WrongUser_ThrowsException | Application | Budget Commands |
| T-5.27 | UpdateBudget_ZeroLimit_ThrowsDomainException | Application | Budget Commands |
| T-5.28 | DeleteBudget_ValidInput_RemovesBudget | Application | Budget Commands |
| T-5.29 | DeleteBudget_NonExistent_ThrowsEntityNotFound | Application | Budget Commands |
| T-5.30 | DeleteBudget_WrongUser_ThrowsException | Application | Budget Commands |
| T-5.31 | GetBudgets_ReturnsOnlyUserBudgets | Application | Budget Queries |
| T-5.32 | GetBudgets_FilteredByMonth_ReturnsMonthOnly | Application | Budget Queries |
| T-5.33 | GetBudgets_CalculatesCurrentSpendCorrectly | Application | Budget Queries |
| T-5.34 | GetBudgets_NoBudgets_ReturnsEmptyList | Application | Budget Queries |
| T-5.35 | GetBudgets_OverBudget_StatusIsOverage | Application | Budget Queries |
| T-5.36 | GetBudgetById_Exists_ReturnsDetailedStatus | Application | Budget Queries |
| T-5.37 | GetBudgetById_WrongUser_ThrowsException | Application | Budget Queries |
| T-5.38 | GetBudgetById_NonExistent_ThrowsException | Application | Budget Queries |
| T-5.39 | GetBudgetVsActual_ReturnsComparisonForMonth | Application | Budget vs Actual |
| T-5.40 | GetBudgetVsActual_NoTransactions_ShowsZeroActual | Application | Budget vs Actual |
| T-5.41 | GetBudgetVsActual_NoBudgets_ShowsUnbudgetedSpending | Application | Budget vs Actual |
| T-5.42 | GetBudgetVsActual_SortedOverBudgetFirst | Application | Budget vs Actual |
| T-5.43 | GetBudgetSummaryForDashboard_CalculatesAggregates | Application | Dashboard Summary |
| T-5.44 | GetBudgetSummaryForDashboard_NoBudgets_ReturnsZeros | Application | Dashboard Summary |
| T-5.45 | GetBudgetSummaryForDashboard_AllOnTrack | Application | Dashboard Summary |
| T-5.46 | BudgetService_ValidateUniqueBudget_Duplicate_Throws | Domain | BudgetService |
| T-5.47 | BudgetService_ValidateUniqueBudget_NoDuplicate_NoException | Domain | BudgetService |
| T-5.48 | BudgetService_CalculateStatusLevel_Under60_Green | Domain | BudgetService |
| T-5.49 | BudgetService_CalculateStatusLevel_60To80_Yellow | Domain | BudgetService |
| T-5.50 | BudgetService_CalculateStatusLevel_80To100_Red | Domain | BudgetService |
| T-5.51 | BudgetService_CalculateStatusLevel_Over100_Overage | Domain | BudgetService |
| T-5.52 | BudgetService_CalculateStatusLevel_Exactly60_Green | Domain | BudgetService |
| T-5.53 | BudgetService_CalculateStatusLevel_Exactly80_Yellow | Domain | BudgetService |
| T-5.54 | BudgetService_CalculateStatusLevel_Exactly100_Red | Domain | BudgetService |
| T-5.55 | BudgetService_CalculateStatusLevel_Zero_Green | Domain | BudgetService |

**Total: 55 tests (28 Application + 27 Domain)**

**Tests by Area:**

| Area | Test Count | Test IDs |
|------|------------|----------|
| Budget Entity | 17 | T-5.01–T-5.17 |
| Budget Commands | 13 | T-5.18–T-5.30 |
| Budget Queries | 8 | T-5.31–T-5.38 |
| Budget vs Actual | 4 | T-5.39–T-5.42 |
| Dashboard Summary | 3 | T-5.43–T-5.45 |
| BudgetService | 10 | T-5.46–T-5.55 |

---

## Deliverables

| # | Deliverable | Layer | Acceptance |
|---|---|---|---|
| D-5.01 | `Budget` aggregate root entity | Domain | Tests T-5.01–T-5.17 pass |
| D-5.02 | `IBudgetRepository` interface | Domain | Compiles; used by BudgetService, handlers, and Infrastructure |
| D-5.03 | `BudgetService` domain service | Domain | Tests T-5.46–T-5.55 pass |
| D-5.04 | `BudgetStatusLevel` enum | Domain | Used by BudgetService and DTOs |
| D-5.05 | `CreateBudgetCommand` + handler | Application | Tests T-5.18–T-5.23 pass |
| D-5.06 | `UpdateBudgetCommand` + handler | Application | Tests T-5.24–T-5.27 pass |
| D-5.07 | `DeleteBudgetCommand` + handler | Application | Tests T-5.28–T-5.30 pass |
| D-5.08 | `GetBudgetsQuery` + handler | Application | Tests T-5.31–T-5.35 pass |
| D-5.09 | `GetBudgetByIdQuery` + handler | Application | Tests T-5.36–T-5.38 pass |
| D-5.10 | `GetBudgetVsActualQuery` + handler | Application | Tests T-5.39–T-5.42 pass |
| D-5.11 | `GetBudgetSummaryForDashboardQuery` + handler | Application | Tests T-5.43–T-5.45 pass |
| D-5.12 | Budget DTOs (BudgetDto, BudgetStatusDto, BudgetVsActualDto, BudgetDashboardSummaryDto) | Application | Compile; used by handlers and frontend |
| D-5.13 | `SupabaseBudgetRepository` | Infrastructure | Implements all `IBudgetRepository` methods |
| D-5.14 | Database migration: `006_CreateBudgetsTable.sql` | Infrastructure | Table + unique constraint + indexes + RLS policies applied |
| D-5.15 | Budget list page (`/Budgets`) | Frontend | Lists budgets with status, edit/delete actions |
| D-5.16 | Create budget page (`/Budgets/Create`) | Frontend | Form → validation → redirect |
| D-5.17 | Budget detail page (`/Budgets/{id}`) | Frontend | Progress bar + transactions in category |
| D-5.18 | Budget vs. actual comparison page (`/Budgets/Comparison`) | Frontend | Table + bar chart + month selector |
| D-5.19 | Dashboard budget status widget | Frontend | Mini progress bars + summary line on dashboard |
| D-5.20 | `_BudgetProgressBar.cshtml` reusable component | Frontend | Color-coded progress bar used across pages |
| D-5.21 | `_BudgetStatusBadge.cshtml` reusable component | Frontend | Status badge (Green/Yellow/Red/Overage) used across pages |
| D-5.22 | Updated `_Layout.cshtml` navigation | Frontend | Budgets + Comparison links added |
| D-5.23 | Updated Infrastructure `DependencyInjection.cs` | Infrastructure | BudgetRepository + BudgetService registered |
| D-5.24 | Domain.Tests for Budget entity (17 tests) | Tests | `dotnet test --filter Category=Domain` all green |
| D-5.25 | Domain.Tests for BudgetService (10 tests) | Tests | `dotnet test --filter Category=Domain` all green |
| D-5.26 | Application.Tests for budget handlers (28 tests) | Tests | `dotnet test --filter Category=Application` all green |

---

## Success Criteria

| # | Criterion | Metric |
|---|---|---|
| SC-5.1 | User can create a budget for a category and month | E2E: Create form → submit → appears in budget list |
| SC-5.2 | Duplicate budget creation is rejected with descriptive error | Same category + month → "already exists" error displayed |
| SC-5.3 | User can update a budget's spending limit | Edit limit → save → new limit shown in list |
| SC-5.4 | User can delete a budget | Delete → confirm → removed from list; transactions unaffected |
| SC-5.5 | Budget list shows correct current spend and status | Import transactions → budget list reflects actual spending |
| SC-5.6 | Dashboard budget widget shows color-coded status indicators | Green/Yellow/Red/Overage badges correctly reflect spending vs. limit |
| SC-5.7 | Budget vs. actual comparison page shows all categories | Categories with and without budgets both visible |
| SC-5.8 | Budget detail page shows transactions in that category for the month | Click budget → see progress bar + transaction list |
| SC-5.9 | Over-budget state is visually distinct | Overage styling (red with warning icon) clearly visible |
| SC-5.10 | Status thresholds are correct (green < 60%, yellow 60-80%, red 80-100%, overage > 100%) | Verified via domain service tests T-5.48–T-5.55 |
| SC-5.11 | All data scoped to current user (tenant isolation) | User A cannot see or modify User B's budgets |
| SC-5.12 | Budget deletion does not affect transactions | Delete budget → transactions still exist with same categories |
| SC-5.13 | Budget entity enforces invariants (positive limit, non-null fields) | Verified via domain entity tests T-5.01–T-5.17 |
| SC-5.14 | All Phase 5 tests pass (55 tests) | `dotnet test` all green |
| SC-5.15 | All prior phase tests still pass (no regressions) | `dotnet test` → Phase 0 + 1 + 2 + 3 + 4 + 5 all green |
| SC-5.16 | Application layer test coverage ≥ 70% | Coverage report on Application project |
| SC-5.17 | Domain test coverage ≥ 80% | Coverage report on Domain project (cumulative) |
| SC-5.18 | RLS policies verified on budgets table | Two users cannot see each other's budgets |
| SC-5.19 | Unique constraint prevents duplicate budgets at DB level | Direct insert of duplicate → constraint violation |

---

## Assumptions

1. **Phases 0–4 are fully implemented and tested.** All foundation, auth, domain model, transaction CRUD, category management, PDF import, analytics dashboard, and search are stable. Current test count: 89 (37 Domain + 52 Application).
2. **Budget entity does NOT exist yet.** Phase 2 spec planned a Budget aggregate root with `IsOverBudget`, `PercentageUsed`, `RemainingAmount`, and `UpdateLimit` methods, but it was **never implemented**. Only the `BudgetId` value object was created. Phase 5 absorbs this deliverable.
3. **`IBudgetRepository` interface does NOT exist yet.** Phase 2 spec planned the repository interface, but it was never created. Phase 5 absorbs this deliverable.
4. **`BudgetId` value object from Phase 2 IS implemented** at `Domain/ValueObjects/BudgetId.cs` with empty Guid guard.
5. **`DateRange` value object from Phase 2 IS implemented** at `Domain/ValueObjects/DateRange.cs` with start/end date validation.
6. **`Money` value object from Phase 2 IS implemented** at `Domain/ValueObjects/Money.cs` with Plus/Minus arithmetic, currency validation, and equality.
7. **Current spend is calculated from transactions at query time** (not cached). For MVP, this is acceptable given the 1000-row MaxResults default per specification query.
8. **Budget month boundaries** are first day of month (00:00:00 UTC) to last day of month (23:59:59 UTC). `DateRange` value object handles this.
9. **Currency is EUR for all budgets** (consistent with Money value object default). Multi-currency budgets deferred to post-MVP.
10. **Chart.js is already available** in the layout (added in Phase 4). The budget vs. actual page reuses the same Chart.js CDN.
11. **Alpine.js is available** for interactive components (confirmation dialogs, inline edit) — added in Phase 3.
12. **`BudgetService` is a domain service** registered in the DI container and injected into Application handlers. It depends only on `IBudgetRepository` (domain interface).
13. **The `BudgetStatusLevel` enum lives in the Domain layer** because it represents a domain concept (budget health). It is used by both the domain service and application DTOs.
14. **Budget deletion has no cascading effects on transactions.** Deleting a budget does not change any transactions. Budgets are purely a tracking/limit overlay.
15. **The "Budgets/Comparison" page includes categories without budgets** to give a complete picture of spending. These rows show actual spending with "No budget" in the limit column.
16. **Migration number is 006** (not 005). Migration `005_CreateUserProfileTrigger.sql` already exists from a previous phase.

---

## Risks & Mitigations

| ID | Risk | Impact | Probability | Mitigation |
|---|---|---|---|---|
| R-5.1 | Current spend calculation slow for users with many transactions | Medium | Medium | Specification limits to 1000 rows; post-MVP: cache spend or use Supabase aggregate RPC |
| R-5.2 | Budget status widget slows dashboard load (additional queries) | Medium | Medium | Dashboard already has 5+ queries; budget summary adds 1 more. Parallelize with `Task.WhenAll` if needed |
| R-5.3 | Month boundary edge cases (timezone-related) | Low | Medium | Use `DateTime.UtcNow` consistently; DateRange handles boundaries; test month start/end explicitly |
| R-5.4 | Unique constraint race condition (two simultaneous creates) | Low | Low | DB `UNIQUE` constraint catches at DB level; handler validates first for better UX error message |
| R-5.5 | BudgetStatusLevel threshold confusion (inclusive vs. exclusive boundaries) | Low | Medium | Explicitly tested in T-5.52, T-5.53, T-5.54; documented thresholds use `>` (not `>=`) |
| R-5.6 | Budget vs. actual page empty for months with no data | Low | Medium | Show informational message: "No spending data for this month" |
| R-5.7 | Progress bar visual overflow when > 100% | Low | Low | CSS `max-width: 100%` with overflow label showing actual percentage |
| R-5.8 | Budget comparison bar chart unreadable with many categories | Low | Medium | Limit to 10 categories in chart; show full data in table |
| R-5.9 | `ON DELETE CASCADE` on category_id FK deletes budget when category is deleted | Medium | Low | Category deletion already blocked if has active transactions (Phase 3); budget deletion is separate |
| R-5.10 | Budget entity backlog from Phase 2 increases Phase 5 scope | Medium | High | Mitigated: Budget entity is a well-defined aggregate root with clear tests; adds ~2 days to Phase 5 timeline |

---

## Implementation Notes

### Recommended Implementation Order

```text
Step 1: Write Domain.Tests for Budget entity (RED phase)
└── Tests T-5.01–T-5.17
└── Verify: tests FAIL (red) — Budget entity doesn't exist yet

Step 2: Implement Budget entity + IBudgetRepository interface (GREEN phase)
└── Domain/Entities/Budget.cs
└── Domain/Repositories/IBudgetRepository.cs
└── Verify: dotnet test --filter Category=Domain — Budget entity tests GREEN

Step 3: Write Domain.Tests for BudgetService (RED phase)
└── Tests T-5.46–T-5.55
└── Mock: IBudgetRepository with Moq
└── Verify: tests FAIL (red)

Step 4: Implement BudgetService + BudgetStatusLevel (GREEN phase)
└── Domain/Services/BudgetService.cs
└── Domain/ValueObjects/BudgetStatusLevel.cs
└── Verify: dotnet test --filter Category=Domain — all new tests GREEN

Step 5: Write Application.Tests for Budget commands (RED phase)
└── Tests T-5.18–T-5.30
└── Mock: IBudgetRepository, ICategoryRepository, IUserContext, BudgetService
└── Verify: tests FAIL (red)

Step 6: Implement Budget commands + handlers (GREEN phase)
└── CreateBudgetCommand, UpdateBudgetCommand, DeleteBudgetCommand
└── Verify: dotnet test --filter Category=Application — command tests GREEN

Step 7: Write Application.Tests for Budget queries (RED phase)
└── Tests T-5.31–T-5.38
└── Mock: IBudgetRepository, ITransactionRepository, IUserContext, BudgetService
└── Verify: tests FAIL (red)

Step 8: Implement Budget queries + handlers (GREEN phase)
└── GetBudgetsQuery, GetBudgetByIdQuery
└── BudgetDto, BudgetStatusDto
└── Verify: tests GREEN

Step 9: Write Application.Tests for Budget vs Actual + Dashboard summary (RED phase)
└── Tests T-5.39–T-5.45
└── Verify: tests FAIL (red)

Step 10: Implement Budget vs Actual + Dashboard summary handlers (GREEN phase)
└── GetBudgetVsActualQuery, GetBudgetSummaryForDashboardQuery
└── BudgetVsActualDto, BudgetDashboardSummaryDto
└── Verify: dotnet test --filter Category=Application — all budget tests GREEN

Step 11: Apply database migration to Supabase
└── Run 006_CreateBudgetsTable.sql
└── Verify: table + unique constraint + indexes + RLS policies in Supabase dashboard

Step 12: Implement Infrastructure repository
└── SupabaseBudgetRepository
└── Update DependencyInjection.cs (register repository + BudgetService)
└── Verify: manual CRUD tests against Supabase

Step 13: Implement Frontend — Budget management pages
└── /Budgets — list with status indicators
└── /Budgets/Create — create form
└── /Budgets/{id} — detail with progress bar + transactions
└── Reusable components: _BudgetProgressBar, _BudgetStatusBadge
└── Verify: manual E2E test

Step 14: Implement Frontend — Budget vs. actual comparison page
└── /Budgets/Comparison — table + bar chart
└── Month selector
└── Chart.js horizontal bar chart
└── Verify: manual E2E test

Step 15: Implement Dashboard budget status widget
└── Update Dashboard.cshtml + DashboardModel
└── Add GetBudgetSummaryForDashboardQuery call
└── Mini progress bars + summary line
└── Verify: widget appears on dashboard with correct data

Step 16: Update navigation
└── _Layout.cshtml: add Budgets + Comparison links
└── Verify: navigation works on all pages

Step 17: End-to-end validation
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

Step 18: Final test + coverage validation
└── dotnet build → zero errors, zero warnings
└── dotnet test → ALL tests green (Phase 0 + 1 + 2 + 3 + 4 + 5)
└── Domain coverage ≥ 80% (cumulative)
└── Application coverage ≥ 70%
└── Audit: no forbidden layer references
```

### Spec-Driven Workflow Compliance

| Step | Workflow Stage | Phase 5 Action |
|------|---------------|----------------|
| 1 | Write Test Spec | Tests written first (Steps 1, 3, 5, 7, 9) |
| 2 | Define Handler Stub | MediatR commands/queries defined (Steps 6, 8, 10) |
| 3 | Build Domain | Budget entity + BudgetService + BudgetStatusLevel (Steps 2, 4) |
| 4 | Implement Persistence | SupabaseBudgetRepository + migration (Steps 11, 12) |
| 5 | Wire UI | Budget pages + dashboard widget (Steps 13–16) |
| 6 | End-to-end Test | Full budget workflow validation (Step 17) |

### NuGet Packages

No new NuGet packages required. All dependencies (MediatR, Supabase, xUnit, Moq) already available from Phase 0–4.

### Layer Dependencies

| Source Layer | Target Layer | Dependency | Method |
|---|---|---|---|
| Frontend | Application | Budget Commands/Queries | `IMediator.Send()` |
| Application | Domain | Budget entity, IBudgetRepository, BudgetService, BudgetStatusLevel | Direct reference |
| Infrastructure | Domain | IBudgetRepository | Implements interface |

### Testing Patterns Used in This Phase

| Pattern | Description | Example |
|---------|-------------|---------|
| Entity Construction Test | Verify entity constructor enforces all invariants | Budget(null userId) → ArgumentNullException |
| Entity Behavior Test | Verify entity methods calculate correctly | Budget.PercentageUsed(€250 of €500) → 0.5 |
| Spend Calculation Test | Verify handler correctly sums transactions for budget category + month | 3 groceries txns (€100+€150+€75) → currentSpend = €325 |
| Status Level Boundary Test | Verify status thresholds at exact boundary values | 60% → Green (not Yellow); 80% → Yellow (not Red) |
| Uniqueness Validation Test | Verify duplicate budget rejected with descriptive error | Same category + month → DomainException "already exists" |
| Comparison Aggregation Test | Verify budget vs. actual includes budgeted and unbudgeted categories | 3 budgets + 1 unbudgeted category → 4 comparison items |
| Dashboard Summary Test | Verify aggregated counts (on track, warning, overage) | 5 budgets → 3 on track, 1 warning, 1 overage |
| Tenant Isolation Test | Verify budget operations reject wrong-user access | Budget owned by user B → EntityNotFoundException for user A |
| Cascade Safety Test | Verify budget deletion doesn't affect transactions | Delete budget → transactions still exist |

### Budget Status Visual Reference

```text
┌──────────────────────────────────────────────────────────┐
│ 🟢 Green (< 60%)                                        │
│ ████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░ 45% — On Track  │
├──────────────────────────────────────────────────────────┤
│ 🟡 Yellow (60–80%)                                       │
│ █████████████████████████░░░░░░░░░░░░░░░ 70% — Caution   │
├──────────────────────────────────────────────────────────┤
│ 🔴 Red (80–100%)                                         │
│ █████████████████████████████████████░░░░ 92% — Warning   │
├──────────────────────────────────────────────────────────┤
│ ⚠️ Overage (> 100%)                                      │
│ ████████████████████████████████████████ 125% — OVER!     │
│ ⚠️ Over budget by €50.00                                 │
└──────────────────────────────────────────────────────────┘
```

### Security Considerations

- All budget operations verify `UserId` from `IUserContext` (handler-level tenant isolation)
- RLS policies on budgets table (belt-and-suspenders with handler-level checks)
- `ON DELETE CASCADE` on `user_id` FK — user deletion cleans up all budgets
- `ON DELETE CASCADE` on `category_id` FK — category deletion cleans up associated budgets
- Unique constraint `(user_id, category_id, month_start)` prevents duplicate budgets at DB level
- Budget limit validated as positive in domain entity constructor + `UpdateLimit` method
- Alpine.js confirmation dialog prevents accidental budget deletes
- Budget comparison page does not leak other users' data (query scoped to current user)
- No Supabase service key exposed in frontend (only anon key)

---

## Cumulative Test Count (Phases 0–5)

> **Note**: These counts reflect the **actual** codebase state as of 2026-02-20 (89 tests through Phase 4), not the originally planned counts from earlier specs.

| Phase | Domain Tests | Application Tests | Total Phase | Cumulative Total |
|-------|--------------|-------------------|-------------|------------------|
| Phase 0–4 (actual) | 37 | 52 | 89 | 89 |
| Phase 5 (planned) | 27 | 28 | 55 | 144 |

**After Phase 5**: 64 Domain tests + 80 Application tests = **144 total tests**

---

_Phase Spec Version: 2.0.0 | Created: 2026-02-15 | Updated: 2026-02-20 | Aligned with Constitution v1.1.0_
_Changes in v2.0.0: Added Budget entity + IBudgetRepository (undelivered from Phase 2); fixed migration number 005→006; corrected test counts to match actual codebase (89 current); removed redundant GetBudgetStatusQuery; added user story priorities and edge cases; fixed success criteria formatting._
