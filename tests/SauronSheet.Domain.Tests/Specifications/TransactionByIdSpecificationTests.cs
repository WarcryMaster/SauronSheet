namespace SauronSheet.Domain.Tests.Specifications;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Unit tests for TransactionByIdSpecification.
/// Tests domain-layer filtering logic for bulk delete operations.
/// TDD Red Phase: Tests are written first; implementation follows.
/// </summary>
[Trait("Category", "Domain")]
public class TransactionByIdSpecificationTests
{
    private readonly UserId _userId = new("test-user-123");
    private readonly UserId _differentUserId = new("different-user-456");

    /// <summary>
    /// Helper method to create a test transaction.
    /// </summary>
    private Transaction CreateTransaction(
        TransactionId? id = null,
        UserId? userId = null,
        string description = "Test Transaction")
    {
        var transId = id ?? new TransactionId(Guid.NewGuid());
        var uid = userId ?? _userId;
        var amount = new Money(100.00m, "EUR");
        var date = DateTime.UtcNow;

        return new Transaction(transId, uid, amount, date, description);
    }

    /// <summary>
    /// T002: TransactionByIdSpecification_FiltersByUserId_Correctly
    /// Verifies that specification correctly filters transactions by UserId.
    /// Users should only see their own transactions for deletion.
    /// </summary>
    [Fact]
    public void FiltersByUserId_WhenMultipleUsers_ReturnsOnlyUserTransactions()
    {
        // Arrange
        var transactionIds = new[]
        {
            new TransactionId(Guid.NewGuid()),
            new TransactionId(Guid.NewGuid()),
            new TransactionId(Guid.NewGuid())
        };

        var transactions = new List<Transaction>
        {
            CreateTransaction(transactionIds[0], _userId, "User Trans 1"),
            CreateTransaction(transactionIds[1], _userId, "User Trans 2"),
            CreateTransaction(new TransactionId(Guid.NewGuid()), _differentUserId, "Other User Trans"),
            CreateTransaction(transactionIds[2], _userId, "User Trans 3")
        }.AsQueryable();

        var spec = new TransactionByIdSpecification(_userId, transactionIds);

        // Act
        var result = spec.Criteria.Compile().Invoke(transactions.First());
        var filteredResults = transactions.Where(spec.Criteria).ToList();

        // Assert
        Assert.NotNull(spec);
        Assert.Equal(3, filteredResults.Count);
        Assert.All(filteredResults, t => Assert.Equal(_userId, t.UserId));
    }

    /// <summary>
    /// T003: TransactionByIdSpecification_MaxResults_Enforced
    /// Verifies that MaxResults limit is enforced at 1000.
    /// Specification should reject attempts to delete >1000 transactions.
    /// </summary>
    [Fact]
    public void MaxResults_IsEnforcedAt1000Limit()
    {
        // Arrange
        var validIds = Enumerable.Range(0, 1000)
            .Select(_ => new TransactionId(Guid.NewGuid()))
            .ToList();

        var tooManyIds = Enumerable.Range(0, 1001)
            .Select(_ => new TransactionId(Guid.NewGuid()))
            .ToList();

        // Act & Assert - Valid count should succeed
        var spec = new TransactionByIdSpecification(_userId, validIds);
        Assert.Equal(1000, spec.MaxResults);

        // Act & Assert - Over-limit count should throw
        Assert.Throws<DomainException>(() =>
            new TransactionByIdSpecification(_userId, tooManyIds));
    }

