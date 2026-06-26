namespace SauronSheet.Application.Features.Analytics.Classification;

using System.Collections.Generic;
using DTOs;
using Domain.Entities;
using Domain.ValueObjects;

/// <summary>
/// Pure classification engine for the annual fixed vs variable analysis.
/// </summary>
public interface IAnnualClassificationEngine
{
    IReadOnlyList<AnnualAnalysisRowDto> Classify(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<SubcategoryId, string> subcategoryNames,
        int year);
}
