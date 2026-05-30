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
/// Specification tests for IBudgetRepository and SupabaseBudgetRepository.
/// Verifies interface contract compliance.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SupabaseBudgetRepositoryTests
{
    private readonly Type _interfaceType = typeof(IBudgetRepository);
    private readonly Type _implType = typeof(SupabaseBudgetRepository);

    [Fact]
    public void SupabaseBudgetRepository_Implements_IBudgetRepository()
    {
        Assert.True(_interfaceType.IsAssignableFrom(_implType),
            $"{_implType.Name} should implement {_interfaceType.Name}");
    }

    [Fact]
    public void GetByIdAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByIdAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Budget?>), method!.ReturnType);
    }

    [Fact]
    public void GetByUserIdAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByUserIdAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<System.Collections.Generic.IReadOnlyList<Budget>>), method!.ReturnType);
    }

    [Fact]
    public void GetByUserAndCategoryAndMonthAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByUserAndCategoryAndMonthAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Budget?>), method!.ReturnType);
    }

    [Fact]
    public void AddAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("AddAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    [Fact]
    public void UpdateAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("UpdateAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    [Fact]
    public void DeleteAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("DeleteAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
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
