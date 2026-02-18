namespace SauronSheet.Application.Features.Transactions.Commands;

using DTOs;
using MediatR;

public record ImportTransactionsFromPdfCommand(
    Stream PdfStream,
    string Filename) : IRequest<ImportResultDto>;
