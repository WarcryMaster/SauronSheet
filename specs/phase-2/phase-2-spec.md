# Phase 2: Core Data Model & Domain Entities

## Quick Reference

- **Status**: Draft
- **Layer Scope**: Domain ONLY ⚠️
- **Phase Type**: Domain-Only
- **Duration**: Weeks 6–8
- **Goal**: Complete domain model with entities, value objects, services, specifications, and repository interfaces
- **Depends On**: Phase 0 (base abstractions: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `DomainException`, `ISpecification<T>`), Phase 1 (`UserId` value object, `IAuthService` interface)
- **Unlocks**: Phase 3 (Transaction Import Pipeline — Application handlers, Infrastructure repositories, Frontend pages)
- **Coverage Requirement**: 100% domain layer (domain-only phase rule per constitution)

> ⚠️ **DOMAIN-ONLY PHASE**: No Application commands/queries, no Infrastructure implementations, no Frontend pages. Only Domain layer code and Domain.Tests.

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
----------------------------------------------------------------
|
------------------------------------------------------------------------------------
|
------------
|
|
 CD-2.1  
|
 Domain-Only phase: NO Application/Infrastructure/Frontend code 
|
 Constitution: phase scope boundaries enforced; out-of-scope = violation            
|
 2026-02-15 
|
|
 CD-2.2  
|
 All entity IDs are strong-typed value objects                  
|
 Constitution: raw Guid/string for entity IDs is a compliance violation             
|
 2026-02-15 
|
|
 CD-2.3  
|
 Money value object supports single currency (EUR)              
|
 Simplifies MVP; multi-currency deferred to post-MVP backlog                        
|
 2026-02-15 
|
|
 CD-2.4  
|
 4 system default categories (immutable)                        
|
 Groceries, Transport, Utilities, Other — cannot be deleted or renamed              
|
 2026-02-15 
|
|
 CD-2.5  
|
 Budget uniqueness: one per user-category-month                 
|
 Prevents conflicting budget definitions for the same scope                         
|
 2026-02-15 
|
|
 CD-2.6  
|
 Specification pattern for all query filtering                  
|
 Domain language for filtering; MaxResults 1000 default; consistent architecture    
|
 2026-02-15 
|
|
 CD-2.7  
|
`BaseSpecification<T>`
 abstract class introduced               
|
 Concrete base for all specifications; reduces boilerplate in each spec             
|
 2026-02-15 
|
|
 CD-2.8  
|
 Repository interfaces define async methods only                
|
 All persistence is async; sync methods would be a code smell                       
|
 2026-02-15 
|
|
 CD-2.9  
|
 CategoryService depends on repository interfaces (not concrete)
|
 Domain services use abstractions only; mocked in tests                             
|
 2026-02-15 
|
|
 CD-2.10 
|
 Entities enforce invariants in constructors and mutation methods
|
 Invalid state is impossible; DomainException thrown on violation                    
|
 2026-02-15 
|

---

## Executive Summary

### In Scope (Domain Layer ONLY)

|
 Area              
|
 Deliverable                                                                                        
|
|
-------------------
|
----------------------------------------------------------------------------------------------------
|
|
 Entities          
|
`Transaction`
 aggregate root (constructor, 
`Categorize`
, 
`UpdateDescription`
)                      
|
|
 Entities          
|
`Category`
 aggregate root (constructor, 
`CanDelete`
, 
`CanRename`
, 
`Rename`
, 
`CreateSystemDefault`
) 
|
|
 Entities          
|
`Budget`
 aggregate root (constructor, 
`IsOverBudget`
, 
`PercentageUsed`
, 
`RemainingAmount`
, 
`UpdateLimit`
) 
|
|
 Value Objects     
|
`TransactionId(Guid)`
 — empty Guid guard                                                          
|
|
 Value Objects     
|
`CategoryId(Guid)`
 — empty Guid guard                                                             
|
|
 Value Objects     
|
`BudgetId(Guid)`
 — empty Guid guard                                                               
|
|
 Value Objects     
|
`Money(decimal, string)`
 — arithmetic, currency validation, comparison properties                  
|
|
 Value Objects     
|
`DateRange(DateTime, DateTime)`
 — end ≥ start validation                                          
|
|
 Domain Services   
|
`CategoryService`
 — 
`ValidateUniqueName`
, 
`CanDeleteCategory`
, 
`GetSystemDefaults`
|
|
 Specifications    
|
`BaseSpecification<T>`
 abstract class implementing 
`ISpecification<T>`
|
|
 Specifications    
|
`TransactionByDateRangeSpecification`
, 
`TransactionByCategorySpecification`
|
|
 Specifications    
|
`TransactionByAmountRangeSpecification`
, 
`TransactionByUserSpecification`
|
|
 Repository Ifaces 
|
`ITransactionRepository`
, 
`ICategoryRepository`
, 
`IBudgetRepository`
|
|
 Tests             
|
 ≥56 unit tests with 100% domain coverage                                                          
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
--------------------------------
|
--------------
|
--------------------------------------------
|
|
 MediatR commands/queries       
|
 Phase 3+     
|
 Application layer — out of scope           
|
|
 Supabase table migrations      
|
 Phase 3      
|
 Infrastructure layer — out of scope        
|
|
 Repository implementations     
|
 Phase 3      
|
 Infrastructure layer — out of scope        
|
|
 UI for transactions/categories 
|
 Phase 3+     
|
 Frontend layer — out of scope              
|
|
 PDF parsing                    
|
 Phase 3      
|
 Infrastructure layer — out of scope        
|
|
 Analytics aggregations         
|
 Phase 4      
|
 Application layer — out of scope           
|
|
 Budget commands/queries        
|
 Phase 5      
|
 Application layer — out of scope           
|
|
 Duplicate detection logic      
|
 Phase 3      
|
 Application/Infrastructure concern         
|
|
 ImportBatch value object       
|
 Phase 3      
|
 Tied to import pipeline feature            
|
|
 Multi-currency support         
|
 Post-MVP     
|
 Complexity deferred; EUR only for now      
|

---

## User Scenarios & Testing

### Scenario 2.1: Transaction Creation with Validation

**As a** domain model
**I want to** enforce business rules on transaction creation
**So that** invalid transactions can never exist in the system

**Acceptance Criteria:**
- Transaction requires: `TransactionId`, `UserId`, `Money` (amount), `DateTime` (date), `string` (description)
- Date cannot be in the future → throws `DomainException`
- Description cannot be empty or whitespace → throws `DomainException`
- Amount can be positive (income) or negative (expense) — zero is allowed
- Optional: `ImportedFrom` (PDF source reference), `CategoryId`
- `CreatedAt` set automatically on construction (`DateTime.UtcNow`)
- `UpdatedAt` is null on construction, set on mutation methods
- Transaction is immutable after creation except via explicit methods (`Categorize`, `UpdateDescription`)

### Scenario 2.2: Category Management with System Defaults

**As a** domain model
**I want to** support user-defined and system-default categories
**So that** transactions can be categorized with protected defaults

**Acceptance Criteria:**
- Category requires: `CategoryId`, `UserId`, `string` (name)
- Optional: `Color` (hex string), `Icon` (icon identifier string)
- 4 system defaults: **Groceries**, **Transport**, **Utilities**, **Other**
- System defaults created via `Category.CreateSystemDefault()` static factory
- System defaults have `IsSystemDefault = true`
- `CanDelete(bool hasActiveTransactions)` returns `false` for system defaults (regardless of transactions)
- `CanDelete(bool hasActiveTransactions)` returns `false` if category has active transactions (even if user-defined)
- `CanDelete(false)` returns `true` for user-defined categories with no active transactions
- `CanRename()` returns `false` for system defaults
- `CanRename()` returns `true` for user-defined categories
- `Rename(string newName)` throws `DomainException` if `IsSystemDefault`
- `Rename(string newName)` throws `DomainException` if new name is empty
- Category names must be unique per user — validated by `CategoryService` (not entity itself)
- Empty name → throws `DomainException` in constructor

