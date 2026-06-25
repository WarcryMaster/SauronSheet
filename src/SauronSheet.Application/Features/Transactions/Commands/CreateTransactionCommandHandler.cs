namespace SauronSheet.Application.Features.Transactions.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class CreateTransactionCommandHandler
    : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<Guid> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var transactionId = new TransactionId(Guid.NewGuid());
        var money = new Money(request.Amount, request.Currency);

        // TZ-FIX: Normalize to UTC so TIMESTAMPTZ stores it correctly.
        // Form inputs produce Unspecified Kind, which would be interpreted
        // as local time by PostgreSQL.
        var normalizedDate = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);

        CategoryId? categoryId = null;
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId.Value));
            if (category == null)
                throw new EntityNotFoundException("Category", request.CategoryId.Value);

            // Feature 3: Safe null-checking for nullable UserId
            // System categories (NULL user_id) are accessible to all users
            if (!category.IsAccessibleToUser(userId))
                throw new EntityNotFoundException("Category", request.CategoryId.Value); // Tenant isolation

            categoryId = new CategoryId(request.CategoryId.Value);
        }

        var transaction = new Transaction(
            transactionId,
            userId,
            money,
            normalizedDate,
            request.Description,
            categoryId);

        await _transactionRepo.AddAsync(transaction);
        return transactionId.Value;
    }
}
