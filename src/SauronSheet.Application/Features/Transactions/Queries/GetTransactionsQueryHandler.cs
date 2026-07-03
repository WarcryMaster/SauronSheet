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

public class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, PaginatedResultDto<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;
    private static readonly Regex SlugInvalidCharsRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled);

    public GetTransactionsQueryHandler(
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

    public async Task<PaginatedResultDto<TransactionDto>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Build composed specification from filters
        ISpecification<Domain.Entities.Transaction> spec = new TransactionByUserSpecification(userId);

        if (request.CategoryId.HasValue)
        {
            var categorySpec = new TransactionByCategorySpecification(new CategoryId(request.CategoryId.Value));
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, categorySpec);
        }

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var dateSpec = new TransactionByDateRangeSpecification(request.StartDate.Value, request.EndDate.Value);
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, dateSpec);
        }
        else if (request.StartDate.HasValue)
        {
            var dateSpec = new TransactionByDateRangeSpecification(request.StartDate.Value, DateTime.MaxValue);
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, dateSpec);
        }
        else if (request.EndDate.HasValue)
        {
            var dateSpec = new TransactionByDateRangeSpecification(DateTime.MinValue, request.EndDate.Value);
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, dateSpec);
        }

        if (!string.IsNullOrEmpty(request.ImportedFrom))
        {
            // Support comma-separated multiple sources
            var sources = request.ImportedFrom
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (sources.Length == 1)
            {
                var sourceSpec = new TransactionByImportedFromSpecification(sources[0]);
                spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, sourceSpec);
            }
            else if (sources.Length > 1)
            {
                var sourceSpec = new TransactionByMultipleImportedFromsSpecification(sources);
                spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, sourceSpec);
            }
        }

        var filtered = await _transactionRepo.FindBySpecificationAsync(spec);

        // Sort by date descending
        var sorted = filtered.OrderByDescending(t => t.Date);

        // Get total count
        var totalCount = sorted.Count();

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        var paginated = sorted.Skip(skip).Take(request.PageSize).ToList();

        // DT-1d: batch-fetch categories once; build in-memory dict to avoid N+1.
        // Identical pattern to GetRecentTransactionsQueryHandler (L51-52).
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

        var dtos = paginated.Select(t =>
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
        }).ToList();

        // CLARIFICATION A-4: TotalPages calculation
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PaginatedResultDto<TransactionDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages);
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
