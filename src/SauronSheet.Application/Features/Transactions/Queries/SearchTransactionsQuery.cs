namespace SauronSheet.Application.Features.Transactions.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query for multi-filter transaction search with pagination.
/// Phase 4 (US5): Search page with keyword, date, category, amount filters.
/// </summary>
public record SearchTransactionsQuery(
    string? Keyword = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? CategoryId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PaginatedResultDto<TransactionDto>>;
