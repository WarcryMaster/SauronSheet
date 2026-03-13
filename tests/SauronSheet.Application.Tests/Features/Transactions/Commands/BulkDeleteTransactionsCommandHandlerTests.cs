using Moq;
using Xunit;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

/// <summary>
/// Unit tests for BulkDeleteTransactionsCommandHandler.
/// Phase 4 (Feature 004): Bulk delete functionality.
/// Tests orchestration logic, error handling, retry behavior, and multi-tenant isolation.
/// </summary>
[Trait("Category", "Application")]
public class BulkDeleteTransactionsCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly BulkDeleteTransactionsCommandHandler _handler;
    private readonly UserId _userId = new("user-123");
    private readonly UserId _differentUserId = new("user-456");

    public BulkDeleteTransactionsCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId.Value);
        _handler = new BulkDeleteTransactionsCommandHandler(
            _transactionRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateTransaction(UserId userId, string description = "Test")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            userId,
            new Money(100.00m, "EUR"),
            DateTime.UtcNow,
            description);
    }

    /// <summary>
    /// T016: BulkDeleteTransactionsHandler_SuccessfulDelete_ReturnsCount
    /// Verifies happy path: 5 transactions deleted successfully.
    /// </summary>
    [Fact]
    public async Task Handle_SuccessfulDelete_Returns5DeletedCount()
    {
        // Arrange
        var ids = Enumerable.Range(0, 5)
            .Select(_ => new TransactionId(Guid.NewGuid()))
            .ToList();

        _transactionRepoMock
            .Setup(x => x.DeleteTransactionsByIdsAsync(_userId, ids))
            .ReturnsAsync(5);

        var command = new BulkDeleteTransactionsCommand(_userId, ids.AsReadOnly());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(result.FailedTransactionIds ?? new List<Guid>());
    }

    /// <summary>
    /// T017: BulkDeleteTransactionsHandler_UserIdValidation_FailsOnMismatch
    /// Verifies multi-tenant isolation: different UserId raises error.
    /// </summary>
    [Fact]
    public async Task Handle_UserIdMismatch_ThrowsUnauthorizedException()
    {
        // Arrange
        var ids = new[] { new TransactionId(Guid.NewGuid()) }.AsReadOnly();

        // Command with different UserId than context
        var command = new BulkDeleteTransactionsCommand(_differentUserId, ids);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _transactionRepoMock.Verify(
            x => x.DeleteTransactionsByIdsAsync(It.IsAny<UserId>(), It.IsAny<IEnumerable<TransactionId>>()),
            Times.Never);
    }

    /// <summary>
    /// T018: BulkDeleteTransactionsHandler_MaxResultsExceeded_ThrowsDomainException
    /// Verifies >1000 transaction rejection at application layer.
    /// </summary>
    [Fact]
    public async Task Handle_MoreThan1000Ids_ReturnsErrorResult()
    {
        // Arrange
        var ids = Enumerable.Range(0, 1001)
            .Select(_ => new TransactionId(Guid.NewGuid()))
            .ToList()
            .AsReadOnly();

        var command = new BulkDeleteTransactionsCommand(_userId, ids);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Count);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("1000", result.ErrorMessage);

        _transactionRepoMock.Verify(
            x => x.DeleteTransactionsByIdsAsync(It.IsAny<UserId>(), It.IsAny<IEnumerable<TransactionId>>()),
            Times.Never);
    }

    /// <summary>
    /// T019: BulkDeleteTransactionsHandler_NetworkTimeout_RetriesThreeTimes
    /// Verifies auto-retry on transient network errors (up to 3 attempts).
    /// </summary>
    [Fact]
    public async Task Handle_NetworkTimeout_RetriesThreeTimes()
    {
        // Arrange
        var ids = new[] { new TransactionId(Guid.NewGuid()) }.AsReadOnly();

        int callCount = 0;
        _transactionRepoMock
            .Setup(x => x.DeleteTransactionsByIdsAsync(_userId, ids))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    // Simulate network timeout on first two calls
                    throw new HttpRequestException("Connection timeout");
                }
                // Succeed on third attempt
                return Task.FromResult(1);
            });

        var command = new BulkDeleteTransactionsCommand(_userId, ids);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(3, callCount); // Verify exactly 3 attempts
    }

    /// <summary>
    /// T020: BulkDeleteTransactionsHandler_PersistentNetworkError_ThrowsAfterThreeAttempts
    /// Verifies that after 3 failed retries, handler returns error (not throws).
    /// Manual retry button enabled on frontend.
    /// </summary>
    [Fact]
    public async Task Handle_PersistentNetworkError_ReturnsErrorAfterThreeAttempts()
    {
        // Arrange
        var ids = new[] { new TransactionId(Guid.NewGuid()) }.AsReadOnly();

        _transactionRepoMock
            .Setup(x => x.DeleteTransactionsByIdsAsync(_userId, ids))
            .Throws(new HttpRequestException("Connection timeout"));

        var command = new BulkDeleteTransactionsCommand(_userId, ids);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Count);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("network", result.ErrorMessage.ToLower());

        // Verify 3 attempts were made
        _transactionRepoMock.Verify(
            x => x.DeleteTransactionsByIdsAsync(_userId, ids),
            Times.Exactly(3));
    }

    /// <summary>
    /// T021: BulkDeleteTransactionsHandler_PartialFailure_RollsBackAll
    /// Verifies atomic semantics: constraint violation causes rollback of all 10 transactions.
    /// </summary>
    [Fact]
    public async Task Handle_PartialFailureDueToConstraint_RollsBackAll()
    {
        // Arrange: 10 transactions, 1 has active budget (constraint violation during delete)
        var ids = Enumerable.Range(0, 10)
            .Select(_ => new TransactionId(Guid.NewGuid()))
            .ToList()
            .AsReadOnly();

        _transactionRepoMock
            .Setup(x => x.DeleteTransactionsByIdsAsync(_userId, ids))
            .Throws(new InvalidOperationException("Cannot delete: transaction has active budget constraint."));

        var command = new BulkDeleteTransactionsCommand(_userId, ids);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Count); // All rolled back (0 deleted)
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("constraint", result.ErrorMessage.ToLower());
    }

    /// <summary>
    /// T022: BulkDeleteTransactionsHandler_CrossUserAttempt_FailsWithForbidden
    /// Verifies cross-tenant abuse prevention: user tries to delete another user's transactions.
    /// </summary>
    [Fact]
    public async Task Handle_CrossUserDeleteAttempt_ThrowsForbidden()
    {
        // Arrange: User A tries to delete User B's transactions
        var ids = new[] { new TransactionId(Guid.NewGuid()) }.AsReadOnly();

        var command = new BulkDeleteTransactionsCommand(
            _differentUserId, // Different user than context
            ids);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));

        // Verify repository was never called (security gate at handler level)
        _transactionRepoMock.Verify(
            x => x.DeleteTransactionsByIdsAsync(It.IsAny<UserId>(), It.IsAny<IEnumerable<TransactionId>>()),
            Times.Never);
    }

    /// <summary>
    /// T023: BulkDeleteTransactionsHandler_EmptySelection_ReturnsZero
    /// Verifies behavior with empty transaction list (edge case).
    /// </summary>
    [Fact]
    public async Task Handle_EmptySelection_ReturnsErrorResult()
    {
        // Arrange
        var ids = new List<TransactionId>().AsReadOnly();

        var command = new BulkDeleteTransactionsCommand(_userId, ids);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Count);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("At least one", result.ErrorMessage);
    }

    /// <summary>
    /// T024: BulkDeleteTransactionsHandler_ErrorMessage_IsUserFriendly
    /// Verifies error messages are clear and helpful (not technical jargon).
    /// </summary>
    [Fact]
    public async Task Handle_ConstraintViolation_ReturnsUserFriendlyErrorMessage()
    {
        // Arrange
        var ids = new[] { new TransactionId(Guid.NewGuid()) }.AsReadOnly();

        _transactionRepoMock
            .Setup(x => x.DeleteTransactionsByIdsAsync(_userId, ids))
            .Throws(new InvalidOperationException("Foreign key constraint violated"));

        var command = new BulkDeleteTransactionsCommand(_userId, ids);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result.ErrorMessage);
        // Error should be translated to user-friendly message (not "Foreign key constraint...")
        Assert.DoesNotContain("Foreign key", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
