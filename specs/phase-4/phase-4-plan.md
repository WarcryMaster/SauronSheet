````markdown
# Phase 4 Implementation Plan

**Version**: 1.0.0  
**Created**: 2026-02-19  
**Aligned with**: Constitution v1.1.0, Phase 4 Spec v1.0.0, Full Spec v1.0.0  
**Duration**: Weeks 14–18  
**Goal**: Analytics queries, dashboard UI with charts, transaction search & filtering — **MVP completion**

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

Phase 4 completes the **MVP milestone** by adding analytics queries, a dashboard with charts, and multi-filter transaction search on top of Phases 0–3 (foundation, auth, domain model, transaction CRUD). This phase is **Full-Stack (Features)** with all layers in scope.

**Key Deliverables:**
- ✅ 2 new domain specifications: `TransactionByDescriptionKeywordSpecification`, `CompositeSpecification<T>`
- ✅ 6 Application queries with handlers: GetSpendingByCategory, GetMonthlyTrends, GetYearlyComparison, GetTransactionSummary, GetRecentTransactions, SearchTransactions
- ✅ 4 Application DTOs: `CategorySpendingDto`, `MonthlyTrendDto`, `YearlyComparisonDto`, `TransactionSummaryDto`
- ✅ Dashboard page (complete replacement of Phase 1 stub) with summary cards, 3 charts (pie, line, bar), recent transactions widget
- ✅ Chart.js 4.4.0 integration via CDN (pie chart for category breakdown, line chart for monthly trends, bar chart for yearly comparison)
- ✅ Date range filter component (reusable partial view with Alpine.js toggle)
- ✅ Transaction search page (`/Transactions/Search`) with multi-filter UI and pagination
- ✅ Updated `_Layout.cshtml` with Chart.js CDN and new navigation items
- ✅ 32 passing tests (7 Domain + 25 Application)
- ✅ Cumulative ~186 tests all green (Phase 0–4)
- ✅ **MVP COMPLETE**: register → import PDF → manage transactions → analytics dashboard

**Key Constraint**: Analytics aggregation happens in Application layer (in-memory LINQ), not database views. Specifications compose filters via `CompositeSpecification.And()`. No new NuGet packages required.

**Constitutional Compliance:**
- ✅ Clean Architecture: Domain = 0 dependencies; Application → Domain only; Infrastructure → Domain only
- ✅ CQRS: 6 new Queries routed through MediatR pipeline (read-only, idempotent)
- ✅ DDD: New domain specifications with validation; CompositeSpecification composes in domain language
- ✅ Test-First: 32 tests written before code (Red-Green-Refactor); 7 Domain + 25 Application
- ✅ Spec-Driven: Single phase spec; layer boundaries respected (all layers in scope)

---

## Implementation Phases

### Phase 4A: Domain Layer — New Specifications (Days 1–2)
Add `TransactionByDescriptionKeywordSpecification` and `CompositeSpecification<T>` with full test coverage.

### Phase 4B: Application Layer — Analytics DTOs (Days 2–3)
Define `CategorySpendingDto`, `MonthlyTrendDto`, `YearlyComparisonDto`, `TransactionSummaryDto`.

### Phase 4C: Application Layer — Analytics Query Handlers (Days 3–7)
Implement 4 analytics query handlers: GetTransactionSummary, GetSpendingByCategory, GetMonthlyTrends, GetYearlyComparison (14 tests).

### Phase 4D: Application Layer — Transaction Queries (Days 7–9)
Implement GetRecentTransactions and SearchTransactions handlers (11 tests).

### Phase 4E: Frontend — Dashboard Page (Days 9–12)
Complete rewrite of Dashboard page with summary cards, Chart.js charts, recent transactions, date range filter.

### Phase 4F: Frontend — Search Page & Navigation (Days 12–14)
Build Transaction search page, reusable date filter partial, update layout and navigation.

### Phase 4G: Integration & MVP Validation (Days 14–16)
E2E testing, coverage reporting, all ~186 tests passing, MVP workflow validation.

---

## Task Breakdown by Component

### 0. PRE-IMPLEMENTATION

#### 0.1: Environment Validation

**Task**: Verify Phase 0, 1, 2, 3 completion and Phase 4 readiness

```sh
✓ Phase 0 build passing         # dotnet build
✓ Phase 0 tests passing         # 13 tests green
✓ Phase 1 build passing         # dotnet build
✓ Phase 1 tests passing         # 22 tests green
✓ Phase 2 build passing         # dotnet build
✓ Phase 2 tests passing         # 81 tests green (domain-only)
✓ Phase 3 build passing         # dotnet build
✓ Phase 3 tests passing         # 38 tests green (full-stack)
✓ Total tests passing           # ~154 tests green
✓ Domain project zero deps      # Verify Domain.csproj has NO external packages
✓ Supabase Auth + RLS working   # Phase 1+3 auth/persistence functional
✓ TransactionByAmountRangeSpec  # Already exists (6 tests green)
✓ Git workspace clean           # Phase 3 merged to main
```

**Acceptance Criteria:**
- Phase 0 + Phase 1 + Phase 2 + Phase 3 combined tests pass (~154 tests total)
- Domain layer has ZERO external NuGet dependencies
- All Phase 3 entities, handlers, and repositories are stable
- Supabase Auth and persistence are working end-to-end
- `TransactionByAmountRangeSpecification` exists and is tested (6 tests)
- Git workspace is clean (ready for Phase 4 development)

---

### 1. DOMAIN LAYER — NEW SPECIFICATIONS

#### 1.1: Write Domain.Tests for TransactionByDescriptionKeywordSpecification (RED Phase)

**Task**: Create test stubs for keyword specification (4 tests)

**Directory structure** (create if not exists):
```sh
mkdir -p tests/SauronSheet.Domain.Tests/Specifications
```

**File**: `tests/SauronSheet.Domain.Tests/Specifications/TransactionByDescriptionKeywordSpecificationTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Specifications;

public class TransactionByDescriptionKeywordSpecificationTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_MatchesPartialKeyword()
    {
        // RED: Will fail until TransactionByDescriptionKeywordSpecification implemented
        // GIVEN a Transaction with description = "Morning Coffee at Starbucks"
        // AND TransactionByDescriptionKeywordSpecification("coffee")
        // WHEN Criteria is compiled and evaluated
        // THEN returns true (case-insensitive partial match)
        Assert.True(false, "Implement TransactionByDescriptionKeywordSpecification");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_NoMatch_ReturnsFalse()
    {
        // GIVEN a Transaction with description = "Grocery shopping"
        // AND TransactionByDescriptionKeywordSpecification("coffee")
        // WHEN Criteria is compiled and evaluated
        // THEN returns false
        Assert.True(false, "Implement no-match case");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_EmptyKeyword_ThrowsDomainException()
    {
        // GIVEN keyword = ""
        // WHEN TransactionByDescriptionKeywordSpecification is constructed
        // THEN throws DomainException with message containing "keyword cannot be empty"
        Assert.True(false, "Implement empty keyword guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_CaseInsensitive()
    {
        // GIVEN a Transaction with description = "COFFEE BEANS"
        // AND TransactionByDescriptionKeywordSpecification("coffee")
        // WHEN Criteria is compiled and evaluated
        // THEN returns true
        Assert.True(false, "Implement case-insensitive matching");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 4 new Domain tests FAIL (red) — TransactionByDescriptionKeywordSpec not yet implemented
# Expected: ~30 prior domain tests still PASS
```