### Scenario 2.3: Budget Tracking with Overage Detection

**As a** domain model
**I want to** track budgets per category per month
**So that** users can detect overspending

**Acceptance Criteria:**
- Budget requires: `BudgetId`, `UserId`, `CategoryId`, `DateRange` (month), `Money` (limit)
- Limit must be positive (> 0) → throws `DomainException` if zero or negative
- `IsOverBudget(Money currentSpend)` returns `true` when spend amount > limit amount
- `IsOverBudget(Money currentSpend)` returns `false` when spend amount ≤ limit amount
- `PercentageUsed(Money currentSpend)` returns decimal (e.g., `0.75` for 75%)
- `PercentageUsed(Money zero)` returns `0.0`
- `RemainingAmount(Money currentSpend)` returns `Money` (limit minus spend)
- `RemainingAmount` can be negative when over budget
- `UpdateLimit(Money newLimit)` changes limit; throws if new limit is not positive
- `UpdateLimit` sets `UpdatedAt` to `DateTime.UtcNow`
- Currency of currentSpend must match limit currency (enforced by `Money` operations)
- One budget per user-category-month — enforced by repository uniqueness check (Phase 3)

### Scenario 2.4: Money Value Object Arithmetic

**As a** domain model
**I want** `Money` to encapsulate currency-safe arithmetic
**So that** financial calculations are always correct

**Acceptance Criteria:**
- `Money(decimal amount, string currency = "EUR")`
- Currency must be non-empty → throws `DomainException` if null/empty/whitespace
- `Plus(Money other)` → adds amounts (same currency only)
- `Minus(Money other)` → subtracts amounts (same currency only)
- Cross-currency `Plus`/`Minus` → throws `DomainException("Currency mismatch: cannot operate on {x} and {y}")`
- `IsPositive` → `true` when amount > 0
- `IsNegative` → `true` when amount < 0
- `IsZero` → `true` when amount == 0
- Value-based equality: same amount AND same currency → equal
- Different amount OR different currency → not equal
- `ToString()` → `"{Amount} {Currency}"` (e.g., `"150.00 EUR"`)

### Scenario 2.5: Specifications Filter Transactions

**As a** domain model
**I want** specifications to express filtering in domain language
**So that** queries are encapsulated without infrastructure knowledge

**Acceptance Criteria:**
- `TransactionByDateRangeSpecification(DateRange range)` → criteria: `t.Date >= range.Start && t.Date <= range.End`
- `TransactionByCategorySpecification(CategoryId categoryId)` → criteria: `t.CategoryId == categoryId`
- `TransactionByAmountRangeSpecification(Money min, Money max)` → criteria: `t.Amount.Amount >= min.Amount && t.Amount.Amount <= max.Amount`
- `TransactionByUserSpecification(UserId userId)` → criteria: `t.UserId == userId`
- All specifications inherit from `BaseSpecification<Transaction>`
- All specifications have `MaxResults = 1000` by default
- `BaseSpecification<T>` implements `ISpecification<T>` with default `Includes` and `IncludeStrings` lists

### Scenario 2.6: Strong-Typed IDs Prevent Misuse

**As a** domain model
**I want** all entity IDs to be strong-typed value objects
**So that** mixing IDs at compile time is impossible

**Acceptance Criteria:**
- `TransactionId(Guid value)` → `Guid.Empty` throws `DomainException`
- `CategoryId(Guid value)` → `Guid.Empty` throws `DomainException`
- `BudgetId(Guid value)` → `Guid.Empty` throws `DomainException`
- Each ID type is distinct: `TransactionId` cannot be assigned to `CategoryId` parameter
- Value-based equality: same Guid value → equal
- `ToString()` returns the Guid string representation

### Scenario 2.7: Repository Interfaces Define Persistence Contracts

**As a** domain model
**I want** repository interfaces to define persistence operations
**So that** infrastructure can implement them without domain knowledge leaking

**Acceptance Criteria:**
- `ITransactionRepository`: Add, GetById, FindBySpecification, GetByUserId, Update, Delete, ExistsDuplicate
- `ICategoryRepository`: Add, GetById, FindByNameAndUser, GetByUserId, GetSystemDefaults, Update, Delete, HasTransactions
- `IBudgetRepository`: Add, GetById, GetByUserAndCategoryAndMonth, GetByUserId, Update, Delete
- All methods are async (`Task<T>` return types)
- No implementation in this phase — interfaces only
- Interfaces live in `Domain/Repositories/`

---

## Functional Requirements

### FR-2.01: Transaction Entity

