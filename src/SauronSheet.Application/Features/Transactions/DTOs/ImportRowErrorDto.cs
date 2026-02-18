namespace SauronSheet.Application.Features.Transactions.DTOs;

public record ImportRowErrorDto(
    int RowNumber,
    string RawData,
    string ErrorMessage);