---

#### 1.2: Write Domain.Tests for CompositeSpecification\<T\> (RED Phase)

**Task**: Create test stubs for composite specification (3 tests)

**File**: `tests/SauronSheet.Domain.Tests/Specifications/CompositeSpecificationTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Specifications;

public class CompositeSpecificationTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void CompositeSpec_And_CombinesTwoSpecs()
    {
        // GIVEN TransactionByUserSpecification(userA) AND TransactionByCategorySpecification(catX)
        // AND a Transaction belonging to userA with categoryId = catX
        // WHEN composed spec Criteria is compiled and evaluated
        // THEN returns true
        Assert.True(false, "Implement CompositeSpecification.And");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CompositeSpec_And_RejectsMismatch()
    {
        // GIVEN TransactionByUserSpecification(userA) AND TransactionByCategorySpecification(catX)
        // AND a Transaction belonging to userA with categoryId = catY (different)
        // WHEN composed spec Criteria is compiled and evaluated
        // THEN returns false (user matches but category doesn't)
        Assert.True(false, "Implement CompositeSpecification mismatch rejection");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CompositeSpec_And_MultipleSpecs()
    {
        // GIVEN specs: user + dateRange + category
        // AND a Transaction matching user and dateRange but NOT category
        // WHEN all three composed with And
        // THEN returns false (all conditions must be true)
        Assert.True(false, "Implement multiple spec composition");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 7 new Domain tests FAIL (red) — specifications not yet implemented
```

---

#### 1.3: Implement TransactionByDescriptionKeywordSpecification (GREEN Phase)

**Task**: Create keyword specification with validation

**File**: `src/SauronSheet.Domain/Specifications/TransactionByDescriptionKeywordSpecification.cs`

```csharp
namespace SauronSheet.Domain.Specifications;

using Entities;
using Exceptions;

/// <summary>
/// Specification that filters transactions by keyword in description.
/// Case-insensitive partial match using Contains.
/// </summary>
public class TransactionByDescriptionKeywordSpecification : BaseSpecification<Transaction>
{
    public TransactionByDescriptionKeywordSpecification(string keyword)
        : base(t => t.Description.ToLower().Contains(keyword.ToLower()))
    {
        if (string.IsNullOrWhiteSpace(keyword))
            throw new DomainException("Search keyword cannot be empty.");
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~TransactionByDescriptionKeyword" --no-build
# Expected: 4 keyword spec tests PASS
```

---

#### 1.4: Implement CompositeSpecification\<T\> (GREEN Phase)

**Task**: Create composite specification with AND logic

**File**: `src/SauronSheet.Domain/Specifications/CompositeSpecification.cs`

```csharp
namespace SauronSheet.Domain.Specifications;

using System;
using System.Linq.Expressions;
using Repositories;

/// <summary>
/// Combines multiple specifications using AND logic.
/// Uses Expression.Invoke + Expression.AndAlso for expression composition.
/// </summary>
/// <remarks>
/// Known limitation: Expression.Invoke does not translate to SQL in most ORMs.
/// Since Supabase Postgrest evaluates specifications in-memory after data fetch,
/// this works correctly for SauronSheet's architecture.
/// </remarks>
public class CompositeSpecification<T> : BaseSpecification<T> where T : class
{
    private CompositeSpecification(Expression<Func<T, bool>> criteria)
        : base(criteria)
    { }

    /// <summary>
    /// Combines two specifications with AND logic.
    /// Both left and right criteria must be satisfied.
    /// </summary>
    public static CompositeSpecification<T> And(
        ISpecification<T> left,
        ISpecification<T> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.AndAlso(
            Expression.Invoke(left.Criteria, parameter),
            Expression.Invoke(right.Criteria, parameter));
        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);

        return new CompositeSpecification<T>(lambda);
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~CompositeSpecification" --no-build
# Expected: 3 composite spec tests PASS
```

---

#### Checkpoint 1: Domain Layer Complete ✓

```sh
dotnet test --filter Category=Domain --no-build
# Expected: ~37 domain tests PASS (30 Phase 0+1+2+3 + 7 Phase 4)
```

**Status**: All domain spec tests passing → Proceed to Phase 4B (Application DTOs)

---

### 2. APPLICATION LAYER — ANALYTICS DTOs

#### 2.1: Create Analytics DTOs (GREEN Phase)

**Task**: Define data transfer objects for analytics queries

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Application/Features/Analytics/DTOs
mkdir -p src/SauronSheet.Application/Features/Analytics/Queries
```

**File**: `src/SauronSheet.Application/Features/Analytics/DTOs/CategorySpendingDto.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.DTOs;

public record CategorySpendingDto(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount,
    string Currency,
    decimal Percentage);
```

**File**: `src/SauronSheet.Application/Features/Analytics/DTOs/MonthlyTrendDto.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.DTOs;

public record MonthlyTrendDto(
    int Month,
    string MonthName,
    decimal TotalExpenses,
    decimal TotalIncome,
    decimal NetAmount,
    string Currency,
    int TransactionCount);
```

**File**: `src/SauronSheet.Application/Features/Analytics/DTOs/YearlyComparisonDto.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.DTOs;

public record YearlyComparisonDto(
    int Month,
    string MonthName,
    decimal Year1Amount,
    decimal Year2Amount,
    decimal Difference,
    decimal? PercentageChange,
    string Currency);
```

**File**: `src/SauronSheet.Application/Features/Analytics/DTOs/TransactionSummaryDto.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.DTOs;

public record TransactionSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount,
    int TransactionCount,
    string Currency,
    DateTime FromDate,
    DateTime ToDate);
```

**Verification**:

```sh
dotnet build
# Expected: Build succeeds (DTOs compile)
```

---

### 3. APPLICATION LAYER — ANALYTICS QUERY HANDLERS

#### 3.1: Write Application.Tests for GetTransactionSummaryQuery (RED Phase)

**Task**: Create test stubs for transaction summary handler (4 tests)

**Directory structure**:
```sh
mkdir -p tests/SauronSheet.Application.Tests/Features/Analytics/Queries
```

**File**: `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetTransactionSummaryQueryTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Common;
using SauronSheet.Domain.Repositories;

namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

