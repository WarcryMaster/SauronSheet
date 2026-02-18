namespace SauronSheet.Application.Features.Transactions.DTOs;

public record PaginatedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
