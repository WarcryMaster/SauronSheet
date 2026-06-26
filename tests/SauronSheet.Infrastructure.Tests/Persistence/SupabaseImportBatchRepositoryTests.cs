namespace SauronSheet.Infrastructure.Tests.Persistence;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Contract and structure tests for SupabaseImportBatchRepository.
/// Verifies interface compliance and method signatures.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SupabaseImportBatchRepositoryTests
{
    private readonly Type _interfaceType = typeof(IImportBatchRepository);
    private readonly Type _implType = typeof(SupabaseImportBatchRepository);

    [Fact]
    public void SupabaseImportBatchRepository_Implements_IImportBatchRepository()
    {
        Assert.True(_interfaceType.IsAssignableFrom(_implType),
            $"{_implType.Name} should implement {_interfaceType.Name}");
    }

    [Fact]
    public void AddAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("AddAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Domain.Entities.ImportBatch), parameters[0].ParameterType);
        Assert.Equal(typeof(UserId), parameters[1].ParameterType);

        Assert.Equal(typeof(Task), method.ReturnType);
    }

    [Fact]
    public void GetByUserIdAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("GetByUserIdAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);

        Assert.Equal(typeof(Task<IReadOnlyList<Domain.Entities.ImportBatch>>), method.ReturnType);
    }

    [Fact]
    public void Constructor_ThrowsOnNullClient()
    {
        Assert.Throws<ArgumentNullException>(() => new SupabaseImportBatchRepository(null!));
    }
}
