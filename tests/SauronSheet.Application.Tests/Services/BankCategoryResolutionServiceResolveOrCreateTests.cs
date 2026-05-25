namespace SauronSheet.Application.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;
using Application.Services;
using Moq;
using Xunit;

/// <summary>
/// Behavioural tests for BankCategoryResolutionService.ResolveOrCreateAsync (PCE-3 / PCE-4).
/// All repository interactions are mocked — no real DB required.
///
/// PCE-3: Get-or-add for category (create if not found, bypass system defaults, handle 23505 conflict).
/// PCE-4: Get-or-add for subcategory (scoped by categoryId, IsAutoCreated=true).
/// </summary>
[Trait("Category", "Application")]
public class BankCategoryResolutionServiceResolveOrCreateTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo;
    private readonly Mock<ISubcategoryRepository> _subcategoryRepo;
    private readonly Mock<IBankCategoryTranslationRepository> _translationRepo;
    private readonly BankCategoryResolutionService _service;
    private readonly UserId _userId;
    private readonly CancellationToken _ct;

    public BankCategoryResolutionServiceResolveOrCreateTests()
    {
        _categoryRepo    = new Mock<ICategoryRepository>();
        _subcategoryRepo = new Mock<ISubcategoryRepository>();
        _translationRepo = new Mock<IBankCategoryTranslationRepository>();
        _service = new BankCategoryResolutionService(
            _categoryRepo.Object,
            _subcategoryRepo.Object,
            _translationRepo.Object);
        _userId = new UserId("auth0|test-resolve-create");
        _ct = CancellationToken.None;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private Category MakeUserCategory(string name) =>
        new Category(
            new CategoryId(Guid.NewGuid()),
            _userId,
            CategoryName.Create(name),
            CategoryType.Expense,
            ColorHex.Create("#607D8B"),
            "tag");

    private Category MakeSystemDefaultCategory(string name) =>
        Category.CreateSystemDefault(
            new CategoryId(Guid.NewGuid()),
            CategoryName.Create(name),
            CategoryType.Expense,
            ColorHex.Create("#3498DB"),
            "tag");

    private Subcategory MakeSubcategory(string name, CategoryId categoryId) =>
        new Subcategory(
            new SubcategoryId(Guid.NewGuid()),
            _userId,
            categoryId,
            SubcategoryName.Create(name),
            isAutoCreated: true);

    // ════════════════════════════════════════════════════════════════════════
    // PCE-3a: category exists → reuse, source=AutoMatched
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_CategoryExists_ReusesExisting_SourceAutoMatched()
    {
        // Arrange
        var existing = MakeUserCategory("Compras");
        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", null, _ct);

        // Assert: existing category reused, no insert
        Assert.NotNull(result.CategoryId);
        Assert.Equal(existing.Id.Value, result.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
        _categoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<string>()), Times.Never);
    }

    // Triangulation PCE-3a — different category name, same reuse behaviour
    [Fact]
    public async Task ResolveOrCreateAsync_CategoryExists_DifferentName_ReusesExisting()
    {
        var existing = MakeUserCategory("Alimentación");
        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "alimentacion"))
            .ReturnsAsync(existing);

        var result = await _service.ResolveOrCreateAsync(_userId, "Alimentación", null, _ct);

        Assert.Equal(existing.Id.Value, result.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
        _categoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<string>()), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-3b: category not found → created, source=AutoMatched
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_CategoryNotFound_CreatesNewCategory_SourceAutoMatched()
    {
        // Arrange: lookup returns null → service should create
        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "viajes y turismo"))
            .ReturnsAsync((Category?)null);

        Category? capturedCategory = null;
        _categoryRepo
            .Setup(r => r.AddAsync(It.IsAny<Category>(), "viajes y turismo"))
            .Callback<Category, string>((cat, _) => capturedCategory = cat)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResolveOrCreateAsync(_userId, "Viajes y turismo", null, _ct);

        // Assert
        Assert.NotNull(capturedCategory);
        Assert.False(capturedCategory!.IsSystemDefault);
        Assert.Equal(capturedCategory.Id.Value, result.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
        _categoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>(), "viajes y turismo"), Times.Once);
    }

    // Triangulation PCE-3b — value with diacritics, normalized key matches
    [Fact]
    public async Task ResolveOrCreateAsync_CategoryWithDiacritics_NormalizedKeyUsed()
    {
        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "educacion y salud"))
            .ReturnsAsync((Category?)null);

        Category? capturedCategory = null;
        _categoryRepo
            .Setup(r => r.AddAsync(It.IsAny<Category>(), "educacion y salud"))
            .Callback<Category, string>((cat, _) => capturedCategory = cat)
            .Returns(Task.CompletedTask);

        var result = await _service.ResolveOrCreateAsync(_userId, "Educación y salud", null, _ct);

        Assert.NotNull(capturedCategory);
        // Raw name stored, normalized key used for dedup
        Assert.Equal("Educación y salud", capturedCategory!.Name.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-3d: null / whitespace rawCategory → RawOnly, nothing created
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_NullRawCategory_ReturnsRawOnly()
    {
        var result = await _service.ResolveOrCreateAsync(_userId, null, null, _ct);

        Assert.Null(result.CategoryId);
        Assert.Null(result.SubcategoryId);
        Assert.Equal(CategorySource.RawOnly, result.CategorySource);
        _categoryRepo.Verify(r => r.FindByNormalizedNameAndUserAsync(It.IsAny<UserId>(), It.IsAny<string>()), Times.Never);
        _categoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<string>()), Times.Never);
    }

    // Triangulation PCE-3d — whitespace is equivalent to null
    [Fact]
    public async Task ResolveOrCreateAsync_WhitespaceRawCategory_ReturnsRawOnly()
    {
        var result = await _service.ResolveOrCreateAsync(_userId, "   ", null, _ct);

        Assert.Null(result.CategoryId);
        Assert.Equal(CategorySource.RawOnly, result.CategorySource);
        _categoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<string>()), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-3e: concurrent insert → DuplicateEntityException → retry-get → reuses race winner
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_InsertConflict_RetriesAndReturnsRaceWinner()
    {
        // Arrange: first lookup → null; AddAsync → 23505 conflict; second lookup → race winner
        var raceWinner = MakeUserCategory("Compras");

        _categoryRepo
            .SetupSequence(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync((Category?)null)    // first call: not found → triggers insert
            .ReturnsAsync(raceWinner);         // second call: retry-get after conflict

        _categoryRepo
            .Setup(r => r.AddAsync(It.IsAny<Category>(), "compras"))
            .ThrowsAsync(new DuplicateEntityException("category", "compras"));

        // Act
        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", null, _ct);

        // Assert: race winner returned, AutoMatched
        Assert.Equal(raceWinner.Id.Value, result.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
        _categoryRepo.Verify(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"), Times.Exactly(2));
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-3c: IsSystemDefault=true found → bypassed; new user category created
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_SystemDefaultFound_BypassedAndNewUserCategoryCreated()
    {
        // Arrange: defensive guard — even if repo returns a system default, bypass it
        var systemDefault = MakeSystemDefaultCategory("Compras");

        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync(systemDefault);   // repo returns system default (should be bypassed)

        Category? capturedNewCat = null;
        _categoryRepo
            .Setup(r => r.AddAsync(It.IsAny<Category>(), "compras"))
            .Callback<Category, string>((cat, _) => capturedNewCat = cat)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", null, _ct);

        // Assert: system default NOT returned; new user category created
        Assert.NotNull(capturedNewCat);
        Assert.False(capturedNewCat!.IsSystemDefault);
        Assert.Equal(capturedNewCat.Id.Value, result.CategoryId!.Value);
        Assert.NotEqual(systemDefault.Id.Value, result.CategoryId.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
        _categoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>(), "compras"), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-4a: subcategory exists → reused (scoped by categoryId)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_SubcategoryExists_ReusesExisting()
    {
        // Arrange
        var cat  = MakeUserCategory("Compras");
        var sub  = MakeSubcategory("Ropa y complementos", cat.Id);

        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync(cat);

        _subcategoryRepo
            .Setup(r => r.FindByNormalizedNameAsync(_userId, cat.Id, "ropa y complementos"))
            .ReturnsAsync(sub);

        // Act
        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", "Ropa y complementos", _ct);

        // Assert: subcategory reused, no insert
        Assert.Equal(sub.Id.Value, result.SubcategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
        _subcategoryRepo.Verify(r => r.AddAsync(It.IsAny<Subcategory>(), It.IsAny<string>()), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-4b: subcategory not found → created with IsAutoCreated=true
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_SubcategoryNotFound_CreatedWithIsAutoCreated()
    {
        // Arrange
        var cat = MakeUserCategory("Compras");

        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync(cat);

        _subcategoryRepo
            .Setup(r => r.FindByNormalizedNameAsync(_userId, cat.Id, "ropa y complementos"))
            .ReturnsAsync((Subcategory?)null);

        Subcategory? capturedSub = null;
        _subcategoryRepo
            .Setup(r => r.AddAsync(It.IsAny<Subcategory>(), "ropa y complementos"))
            .Callback<Subcategory, string>((sub, _) => capturedSub = sub)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", "Ropa y complementos", _ct);

        // Assert: subcategory created with IsAutoCreated=true, scoped to category
        Assert.NotNull(capturedSub);
        Assert.True(capturedSub!.IsAutoCreated);
        Assert.Equal(cat.Id.Value, capturedSub.CategoryId.Value);
        Assert.Equal(capturedSub.Id.Value, result.SubcategoryId!.Value);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-4c: null subcategory → SubcategoryId=null (no subcategory created)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_NullRawSubcategory_SubcategoryIdNull()
    {
        var cat = MakeUserCategory("Compras");
        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync(cat);

        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", null, _ct);

        Assert.Null(result.SubcategoryId);
        _subcategoryRepo.Verify(r => r.FindByNormalizedNameAsync(It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<string>()), Times.Never);
        _subcategoryRepo.Verify(r => r.AddAsync(It.IsAny<Subcategory>(), It.IsAny<string>()), Times.Never);
    }

    // Triangulation PCE-4c — whitespace subcategory also yields SubcategoryId=null
    [Fact]
    public async Task ResolveOrCreateAsync_WhitespaceRawSubcategory_SubcategoryIdNull()
    {
        var cat = MakeUserCategory("Compras");
        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync(cat);

        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", "  ", _ct);

        Assert.Null(result.SubcategoryId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-4d: subcategory scope is (userId, categoryId) — not global
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveOrCreateAsync_SubcategoryLookup_ScopedByCategoryId()
    {
        // Arrange: two different categories — same subcategory name should be scoped per category
        var catA = MakeUserCategory("Compras");
        var catB = MakeUserCategory("Alimentación");
        var subInCatA = MakeSubcategory("Online", catA.Id);

        _categoryRepo
            .Setup(r => r.FindByNormalizedNameAndUserAsync(_userId, "compras"))
            .ReturnsAsync(catA);

        _subcategoryRepo
            .Setup(r => r.FindByNormalizedNameAsync(_userId, catA.Id, "online"))
            .ReturnsAsync(subInCatA);

        // catB.Id scope: NOT called (different category)
        _subcategoryRepo
            .Setup(r => r.FindByNormalizedNameAsync(_userId, catB.Id, "online"))
            .ReturnsAsync((Subcategory?)null);

        // Act: resolve for catA
        var result = await _service.ResolveOrCreateAsync(_userId, "Compras", "Online", _ct);

        // Assert: subcategory scoped to catA, not catB
        Assert.Equal(subInCatA.Id.Value, result.SubcategoryId!.Value);
        _subcategoryRepo.Verify(r => r.FindByNormalizedNameAsync(_userId, catB.Id, It.IsAny<string>()), Times.Never);
    }
}
