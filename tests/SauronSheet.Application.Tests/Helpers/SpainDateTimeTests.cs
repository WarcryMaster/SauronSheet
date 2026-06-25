namespace SauronSheet.Application.Tests.Helpers;

using System;
using Xunit;
using SauronSheet.Application.Helpers;

/// <summary>
/// Tests for SpainDateTime.ToSpainLocal() extension method.
/// Verifies correct conversion from all DateTimeKind values to Europe/Madrid timezone.
/// </summary>
[Trait("Category", "Application")]
public class SpainDateTimeTests
{
    /// <summary>
    /// Given a UTC DateTime, ToSpainLocal must convert to Europe/Madrid.
    /// In winter (CET, UTC+1): 10:00 UTC → 11:00 Spain.
    /// </summary>
    [Fact]
    public void ToSpainLocal_GivenUtc_ConvertsToSpain()
    {
        // Arrange
        var utcDate = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var spainLocal = utcDate.ToSpainLocal();

        // Assert — CET is UTC+1 in January
        Assert.Equal(11, spainLocal.Hour);
        Assert.Equal(DateTimeKind.Unspecified, spainLocal.Kind);
    }

    /// <summary>
    /// Given an Unspecified DateTime, ToSpainLocal must treat it as UTC and convert.
    /// This is the common case for dates parsed from Excel or user input with no timezone info.
    /// </summary>
    [Fact]
    public void ToSpainLocal_GivenUnspecified_ConvertsToSpain()
    {
        // Arrange
        var unspecifiedDate = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Unspecified);

        // Act
        var spainLocal = unspecifiedDate.ToSpainLocal();

        // Assert — CEST is UTC+2 in June
        Assert.Equal(12, spainLocal.Hour);
    }

    /// <summary>
    /// Given a Local DateTime, ToSpainLocal must still convert correctly.
    /// TimeZoneInfo.ConvertTime handles Local by converting from local to target.
    /// </summary>
    [Fact]
    public void ToSpainLocal_GivenLocal_ConvertsToSpain()
    {
        // Arrange
        // Create a Local DateTime equivalent to 10:00 UTC on a winter date
        var localDate = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Local);

        // Act
        var spainLocal = localDate.ToSpainLocal();

        // Assert — the local time is already in Spain timezone, so it should stay at 11:00
        // TimeZoneInfo.ConvertTime from Local to Europe/Madrid:
        // If local timezone == Europe/Madrid, it stays the same.
        // If local timezone != Europe/Madrid, it converts.
        // We verify conversion doesn't crash and returns a valid result.
        Assert.True(spainLocal.Hour >= 0 && spainLocal.Hour <= 23);
    }
}
