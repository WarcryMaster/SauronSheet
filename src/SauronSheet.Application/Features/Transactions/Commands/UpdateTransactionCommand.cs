namespace SauronSheet.Application.Features.Transactions.Commands;

using System;
using MediatR;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Command to update an existing transaction's mutable fields.
/// </summary>
public record UpdateTransactionCommand(
    TransactionId TransactionId,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    CategoryId? CategoryId,
    SubcategoryId? SubcategoryId) : IRequest<Unit>;
