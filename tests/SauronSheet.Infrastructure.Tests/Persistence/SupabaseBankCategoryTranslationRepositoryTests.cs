namespace SauronSheet.Infrastructure.Tests.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Domain.Repositories;
using Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Specification tests for SupabaseBankCategoryTranslationRepository.
/// Verifies interface contract compliance and method signatures.
/// CR-2e-infra: verifies exact-before-generic query execution order using the test seam.
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

    // -------------------------------------------------------------------------
    // CR-2e-infra: Exact match query executed BEFORE generic fallback
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CR_2e_Infra_ExactExecutedBeforeGeneric()
    {
        // CR-2e-infra: ExecuteExactMatchQueryAsync MUST run before ExecuteGenericMatchQueryAsync
        // GIVEN fila-A (exact: Compras+Ropa → Moda) AND fila-B (generic: Compras+null → General)
        var rows = new List<BankCategoryTranslationRow>
        {
            new() { BankCategory = "Compras", BankSubcategory = "Ropa", ResolvedCategoryName = "Moda" },
            new() { BankCategory = "Compras", BankSubcategory = null, ResolvedCategoryName = "General" }
        };

        var repo = new TestableSupabaseBankCategoryTranslationRepository(rows);

        // WHEN FindByBankCategoryAsync("Compras", "Ropa")
        var result = await repo.FindByBankCategoryAsync("Compras", "Ropa");

        // THEN "exact" is called first
        Assert.True(repo.CallOrder.Count >= 1, "At least one query should have been executed");
        Assert.Equal("exact", repo.CallOrder[0]);
        // AND result is fila-A (exact wins over generic)
        Assert.NotNull(result);
        Assert.Equal("Moda", result!.ResolvedCategoryName);
    }

    [Fact]
    public async Task CR_2e_Infra_FallbackGenericWhenNoExact()
    {
        // CR-2e-infra-fallback: when no exact match, MUST fall back to generic
        // GIVEN only fila-B (generic: Compras+null → General), no exact row for Ropa
        var rows = new List<BankCategoryTranslationRow>
        {
            new() { BankCategory = "Compras", BankSubcategory = null, ResolvedCategoryName = "General" }
        };

        var repo = new TestableSupabaseBankCategoryTranslationRepository(rows);

        // WHEN FindByBankCategoryAsync("Compras", "Ropa")
        var result = await repo.FindByBankCategoryAsync("Compras", "Ropa");

        // THEN exact query was attempted (and returned null)
        Assert.Contains("exact", repo.CallOrder);
        // AND generic fallback was used
        Assert.Contains("generic", repo.CallOrder);
        Assert.NotNull(result);
        Assert.Equal("General", result!.ResolvedCategoryName);
    }
}

/// <summary>
/// Test seam subclass: overrides exact/generic query methods with in-memory data.
/// Tracks call order to verify CR-2e exact-before-generic invariant without a real Supabase client.
/// </summary>
internal class TestableSupabaseBankCategoryTranslationRepository
    : SupabaseBankCategoryTranslationRepository
{
    private readonly List<BankCategoryTranslationRow> _rows;

    public List<string> CallOrder { get; } = new();

    public TestableSupabaseBankCategoryTranslationRepository(List<BankCategoryTranslationRow> rows)
        : base() // uses protected no-arg constructor — seam methods never touch _client
        => _rows = rows;

    internal override Task<BankCategoryTranslationRow?> ExecuteExactMatchQueryAsync(
        string bankCategory, string bankSubcategory)
    {
        CallOrder.Add("exact");
        var row = _rows.FirstOrDefault(r =>
            r.BankCategory == bankCategory && r.BankSubcategory == bankSubcategory);
        return Task.FromResult<BankCategoryTranslationRow?>(row);
    }

    internal override Task<BankCategoryTranslationRow?> ExecuteGenericMatchQueryAsync(
        string bankCategory)
    {
        CallOrder.Add("generic");
        var row = _rows.FirstOrDefault(r =>
            r.BankCategory == bankCategory && r.BankSubcategory == null);
        return Task.FromResult<BankCategoryTranslationRow?>(row);
    }
}
