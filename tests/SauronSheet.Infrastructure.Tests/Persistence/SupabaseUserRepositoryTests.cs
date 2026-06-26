namespace SauronSheet.Infrastructure.Tests.Persistence;

using System;
using System.Threading.Tasks;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Contract and structure tests for SupabaseUserRepository.
/// Verifies interface compliance and method signatures.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SupabaseUserRepositoryTests
{
    private readonly Type _interfaceType = typeof(IUserProfileRepository);
    private readonly Type _implType = typeof(SupabaseUserRepository);

    [Fact]
    public void SupabaseUserRepository_Implements_IUserProfileRepository()
    {
        Assert.True(_interfaceType.IsAssignableFrom(_implType),
            $"{_implType.Name} should implement {_interfaceType.Name}");
    }

    [Fact]
    public void EnsureExistsAsync_IsDefinedOnInterface()
    {
        var method = _interfaceType.GetMethod("EnsureExistsAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);

        Assert.Equal(typeof(Task), method.ReturnType);
    }

    [Fact]
    public void Constructor_ThrowsOnNullClient()
    {
        Assert.Throws<ArgumentNullException>(() => new SupabaseUserRepository(null!));
    }
}
