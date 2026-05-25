namespace SauronSheet.Infrastructure.Tests.Persistence;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Specification tests for SupabaseSubcategoryRepository.
/// Verifies interface contract compliance and method signatures.
/// Full behavioral testing is done in Integration tests with real Supabase.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SupabaseSubcategoryRepositoryTests
{
    private readonly Type _interfaceType = typeof(ISubcategoryRepository);
    private readonly Type _implType = typeof(SupabaseSubcategoryRepository);

    [Fact]
    public void SupabaseSubcategoryRepository_Implements_ISubcategoryRepository()
    {
        // Assert
        Assert.True(_interfaceType.IsAssignableFrom(_implType),
            $"{_implType.Name} should implement {_interfaceType.Name}");
    }

    [Fact]
    public void GetByIdAsync_IsDefinedOnInterface()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("GetByIdAsync");

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void GetByIdAsync_AcceptsSubcategoryId_ReturnsNullableSubcategory()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("GetByIdAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        // Assert
        Assert.Single(parameters);
        Assert.Equal(typeof(SubcategoryId), parameters[0].ParameterType);
        Assert.Equal(typeof(Task<Subcategory?>), method.ReturnType);
    }

    [Fact]
    public void GetByUserIdAsync_IsDefinedOnInterface()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("GetByUserIdAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<IReadOnlyList<Subcategory>>), method!.ReturnType);
    }

    [Fact]
    public void GetByCategoryIdAsync_IsDefinedOnInterface()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("GetByCategoryIdAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<IReadOnlyList<Subcategory>>), method!.ReturnType);
    }

    [Fact]
    public void FindByNameAsync_IsDefinedOnInterface()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("FindByNameAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Subcategory?>), method!.ReturnType);
    }

    [Fact]
    public void FindByNameAsync_AcceptsCorrectParameters()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("FindByNameAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(CategoryId), parameters[1].ParameterType);
        Assert.Equal(typeof(string), parameters[2].ParameterType);
    }

    [Fact]
    public void AddAsync_IsDefinedOnInterface()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("AddAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    [Fact]
    public void AddAsync_AcceptsSubcategoryAndNormalizedName()
    {
        // After task 1.8: AddAsync MUST require normalizedName so the NOT NULL
        // DB column gets a value on every subcategory insert.
        var method = _interfaceType.GetMethod("AddAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        // Assert: 2 parameters — Subcategory + normalizedName string
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Subcategory), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
    }

    // ── FindByNormalizedNameAsync (task 1.7 / PCE-4 contract) ────────────────

    [Fact]
    public void FindByNormalizedNameAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("FindByNormalizedNameAsync",
            new[] { typeof(UserId), typeof(CategoryId), typeof(string) });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Subcategory?>), method!.ReturnType);
    }

    [Fact]
    public void FindByNormalizedNameAsync_AcceptsCorrectParameters()
    {
        var method = _interfaceType.GetMethod("FindByNormalizedNameAsync",
            new[] { typeof(UserId), typeof(CategoryId), typeof(string) });
        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(CategoryId), parameters[1].ParameterType);
        Assert.Equal(typeof(string), parameters[2].ParameterType);
    }

    [Fact]
    public void FindByNormalizedNameAsync_IsImplementedInRepository()
    {
        var method = _implType.GetMethod("FindByNormalizedNameAsync",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(UserId), typeof(CategoryId), typeof(string) },
            null);

        Assert.NotNull(method);
    }

    [Fact]
    public void GetByIdAsync_IsImplementedInRepository()
    {
        // Arrange & Act
        var method = _implType.GetMethod("GetByIdAsync",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void GetByUserIdAsync_IsImplementedInRepository()
    {
        // Arrange & Act
        var method = _implType.GetMethod("GetByUserIdAsync",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void GetByCategoryIdAsync_IsImplementedInRepository()
    {
        // Arrange & Act
        var method = _implType.GetMethod("GetByCategoryIdAsync",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void FindByNameAsync_IsImplementedInRepository()
    {
        // Arrange & Act
        var method = _implType.GetMethod("FindByNameAsync",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void AddAsync_IsImplementedInRepository()
    {
        // Arrange & Act
        var method = _implType.GetMethod("AddAsync",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void Constructor_AcceptsSupabaseClient()
    {
        // Arrange & Act
        var constructors = _implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        var ctor = Assert.Single(constructors);
        var parameters = ctor!.GetParameters();
        var param = Assert.Single(parameters);
        Assert.Equal(typeof(Supabase.Client), param.ParameterType);
    }
}
