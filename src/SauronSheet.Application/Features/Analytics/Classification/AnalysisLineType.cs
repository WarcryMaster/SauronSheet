namespace SauronSheet.Application.Features.Analytics.Classification;

using System;

/// <summary>
/// Classification line for the annual fixed vs variable analysis.
/// </summary>
public enum AnalysisLineType
{
    ExpenseFixed,
    ExpenseVariable,
    IncomeFixed,
    IncomeVariable
}

/// <summary>
/// Spanish display labels for <see cref="AnalysisLineType"/>.
/// </summary>
public static class AnalysisLineTypeExtensions
{
    public static string GetTypeLabel(this AnalysisLineType lineType)
    {
        return lineType switch
        {
            AnalysisLineType.ExpenseFixed => "Gasto Fijo",
            AnalysisLineType.ExpenseVariable => "Gasto Variable",
            AnalysisLineType.IncomeFixed => "Ingreso Fijo",
            AnalysisLineType.IncomeVariable => "Ingreso Variable",
            _ => throw new ArgumentOutOfRangeException(nameof(lineType))
        };
    }
}
