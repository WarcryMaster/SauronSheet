namespace SauronSheet.Infrastructure.Tests.Persistence;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Specification tests for ICategoryRepository and SupabaseCategoryRepository.
/// Verifies interface contract compliance — new methods added for normalized_name support.
/// TDD RED cycle: these tests reference methods that don't exist yet on the interface.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SupabaseCategoryRepositoryTests
{
    private readonly Type _interfaceType = typeof(ICategoryRepository);
    private readonly Type _implType = typeof(SupabaseCategoryRepository);

    // ── FindByNormalizedNameAndUserAsync (task 1.7 / PCE-2 contract) ─────────

    [Fact]
    public void FindByNormalizedNameAndUserAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("FindByNormalizedNameAndUserAsync");

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Category?>), method!.ReturnType);
    }

    [Fact]
    public void FindByNormalizedNameAndUserAsync_AcceptsCorrectParameters()
    {
        var method = _interfaceType.GetMethod("FindByNormalizedNameAndUserAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(CategoryType), parameters[2].ParameterType);
    }

    [Fact]
    public void FindByNormalizedNameAndUserAsync_IsImplementedInRepository()
    {
        var method = _implType.GetMethod("FindByNormalizedNameAndUserAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
    }

    // ── AddAsync — updated signature with normalizedName (task 1.8) ──────────

    [Fact]
    public void AddAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("AddAsync");

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    [Fact]
    public void AddAsync_AcceptsCategoryAndNormalizedName()
    {
        // After task 1.8: AddAsync MUST require normalizedName so the NOT NULL
        // column gets a value on every insert.
        var method = _interfaceType.GetMethod("AddAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Category), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
    }

    [Fact]
    public void AddAsync_IsImplementedInRepository()
    {
        var method = _implType.GetMethod("AddAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
    }

    // ── Existing methods still present ───────────────────────────────────────

    [Fact]
    public void GetByIdAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void GetByUserIdAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByUserIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void FindByNameAndUserAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("FindByNameAndUserAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void Constructor_AcceptsSupabaseClient()
    {
        var constructors = _implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var ctor = Assert.Single(constructors);
        var parameters = ctor!.GetParameters();
        var param = Assert.Single(parameters);
        Assert.Equal(typeof(Supabase.Client), param.ParameterType);
    }
}
