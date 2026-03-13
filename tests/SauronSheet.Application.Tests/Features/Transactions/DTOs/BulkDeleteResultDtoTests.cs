using Xunit;
using System.Text.Json;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Transactions.DTOs;

/// <summary>
/// Tests for BulkDeleteResultDto.
/// Phase 4 (Feature 004): Response object validation and serialization.
/// </summary>
[Trait("Category", "Application")]
public class BulkDeleteResultDtoTests
{
    /// <summary>
    /// T026: BulkDeleteResultDto_Serialization_Succeeds
    /// Verifies JSON roundtrip: object → JSON → object preserves state.
    /// </summary>
    [Fact]
    public void Serialization_ToJsonAndBack_PreservesState()
    {
        // Arrange
        var dto = new BulkDeleteResultDto(
            Count: 5,
            ErrorMessage: null,
            FailedTransactionIds: new[] { Guid.Empty }
        );

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var deserialized = JsonSerializer.Deserialize<BulkDeleteResultDto>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(5, deserialized.Count);
        Assert.Null(deserialized.ErrorMessage);
    }

    /// <summary>
    /// T027: BulkDeleteResultDto_FailedIds_Tracked
    /// Verifies that FailedTransactionIds list is properly captured and serialized.
    /// </summary>
    [Fact]
    public void FailedIds_AreTacked_InDto()
    {
        // Arrange
        var failedId1 = Guid.NewGuid();
        var failedId2 = Guid.NewGuid();
        var failedIds = new[] { failedId1, failedId2 };

        var dto = new BulkDeleteResultDto(
            Count: 3,
            ErrorMessage: "1 transaction failed due to constraint.",
            FailedTransactionIds: failedIds
        );

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<BulkDeleteResultDto>(json);

        // Assert
        Assert.NotNull(deserialized?.FailedTransactionIds);
        Assert.Contains(failedId1, deserialized.FailedTransactionIds);
        Assert.Contains(failedId2, deserialized.FailedTransactionIds);
        Assert.NotNull(deserialized.ErrorMessage);
    }
}