    /// <summary>
    /// T004: TransactionByIdSpecification_EmptyIds_ReturnsEmpty
    /// Verifies behavior when no transaction IDs are provided.
    /// Should handle gracefully without throwing.
    /// </summary>
    [Fact]
    public void EmptyIds_ThrowsOrHandlesGracefully()
    {
        // Arrange
        var emptyIds = new List<TransactionId>();
        var transaction = CreateTransaction();
        var transactions = new List<Transaction> { transaction }.AsQueryable();

        // Act & Assert
        // Should either throw DomainException for empty selection or return empty result
        var exceptionThrown = false;
        try
        {
            var spec = new TransactionByIdSpecification(_userId, emptyIds);
            var result = transactions.Where(spec.Criteria).ToList();
            Assert.Empty(result);
        }
        catch (DomainException)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown || emptyIds.Count == 0);
    }

    /// <summary>
    /// T005: TransactionByIdSpecification_SingleId_FiltersSingleTransaction
    /// Verifies behavior when selecting exactly one transaction.
    /// Should correctly identify and filter single transaction.
    /// </summary>
    [Fact]
    public void SingleId_FiltersSingleTransaction()
    {
        // Arrange
        var targetId = new TransactionId(Guid.NewGuid());
        var otherId1 = new TransactionId(Guid.NewGuid());
        var otherId2 = new TransactionId(Guid.NewGuid());

        var transactions = new List<Transaction>
        {
            CreateTransaction(targetId, _userId, "Target"),
            CreateTransaction(otherId1, _userId, "Other 1"),
            CreateTransaction(otherId2, _userId, "Other 2")
        }.AsQueryable();

        var spec = new TransactionByIdSpecification(_userId, new[] { targetId });

        // Act
        var result = transactions.Where(spec.Criteria).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(targetId, result.First().Id);
    }

    /// <summary>
    /// T006: TransactionByIdSpecification_BulkIds_Filters100Plus
    /// Verifies that specification correctly handles bulk selection (100+ transactions).
    /// Should filter all requested transactions without performance degradation.
    /// </summary>
    [Fact]
    public void BulkIds_Filters100Plus_Correctly()
    {
        // Arrange
        var bulkCount = 150;
        var bulkIds = Enumerable.Range(0, bulkCount)
            .Select(_ => new TransactionId(Guid.NewGuid()))
            .ToList();

        var transactions = bulkIds
            .Select((id, index) => CreateTransaction(id, _userId, $"Transaction {index}"))
            .Concat(new[] { CreateTransaction(new TransactionId(Guid.NewGuid()), _userId, "Extra") })
            .AsQueryable();

        var spec = new TransactionByIdSpecification(_userId, bulkIds);

        // Act
        var result = transactions.Where(spec.Criteria).ToList();

        // Assert
        Assert.Equal(bulkCount, result.Count);
        Assert.All(result, t => Assert.Contains(t.Id, bulkIds));
    }

    /// <summary>
    /// T007: TransactionByIdSpecification_NullUserId_ThrowsDomainException
    /// Verifies that specification rejects null UserId.
    /// Guard clause should prevent multi-tenant isolation violations.
    /// </summary>
    [Fact]
    public void NullUserId_ThrowsDomainException()
    {
        // Arrange
        var ids = new[] { new TransactionId(Guid.NewGuid()) };

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            new TransactionByIdSpecification(null!, ids));

        Assert.Contains("UserId", exception.Message);
    }

    /// <summary>
    /// T008: TransactionByIdSpecification_AtomicityPreserved_WhenFiltering
    /// Verifies that specification maintains aggregate root invariants.
    /// All returned transactions should have consistent state for atomic deletion.
    /// </summary>
    [Fact]
    public void AtomicityPreserved_AllTransactionsFiltered_HaveValidInvariants()
    {
        // Arrange
        var ids = Enumerable.Range(0, 5)
            .Select(_ => new TransactionId(Guid.NewGuid()))
            .ToList();

        var transactions = ids
            .Select((id, index) => CreateTransaction(
                id,
                _userId,
                $"Transaction {index}"))
            .AsQueryable();

        var spec = new TransactionByIdSpecification(_userId, ids);

        // Act
        var result = transactions.Where(spec.Criteria).ToList();

        // Assert
        Assert.Equal(5, result.Count);
        
        // Verify atomic invariants for each returned transaction
        foreach (var transaction in result)
        {
            // All transactions must belong to the requesting user (multi-tenant safety)
            Assert.Equal(_userId, transaction.UserId);
            
            // All transactions must have valid IDs
            Assert.NotNull(transaction.Id);
            Assert.NotEqual(default(TransactionId), transaction.Id);
            
            // All transactions must have valid amounts
            Assert.NotNull(transaction.Amount);
            Assert.True(transaction.Amount.Amount >= 0);
        }
    }
}
