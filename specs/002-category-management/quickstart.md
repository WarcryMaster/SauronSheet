# Quickstart: Category Management Feature

**Feature**: 002-category-management | **Phase**: Implementation | **Date**: March 7, 2026

This guide provides the step-by-step workflow for implementing Category Management following the specification and implementation plan.

---

## Prerequisites

- Visual Studio 2022+ or VS Code with .NET extension
- .NET 10 SDK installed
- Supabase account with database configured
- SauronSheet repository cloned
- MediatR 12+ already configured in solution

---

## Workflow: Domain Layer First (Red-Green-Refactor)

### Step 1: Write Domain Tests (RED phase)

**Location**: `tests/SauronSheet.Domain.Tests/Categories/`

#### 1.1 CategoryNameTests.cs

```csharp
[TestClass]
public class CategoryNameTests
{
    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Create_WithEmptyName_ThrowsDomainException()
    {
        // Arrange & Act
        var result = CategoryName.Create("   ");
        
        // Assert is implicit (exception expected)
    }

    [TestMethod]
    public void Create_WithValidName_ReturnsValueObject()
    {
        // Arrange
        var name = "Groceries";
        
        // Act
        var result = CategoryName.Create(name);
        
        // Assert
        Assert.AreEqual(name, result.Value);
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Create_WithNameExceeding50Chars_ThrowsDomainException()
    {
        // Arrange
        var longName = new string('a', 51);
        
        // Act
        var result = CategoryName.Create(longName);
    }
}
```

#### 1.2 ColorHexTests.cs

```csharp
[TestClass]
public class ColorHexTests
{
    [TestMethod]
    public void Create_WithValidHex_ReturnsValueObject()
    {
        // Arrange
        var hex = "#F39C12";
        
        // Act
        var result = ColorHex.Create(hex);
        
        // Assert
        Assert.AreEqual("#F39C12", result.Value);
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Create_WithInvalidHex_ThrowsDomainException()
    {
        // Arrange
        var invalidHex = "#GGGGGG"; // Invalid hex chars
        
        // Act
        var result = ColorHex.Create(invalidHex);
    }

    [TestMethod]
    public void Create_WithLowercaseHex_NormalizesToUppercase()
    {
        // Arrange
        var hex = "#f39c12";
        
        // Act
        var result = ColorHex.Create(hex);
        
        // Assert
        Assert.AreEqual("#F39C12", result.Value);
    }
}
```

#### 1.3 CategoryTests.cs

```csharp
[TestClass]
public class CategoryTests
{
    [TestMethod]
    public void Constructor_WithValidInputs_CreatesEntity()
    {
        // Arrange
        var id = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-123");
        var name = CategoryName.Create("Groceries");
        var type = CategoryType.Expense;
        var color = ColorHex.Create("#F39C12");
        
        // Act
        var category = new Category(id, userId, name, type, color, "shopping-cart", false);
        
        // Assert
        Assert.AreEqual(id, category.Id);
        Assert.AreEqual("Groceries", category.Name.Value);
    }

    [TestMethod]
    public void CanDelete_WithSystemCategory_ReturnsFalse()
    {
        // Arrange
        var category = CreateSystemCategory();
        
        // Act
        var canDelete = category.CanDelete(hasTransactions: false);
        
        // Assert
        Assert.IsFalse(canDelete);
    }

    [TestMethod]
    public void CanDelete_WithTransactions_ReturnsFalse()
    {
        // Arrange
        var category = CreateCustomCategory();
        
        // Act
        var canDelete = category.CanDelete(hasTransactions: true);
        
        // Assert
        Assert.IsFalse(canDelete);
    }

    [TestMethod]
    public void CanDelete_CustomCategoryNoTransactions_ReturnsTrue()
    {
        // Arrange
        var category = CreateCustomCategory();
        
        // Act
        var canDelete = category.CanDelete(hasTransactions: false);
        
        // Assert
        Assert.IsTrue(canDelete);
    }

    private Category CreateCustomCategory() =>
        new(
            new CategoryId(Guid.NewGuid()),
            new UserId("user-123"),
            CategoryName.Create("Custom"),
            CategoryType.Expense,
            ColorHex.Create("#F39C12"),
            "star",
            isSystemDefault: false);

    private Category CreateSystemCategory() =>
        new(
            new CategoryId(Guid.NewGuid()),
            new UserId("user-123"),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar",
            isSystemDefault: true);
}
```

#### 1.4 CategoryServiceTests.cs

```csharp
[TestClass]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _mockRepository;
    private CategoryService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<ICategoryRepository>();
        _service = new CategoryService(_mockRepository.Object);
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public async Task ValidateUniqueName_WithDuplicateName_ThrowsDomainException()
    {
        // Arrange
        var userId = new UserId("user-123");
        var existingCategory = new Category(
            new CategoryId(Guid.NewGuid()),
            userId,
            CategoryName.Create("Groceries"),
            CategoryType.Expense,
            ColorHex.Create("#F39C12"),
            "shopping-cart",
            false);
        
        _mockRepository
            .Setup(r => r.FindByNameAndUserAsync(userId, "Groceries"))
            .ReturnsAsync(existingCategory);
        
        // Act
        await _service.ValidateUniqueName(userId, "Groceries");
    }

    [TestMethod]
    public void CanDeleteCategory_WithSystemDefault_ReturnsFalse()
    {
        // Arrange
        var category = new Category(
            new CategoryId(Guid.NewGuid()),
            new UserId("user-123"),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar",
            isSystemDefault: true);
        
        // Act
        var canDelete = _service.CanDeleteCategory(category, hasTransactions: false);
        
        // Assert
        Assert.IsFalse(canDelete);
    }

    [TestMethod]
    public void GetSystemDefaults_ReturnsAll24Categories()
    {
        // Act
        var defaults = _service.GetSystemDefaults();
        
        // Assert
        Assert.AreEqual(24, defaults.Count);
        Assert.IsTrue(defaults.All(c => c.IsSystemDefault));
        Assert.AreEqual(5, defaults.Count(c => c.Type == CategoryType.Income));
        Assert.AreEqual(19, defaults.Count(c => c.Type == CategoryType.Expense));
    }
}
```

