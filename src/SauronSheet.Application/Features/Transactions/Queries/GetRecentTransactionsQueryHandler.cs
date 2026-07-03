namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using SauronSheet.Application.Helpers;

/// <summary>
/// Handler for GetRecentTransactionsQuery.
/// Returns the N most recent transactions ordered by date descending.
/// Phase 4 (US5).
/// DT-1b/DT-1c: SubcategoryName populated via single batch fetch (no N+1).
/// </summary>
public class GetRecentTransactionsQueryHandler
    : IRequestHandler<GetRecentTransactionsQuery, List<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;
    private static readonly Regex SlugInvalidCharsRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled);

    public GetRecentTransactionsQueryHandler(
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

    public async Task<List<TransactionDto>> Handle(
        GetRecentTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var userSpec = new TransactionByUserSpecification(userId);
        var allTransactions = await _transactionRepo.FindBySpecificationAsync(userSpec);

        // Load categories for name lookup
        IReadOnlyList<Domain.Entities.Category> userCategories = await _categoryRepo.GetByUserIdAsync(userId) ?? Array.Empty<Domain.Entities.Category>();
        IReadOnlyList<Domain.Entities.Category> systemCategories = await _categoryRepo.GetSystemDefaultsAsync() ?? Array.Empty<Domain.Entities.Category>();
        Dictionary<CategoryId, Domain.Entities.Category> categoryLookup = userCategories
            .Concat(systemCategories)
            .GroupBy(c => c.Id)
            .ToDictionary(g => g.Key, g => g.First());

        // DT-1b: batch-fetch subcategories once; build in-memory dict to avoid N+1.
        // TryGetValue used at mapping time — null-safe for DT-1c (SubcategoryId == null).
        var subcategories = await _subcategoryRepo.GetByUserIdAsync(userId);
        var subcategoryLookup = subcategories.ToDictionary(s => s.Id, s => s.Name.Value);

        var recent = allTransactions
            .OrderByDescending(t => t.Date)
            .Take(request.Count)
            .Select(t =>
            {
                Domain.Entities.Category? category = null;
                if (t.CategoryId is CategoryId categoryId)
                {
                    categoryLookup.TryGetValue(categoryId, out category);
                }

                return new TransactionDto(
                t.Id.Value,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Date.ToSpainLocal(),
                t.Description,
                t.CategoryId?.Value,
                category?.Name.Value,
                t.ImportedFrom,
                t.CreatedAt,
                BankCategory: t.BankCategory,
                BankSubcategory: t.BankSubcategory,
                SubcategoryId: t.SubcategoryId?.Value.ToString(),
                SubcategoryName: t.SubcategoryId != null && subcategoryLookup.TryGetValue(t.SubcategoryId, out var subName)
                    ? subName
                    : null,
                CategorySource: t.CategorySource.ToString(),
                CategoryIsSystemDefault: category?.IsSystemDefault == true,
                CategorySystemSlug: category?.IsSystemDefault == true ? BuildSystemCategorySlug(category.Name.Value) : null);
            })
            .ToList();

        return recent;
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
