namespace JameJafari.Core.DTOs;

/// <summary>Compact person info for lists and parent columns (same entity as Person).</summary>
public record PersonSummaryDto(
    int Id,
    string DisplayName,
    string? NickName,
    string? PicturePath,
    bool IsDead);
