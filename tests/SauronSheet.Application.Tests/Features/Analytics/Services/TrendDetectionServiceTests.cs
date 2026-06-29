namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;

[Trait("Category", "Application")]
public class TrendDetectionServiceTests
{
    [Fact]
    public void Compute_WhenChangeAboveTenPercent_ClassifiesGrowing()
    {
        IReadOnlyList<TrendDto> trends = TrendDetectionService.Compute(new Dictionary<string, decimal?>
        {
            ["Food"] = 15m
        });

        Assert.Single(trends);
        Assert.Equal("growing", trends[0].Direction);
    }

    [Fact]
    public void Compute_WhenChangeBetweenMinusTenAndTen_ClassifiesStable()
    {
        IReadOnlyList<TrendDto> trends = TrendDetectionService.Compute(new Dictionary<string, decimal?>
        {
            ["Transport"] = 5m
        });

        Assert.Single(trends);
        Assert.Equal("stable", trends[0].Direction);
    }

    [Fact]
    public void Compute_WhenChangeBelowMinusTen_ClassifiesDeclining()
    {
        IReadOnlyList<TrendDto> trends = TrendDetectionService.Compute(new Dictionary<string, decimal?>
        {
            ["Restaurants"] = -20m
        });

        Assert.Single(trends);
        Assert.Equal("declining", trends[0].Direction);
    }

    [Fact]
    public void Compute_WhenNoYoYData_ClassifiesInsufficient()
    {
        IReadOnlyList<TrendDto> trends = TrendDetectionService.Compute(new Dictionary<string, decimal?>
        {
            ["New Category"] = null
        });

        Assert.Single(trends);
        Assert.Equal("insufficient", trends[0].Direction);
    }
}
