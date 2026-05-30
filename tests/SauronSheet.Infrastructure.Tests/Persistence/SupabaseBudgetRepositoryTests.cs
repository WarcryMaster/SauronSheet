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
/// Specification and integration tests for IBudgetRepository and SupabaseBudgetRepository.
/// Verifies interface contract compliance, BudgetRow structure, domain/infra mapping,
/// and query method contracts.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SupabaseBudgetRepositoryTests
{
    private readonly Type _interfaceType = typeof(IBudgetRepository);
    private readonly Type _implType = typeof(SupabaseBudgetRepository);

    // ────────────────────────────────────────────────────────────────────────
    // Interface contract compliance
    // ────────────────────────────────────────────────────────────────────────

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
    public void GetByUserAndCategoryAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByUserAndCategoryAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<System.Collections.Generic.IReadOnlyList<Budget>>), method!.ReturnType);
    }

    [Fact]
    public void GetActiveByUserAndCategoryAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetActiveByUserAndCategoryAsync");
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(CategoryId), parameters[1].ParameterType);
        Assert.Equal(typeof(DateOnly), parameters[2].ParameterType);
        Assert.Equal(typeof(Task<Budget?>), method.ReturnType);
    }

    [Fact]
    public void GetByUserAndDateRangeAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByUserAndDateRangeAsync");
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(DateOnly), parameters[1].ParameterType);
        Assert.Equal(typeof(DateOnly?), parameters[2].ParameterType);
        Assert.Equal(typeof(Task<System.Collections.Generic.IReadOnlyList<Budget>>), method.ReturnType);
    }

    [Fact]
    public void GetByUserAndCategoryAndMonthAsync_IsNOT_DefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByUserAndCategoryAndMonthAsync");
        Assert.Null(method); // Obsolete method removed in redesign
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

    // ────────────────────────────────────────────────────────────────────────
    // BudgetRow structure (Task 3.2)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BudgetRow_Has_EffectiveFrom_Column()
    {
        var property = typeof(BudgetRow).GetProperty("EffectiveFrom");
        Assert.NotNull(property);
        Assert.Equal(typeof(DateTime), property!.PropertyType);
    }

    [Fact]
    public void BudgetRow_Has_EffectiveUntil_Column()
    {
        var property = typeof(BudgetRow).GetProperty("EffectiveUntil");
        Assert.NotNull(property);
        Assert.Equal(typeof(DateTime?), property!.PropertyType); // Nullable for DATE? column
    }

    [Fact]
    public void BudgetRow_Has_PeriodGranularity_Column()
    {
        var property = typeof(BudgetRow).GetProperty("PeriodGranularity");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property!.PropertyType);
    }

    [Fact]
    public void BudgetRow_Does_NOT_Have_PeriodStart()
    {
        var property = typeof(BudgetRow).GetProperty("PeriodStart");
        Assert.Null(property);
    }

    [Fact]
    public void BudgetRow_Does_NOT_Have_PeriodEnd()
    {
        var property = typeof(BudgetRow).GetProperty("PeriodEnd");
        Assert.Null(property);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Mapping: Domain ↔ Infrastructure (Task 3.4)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDomain_FromRow_MapsAllFields()
    {
        var budgetId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var categoryId = Guid.NewGuid();
        var effectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var limitAmount = 500m;
        var currency = "EUR";
        var createdAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var row = new BudgetRow
        {
            Id = budgetId.ToString(),
            UserId = userId,
            CategoryId = categoryId.ToString(),
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = null, // permanent budget
            PeriodGranularity = "Monthly",
            LimitAmount = limitAmount,
            Currency = currency,
            CreatedAt = createdAt,
            UpdatedAt = null
        };

        var budget = row.ToDomain();

        Assert.Equal(budgetId, budget.Id.Value);
        Assert.Equal(userId, budget.UserId.Value);
        Assert.Equal(categoryId, budget.CategoryId.Value);
        Assert.Equal(DateOnly.FromDateTime(effectiveFrom), budget.EffectiveFrom);
        Assert.Null(budget.EffectiveUntil);
        Assert.Equal(BudgetPeriod.Monthly, budget.PeriodGranularity);
        Assert.Equal(limitAmount, budget.Limit.Amount);
        Assert.Equal(currency, budget.Limit.Currency);
        Assert.Equal(createdAt, budget.CreatedAt);
        Assert.Null(budget.UpdatedAt);
    }

    [Fact]
    public void ToDomain_FromRow_WithEffectiveUntil_MapsCorrectly()
    {
        var effectiveUntil = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var row = new BudgetRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            CategoryId = Guid.NewGuid().ToString(),
            EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveUntil = effectiveUntil,
            PeriodGranularity = "Quarterly",
            LimitAmount = 2000m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        };

        var budget = row.ToDomain();

        Assert.NotNull(budget.EffectiveUntil);
        Assert.Equal(DateOnly.FromDateTime(effectiveUntil), budget.EffectiveUntil!.Value);
        Assert.Equal(BudgetPeriod.Quarterly, budget.PeriodGranularity);
    }

    [Fact]
    public void ToDomain_FromRow_SemesterGranularity()
    {
        var row = new BudgetRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            CategoryId = Guid.NewGuid().ToString(),
            EffectiveFrom = new DateTime(2026, 1, 1),
            PeriodGranularity = "Semester",
            LimitAmount = 3000m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        };

        var budget = row.ToDomain();
        Assert.Equal(BudgetPeriod.Semester, budget.PeriodGranularity);
    }

    [Fact]
    public void ToDomain_FromRow_AnnualGranularity()
    {
        var row = new BudgetRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            CategoryId = Guid.NewGuid().ToString(),
            EffectiveFrom = new DateTime(2026, 1, 1),
            PeriodGranularity = "Annual",
            LimitAmount = 12000m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        };

        var budget = row.ToDomain();
        Assert.Equal(BudgetPeriod.Annual, budget.PeriodGranularity);
    }

    [Fact]
    public void FromDomain_ToRow_Roundtrip_IsConsistent()
    {
        var id = new BudgetId(Guid.NewGuid());
        var userId = new UserId(Guid.NewGuid().ToString());
        var categoryId = new CategoryId(Guid.NewGuid());
        var effectiveFrom = new DateOnly(2026, 3, 15);
        DateOnly? effectiveUntil = new DateOnly(2026, 9, 30);

        var budget = new Budget(
            id, userId, categoryId,
            effectiveFrom, effectiveUntil,
            BudgetPeriod.Monthly,
            new Money(750m, "EUR"));

        var row = BudgetRow.FromDomain(budget);

        Assert.Equal(id.Value.ToString(), row.Id);
        Assert.Equal(userId.Value, row.UserId);
        Assert.Equal(categoryId.Value.ToString(), row.CategoryId);
        Assert.Equal(effectiveFrom, DateOnly.FromDateTime(row.EffectiveFrom));
        Assert.Equal(effectiveUntil.Value, DateOnly.FromDateTime(row.EffectiveUntil!.Value));
        Assert.Equal("Monthly", row.PeriodGranularity);
        Assert.Equal(750m, row.LimitAmount);
        Assert.Equal("EUR", row.Currency);
    }

    [Fact]
    public void FromDomain_ToRow_PermanentBudget_HasNullEffectiveUntil()
    {
        var budget = new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(Guid.NewGuid().ToString()),
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            null, // permanent
            BudgetPeriod.Annual,
            new Money(12000m, "EUR"));

        var row = BudgetRow.FromDomain(budget);

        Assert.Null(row.EffectiveUntil);
        Assert.Equal("Annual", row.PeriodGranularity);
    }

    [Fact]
    public void FromDomainForInsert_DoesNotSetTimestamps()
    {
        var budget = new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(Guid.NewGuid().ToString()),
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            null,
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        var row = BudgetRow.FromDomainForInsert(budget);

        Assert.Equal(default, row.CreatedAt);  // Should be default(DateTime)
        Assert.Null(row.UpdatedAt);            // Should be null
    }

    [Fact]
    public void ToDomain_FromRowThenBack_IsIdempotent()
    {
        // Full roundtrip: domain → row → domain → row
        var original = new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(Guid.NewGuid().ToString()),
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 12, 31),
            BudgetPeriod.Quarterly,
            new Money(2000m, "EUR"));

        var row1 = BudgetRow.FromDomain(original);
        var reconstructed = row1.ToDomain();
        var row2 = BudgetRow.FromDomain(reconstructed);

        // All field-level equality
        Assert.Equal(row1.Id, row2.Id);
        Assert.Equal(row1.UserId, row2.UserId);
        Assert.Equal(row1.CategoryId, row2.CategoryId);
        Assert.Equal(row1.EffectiveFrom, row2.EffectiveFrom);
        Assert.Equal(row1.EffectiveUntil, row2.EffectiveUntil);
        Assert.Equal(row1.PeriodGranularity, row2.PeriodGranularity);
        Assert.Equal(row1.LimitAmount, row2.LimitAmount);
        Assert.Equal(row1.Currency, row2.Currency);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Integration: query method contracts (Task 3.3 / 3.5)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetActiveByUserAndCategoryAsync_ExistsOnImplementation()
    {
        var method = _implType.GetMethod("GetActiveByUserAndCategoryAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(CategoryId), parameters[1].ParameterType);
        Assert.Equal(typeof(DateOnly), parameters[2].ParameterType);
        Assert.Equal(typeof(Task<Budget?>), method.ReturnType);
    }

    [Fact]
    public void GetByUserAndDateRangeAsync_ExistsOnImplementation()
    {
        var method = _implType.GetMethod("GetByUserAndDateRangeAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(DateOnly), parameters[1].ParameterType);
        Assert.Equal(typeof(DateOnly?), parameters[2].ParameterType);
        Assert.Equal(typeof(Task<System.Collections.Generic.IReadOnlyList<Budget>>), method.ReturnType);
    }

    [Fact]
    public void GetByUserAndCategoryAndMonthAsync_DoesNotExistOnImplementation()
    {
        var method = _implType.GetMethod("GetByUserAndCategoryAndMonthAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.Null(method); // Obsolete method removed
    }

    [Fact]
    public void GetByUserAndCategoryAsync_ExistsOnImplementation()
    {
        var method = _implType.GetMethod("GetByUserAndCategoryAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(CategoryId), parameters[1].ParameterType);
        Assert.Equal(typeof(Task<System.Collections.Generic.IReadOnlyList<Budget>>), method.ReturnType);
    }
}
