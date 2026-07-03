namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Application.Helpers;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using DTOs;
using MediatR;

/// <summary>
/// Handles GetTransactionByIdQuery: fetches a single transaction, validates tenant ownership,
/// and returns an enriched DTO with resolved category and subcategory names.
/// </summary>
public class GetTransactionByIdQueryHandler
    : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;
    private static readonly Regex SlugInvalidCharsRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled);

    public GetTransactionByIdQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        ISubcategoryRepository subcategoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _subcategoryRepo = subcategoryRepo;
        _userContext = userContext;
    }

    public async Task<TransactionDto> Handle(
        GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        UserId userId = new(_userContext.UserId);
        TransactionId transactionId = new(request.TransactionId);

        Domain.Entities.Transaction? transaction = await _transactionRepo.GetByIdAsync(transactionId);

        // Tenant validation: not found OR belongs to another user
        if (transaction == null || transaction.UserId != userId)
            throw new EntityNotFoundException("Transaction", request.TransactionId);

        // Resolve category name (if assigned)
        string? categoryName = null;
        bool categoryIsSystemDefault = false;
        string? categorySystemSlug = null;
        if (transaction.CategoryId is CategoryId categoryId)
        {
            Domain.Entities.Category? category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category == null)
            {
                IReadOnlyList<Domain.Entities.Category> systemCategories = await _categoryRepo.GetSystemDefaultsAsync() ?? Array.Empty<Domain.Entities.Category>();
                category = systemCategories.FirstOrDefault(c => c.Id == categoryId);
            }

            categoryName = category?.Name.Value;
            categoryIsSystemDefault = category?.IsSystemDefault == true;
            categorySystemSlug = categoryIsSystemDefault && category != null
                ? BuildSystemCategorySlug(category.Name.Value)
                : null;
        }

        // Resolve subcategory name (if assigned)
        string? subcategoryName = null;
        if (transaction.SubcategoryId is SubcategoryId subcategoryId)
        {
            Domain.Entities.Subcategory? subcategory = await _subcategoryRepo.GetByIdAsync(subcategoryId);
            subcategoryName = subcategory?.Name.Value;
        }

        return new TransactionDto(
            transaction.Id.Value,
            transaction.Amount.Amount,
            transaction.Amount.Currency,
            transaction.Date.ToSpainLocal(),
            transaction.Description,
            transaction.CategoryId?.Value,
            categoryName,
            transaction.ImportedFrom,
            transaction.CreatedAt,
            BankCategory: transaction.BankCategory,
            BankSubcategory: transaction.BankSubcategory,
            SubcategoryId: transaction.SubcategoryId?.Value.ToString(),
            SubcategoryName: subcategoryName,
            CategorySource: transaction.CategorySource.ToString(),
            CategoryIsSystemDefault: categoryIsSystemDefault,
            CategorySystemSlug: categorySystemSlug);
    }

    private static string BuildSystemCategorySlug(string categoryName)
    {
        string normalized = NormalizeForSlug(categoryName);
        string collapsed = SlugInvalidCharsRegex.Replace(normalized, "-").Trim('-');

        return string.IsNullOrWhiteSpace(collapsed)
            ? "unknown"
            : collapsed;
    }

    private static string NormalizeForSlug(string value)
    {
        string formD = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(formD.Length);

        foreach (char character in formD)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
