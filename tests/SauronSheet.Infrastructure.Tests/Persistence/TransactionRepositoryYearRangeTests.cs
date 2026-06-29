using Xunit;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Infrastructure.Tests.Persistence;

/// <summary>
/// RED: Specification tests for the new GetByUserIdAndYearRangeAsync method.
/// These tests fail initially because the method does not exist yet.
/// Phase: PR 1 — T1 Core — Annual Report Redesign.
/// </summary>
[Trait("Category", "Infrastructure")]
public class TransactionRepositoryYearRangeSpecificationTests
{
    // ── Interface Contract Tests (RED: method doesn't exist yet) ──

    /// <summary>
    /// Verifies that GetByUserIdAndYearRangeAsync is defined on the interface.
    /// RED: Will fail to compile until the method signature is added.
    /// </summary>
    [Fact]
    public void GetByUserIdAndYearRangeAsync_IsDefinedOnInterface()
    {
        // Arrange & Act
        Type interfaceType = typeof(ITransactionRepository);
        System.Reflection.MethodInfo? methodInfo = interfaceType.GetMethod("GetByUserIdAndYearRangeAsync");

        // Assert
        Assert.NotNull(methodInfo);
    }

    /// <summary>
    /// Verifies correct parameters: UserId + fromYear + toYear.
    /// </summary>
    [Fact]
    public void GetByUserIdAndYearRangeAsync_HasCorrectParameters()
    {
        // Arrange & Act
        Type interfaceType = typeof(ITransactionRepository);
        System.Reflection.MethodInfo? methodInfo = interfaceType.GetMethod("GetByUserIdAndYearRangeAsync");
        Assert.NotNull(methodInfo);

        System.Reflection.ParameterInfo[] parameters = methodInfo!.GetParameters();

        // Assert: 3 params: UserId, int fromYear, int toYear
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(UserId), parameters[0].ParameterType);
        Assert.Equal(typeof(int), parameters[1].ParameterType);
        Assert.Equal(typeof(int), parameters[2].ParameterType);
    }

    /// <summary>
    /// Verifies return type is Task of IReadOnlyList of Transaction.
    /// </summary>
    [Fact]
    public void GetByUserIdAndYearRangeAsync_ReturnsTaskOfTransactions()
    {
        // Arrange & Act
        Type interfaceType = typeof(ITransactionRepository);
        System.Reflection.MethodInfo? methodInfo = interfaceType.GetMethod("GetByUserIdAndYearRangeAsync");
        Assert.NotNull(methodInfo);

        Type returnType = methodInfo!.ReturnType;

        // Assert: Task<IReadOnlyList<Transaction>>
        Assert.True(returnType.IsGenericType, "Return type should be generic (Task<T>)");
        Assert.Equal(typeof(System.Threading.Tasks.Task<>).Name, returnType.GetGenericTypeDefinition().Name);

        Type taskArgument = returnType.GetGenericArguments()[0];
        Assert.True(taskArgument.IsGenericType, "Task argument should be generic (IReadOnlyList<T>)");
        Assert.Equal(typeof(IReadOnlyList<>).Name, taskArgument.GetGenericTypeDefinition().Name);
    }

    /// <summary>
    /// Verifies that SupabaseTransactionRepository implements the method.
    /// </summary>
    [Fact]
    public void GetByUserIdAndYearRangeAsync_ImplementedInRepository()
    {
        // Arrange & Act
        Type repositoryType = typeof(SauronSheet.Infrastructure.Persistence.SupabaseTransactionRepository);
        System.Reflection.MethodInfo? methodInfo = repositoryType.GetMethod(
            "GetByUserIdAndYearRangeAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.NotNull(methodInfo);
    }

    /// <summary>
    /// Verifies the method is async by checking return type is Task.
    /// </summary>
    [Fact]
    public void GetByUserIdAndYearRangeAsync_IsAsyncMethod()
    {
        // Arrange & Act
        Type interfaceType = typeof(ITransactionRepository);
        System.Reflection.MethodInfo? methodInfo = interfaceType.GetMethod("GetByUserIdAndYearRangeAsync");
        Assert.NotNull(methodInfo);

        Type returnType = methodInfo!.ReturnType;

        // Assert: return type must be Task or Task<T> (awaitable)
        Assert.True(
            returnType.Name == "Task`1" || returnType.Name == "Task",
            "Return type should be Task or Task<T> (awaitable)");
    }

    /// <summary>
    /// Verifies the method is public.
    /// </summary>
    [Fact]
    public void GetByUserIdAndYearRangeAsync_IsPublicMethod()
    {
        // Arrange & Act
        Type interfaceType = typeof(ITransactionRepository);
        System.Reflection.MethodInfo? methodInfo = interfaceType.GetMethod("GetByUserIdAndYearRangeAsync");
        Assert.NotNull(methodInfo);
        Assert.True(methodInfo!.IsPublic, "Method should be public");
    }
}
