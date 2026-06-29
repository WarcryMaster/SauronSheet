namespace SauronSheet.Application.Features.Analytics.Services;

using System.Collections.Generic;
using System.Linq;
using DTOs;

/// <summary>
/// Pure trend classification service (REQ-015).
/// </summary>
public static class TrendDetectionService
{
    public static IReadOnlyList<TrendDto> Compute(IReadOnlyDictionary<string, decimal?> categoryYoYChangePct)
    {
        List<TrendDto> trends = new();

        foreach (KeyValuePair<string, decimal?> entry in categoryYoYChangePct.OrderBy(x => x.Key))
        {
            string direction;
            string icon;

            if (!entry.Value.HasValue)
            {
                direction = "insufficient";
                icon = "•";
            }
            else if (entry.Value.Value > 10m)
            {
                direction = "growing";
                icon = "↑";
            }
            else if (entry.Value.Value < -10m)
            {
                direction = "declining";
                icon = "↓";
            }
            else
            {
                direction = "stable";
                icon = "→";
            }

            trends.Add(new TrendDto(
                Category: entry.Key,
                Direction: direction,
                ChangePercentage: entry.Value,
                Icon: icon));
        }

        return trends.AsReadOnly();
    }
}
