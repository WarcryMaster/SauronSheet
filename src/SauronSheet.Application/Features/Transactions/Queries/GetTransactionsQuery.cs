namespace SauronSheet.Application.Features.Transactions.Queries;

using DTOs;
using MediatR;

public record GetTransactionsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    Guid? CategoryId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<PaginatedResultDto<TransactionDto>>;
