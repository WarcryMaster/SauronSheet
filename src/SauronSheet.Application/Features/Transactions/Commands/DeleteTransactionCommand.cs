namespace SauronSheet.Application.Features.Transactions.Commands;

using MediatR;

public record DeleteTransactionCommand(
    Guid TransactionId) : IRequest<Unit>;
