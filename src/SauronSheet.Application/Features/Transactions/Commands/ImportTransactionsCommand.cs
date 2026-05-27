namespace SauronSheet.Application.Features.Transactions.Commands;

using DTOs;
using MediatR;

/// <summary>
/// Command to import bank transactions from an Excel statement file (.xls or .xlsx).
/// Neutral replacement for <c>ImportTransactionsFromPdfCommand</c>.
/// </summary>
public record ImportTransactionsCommand(
    Stream FileStream,
    string Filename) : IRequest<ImportResultDto>;
