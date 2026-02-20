````markdown
# Phase 5 Implementation Plan

**Version**: 1.0.0  
**Created**: 2026-02-20  
**Aligned with**: Constitution v1.1.0, Phase 5 Spec v1.0.0, Full Spec v1.0.0  
**Duration**: Weeks 19–21  
**Goal**: Budget CRUD, overage detection, visual alerts on dashboard, budget vs. actual reporting

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Implementation Phases](#implementation-phases)
- [Task Breakdown by Component](#task-breakdown-by-component)
- [Dependency Graph](#dependency-graph)
- [Red-Green-Refactor Workflow](#red-green-refactor-workflow)
- [Validation Checkpoints](#validation-checkpoints)
- [Risk Mitigation](#risk-mitigation)

---

## Executive Summary

Phase 5 delivers **Budget Management & Alerts** on top of the MVP foundation (Phases 0–4). This is a **Full-Stack (Features)** phase with all layers in scope. Phase 5 absorbs the Budget entity, IBudgetRepository, and Budget entity tests that were planned in Phase 2 but **never implemented** (only `BudgetId` value object exists).

**Key Deliverables:**
- ✅ `Budget` aggregate root entity (constructor, `IsOverBudget`, `PercentageUsed`, `RemainingAmount`, `UpdateLimit`)
- ✅ `IBudgetRepository` interface (6 methods: GetByIdAsync, GetByUserIdAsync, GetByUserAndCategoryAndMonthAsync, AddAsync, UpdateAsync, DeleteAsync)
- ✅ `BudgetService` domain service (uniqueness validation, status level calculation)
- ✅ `BudgetStatusLevel` enum (Green, Yellow, Red, Overage)
- ✅ 7 Application commands/queries with handlers: CreateBudget, UpdateBudget, DeleteBudget, GetBudgets, GetBudgetById, GetBudgetVsActual, GetBudgetSummaryForDashboard
- ✅ 4 Application DTOs: `BudgetDto`, `BudgetStatusDto`, `BudgetVsActualDto`, `BudgetDashboardSummaryDto`
- ✅ `SupabaseBudgetRepository` (implements `IBudgetRepository`)
- ✅ Database migration: `006_CreateBudgetsTable.sql` with indexes, unique constraint, RLS
- ✅ Budget management page (`/Budgets`) — create, edit, delete
- ✅ Budget detail page (`/Budgets/{id}`) — single budget with spend progress
- ✅ Budget vs. actual page (`/Budgets/Comparison`) — all budgets for a month
- ✅ Dashboard budget status widget (green/yellow/red indicators)
- ✅ Reusable components: `_BudgetProgressBar.cshtml`, `_BudgetStatusBadge.cshtml`
- ✅ Updated `_Layout.cshtml` navigation with budget links
- ✅ ≥57 passing tests (19 Domain Budget entity + 10 Domain BudgetService + 28 Application handler)
- ✅ Cumulative ~243 tests all green (Phase 0–5)

**Key Constraint**: Current spend is calculated from transactions at query time (not denormalized). Budget status thresholds: Green < 60%, Yellow 60–80%, Red 80–100%, Overage > 100%. No new NuGet packages required.

**Constitutional Compliance:**
- ✅ Clean Architecture: Domain = 0 dependencies; Application → Domain only; Infrastructure → Domain only
- ✅ CQRS: 3 Commands + 4 Queries routed through MediatR pipeline
- ✅ DDD: Budget aggregate root with invariants; BudgetService for cross-entity logic; strong-typed IDs; repository interfaces
- ✅ Test-First: ≥55 tests written before code (Red-Green-Refactor); 27 Domain + 28 Application
- ✅ Spec-Driven: Single phase spec; layer boundaries respected (all layers in scope)

---

## Implementation Phases

### Phase 5A: Domain Layer — Budget Entity & BudgetStatusLevel (Days 1–3)
Create `Budget` aggregate root with `BudgetId`, `UserId`, `CategoryId`, `DateRange`, `Money` (limit). Add `BudgetStatusLevel` enum. Implement mutation methods: `IsOverBudget`, `PercentageUsed`, `RemainingAmount`, `UpdateLimit`. Write 17 unit tests.

### Phase 5B: Domain Layer — IBudgetRepository & BudgetService (Days 3–4)
Define `IBudgetRepository` interface with 6 async methods. Create `BudgetService` domain service for uniqueness validation and status level calculation. Write 10 unit tests.

### Phase 5C: Application Layer — Budget DTOs (Days 4–5)
Define `BudgetDto`, `BudgetStatusDto`, `BudgetVsActualDto`, `BudgetDashboardSummaryDto`.

### Phase 5D: Application Layer — Budget Command Handlers (Days 5–8)
Implement `CreateBudgetCommand`, `UpdateBudgetCommand`, `DeleteBudgetCommand` handlers with tenant scoping and uniqueness enforcement. Write 12 tests.

### Phase 5E: Application Layer — Budget Query Handlers (Days 8–11)
Implement `GetBudgetsQuery`, `GetBudgetByIdQuery`, `GetBudgetVsActualQuery`, `GetBudgetSummaryForDashboardQuery` handlers. Write 16 tests.

### Phase 5F: Infrastructure — Database Migration & Repository (Days 11–13)
Apply `006_CreateBudgetsTable.sql` migration with indexes, unique constraint (`user_id, category_id, period_start`), and RLS. Implement `SupabaseBudgetRepository`.

### Phase 5G: Frontend — Budget Management Pages (Days 13–16)
Build Budget list, create, edit, delete pages at `/Budgets`. Build Budget detail page at `/Budgets/{id}`. Build Budget vs. actual comparison page at `/Budgets/Comparison`.

### Phase 5H: Frontend — Dashboard Widget & Shared Components (Days 16–18)
Add budget status widget to Dashboard page. Create `_BudgetProgressBar.cshtml` and `_BudgetStatusBadge.cshtml` partials. Update `_Layout.cshtml` navigation.

### Phase 5I: Integration & Validation (Days 18–21)
E2E testing, coverage reporting, all ~243 tests passing, budget workflow validation.

---

## Task Breakdown by Component

### 0. PRE-IMPLEMENTATION

#### 0.1: Environment Validation

**Task**: Verify Phase 0–4 completion and Phase 5 readiness

```sh
✓ Phase 0 build passing         # dotnet build
✓ Phase 0 tests passing         # 13 tests green
✓ Phase 1 build passing         # dotnet build
✓ Phase 1 tests passing         # 22 tests green
✓ Phase 2 build passing         # dotnet build
✓ Phase 2 tests passing         # 81 tests green (domain-only)
✓ Phase 3 build passing         # dotnet build
✓ Phase 3 tests passing         # 38 tests green (full-stack)
✓ Phase 4 build passing         # dotnet build
✓ Phase 4 tests passing         # 32 tests green (full-stack, MVP)
✓ Total tests passing           # ~186 tests green
✓ Domain project zero deps      # Verify Domain.csproj has NO external packages
✓ BudgetId VO exists            # Phase 2 delivered BudgetId(Guid) value object
✓ Budget entity NOT exists      # Confirm Budget entity was never implemented
✓ IBudgetRepository NOT exists  # Confirm repository interface was never implemented
✓ Supabase Auth + RLS working   # Phase 1+3 auth/persistence functional
✓ Dashboard w/ Chart.js         # Phase 4 dashboard with analytics is functional
✓ Git workspace clean           # Phase 4 merged to main
```

**Acceptance Criteria:**
- Phase 0 + Phase 1 + Phase 2 + Phase 3 + Phase 4 combined tests pass (~186 tests total)
- Domain layer has ZERO external NuGet dependencies
- `BudgetId` value object exists in `Domain/ValueObjects/BudgetId.cs`
- No `Budget` entity, `IBudgetRepository`, or `BudgetService` exists yet
- All Phase 4 analytics and dashboard are stable
- Git workspace is clean (ready for Phase 5 development)

---

### 1. DOMAIN LAYER — BUDGET ENTITY

#### 1.1: Create BudgetStatusLevel Enum

**Task**: Define the budget status level enum used for visual indicators

**File**: `src/SauronSheet.Domain/ValueObjects/BudgetStatusLevel.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Budget status thresholds for visual indicators.
/// Green: spent < 60% of limit
/// Yellow: spent 60–80% of limit
/// Red: spent 80–100% of limit
/// Overage: spent > 100% of limit
/// </summary>
public enum BudgetStatusLevel
{
    Green,
    Yellow,
    Red,
    Overage
}
```

**Verification**:

```sh
dotnet build --project src/SauronSheet.Domain/
# Expected: Build succeeds
```

---

#### 1.2: Write Domain.Tests for Budget Entity (RED Phase)

**Task**: Create test stubs for `Budget` entity (19 tests)

**File**: `tests/SauronSheet.Domain.Tests/Entities/BudgetTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Entities;

public class BudgetTests
{
    // === Construction Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_ValidConstruction_SetsAllProperties()
    {
        // GIVEN valid BudgetId, UserId, CategoryId, DateRange (month), Money limit
        // WHEN Budget is constructed
        // THEN all properties are set correctly and CreatedAt is set
        Assert.True(false, "Implement Budget entity construction");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullUserId_ThrowsArgumentNullException()
    {
        // GIVEN null UserId
        // WHEN Budget is constructed
        // THEN throws ArgumentNullException
        Assert.True(false, "Implement UserId null guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullCategoryId_ThrowsArgumentNullException()
    {
        // GIVEN null CategoryId
        // WHEN Budget is constructed
        // THEN throws ArgumentNullException
        Assert.True(false, "Implement CategoryId null guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullPeriod_ThrowsArgumentNullException()
    {
        // GIVEN null DateRange period
        // WHEN Budget is constructed
        // THEN throws ArgumentNullException
        Assert.True(false, "Implement Period null guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullLimit_ThrowsArgumentNullException()
    {
        // GIVEN null Money limit
        // WHEN Budget is constructed
        // THEN throws ArgumentNullException
        Assert.True(false, "Implement Limit null guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_ZeroLimit_ThrowsDomainException()
    {
        // GIVEN Money limit with Amount = 0
        // WHEN Budget is constructed
        // THEN throws DomainException("Budget limit must be positive.")
        Assert.True(false, "Implement zero limit guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NegativeLimit_ThrowsDomainException()
    {
        // GIVEN Money limit with Amount = -100
        // WHEN Budget is constructed
        // THEN throws DomainException("Budget limit must be positive.")
        Assert.True(false, "Implement negative limit guard");
    }

    // === IsOverBudget Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_SpendExceedsLimit_ReturnsTrue()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 600 EUR
        // WHEN IsOverBudget is called
        // THEN returns true
        Assert.True(false, "Implement IsOverBudget exceeds case");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_SpendBelowLimit_ReturnsFalse()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 300 EUR
        // WHEN IsOverBudget is called
        // THEN returns false
        Assert.True(false, "Implement IsOverBudget below case");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_SpendEqualsLimit_ReturnsFalse()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 500 EUR
        // WHEN IsOverBudget is called
        // THEN returns false (at limit is not over)
        Assert.True(false, "Implement IsOverBudget equal case");
    }

    // === PercentageUsed Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_ZeroSpend_ReturnsZero()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 0 EUR
        // WHEN PercentageUsed is called
        // THEN returns 0.0m
        Assert.True(false, "Implement PercentageUsed zero case");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_HalfSpend_ReturnsFiftyPercent()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 250 EUR
        // WHEN PercentageUsed is called
        // THEN returns 0.50m (50%)
        Assert.True(false, "Implement PercentageUsed half case");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_OverSpend_ReturnsGreaterThanOne()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 625 EUR
        // WHEN PercentageUsed is called
        // THEN returns 1.25m (125%) — domain returns raw, UI caps
        Assert.True(false, "Implement PercentageUsed overage case");
    }

    // === RemainingAmount Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void RemainingAmount_UnderBudget_ReturnsPositive()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 300 EUR
        // WHEN RemainingAmount is called
        // THEN returns Money(200, "EUR")
        Assert.True(false, "Implement RemainingAmount under budget case");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void RemainingAmount_OverBudget_ReturnsNegative()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 700 EUR
        // WHEN RemainingAmount is called
        // THEN returns Money(-200, "EUR")
        Assert.True(false, "Implement RemainingAmount over budget case");
    }

    // === UpdateLimit Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateLimit_ValidPositiveLimit_UpdatesLimitAndTimestamp()
    {
        // GIVEN Budget with limit 500 EUR
        // WHEN UpdateLimit(Money(600, "EUR")) is called
        // THEN Limit is updated to 600 EUR AND UpdatedAt is set
        Assert.True(false, "Implement UpdateLimit valid case");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateLimit_ZeroLimit_ThrowsDomainException()
    {
        // GIVEN Budget with limit 500 EUR
        // WHEN UpdateLimit(Money(0, "EUR")) is called
        // THEN throws DomainException("Budget limit must be positive.")
        Assert.True(false, "Implement UpdateLimit zero guard");
    }

    // === Currency Validation Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_CurrencyMismatch_ThrowsInvalidOperationException()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 300 USD (different currency)
        // WHEN IsOverBudget is called
        // THEN throws InvalidOperationException (EnsureSameCurrency)
        Assert.True(false, "Implement IsOverBudget currency mismatch guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_CurrencyMismatch_ThrowsInvalidOperationException()
    {
        // GIVEN Budget with limit 500 EUR
        // AND currentSpend = 250 USD (different currency)
        // WHEN PercentageUsed is called
        // THEN throws InvalidOperationException (EnsureSameCurrency)
        Assert.True(false, "Implement PercentageUsed currency mismatch guard");
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~BudgetTests" --no-build
# Expected: 19 new Domain tests FAIL (red) — Budget entity not yet implemented
# Expected: ~186 prior tests still PASS
```

---

#### 1.3: Implement Budget Entity (GREEN Phase)

**Task**: Create `Budget` aggregate root entity with validation and business methods

**File**: `src/SauronSheet.Domain/Entities/Budget.cs`

```csharp
namespace SauronSheet.Domain.Entities;

using System;
using Common;
using Exceptions;
using ValueObjects;

/// <summary>
/// Budget aggregate root.
/// Represents a monthly spending limit for a specific category.
/// One budget per user-category-month (uniqueness enforced by repository + DB constraint).
/// </summary>
public class Budget : AggregateRoot<BudgetId>
{
    public UserId UserId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public DateRange Period { get; private set; }
    public Money Limit { get; private set; }

    public Budget(
        BudgetId id,
        UserId userId,
        CategoryId categoryId,
        DateRange period,
        Money limit)
        : base(id)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        Period = period ?? throw new ArgumentNullException(nameof(period));

        if (limit == null) throw new ArgumentNullException(nameof(limit));
        if (!limit.IsPositive)
            throw new DomainException("Budget limit must be positive.");

        Limit = limit;
    }

    /// <summary>
    /// Whether current spending exceeds the budget limit.
    /// Returns true only when spend strictly exceeds limit.
    /// </summary>
    public bool IsOverBudget(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        EnsureSameCurrency(currentSpend);
        return currentSpend.Amount > Limit.Amount;
    }

    /// <summary>
    /// Percentage of budget used (0.0 = 0%, 1.0 = 100%, >1.0 = overage).
    /// Domain returns raw value; UI is responsible for capping display.
    /// </summary>
    public decimal PercentageUsed(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        EnsureSameCurrency(currentSpend);
        if (Limit.Amount == 0) return 0; // Defensive: constructor prevents limit <= 0
        return currentSpend.Amount / Limit.Amount;
    }

    /// <summary>
    /// Remaining budget amount (Limit - currentSpend).
    /// Returns negative Money when over budget.
    /// </summary>
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

    /// <summary>
    /// Update the budget limit. Throws if new limit is zero or negative.
    /// Sets UpdatedAt timestamp.
    /// </summary>
    public void UpdateLimit(Money newLimit)
    {
        if (newLimit == null) throw new ArgumentNullException(nameof(newLimit));
        if (!newLimit.IsPositive)
            throw new DomainException("Budget limit must be positive.");

        Limit = newLimit;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~BudgetTests" --no-build
# Expected: 19 Budget entity tests PASS (green)
```

---

#### Checkpoint 1: Budget Entity Tests Green ✓

```sh
dotnet test --filter Category=Domain --no-build
# Expected: ~44 domain tests PASS (37 Phase 0–4 + 17 Phase 5 Budget - 10 pending BudgetService)
```

---

### 2. DOMAIN LAYER — IBUDGETREPOSITORY & BUDGETSERVICE

#### 2.1: Define IBudgetRepository Interface

**Task**: Create `IBudgetRepository` contract in Domain layer

**File**: `src/SauronSheet.Domain/Repositories/IBudgetRepository.cs`

```csharp
namespace SauronSheet.Domain.Repositories;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;

/// <summary>
/// Repository interface for Budget aggregate.
/// Defined in Domain layer as contract; implemented in Infrastructure.
/// Phase 5: Budget Management.
/// </summary>
public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(BudgetId id);
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId);
    Task<Budget?> GetByUserAndCategoryAndMonthAsync(UserId userId, CategoryId categoryId, DateRange period);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(BudgetId id);
}
```

**Verification**:

```sh
dotnet build --project src/SauronSheet.Domain/
# Expected: Build succeeds
```

---

#### 2.2: Write Domain.Tests for BudgetService (RED Phase)

**Task**: Create test stubs for `BudgetService` domain service (10 tests)

**File**: `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Services;

public class BudgetServiceTests
{
    // === ValidateUniqueBudget Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_NoDuplicate_Succeeds()
    {
        // GIVEN no existing budget for userId + categoryId + period
        // WHEN ValidateUniqueBudget is called
        // THEN no exception is thrown
        Assert.True(false, "Implement BudgetService uniqueness check — no duplicate");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_DuplicateExists_ThrowsDomainException()
    {
        // GIVEN an existing budget for userId + categoryId + period
        // WHEN ValidateUniqueBudget is called with same combination
        // THEN throws DomainException containing "already exists"
        Assert.True(false, "Implement BudgetService uniqueness check — duplicate");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_SameUserDifferentCategory_Succeeds()
    {
        // GIVEN a budget exists for userId + categoryA + period
        // WHEN ValidateUniqueBudget is called for userId + categoryB + period
        // THEN no exception (different category is fine)
        Assert.True(false, "Implement uniqueness — different category OK");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_SameUserSameCategoryDifferentMonth_Succeeds()
    {
        // GIVEN a budget exists for userId + categoryId + February
        // WHEN ValidateUniqueBudget is called for userId + categoryId + March
        // THEN no exception (different month is fine)
        Assert.True(false, "Implement uniqueness — different month OK");
    }

    // === GetStatusLevel Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_Under60Percent_ReturnsGreen()
    {
        // GIVEN percentage = 0.50 (50%)
        // WHEN GetStatusLevel(percentage) is called
        // THEN returns BudgetStatusLevel.Green
        Assert.True(false, "Implement GetStatusLevel Green");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_At60Percent_ReturnsGreen()
    {
        // GIVEN percentage = 0.60 (60%)
        // WHEN GetStatusLevel(percentage) is called
        // THEN returns BudgetStatusLevel.Green (threshold is > 0.6, not >=)
        Assert.True(false, "Implement GetStatusLevel Green at 60% boundary");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_At75Percent_ReturnsYellow()
    {
        // GIVEN percentage = 0.75 (75%)
        // WHEN GetStatusLevel(percentage) is called
        // THEN returns BudgetStatusLevel.Yellow
        Assert.True(false, "Implement GetStatusLevel Yellow mid-range");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_At80Percent_ReturnsYellow()
    {
        // GIVEN percentage = 0.80 (80%)
        // WHEN GetStatusLevel(percentage) is called
        // THEN returns BudgetStatusLevel.Yellow (threshold is > 0.8, not >=)
        Assert.True(false, "Implement GetStatusLevel Yellow at 80% boundary");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_At100Percent_ReturnsRed()
    {
        // GIVEN percentage = 1.00 (100%)
        // WHEN GetStatusLevel(percentage) is called
        // THEN returns BudgetStatusLevel.Red
        Assert.True(false, "Implement GetStatusLevel Red at 100%");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_Over100Percent_ReturnsOverage()
    {
        // GIVEN percentage = 1.25 (125%)
        // WHEN GetStatusLevel(percentage) is called
        // THEN returns BudgetStatusLevel.Overage
        Assert.True(false, "Implement GetStatusLevel Overage");
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~BudgetServiceTests" --no-build
# Expected: 10 new Domain tests FAIL (red) — BudgetService not yet implemented
```

---

#### 2.3: Implement BudgetService Domain Service (GREEN Phase)

**Task**: Create `BudgetService` with uniqueness validation and status level calculation

**File**: `src/SauronSheet.Domain/Services/BudgetService.cs`

```csharp
namespace SauronSheet.Domain.Services;

using System;
using System.Threading.Tasks;
using Entities;
using Exceptions;
using Repositories;
using ValueObjects;

/// <summary>
/// Domain service for cross-entity budget logic.
/// Handles uniqueness validation (budget + category + month per user)
/// and budget status level calculation (Green/Yellow/Red/Overage).
/// </summary>
public class BudgetService
{
    private readonly IBudgetRepository _budgetRepository;

    public BudgetService(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
    }

    /// <summary>
    /// Validate that no budget already exists for the same user, category, and period.
    /// Throws DomainException if a duplicate is found.
    /// </summary>
    public async Task ValidateUniqueBudget(UserId userId, CategoryId categoryId, DateRange period)
    {
        var existing = await _budgetRepository.GetByUserAndCategoryAndMonthAsync(userId, categoryId, period);
        if (existing is not null)
            throw new DomainException(
                $"A budget for this category in {period.StartDate:MMMM yyyy} already exists.");
    }

    /// <summary>
    /// Calculate the budget status level based on percentage used.
    /// Thresholds: Green < 60%, Yellow 60–80%, Red 80–100%, Overage > 100%.
    /// </summary>
    public static BudgetStatusLevel GetStatusLevel(decimal percentageUsed)
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

**Verification**:

```sh
dotnet test --filter "ClassName~BudgetServiceTests" --no-build
# Expected: 10 BudgetService tests PASS (green)
```

---

#### Checkpoint 2: Domain Layer Complete ✓

```sh
dotnet test --filter Category=Domain --no-build
# Expected: ~66 domain tests PASS (37 Phase 0–4 + 19 Budget entity + 10 BudgetService)
```

**Status**: All domain tests passing → Proceed to Phase 5C (Application DTOs)

---

### 3. APPLICATION LAYER — BUDGET DTOs

#### 3.1: Create Budget DTOs

**Task**: Define data transfer objects for budget queries

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Application/Features/Budgets/DTOs
mkdir -p src/SauronSheet.Application/Features/Budgets/Commands
mkdir -p src/SauronSheet.Application/Features/Budgets/Queries
```

**File**: `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetDto.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Basic budget representation for list views.
/// </summary>
public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal LimitAmount,
    string Currency,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

**File**: `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetStatusDto.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Budget with current spend status for detail views.
/// Includes calculated fields: CurrentSpend, RemainingAmount, PercentageUsed, StatusLevel.
/// </summary>
public record BudgetStatusDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal LimitAmount,
    decimal CurrentSpend,
    decimal RemainingAmount,
    decimal PercentageUsed,
    string StatusLevel,
    string Currency,
    DateTime PeriodStart,
    DateTime PeriodEnd);
```

**File**: `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetVsActualDto.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Budget vs. actual comparison per category for a given month.
/// Categories with spending but no budget show BudgetLimit as null.
/// </summary>
public record BudgetVsActualDto(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal? BudgetLimit,
    decimal ActualSpend,
    decimal? Difference,
    decimal? PercentageUsed,
    string? StatusLevel,
    string Currency);
```

**File**: `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetDashboardSummaryDto.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.DTOs;

using System.Collections.Generic;

/// <summary>
/// Aggregated budget health summary for the dashboard widget.
/// Shows list of budget statuses and summary counts.
/// </summary>
public record BudgetDashboardSummaryDto(
    List<BudgetStatusDto> Budgets,
    int TotalBudgets,
    int OnTrackCount,
    int OverBudgetCount);
```

**Verification**:

```sh
dotnet build --project src/SauronSheet.Application/
# Expected: Build succeeds
```

---

### 4. APPLICATION LAYER — BUDGET COMMAND HANDLERS

#### 4.1: Write Application.Tests for CreateBudgetCommand (RED Phase)

**Task**: Create test stubs for `CreateBudgetCommandHandler` (5 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/CreateBudgetCommandHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using MediatR;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class CreateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidBudget_CreatesBudgetAndReturnsId()
    {
        // GIVEN valid category exists, no duplicate budget
        // WHEN CreateBudgetCommand is handled
        // THEN budget is added to repository and new Guid is returned
        Assert.True(false, "Implement CreateBudget happy path");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DuplicateBudget_ThrowsDomainException()
    {
        // GIVEN budget already exists for this user + category + month
        // WHEN CreateBudgetCommand is handled
        // THEN throws DomainException (uniqueness violation)
        Assert.True(false, "Implement CreateBudget duplicate check");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_CategoryNotFound_ThrowsEntityNotFoundException()
    {
        // GIVEN category ID does not exist for user
        // WHEN CreateBudgetCommand is handled
        // THEN throws EntityNotFoundException
        Assert.True(false, "Implement CreateBudget category validation");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ZeroLimit_ThrowsDomainException()
    {
        // GIVEN limit amount = 0
        // WHEN CreateBudgetCommand is handled
        // THEN throws DomainException from Budget constructor
        Assert.True(false, "Implement CreateBudget zero limit guard");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_UsesCurrentUserContext()
    {
        // GIVEN authenticated user "user-A"
        // WHEN CreateBudgetCommand is handled
        // THEN budget is created with UserId = "user-A"
        Assert.True(false, "Implement CreateBudget tenant scoping");
    }
}
```

---

#### 4.2: Define CreateBudgetCommand & Handler (GREEN Phase)

**File**: `src/SauronSheet.Application/Features/Budgets/Commands/CreateBudgetCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to create a new monthly budget for a category.
/// Phase 5 (Scenario 5.1).
/// </summary>
public record CreateBudgetCommand(
    Guid CategoryId,
    decimal LimitAmount,
    DateTime PeriodStart,
    DateTime PeriodEnd) : IRequest<Guid>;
```

**File**: `src/SauronSheet.Application/Features/Budgets/Commands/CreateBudgetCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using MediatR;

/// <summary>
/// Handler for CreateBudgetCommand.
/// Validates category existence, budget uniqueness, then creates budget.
/// </summary>
public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, Guid>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly BudgetService _budgetService;
    private readonly IUserContext _userContext;

    public CreateBudgetCommandHandler(
        IBudgetRepository budgetRepo,
        ICategoryRepository categoryRepo,
        BudgetService budgetService,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _categoryRepo = categoryRepo;
        _budgetService = budgetService;
        _userContext = userContext;
    }

    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var categoryId = new CategoryId(request.CategoryId);

        // Validate category exists for this user
        var category = await _categoryRepo.GetByIdAsync(categoryId);
        if (category is null || !category.UserId.Value.Equals(userId.Value, StringComparison.OrdinalIgnoreCase))
            throw new EntityNotFoundException("Category", request.CategoryId);

        // Build domain values
        var period = new DateRange(request.PeriodStart, request.PeriodEnd);
        var limit = new Money(request.LimitAmount);

        // Validate uniqueness via domain service
        await _budgetService.ValidateUniqueBudget(userId, categoryId, period);

        // Create aggregate
        var budgetId = new BudgetId(Guid.NewGuid());
        var budget = new Budget(budgetId, userId, categoryId, period, limit);

        await _budgetRepo.AddAsync(budget);
        return budgetId.Value;
    }
}
```

---

#### 4.3: Write Application.Tests for UpdateBudgetCommand (RED Phase)

**Task**: Create test stubs for `UpdateBudgetCommandHandler` (3 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/UpdateBudgetCommandHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class UpdateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidUpdate_UpdatesLimitAndPersists()
    {
        // GIVEN existing budget with limit 500 EUR
        // WHEN UpdateBudgetCommand with new limit 600 is handled
        // THEN budget.UpdateLimit is called and repository.UpdateAsync is called
        Assert.True(false, "Implement UpdateBudget happy path");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetNotFound_ThrowsEntityNotFoundException()
    {
        // GIVEN budget ID does not exist
        // WHEN UpdateBudgetCommand is handled
        // THEN throws EntityNotFoundException
        Assert.True(false, "Implement UpdateBudget not found");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DifferentUserBudget_ThrowsEntityNotFoundException()
    {
        // GIVEN budget belongs to user-B but current user is user-A
        // WHEN UpdateBudgetCommand is handled
        // THEN throws EntityNotFoundException (tenant isolation)
        Assert.True(false, "Implement UpdateBudget tenant isolation");
    }
}
```

---

#### 4.4: Define UpdateBudgetCommand & Handler (GREEN Phase)

**File**: `src/SauronSheet.Application/Features/Budgets/Commands/UpdateBudgetCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to update an existing budget's spending limit.
/// Only the limit amount can be changed; category and period are immutable.
/// Phase 5 (Scenario 5.3).
/// </summary>
public record UpdateBudgetCommand(Guid BudgetId, decimal NewLimitAmount) : IRequest<Unit>;
```

**File**: `src/SauronSheet.Application/Features/Budgets/Commands/UpdateBudgetCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

/// <summary>
/// Handler for UpdateBudgetCommand.
/// Validates ownership and delegates to Budget.UpdateLimit domain method.
/// </summary>
public class UpdateBudgetCommandHandler : IRequestHandler<UpdateBudgetCommand, Unit>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly IUserContext _userContext;

    public UpdateBudgetCommandHandler(IBudgetRepository budgetRepo, IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgetId = new BudgetId(request.BudgetId);

        var budget = await _budgetRepo.GetByIdAsync(budgetId);
        if (budget is null || budget.UserId.Value != userId.Value)
            throw new EntityNotFoundException("Budget", request.BudgetId);

        var newLimit = new Money(request.NewLimitAmount);
        budget.UpdateLimit(newLimit);

        await _budgetRepo.UpdateAsync(budget);
        return Unit.Value;
    }
}
```

---

#### 4.5: Write Application.Tests for DeleteBudgetCommand (RED Phase)

**Task**: Create test stubs for `DeleteBudgetCommandHandler` (4 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/DeleteBudgetCommandHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class DeleteBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidDelete_RemovesBudget()
    {
        // GIVEN existing budget owned by current user
        // WHEN DeleteBudgetCommand is handled
        // THEN repository.DeleteAsync is called with budget ID
        Assert.True(false, "Implement DeleteBudget happy path");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetNotFound_ThrowsEntityNotFoundException()
    {
        // GIVEN budget ID does not exist
        // WHEN DeleteBudgetCommand is handled
        // THEN throws EntityNotFoundException
        Assert.True(false, "Implement DeleteBudget not found");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DifferentUserBudget_ThrowsEntityNotFoundException()
    {
        // GIVEN budget owned by user-B, current user is user-A
        // WHEN DeleteBudgetCommand is handled
        // THEN throws EntityNotFoundException (tenant isolation)
        Assert.True(false, "Implement DeleteBudget tenant isolation");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeletedBudget_DoesNotAffectTransactions()
    {
        // GIVEN budget with associated transactions
        // WHEN budget is deleted
        // THEN transactions remain unmodified (verify no cascade in handler)
        Assert.True(false, "Implement DeleteBudget no cascade verification");
    }
}
```

---

#### 4.6: Define DeleteBudgetCommand & Handler (GREEN Phase)

**File**: `src/SauronSheet.Application/Features/Budgets/Commands/DeleteBudgetCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to delete a budget.
/// No cascading effects on transactions (budget is a tracking overlay).
/// Phase 5 (Scenario 5.4).
/// </summary>
public record DeleteBudgetCommand(Guid BudgetId) : IRequest<Unit>;
```

**File**: `src/SauronSheet.Application/Features/Budgets/Commands/DeleteBudgetCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

/// <summary>
/// Handler for DeleteBudgetCommand.
/// Validates ownership and deletes budget. No transaction cascade.
/// </summary>
public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand, Unit>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly IUserContext _userContext;

    public DeleteBudgetCommandHandler(IBudgetRepository budgetRepo, IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgetId = new BudgetId(request.BudgetId);

        var budget = await _budgetRepo.GetByIdAsync(budgetId);
        if (budget is null || budget.UserId.Value != userId.Value)
            throw new EntityNotFoundException("Budget", request.BudgetId);

        await _budgetRepo.DeleteAsync(budgetId);
        return Unit.Value;
    }
}
```

---

#### Checkpoint 3: Command Handlers Complete ✓

```sh
dotnet test --filter "Category=Application&ClassName~Budget" --no-build
# Expected: 12 command handler tests PASS
```

---

### 5. APPLICATION LAYER — BUDGET QUERY HANDLERS

#### 5.1: Write Application.Tests for GetBudgetsQuery (RED Phase)

**Task**: Create test stubs for `GetBudgetsQueryHandler` (4 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetsQueryHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetsQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetsExist_ReturnsBudgetStatusDtoList()
    {
        // GIVEN user has 3 budgets with transactions in their categories
        // WHEN GetBudgetsQuery is handled
        // THEN returns list of 3 BudgetStatusDto sorted alphabetically by category name
        //      each with CurrentSpend, RemainingAmount, PercentageUsed, StatusLevel calculated
        Assert.True(false, "Implement GetBudgets happy path with status");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudgets_ReturnsEmptyList()
    {
        // GIVEN user has no budgets
        // WHEN GetBudgetsQuery is handled
        // THEN returns empty list
        Assert.True(false, "Implement GetBudgets empty list");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_WithYearMonthFilter_FiltersCorrectly()
    {
        // GIVEN user has budgets for Feb and Mar
        // WHEN GetBudgetsQuery with Year=2026, Month=2 is handled
        // THEN returns only Feb budgets with status
        Assert.True(false, "Implement GetBudgets year/month filter");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_ReturnsOnlyOwnBudgets()
    {
        // GIVEN budgets for user-A and user-B
        // WHEN user-A queries budgets
        // THEN only user-A budgets returned
        Assert.True(false, "Implement GetBudgets tenant scoping");
    }
}
```

---

#### 5.2: Define GetBudgetsQuery & Handler (GREEN Phase)

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetsQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get all budgets for the current user, with optional year/month filter.
/// Returns BudgetStatusDto with spend calculations for progress bars and status indicators.
/// Phase 5 (Scenario 5.2).
/// </summary>
public record GetBudgetsQuery(int? Year = null, int? Month = null) : IRequest<List<BudgetStatusDto>>;
```

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetsQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Services;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetBudgetsQuery.
/// Returns budget list with status (CurrentSpend, Remaining, PercentageUsed, StatusLevel).
/// Optional year/month filtering, sorted alphabetically by category name.
/// </summary>
public class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, List<BudgetStatusDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetsQueryHandler(
        IBudgetRepository budgetRepo,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<BudgetStatusDto>> Handle(GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgets = await _budgetRepo.GetByUserIdAsync(userId);

        // Optional year/month filter
        if (request.Year.HasValue && request.Month.HasValue)
        {
            budgets = budgets
                .Where(b => b.Period.StartDate.Year == request.Year.Value
                         && b.Period.StartDate.Month == request.Month.Value)
                .ToList();
        }
        else if (request.Year.HasValue)
        {
            budgets = budgets
                .Where(b => b.Period.StartDate.Year == request.Year.Value)
                .ToList();
        }

        // Load categories for name/color lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        // Load all transactions for spend calculation across all budget periods
        var results = new List<BudgetStatusDto>();
        foreach (var budget in budgets)
        {
            // Calculate current spend from transactions in this category and period
            var userSpec = new TransactionByUserSpecification(userId);
            var dateSpec = new TransactionByDateRangeSpecification(budget.Period.StartDate, budget.Period.EndDate);
            var categorySpec = new TransactionByCategorySpecification(budget.CategoryId);
            var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(
                CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec),
                categorySpec);

            var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);
            var currentSpendAmount = transactions
                .Where(t => t.Amount.IsNegative)
                .Sum(t => Math.Abs(t.Amount.Amount));

            var currentSpend = new Money(currentSpendAmount);
            var percentageUsed = budget.PercentageUsed(currentSpend);
            var remaining = budget.RemainingAmount(currentSpend);
            var statusLevel = BudgetService.GetStatusLevel(percentageUsed);

            var catName = "Unknown";
            string? catColor = null;
            if (categoryLookup.TryGetValue(budget.CategoryId, out var cat))
            {
                catName = cat.Name;
                catColor = cat.Color;
            }

            results.Add(new BudgetStatusDto(
                budget.Id.Value,
                budget.CategoryId.Value,
                catName,
                catColor,
                budget.Limit.Amount,
                currentSpendAmount,
                remaining.Amount,
                percentageUsed,
                statusLevel.ToString(),
                budget.Limit.Currency,
                budget.Period.StartDate,
                budget.Period.EndDate));
        }

        return results
            .OrderBy(b => b.CategoryName)
            .ToList();
    }
}
```

---

#### 5.3: Write Application.Tests for GetBudgetByIdQuery (RED Phase)

**Task**: Create test stubs for `GetBudgetByIdQueryHandler` (3 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetByIdQueryHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetByIdQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetExists_ReturnsBudgetStatusDto()
    {
        // GIVEN budget exists with spend calculated from transactions
        // WHEN GetBudgetByIdQuery is handled
        // THEN returns BudgetStatusDto with correct spend, remaining, percentage, status level
        Assert.True(false, "Implement GetBudgetById happy path");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetNotFound_ThrowsEntityNotFoundException()
    {
        // GIVEN budget ID does not exist
        // WHEN GetBudgetByIdQuery is handled
        // THEN throws EntityNotFoundException
        Assert.True(false, "Implement GetBudgetById not found");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ZeroTransactions_ReturnsZeroSpend()
    {
        // GIVEN budget exists but no transactions in category for the period
        // WHEN GetBudgetByIdQuery is handled
        // THEN CurrentSpend = 0, PercentageUsed = 0, StatusLevel = Green
        Assert.True(false, "Implement GetBudgetById zero transactions");
    }
}
```

---

#### 5.4: Define GetBudgetByIdQuery & Handler (GREEN Phase)

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetByIdQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get a single budget with current spend status.
/// Phase 5 (Scenario 5.7).
/// </summary>
public record GetBudgetByIdQuery(Guid BudgetId) : IRequest<BudgetStatusDto>;
```

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetByIdQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetBudgetByIdQuery.
/// Calculates current spend from transactions and returns budget with status.
/// </summary>
public class GetBudgetByIdQueryHandler : IRequestHandler<GetBudgetByIdQuery, BudgetStatusDto>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetByIdQueryHandler(
        IBudgetRepository budgetRepo,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<BudgetStatusDto> Handle(GetBudgetByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgetId = new BudgetId(request.BudgetId);

        var budget = await _budgetRepo.GetByIdAsync(budgetId);
        if (budget is null || budget.UserId.Value != userId.Value)
            throw new EntityNotFoundException("Budget", request.BudgetId);

        // Calculate current spend from transactions in this category and period
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(budget.Period.StartDate, budget.Period.EndDate);
        var categorySpec = new TransactionByCategorySpecification(budget.CategoryId);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(
            CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec),
            categorySpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);
        var currentSpendAmount = transactions
            .Where(t => t.Amount.IsNegative)
            .Sum(t => Math.Abs(t.Amount.Amount));

        var currentSpend = new Money(currentSpendAmount);
        var percentageUsed = budget.PercentageUsed(currentSpend);
        var remaining = budget.RemainingAmount(currentSpend);
        var statusLevel = BudgetService.GetStatusLevel(percentageUsed);

        // Get category info
        var category = await _categoryRepo.GetByIdAsync(budget.CategoryId);

        return new BudgetStatusDto(
            budget.Id.Value,
            budget.CategoryId.Value,
            category?.Name ?? "Unknown",
            category?.Color,
            budget.Limit.Amount,
            currentSpendAmount,
            remaining.Amount,
            percentageUsed,
            statusLevel.ToString(),
            budget.Limit.Currency,
            budget.Period.StartDate,
            budget.Period.EndDate);
    }
}
```

---

#### 5.5: Write Application.Tests for GetBudgetVsActualQuery (RED Phase)

**Task**: Create test stubs for `GetBudgetVsActualQueryHandler` (5 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetVsActualQueryHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetVsActualQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetsAndTransactions_ReturnsComparison()
    {
        // GIVEN budgets for 2 categories and transactions in those categories
        // WHEN GetBudgetVsActualQuery is handled
        // THEN returns comparison DTOs with correct budget vs actual values
        Assert.True(false, "Implement GetBudgetVsActual happy path");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_CategoryWithSpendButNoBudget_ShowsNoBudget()
    {
        // GIVEN category has transactions but no budget set
        // WHEN GetBudgetVsActualQuery is handled
        // THEN DTO shows BudgetLimit = null for that category
        Assert.True(false, "Implement GetBudgetVsActual unbudgeted category");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetWithNoSpend_ShowsZeroActual()
    {
        // GIVEN budget exists but no transactions for that category/month
        // WHEN GetBudgetVsActualQuery is handled
        // THEN ActualSpend = 0 for that budget
        Assert.True(false, "Implement GetBudgetVsActual zero spend");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SummaryRow_TotalsCorrectly()
    {
        // GIVEN multiple budgets with spending
        // WHEN GetBudgetVsActualQuery is handled
        // THEN total budgeted, total actual, total difference are calculated
        Assert.True(false, "Implement GetBudgetVsActual summary totals");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SortOrder_OverBudgetFirst()
    {
        // GIVEN budgets: one over, one under
        // WHEN GetBudgetVsActualQuery is handled
        // THEN over-budget categories appear first, sorted by percentage descending
        Assert.True(false, "Implement GetBudgetVsActual sort order");
    }
}
```

---

#### 5.6: Define GetBudgetVsActualQuery & Handler (GREEN Phase)

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetVsActualQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get budget vs. actual spending comparison for a given month.
/// Phase 5 (Scenario 5.6).
/// </summary>
public record GetBudgetVsActualQuery(int Year, int Month) : IRequest<List<BudgetVsActualDto>>;
```

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetVsActualQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Services;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetBudgetVsActualQuery.
/// Compares budgets vs. actual spending per category.
/// Includes categories with spending but no budget.
/// Sorted by over-budget first, then percentage descending.
/// </summary>
public class GetBudgetVsActualQueryHandler : IRequestHandler<GetBudgetVsActualQuery, List<BudgetVsActualDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetVsActualQueryHandler(
        IBudgetRepository budgetRepo,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<BudgetVsActualDto>> Handle(
        GetBudgetVsActualQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Construct DateRange from year + month
        var periodStart = new DateTime(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var period = new DateRange(periodStart, periodEnd);

        // Load all user budgets for the period
        var allBudgets = await _budgetRepo.GetByUserIdAsync(userId);
        var periodBudgets = allBudgets
            .Where(b => b.Period.StartDate.Year == request.Year
                     && b.Period.StartDate.Month == request.Month)
            .ToList();

        // Load all transactions for the period
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(periodStart, periodEnd);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);
        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Only expenses
        var expenses = transactions.Where(t => t.Amount.IsNegative).ToList();

        // Group expenses by category
        var spendByCategory = expenses
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        // Load categories for lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        var results = new List<BudgetVsActualDto>();

        // Budgeted categories
        var budgetedCategoryIds = new HashSet<CategoryId?>();
        foreach (var budget in periodBudgets)
        {
            budgetedCategoryIds.Add(budget.CategoryId);
            var actualSpend = spendByCategory.GetValueOrDefault(budget.CategoryId, 0m);
            var difference = budget.Limit.Amount - actualSpend;
            var currentSpend = new Money(actualSpend);
            var percentageUsed = budget.PercentageUsed(currentSpend);
            var statusLevel = BudgetService.GetStatusLevel(percentageUsed);

            var catName = "Unknown";
            string? catColor = null;
            if (categoryLookup.TryGetValue(budget.CategoryId, out var cat))
            {
                catName = cat.Name;
                catColor = cat.Color;
            }

            results.Add(new BudgetVsActualDto(
                budget.CategoryId.Value,
                catName,
                catColor,
                budget.Limit.Amount,
                actualSpend,
                difference,
                percentageUsed,
                statusLevel.ToString(),
                budget.Limit.Currency));
        }

        // Unbudgeted categories with spending
        foreach (var kvp in spendByCategory)
        {
            if (kvp.Key == null || budgetedCategoryIds.Contains(kvp.Key)) continue;

            var catName = "Uncategorized";
            string? catColor = null;
            if (categoryLookup.TryGetValue(kvp.Key, out var cat))
            {
                catName = cat.Name;
                catColor = cat.Color;
            }

            results.Add(new BudgetVsActualDto(
                kvp.Key.Value,
                catName,
                catColor,
                null,
                kvp.Value,
                null,
                null,
                null,
                "EUR"));
        }

        // Sort: over-budget first, then by percentage descending
        return results
            .OrderByDescending(r => r.PercentageUsed.HasValue && r.PercentageUsed > 1.0m)
            .ThenByDescending(r => r.PercentageUsed ?? 0)
            .ToList();
    }
}
```

---

#### 5.7: Write Application.Tests for GetBudgetSummaryForDashboardQuery (RED Phase)

**Task**: Create test stubs for `GetBudgetSummaryForDashboardQueryHandler` (4 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetSummaryForDashboardQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetsExist_ReturnsAggregatedSummary()
    {
        // GIVEN 3 budgets: 2 on-track (Green/Yellow), 1 over-budget (Overage)
        // WHEN GetBudgetSummaryForDashboardQuery is handled
        // THEN TotalBudgets=3, OnTrackCount=2, OverBudgetCount=1
        Assert.True(false, "Implement dashboard summary happy path");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudgets_ReturnsEmptySummary()
    {
        // GIVEN user has no budgets
        // WHEN GetBudgetSummaryForDashboardQuery is handled
        // THEN TotalBudgets=0, OnTrackCount=0, OverBudgetCount=0, Budgets=[]
        Assert.True(false, "Implement dashboard summary empty");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AllOnTrack_NoOverBudget()
    {
        // GIVEN 2 budgets both under 60% usage (Green)
        // WHEN GetBudgetSummaryForDashboardQuery is handled
        // THEN OnTrackCount=2, OverBudgetCount=0
        Assert.True(false, "Implement dashboard summary all green");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_OnlyCurrentUserBudgets()
    {
        // GIVEN budgets for user-A and user-B
        // WHEN user-A queries dashboard summary
        // THEN only user-A budgets are included
        Assert.True(false, "Implement dashboard summary tenant scoping");
    }
}
```

---

#### 5.8: Define GetBudgetSummaryForDashboardQuery & Handler (GREEN Phase)

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get aggregated budget health summary for the dashboard widget.
/// Phase 5 (Scenario 5.5).
/// </summary>
public record GetBudgetSummaryForDashboardQuery(int Year, int Month)
    : IRequest<BudgetDashboardSummaryDto>;
```

**File**: `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Services;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetBudgetSummaryForDashboardQuery.
/// Aggregates budget statuses for the current month's dashboard widget.
/// Calculates current spend from transactions per budget.
/// </summary>
public class GetBudgetSummaryForDashboardQueryHandler
    : IRequestHandler<GetBudgetSummaryForDashboardQuery, BudgetDashboardSummaryDto>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetSummaryForDashboardQueryHandler(
        IBudgetRepository budgetRepo,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<BudgetDashboardSummaryDto> Handle(
        GetBudgetSummaryForDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Construct DateRange from year + month
        var periodStart = new DateTime(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Load budgets for the period
        var allBudgets = await _budgetRepo.GetByUserIdAsync(userId);
        var periodBudgets = allBudgets
            .Where(b => b.Period.StartDate.Year == request.Year
                     && b.Period.StartDate.Month == request.Month)
            .ToList();

        if (!periodBudgets.Any())
        {
            return new BudgetDashboardSummaryDto(
                new List<BudgetStatusDto>(), 0, 0, 0);
        }

        // Load all transactions for the period (single query, then filter in-memory)
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(periodStart, periodEnd);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);
        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Group expenses by category
        var spendByCategory = transactions
            .Where(t => t.Amount.IsNegative)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        // Load categories for lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        var budgetStatuses = new List<BudgetStatusDto>();
        var overBudgetCount = 0;

        foreach (var budget in periodBudgets)
        {
            var actualSpend = spendByCategory.GetValueOrDefault(budget.CategoryId, 0m);
            var currentSpend = new Money(actualSpend);
            var percentageUsed = budget.PercentageUsed(currentSpend);
            var remaining = budget.RemainingAmount(currentSpend);
            var statusLevel = BudgetService.GetStatusLevel(percentageUsed);

            if (statusLevel == BudgetStatusLevel.Overage)
                overBudgetCount++;

            var catName = "Unknown";
            string? catColor = null;
            if (categoryLookup.TryGetValue(budget.CategoryId, out var cat))
            {
                catName = cat.Name;
                catColor = cat.Color;
            }

            budgetStatuses.Add(new BudgetStatusDto(
                budget.Id.Value,
                budget.CategoryId.Value,
                catName,
                catColor,
                budget.Limit.Amount,
                actualSpend,
                remaining.Amount,
                percentageUsed,
                statusLevel.ToString(),
                budget.Limit.Currency,
                budget.Period.StartDate,
                budget.Period.EndDate));
        }

        return new BudgetDashboardSummaryDto(
            budgetStatuses,
            periodBudgets.Count,
            periodBudgets.Count - overBudgetCount,
            overBudgetCount);
    }
}
```

---

#### Checkpoint 4: All Application Tests Complete ✓

```sh
dotnet test --filter "Category=Application&ClassName~Budget" --no-build
# Expected: 28 budget application tests PASS
dotnet test --filter "Category=Application" --no-build
# Expected: ~102 total application tests PASS (74 Phase 0–4 + 28 Phase 5)
```

---

### 6. INFRASTRUCTURE — DATABASE MIGRATION & REPOSITORY

#### 6.1: Create Database Migration

**Task**: Create `006_CreateBudgetsTable.sql` migration with indexes, unique constraint, RLS, and FK cascade

**File**: `src/SauronSheet.Infrastructure/Persistence/Migrations/006_CreateBudgetsTable.sql`

```sql
-- Migration: 006_CreateBudgetsTable.sql
-- Phase 5: Budget Management & Alerts
-- Purpose: Monthly budgets per category with spending limit tracking

CREATE TABLE IF NOT EXISTS public.budgets (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES public.categories(id) ON DELETE CASCADE,
    period_start TIMESTAMPTZ NOT NULL,
    period_end TIMESTAMPTZ NOT NULL,
    limit_amount DECIMAL(18, 2) NOT NULL CHECK (limit_amount > 0),
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(user_id, category_id, period_start)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_budgets_user ON public.budgets(user_id);
CREATE INDEX IF NOT EXISTS idx_budgets_user_period ON public.budgets(user_id, period_start);
CREATE INDEX IF NOT EXISTS idx_budgets_category ON public.budgets(category_id);

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

**Verification**:

```sh
# Apply migration in Supabase dashboard SQL editor or CLI
# Verify table, indexes, constraint, and RLS policies exist
```

---

#### 6.2: Implement SupabaseBudgetRepository

**Task**: Create `SupabaseBudgetRepository` implementing `IBudgetRepository`

**File**: `src/SauronSheet.Infrastructure/Persistence/SupabaseBudgetRepository.cs`

```csharp
namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;

/// <summary>
/// Postgrest DTO for the budgets table.
/// </summary>
[Table("budgets")]
internal class BudgetRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id")]
    public string UserId { get; set; } = "";

    [Column("category_id")]
    public string CategoryId { get; set; } = "";

    [Column("period_start")]
    public DateTime PeriodStart { get; set; }

    [Column("period_end")]
    public DateTime PeriodEnd { get; set; }

    [Column("limit_amount")]
    public decimal LimitAmount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "EUR";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public Budget ToDomain()
    {
        return new Budget(
            new BudgetId(Guid.Parse(Id)),
            new UserId(UserId),
            new CategoryId(Guid.Parse(CategoryId)),
            new DateRange(PeriodStart, PeriodEnd),
            new Money(LimitAmount, Currency));
    }

    public static BudgetRow FromDomain(Budget b)
    {
        return new BudgetRow
        {
            Id = b.Id.Value.ToString(),
            UserId = b.UserId.Value,
            CategoryId = b.CategoryId.Value.ToString(),
            PeriodStart = b.Period.StartDate,
            PeriodEnd = b.Period.EndDate,
            LimitAmount = b.Limit.Amount,
            Currency = b.Limit.Currency,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}

/// <summary>
/// Supabase implementation of IBudgetRepository.
/// Phase 5: Budget Management.
/// </summary>
public class SupabaseBudgetRepository : IBudgetRepository
{
    private readonly Supabase.Client _client;

    public SupabaseBudgetRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<Budget?> GetByIdAsync(BudgetId id)
    {
        var response = await _client.From<BudgetRow>()
            .Where(r => r.Id == id.Value.ToString())
            .Get();
        var row = response.Models.FirstOrDefault();
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId)
    {
        var response = await _client.From<BudgetRow>()
            .Where(r => r.UserId == userId.Value)
            .Get();
        return response.Models.Select(r => r.ToDomain()).ToList();
    }

    public async Task<Budget?> GetByUserAndCategoryAndMonthAsync(
        UserId userId, CategoryId categoryId, DateRange period)
    {
        var response = await _client.From<BudgetRow>()
            .Where(r => r.UserId == userId.Value)
            .Where(r => r.CategoryId == categoryId.Value.ToString())
            .Where(r => r.PeriodStart == period.StartDate)
            .Get();
        var row = response.Models.FirstOrDefault();
        return row?.ToDomain();
    }

    public async Task AddAsync(Budget budget)
    {
        var row = BudgetRow.FromDomain(budget);
        await _client.From<BudgetRow>().Insert(row);
    }

    public async Task UpdateAsync(Budget budget)
    {
        var row = BudgetRow.FromDomain(budget);
        await _client.From<BudgetRow>()
            .Where(r => r.Id == budget.Id.Value.ToString())
            .Update(row);
    }

    public async Task DeleteAsync(BudgetId id)
    {
        await _client.From<BudgetRow>()
            .Where(r => r.Id == id.Value.ToString())
            .Delete();
    }
}
```

---

#### 6.3: Register BudgetService & SupabaseBudgetRepository in DI

**Task**: Update `Infrastructure/DependencyInjection.cs` to register budget-related services

**Modification**: `src/SauronSheet.Infrastructure/DependencyInjection.cs`

Add to the repository registrations section:

```csharp
// Budget persistence and domain service (NEW in Phase 5)
services.AddScoped<IBudgetRepository, SupabaseBudgetRepository>();
services.AddScoped<BudgetService>();
```

**Verification**:

```sh
dotnet build
# Expected: Build succeeds with all new services registered
```

---

#### Checkpoint 5: Infrastructure Complete ✓

```sh
dotnet build
# Expected: Full solution builds with zero errors
```

---

### 7. FRONTEND — BUDGET MANAGEMENT PAGES

#### 7.1: Create Budget List Page (/Budgets)

**Task**: Budget management landing page with list, status indicators, month filter, CRUD actions

**Files**:
- `src/SauronSheet.Frontend/Pages/Budgets/Index.cshtml.cs`
- `src/SauronSheet.Frontend/Pages/Budgets/Index.cshtml`

**PageModel Pattern** (following DashboardModel pattern):

```csharp
[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetStatusDto> Budgets { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Resolve budget list with status for current month
        // Uses GetBudgetsQuery(Year, Month) → List<BudgetStatusDto>
        // Each BudgetStatusDto includes CurrentSpend, Remaining, PercentageUsed, StatusLevel
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid budgetId)
    {
        // Send DeleteBudgetCommand via MediatR
    }
}
```

**View Features**:
- Month selector (Alpine.js dropdown or HTML date input year-month)
- Budget table: Category Name | Limit | Current Spend | Remaining | % Used | Status Badge | Edit/Delete actions
- Status badges color-coded (Green/Yellow/Red/Overage)
- Empty state: "No budgets set for {month}. Create one to start tracking."
- "Create Budget" button linking to `/Budgets/Create`

---

#### 7.2: Create Budget Create Page (/Budgets/Create)

**Task**: Form for creating a new budget

**Files**:
- `src/SauronSheet.Frontend/Pages/Budgets/Create.cshtml.cs`
- `src/SauronSheet.Frontend/Pages/Budgets/Create.cshtml`

**PageModel Pattern:**

```csharp
[Authorize]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty]
    public Guid CategoryId { get; set; }

    [BindProperty]
    public decimal LimitAmount { get; set; }

    [BindProperty]
    public DateTime Month { get; set; } = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

    public async Task<IActionResult> OnGetAsync()
    {
        // Load categories for dropdown
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Build DateRange from Month (1st to last day)
        // Send CreateBudgetCommand
        // On success: redirect to /Budgets with success message
        // On DomainException: show error, retain form values
    }
}
```

**View Features**:
- Category dropdown (all user categories)
- Month picker (defaults to current month)
- Limit amount input (positive decimal, EUR)
- Submit/Cancel buttons
- Validation error display
- Currency defaults to EUR (displayed as label)

---

#### 7.3: Create Budget Edit Page (/Budgets/Edit/{id})

**Task**: Form for editing a budget's limit

**Files**:
- `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml.cs`
- `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml`

**PageModel Pattern:**

```csharp
[Authorize]
public class EditModel : PageModel
{
    private readonly IMediator _mediator;

    public BudgetStatusDto Budget { get; set; }

    [BindProperty]
    public Guid BudgetId { get; set; }

    [BindProperty]
    public decimal NewLimitAmount { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        // Load budget by ID via GetBudgetByIdQuery
        // Display current info (category/month read-only, limit editable)
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Send UpdateBudgetCommand
        // On success: redirect to /Budgets
    }
}
```

**View Features**:
- Read-only display of: Category name, Month/Period
- Editable limit amount input
- Current spend & status displayed for context
- Save/Cancel buttons

---

#### 7.4: Create Budget Detail Page (/Budgets/Detail/{id})

**Task**: Single budget detail view with progress bar and transaction list

**Files**:
- `src/SauronSheet.Frontend/Pages/Budgets/Detail.cshtml.cs`
- `src/SauronSheet.Frontend/Pages/Budgets/Detail.cshtml`

**PageModel Pattern:**

```csharp
[Authorize]
public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public BudgetStatusDto Budget { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        // Load budget via GetBudgetByIdQuery
        // Load transactions for this category + period via SearchTransactionsQuery
    }
}
```

**View Features**:
- Header: Category name, Month, Limit
- Large color-coded progress bar (via `_BudgetProgressBar` partial)
- Status badge (via `_BudgetStatusBadge` partial)
- Spend summary: Current Spend / Limit | Remaining | Percentage
- Transaction list for the category/month
- Edit button, "Back to budgets" link

---

#### 7.5: Create Budget vs. Actual Comparison Page (/Budgets/Comparison)

**Task**: All budgets vs. actual spending for a month, with horizontal bar chart

**Files**:
- `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml.cs`
- `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml`

**PageModel Pattern:**

```csharp
[Authorize]
public class ComparisonModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetVsActualDto> Comparison { get; set; } = new();
    public decimal TotalBudgeted { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalDifference { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Default to current year/month if not specified
        var year = Year ?? DateTime.UtcNow.Year;
        var month = Month ?? DateTime.UtcNow.Month;
        // Send GetBudgetVsActualQuery(year, month)
        // Calculate totals
    }
}
```

**View Features**:
- Month selector
- Comparison table: Category | Budget Limit | Actual | Difference | Status
- Summary row at bottom: totals
- Horizontal bar chart (Chart.js) showing budget vs. actual per category
- Categories with no budget show "No budget" label
- Sorted: over-budget first, then by percentage descending

---

#### 7.6: Create Reusable Shared Components

**Task**: Create reusable partials for budget progress bar and status badge

**File**: `src/SauronSheet.Frontend/Shared/_BudgetProgressBar.cshtml`

```razor
@* Budget progress bar partial view.
   Model: expects BudgetStatusDto or a dynamic with PercentageUsed and StatusLevel. *@
@model SauronSheet.Application.Features.Budgets.DTOs.BudgetStatusDto

@{
    var percentage = Math.Min(Model.PercentageUsed * 100, 100);
    var actualPercentage = Model.PercentageUsed * 100;
    var colorClass = Model.StatusLevel switch
    {
        "Green" => "bg-green-500",
        "Yellow" => "bg-yellow-500",
        "Red" => "bg-red-500",
        "Overage" => "bg-red-700",
        _ => "bg-gray-400"
    };
}

<div class="w-full bg-gray-200 rounded-full h-4 relative">
    <div class="@colorClass h-4 rounded-full transition-all duration-300"
         style="width: @(percentage)%"></div>
    @if (actualPercentage > 100)
    {
        <span class="absolute right-0 top-0 text-xs text-red-700 font-bold pr-1">
            @(Math.Round(actualPercentage, 0))%
        </span>
    }
</div>
```

**File**: `src/SauronSheet.Frontend/Shared/_BudgetStatusBadge.cshtml`

```razor
@* Budget status badge partial view.
   Model: string StatusLevel (Green, Yellow, Red, Overage). *@
@model string

@{
    var (bgColor, textColor, label) = Model switch
    {
        "Green" => ("bg-green-100", "text-green-800", "On Track"),
        "Yellow" => ("bg-yellow-100", "text-yellow-800", "Warning"),
        "Red" => ("bg-red-100", "text-red-800", "Near Limit"),
        "Overage" => ("bg-red-200", "text-red-900", "Over Budget"),
        _ => ("bg-gray-100", "text-gray-800", "Unknown")
    };
}

<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium @bgColor @textColor">
    @label
</span>
```

---

#### 7.7: Add Budget Status Widget to Dashboard

**Task**: Integrate budget status widget into the existing Dashboard page

**Modification**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs`

Add property:

```csharp
public BudgetDashboardSummaryDto? BudgetSummary { get; set; }
```

Add to `OnGetAsync()`:

```csharp
// Phase 5: Budget status widget
BudgetSummary = await _mediator.Send(
    new GetBudgetSummaryForDashboardQuery(FromDate.Year, FromDate.Month));
```

**Modification**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`

Add budget widget section after the main analytics charts:

```razor
<!-- Budget Status Widget (Phase 5) -->
<div class="bg-white rounded-lg shadow p-6">
    <h3 class="text-lg font-semibold mb-4">Budget Status</h3>
    @if (Model.BudgetSummary == null || Model.BudgetSummary.TotalBudgets == 0)
    {
        <p class="text-gray-500">No budgets set.
           <a href="/Budgets/Create" class="text-blue-600 hover:underline">
              Create budgets to track spending.
           </a>
        </p>
    }
    else
    {
        <p class="text-sm text-gray-600 mb-3">
            @Model.BudgetSummary.OnTrackCount of @Model.BudgetSummary.TotalBudgets budgets on track
            @if (Model.BudgetSummary.OverBudgetCount > 0)
            {
                <span class="text-red-600 font-medium">
                    — @Model.BudgetSummary.OverBudgetCount over limit
                </span>
            }
        </p>
        @foreach (var budget in Model.BudgetSummary.Budgets)
        {
            <div class="mb-3">
                <div class="flex justify-between text-sm mb-1">
                    <a href="/Budgets/Detail/@budget.Id" class="hover:underline">
                        @budget.CategoryName
                    </a>
                    <span>€@budget.CurrentSpend / €@budget.LimitAmount</span>
                </div>
                <partial name="_BudgetProgressBar" model="budget" />
            </div>
        }
    }
</div>
```

---

#### 7.8: Update Layout Navigation

**Task**: Add budget links to `_Layout.cshtml` navigation

**Modification**: `src/SauronSheet.Frontend/Shared/_Layout.cshtml`

Add to the authenticated nav items section (after existing Transactions / Dashboard links):

```html
<a href="/Budgets" class="text-gray-300 hover:text-white px-3 py-2 rounded-md text-sm font-medium">
    Budgets
</a>
```

---

#### 7.9: Add Category Deletion Budget Warning

**Task**: Update category delete confirmation to warn about associated budgets

**Modification**: Category delete handler (existing) and category management page

When a user deletes a category that has active budgets:
1. The delete handler queries `IBudgetRepository.GetByUserIdAsync(userId)` and filters by `CategoryId` to count associated budgets.
2. If `budgetCount > 0`, the confirmation dialog shows: **"This category has X active budget(s). Deleting will also remove them."**
3. The delete still proceeds via DB `ON DELETE CASCADE` on `category_id` FK — no domain-level blocking.
4. Budgets are tracking overlays; they should not prevent category deletion.

**Frontend implementation note**: The category management page (existing from Phase 3) must be updated to:
- Query budget count for the category before showing the delete confirmation
- Display the warning message in the Alpine.js confirmation modal
- No backend change needed beyond passing the budget count to the view

---

#### Checkpoint 6: Frontend Complete ✓

```sh
dotnet build --project src/SauronSheet.Frontend/
# Expected: Build succeeds
dotnet run --project src/SauronSheet.Frontend/
# Expected: All budget pages render correctly; dashboard widget shows budget status
```

---

### 8. INTEGRATION & VALIDATION

#### 8.1: Run Full Test Suite

```sh
dotnet test
# Expected: ~243 tests total (~186 Phase 0–4 + ~57 Phase 5)
# Expected: ALL tests PASS (green)
```

#### 8.2: Coverage Report

```sh
dotnet test --filter Category=Domain --collect:"XPlat Code Coverage"
# Expected: Domain layer ≥ 80% (Constitution minimum)
# Phase 5 additions: 29 Domain tests (19 Budget entity + 10 BudgetService)

dotnet test --filter Category=Application --collect:"XPlat Code Coverage"
# Expected: Application layer ≥ 70% (Constitution minimum)
# Phase 5 additions: 28 Application tests
```

#### 8.3: Architecture Compliance Audit

```sh
# Verify Domain.csproj has ZERO external NuGet dependencies
# Verify Application.csproj references ONLY Domain
# Verify Infrastructure.csproj references ONLY Domain
# Verify Frontend.csproj references Application + Infrastructure
# Verify no upward layer references
```

#### 8.4: E2E Budget Workflow Validation

Validate complete budget lifecycle:

1. **Auth**: User logs in → JWT established
2. **Create Budget**: Navigate to `/Budgets/Create` → Select category "Groceries" → Enter limit €500 → Select February 2026 → Submit → Redirected to `/Budgets` with budget in list
3. **View List**: Navigate to `/Budgets` → See Groceries €500 with status indicator
4. **View Detail**: Click Groceries budget → See progress bar, transaction list for Feb
5. **Edit Budget**: Click Edit → Change limit to €600 → Save → Limit updated
6. **Dashboard Widget**: Navigate to `/Dashboard` → Budget Status widget shows Groceries progress
7. **Comparison**: Navigate to `/Budgets/Comparison` → See Groceries budget vs. actual
8. **Delete Budget**: Click Delete on Groceries → Confirm → Budget removed → Transactions unaffected
9. **Duplicate Guard**: Try creating another Groceries Feb 2026 budget → Error message displayed

---

## Dependency Graph

```
Phase 5A (Budget Entity)
    ↓
Phase 5B (IBudgetRepository + BudgetService)
    ↓
Phase 5C (Budget DTOs)
    ↓
Phase 5D (Command Handlers) ←────── Phase 5C
    ↓
Phase 5E (Query Handlers) ←─────── Phase 5C, Phase 5D
    ↓
Phase 5F (DB Migration + Repository)
    ↓
Phase 5G (Budget Pages) ←────────── Phase 5D, Phase 5E, Phase 5F
    ↓
Phase 5H (Dashboard Widget + Shared Components) ←── Phase 5E, Phase 5G
    ↓
Phase 5I (Integration & Validation) ←── ALL above
```

---

## Red-Green-Refactor Workflow

The TDD workflow for each component follows this pattern:

| Step | Action | Phase | Example |
|------|--------|-------|---------|
| 1 | Write failing test stubs | RED | `BudgetTests.cs` — 19 tests fail |
| 2 | Implement minimum code to pass | GREEN | `Budget.cs` — all 19 tests pass |
| 3 | Refactor for clarity and DRY | REFACTOR | Extract shared test helpers |
| 4 | Verify no regressions | VERIFY | `dotnet test` — all prior tests still pass |

**Applied to Phase 5:**
- **Domain RED**: Write 29 test stubs (19 Budget entity + 10 BudgetService) → all fail
- **Domain GREEN**: Implement Budget entity, IBudgetRepository, BudgetService → 29 tests pass
- **Application RED**: Write 28 test stubs for handlers → all fail
- **Application GREEN**: Implement 7 handlers with DTOs → 28 tests pass
- **Refactor**: Extract shared test helpers, ensure consistent patterns

---

## Validation Checkpoints

| Checkpoint | Gate | Expected |
|------------|------|----------|
| CP-1 | Budget entity tests green | 19 Domain tests pass |
| CP-2 | Domain layer complete | ~66 Domain tests pass (37 prior + 29 new) |
| CP-3 | Command handlers complete | 12 Application tests pass |
| CP-4 | All Application tests complete | 28 budget Application tests pass |
| CP-5 | Infrastructure complete | Solution builds, DI registers all services |
| CP-6 | Frontend complete | All pages render, budget CRUD working |
| CP-7 | Full integration | ~243 total tests pass, E2E workflow validated |

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Budget entity was never implemented in Phase 2 | Scope increase: must build entity + tests from scratch | Spec already accounts for this; 17 entity tests + entity code planned explicitly |
| Current spend calculation performance | N+1 queries per budget in dashboard | Batch load transactions for the period, then filter in-memory per category |
| BudgetId value object uses `ArgumentException` not `DomainException` | Inconsistency with other VOs | Assess during implementation; existing code uses `ArgumentException` so maintain consistency |
| Chart.js horizontal bar chart for comparison | New chart type not in Phase 4 | Horizontal bar is standard Chart.js type; same CDN already loaded |
| Category deletion cascading to budget | DB `ON DELETE CASCADE` on `category_id` FK | Documented in spec (CD-5.10 edge case); UI shows warning: "This category has X active budget(s). Deleting will also remove them." Delete handler queries `IBudgetRepository` for budget count before proceeding. No domain-level blocking (budgets are tracking overlays). |
| Currency mismatch in budget calculations | Budget limit in EUR compared with spending in different currency | `EnsureSameCurrency` validation added to `IsOverBudget` and `PercentageUsed`; `RemainingAmount` delegates to `Money.Minus` which already validates. Throws `InvalidOperationException` on mismatch. |
| DateRange matching for budget lookup | PeriodStart must match exactly | Use first-day-of-month normalization in commands; `period_start` column indexed |
| Dashboard query performance with budget widget | Additional query per dashboard load | Single batch query for all budgets; spend calculated from already-loaded transactions |

---

## Files Created / Modified Summary

### New Files (Phase 5)

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `src/SauronSheet.Domain/Entities/Budget.cs` | Budget aggregate root |
| Domain | `src/SauronSheet.Domain/ValueObjects/BudgetStatusLevel.cs` | Status level enum |
| Domain | `src/SauronSheet.Domain/Repositories/IBudgetRepository.cs` | Repository interface |
| Domain | `src/SauronSheet.Domain/Services/BudgetService.cs` | Domain service (uniqueness + status) |
| Application | `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetDto.cs` | Basic budget DTO |
| Application | `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetStatusDto.cs` | Budget with status DTO |
| Application | `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetVsActualDto.cs` | Comparison DTO |
| Application | `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetDashboardSummaryDto.cs` | Dashboard widget DTO |
| Application | `src/SauronSheet.Application/Features/Budgets/Commands/CreateBudgetCommand.cs` | Create command |
| Application | `src/SauronSheet.Application/Features/Budgets/Commands/CreateBudgetCommandHandler.cs` | Create handler |
| Application | `src/SauronSheet.Application/Features/Budgets/Commands/UpdateBudgetCommand.cs` | Update command |
| Application | `src/SauronSheet.Application/Features/Budgets/Commands/UpdateBudgetCommandHandler.cs` | Update handler |
| Application | `src/SauronSheet.Application/Features/Budgets/Commands/DeleteBudgetCommand.cs` | Delete command |
| Application | `src/SauronSheet.Application/Features/Budgets/Commands/DeleteBudgetCommandHandler.cs` | Delete handler |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetsQuery.cs` | List query |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetsQueryHandler.cs` | List handler |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetByIdQuery.cs` | Detail query |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetByIdQueryHandler.cs` | Detail handler |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetVsActualQuery.cs` | Comparison query |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetVsActualQueryHandler.cs` | Comparison handler |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQuery.cs` | Dashboard widget query |
| Application | `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandler.cs` | Dashboard widget handler |
| Infrastructure | `src/SauronSheet.Infrastructure/Persistence/Migrations/006_CreateBudgetsTable.sql` | DB migration |
| Infrastructure | `src/SauronSheet.Infrastructure/Persistence/SupabaseBudgetRepository.cs` | Repository implementation |
| Frontend | `src/SauronSheet.Frontend/Pages/Budgets/Index.cshtml` + `.cs` | Budget list page |
| Frontend | `src/SauronSheet.Frontend/Pages/Budgets/Create.cshtml` + `.cs` | Budget create page |
| Frontend | `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml` + `.cs` | Budget edit page |
| Frontend | `src/SauronSheet.Frontend/Pages/Budgets/Detail.cshtml` + `.cs` | Budget detail page |
| Frontend | `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml` + `.cs` | Budget vs. actual page |
| Frontend | `src/SauronSheet.Frontend/Shared/_BudgetProgressBar.cshtml` | Reusable progress bar |
| Frontend | `src/SauronSheet.Frontend/Shared/_BudgetStatusBadge.cshtml` | Reusable status badge |
| Tests | `tests/SauronSheet.Domain.Tests/Entities/BudgetTests.cs` | 19 Budget entity tests |
| Tests | `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs` | 10 BudgetService tests |
| Tests | `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/CreateBudgetCommandHandlerTests.cs` | 5 tests |
| Tests | `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/UpdateBudgetCommandHandlerTests.cs` | 3 tests |
| Tests | `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/DeleteBudgetCommandHandlerTests.cs` | 4 tests |
| Tests | `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetsQueryHandlerTests.cs` | 4 tests |
| Tests | `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetByIdQueryHandlerTests.cs` | 3 tests |
| Tests | `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetVsActualQueryHandlerTests.cs` | 5 tests |
| Tests | `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandlerTests.cs` | 4 tests |

### Modified Files (Phase 5)

| File | Modification |
|------|-------------|
| `src/SauronSheet.Infrastructure/DependencyInjection.cs` | Register `IBudgetRepository`, `SupabaseBudgetRepository`, `BudgetService` |
| `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs` | Add `BudgetSummary` property + query in `OnGetAsync()` |
| `src/SauronSheet.Frontend/Pages/Dashboard.cshtml` | Add budget status widget section |
| `src/SauronSheet.Frontend/Shared/_Layout.cshtml` | Add "Budgets" nav link |

````