```csharp
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
    {
        // Invariants:
        // - userId cannot be null
        // - date cannot be in the future (> DateTime.UtcNow)
        // - description cannot be null, empty, or whitespace
        // Sets Id, UserId, Amount, Date, Description, CategoryId, ImportedFrom
        // CreatedAt set by base Entity<TId>
    }

    public void Categorize(CategoryId categoryId)
    {
        // Sets CategoryId to new value
        // Sets UpdatedAt to DateTime.UtcNow
    }

    public void UpdateDescription(string newDescription)
    {
        // Throws DomainException if newDescription is null/empty/whitespace
        // Sets Description to new value
        // Sets UpdatedAt to DateTime.UtcNow
    }
}
Invariant Rules:

Invariant	Guard	Exception Message
UserId required	userId is null	"UserId is required."
Date not in future	date > DateTime.UtcNow	"Transaction date cannot be in the future."
Description required	string.IsNullOrWhiteSpace(description)	"Description is required."
UpdateDescription guard	string.IsNullOrWhiteSpace(newDescription)	"Description is required."
FR-2.02: Category Entity
csharp
public class Category : AggregateRoot<CategoryId>
{
    public UserId UserId { get; private set; }
    public string Name { get; private set; }
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public bool IsSystemDefault { get; private set; }

    // Private constructor for internal/factory use
    private Category(
        CategoryId id,
        UserId userId,
        string name,
        string? color,
        string? icon,
        bool isSystemDefault)
    {
        // Invariants:
        // - userId cannot be null
        // - name cannot be null, empty, or whitespace
        // Sets all properties
    }

    // Public constructor for user-defined categories
    public Category(
        CategoryId id,
        UserId userId,
        string name,
        string? color = null,
        string? icon = null)
        : this(id, userId, name, color, icon, isSystemDefault: false)
    { }

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
System Default Categories:

Name	Purpose
Groceries	Food and household supplies
Transport	Transportation and fuel
Utilities	Bills, electricity, water, internet
Other	Uncategorized expenses
Invariant Rules:

Invariant	Guard	Exception Message
UserId required	userId is null	"UserId is required."
Name required	string.IsNullOrWhiteSpace(name)	"Category name is required."
System default immutable	Rename() on system default	"Cannot rename a system default category."
Rename name required	string.IsNullOrWhiteSpace(newName)	"Category name is required."
FR-2.03: Budget Entity
csharp
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
    {
        // Invariants:
        // - userId cannot be null
        // - categoryId cannot be null
        // - month cannot be null
        // - limit must be positive (limit.Amount > 0)
        // Sets all properties
    }

    public bool IsOverBudget(Money currentSpend)
        => currentSpend.Amount > Limit.Amount;

    public decimal PercentageUsed(Money currentSpend)
        => Limit.Amount == 0 ? 0m : currentSpend.Amount / Limit.Amount;

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
Invariant Rules:

Invariant	Guard	Exception Message
UserId required	userId is null	"UserId is required."
CategoryId required	categoryId is null	"CategoryId is required."
Month required	month is null	"Month is required."
Limit must be positive	limit.Amount <= 0	"Budget limit must be positive."
UpdateLimit must be positive	newLimit.Amount <= 0	"Budget limit must be positive."
FR-2.04: Value Objects
TransactionId
csharp
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
CategoryId
csharp
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
BudgetId
csharp
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
Money
csharp
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
DateRange
csharp
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
Value Objects Summary:

Value Object	Properties	Validation	ToString Format
TransactionId	Guid Value	Guid.Empty → DomainException	Guid string
CategoryId	Guid Value	Guid.Empty → DomainException	Guid string
BudgetId	Guid Value	Guid.Empty → DomainException	Guid string
UserId	string Value	Null/empty/whitespace → DomainException (Phase 1)	Raw string value
Money	decimal Amount, string Currency	Empty currency → DomainException; cross-currency throws	"150.00 EUR"
DateRange	DateTime StartDate, DateTime EndDate	End < Start → DomainException	"2026-01-01 to 2026-01-31"
FR-2.05: CategoryService
csharp
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
Service Methods:

Method	Input	Output	Side Effects / Throws
ValidateUniqueName	UserId userId, string name	Task (void)	Throws DomainException if duplicate found
CanDeleteCategory	Category category, bool hasActiveTxns	bool	None — delegates to Category.CanDelete()
GetSystemDefaults	UserId userId	IReadOnlyList<Category> (4)	None — creates 4 default categories
FR-2.06: BaseSpecification and Concrete Specifications
BaseSpecification<T>
csharp
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
TransactionByDateRangeSpecification
csharp
public class TransactionByDateRangeSpecification : BaseSpecification<Transaction>
{
    public TransactionByDateRangeSpecification(DateRange range)
        : base(t => t.Date >= range.StartDate && t.Date <= range.EndDate)
    { }
}
TransactionByCategorySpecification
csharp
public class TransactionByCategorySpecification : BaseSpecification<Transaction>
{
    public TransactionByCategorySpecification(CategoryId categoryId)
        : base(t => t.CategoryId == categoryId)
    { }
}
TransactionByAmountRangeSpecification
csharp
public class TransactionByAmountRangeSpecification : BaseSpecification<Transaction>
{
    public TransactionByAmountRangeSpecification(Money min, Money max)
        : base(t => t.Amount.Amount >= min.Amount && t.Amount.Amount <= max.Amount)
    { }
}
TransactionByUserSpecification
csharp
public class TransactionByUserSpecification : BaseSpecification<Transaction>
{
    public TransactionByUserSpecification(UserId userId)
        : base(t => t.UserId == userId)
    { }
}
FR-2.07: Repository Interfaces
ITransactionRepository
csharp
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
ICategoryRepository
csharp
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
IBudgetRepository
csharp
public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(BudgetId id);
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId);
    Task<Budget?> GetByUserAndCategoryAndMonthAsync(UserId userId, CategoryId categoryId, DateRange month);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(BudgetId id);
}
Repository Interface Summary:

Interface	Methods	Key Query Methods
ITransactionRepository	8	FindBySpecificationAsync, ExistsDuplicateAsync
ICategoryRepository	8	FindByNameAndUserAsync, HasTransactionsAsync
IBudgetRepository	6	GetByUserAndCategoryAndMonthAsync
Architecture Notes
File Structure (Phase 2 Additions)
text
Domain/
├── Common/                                        # (from Phase 0 — unchanged)
│   ├── Entity.cs
│   ├── AggregateRoot.cs
│   └── ValueObject.cs
├── Entities/                                      # NEW
│   ├── Transaction.cs
│   ├── Category.cs
│   └── Budget.cs
├── ValueObjects/                                  # NEW (UserId from Phase 1)
│   ├── UserId.cs                                  # (from Phase 1)
│   ├── TransactionId.cs
│   ├── CategoryId.cs
│   ├── BudgetId.cs
│   ├── Money.cs
│   └── DateRange.cs
├── Services/                                      # NEW (IAuthService from Phase 1)
│   ├── IAuthService.cs                            # (from Phase 1)
│   ├── AuthResult.cs                              # (from Phase 1)
│   ├── UserProfile.cs                             # (from Phase 1)
│   └── CategoryService.cs
├── Specifications/                                # NEW (ISpecification from Phase 0)
│   ├── ISpecification.cs                          # (from Phase 0)
│   ├── BaseSpecification.cs
│   ├── TransactionByDateRangeSpecification.cs
│   ├── TransactionByCategorySpecification.cs
│   ├── TransactionByAmountRangeSpecification.cs
│   └── TransactionByUserSpecification.cs
├── Repositories/                                  # NEW
│   ├── ITransactionRepository.cs
│   ├── ICategoryRepository.cs
│   └── IBudgetRepository.cs
└── Exceptions/                                    # (from Phase 0 — unchanged)
    ├── DomainException.cs
    └── EntityNotFoundException.cs
Test Structure (Phase 2 Additions)
text
tests/
└── SauronSheet.Domain.Tests/
    ├── Common/                                    # (from Phase 0 — unchanged)
    │   ├── EntityBaseTests.cs
    │   └── ValueObjectBaseTests.cs
    ├── Entities/                                  # NEW
    │   ├── TransactionTests.cs
    │   ├── CategoryTests.cs
    │   └── BudgetTests.cs
    ├── ValueObjects/                              # NEW (UserIdTests from Phase 1)
    │   ├── UserIdTests.cs                         # (from Phase 1)
    │   ├── TransactionIdTests.cs
    │   ├── CategoryIdTests.cs
    │   ├── BudgetIdTests.cs
    │   ├── MoneyTests.cs
    │   └── DateRangeTests.cs
    ├── Services/                                  # NEW
    │   └── CategoryServiceTests.cs
    ├── Specifications/                            # NEW (SpecificationBaseTests from Phase 0)
    │   ├── SpecificationBaseTests.cs              # (from Phase 0)
    │   ├── TransactionByDateRangeSpecTests.cs
    │   ├── TransactionByCategorySpecTests.cs
    │   ├── TransactionByAmountRangeSpecTests.cs
    │   └── TransactionByUserSpecTests.cs
    └── Exceptions/                                # (from Phase 0 — unchanged)
        ├── DomainExceptionTests.cs
        └── EntityNotFoundExceptionTests.cs
NuGet Packages (Phase 2)
Project	Packages	Notes
SauronSheet.Domain	None (zero dependencies)	Constitution mandate maintained
SauronSheet.Domain.Tests	xUnit, xUnit.runner.visualstudio, Moq, coverlet.collector	Moq used for CategoryService tests
Domain Layer Dependency Verification
text
Domain project: ZERO <ProjectReference> entries
Domain project: ZERO <PackageReference> entries
Domain.Tests project: references ONLY Domain
No changes to Application, Infrastructure, or Frontend projects in this phase.

Test Specifications
Transaction Entity Tests
text
TEST T-2.01: Transaction_ValidConstruction_SetsAllProperties
  GIVEN valid TransactionId, UserId, Money(100, "EUR"), date = yesterday, description = "Groceries"
  WHEN Transaction is constructed
  THEN Id equals provided TransactionId
  AND UserId equals provided UserId
  AND Amount equals Money(100, "EUR")
  AND Date equals yesterday
  AND Description equals "Groceries"
  AND CategoryId is null
  AND ImportedFrom is null
  AND CreatedAt ≈ DateTime.UtcNow (±1s)
  AND UpdatedAt is null

TEST T-2.02: Transaction_FutureDate_ThrowsDomainException
  GIVEN valid TransactionId, UserId, Money(50, "EUR"), date = tomorrow
  WHEN Transaction is constructed
  THEN throws DomainException with message containing "cannot be in the future"

TEST T-2.03: Transaction_EmptyDescription_ThrowsDomainException
  GIVEN valid TransactionId, UserId, Money(50, "EUR"), date = yesterday, description = ""
  WHEN Transaction is constructed
  THEN throws DomainException with message containing "Description is required"

TEST T-2.04: Transaction_Categorize_UpdatesCategoryId
  GIVEN a valid Transaction with CategoryId = null
  AND a valid CategoryId
  WHEN Categorize(categoryId) is called
  THEN CategoryId equals the provided CategoryId

TEST T-2.05: Transaction_UpdateDescription_ChangesDescription
  GIVEN a valid Transaction with description "Old"
  WHEN UpdateDescription("New description") is called
  THEN Description equals "New description"
  AND UpdatedAt is not null
  AND UpdatedAt ≈ DateTime.UtcNow (±1s)

TEST T-2.06: Transaction_NullUserId_ThrowsDomainException
  GIVEN null UserId
  WHEN Transaction is constructed
  THEN throws DomainException with message containing "UserId is required"

TEST T-2.51: Transaction_UpdateDescription_Empty_ThrowsDomainException
  GIVEN a valid Transaction
  WHEN UpdateDescription("") is called
  THEN throws DomainException with message containing "Description is required"

TEST T-2.52: Transaction_Categorize_SetsUpdatedAt
  GIVEN a valid Transaction with UpdatedAt = null
  AND a valid CategoryId
  WHEN Categorize(categoryId) is called
  THEN UpdatedAt is not null
  AND UpdatedAt ≈ DateTime.UtcNow (±1s)

TEST T-2.57: Transaction_WhitespaceDescription_ThrowsDomainException
  GIVEN valid TransactionId, UserId, Money(50, "EUR"), date = yesterday, description = "   "
  WHEN Transaction is constructed
  THEN throws DomainException with message containing "Description is required"

TEST T-2.58: Transaction_WithOptionalFields_SetsCorrectly
  GIVEN valid TransactionId, UserId, Money(100, "EUR"), date = yesterday, description = "Test"
  AND categoryId = valid CategoryId, importedFrom = "bank-statement.pdf"
  WHEN Transaction is constructed
  THEN CategoryId equals provided CategoryId
  AND ImportedFrom equals "bank-statement.pdf"
Category Entity Tests
text
TEST T-2.07: Category_ValidConstruction_SetsProperties
  GIVEN valid CategoryId, UserId, name = "Food"
  WHEN Category is constructed (public constructor)
  THEN Name equals "Food"
  AND IsSystemDefault is false
  AND Color is null
  AND Icon is null
  AND CreatedAt ≈ DateTime.UtcNow (±1s)

TEST T-2.08: Category_SystemDefault_CanDeleteReturnsFalse
  GIVEN a system default Category (IsSystemDefault = true)
  WHEN CanDelete(false) is called
  THEN returns false

TEST T-2.09: Category_WithActiveTransactions_CanDeleteReturnsFalse
  GIVEN a user-defined Category (IsSystemDefault = false)
  WHEN CanDelete(true) is called (hasActiveTransactions = true)
  THEN returns false

TEST T-2.10: Category_UserDefined_NoTransactions_CanDeleteReturnsTrue
  GIVEN a user-defined Category (IsSystemDefault = false)
  WHEN CanDelete(false) is called (hasActiveTransactions = false)
  THEN returns true

TEST T-2.11: Category_SystemDefault_CanRenameReturnsFalse
  GIVEN a system default Category (IsSystemDefault = true)
  WHEN CanRename() is called
  THEN returns false

TEST T-2.12: Category_UserDefined_RenameChangesName
  GIVEN a user-defined Category with name "Old Name"
  WHEN Rename("New Name") is called
  THEN Name equals "New Name"
  AND UpdatedAt is not null
  AND UpdatedAt ≈ DateTime.UtcNow (±1s)

TEST T-2.13: Category_EmptyName_ThrowsDomainException
  GIVEN valid CategoryId, UserId, name = ""
  WHEN Category is constructed
  THEN throws DomainException with message containing "name is required"

TEST T-2.14: Category_CreateSystemDefault_SetsFlag
  GIVEN valid CategoryId, UserId, name = "Groceries"
  WHEN Category.CreateSystemDefault() is called
  THEN IsSystemDefault is true
  AND Name equals "Groceries"

TEST T-2.53: Category_CanRename_UserDefined_ReturnsTrue
  GIVEN a user-defined Category (IsSystemDefault = false)
  WHEN CanRename() is called
  THEN returns true

TEST T-2.59: Category_Rename_SystemDefault_ThrowsDomainException
  GIVEN a system default Category (IsSystemDefault = true)
  WHEN Rename("New Name") is called
  THEN throws DomainException with message containing "Cannot rename a system default"

TEST T-2.60: Category_Rename_EmptyName_ThrowsDomainException
  GIVEN a user-defined Category
  WHEN Rename("") is called
  THEN throws DomainException with message containing "name is required"

TEST T-2.61: Category_WithColorAndIcon_SetsOptionalProperties
  GIVEN valid CategoryId, UserId, name = "Food", color = "#FF5733", icon = "shopping-cart"
  WHEN Category is constructed
  THEN Color equals "#FF5733"
  AND Icon equals "shopping-cart"
Budget Entity Tests
text
TEST T-2.15: Budget_ValidConstruction_SetsProperties
  GIVEN valid BudgetId, UserId, CategoryId, DateRange(Jan 1 - Jan 31), Money(500, "EUR")
  WHEN Budget is constructed
  THEN all properties match constructor arguments
  AND CreatedAt ≈ DateTime.UtcNow (±1s)
  AND UpdatedAt is null

TEST T-2.16: Budget_IsOverBudget_SpendExceedsLimit_ReturnsTrue
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN IsOverBudget(Money(150, "EUR")) is called
  THEN returns true

TEST T-2.17: Budget_IsOverBudget_SpendUnderLimit_ReturnsFalse
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN IsOverBudget(Money(50, "EUR")) is called
  THEN returns false

TEST T-2.18: Budget_PercentageUsed_CalculatesCorrectly
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN PercentageUsed(Money(75, "EUR")) is called
  THEN returns 0.75m

TEST T-2.19: Budget_RemainingAmount_CalculatesCorrectly
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN RemainingAmount(Money(75, "EUR")) is called
  THEN returns Money(25, "EUR")

TEST T-2.20: Budget_RemainingAmount_Negative_WhenOverBudget
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN RemainingAmount(Money(150, "EUR")) is called
  THEN returns Money(-50, "EUR")

TEST T-2.21: Budget_UpdateLimit_ChangesLimit
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN UpdateLimit(Money(200, "EUR")) is called
  THEN Limit equals Money(200, "EUR")
  AND UpdatedAt is not null
  AND UpdatedAt ≈ DateTime.UtcNow (±1s)

TEST T-2.22: Budget_ZeroLimit_ThrowsDomainException
  GIVEN valid BudgetId, UserId, CategoryId, DateRange, limit = Money(0, "EUR")
  WHEN Budget is constructed
  THEN throws DomainException with message containing "must be positive"

TEST T-2.56: Budget_PercentageUsed_ZeroSpend_ReturnsZero
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN PercentageUsed(Money(0, "EUR")) is called
  THEN returns 0.0m

TEST T-2.62: Budget_NegativeLimit_ThrowsDomainException
  GIVEN valid BudgetId, UserId, CategoryId, DateRange, limit = Money(-50, "EUR")
  WHEN Budget is constructed
  THEN throws DomainException with message containing "must be positive"

TEST T-2.63: Budget_UpdateLimit_ZeroAmount_ThrowsDomainException
  GIVEN a valid Budget
  WHEN UpdateLimit(Money(0, "EUR")) is called
  THEN throws DomainException with message containing "must be positive"

TEST T-2.64: Budget_IsOverBudget_ExactLimit_ReturnsFalse
  GIVEN Budget with Limit = Money(100, "EUR")
  WHEN IsOverBudget(Money(100, "EUR")) is called
  THEN returns false (spend == limit is NOT over budget)

TEST T-2.65: Budget_NullUserId_ThrowsDomainException
  GIVEN null UserId
  WHEN Budget is constructed
  THEN throws DomainException with message containing "UserId is required"

TEST T-2.66: Budget_NullCategoryId_ThrowsDomainException
  GIVEN null CategoryId
  WHEN Budget is constructed
  THEN throws DomainException with message containing "CategoryId is required"

TEST T-2.67: Budget_NullMonth_ThrowsDomainException
  GIVEN null DateRange month
  WHEN Budget is constructed
  THEN throws DomainException with message containing "Month is required"
Money Value Object Tests
text
TEST T-2.23: Money_Plus_SameCurrency_AddsAmounts
  GIVEN Money(100, "EUR") and Money(50, "EUR")
  WHEN Plus is called
  THEN returns Money(150, "EUR")

TEST T-2.24: Money_Minus_SameCurrency_SubtractsAmounts
  GIVEN Money(100, "EUR") and Money(30, "EUR")
  WHEN Minus is called
  THEN returns Money(70, "EUR")

TEST T-2.25: Money_Plus_DifferentCurrency_ThrowsDomainException
  GIVEN Money(100, "EUR") and Money(50, "USD")
  WHEN Plus is called
  THEN throws DomainException with message containing "Currency mismatch"

TEST T-2.26: Money_IsPositive_PositiveAmount_ReturnsTrue
  GIVEN Money(50, "EUR")
  WHEN IsPositive is read
  THEN returns true

TEST T-2.27: Money_IsNegative_NegativeAmount_ReturnsTrue
  GIVEN Money(-50, "EUR")
  WHEN IsNegative is read
  THEN returns true

TEST T-2.28: Money_IsZero_ZeroAmount_ReturnsTrue
  GIVEN Money(0, "EUR")
  WHEN IsZero is read
  THEN returns true

TEST T-2.29: Money_Equality_SameAmountAndCurrency
  GIVEN Money(100, "EUR") and Money(100, "EUR")
  WHEN compared for equality
  THEN they are equal

TEST T-2.30: Money_Inequality_DifferentAmount
  GIVEN Money(100, "EUR") and Money(200, "EUR")
  WHEN compared for equality
  THEN they are NOT equal

TEST T-2.31: Money_Inequality_DifferentCurrency
  GIVEN Money(100, "EUR") and Money(100, "USD")
  WHEN compared for equality
  THEN they are NOT equal

TEST T-2.54: Money_EmptyCurrency_ThrowsDomainException
  GIVEN amount = 100, currency = ""
  WHEN Money is constructed
  THEN throws DomainException with message containing "Currency is required"

TEST T-2.68: Money_Minus_DifferentCurrency_ThrowsDomainException
  GIVEN Money(100, "EUR") and Money(30, "USD")
  WHEN Minus is called
  THEN throws DomainException with message containing "Currency mismatch"

TEST T-2.69: Money_DefaultCurrency_IsEUR
  GIVEN amount = 100, no currency specified
  WHEN Money is constructed
  THEN Currency equals "EUR"

TEST T-2.70: Money_ToString_FormatsCorrectly
  GIVEN Money(150.5, "EUR")
  WHEN ToString() is called
  THEN returns "150.50 EUR"
DateRange Value Object Tests
text
TEST T-2.32: DateRange_ValidConstruction_SetsProperties
  GIVEN startDate = Jan 1 2026, endDate = Jan 31 2026
  WHEN DateRange is constructed
  THEN StartDate equals Jan 1 2026
  AND EndDate equals Jan 31 2026

TEST T-2.33: DateRange_EndBeforeStart_ThrowsDomainException
  GIVEN startDate = Jan 31 2026, endDate = Jan 1 2026
  WHEN DateRange is constructed
  THEN throws DomainException with message containing "End date must be greater than or equal to start date"

TEST T-2.34: DateRange_Equality_SameValues
  GIVEN two DateRange instances with same start and end
  WHEN compared for equality
  THEN they are equal

TEST T-2.55: DateRange_SameStartAndEnd_IsValid
  GIVEN startDate = Jan 15 2026, endDate = Jan 15 2026
  WHEN DateRange is constructed
  THEN no exception is thrown
  AND StartDate equals EndDate

TEST T-2.71: DateRange_ToString_FormatsCorrectly
  GIVEN DateRange(Jan 1 2026, Jan 31 2026)
  WHEN ToString() is called
  THEN returns "2026-01-01 to 2026-01-31"
Strong-Typed ID Tests
text
TEST T-2.35: TransactionId_EmptyGuid_ThrowsDomainException
  GIVEN Guid.Empty
  WHEN TransactionId is constructed
  THEN throws DomainException with message containing "cannot be empty"

TEST T-2.36: TransactionId_ValidGuid_SetsValue
  GIVEN a valid non-empty Guid
  WHEN TransactionId is constructed
  THEN Value equals the provided Guid
  AND ToString() returns the Guid string

TEST T-2.37: CategoryId_EmptyGuid_ThrowsDomainException
  GIVEN Guid.Empty
  WHEN CategoryId is constructed
  THEN throws DomainException with message containing "cannot be empty"

TEST T-2.38: BudgetId_EmptyGuid_ThrowsDomainException
  GIVEN Guid.Empty
  WHEN BudgetId is constructed
  THEN throws DomainException with message containing "cannot be empty"

TEST T-2.72: CategoryId_ValidGuid_SetsValue
  GIVEN a valid non-empty Guid
  WHEN CategoryId is constructed
  THEN Value equals the provided Guid
  AND ToString() returns the Guid string

TEST T-2.73: BudgetId_ValidGuid_SetsValue
  GIVEN a valid non-empty Guid
  WHEN BudgetId is constructed
  THEN Value equals the provided Guid
  AND ToString() returns the Guid string

TEST T-2.74: TransactionId_Equality_SameGuid
  GIVEN two TransactionId instances with the same Guid
  WHEN compared for equality
  THEN they are equal

TEST T-2.75: TransactionId_Inequality_DifferentGuid
  GIVEN two TransactionId instances with different Guids
  WHEN compared for equality
  THEN they are NOT equal
CategoryService Tests
text
TEST T-2.39: CategoryService_ValidateUniqueName_Duplicate_Throws
  GIVEN ICategoryRepository.FindByNameAndUserAsync returns an existing Category
  WHEN ValidateUniqueName(userId, "Groceries") is called
  THEN throws DomainException with message containing "already exists"

TEST T-2.40: CategoryService_ValidateUniqueName_Unique_NoException
  GIVEN ICategoryRepository.FindByNameAndUserAsync returns null
  WHEN ValidateUniqueName(userId, "NewCategory") is called
  THEN no exception is thrown

TEST T-2.41: CategoryService_CanDeleteCategory_SystemDefault_False
  GIVEN a system default Category
  WHEN CanDeleteCategory(category, false) is called
  THEN returns false

TEST T-2.42: CategoryService_CanDeleteCategory_ActiveTxns_False
  GIVEN a user-defined Category
  WHEN CanDeleteCategory(category, true) is called
  THEN returns false

TEST T-2.43: CategoryService_CanDeleteCategory_Eligible_True
  GIVEN a user-defined Category with no active transactions
  WHEN CanDeleteCategory(category, false) is called
  THEN returns true

TEST T-2.44: CategoryService_GetSystemDefaults_ReturnsFourCategories
  GIVEN a valid UserId
  WHEN GetSystemDefaults(userId) is called
  THEN returns IReadOnlyList with Count == 4
  AND all have IsSystemDefault == true
  AND names are "Groceries", "Transport", "Utilities", "Other"

TEST T-2.76: CategoryService_GetSystemDefaults_AllHaveValidIds
  GIVEN a valid UserId
  WHEN GetSystemDefaults(userId) is called
  THEN all returned categories have non-empty CategoryId values

TEST T-2.77: CategoryService_GetSystemDefaults_AllHaveCorrectUserId
  GIVEN a valid UserId("user-123")
  WHEN GetSystemDefaults(userId) is called
  THEN all returned categories have UserId equal to the provided userId
Specification Tests
text
TEST T-2.45: DateRangeSpec_MatchesTransactionsInRange
  GIVEN a Transaction with date = Jan 15 2026
  AND TransactionByDateRangeSpecification(Jan 1 - Jan 31 2026)
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns true

TEST T-2.46: DateRangeSpec_ExcludesTransactionsOutOfRange
  GIVEN a Transaction with date = Feb 15 2026
  AND TransactionByDateRangeSpecification(Jan 1 - Jan 31 2026)
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns false

TEST T-2.47: CategorySpec_MatchesTransactionsWithCategory
  GIVEN a Transaction with CategoryId = X
  AND TransactionByCategorySpecification(X)
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns true

TEST T-2.48: AmountRangeSpec_MatchesTransactionsInRange
  GIVEN a Transaction with Amount = Money(75, "EUR")
  AND TransactionByAmountRangeSpecification(Money(50, "EUR"), Money(100, "EUR"))
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns true

TEST T-2.49: UserSpec_MatchesTransactionsForUser
  GIVEN a Transaction with UserId = "user-123"
  AND TransactionByUserSpecification(UserId("user-123"))
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns true

TEST T-2.50: AllSpecs_DefaultMaxResults_1000
  GIVEN instances of all four specification types
  WHEN MaxResults is read from each
  THEN all equal 1000

TEST T-2.78: DateRangeSpec_IncludesBoundaryDates
  GIVEN a Transaction with date = Jan 1 2026 (start boundary)
  AND TransactionByDateRangeSpecification(Jan 1 - Jan 31 2026)
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns true

TEST T-2.79: CategorySpec_ExcludesTransactionsWithDifferentCategory
  GIVEN a Transaction with CategoryId = X
  AND TransactionByCategorySpecification(Y) where Y ≠ X
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns false

TEST T-2.80: AmountRangeSpec_ExcludesTransactionsOutOfRange
  GIVEN a Transaction with Amount = Money(200, "EUR")
  AND TransactionByAmountRangeSpecification(Money(50, "EUR"), Money(100, "EUR"))
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns false

TEST T-2.81: UserSpec_ExcludesTransactionsForDifferentUser
  GIVEN a Transaction with UserId = "user-123"
  AND TransactionByUserSpecification(UserId("user-456"))
  WHEN Criteria is compiled and evaluated against the transaction
  THEN returns false
Test Summary
Test ID	Test Name	Category	Area
T-2.01	Transaction_ValidConstruction_SetsAllProperties	Domain	Transaction
T-2.02	Transaction_FutureDate_ThrowsDomainException	Domain	Transaction
T-2.03	Transaction_EmptyDescription_ThrowsDomainException	Domain	Transaction
T-2.04	Transaction_Categorize_UpdatesCategoryId	Domain	Transaction
T-2.05	Transaction_UpdateDescription_ChangesDescription	Domain	Transaction
T-2.06	Transaction_NullUserId_ThrowsDomainException	Domain	Transaction
T-2.07	Category_ValidConstruction_SetsProperties	Domain	Category
T-2.08	Category_SystemDefault_CanDeleteReturnsFalse	Domain	Category
T-2.09	Category_WithActiveTransactions_CanDeleteReturnsFalse	Domain	Category
T-2.10	Category_UserDefined_NoTransactions_CanDeleteReturnsTrue	Domain	Category
T-2.11	Category_SystemDefault_CanRenameReturnsFalse	Domain	Category
T-2.12	Category_UserDefined_RenameChangesName	Domain	Category
T-2.13	Category_EmptyName_ThrowsDomainException	Domain	Category
T-2.14	Category_CreateSystemDefault_SetsFlag	Domain	Category
T-2.15	Budget_ValidConstruction_SetsProperties	Domain	Budget
T-2.16	Budget_IsOverBudget_SpendExceedsLimit_ReturnsTrue	Domain	Budget
T-2.17	Budget_IsOverBudget_SpendUnderLimit_ReturnsFalse	Domain	Budget
T-2.18	Budget_PercentageUsed_CalculatesCorrectly	Domain	Budget
T-2.19	Budget_RemainingAmount_CalculatesCorrectly	Domain	Budget
T-2.20	Budget_RemainingAmount_Negative_WhenOverBudget	Domain	Budget
T-2.21	Budget_UpdateLimit_ChangesLimit	Domain	Budget
T-2.22	Budget_ZeroLimit_ThrowsDomainException	Domain	Budget
T-2.23	Money_Plus_SameCurrency_AddsAmounts	Domain	Money
T-2.24	Money_Minus_SameCurrency_SubtractsAmounts	Domain	Money
T-2.25	Money_Plus_DifferentCurrency_ThrowsDomainException	Domain	Money
T-2.26	Money_IsPositive_PositiveAmount_ReturnsTrue	Domain	Money
T-2.27	Money_IsNegative_NegativeAmount_ReturnsTrue	Domain	Money
T-2.28	Money_IsZero_ZeroAmount_ReturnsTrue	Domain	Money
T-2.29	Money_Equality_SameAmountAndCurrency	Domain	Money
T-2.30	Money_Inequality_DifferentAmount	Domain	Money
T-2.31	Money_Inequality_DifferentCurrency	Domain	Money
T-2.32	DateRange_ValidConstruction_SetsProperties	Domain	DateRange
T-2.33	DateRange_EndBeforeStart_ThrowsDomainException	Domain	DateRange
T-2.34	DateRange_Equality_SameValues	Domain	DateRange
T-2.35	TransactionId_EmptyGuid_ThrowsDomainException	Domain	Strong-Typed ID
T-2.36	TransactionId_ValidGuid_SetsValue	Domain	Strong-Typed ID
T-2.37	CategoryId_EmptyGuid_ThrowsDomainException	Domain	Strong-Typed ID
T-2.38	BudgetId_EmptyGuid_ThrowsDomainException	Domain	Strong-Typed ID
T-2.39	CategoryService_ValidateUniqueName_Duplicate_Throws	Domain	CategoryService
T-2.40	CategoryService_ValidateUniqueName_Unique_NoException	Domain	CategoryService
T-2.41	CategoryService_CanDeleteCategory_SystemDefault_False	Domain	CategoryService
T-2.42	CategoryService_CanDeleteCategory_ActiveTxns_False	Domain	CategoryService
T-2.43	CategoryService_CanDeleteCategory_Eligible_True	Domain	CategoryService
T-2.44	CategoryService_GetSystemDefaults_ReturnsFourCategories	Domain	CategoryService
T-2.45	DateRangeSpec_MatchesTransactionsInRange	Domain	Specification
T-2.46	DateRangeSpec_ExcludesTransactionsOutOfRange	Domain	Specification
T-2.47	CategorySpec_MatchesTransactionsWithCategory	Domain	Specification
T-2.48	AmountRangeSpec_MatchesTransactionsInRange	Domain	Specification
T-2.49	UserSpec_MatchesTransactionsForUser	Domain	Specification
T-2.50	AllSpecs_DefaultMaxResults_1000	Domain	Specification
T-2.51	Transaction_UpdateDescription_Empty_ThrowsDomainException	Domain	Transaction
T-2.52	Transaction_Categorize_SetsUpdatedAt	Domain	Transaction
T-2.53	Category_CanRename_UserDefined_ReturnsTrue	Domain	Category
T-2.54	Money_EmptyCurrency_ThrowsDomainException	Domain	Money
T-2.55	DateRange_SameStartAndEnd_IsValid	Domain	DateRange
T-2.56	Budget_PercentageUsed_ZeroSpend_ReturnsZero	Domain	Budget
T-2.57	Transaction_WhitespaceDescription_ThrowsDomainException	Domain	Transaction
T-2.58	Transaction_WithOptionalFields_SetsCorrectly	Domain	Transaction
T-2.59	Category_Rename_SystemDefault_ThrowsDomainException	Domain	Category
T-2.60	Category_Rename_EmptyName_ThrowsDomainException	Domain	Category
T-2.61	Category_WithColorAndIcon_SetsOptionalProperties	Domain	Category
T-2.62	Budget_NegativeLimit_ThrowsDomainException	Domain	Budget
T-2.63	Budget_UpdateLimit_ZeroAmount_ThrowsDomainException	Domain	Budget
T-2.64	Budget_IsOverBudget_ExactLimit_ReturnsFalse	Domain	Budget
T-2.65	Budget_NullUserId_ThrowsDomainException	Domain	Budget
T-2.66	Budget_NullCategoryId_ThrowsDomainException	Domain	Budget
T-2.67	Budget_NullMonth_ThrowsDomainException	Domain	Budget
T-2.68	Money_Minus_DifferentCurrency_ThrowsDomainException	Domain	Money
T-2.69	Money_DefaultCurrency_IsEUR	Domain	Money
T-2.70	Money_ToString_FormatsCorrectly	Domain	Money
| T-2.71  | DateRange_ToString_FormatsCorrectly                           | Domain   | DateRange       |
| T-2.72  | CategoryId_ValidGuid_SetsValue                                | Domain   | Strong-Typed ID |
| T-2.73  | BudgetId_ValidGuid_SetsValue                                  | Domain   | Strong-Typed ID |
| T-2.74  | TransactionId_Equality_SameGuid                               | Domain   | Strong-Typed ID |
| T-2.75  | TransactionId_Inequality_DifferentGuid                        | Domain   | Strong-Typed ID |
| T-2.76  | CategoryService_GetSystemDefaults_AllHaveValidIds             | Domain   | CategoryService |
| T-2.77  | CategoryService_GetSystemDefaults_AllHaveCorrectUserId        | Domain   | CategoryService |
| T-2.78  | DateRangeSpec_IncludesBoundaryDates                           | Domain   | Specification   |
| T-2.79  | CategorySpec_ExcludesTransactionsWithDifferentCategory        | Domain   | Specification   |
| T-2.80  | AmountRangeSpec_ExcludesTransactionsOutOfRange                | Domain   | Specification   |
| T-2.81  | UserSpec_ExcludesTransactionsForDifferentUser                 | Domain   | Specification   |

**Total: 81 tests (all Domain)**

**Tests by Area:**

|
 Area            
|
 Test Count 
|
 Test IDs                                      
|
|
-----------------
|
------------
|
-----------------------------------------------
|
|
 Transaction     
|
 10         
|
 T-2.01–T-2.06, T-2.51, T-2.52, T-2.57, T-2.58 
|
|
 Category        
|
 12         
|
 T-2.07–T-2.14, T-2.53, T-2.59–T-2.61        
|
|
 Budget          
|
 15         
|
 T-2.15–T-2.22, T-2.56, T-2.62–T-2.67        
|
|
 Money           
|
 13         
|
 T-2.23–T-2.31, T-2.54, T-2.68–T-2.70        
|
|
 DateRange       
|
 5          
|
 T-2.32–T-2.34, T-2.55, T-2.71               
|
|
 Strong-Typed ID 
|
 8          
|
 T-2.35–T-2.38, T-2.72–T-2.75                
|
|
 CategoryService 
|
 8          
|
 T-2.39–T-2.44, T-2.76, T-2.77               
|
|
 Specification   
|
 10         
|
 T-2.45–T-2.50, T-2.78–T-2.81                
|

---

## Deliverables

|
#
|
 Deliverable                                                  
|
 Layer  
|
 Acceptance                                                     
|
|
--------
|
--------------------------------------------------------------
|
--------
|
----------------------------------------------------------------
|
|
 D-2.01 
|
`Transaction`
 aggregate root                                 
|
 Domain 
|
 Tests T-2.01–T-2.06, T-2.51, T-2.52, T-2.57, T-2.58 pass    
|
|
 D-2.02 
|
`Category`
 aggregate root                                    
|
 Domain 
|
 Tests T-2.07–T-2.14, T-2.53, T-2.59–T-2.61 pass              
|
|
 D-2.03 
|
`Budget`
 aggregate root                                      
|
 Domain 
|
 Tests T-2.15–T-2.22, T-2.56, T-2.62–T-2.67 pass              
|
|
 D-2.04 
|
`Money`
 value object                                         
|
 Domain 
|
 Tests T-2.23–T-2.31, T-2.54, T-2.68–T-2.70 pass              
|
|
 D-2.05 
|
`DateRange`
 value object                                     
|
 Domain 
|
 Tests T-2.32–T-2.34, T-2.55, T-2.71 pass                     
|
|
 D-2.06 
|
`TransactionId`
, 
`CategoryId`
, 
`BudgetId`
 value objects      
|
 Domain 
|
 Tests T-2.35–T-2.38, T-2.72–T-2.75 pass                      
|
|
 D-2.07 
|
`CategoryService`
 domain service                             
|
 Domain 
|
 Tests T-2.39–T-2.44, T-2.76, T-2.77 pass                     
|
|
 D-2.08 
|
`BaseSpecification<T>`
 abstract class                        
|
 Domain 
|
 Compiles; used by all concrete specifications                  
|
|
 D-2.09 
|
 4 concrete specifications                                    
|
 Domain 
|
 Tests T-2.45–T-2.50, T-2.78–T-2.81 pass                      
|
|
 D-2.10 
|
`ITransactionRepository`
 interface                           
|
 Domain 
|
 Compiles; 8 async methods defined                              
|
|
 D-2.11 
|
`ICategoryRepository`
 interface                              
|
 Domain 
|
 Compiles; 8 async methods defined                              
|
|
 D-2.12 
|
`IBudgetRepository`
 interface                                
|
 Domain 
|
 Compiles; 6 async methods defined                              
|
|
 D-2.13 
|
 All Domain.Tests (81 tests)                                  
|
 Tests  
|
`dotnet test --filter Category=Domain`
 all green; 100% coverage 
|

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
------------------------------------------------------------------------
|
--------------------------------------------------------------
|
|
 SC-2.1 
|
 All 81 domain tests pass                                              
|
`dotnet test --filter Category=Domain`
 exit code 0           
|
|
 SC-2.2 
|
 Domain test coverage = 100%                                           
|
 coverlet report on all Domain code (domain-only phase rule)  
|
|
 SC-2.3 
|
 No Application/Infrastructure/Frontend code created or modified        
|
 Manual audit: no changes outside Domain/ and Domain.Tests/   
|
|
 SC-2.4 
|
 All entities use strong-typed IDs                                     
|
 Code review: no raw Guid/string used as entity identifiers   
|
|
 SC-2.5 
|
 All value objects are immutable                                       
|
 No public setters; all use C# record types                   
|
|
 SC-2.6 
|
 CategoryService uses mocked repository interfaces                     
|
 Test inspection: Moq used for 
`ICategoryRepository`
|
|
 SC-2.7 
|
 Domain project has ZERO NuGet dependencies                            
|
 No 
`<PackageReference>`
 in Domain.csproj                     
|
|
 SC-2.8 
|
 Domain project has ZERO project references                            
|
 No 
`<ProjectReference>`
 in Domain.csproj                     
|
|
 SC-2.9 
|
 All repository interfaces define async methods only                   
|
 Code review: all return 
`Task<T>`
|
|
 SC-2.10
|
 System default categories are exactly 4 with correct names            
|
 Test T-2.44 validates count and names                        
|
|
 SC-2.11
|
 Money arithmetic enforces same-currency constraint                    
|
 Tests T-2.25 and T-2.68 validate cross-currency rejection    
|
|
 SC-2.12
|
 Existing Phase 0 and Phase 1 tests still pass                        
|
`dotnet test`
 (all) exit code 0; no regressions              
|

---

## Assumptions

1. **Phase 0 base abstractions are implemented and tested.** `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `DomainException`, `EntityNotFoundException`, and `ISpecification<T>` are available and stable.
2. **Phase 1 `UserId` value object is implemented.** The `UserId(string)` value object with null/empty guard is available in `Domain/ValueObjects/UserId.cs`.
3. **Domain project remains at ZERO NuGet dependencies.** All domain code uses only .NET BCL types. No third-party packages are allowed.
4. **`Expression<Func<T, bool>>` from `System.Linq.Expressions` is part of .NET BCL** and does not count as an external dependency.
5. **Specifications are tested by compiling the `Criteria` expression and evaluating it against in-memory Transaction objects.** No real database is needed for specification tests.
6. **CategoryService tests use Moq to mock `ICategoryRepository`.** The repository interface is the only dependency that needs mocking in this phase.
7. **`DateTime.UtcNow` is used for all timestamps.** No time zone conversion or abstraction (`IClock`) is introduced in this phase. If test flakiness occurs due to timing, a ±1 second tolerance is applied.
8. **Money uses `decimal` for financial precision.** No floating-point types (`float`, `double`) are used for monetary amounts.
9. **Budget uniqueness (one per user-category-month) is enforced at the repository level (Phase 3)**, not as a domain entity invariant, because the entity cannot query the repository.
10. **All tests use `[Trait("Category", "Domain")]`** attribute for filtering via `dotnet test --filter Category=Domain`.

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
--------------------------------------------------------------
|
--------
|
-------------
|
-----------------------------------------------------------------------------
|
|
 R-2.1 
