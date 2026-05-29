namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Budget status thresholds for visual indicators.
/// Green: spent &lt; 75% of limit
/// Yellow: spent 75%–&lt;100% of limit
/// Red: spent exactly 100% of limit
/// Overage: spent &gt; 100% of limit
/// </summary>
public enum BudgetStatusLevel
{
    Green,
    Yellow,
    Red,
    Overage
}
