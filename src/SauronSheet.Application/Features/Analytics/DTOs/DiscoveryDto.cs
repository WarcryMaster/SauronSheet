namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Discovery DTO (REQ-013).
/// </summary>
public record DiscoveryDto(
    string Icon,
    string Title,
    string Description,
    string Category);
