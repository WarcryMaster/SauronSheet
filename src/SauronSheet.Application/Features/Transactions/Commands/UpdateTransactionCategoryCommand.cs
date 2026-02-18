namespace SauronSheet.Application.Features.Transactions.Commands;

using MediatR;

public record UpdateTransactionCategoryCommand(
    Guid TransactionId,
    Guid? CategoryId) : IRequest<Unit>;