### Step 2: Create Domain Layer (GREEN phase)

**Location**: `src/SauronSheet.Domain/`

1. **Create `ValueObjects/CategoryId.cs`**
   ```csharp
   public record CategoryId(Guid Value)
   {
       public CategoryId() : this(Guid.NewGuid()) { }
       static CategoryId() { }
   }
   ```

2. **Create `ValueObjects/CategoryName.cs`** — See data-model.md for full implementation

3. **Create `ValueObjects/ColorHex.cs`** — See data-model.md for full implementation

4. **Create `ValueObjects/CategoryType.cs`** — Enum with Income/Expense

5. **Create `Entities/Category.cs`** — AggregateRoot with CanDelete() and Update() methods

6. **Create `Services/CategoryService.cs`** — Cross-entity validation logic

7. **Create `Repositories/ICategoryRepository.cs`** — Interface contracts

### Step 3: Implement Infrastructure Repository

**Location**: `src/SauronSheet.Infrastructure/Persistence/`

1. **Create `SupabaseCategoryRepository.cs`** — Postgrest client integration
2. **Database Migration**: `Migrations/20260307_SeedSystemDefaultCategories.sql`
   - CREATE TABLE categories
   - 24 INSERT statements for system defaults
   - Idempotent logic (IF NOT EXISTS)

### Step 4: Implement Application Layer (CQRS Handlers)

**Location**: `src/SauronSheet.Application/Features/Categories/`

#### 4.1 Commands

**CreateCategoryCommandHandler.cs**
```csharp
public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryId>
{
    private readonly ICategoryRepository _repository;
    private readonly CategoryService _service;
    private readonly IUserContext _userContext; // Extracts UserId from JWT

    public async Task<CategoryId> Handle(CreateCategoryCommand request, CancellationToken token)
    {
        // 1. Extract user context
        var userId = _userContext.GetUserId();
        
        // 2. Validate via Domain Service
        await _service.ValidateUniqueName(userId, request.Name);
        
        // 3. Create Domain Entity
        var category = new Category(
            new CategoryId(Guid.NewGuid()),
            userId,
            CategoryName.Create(request.Name),
            Enum.Parse<CategoryType>(request.Type),
            ColorHex.Create(request.Color),
            request.IconName,
            isSystemDefault: false);
        
        // 4. Persist
        await _repository.AddAsync(category);
        
        // 5. Return new ID
        return category.Id;
    }
}
```

**UpdateCategoryCommandHandler.cs** — Prevents Type/IsSystemDefault changes

**DeleteCategoryCommandHandler.cs** — Queries transaction count; guards via service

#### 4.2 Queries

**GetAllCategoriesQueryHandler.cs** — Returns system + custom categories DTO

**SearchCategoriesQueryHandler.cs** — Filters by name (case-insensitive)

#### 4.3 DTOs

**CategoryDto.cs** — Data transfer object with all fields

### Step 5: Wire Frontend (Razor Pages)

**Location**: `src/SauronSheet.Frontend/Pages/`

1. **Create `Categories.cshtml.cs`** — PageModel with OnGet/OnPost handlers
2. **Create `Categories.cshtml`** — MDBootstrap form + categories list

### Step 6: Integration Tests

**Location**: `tests/SauronSheet.Application.Tests/`

```csharp
[TestClass]
public class CreateCategoryCommandHandlerTests
{
    private Mock<ICategoryRepository> _mockRepository;
    private Mock<CategoryService> _mockService;
    private CreateCategoryCommandHandler _handler;

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesCategoryAndReturnsId()
    {
        // Arrange
        var command = new CreateCategoryCommand
        {
            Name = "Coffee",
            Type = "Expense",
            Color = "#8B6F47",
            IconName = "coffee"
        };
        
        // Act
        var categoryId = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(categoryId);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
    }
}
```

---

## Testing & Validation

### Run All Tests

```bash
# From repository root
dotnet test

# Domain tests only
dotnet test --filter "Category=Domain"

# Application tests only
dotnet test --filter "Category=Application"
```

### Verify Installation

1. **Check database migration runs**: 24 system categories should appear in `categories` table
2. **Create a custom category**: Test API via Postman or frontend form
3. **Verify uniqueness guard**: Try creating duplicate name; should fail with error
4. **Test delete guard**: Create category, add transaction, attempt delete; should fail
5. **Check system defaults immutable**: Attempt to edit/delete system category; should be disabled in UI

---

## Debugging Checklist

| Issue | Check |
|-------|-------|
| Migration fails | Supabase table already exists? Check schema. |
| DomainException "Name exists" | Repository mock returning duplicate? |
| Delete blocked unexpectedly | Transaction count query returning > 0? |
| System categories not seeding | Migration file in correct location? |
| Frontend form not submitting | Validation error? Check browser console. |
| Color picker not working | HTML5 input type supported in browser? |
| Icon selector empty | Bootstrap icon library loaded via CDN? |

---

## Next: Task Generation

Once Domain/Application/Infrastructure/Frontend layers are implemented and tested:

```bash
# Generate detailed task list
.specify\scripts\powershell\generate-tasks.ps1 -FeatureSpec "specs/002-category-management/spec.md"
```

This will create `tasks.md` with dependency-ordered tasks for code review and team collaboration.