|
 Specification 
`Criteria`
 expressions not compatible with Supabase query translation (Phase 3) 
|
 Medium 
|
 Medium      
|
 Specs tested in-memory now; Infrastructure layer may need to translate expressions to Postgrest filters. Document as known limitation. 
|
|
 R-2.2 
|
`DateTime.UtcNow`
 in constructors causes test flakiness       
|
 Low    
|
 Medium      
|
 Apply ±1 second tolerance in assertions; consider 
`IClock`
 abstraction in future if persistent issue. 
|
|
 R-2.3 
|
`record`
 inheritance (
`Money : ValueObject`
) has C# edge cases 
|
 Low    
|
 Low         
|
 Test equality explicitly; verify record semantics with inheritance.          
|
|
 R-2.4 
|
 Large test count (81) makes test maintenance costly           
|
 Low    
|
 Low         
|
 Tests are simple and focused; Arrange-Act-Assert keeps them maintainable.    
|
|
 R-2.5 
|
 Budget 
`PercentageUsed`
 division edge case (limit = 0)       
|
 Medium 
|
 Low         
|
 Constructor prevents zero limit; 
`PercentageUsed`
 has guard returning 0m.    
|
|
 R-2.6 
|
 CategoryService 
`GetSystemDefaults`
 generates new Guids each call 
