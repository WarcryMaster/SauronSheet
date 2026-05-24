namespace SauronSheet.Infrastructure.Tests.Persistence;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Domain.Repositories;
using Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Specification tests for SupabaseBankCategoryTranslationRepository.
/// Verifies interface contract compliance and method signatures.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SupabaseBankCategoryTranslationRepositoryTests
{
    private readonly Type _interfaceType = typeof(IBankCategoryTranslationRepository);
    private readonly Type _implType = typeof(SupabaseBankCategoryTranslationRepository);

    [Fact]
    public void SupabaseBankCategoryTranslationRepository_Implements_IBankCategoryTranslationRepository()
    {
        // Assert
        Assert.True(_interfaceType.IsAssignableFrom(_implType),
            $"{_implType.Name} should implement {_interfaceType.Name}");
    }

    [Fact]
    public void FindByBankCategoryAsync_IsDefinedOnInterface()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("FindByBankCategoryAsync");

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void FindByBankCategoryAsync_AcceptsCorrectParameters()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("FindByBankCategoryAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
    }

    [Fact]
    public void FindByBankCategoryAsync_ReturnsNullableBankCategoryTranslation()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("FindByBankCategoryAsync");
        Assert.NotNull(method);

        // Assert
        var returnType = method!.ReturnType;
        Assert.True(returnType.IsGenericType, "Return type should be generic (Task<T>)");
        Assert.Equal(typeof(Task<>).Name, returnType.GetGenericTypeDefinition().Name);

        var taskArgument = returnType.GetGenericArguments()[0];
        // BankCategoryTranslation is a record (reference type).
        // For reference types, the nullable annotation (?) doesn't change runtime type.
        Assert.Equal(typeof(BankCategoryTranslation), taskArgument);
    }

    [Fact]
    public void FindByBankCategoryAsync_IsImplementedInRepository()
    {
        // Arrange & Act
        var method = _implType.GetMethod("FindByBankCategoryAsync",
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
