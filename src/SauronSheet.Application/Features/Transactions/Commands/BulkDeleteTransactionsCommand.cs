using MediatR;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Features.Transactions.Commands;

/// <summary>
/// Command to delete multiple transactions atomically.
/// Implements CQRS command pattern for state-changing operations.
/// </summary>
public record BulkDeleteTransactionsCommand(
    UserId UserId,
    IReadOnlyList<TransactionId> TransactionIds)
    : IRequest<BulkDeleteResultDto>;
