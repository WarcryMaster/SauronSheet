namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Defines the time granularity for a budget policy.
/// Monthly: budget limit applies per calendar month.
/// Quarterly: budget limit applies per calendar quarter (Q1, Q2, Q3, Q4).
/// Semester: budget limit applies per calendar half (H1, H2).
/// Annual: budget limit applies per calendar year.
/// </summary>
public enum BudgetPeriod
{
    Monthly,
    Quarterly,
    Semester,
    Annual
}
