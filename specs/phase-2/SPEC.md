# SauronSheet Phase 2: Core Data Model & Domain Entities

**Version**: 1.0.0  
**Duration**: 2-3 weeks  
**Status**: ⏳ Blocked by Phase 1  
**Depends**: Phase 0, Phase 1

---

## Goal

Implement core business entities (Transaction, Category, Budget) with domain-driven invariants. This creates the data model foundation for PDF import (Phase 3) and analytics (Phase 4).

---

## Requirements

### Functional Requirements

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| **FR-001** | Category entity with system defaults | Groceries, Transport, Utilities, Other |
| **FR-002** | Transaction entity with validation | Amount > 0, category required, date validation |
| **FR-003** | Budget entity with overspend detection | IsOverBudget() method returns true if spent > limit |
| **FR-004** | Categories marked as system or custom | IsSystemDefault flag prevents deletion |
| **FR-005** | Transactions queryable by date range | GetTransactionsByMonth() returns filtered list |

### Non-Functional Requirements
- NF-001: 20+ unit tests verifying invariants
- NF-002: All entities immutable after creation
- NF-003: Domain exceptions used for validation errors
- NF-004: Value objects for Amount, Money types

---

## Architecture

### New Entities

**Category**
```csharp
public class Category : Entity<Guid>
{
    public string Name { get; private set; }
    public string Icon { get; private set; }
    public bool IsSystemDefault { get; private set; }
    public Guid UserId { get; private set; }
    
    public static Category CreateSystem(string name, string icon) => new(name, icon, isSystemDefault: true);
    public static Category CreateCustom(Guid userId, string name, string icon) => new(userId, name, icon);
}
```

**Transaction**
```csharp
public class Transaction : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string Description { get; private set; }
}
```

**Budget**
```csharp
public class Budget : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Money Limit { get; private set; }
    
    public bool IsOverBudget(Money spent) => spent > Limit;
}
```

### Value Objects

**Money**
```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; } = "USD";
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

## Deliverables

### Domain Layer
- [ ] `Domain/Entities/Category.cs` (factory methods for system/custom)
- [ ] `Domain/Entities/Transaction.cs` (with validation)
- [ ] `Domain/Entities/Budget.cs` (IsOverBudget logic)
- [ ] `Domain/ValueObjects/Money.cs` (Currency, Amount)
- [ ] `Domain/Services/CategoryService.cs` (get system categories, validate custom)
- [ ] `Domain/Repositories/ITransactionRepository.cs` (extends IRepository)
- [ ] `Domain/Repositories/ICategoryRepository.cs`
- [ ] `Domain/Repositories/IBudgetRepository.cs`

### Application Layer
- [ ] `Application/Features/Categories/CreateCategoryCommand.cs` + handler
- [ ] `Application/Features/Categories/GetCategoriesQuery.cs` + handler
- [ ] `Application/Features/Transactions/CreateTransactionCommand.cs` + handler
- [ ] `Application/Features/Budgets/CreateBudgetCommand.cs` + handler
- [ ] `Application/Tests/Features/Entities/CategoryTests.cs` (8 tests)
- [ ] `Application/Tests/Features/Entities/TransactionTests.cs` (8 tests)
- [ ] `Application/Tests/Features/Entities/BudgetTests.cs` (4 tests)

### Infrastructure Layer
- [ ] `Infrastructure/Persistence/Migrations/003_CreateCategoriesTable.sql`
- [ ] `Infrastructure/Persistence/Migrations/004_CreateTransactionsTable.sql`
- [ ] `Infrastructure/Persistence/Migrations/005_CreateBudgetsTable.sql`
- [ ] `Infrastructure/Persistence/Repositories/CategoryRepository.cs`
- [ ] `Infrastructure/Persistence/Repositories/TransactionRepository.cs`
- [ ] `Infrastructure/Persistence/Repositories/BudgetRepository.cs`

### Frontend Layer
- [ ] `Frontend/Pages/Categories/Index.cshtml` (list system categories)
- [ ] `Frontend/Pages/Categories/Create.cshtml` (custom category form)
- [ ] `Frontend/Pages/Transactions/Index.cshtml` (transaction list)
- [ ] `Frontend/Pages/Transactions/Create.cshtml` (manual entry form)

---

## Test Specifications

### Category Tests (8 tests)

- **T02-001**: System category cannot be deleted (IsSystemDefault = true)
- **T02-002**: Custom category can be created by user
- **T02-003**: CategoryService returns 4 system categories (Groceries, Transport, Utilities, Other)
- **T02-004**: Category name validation rejects empty string
- **T02-005**: Category name max length enforced (50 characters)
- **T02-006**: Two categories with same name but different user IDs allowed
- **T02-007**: GetCategoriesQuery returns only current user's categories
- **T02-008**: Duplicate category name for user rejected

### Transaction Tests (8 tests)

- **T02-009**: Transaction amount must be > 0
- **T02-010**: Transaction requires category ID
- **T02-011**: Transaction date cannot be in future
- **T02-012**: Transaction description optional but max 500 chars if provided
- **T02-013**: Transaction amount uses Money value object
- **T02-014**: GetTransactionsByMonth returns only specified month
- **T02-015**: Transaction sum calculation for category
- **T02-016**: Transaction queries respect ScopedQueryBehavior (tenant isolation)

### Budget Tests (4 tests)

- **T02-017**: Budget creation requires limit > 0
- **T02-018**: IsOverBudget returns true when spent > limit
- **T02-019**: IsOverBudget returns false when spent <= limit
- **T02-020**: Budget queries return only current user's budgets

---

## Success Criteria

✅ Phase 2 is complete when:

1. `dotnet test` shows **20/20 Phase 2 tests passing**
2. 4 system categories in database (Groceries, Transport, Utilities, Other)
3. Category, Transaction, Budget entities fully functional
4. Money value object used in all amounts
5. All Phase 0 + Phase 1 tests still passing (11 + 8 = 19)
6. Categories page shows system + user categories
7. Transaction creation form validates and stores data

Total passing tests: 11 (Phase 0) + 8 (Phase 1) + 20 (Phase 2) = **39 tests**

---

## Database Schema

```sql
-- Categories
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    name VARCHAR(50) NOT NULL,
    icon VARCHAR(50),
    is_system_default BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT NOW()
);
CREATE UNIQUE INDEX idx_categories_user_name ON categories(user_id, name) 
    WHERE NOT is_system_default;

-- Transactions
CREATE TABLE transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    category_id UUID NOT NULL REFERENCES categories(id),
    amount DECIMAL(12,2) NOT NULL CHECK (amount > 0),
    transaction_date DATE NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX idx_transactions_user_date ON transactions(user_id, transaction_date);

-- Budgets
CREATE TABLE budgets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    category_id UUID NOT NULL REFERENCES categories(id),
    limit_amount DECIMAL(12,2) NOT NULL CHECK (limit_amount > 0),
    month_year DATE NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, category_id, month_year)
);
```

---

## Timeline

- **Week 1**: Entity definitions + Money value object + tests
- **Week 2**: Repository implementations + migrations + 20 tests
- **Week 3**: Pages + CategoryService + system categories

Target: 20 tests green + core data model functional

---

**Specification Version**: 1.0.0  
**Last Updated**: 2026-02-14
