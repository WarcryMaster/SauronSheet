namespace SauronSheet.Application.Features.Transactions.Queries;

using System.Collections.Generic;
using MediatR;

/// <summary>
/// Query to retrieve distinct ImportedFrom source values for the current user.
/// Used to populate the searchable datalist on the /transactions page.
/// </summary>
public record GetDistinctImportedSourcesQuery()
    : IRequest<List<string>>;
