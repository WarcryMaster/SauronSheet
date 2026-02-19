namespace SauronSheet.Application.Features.Transactions.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get the N most recent transactions for the current user.
/// Phase 4 (US5): Dashboard recent transactions list.
/// </summary>
public record GetRecentTransactionsQuery(int Count = 10) : IRequest<List<TransactionDto>>;
