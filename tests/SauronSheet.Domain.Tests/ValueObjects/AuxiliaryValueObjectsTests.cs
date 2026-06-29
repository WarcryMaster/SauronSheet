using System;
using System.Collections.Generic;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;
using Xunit;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class AuxiliaryValueObjectsTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_DefaultConstructor_SetsMinAndMaxDates()
    {
        // Act
        DateRange dateRange = new DateRange();

        // Assert
        Assert.Equal(DateTime.MinValue, dateRange.StartDate);
        Assert.Equal(DateTime.MaxValue, dateRange.EndDate);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_ValidConstructor_SetsDates()
    {
        // Arrange
        DateTime startDate = new DateTime(2026, 1, 1);
        DateTime endDate = new DateTime(2026, 12, 31);

        // Act
        DateRange dateRange = new DateRange(startDate, endDate);

        // Assert
        Assert.Equal(startDate, dateRange.StartDate);
        Assert.Equal(endDate, dateRange.EndDate);
        Assert.Equal("2026-01-01 to 2026-12-31", dateRange.ToString());
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DateRange_EndBeforeStart_ThrowsArgumentException()
    {
        // Arrange
        DateTime startDate = new DateTime(2026, 2, 1);
        DateTime endDate = new DateTime(2026, 1, 31);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DateRange(startDate, endDate));

        // Assert
        Assert.Contains("End date cannot be before start date.", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserProfile_ValidData_SetsProperties()
    {
        // Arrange
        UserId userId = new UserId("user-profile-1");
        string email = "test@example.com";
        string displayName = "Test User";
        DateTime createdAt = new DateTime(2026, 6, 1, 8, 30, 0, DateTimeKind.Utc);

        // Act
        UserProfile profile = new UserProfile(userId, email, displayName, createdAt);

        // Assert
        Assert.Equal(userId, profile.Id);
        Assert.Equal(email, profile.Email);
        Assert.Equal(displayName, profile.DisplayName);
        Assert.Equal(createdAt, profile.CreatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserProfile_EmptyEmail_ThrowsDomainException()
    {
        // Arrange
        UserId userId = new UserId("user-profile-2");

        // Act
        DomainException exception = Assert.Throws<DomainException>(() => new UserProfile(userId, "", "Name", DateTime.UtcNow));

        // Assert
        Assert.Contains("Email cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void RawTransactionRow_DefaultCurrency_IsEur()
    {
        // Act
        RawTransactionRow row = new RawTransactionRow(
            RowNumber: 1,
            Date: "2026-06-01",
            Category: "Food",
            SubCategory: "Groceries",
            Description: "Market",
            Comment: null,
            Amount: "-25.50",
            Balance: "1000.00");

        // Assert
        Assert.Equal("EUR", row.Currency);
        Assert.Equal("Food", row.Category);
        Assert.Equal("-25.50", row.Amount);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void StatementParseResult_SetsRowsErrorsAndSkippedCount()
    {
        // Arrange
        RawTransactionRow row = new RawTransactionRow(
            RowNumber: 2,
            Date: "2026-06-02",
            Category: "Salary",
            SubCategory: "Main",
            Description: "Payroll",
            Comment: null,
            Amount: "2500.00",
            Balance: "3500.00",
            Currency: "EUR");
        StatementParseRowError rowError = new StatementParseRowError(
            RowNumber: 7,
            RawContent: "invalid,row,data",
            Reason: "Invalid date");
        IReadOnlyList<RawTransactionRow> rows = new List<RawTransactionRow> { row };
        IReadOnlyList<StatementParseRowError> rowErrors = new List<StatementParseRowError> { rowError };

        // Act
        StatementParseResult result = new StatementParseResult(rows, rowErrors, SkippedCount: 3);

        // Assert
        Assert.Single(result.Rows);
        Assert.Single(result.RowErrors);
        Assert.Equal(3, result.SkippedCount);
        Assert.Equal(7, result.RowErrors[0].RowNumber);
        Assert.Equal("Invalid date", result.RowErrors[0].Reason);
    }
}
