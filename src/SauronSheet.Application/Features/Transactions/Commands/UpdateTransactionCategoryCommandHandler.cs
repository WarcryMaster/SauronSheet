namespace SauronSheet.Application.Features.Transactions.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class UpdateTransactionCategoryCommandHandler
    : IRequestHandler<UpdateTransactionCategoryCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public UpdateTransactionCategoryCommandHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        UpdateTransactionCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var transactionId = new TransactionId(request.TransactionId);

        var transaction = await _transactionRepo.GetByIdAsync(transactionId);
        if (transaction == null)
            throw new EntityNotFoundException("Transaction", request.TransactionId);

        // Tenant scoping
        if (transaction.UserId != userId)
            throw new EntityNotFoundException("Transaction", request.TransactionId);

        // Validate category exists if provided
        CategoryId? categoryId = null;
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId.Value));
            if (category == null || category.UserId != userId)
                throw new EntityNotFoundException("Category", request.CategoryId.Value);

            categoryId = new CategoryId(request.CategoryId.Value);
        }

        transaction.Categorize(categoryId);
        await _transactionRepo.UpdateAsync(transaction);

        return Unit.Value;
    }
}
