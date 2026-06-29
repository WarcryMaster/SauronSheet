namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Achievement DTO (REQ-014).
/// </summary>
public record AchievementDto(
    string Id,
    string Title,
    string Description,
    string Icon,
    bool Unlocked);
