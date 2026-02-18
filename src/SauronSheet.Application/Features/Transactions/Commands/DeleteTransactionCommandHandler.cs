namespace SauronSheet.Application.Features.Transactions.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class DeleteTransactionCommandHandler
    : IRequestHandler<DeleteTransactionCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public DeleteTransactionCommandHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        DeleteTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var transactionId = new TransactionId(request.TransactionId);

        var transaction = await _transactionRepo.GetByIdAsync(transactionId);
        if (transaction == null)
            throw new EntityNotFoundException("Transaction", request.TransactionId);

        // Tenant scoping: verify UserId matches
        if (transaction.UserId != userId)
            throw new EntityNotFoundException("Transaction", request.TransactionId);

        await _transactionRepo.DeleteAsync(transactionId);
        return Unit.Value;
    }
}
