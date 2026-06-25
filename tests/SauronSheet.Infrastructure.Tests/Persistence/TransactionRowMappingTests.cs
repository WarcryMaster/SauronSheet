namespace SauronSheet.Infrastructure.Tests.Persistence;

using System;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Tests for TransactionRow <-> Transaction mapping, specifically DateTime Kind handling.
/// TIMESTAMPTZ in PostgreSQL stores UTC. The mapping must ensure DateTime.Kind is Utc
/// so that downstream conversions (ToSpainLocal, etc.) work correctly.
/// </summary>
[Trait("Category", "Infrastructure")]
public class TransactionRowMappingTests
{
    /// <summary>
    /// TZ-3: ToDomain must interpret the Date from TIMESTAMPTZ as UTC.
    /// The Postgrest client returns DateTime with Unspecified Kind from TIMESTAMPTZ columns,
    /// but semantically the value is UTC.
    /// </summary>
    [Fact]
    public void ToDomain_Date_InterpretedAsUtc()
    {
        // Arrange — create a TransactionRow as returned by Postgrest from TIMESTAMPTZ
        var row = new TransactionRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "test-user",
            Amount = 100m,
            Currency = "EUR",
            Date = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified),
            Description = "Test transaction"
        };

        // Act
        var domain = row.ToDomain();

        // Assert — Kind must be Utc
        Assert.Equal(DateTimeKind.Utc, domain.Date.Kind);
        Assert.Equal(15, domain.Date.Day);
        Assert.Equal(6, domain.Date.Month);
        Assert.Equal(2024, domain.Date.Year);
    }

    /// <summary>
    /// TZ-4: FromDomain must preserve Utc Kind when mapping from Transaction to TransactionRow.
    /// </summary>
    [Fact]
    public void FromDomain_Date_SerializedAsUtc()
    {
        // Arrange
        var transaction = new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("test-user"),
            new Money(100m, "EUR"),
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            "Test transaction");

        // Act
        var row = TransactionRow.FromDomain(transaction);

        // Assert
        Assert.Equal(DateTimeKind.Utc, row.Date.Kind);
        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc), row.Date);
    }

    /// <summary>
    /// TZ-5: FromDomainForInsert must preserve Utc Kind when mapping for insert.
    /// </summary>
    [Fact]
    public void FromDomainForInsert_Date_SerializedAsUtc()
    {
        // Arrange
        var transaction = new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("test-user"),
            new Money(100m, "EUR"),
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            "Test transaction");

        // Act
        var row = TransactionRow.FromDomainForInsert(transaction);

        // Assert
        Assert.Equal(DateTimeKind.Utc, row.Date.Kind);
        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc), row.Date);
    }
}
