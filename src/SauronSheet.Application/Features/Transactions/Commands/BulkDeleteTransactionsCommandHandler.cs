using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Features.Transactions.Commands;

/// <summary>
/// Handler for BulkDeleteTransactionsCommand.
/// Orchestrates atomic deletion of multiple transactions with multi-tenant safety.
/// Implements retry logic for transient network errors (max 3 attempts, 1s linear backoff).
/// </summary>
public class BulkDeleteTransactionsCommandHandler : IRequestHandler<BulkDeleteTransactionsCommand, BulkDeleteResultDto>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserContext _userContext;
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 1000; // 1 second linear backoff

    public BulkDeleteTransactionsCommandHandler(
        ITransactionRepository transactionRepository,
        IUserContext userContext)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Deletes multiple transactions atomically.
    /// Validates multi-tenant isolation, retries on transient errors, returns user-friendly results.
    /// </summary>
    public async Task<BulkDeleteResultDto> Handle(
        BulkDeleteTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        // Step 1: Validate multi-tenant isolation (UserId must match authenticated user)
        var contextUserId = new UserId(_userContext.UserId);
        if (contextUserId != request.UserId)
        {
            throw new UnauthorizedAccessException(
                "Cannot delete transactions for a different user. Multi-tenant isolation violation.");
        }

        // Step 2: Create specification for filtering transactions
        // This will also validate UserId, IDs count, MaxResults, etc.
        TransactionByIdSpecification spec;
        try
        {
            spec = new TransactionByIdSpecification(request.UserId, request.TransactionIds);
        }
        catch (DomainException ex)
        {
            return new BulkDeleteResultDto(
                Count: 0,
                ErrorMessage: ex.Message,
                FailedTransactionIds: null);
        }

        // Step 3: Attempt deletion with retry logic
        int deletedCount = 0;
        string? errorMessage = null;

        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                deletedCount = await _transactionRepository.DeleteTransactionsByIdsAsync(
                    request.UserId,
                    request.TransactionIds);
                
                // Success - exit retry loop
                break;
            }
            catch (HttpRequestException) when (attempt < MaxRetryAttempts - 1)
            {
                // Transient network error (timeout, unavailable)
                // Retry after delay
                await Task.Delay(RetryDelayMs, cancellationToken);
                continue;
            }
            catch (HttpRequestException) when (attempt == MaxRetryAttempts - 1)
            {
                // Final attempt failed - return error for manual retry
                errorMessage = "Network error: Could not connect to server. Please check your internet connection and try again.";
                break;
            }
            catch (InvalidOperationException ex)
            {
                // Business logic error (constraint violation, etc.)
                // These should NOT be retried
                errorMessage = TranslateBusinessError(ex.Message);
                break;
            }
            catch (Exception ex)
            {
                // Unexpected error
                errorMessage = $"Unexpected error during deletion: {ex.Message}";
                break;
            }
        }

        // Step 4: Return result
        return new BulkDeleteResultDto(
            Count: deletedCount,
            ErrorMessage: errorMessage,
            FailedTransactionIds: null);
    }

    /// <summary>
    /// Translates database/business errors to user-friendly messages.
    /// </summary>
    private static string TranslateBusinessError(string technicalError)
    {
        // Map technical errors to user-friendly messages
        if (technicalError.Contains("constraint", StringComparison.OrdinalIgnoreCase) ||
            technicalError.Contains("Foreign key", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot delete: One or more transactions have active constraints (e.g., budget associations). Please review and try again.";
        }

        if (technicalError.Contains("permission", StringComparison.OrdinalIgnoreCase))
        {
            return "You do not have permission to delete these transactions.";
        }

        // Generic fallback
        return "Deletion failed: Please try again or contact support if the problem persists.";
    }
}
