namespace SauronSheet.Application.Features.Categories.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class DeleteCategoryCommandHandler
    : IRequestHandler<DeleteCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly CategoryService _categoryService;
    private readonly IUserContext _userContext;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        CategoryService categoryService,
        IUserContext userContext)
    {
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _categoryService = categoryService;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var categoryId = new CategoryId(request.CategoryId);

        var category = await _categoryRepo.GetByIdAsync(categoryId);
        if (category == null)
            throw new EntityNotFoundException("Category", request.CategoryId);

        // Tenant scoping
        if (category.UserId != userId)
            throw new EntityNotFoundException("Category", request.CategoryId);

        // Check if has transactions via transaction repository (SRP: category repo must not query transactions)
        bool hasTransactions = await _transactionRepo.HasTransactionsForCategoryAsync(categoryId);

        // Validate can delete via domain service
        if (!_categoryService.CanDeleteCategory(category, hasTransactions))
            throw new DomainException(
                hasTransactions
                    ? "Cannot delete category with active transactions."
                    : "Cannot delete a system default category.");

        await _categoryRepo.DeleteAsync(categoryId);
        return Unit.Value;
    }
}