public class GetTransactionSummaryQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_CalculatesCorrectly()
    {
        // RED: Will fail until GetTransactionSummaryQueryHandler implemented
        // GIVEN transactions: +€500, +€200 (income), -€300, -€100, -€50 (expenses)
        // WHEN handler processes query
        // THEN TotalIncome=700, TotalExpenses=450, NetAmount=250, Count=5
        Assert.True(false, "Implement GetTransactionSummaryQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_NoTransactions_ReturnsZeros()
    {
        // GIVEN no transactions for user in date range
        // THEN TotalIncome=0, TotalExpenses=0, NetAmount=0, Count=0
        Assert.True(false, "Implement empty result handling");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_OnlyExpenses_NetIsNegative()
    {
        // GIVEN transactions: -€300, -€200 (expenses only)
        // THEN TotalIncome=0, TotalExpenses=500, NetAmount=-500
        Assert.True(false, "Implement negative net handling");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_RespectsDateRange()
    {
        // GIVEN transactions in January (€100) and February (€200)
        // AND date range covers only January
        // THEN TotalExpenses=100, Count=1
        Assert.True(false, "Implement date range filtering");
    }
}
```

---

#### 3.2: Write Application.Tests for GetSpendingByCategoryQuery (RED Phase)

**Task**: Create test stubs for spending by category handler (4 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetSpendingByCategoryQueryTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Common;
using SauronSheet.Domain.Repositories;

namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

public class GetSpendingByCategoryQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_WithTransactions_ReturnsGroupedData()
    {
        // RED: Will fail until handler implemented
        // GIVEN 5 transactions: 2 in "Groceries" (€50+€30), 2 in "Transport" (€20+€10), 1 uncategorized (€15)
        // THEN returns 3 entries, Groceries=80 (64%), Transport=30 (24%), Uncategorized=15 (12%)
        Assert.True(false, "Implement GetSpendingByCategoryQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_NoTransactions_ReturnsEmptyList()
    {
        Assert.True(false, "Implement empty result handling");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_OnlyIncomeTransactions_ReturnsEmptyList()
    {
        // Only positive-amount transactions → empty (only expenses analyzed)
        Assert.True(false, "Implement income filtering");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_MoreThanSixCategories_GroupsIntoOther()
    {
        // 8 categories → returns 7 entries (top 6 + "Other")
        Assert.True(false, "Implement Other grouping");
    }
}
```

---

#### 3.3: Write Application.Tests for GetMonthlyTrendsQuery (RED Phase)

**Task**: Create test stubs for monthly trends handler (3 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlyTrendsQueryTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Common;
using SauronSheet.Domain.Repositories;

namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

public class GetMonthlyTrendsQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetMonthlyTrends_FullYear_Returns12Entries()
    {
        // RED: Will fail until handler implemented
        // GIVEN transactions spread across Jan, Mar, Jun, Dec
        // THEN returns exactly 12 entries; months without transactions have zero amounts
        Assert.True(false, "Implement GetMonthlyTrendsQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetMonthlyTrends_NoTransactions_Returns12ZeroEntries()
    {
        // GIVEN no transactions → returns 12 entries all with zeros
        Assert.True(false, "Implement empty year handling");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetMonthlyTrends_SeparatesIncomeAndExpenses()
    {
        // GIVEN January: €500 income, -€300 expense, -€100 expense
        // THEN January entry: TotalIncome=500, TotalExpenses=400, NetAmount=100
        Assert.True(false, "Implement income/expense separation");
    }
}
```

---

#### 3.4: Write Application.Tests for GetYearlyComparisonQuery (RED Phase)

**Task**: Create test stubs for yearly comparison handler (3 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetYearlyComparisonQueryTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Common;
using SauronSheet.Domain.Repositories;

namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

public class GetYearlyComparisonQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_TwoYears_ReturnsMonthlyComparison()
    {
        // RED: Will fail until handler implemented
        // GIVEN Year 2025: Jan €100, Feb €200; Year 2026: Jan €150, Feb €180
        // THEN 12 entries: Jan={100,150,50}, Feb={200,180,-20}
        Assert.True(false, "Implement GetYearlyComparisonQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_NoDataForOneYear_ReturnsZeros()
    {
        // Year 2024 has none → Year1Amount = 0 for all months
        Assert.True(false, "Implement missing year handling");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_PercentageChange_ZeroDivision()
    {
        // Year1 January = €0, Year2 January = €150 → PercentageChange = null
        Assert.True(false, "Implement zero division protection");
    }
}
```

---

#### 3.5: Implement GetTransactionSummaryQuery + Handler (GREEN Phase)

**Task**: Create transaction summary query and handler

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetTransactionSummaryQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

public record GetTransactionSummaryQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<TransactionSummaryDto>;
```

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetTransactionSummaryQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetTransactionSummaryQueryHandler
    : IRequestHandler<GetTransactionSummaryQuery, TransactionSummaryDto>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetTransactionSummaryQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<TransactionSummaryDto> Handle(
        GetTransactionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Compose specifications: user + date range
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(
            new DateRange(request.FromDate, request.ToDate));
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        var totalIncome = transactions
            .Where(t => t.Amount.IsPositive)
            .Sum(t => t.Amount.Amount);

        var totalExpenses = transactions
            .Where(t => t.Amount.IsNegative)
            .Sum(t => Math.Abs(t.Amount.Amount));

        return new TransactionSummaryDto(
            totalIncome,
            totalExpenses,
            totalIncome - totalExpenses,
            transactions.Count,
            "EUR",
            request.FromDate,
            request.ToDate);
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~GetTransactionSummaryQuery" --no-build
# Expected: 4 summary tests PASS
```

---

#### 3.6: Implement GetSpendingByCategoryQuery + Handler (GREEN Phase)

**Task**: Create spending by category query and handler

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetSpendingByCategoryQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

public record GetSpendingByCategoryQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<List<CategorySpendingDto>>;
```

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetSpendingByCategoryQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetSpendingByCategoryQueryHandler
    : IRequestHandler<GetSpendingByCategoryQuery, List<CategorySpendingDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetSpendingByCategoryQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<CategorySpendingDto>> Handle(
        GetSpendingByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Compose specifications: user + date range
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(
            new DateRange(request.FromDate, request.ToDate));
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Filter to expenses only (negative amounts)
        var expenses = transactions.Where(t => t.Amount.IsNegative).ToList();
        if (!expenses.Any())
            return new List<CategorySpendingDto>();

        var totalSpending = expenses.Sum(t => Math.Abs(t.Amount.Amount));

        // Load categories for name/color lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        // Group by category
        var grouped = expenses
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var amount = g.Sum(t => Math.Abs(t.Amount.Amount));
                var categoryName = "Uncategorized";
                string? categoryColor = null;

                if (g.Key != null && categoryLookup.TryGetValue(g.Key, out var category))
                {
                    categoryName = category.Name;
                    categoryColor = category.Color;
                }

                return new CategorySpendingDto(
                    g.Key?.Value,
                    categoryName,
                    categoryColor,
                    amount,
                    "EUR",
                    totalSpending > 0 ? Math.Round(amount / totalSpending * 100, 1) : 0);
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // If more than 6 categories, group remaining into "Other"
        if (grouped.Count > 6)
        {
            var top6 = grouped.Take(6).ToList();
            var otherAmount = grouped.Skip(6).Sum(c => c.Amount);
            var otherPercentage = totalSpending > 0 ? Math.Round(otherAmount / totalSpending * 100, 1) : 0;
            top6.Add(new CategorySpendingDto(null, "Other", "#6B7280", otherAmount, "EUR", otherPercentage));
            return top6;
        }

        return grouped;
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~GetSpendingByCategoryQuery" --no-build
# Expected: 4 spending by category tests PASS
```

---

#### 3.7: Implement GetMonthlyTrendsQuery + Handler (GREEN Phase)

**Task**: Create monthly trends query and handler

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlyTrendsQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

public record GetMonthlyTrendsQuery(
    int Year) : IRequest<List<MonthlyTrendDto>>;
```

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlyTrendsQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using System.Globalization;
using Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetMonthlyTrendsQueryHandler
    : IRequestHandler<GetMonthlyTrendsQuery, List<MonthlyTrendDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetMonthlyTrendsQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<MonthlyTrendDto>> Handle(
        GetMonthlyTrendsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Load all transactions for the year
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(
            new DateRange(
                new DateTime(request.Year, 1, 1),
                new DateTime(request.Year, 12, 31)));
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Group by month and calculate
        var grouped = transactions.GroupBy(t => t.Date.Month);
        var monthLookup = grouped.ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<MonthlyTrendDto>();
        for (int month = 1; month <= 12; month++)
        {
            var monthTransactions = monthLookup.GetValueOrDefault(month, new());
            var income = monthTransactions.Where(t => t.Amount.IsPositive).Sum(t => t.Amount.Amount);
            var expenses = monthTransactions.Where(t => t.Amount.IsNegative).Sum(t => Math.Abs(t.Amount.Amount));

            result.Add(new MonthlyTrendDto(
                month,
                CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                expenses,
                income,
                income - expenses,
                "EUR",
                monthTransactions.Count));
        }

        return result;
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~GetMonthlyTrendsQuery" --no-build
# Expected: 3 monthly trends tests PASS
```

---

#### 3.8: Implement GetYearlyComparisonQuery + Handler (GREEN Phase)

**Task**: Create yearly comparison query and handler

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetYearlyComparisonQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

public record GetYearlyComparisonQuery(
    int Year1,
    int Year2) : IRequest<List<YearlyComparisonDto>>;
```

**File**: `src/SauronSheet.Application/Features/Analytics/Queries/GetYearlyComparisonQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Analytics.Queries;

using System.Globalization;
using Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetYearlyComparisonQueryHandler
    : IRequestHandler<GetYearlyComparisonQuery, List<YearlyComparisonDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetYearlyComparisonQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<YearlyComparisonDto>> Handle(
        GetYearlyComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Load Year 1 transactions
        var year1Spec = CompositeSpecification<Domain.Entities.Transaction>.And(
            new TransactionByUserSpecification(userId),
            new TransactionByDateRangeSpecification(
                new DateRange(new DateTime(request.Year1, 1, 1), new DateTime(request.Year1, 12, 31))));
        var year1Transactions = await _transactionRepo.FindBySpecificationAsync(year1Spec);

        // Load Year 2 transactions
        var year2Spec = CompositeSpecification<Domain.Entities.Transaction>.And(
            new TransactionByUserSpecification(userId),
            new TransactionByDateRangeSpecification(
                new DateRange(new DateTime(request.Year2, 1, 1), new DateTime(request.Year2, 12, 31))));
        var year2Transactions = await _transactionRepo.FindBySpecificationAsync(year2Spec);

        // Group by month (expenses only)
        var year1Monthly = year1Transactions
            .Where(t => t.Amount.IsNegative)
            .GroupBy(t => t.Date.Month)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        var year2Monthly = year2Transactions
            .Where(t => t.Amount.IsNegative)
            .GroupBy(t => t.Date.Month)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        var result = new List<YearlyComparisonDto>();
        for (int month = 1; month <= 12; month++)
        {
            var y1Amount = year1Monthly.GetValueOrDefault(month, 0);
            var y2Amount = year2Monthly.GetValueOrDefault(month, 0);
            var difference = y2Amount - y1Amount;
            decimal? percentageChange = y1Amount > 0
                ? Math.Round((difference / y1Amount) * 100, 1)
                : null; // null when Year1 is 0 (avoid division by zero)

            result.Add(new YearlyComparisonDto(
                month,
                CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                y1Amount,
                y2Amount,
                difference,
                percentageChange,
                "EUR"));
        }

        return result;
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~GetYearlyComparisonQuery" --no-build
# Expected: 3 yearly comparison tests PASS
```

---

#### Checkpoint 2: Analytics Handlers Complete ✓

```sh
dotnet test --filter Category=Application --no-build
# Expected: Prior Phase 1+3 tests + 14 new analytics tests PASS
```

**Status**: All analytics handlers passing → Proceed to Phase 4D (Transaction Queries)

---

### 4. APPLICATION LAYER — TRANSACTION QUERIES

#### 4.1: Write Application.Tests for GetRecentTransactionsQuery (RED Phase)

**Task**: Create test stubs for recent transactions handler (3 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetRecentTransactionsQueryTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Application.Common;
using SauronSheet.Domain.Repositories;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

public class GetRecentTransactionsQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_ReturnsLastN()
    {
        // RED: Will fail until GetRecentTransactionsQueryHandler implemented
        // GIVEN 20 transactions → request count=10
        // THEN returns 10, sorted by date descending
        Assert.True(false, "Implement GetRecentTransactionsQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_FewerThanN_ReturnsAll()
    {
        // GIVEN 3 transactions → request count=10 → returns 3
        Assert.True(false, "Implement fewer-than-N handling");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_NoTransactions_ReturnsEmptyList()
    {
        Assert.True(false, "Implement empty result handling");
    }
}
```

---

#### 4.2: Write Application.Tests for SearchTransactionsQuery (RED Phase)

**Task**: Create test stubs for search handler (8 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/SearchTransactionsQueryTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Application.Common;
using SauronSheet.Domain.Repositories;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

public class SearchTransactionsQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByKeyword_FiltersCorrectly()
    {
        // RED: Will fail until SearchTransactionsQueryHandler implemented
        // GIVEN transactions: "Coffee shop", "Grocery store", "Coffee beans", "Gas station"
        // AND keyword="coffee" → returns 2 (case-insensitive)
        Assert.True(false, "Implement SearchTransactionsQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByDateRange_FiltersCorrectly()
    {
        // GIVEN: Jan 5, Jan 15, Feb 1, Feb 20
        // AND fromDate=Jan 10, toDate=Feb 5 → returns 2 (Jan 15, Feb 1)
        Assert.True(false, "Implement date range filtering");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByCategory_FiltersCorrectly()
    {
        // GIVEN: 3 in "Groceries", 2 in "Transport"
        // AND categoryId=GroceriesId → returns 3
        Assert.True(false, "Implement category filtering");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByAmountRange_FiltersCorrectly()
    {
        // GIVEN amounts: €10, €50, €100, €200, €500
        // AND minAmount=50, maxAmount=200 → returns 3
        Assert.True(false, "Implement amount range filtering");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_CombinedFilters_AppliesAll()
    {
        // keyword + categoryId + fromDate → only matching ALL criteria
        Assert.True(false, "Implement combined filter logic");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_NoFilters_ReturnsAllUserTransactions()
    {
        // 10 transactions, no filters → returns all 10 (paginated)
        Assert.True(false, "Implement no-filter case");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_NoResults_ReturnsEmptyPage()
    {
        // Filters match nothing → PaginatedResultDto with Items=empty, TotalCount=0
        Assert.True(false, "Implement empty result page");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_Paginated_RespectsPageSize()
    {
        // 100 matching, page=2, pageSize=25 → 25 items, TotalCount=100, TotalPages=4
        Assert.True(false, "Implement pagination");
    }
}
```

---

#### 4.3: Implement GetRecentTransactionsQuery + Handler (GREEN Phase)

**Task**: Create recent transactions query and handler

**File**: `src/SauronSheet.Application/Features/Transactions/Queries/GetRecentTransactionsQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Queries;

using DTOs;
using MediatR;

public record GetRecentTransactionsQuery(
    int Count = 10) : IRequest<List<TransactionDto>>;
```

**File**: `src/SauronSheet.Application/Features/Transactions/Queries/GetRecentTransactionsQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Queries;

using Common;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetRecentTransactionsQueryHandler
    : IRequestHandler<GetRecentTransactionsQuery, List<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetRecentTransactionsQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<TransactionDto>> Handle(
        GetRecentTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var transactions = await _transactionRepo.GetByUserIdAsync(userId);

        // Load categories for name lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        return transactions
            .OrderByDescending(t => t.Date)
            .Take(request.Count)
            .Select(t => new TransactionDto(
                t.Id.Value,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Date,
                t.Description,
                t.CategoryId?.Value,
                t.CategoryId != null && categoryLookup.TryGetValue(t.CategoryId, out var name)
                    ? name : null,
                t.ImportedFrom,
                t.CreatedAt))
            .ToList();
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~GetRecentTransactionsQuery" --no-build
# Expected: 3 recent transactions tests PASS
```

---

#### 4.4: Implement SearchTransactionsQuery + Handler (GREEN Phase)

**Task**: Create search transactions query and handler

**File**: `src/SauronSheet.Application/Features/Transactions/Queries/SearchTransactionsQuery.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Queries;

using DTOs;
using MediatR;

public record SearchTransactionsQuery(
    string? Keyword = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? CategoryId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PaginatedResultDto<TransactionDto>>;
```

**File**: `src/SauronSheet.Application/Features/Transactions/Queries/SearchTransactionsQueryHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Queries;

using Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class SearchTransactionsQueryHandler
    : IRequestHandler<SearchTransactionsQuery, PaginatedResultDto<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public SearchTransactionsQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<PaginatedResultDto<TransactionDto>> Handle(
        SearchTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Build composed specification starting with user scope
        ISpecification<Domain.Entities.Transaction> spec =
            new TransactionByUserSpecification(userId);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByDescriptionKeywordSpecification(request.Keyword));
        }

        if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByDateRangeSpecification(
                    new DateRange(request.FromDate.Value, request.ToDate.Value)));
        }

        if (request.CategoryId.HasValue)
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByCategorySpecification(
                    new CategoryId(request.CategoryId.Value)));
        }

        if (request.MinAmount.HasValue && request.MaxAmount.HasValue)
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByAmountRangeSpecification(
                    new Money(request.MinAmount.Value),
                    new Money(request.MaxAmount.Value)));
        }

        // Execute query
        var allMatching = await _transactionRepo.FindBySpecificationAsync(spec);

        // Load categories for name lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        // Apply pagination
        var totalCount = allMatching.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var pagedItems = allMatching
            .OrderByDescending(t => t.Date)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransactionDto(
                t.Id.Value,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Date,
                t.Description,
                t.CategoryId?.Value,
                t.CategoryId != null && categoryLookup.TryGetValue(t.CategoryId, out var name)
                    ? name : null,
                t.ImportedFrom,
                t.CreatedAt))
            .ToList();

        return new PaginatedResultDto<TransactionDto>(
            pagedItems,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages);
    }
}
```

**Verification**:

```sh
dotnet test --filter "ClassName~SearchTransactionsQuery" --no-build
# Expected: 8 search tests PASS
```

---

#### Checkpoint 3: All Application Handlers Complete ✓

```sh
dotnet test --filter Category=Application --no-build
# Expected: All Phase 1+3 tests + 25 new Phase 4 tests PASS
```

**Status**: All application handlers passing → Proceed to Phase 4E (Frontend Dashboard)

---

### 5. FRONTEND — DASHBOARD PAGE

#### 5.1: Update _Layout.cshtml with Chart.js CDN and Navigation

**Task**: Add Chart.js CDN script and update navigation for Phase 4

**File**: `src/SauronSheet.Frontend/Shared/_Layout.cshtml` (UPDATE existing)

**Add to `<head>` section:**
```html
<!-- Chart.js for analytics charts -->
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
```

**Update navigation items (authenticated):**
```html
<!-- Add after existing nav items -->
<a href="/Dashboard" class="...">📊 Dashboard</a>
<a href="/Transactions" class="...">💳 Transactions</a>
<a href="/Transactions/Search" class="...">🔍 Search</a>
<a href="/Transactions/Upload" class="...">📄 Upload PDF</a>
<a href="/Categories" class="...">🏷️ Categories</a>
```

---

#### 5.2: Implement Dashboard PageModel

**Task**: Complete rewrite of Dashboard page (replaces Phase 1 stub)

**File**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Queries;

namespace SauronSheet.Frontend.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public DashboardModel(IMediator mediator) => _mediator = mediator;

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
```

---

#### 5.3: Implement Dashboard View

**Task**: Create Dashboard Razor view with summary cards, charts, recent transactions, date filter

**File**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`

**Key Sections:**
1. **Date Range Filter** — `<partial name="Components/_DateRangeFilter" />` with query parameter bindings
2. **Summary Cards** — 4-column grid (Income green, Expenses red, Net green/red, Count blue)
3. **Charts Section** — 2-column grid on desktop:
   - Pie chart: `<canvas id="categoryPieChart">`
   - Line chart: `<canvas id="monthlyTrendsChart">`
   - Bar chart: `<canvas id="yearlyComparisonChart">`
4. **Recent Transactions** — Table with Date, Description, Amount (colored), Category
5. **Chart.js Initialization** — `<script>` block with `@Html.Raw(Json.Serialize(Model.SpendingByCategory))` etc.
6. **Empty State** — Shown when `Model.Summary.TransactionCount == 0`

---

#### 5.4: Create Date Range Filter Partial

**Task**: Create reusable date range filter component

**File**: `src/SauronSheet.Frontend/Shared/Components/_DateRangeFilter.cshtml`

```html
<!-- Reusable partial view using Alpine.js -->
<div x-data="{ showCustom: false }" class="flex flex-wrap gap-2 items-center mb-6">
    <select name="DateFilter" class="rounded-md border-gray-300 shadow-sm ..."
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
    <button type="submit" class="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700">
        Apply
    </button>
</div>
```

---

#### 5.5: Create Chart.js Initialization Script

**Task**: Create reusable chart initialization JavaScript

**File**: `src/SauronSheet.Frontend/wwwroot/js/charts.js`

```javascript
// Chart.js initialization functions for SauronSheet analytics

/**
 * Initialize pie chart for spending by category
 */
function initCategoryPieChart(canvasId, categoryData) {
    const defaultColors = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899', '#6B7280'];
    new Chart(document.getElementById(canvasId), {
        type: 'pie',
        data: {
            labels: categoryData.map(c => c.categoryName),
            datasets: [{
                data: categoryData.map(c => c.amount),
                backgroundColor: categoryData.map((c, i) => c.categoryColor || defaultColors[i % defaultColors.length])
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'bottom' },
                tooltip: {
                    callbacks: {
                        label: (ctx) => {
                            const item = categoryData[ctx.dataIndex];
                            return `${ctx.label}: €${ctx.parsed.toFixed(2)} (${item.percentage.toFixed(1)}%)`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * Initialize line chart for monthly trends
 */
function initMonthlyTrendsChart(canvasId, monthlyData) {
    new Chart(document.getElementById(canvasId), {
        type: 'line',
        data: {
            labels: monthlyData.map(m => m.monthName),
            datasets: [
                {
                    label: 'Expenses',
                    data: monthlyData.map(m => m.totalExpenses),
                    borderColor: '#EF4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.1)',
                    tension: 0.3,
                    fill: true
                },
                {
                    label: 'Income',
                    data: monthlyData.map(m => m.totalIncome),
                    borderColor: '#10B981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    tension: 0.3,
                    fill: true
                }
            ]
        },
        options: {
            responsive: true,
            scales: { y: { beginAtZero: true } },
            plugins: { legend: { position: 'bottom' } }
        }
    });
}

/**
 * Initialize bar chart for yearly comparison
 */
function initYearlyComparisonChart(canvasId, yearlyData, year1Label, year2Label) {
    new Chart(document.getElementById(canvasId), {
        type: 'bar',
        data: {
            labels: yearlyData.map(y => y.monthName),
            datasets: [
                {
                    label: year1Label,
                    data: yearlyData.map(y => y.year1Amount),
                    backgroundColor: '#3B82F6'
                },
                {
                    label: year2Label,
                    data: yearlyData.map(y => y.year2Amount),
                    backgroundColor: '#8B5CF6'
                }
            ]
        },
        options: {
            responsive: true,
            scales: { y: { beginAtZero: true } },
            plugins: { legend: { position: 'bottom' } }
        }
    });
}
```

---

#### Checkpoint 4: Dashboard Page Complete ✓

```sh
dotnet run --project src/SauronSheet.Frontend/
# Visual Check (in browser):
# ✓ /Dashboard loads with summary cards
# ✓ Pie chart renders (or empty state shown)
# ✓ Line chart renders 12 months
# ✓ Bar chart renders yearly comparison
# ✓ Recent transactions table visible
# ✓ Date range filter functional
# ✓ No JavaScript console errors
```

**Status**: Dashboard rendering → Proceed to Phase 4F (Search Page)

---

### 6. FRONTEND — SEARCH PAGE & NAVIGATION

#### 6.1: Implement Search Transactions PageModel

**Task**: Create multi-filter search page

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Search.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Application.Features.Categories.Queries;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class SearchModel : PageModel
{
    private readonly IMediator _mediator;

    public SearchModel(IMediator mediator) => _mediator = mediator;

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
```

---

#### 6.2: Implement Search View

**Task**: Create Search Razor view with filter panel and paginated results

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Search.cshtml`

**Key Sections:**
1. **Filter Panel** — Form with:
   - Keyword text input with 🔍 icon
   - Date range: From and To date pickers
   - Category dropdown (populated from `Model.Categories` + "All Categories")
   - Amount range: Min and Max number inputs
   - Search button + Clear filters link (`href="/Transactions/Search"`)
2. **Result Summary** — "Showing N of M transactions matching filters"
3. **Results Table** — Same format as transaction list (Date, Description, Amount, Category)
4. **Pagination Controls** — Previous / Next / Page numbers
5. **Empty State** — "No transactions match your filters."

**All filter values bound via `SupportsGet = true` → preserved in URL query parameters**

---

#### 6.3: Update Transaction List with Filter Controls

**Task**: Add date range filter and category filter to existing `/Transactions` page

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml` (UPDATE)

**Add at top of page:**
- Include `_DateRangeFilter` partial view
- Category dropdown filter
- Apply button → reloads page with query parameters

---

#### Checkpoint 5: Frontend Complete ✓

```sh
dotnet run --project src/SauronSheet.Frontend/
# Visual Check:
# ✓ /Dashboard — full analytics dashboard with cards, charts, recent transactions
# ✓ /Transactions/Search — filter panel with all 5 filter types
# ✓ /Transactions — updated with filter controls
# ✓ Navigation: Dashboard, Transactions, Search, Upload, Categories links
# ✓ Chart.js loaded (3 charts render on Dashboard)
# ✓ Alpine.js toggle works (custom date range)
# ✓ No JavaScript console errors
# ✓ Responsive: 320px, 768px, 1024px viewports
```

**Status**: All frontend pages working → Proceed to Phase 4G (Integration & MVP Validation)

---

### 7. INTEGRATION & MVP VALIDATION

#### 7.1: Run Full Test Suite

**Task**: Verify all tests pass (Phase 0 through Phase 4)

```sh
dotnet build
# Expected: Zero errors, zero warnings

dotnet test
# Expected: ~186 tests PASS (Phase 0: 13 + Phase 1: 22 + Phase 2: 81 + Phase 3: 38 + Phase 4: 32)
# Phase 4 breakdown: 7 Domain + 25 Application
```

---

#### 7.2: Coverage Verification

**Task**: Verify test coverage meets constitution requirements

```sh
# Domain coverage (≥ 80% global minimum)
dotnet test tests/SauronSheet.Domain.Tests/ --collect:"XPlat Code Coverage"
# Expected: ~37 domain tests → Domain coverage ≥ 80%

# Application coverage (≥ 70% minimum)
dotnet test tests/SauronSheet.Application.Tests/ --collect:"XPlat Code Coverage"
# Expected: Application handlers ≥ 70% coverage
```

---

#### 7.3: Dependency Rules Verification

**Task**: Audit .csproj files to ensure Clean Architecture maintained

```sh
echo "=== Phase 4 Dependency Verification ==="

# Domain MUST have ZERO project references
DOMAIN_REFS=$(grep -c "ProjectReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj || echo "0")
if [ "$DOMAIN_REFS" -eq "0" ]; then
  echo "✓ PASS - Domain has zero project references"
else
  echo "❌ FAIL - Domain has project references"
fi

# Domain MUST have ZERO NuGet package references
DOMAIN_PKGS=$(grep -c "PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj || echo "0")
if [ "$DOMAIN_PKGS" -eq "0" ]; then
  echo "✓ PASS - Domain has zero NuGet dependencies"
else
  echo "❌ FAIL - Domain has NuGet dependencies"
fi

# Application → Domain only
APP_DOMAIN=$(grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | grep -c "Domain" || echo "0")
if [ "$APP_DOMAIN" -eq "1" ]; then
  echo "✓ PASS - Application references Domain only"
else
  echo "❌ FAIL - Application has incorrect references"
fi

# Infrastructure → Domain only
INFRA_DOMAIN=$(grep "ProjectReference" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj | grep -c "Domain" || echo "0")
if [ "$INFRA_DOMAIN" -eq "1" ]; then
  echo "✓ PASS - Infrastructure references Domain only"
else
  echo "❌ FAIL - Infrastructure has incorrect references"
fi

# Frontend → Application + Infrastructure (2 refs)
FRONTEND_REFS=$(grep "ProjectReference" src/SauronSheet.Frontend/SauronSheet.Frontend.csproj | wc -l)
if [ "$FRONTEND_REFS" -eq "2" ]; then
  echo "✓ PASS - Frontend references 2 projects"
else
  echo "❌ FAIL - Frontend has incorrect reference count: $FRONTEND_REFS (expected 2)"
fi

# No new NuGet packages in Phase 4 (Chart.js via CDN)
echo "=== NuGet Package Verification ==="
echo "Domain NuGets: $DOMAIN_PKGS (expected 0)"
echo "Chart.js: CDN only (no NuGet)"
echo "No new NuGet packages required for Phase 4"
```

---

#### 7.4: Manual E2E MVP Validation

**Test Scenarios:**

```
== MVP Full Workflow Test ==

Test Scenario 1: Register + Login (Phase 1)
- Navigate to /Auth/Register
- Register: test@example.com, password123
- Login → Expected: Redirect to /Dashboard

Test Scenario 2: Dashboard (Empty State)
- On /Dashboard → Expected: "No spending data yet. Import a PDF..."
- All summary cards show €0.00 / 0
- Charts show empty state

Test Scenario 3: PDF Import (Phase 3) 
- Navigate to /Transactions/Upload
- Upload sample PDF
- Expected: Import results show N imported, M skipped
- Return to /Dashboard
- Expected: Summary cards updated, pie chart shows categories

Test Scenario 4: Manual Transaction (Phase 3)
- Navigate to /Transactions/Add
- Create expense: date=today, desc="Coffee", amount=-5.50, category="Other"
- Return to /Dashboard
- Expected: Transaction appears in recent transactions widget

Test Scenario 5: Analytics Dashboard
- /Dashboard with data:
  ✓ Summary cards show income / expenses / net / count
  ✓ Pie chart shows spending by category
  ✓ Line chart shows 12-month trends
  ✓ Bar chart shows current year vs. previous year
  ✓ Recent transactions shows last 10

Test Scenario 6: Date Range Filter
- Change filter to "This Year" → Dashboard updates
- Change filter to "Last Month" → Dashboard updates
- Change filter to "Custom" → Date pickers appear
- Enter custom dates → Apply → Dashboard updates

Test Scenario 7: Transaction Search
- Navigate to /Transactions/Search
- Search keyword: "coffee" → matches shown
- Add category filter → results narrowed
- Add date range → results narrowed further
- Add amount range → results narrowed further
- Clear all filters → all transactions shown
- Verify pagination with many results

Test Scenario 8: Tenant Isolation
- Open incognito window, register different user
- Import PDF for second user
- Verify: /Dashboard for user 1 does NOT show user 2's transactions
- Verify: /Transactions/Search for user 1 does NOT show user 2's data

Test Scenario 9: Responsive Design
- Resize browser to 320px width (mobile)
  ✓ Summary cards: 2x2 grid
  ✓ Charts: stacked vertically, full width
  ✓ Recent transactions: card layout
  ✓ No horizontal scroll
- Resize to 1024px+ (desktop)
  ✓ Summary cards: 4 columns
  ✓ Charts: side-by-side layout

Test Scenario 10: Browser Console
- Open DevTools → Console
- Navigate through all pages
- Expected: ZERO JavaScript errors
- Chart.js loaded and initialized correctly

== MVP COMPLETE ✅ ==
All core features operational:
✓ Register / Login / Logout
✓ PDF import with duplicate detection
✓ Transaction CRUD
✓ Category management with system defaults
✓ Analytics dashboard with 3 chart types
✓ Transaction search with multi-filter
✓ Date range filter (reusable)
✓ Responsive design
✓ Tenant isolation
✓ ~186 automated tests passing
```

---

## Dependency Graph

```
┌─────────────────────────────────────────────────────┐
│              SauronSheet.sln (Phase 4 — MVP) 🏁      │
└─────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼──────────────────┐
        │                 │                  │
    ┌───────────┐    ┌────────────┐   ┌──────────┐
    │   src/    │    │   tests/   │   │ Root Cfg │
    └───────────┘    └────────────┘   └──────────┘
        │                   │              │
   ┌────┴──────┐      ┌─────┴─────┐     │
   │            │      │           │     │
┌──────────┐  ┌──────┐┌──────────┐┌──┐ ┌──┐
│ Domain   │  │ App  ││Infra     ││F ││.c│
│(+Keyword)│  │(+Ana-││(unchanged║│r││o│
│(+Compo-  │  │lytics)││   for   ││n││n│
│ site)    │  │(+Srch)││  Phase4 ││t││f│
└──────────┘  └──────┘└──────────┘└──┘ └──┘
   ↑              ↑        ↑           ↑   ↑
   │              │        │     (Dashboard) global
Domain.Tests   App.Tests  Infra   (Search)   json
(~37 tests)    (~74 tests) (Supabase) (Charts) (SDK)
                                Frontend
                    (Dashboard/Search/DateFilter/Charts.js)
```

**Key Rules (Phase 4 Enforcement)**:
- Domain → ZERO dependencies (2 new spec classes: keyword + composite)
- Application → Domain + MediatR (6 new queries, 4 new DTOs)
- Infrastructure → Domain only (NO changes in Phase 4 — reuses existing repos)
- Frontend → Application + Infrastructure (Dashboard rewrite, Search page, date filter, charts.js)
- Chart.js loaded via CDN (no NuGet package)
- No new NuGet packages in ANY project

---

## Red-Green-Refactor Workflow

### Example: Implementing SearchTransactionsQuery

**Step 1: RED**
- Write test stub for T-4.18: `SearchTransactions_ByKeyword_FiltersCorrectly()`
- Test FAILS (SearchTransactionsQueryHandler doesn't exist)

**Step 2: GREEN**
- Implement `SearchTransactionsQuery` record
- Implement `SearchTransactionsQueryHandler`:
  - Build `TransactionByUserSpecification`
  - Compose with `TransactionByDescriptionKeywordSpecification` using `CompositeSpecification.And()`
  - Query `ITransactionRepository.FindBySpecificationAsync(composedSpec)`
  - Map to `TransactionDto` list, apply pagination
  - Return `PaginatedResultDto<TransactionDto>`
- Test PASSES

**Step 3: REFACTOR**
- Add remaining filter parameters (date range, category, amount range)
- Write tests T-4.19 to T-4.25
- Tests FAIL initially, then implement each filter composition → PASS
- Refactor: extract category lookup to shared helper method
- Ensure consistent `OrderByDescending(t.Date)` across all transaction queries

**Result**: Search handler fully tested, all 8 filter/pagination tests pass

### Example: Implementing CompositeSpecification\<T\>

**Step 1: RED**
- Write test for T-4.30: compose user + category specs → evaluate against matching transaction
- Test FAILS (CompositeSpecification doesn't exist)

**Step 2: GREEN**
- Implement static `And()` method using `Expression.Invoke` + `Expression.AndAlso`
- Compile lambda from composed expression tree
- Test PASSES

**Step 3: REFACTOR**
- Test T-4.31: mismatch → false (already passes with correct implementation)
- Test T-4.32: triple composition → false when partial match (chain two `.And()` calls)
- Verify all 3 tests pass

**Result**: CompositeSpecification proven with 3 tests covering match, mismatch, and multi-composition

---

## Validation Checkpoints

### Checkpoint 4A: Domain Specifications (End of Day 2)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Domain --no-build
Metrics:
  ✓ ~37 domain tests PASS (30 Phase 0+1+2+3 + 7 Phase 4)
  ✓ TransactionByDescriptionKeywordSpecification: 4 tests green
  ✓ CompositeSpecification: 3 tests green
  ✓ Domain.csproj still has ZERO dependencies
```

### Checkpoint 4B: Application DTOs (End of Day 3)
```
Status: ✓ PASS
Verification Command: dotnet build
Metrics:
  ✓ 4 new DTOs compile (CategorySpendingDto, MonthlyTrendDto, YearlyComparisonDto, TransactionSummaryDto)
  ✓ Analytics feature folder created
  ✓ No new NuGet packages
```

### Checkpoint 4C: Analytics Handlers (End of Day 7)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Application --no-build
Metrics:
  ✓ 14 new analytics tests PASS (4 summary + 4 category + 3 trends + 3 comparison)
  ✓ All handlers use CompositeSpecification for filtering
  ✓ GetSpendingByCategory groups into "Other" when > 6 categories
  ✓ GetMonthlyTrends always returns 12 entries
  ✓ GetYearlyComparison handles zero division (null PercentageChange)
```

### Checkpoint 4D: Transaction Queries (End of Day 9)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Application --no-build
Metrics:
  ✓ 11 new transaction query tests PASS (3 recent + 8 search)
  ✓ SearchTransactionsQuery composes up to 5 specifications
  ✓ Pagination: TotalCount, TotalPages, PageSize correct
  ✓ Combined filters: all active filters applied with AND logic
  ✓ Total: ~74 Application tests PASS (49 Phase 1+3 + 25 Phase 4)
```

### Checkpoint 4E: Frontend Dashboard (End of Day 12)
```
Status: ✓ PASS
Verification Command: dotnet run --project src/SauronSheet.Frontend/
Visual Check (in browser):
  ✓ /Dashboard loads with summary cards
  ✓ Pie chart renders category breakdown
  ✓ Line chart renders 12-month trends
  ✓ Bar chart renders yearly comparison
  ✓ Recent transactions table shows last 10
  ✓ Date range filter works (Alpine.js toggle)
  ✓ Chart.js CDN loaded (no console errors)
  ✓ Empty state shown when no data
  ✓ Responsive: cards/charts stack on mobile
```

### Checkpoint 4F: Search Page (End of Day 14)
```
Status: ✓ PASS
Verification Command: dotnet run --project src/SauronSheet.Frontend/
Visual Check:
  ✓ /Transactions/Search loads with filter panel
  ✓ Keyword search works (case-insensitive)
  ✓ Date range filter works
  ✓ Category dropdown populated
  ✓ Amount range filter works
  ✓ Combined filters work
  ✓ Pagination controls functional
  ✓ Filter values preserved in URL
  ✓ "Clear all filters" button works
  ✓ Navigation updated (Dashboard, Search links)
```

### Checkpoint 4G: Integration & MVP (End of Day 16)
```
Status: ✓ PASS
Verification Commands (run in order):
  1. dotnet build                                    # Exit code 0, zero warnings
  2. dotnet test                                     # Output: ~186 tests passed
  3. coverlet (domain + application coverage)        # Domain ≥ 80%, App ≥ 70%
  4. Dependency verification script                  # All assertions PASS
  5. Manual E2E: full MVP workflow                   # All 10 test scenarios pass

Final Metrics:
  ✓ Full build: zero errors, zero warnings (TreatWarningsAsErrors enforced)
  ✓ All ~186 tests: PASS (37 Domain + 74 Application + 75 older phases)
  ✓ Coverage reports generated
  ✓ Dependency rules verified (Domain = 0 deps)
  ✓ No new NuGet packages
  ✓ Dashboard with 3 chart types
  ✓ Multi-filter transaction search
  ✓ Date range filter reusable
  ✓ Responsive design verified
  ✓ Tenant isolation verified
  ✓ No JavaScript console errors
  ✅ MVP COMPLETE — Solution ready for Phase 5/6
```

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| In-memory aggregation slow for >1000 transactions | Medium | Medium | MaxResults 1000 limits data; post-MVP: Supabase RPC for server-side aggregation |
| Chart.js CDN unavailable or slow | Low | Low | Fallback: bundle Chart.js locally; pin version 4.4.0 for stability |
| `Expression.Invoke` in CompositeSpecification | Medium | Medium | Specs evaluated in-memory after repo loads data; documented as known limitation |
| Date range edge cases (timezone, DST) | Medium | Medium | Use `DateTime.UtcNow` consistently; test boundary dates explicitly |
| Dashboard loads slowly (5 sequential queries) | Medium | Medium | Queries are independent — can parallelize with `Task.WhenAll` post-MVP |
| Pie chart unreadable (many small categories) | Low | Medium | Group into "Other" when > 6 categories; tested in T-4.04 |
| Search keyword injection | Low | Low | Specifications use parameterized expressions; no raw SQL |
| Yearly comparison meaningless for new users | Low | High | Show zero bars for year with no data; informational message in empty state |
| Chart.js version drift in CDN | Low | Low | Pinned to 4.4.0 in CDN URL; upgrade explicitly in Phase 6 |
| PaginatedResultDto field naming mismatch | Low | Low | Reuse existing Phase 3 DTO; verified in conversation fix |

---

## Success Criteria Summary

| Criterion | Status | Objective Validation Command |
|-----------|--------|-----------|
| 7 domain tests pass | ✓ | `dotnet test --filter Category=Domain` → ~37 tests (7 new) pass |
| 25 application tests pass | ✓ | `dotnet test --filter Category=Application` → ~74 tests (25 new) pass |
| Total ~186 tests pass | ✓ | `dotnet test` → output shows "~186 passed" |
| Domain coverage ≥ 80% | ✓ | coverlet report shows Domain files ≥ 80% |
| Application coverage ≥ 70% | ✓ | coverlet report shows Application handlers ≥ 70% |
| Dependency rules enforced | ✓ | Verification script shows all assertions PASS |
| Dashboard renders with charts | ✓ | Browser at `/Dashboard`: 3 charts + cards + recent transactions |
| Date range filter works | ✓ | Change filter → data updates without page reload errors |
| Search page with multi-filter | ✓ | `/Transactions/Search` with 5 filter types + pagination |
| Filter values in URL | ✓ | URL contains query parameters; page refresh retains filters |
| Responsive design | ✓ | 320px, 768px, 1024px viewports functional |
| No JS console errors | ✓ | DevTools Console clean on all pages |
| Tenant isolation | ✓ | Two users cannot see each other's analytics |
| No new NuGet packages | ✓ | Chart.js via CDN only |
| **MVP COMPLETE** | ✅ | Full workflow: register → import → manage → analytics |

---

## Next Steps (Post-Phase 4)

Once Phase 4 is complete and all checkpoints PASS:

1. **Merge to main**: Create PR with all Phase 4 deliverables (**MVP milestone complete**)
2. **MVP Announcement**: Application is functionally complete for basic expense tracking
3. **Begin Phase 5**: Budget Management & Alerts (post-MVP enhancement)
4. **Phase 5 prep**: All prior tests passing? ✓ Ready for budget entity + CRUD + alert rules
5. **Performance baseline**: Measure dashboard load time with real data for Phase 6 optimization targets

---

**Created**: 2026-02-19  
**Version**: 1.0.0  
**Duration**: 16 days (Weeks 14–18)  
**Status**: Ready for implementation ✅

````
