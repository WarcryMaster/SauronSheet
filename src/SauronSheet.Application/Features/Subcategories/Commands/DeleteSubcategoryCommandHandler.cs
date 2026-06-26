namespace SauronSheet.Application.Features.Subcategories.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class DeleteSubcategoryCommandHandler
    : IRequestHandler<DeleteSubcategoryCommand, Unit>
{
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public DeleteSubcategoryCommandHandler(
        ISubcategoryRepository subcategoryRepo,
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _subcategoryRepo = subcategoryRepo;
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        DeleteSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var subcategoryId = new SubcategoryId(request.SubcategoryId);

        var subcategory = await _subcategoryRepo.GetByIdAsync(subcategoryId);
        if (subcategory == null)
            throw new EntityNotFoundException("Subcategory", request.SubcategoryId);

        var category = await _categoryRepo.GetByIdAsync(subcategory.CategoryId);
        if (category == null || category.UserId != userId)
            throw new EntityNotFoundException("Subcategory", request.SubcategoryId);

        bool hasTransactions = await _transactionRepo.HasTransactionsForSubcategoryAsync(subcategoryId);
        if (hasTransactions)
            throw new DomainException("Cannot delete subcategory with active transactions.");

        await _subcategoryRepo.DeleteAsync(subcategoryId);
        return Unit.Value;
    }
}
