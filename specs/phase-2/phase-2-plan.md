# Phase 2 Implementation Plan

**Version**: 1.0.0  
**Created**: 2026-02-15  
**Aligned with**: Constitution v1.1.0, Phase 2 Spec v1.0.0, Full Spec v1.0.0  
**Duration**: Weeks 6–8  
**Goal**: Complete domain model with 81 unit tests, 100% coverage, domain-only scope

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Implementation Phases](#implementation-phases)
- [Task Breakdown by Component](#task-breakdown-by-component)
- [Red-Green-Refactor Workflow](#red-green-refactor-workflow)
- [Validation Checkpoints](#validation-checkpoints)
- [Risk Mitigation](#risk-mitigation)

---

## Executive Summary

Phase 2 is a **Domain-Only phase** that completes the SauronSheet domain model. This phase builds on Phase 0's abstractions and Phase 1's `UserId` value object to deliver a comprehensive, tested, and immutable domain layer.

**Key Deliverables:**
- ✅ 3 aggregate root entities: `Transaction`, `Category`, `Budget`
- ✅ 6 strong-typed value objects: `TransactionId`, `CategoryId`, `BudgetId`, `Money`, `DateRange`, + reuse `UserId`
- ✅ 1 domain service: `CategoryService` with 3 business methods
- ✅ 1 abstract specification base: `BaseSpecification<T>`
- ✅ 4 concrete specifications: `TransactionByDateRange`, `TransactionByCategory`, `TransactionByAmountRange`, `TransactionByUser`
- ✅ 3 repository interfaces (Domain contracts, NO implementation)
- ✅ **81 unit tests** with **100% domain coverage** (domain-only phase rule)
- ✅ Updated `Directory.Build.props` if needed (dependencies unchanged)
- ✅ Constitution compliance verified (zero violations)

**Key Constraint**: This phase is **Domain-Only**. Any Application/Infrastructure/Frontend code is an immediate constitution violation and must be rejected.

**Constitutional Compliance:**
- ✅ Clean Architecture: Domain layer remains zero external dependencies
- ✅ DDD: Strong-typed IDs, value objects with validation, guard methods, system defaults pattern
- ✅ Test-First: 81 tests written before implementation (RED phase), then implementation (GREEN), then refactor
- ✅ Spec-Driven: Single phase spec, layer boundaries enforced (Domain ONLY)
- ✅ Coverage: 100% domain layer (domain-only phase mandate from Constitution)

---

## Implementation Phases

### Phase 2A: Value Objects & Foundation (Days 1-2)
Create strong-typed IDs and foundational value objects with validation.

### Phase 2B: Money & DateRange Value Objects (Days 2-3)
Implement financial and temporal value objects with arithmetic and validation.

### Phase 2C: Transaction Entity & Specifications (Days 3-5)
Build transaction aggregate root and query specifications.

### Phase 2D: Category Entity & Domain Service (Days 5-6)
Implement category management with system defaults and domain service.

### Phase 2E: Budget Entity & Repository Interfaces (Days 6-7)
Complete budget aggregate root and define repository contracts.

### Phase 2F: Integration & Validation (Days 7-8)
E2E testing, coverage reporting, architecture audit.

---

## Task Breakdown by Component

### 0. PRE-IMPLEMENTATION

#### 0.1: Environment Validation

**Task**: Verify Phase 1 completion and Phase 2 readiness

```sh
✓ Phase 0 build passing         # dotnet build
✓ Phase 0 tests passing         # 13 tests green
✓ Phase 1 build passing         # dotnet build
✓ Phase 1 tests passing         # 22 tests green (8 Domain + 14 Application)
✓ Domain project zero deps      # Verify Domain.csproj has NO external packages
✓ UserId VO implemented          # Phase 1 deliverable
✓ Git workspace clean           # Phase 1 merged to main
```

**Acceptance Criteria:**
- Phase 0 + Phase 1 combined tests pass (35 tests)
- Domain layer has ZERO external NuGet dependencies
- All Phase 1 value objects and interfaces are stable and usable
- Git workspace is clean (ready for Phase 2 development)

---

### 1. DOMAIN LAYER EXTENSIONS — VALUE OBJECTS

#### 1.1: Write Strong-Typed ID Tests (RED Phase)

**Task**: Create test stubs for `TransactionId`, `CategoryId`, `BudgetId` value objects (6 tests)

**Directory structure** (create if not exists):
```sh
mkdir -p tests/SauronSheet.Domain.Tests/ValueObjects
```

**File**: `tests/SauronSheet.Domain.Tests/ValueObjects/TransactionIdTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class TransactionIdTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void TransactionId_ValidGuid_SetsValue()
    {
        Assert.True(false, "Implement TransactionId");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void TransactionId_EmptyGuid_ThrowsDomainException()
    {
        Assert.True(false, "Implement TransactionId empty guard");
    }
}
```

**File**: `tests/SauronSheet.Domain.Tests/ValueObjects/CategoryIdTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class CategoryIdTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryId_ValidGuid_SetsValue()
    {
        var guid = Guid.NewGuid();
        var categoryId = new CategoryId(guid);
        Assert.Equal(guid, categoryId.Value);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryId_EmptyGuid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new CategoryId(Guid.Empty));
    }
}
```

**File**: `tests/SauronSheet.Domain.Tests/ValueObjects/BudgetIdTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class BudgetIdTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void BudgetId_ValidGuid_SetsValue()
    {
        var guid = Guid.NewGuid();
        var budgetId = new BudgetId(guid);
        Assert.Equal(guid, budgetId.Value);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void BudgetId_EmptyGuid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new BudgetId(Guid.Empty));
    }
}
```

**Verification**:
```sh
dotnet test --filter Category=Domain --no-build
# Expected: 6 new tests FAIL (red) — IDs not yet implemented
# Expected: 19 Phase 0+1 tests still PASS
```

---

#### 1.2: Implement Strong-Typed IDs (GREEN Phase)

**Task**: Create `TransactionId`, `CategoryId`, `BudgetId` value objects

**File**: `src/SauronSheet.Domain/ValueObjects/TransactionId.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

using Common;
using Exceptions;

public record TransactionId : ValueObject
{
    public Guid Value { get; }

    public TransactionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("TransactionId cannot be empty.");

        Value = value;
    }

    public override string ToString() => Value.ToString();
}
```

**File**: `src/SauronSheet.Domain/ValueObjects/CategoryId.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

using Common;
using Exceptions;

public record CategoryId : ValueObject
{
    public Guid Value { get; }

    public CategoryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("CategoryId cannot be empty.");

        Value = value;
    }

    public override string ToString() => Value.ToString();
}
```

**File**: `src/SauronSheet.Domain/ValueObjects/BudgetId.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

using Common;
using Exceptions;

public record BudgetId : ValueObject
{
    public Guid Value { get; }

    public BudgetId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("BudgetId cannot be empty.");

        Value = value;
    }

    public override string ToString() => Value.ToString();
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 6 ID tests PASS
```

---

#### 1.3: Write Money & DateRange Tests (RED Phase)

**Task**: Create test stubs for `Money` and `DateRange` value objects (18 tests)

**File**: `tests/SauronSheet.Domain.Tests/ValueObjects/MoneyTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void Money_Plus_SameCurrency_AddsAmounts()
    {
        Assert.True(false, "Implement Money.Plus");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_Minus_SameCurrency_SubtractsAmounts()
    {
        Assert.True(false, "Implement Money.Minus");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_Plus_DifferentCurrency_ThrowsDomainException()
    {
        Assert.True(false, "Implement currency validation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_IsPositive_PositiveAmount_ReturnsTrue()
    {
        Assert.True(false, "Implement IsPositive");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_IsNegative_NegativeAmount_ReturnsTrue()
    {
        Assert.True(false, "Implement IsNegative");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_IsZero_ZeroAmount_ReturnsTrue()
    {
        Assert.True(false, "Implement IsZero");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_Equality_SameAmountAndCurrency()
    {
        Assert.True(false, "Implement equality");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_Inequality_DifferentAmount()
    {
        Assert.True(false, "Implement inequality");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_Inequality_DifferentCurrency()
    {
        Assert.True(false, "Implement currency comparison");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_EmptyCurrency_ThrowsDomainException()
    {
        Assert.True(false, "Implement currency guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_DefaultCurrency_IsEUR()
    {
        Assert.True(false, "Implement default currency");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_ToString_FormatsCorrectly()
    {
        Assert.True(false, "Implement ToString");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Money_Minus_DifferentCurrency_ThrowsDomainException()
    {
        Assert.True(false, "Implement Minus currency validation");
    }
}
```

**File**: `tests/SauronSheet.Domain.Tests/ValueObjects/DateRangeTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_ValidConstruction_SetsProperties()
    {
        Assert.True(false, "Implement DateRange");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_EndBeforeStart_ThrowsDomainException()
    {
        Assert.True(false, "Implement end date validation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_Equality_SameValues()
    {
        Assert.True(false, "Implement equality");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_SameStartAndEnd_IsValid()
    {
        Assert.True(false, "Implement same-date validation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_ToString_FormatsCorrectly()
    {
        Assert.True(false, "Implement ToString");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 13 new Money + DateRange tests FAIL (red)
# Expected: 25 prior tests still PASS
```

---

#### 1.4: Implement Money & DateRange (GREEN Phase)

**Task**: Create value objects with arithmetic, validation, and business rules

**File**: `src/SauronSheet.Domain/ValueObjects/Money.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

using Common;
using Exceptions;

public record Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "EUR")
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        Amount = amount;
        Currency = currency;
    }

    public Money Plus(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Minus(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;
    public bool IsZero => Amount == 0;

    public override string ToString() => $"{Amount:F2} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException(
                $"Currency mismatch: cannot operate on {Currency} and {other.Currency}.");
    }
}
```

**File**: `src/SauronSheet.Domain/ValueObjects/DateRange.cs`

```csharp
namespace SauronSheet.Domain.ValueObjects;

using Common;
using Exceptions;

public record DateRange : ValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new DomainException("End date must be greater than or equal to start date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: All 38 Domain tests PASS
# Breakdown: 19 Phase 0+1 base + 6 Strong-Typed IDs + 13 Money+DateRange
```

---

### 2. DOMAIN LAYER EXTENSIONS — ENTITIES

#### 2.1: Write Entity Tests (RED Phase)

**Task**: Create test stubs for `Transaction`, `Category`, `Budget` entities (37 tests)

**Directory structure** (create if not exists):
```sh
mkdir -p tests/SauronSheet.Domain.Tests/Entities
```

**File**: `tests/SauronSheet.Domain.Tests/Entities/TransactionTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.Entities;

public class TransactionTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_ValidConstruction_SetsAllProperties()
    {
        Assert.True(false, "Implement Transaction");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_FutureDate_ThrowsDomainException()
    {
        Assert.True(false, "Implement future date guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_EmptyDescription_ThrowsDomainException()
    {
        Assert.True(false, "Implement description guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_WhitespaceDescription_ThrowsDomainException()
    {
        Assert.True(false, "Implement whitespace description guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_NullUserId_ThrowsDomainException()
    {
        Assert.True(false, "Implement UserId guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_Categorize_UpdatesCategoryId()
    {
        Assert.True(false, "Implement Categorize method");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_Categorize_SetsUpdatedAt()
    {
        Assert.True(false, "Implement UpdatedAt on Categorize");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_UpdateDescription_ChangesDescription()
    {
        Assert.True(false, "Implement UpdateDescription method");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_UpdateDescription_Empty_ThrowsDomainException()
    {
        Assert.True(false, "Implement UpdateDescription guard");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Transaction_WithOptionalFields_SetsCorrectly()
    {
        Assert.True(false, "Implement optional fields");
    }
}
```

**Similar files to create:**
- `CategoryTests.cs` (12 tests: construction, system defaults, guards, rename)
- `BudgetTests.cs` (15 tests: construction, budget status, updates)

**Verification**:
```sh
dotnet test --filter Category=Domain --no-build
# Expected: 37 new entity tests FAIL (red)
# Expected: 38 prior tests still PASS
```

---

#### 2.2: Implement Transaction Entity (GREEN Phase)

**Task**: Create `Transaction` aggregate root with invariants and mutation methods

**Directory structure** (create if not exists):
```sh
mkdir -p src/SauronSheet.Domain/Entities
```

**File**: `src/SauronSheet.Domain/Entities/Transaction.cs`

```csharp
namespace SauronSheet.Domain.Entities;

using Common;
using ValueObjects;
using Exceptions;

public class Transaction : AggregateRoot<TransactionId>
{
    public UserId UserId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public string? ImportedFrom { get; private set; }

    public Transaction(
        TransactionId id,
        UserId userId,
        Money amount,
        DateTime date,
        string description,
        CategoryId? categoryId = null,
        string? importedFrom = null)
        : base(id)
    {
        if (userId == null)
            throw new DomainException("UserId is required.");

        if (date > DateTime.UtcNow)
            throw new DomainException("Transaction date cannot be in the future.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        UserId = userId;
        Amount = amount;
        Date = date;
        Description = description;
        CategoryId = categoryId;
        ImportedFrom = importedFrom;
    }

    public void Categorize(CategoryId categoryId)
    {
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string newDescription)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new DomainException("Description is required.");

        Description = newDescription;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: Transaction tests PASS (10 tests)
# Expected: 48 total tests PASS
```

---

#### 2.3: Implement Category Entity (GREEN Phase)

**Task**: Create `Category` aggregate root with system defaults and guards

**File**: `src/SauronSheet.Domain/Entities/Category.cs`

```csharp
namespace SauronSheet.Domain.Entities;

using Common;
using ValueObjects;
using Exceptions;

public class Category : AggregateRoot<CategoryId>
{
    public UserId UserId { get; private set; }
    public string Name { get; private set; }
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public bool IsSystemDefault { get; private set; }

    // Private constructor for internal use
    private Category(
        CategoryId id,
        UserId userId,
        string name,
        string? color,
        string? icon,
        bool isSystemDefault)
        : base(id)
    {
        if (userId == null)
            throw new DomainException("UserId is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        UserId = userId;
        Name = name;
        Color = color;
        Icon = icon;
        IsSystemDefault = isSystemDefault;
    }

    // Public constructor for user-defined categories
    public Category(
        CategoryId id,
        UserId userId,
        string name,
        string? color = null,
        string? icon = null)
        : this(id, userId, name, color, icon, isSystemDefault: false)
    {
    }

    // Static factory for system defaults
    public static Category CreateSystemDefault(
        CategoryId id,
        UserId userId,
        string name)
    {
        return new Category(id, userId, name, color: null, icon: null, isSystemDefault: true);
    }

    // Guard methods
    public bool CanDelete(bool hasActiveTransactions)
        => !IsSystemDefault && !hasActiveTransactions;

    public bool CanRename()
        => !IsSystemDefault;

    // Mutation methods
    public void Rename(string newName)
    {
        if (IsSystemDefault)
            throw new DomainException("Cannot rename a system default category.");

        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Category name is required.");

        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: Category tests PASS (12 tests)
# Expected: 60 total tests PASS
```

---

#### 2.4: Implement Budget Entity (GREEN Phase)

**Task**: Create `Budget` aggregate root with overage detection and calculations

**File**: `src/SauronSheet.Domain/Entities/Budget.cs`

```csharp
namespace SauronSheet.Domain.Entities;

using Common;
using ValueObjects;
using Exceptions;

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
        if (userId == null)
            throw new DomainException("UserId is required.");

        if (categoryId == null)
            throw new DomainException("CategoryId is required.");

        if (month == null)
            throw new DomainException("Month is required.");

        if (limit.Amount <= 0)
            throw new DomainException("Budget limit must be positive.");

        UserId = userId;
        CategoryId = categoryId;
        Month = month;
        Limit = limit;
    }

    public bool IsOverBudget(Money currentSpend)
        => currentSpend.Amount > Limit.Amount;

    public decimal PercentageUsed(Money currentSpend)
        {
            // Guard is defensive: constructor prevents Limit.Amount <= 0,
            // but we keep this check for robustness against future changes
            return Limit.Amount == 0 ? 0m : currentSpend.Amount / Limit.Amount;
        }

    public Money RemainingAmount(Money currentSpend)
        => Limit.Minus(currentSpend);

    public void UpdateLimit(Money newLimit)
    {
        if (newLimit.Amount <= 0)
            throw new DomainException("Budget limit must be positive.");

        Limit = newLimit;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: Budget tests PASS (15 tests)
# Expected: 75 total tests PASS
```

---

### 3. DOMAIN LAYER EXTENSIONS — SERVICES & SPECIFICATIONS

#### 3.1: Write Domain Service & Specification Tests (RED Phase)

**Task**: Create test stubs for `CategoryService` and specifications (16 tests)

**File**: `tests/SauronSheet.Domain.Tests/Services/CategoryServiceTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Entities;

namespace SauronSheet.Domain.Tests.Services;

public class CategoryServiceTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public async Task CategoryService_ValidateUniqueName_Duplicate_Throws()
    {
        // Arrange
        var mockRepo = new Mock<ICategoryRepository>();
        var userId = new UserId("user-123");
        var existingCategory = new Category(
            new CategoryId(Guid.NewGuid()),
            userId,
            "Groceries");

        mockRepo
            .Setup(r => r.FindByNameAndUserAsync(userId, "Groceries"))
            .ReturnsAsync(existingCategory);

        var service = new CategoryService(mockRepo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => service.ValidateUniqueName(userId, "Groceries"));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task CategoryService_ValidateUniqueName_Unique_NoException()
    {
        Assert.True(false, "Implement unique validation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryService_CanDeleteCategory_SystemDefault_False()
    {
        Assert.True(false, "Implement guard delegation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryService_CanDeleteCategory_Eligible_True()
    {
        Assert.True(false, "Implement eligible check");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryService_GetSystemDefaults_ReturnsFourCategories()
    {
        Assert.True(false, "Implement system defaults factory");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryService_GetSystemDefaults_AllHaveValidIds()
    {
        Assert.True(false, "Implement ID generation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryService_GetSystemDefaults_AllHaveCorrectUserId()
    {
        // Arrange
        var userId = new UserId("user-456");
        var service = new CategoryService(new Mock<ICategoryRepository>().Object);

        // Act
        var defaults = service.GetSystemDefaults(userId);

        // Assert
        Assert.All(defaults, category => Assert.Equal(userId, category.UserId));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategoryService_CanDeleteCategory_ActiveTxns_False()
    {
        Assert.True(false, "Implement active transaction check");
    }
}
```

**File**: `tests/SauronSheet.Domain.Tests/Specifications/SpecificationTests.cs`

```csharp
using Xunit;
using System.Linq.Expressions;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Specifications;

public class SpecificationTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void DateRangeSpec_MatchesTransactionsInRange()
    {
        Assert.True(false, "Implement date range filtering");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRangeSpec_ExcludesTransactionsOutOfRange()
    {
        Assert.True(false, "Implement exclusion");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategorySpec_MatchesTransactionsWithCategory()
    {
        Assert.True(false, "Implement category filtering");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AmountRangeSpec_MatchesTransactionsInRange()
    {
        Assert.True(false, "Implement amount range filtering");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserSpec_MatchesTransactionsForUser()
    {
        Assert.True(false, "Implement user filtering");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AllSpecs_DefaultMaxResults_1000()
    {
        Assert.True(false, "Implement MaxResults");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRangeSpec_IncludesBoundaryDates()
    {
        Assert.True(false, "Implement boundary inclusion");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CategorySpec_ExcludesTransactionsWithDifferentCategory()
    {
        Assert.True(false, "Implement category exclusion");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 16 new service + spec tests FAIL (red)
# Expected: 75 prior tests still PASS
```

---

#### 3.2: Implement CategoryService (GREEN Phase)

**Task**: Create domain service with repository dependency (mocked in tests)

**File**: `src/SauronSheet.Domain/Services/CategoryService.cs`

```csharp
namespace SauronSheet.Domain.Services;

using Entities;
using ValueObjects;
using Repositories;
using Exceptions;

public class CategoryService
{
    private readonly ICategoryRepository _categoryRepo;

    public CategoryService(ICategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
    }

    public async Task ValidateUniqueName(UserId userId, string name)
    {
        var existing = await _categoryRepo.FindByNameAndUserAsync(userId, name);
        if (existing is not null)
            throw new DomainException($"Category '{name}' already exists for this user.");
    }

    public bool CanDeleteCategory(Category category, bool hasActiveTransactions)
        => category.CanDelete(hasActiveTransactions);

    public IReadOnlyList<Category> GetSystemDefaults(UserId userId)
    {
        return new List<Category>
        {
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Groceries"),
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Transport"),
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Utilities"),
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Other"),
        }.AsReadOnly();
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: CategoryService tests PASS (8 tests)
# Expected: 83 total tests PASS
```

---

#### 3.3: Implement BaseSpecification & Concrete Specifications (GREEN Phase)

**Task**: Create abstract base and 4 concrete specification classes

**Directory structure** (create if not exists):
```sh
mkdir -p src/SauronSheet.Domain/Specifications
```

**File**: `src/SauronSheet.Domain/Specifications/BaseSpecification.cs`

```csharp
namespace SauronSheet.Domain.Specifications;

using System.Linq.Expressions;
using Repositories;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; }
    public int MaxResults { get; protected set; } = 1000;
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();

    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
    }
}
```

**File**: `src/SauronSheet.Domain/Specifications/TransactionByDateRangeSpecification.cs`

```csharp
namespace SauronSheet.Domain.Specifications;

using Entities;
using ValueObjects;

public class TransactionByDateRangeSpecification : BaseSpecification<Transaction>
{
    public TransactionByDateRangeSpecification(DateRange range)
        : base(t => t.Date >= range.StartDate && t.Date <= range.EndDate)
    {
    }
}
```

**File**: `src/SauronSheet.Domain/Specifications/TransactionByCategorySpecification.cs`

```csharp
namespace SauronSheet.Domain.Specifications;

using Entities;
using ValueObjects;

public class TransactionByCategorySpecification : BaseSpecification<Transaction>
{
    public TransactionByCategorySpecification(CategoryId categoryId)
        : base(t => t.CategoryId == categoryId)
    {
    }
}
```

**File**: `src/SauronSheet.Domain/Specifications/TransactionByAmountRangeSpecification.cs`

```csharp
namespace SauronSheet.Domain.Specifications;

using Entities;
using ValueObjects;

public class TransactionByAmountRangeSpecification : BaseSpecification<Transaction>
{
    public TransactionByAmountRangeSpecification(Money min, Money max)
        : base(t => t.Amount.Amount >= min.Amount && t.Amount.Amount <= max.Amount)
    {
    }
}
```

**File**: `src/SauronSheet.Domain/Specifications/TransactionByUserSpecification.cs`

```csharp
namespace SauronSheet.Domain.Specifications;

using Entities;
using ValueObjects;

public class TransactionByUserSpecification : BaseSpecification<Transaction>
{
    public TransactionByUserSpecification(UserId userId)
        : base(t => t.UserId == userId)
    {
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: Specification tests PASS (8 tests)
# Expected: 91 total tests PASS (but we have 81 target, so we're ahead)
```

---

### 4. DOMAIN LAYER — REPOSITORY INTERFACES

#### 4.1: Create Repository Interfaces (GREEN Phase)

**Task**: Define repository contracts (no implementation)

**Important Pattern Decision**: 
- All repository methods are `async Task<...>` (future-proof for Supabase)
- `GetBy*Async` methods return `T?` when single entity (nullable for "not found" semantics)
- Collection methods return `IReadOnlyList<T>` (never null; empty list if no results)
- This pattern prevents null-checking burden while maintaining LINQ composability

**Directory structure** (already exists from Phase 0):
```sh
src/SauronSheet.Domain/Repositories/
```

**File**: `src/SauronSheet.Domain/Repositories/ITransactionRepository.cs`

```csharp
namespace SauronSheet.Domain.Repositories;

using Entities;
using ValueObjects;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(TransactionId id);
    Task<IReadOnlyList<Transaction>> GetByUserIdAsync(UserId userId);
    Task<IReadOnlyList<Transaction>> FindBySpecificationAsync(ISpecification<Transaction> specification);
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(TransactionId id);
    Task<bool> ExistsAsync(TransactionId id);
    Task<bool> ExistsDuplicateAsync(UserId userId, DateTime date, decimal amount, string description);
}
```

**File**: `src/SauronSheet.Domain/Repositories/ICategoryRepository.cs`

```csharp
namespace SauronSheet.Domain.Repositories;

using Entities;
using ValueObjects;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id);
    Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId);
    Task<Category?> FindByNameAndUserAsync(UserId userId, string name);
    Task<IReadOnlyList<Category>> GetSystemDefaultsAsync(UserId userId);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(CategoryId id);
    Task<bool> HasTransactionsAsync(CategoryId categoryId);
}
```

**File**: `src/SauronSheet.Domain/Repositories/IBudgetRepository.cs`

```csharp
namespace SauronSheet.Domain.Repositories;

using Entities;
using ValueObjects;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(BudgetId id);
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId);
    Task<Budget?> GetByUserAndCategoryAndMonthAsync(UserId userId, CategoryId categoryId, DateRange month);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(BudgetId id);
}
```

**Verification**:

```sh
dotnet build
# Expected: Build succeeds (no compilation errors)

# Note on Repository Interface Patterns:
# - GetByIdAsync returns T? (nullable) when entity not found
# - GetByUserIdAsync, FindBySpecification, GetSystemDefaults return IReadOnlyList<T>
#   Never null; empty collection when no results
# - ExistsAsync returns bool
# - HasTransactionsAsync returns bool
# This pattern ensures:
#   * Optional single entities use nullable reference types
#   * Collections always valid (may be empty) for LINQ compatibility
#   * Boolean checks for existence queries
```

---

### 5. INTEGRATION & VALIDATION

#### 5.1: Full Build

**Task**: Verify entire solution builds with zero warnings

```sh
dotnet build
# Expected: Build succeeds
# Expected: Zero errors, zero warnings (TreatWarningsAsErrors=true)
```

---

#### 5.2: Run All Tests

**Task**: Execute all 81 domain tests with proper categorization

```sh
dotnet test --filter Category=Domain --no-build
# Phase 2 Target: 81 tests total
# Breakdown:
#   - Phase 0+1 Base: 19 tests (should still pass)
#   - Phase 2 New: 62 tests (10 Transaction + 12 Category + 15 Budget + 8 Service + 8 Spec + 9 IDs+VO)
# Expected: 81 tests total PASS
```

---

#### 5.3: Generate Test Coverage Report

**Task**: Generate code coverage report (Domain ≥ 100% for Phase 2)

```sh
# Install coverlet globally if not already installed
dotnet tool install -g coverlet.console

# Run coverage for Domain layer ONLY
coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Domain.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage.xml" \
  --include "[SauronSheet.Domain]*" \
  --exclude "[SauronSheet.Domain.Tests]*"

# View report
# Expected: Phase 2 Domain files (Entities/, ValueObjects/, Services/, Specifications/) = 100% coverage
# Expected: Phase 0+1 Domain files = still ≥80%
# Expected: Overall Domain = 100% (per constitutional mandate for Phase 2)
```

---

#### 5.4: Verify Dependency Rules

**Task**: Audit .csproj files to ensure Clean Architecture maintained

```sh
# Objective verification: Check Domain has NO dependencies
echo "=== Domain Dependencies ==="
if grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj > /dev/null; then
  echo "❌ FAIL - Domain has dependencies"
  grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj
else
  echo "✓ PASS - Domain has zero dependencies"
fi

# Check Application references ONLY Domain
echo "=== Application Dependencies ==="
APP_REFS=$(grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | grep -c "Domain")
if [ "$APP_REFS" -eq 1 ]; then
  echo "✓ PASS - Application references Domain only"
else
  echo "❌ FAIL - Application has incorrect references"
  grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj
fi

# Verify NO Application/Infrastructure/Frontend changes in Phase 2
echo "=== Phase 2 Scope Verification ==="
if git status --short | grep -E "Application|Infrastructure|Frontend" | grep -v ".md"; then
  echo "❌ FAIL - Phase 2 modified out-of-scope layers"
else
  echo "✓ PASS - Phase 2 scope respected (Domain-Only)"
fi
```

---

## Red-Green-Refactor Workflow

### Example: Implementing Transaction Entity

**Step 1: RED**
- Write test stubs in `TransactionTests.cs` for T-2.01 to T-2.10
- Tests FAIL (Transaction class doesn't exist)

**Step 2: GREEN**
- Create `Transaction.cs` with constructor and basic properties
- Implement invariant guards (null UserId, future date, empty description)
- Tests T-2.01, T-2.02, T-2.03, T-2.05, T-2.06 PASS

**Step 3: REFACTOR**
- Add `Categorize()` and `UpdateDescription()` mutation methods
- Add `UpdatedAt` setting in mutation methods
- Tests T-2.04, T-2.07, T-2.08, T-2.09, T-2.10 PASS
- Verify all 10 tests still passing
- Refactor for clarity: extract guard methods, improve error messages

**Result**: Transaction entity fully tested, all invariants proven by tests, no untested code paths

---

## Validation Checkpoints

### Checkpoint 2A: Value Objects Complete (End of Day 2)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Domain --no-build
Metrics:
  ✓ 38 domain tests PASS (19 Phase 0+1 + 6 Strong-Typed IDs + 13 Money+DateRange)
  ✓ Domain.csproj has ZERO dependencies (audit passed)
  ✓ All value objects immutable (record types)
  ✓ All arithmetic and validation working
```

**Status**: Value objects complete, ready for entities

---

### Checkpoint 2B: Entities Complete (End of Day 5)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Domain --no-build
Metrics:
  ✓ 75 domain tests PASS (38 from Checkpoint 2A + 37 entity tests)
  ✓ Transaction entity: 10 tests passing
  ✓ Category entity: 12 tests passing
  ✓ Budget entity: 15 tests passing
  ✓ All invariants enforced in constructors
  ✓ All mutation methods setting UpdatedAt
  ✓ System defaults immutable and guarded
```

**Status**: All three aggregate roots implemented with full business logic

---

### Checkpoint 2C: Services & Specifications Complete (End of Day 6)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Domain --no-build
Metrics:
  ✓ 91 domain tests PASS (75 from Checkpoint 2B + 8 CategoryService + 8 Specifications)
  ⚠️ Note: Target is 81 Phase 2 Domain tests. 91 total includes Phase 0+1 base tests (19).
     Phase 2-only: 91 - 19 = 72 tests (includes 8 service + 8 spec bonus coverage)
  ✓ CategoryService delegates to mocked ICategoryRepository
  ✓ BaseSpecification abstract base providing boilerplate
  ✓ 4 concrete specifications with working Criteria expressions
  ✓ All specifications default MaxResults = 1000
  ✓ Repository interfaces compile and define contracts
```

**Status**: Domain services and specifications ready for Application layer to consume

---

### Checkpoint 2D: Integration & Validation Complete (End of Day 8)
```
Status: ✓ PASS
Verification Commands (run in order):
  1. dotnet build                    # Exit code 0, zero warnings
  2. dotnet test --filter Category=Domain --no-build  # Output: "81 passed" (or close)
  3. coverlet (see 5.3)              # Domain coverage ≥ 100%
  4. Bash script from 5.4            # All assertions PASS
  5. Git log verify Phase 2 commits  # Only Domain layer changed

Final Metrics:
  ✓ Full build: zero errors, zero warnings (TreatWarningsAsErrors enforced)
  ✓ All 81 domain tests: PASS (11 Phase 0 + 8 Phase 1 + 62 Phase 2)
  ✓ Coverage reports generated (coverage.xml shows 100% Phase 2)
  ✓ Dependency rules verified (Domain=0 refs, no App/Infra/Frontend changes)
  ✓ Constitution compliance verified (Domain-Only phase respected)
  ✓ Solution ready for Phase 3 (Transaction Import Pipeline)
```

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| `DateTime.UtcNow` in constructors causes test flakiness | Medium | Low | Apply ±1 second tolerance in assertions; use DateTime.Now for tests if needed |
| `record` inheritance edge cases with C# | Low | Low | Test equality and inheritance explicitly |
| Large test count (81) makes maintenance costly | Low | Low | Tests are simple Arrange-Act-Assert; keep them maintainable |
| Budget `PercentageUsed` division (limit = 0) | Low | Low | Constructor prevents zero limit; PercentageUsed guards against edge case |
| CategoryService `GetSystemDefaults` generates new Guids each call | Low | Low | Expected behavior for factory method in Phase 2. In Phase 3, defaults are seeded once in database and loaded via repository (deterministic per user). Tests should not depend on specific GUID values. |
| Specification expressions not compatible with Supabase later | Medium | Medium | **Phase 2 Limitation**: Specifications tested in-memory via expression tree compilation. **Phase 3 Plan**: Evaluate if expression trees can be translated directly to Postgrest filters, or create adapter pattern. Document supported expression types. |

---

## Success Criteria Summary

| Criterion | Status | Objective Validation Command |
|-----------|--------|-----------|
| 81 domain tests pass | ✓ | `dotnet test --filter Category=Domain` → output shows "81 passed" |
| Domain coverage = 100% | ✓ | `coverlet` report shows Phase 2 Domain files = 100% |
| No Application/Infrastructure/Frontend changes | ✓ | `git status` shows ONLY changes in `src/Domain/` and `tests/Domain.Tests/` |
| Dependency rules enforced | ✓ | Bash script in 5.4 shows all assertions PASS |
| All value objects immutable | ✓ | Code review: no public setters; all use C# record types |
| All entities use strong-typed IDs | ✓ | Code review: no raw Guid/string for entity IDs |
| CategoryService uses mocked repo interfaces | ✓ | Test inspection: Moq used for `ICategoryRepository` |
| Domain project has ZERO dependencies | ✓ | No `<PackageReference>` in `Domain.csproj` |
| Repository interfaces compile | ✓ | `dotnet build` exit code 0 |
| System defaults are exactly 4 with correct names | ✓ | Test T-2.44 validates count and names |
| Money arithmetic enforces same-currency | ✓ | Tests T-2.25 and T-2.68 validate cross-currency rejection |
| Existing Phase 0 & 1 tests still pass | ✓ | `dotnet test` (all) exit code 0; no regressions |

---

## Next Steps (Post-Phase 2)

Once Phase 2 is complete and all checkpoints PASS:

1. **Merge to main**: Create PR with all Phase 2 deliverables (domain model complete)
2. **Begin Phase 3**: Transition to Transaction Import Pipeline (Application + Infrastructure + Frontend)
3. **Phase 3 prep**: Domain tests still passing? ✓ Ready for Phase 3 command/query handlers

---

**Created**: 2026-02-15  
**Version**: 1.0.0  
**Duration**: 8 days (Weeks 6–8)  
**Status**: Ready for implementation ✅
