namespace SauronSheet.Application.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Application.Services;
using Moq;
using Xunit;

/// <summary>
/// Behavioral tests for BankCategoryResolutionService.
/// Mocks all 3 repository interfaces and tests all resolution paths.
/// </summary>
[Trait("Category", "Application")]
public class BankCategoryResolutionServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo;
    private readonly Mock<ISubcategoryRepository> _subcategoryRepo;
    private readonly Mock<IBankCategoryTranslationRepository> _translationRepo;
    private readonly BankCategoryResolutionService _service;
    private readonly UserId _userId;
    private readonly CancellationToken _ct;

    public BankCategoryResolutionServiceTests()
    {
        _categoryRepo = new Mock<ICategoryRepository>();
        _subcategoryRepo = new Mock<ISubcategoryRepository>();
        _translationRepo = new Mock<IBankCategoryTranslationRepository>();
        _service = new BankCategoryResolutionService(
            _categoryRepo.Object,
            _subcategoryRepo.Object,
            _translationRepo.Object);
        _userId = new UserId("auth0|testuser");
        _ct = CancellationToken.None;
    }

    private Category MakeCategory(string name)
    {
        return new Category(
            new CategoryId(Guid.NewGuid()),
            _userId,
            CategoryName.Create(name),
            CategoryType.Expense,
            ColorHex.Create("#3498DB"),
            "tag");
    }

    private Subcategory MakeSubcategory(string name, CategoryId categoryId)
    {
        return new Subcategory(
            new SubcategoryId(Guid.NewGuid()),
            _userId,
            categoryId,
            SubcategoryName.Create(name),
            false);
    }

    // ── CR-2c: Sin match → RawOnly ──────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_NoMatch_ReturnsRawOnly()
    {
        // Arrange
        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("ING Direct", null))
            .ReturnsAsync((BankCategoryTranslation?)null);

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category>().AsReadOnly());

        // Act
        var result = await _service.ResolveAsync(_userId, "ING Direct", null, _ct);

        // Assert
        Assert.Null(result.CategoryId);
        Assert.Null(result.SubcategoryId);
        Assert.Equal(CategorySource.RawOnly, result.CategorySource);
    }

    // ── CR-2a: Match vía traducción → AutoMatched ────────────────────────────

    [Fact]
    public async Task ResolveAsync_TranslationMatch_ReturnsAutoMatched()
    {
        // Arrange
        var alimenCat = MakeCategory("Alimentación");

        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("Aliment.", null))
            .ReturnsAsync(new BankCategoryTranslation("Aliment.", null, "Alimentación", null));

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category> { alimenCat }.AsReadOnly());

        // Act
        var result = await _service.ResolveAsync(_userId, "Aliment.", null, _ct);

        // Assert
        Assert.NotNull(result.CategoryId);
        Assert.Equal(alimenCat.Id.Value, result.CategoryId!.Value);
        Assert.Null(result.SubcategoryId);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
    }

    // ── CR-2b: Match directo (case-insensitive) → AutoMatched ────────────────

    [Fact]
    public async Task ResolveAsync_DirectNameMatch_CaseInsensitive_ReturnsAutoMatched()
    {
        // Arrange
        var comprasCat = MakeCategory("Compras");

        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("compras", null))
            .ReturnsAsync((BankCategoryTranslation?)null);

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category> { comprasCat }.AsReadOnly());

        // Act
        var result = await _service.ResolveAsync(_userId, "compras", null, _ct);

        // Assert
        Assert.NotNull(result.CategoryId);
        Assert.Equal(comprasCat.Id.Value, result.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
    }

    // ── CR-2d: Subcategoría anidada ──────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_WithSubcategoryMatch_ReturnsBothIds()
    {
        // Arrange
        var comprasCat = MakeCategory("Compras");
        var ropaSub = MakeSubcategory("Ropa y complementos", comprasCat.Id);

        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("Compras", "Ropa y complementos"))
            .ReturnsAsync((BankCategoryTranslation?)null);

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category> { comprasCat }.AsReadOnly());

        _subcategoryRepo
            .Setup(r => r.GetByCategoryIdAsync(comprasCat.Id))
            .ReturnsAsync(new List<Subcategory> { ropaSub }.AsReadOnly());

        // Act
        var result = await _service.ResolveAsync(_userId, "Compras", "Ropa y complementos", _ct);

        // Assert
        Assert.NotNull(result.CategoryId);
        Assert.Equal(comprasCat.Id.Value, result.CategoryId!.Value);
        Assert.NotNull(result.SubcategoryId);
        Assert.Equal(ropaSub.Id.Value, result.SubcategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
    }

    // ── CR-2a variant: Translation found but user doesn't have that category → RawOnly ──

    [Fact]
    public async Task ResolveAsync_TranslationFoundButCategoryMissing_ReturnsRawOnly()
    {
        // Arrange
        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("Aliment.", null))
            .ReturnsAsync(new BankCategoryTranslation("Aliment.", null, "Alimentación", null));

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category>().AsReadOnly()); // No "Alimentación" category

        // Act
        var result = await _service.ResolveAsync(_userId, "Aliment.", null, _ct);

        // Assert
        Assert.Null(result.CategoryId);
        Assert.Null(result.SubcategoryId);
        Assert.Equal(CategorySource.RawOnly, result.CategorySource);
    }

    // ── Edge: empty bankCategory → RawOnly ───────────────────────────────────

    [Fact]
    public async Task ResolveAsync_EmptyBankCategory_ReturnsRawOnly()
    {
        // Arrange
        // No setup needed — should short-circuit on empty input

        // Act
        var result = await _service.ResolveAsync(_userId, "", null, _ct);

        // Assert
        Assert.Null(result.CategoryId);
        Assert.Null(result.SubcategoryId);
        Assert.Equal(CategorySource.RawOnly, result.CategorySource);
    }

    // ── Edge: whitespace bankCategory → RawOnly ──────────────────────────────

    [Fact]
    public async Task ResolveAsync_WhitespaceBankCategory_ReturnsRawOnly()
    {
        // Arrange
        // Act
        var result = await _service.ResolveAsync(_userId, "   ", null, _ct);

        // Assert
        Assert.Null(result.CategoryId);
        Assert.Equal(CategorySource.RawOnly, result.CategorySource);
    }

    // ── Edge: null bankCategory → RawOnly ────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_NullBankCategory_ReturnsRawOnly()
    {
        // Arrange
        // Act
        var result = await _service.ResolveAsync(_userId, null, null, _ct);

        // Assert
        Assert.Null(result.CategoryId);
        Assert.Equal(CategorySource.RawOnly, result.CategorySource);
    }

    // ── Subcategory match: subcategory not found → no SubcategoryId ───────────

    [Fact]
    public async Task ResolveAsync_SubcategoryNotFound_ReturnsCategoryOnly()
    {
        // Arrange
        var comprasCat = MakeCategory("Compras");

        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("Compras", "Ropa"))
            .ReturnsAsync((BankCategoryTranslation?)null);

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category> { comprasCat }.AsReadOnly());

        _subcategoryRepo
            .Setup(r => r.GetByCategoryIdAsync(comprasCat.Id))
            .ReturnsAsync(new List<Subcategory>().AsReadOnly()); // No subcategories

        // Act
        var result = await _service.ResolveAsync(_userId, "Compras", "Ropa", _ct);

        // Assert
        Assert.NotNull(result.CategoryId);
        Assert.Null(result.SubcategoryId);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
    }

    // ── CR-2e: Exact translation wins over generic (regression) ──────────────

    [Fact]
    public async Task ResolveAsync_ExactTranslationExists_WinsOverGenericTranslation_ReturnsModa()
    {
        // Arrange
        // Spec CR-2e: fila-A exact (bank_category="Compras", bank_subcategory="Ropa" → resolved="Moda")
        //             fila-B generic (bank_category="Compras", bank_subcategory=null → resolved="General")
        // Repository MUST return the exact translation ("Moda") when called with ("Compras", "Ropa").
        // If the repository incorrectly returns the generic result first, the service would
        // resolve to "General" instead of "Moda" — this assertion would then fail.
        var modaCat = MakeCategory("Moda");
        var generalCat = MakeCategory("General");

        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("Compras", "Ropa"))
            .ReturnsAsync(new BankCategoryTranslation("Compras", "Ropa", "Moda", null));

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category> { modaCat, generalCat }.AsReadOnly());

        // No subcategory "Ropa" exists under "Moda" — service will return SubcategoryId=null
        _subcategoryRepo
            .Setup(r => r.GetByCategoryIdAsync(modaCat.Id))
            .ReturnsAsync(new List<Subcategory>().AsReadOnly());

        // Act
        var result = await _service.ResolveAsync(_userId, "Compras", "Ropa", _ct);

        // Assert: resolved category is "Moda" (exact match wins), NOT "General" (generic fallback)
        Assert.NotNull(result.CategoryId);
        Assert.Equal(modaCat.Id.Value, result.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
    }

    // ── CR-2e triangulation: different pair, exact still wins ────────────────

    [Fact]
    public async Task ResolveAsync_ExactTranslationWithDifferentPair_WinsOverGenericTranslation()
    {
        // Triangulation for CR-2e: verifies exact-before-generic logic is general, not hardcoded.
        // "Alimentación"+"Frutas" → "Fruta y Verdura" (exact) must win over
        // "Alimentación"+null → "Alimentación General" (generic).
        var frutaVerduraCat = MakeCategory("Fruta y Verdura");
        var alimentacionGeneralCat = MakeCategory("Alimentación General");

        _translationRepo
            .Setup(r => r.FindByBankCategoryAsync("Alimentación", "Frutas"))
            .ReturnsAsync(new BankCategoryTranslation("Alimentación", "Frutas", "Fruta y Verdura", null));

        _categoryRepo
            .Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Category> { frutaVerduraCat, alimentacionGeneralCat }.AsReadOnly());

        // No subcategory "Frutas" under "Fruta y Verdura" — service will return SubcategoryId=null
        _subcategoryRepo
            .Setup(r => r.GetByCategoryIdAsync(frutaVerduraCat.Id))
            .ReturnsAsync(new List<Subcategory>().AsReadOnly());

        // Act
        var result = await _service.ResolveAsync(_userId, "Alimentación", "Frutas", _ct);

        // Assert: exact translation ("Fruta y Verdura") wins, not the generic fallback
        Assert.NotNull(result.CategoryId);
        Assert.Equal(frutaVerduraCat.Id.Value, result.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, result.CategorySource);
    }
}
