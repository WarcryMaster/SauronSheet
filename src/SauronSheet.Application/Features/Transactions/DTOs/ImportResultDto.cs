namespace SauronSheet.Application.Features.Transactions.DTOs;

public record ImportResultDto(
    int ImportedCount,
    int SkippedCount,
    int TotalProcessed,
    string Filename,
    DateTime ImportedAt,
    List<ImportRowErrorDto> Errors);