|
 Low 
|
 Low         
|
 Expected behavior for factory method; in Phase 3, defaults are seeded once and persisted. 
|

---

## Implementation Notes

### Recommended Implementation Order
Step 1: Create Value Object files and write tests (RED phase)
└── TransactionId, CategoryId, BudgetId, Money, DateRange
└── Tests: T-2.23–T-2.31, T-2.32–T-2.38, T-2.54–T-2.55, T-2.68–T-2.75
└── Verify: tests FAIL (red)

Step 2: Implement Value Objects (GREEN phase)
└── All value objects with validation and arithmetic
└── Verify: dotnet test --filter Category=Domain — value object tests GREEN

Step 3: Create Entity files and write tests (RED phase)
└── Transaction, Category, Budget
└── Tests: T-2.01–T-2.22, T-2.51–T-2.53, T-2.56–T-2.67
└── Verify: tests FAIL (red)

Step 4: Implement Entities (GREEN phase)
└── All three aggregate roots with invariants and mutation methods
└── Verify: dotnet test --filter Category=Domain — entity tests GREEN

Step 5: Create Repository Interfaces
└── ITransactionRepository, ICategoryRepository, IBudgetRepository
└── No tests needed (compile-check only)
└── Verify: dotnet build succeeds

Step 6: Create CategoryService tests (RED phase)
└── Tests: T-2.39–T-2.44, T-2.76, T-2.77
└── Mock ICategoryRepository with Moq
└── Verify: tests FAIL (red)

