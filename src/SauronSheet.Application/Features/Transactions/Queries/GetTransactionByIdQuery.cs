namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using DTOs;
using MediatR;

/// <summary>
/// Query to fetch a single transaction by its ID.
/// Returns an enriched TransactionDto with resolved category and subcategory names.
/// Tenant-scoped: only returns transactions belonging to the current user.
/// </summary>
public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>;
