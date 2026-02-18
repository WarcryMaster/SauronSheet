namespace SauronSheet.Application.Features.Transactions.Commands;

using MediatR;

public record CreateTransactionCommand(
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid? CategoryId) : IRequest<Guid>;
