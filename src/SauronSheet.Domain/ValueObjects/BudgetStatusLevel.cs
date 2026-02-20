namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Budget status thresholds for visual indicators.
/// Green: spent &lt; 60% of limit
/// Yellow: spent 60–80% of limit
/// Red: spent 80–100% of limit
/// Overage: spent &gt; 100% of limit
/// </summary>
public enum BudgetStatusLevel
{
    Green,
    Yellow,
    Red,
    Overage
}
