using SauronSheet.Domain.Common;
using Moq;
using Xunit;

using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

/// <summary>
/// Tests for GetDistinctImportedSourcesQueryHandler.
/// RF-6a: distinct sources + alphabetical order.
/// RF-6b: empty list when no transactions.
/// </summary>
public class GetDistinctImportedSourcesQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetDistinctImportedSourcesQueryHandler _handler;

    public GetDistinctImportedSourcesQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetDistinctImportedSourcesQueryHandler(
            _transactionRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateTransaction(string? importedFrom)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(50m, "EUR"),
            DateTime.UtcNow,
            "Test transaction",
            importedFrom: importedFrom);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetDistinctSources_ReturnsDistinctAlphabetical()
    {
        // Arrange - transactions from 3 source files, with one duplicate
        var transactions = new List<Transaction>
        {
            CreateTransaction("recibos.pdf"),
            CreateTransaction("nomina.pdf"),
            CreateTransaction("facturas.pdf"),
            CreateTransaction("nomina.pdf"), // duplicate
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(new GetDistinctImportedSourcesQuery(), CancellationToken.None);

        // Assert - distinct + alphabetical
        Assert.Equal(3, result.Count);
        Assert.Equal("facturas.pdf", result[0]);
        Assert.Equal("nomina.pdf", result[1]);
        Assert.Equal("recibos.pdf", result[2]);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetDistinctSources_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _handler.Handle(new GetDistinctImportedSourcesQuery(), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetDistinctSources_FiltersNullAndEmpty()
    {
        // Arrange - mix of null, empty, and valid sources
        var transactions = new List<Transaction>
        {
            CreateTransaction(null),
            CreateTransaction(""),
            CreateTransaction("nomina.pdf"),
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(new GetDistinctImportedSourcesQuery(), CancellationToken.None);

        // Assert - only "nomina.pdf" should pass (whitespace-only is not filtered by IsNullOrEmpty)
        Assert.Single(result);
        Assert.Equal("nomina.pdf", result[0]);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetDistinctSources_CaseInsensitiveDistinct()
    {
        // Arrange - same source with different casing
        var transactions = new List<Transaction>
        {
            CreateTransaction("NOMINA.PDF"),
            CreateTransaction("nomina.pdf"),
            CreateTransaction("Nomina.pdf"),
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(new GetDistinctImportedSourcesQuery(), CancellationToken.None);

        // Assert - only 1 result due to case-insensitive distinct
        Assert.Single(result);
    }
}
