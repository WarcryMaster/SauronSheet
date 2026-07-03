namespace SauronSheet.Application.Features.Categories.Queries;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;
    private static readonly Regex SlugInvalidCharsRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled);

    public GetCategoriesQueryHandler(
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var categories = await _categoryRepo.GetByUserIdAsync(userId);

        var categoryIds = categories.Select(c => c.Id).ToList();
        var counts = await _transactionRepo.GetCountsByCategoriesAsync(categoryIds);

        var result = categories.Select(c => new CategoryDto(
            c.Id.Value,
            c.Name.Value,
            c.Type.ToString(),
            c.Color.Value,
            c.IconName,
            c.CreatedAt,
            c.UpdatedAt ?? DateTime.UtcNow,
            TransactionCount: counts.GetValueOrDefault(c.Id, 0),
            IsAutoCreated: c.IsAutoCreated,
            IsSystemDefault: c.IsSystemDefault,
            SystemSlug: c.IsSystemDefault ? BuildSystemCategorySlug(c.Name.Value) : null
        )).ToList();

        // Sort alphabetically by name (system defaults removed in Chunk 3)
        return result
            .OrderBy(c => c.Name)
            .ToList();
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