Step 7: Implement CategoryService (GREEN phase)
└── ValidateUniqueName, CanDeleteCategory, GetSystemDefaults
└── Verify: dotnet test --filter Category=Domain — service tests GREEN

Step 8: Create BaseSpecification and Specification tests (RED phase)
└── Tests: T-2.45–T-2.50, T-2.78–T-2.81
└── Verify: tests FAIL (red)

Step 9: Implement Specifications (GREEN phase)
└── BaseSpecification, 4 concrete specifications
└── Verify: dotnet test --filter Category=Domain — specification tests GREEN

Step 10: Final validation
└── dotnet build → zero errors, zero warnings
└── dotnet test → ALL tests green (Phase 0 + Phase 1 + Phase 2)
└── dotnet test --filter Category=Domain → 81 Phase 2 tests green
└── Coverage report → domain = 100%
└── Audit Domain.csproj → zero , zero
└── Audit: no Application/Infrastructure/Frontend changes

text

### Spec-Driven Workflow Compliance

| Step | Workflow Stage          | Phase 2 Action                                                        |
|------|-------------------------|-----------------------------------------------------------------------|
| 1    | Write Test Spec         | ✅ Tests written first (Steps 1, 3, 6, 8 above)                      |
| 2    | Define Handler Stub     | ⛔ N/A — no MediatR handlers in Domain-Only phase                    |
| 3    | Build Domain            | ✅ Value objects, entities, service, specifications (Steps 2, 4, 7, 9)|
| 4    | Implement Persistence   | ⛔ N/A — Domain-Only phase; deferred to Phase 3                      |
| 5    | Wire UI                 | ⛔ N/A — Domain-Only phase; deferred to Phase 3+                     |

### Testing Patterns Used in This Phase

| Pattern               | Description                                                        | Example                                          |
|-----------------------|--------------------------------------------------------------------|--------------------------------------------------|
| Direct Construction   | Test entity/VO creation and invariant validation                   | `new Transaction(id, userId, amount, date, desc)` |
| Exception Assertion   | Verify DomainException thrown with expected message                 | `Assert.Throws<DomainException>(() => ...)`      |
| Mutation Verification | Call mutation method, verify property changed + UpdatedAt set      | `transaction.Categorize(catId); Assert.NotNull(transaction.UpdatedAt)` |
| Guard Method          | Verify boolean guard methods return expected values                | `Assert.False(category.CanDelete(false))`        |
| Expression Evaluation | Compile specification Criteria and evaluate against in-memory data | `spec.Criteria.Compile()(transaction)`           |
| Mock Repository       | Use Moq to mock repository interfaces for domain service tests    | `mock.Setup(r => r.FindByNameAndUserAsync(...))` |

---

_Phase Spec Version: 1.0.0 | Created: 2026-02-15 | Aligned with Constitution v1.1.0_