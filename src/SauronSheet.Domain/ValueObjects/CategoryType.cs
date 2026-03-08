namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Category type enumeration: Income or Expense.
/// Used to classify and separate cash flows.
/// </summary>
public enum CategoryType
{
    /// <summary>
    /// Money coming in (salary, bonuses, investments, etc.)
    /// </summary>
    Income = 0,

    /// <summary>
    /// Money going out (groceries, utilities, entertainment, etc.)
    /// </summary>
    Expense = 1
}
