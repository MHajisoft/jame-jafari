namespace JameJafari.Core.DTOs;

/// <summary>Compact person info for lists and parent columns (same entity as Person).</summary>
public class PersonSummaryResponse
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = "";
    public string? NickName { get; init; }
    public string? PicturePath { get; init; }
    public bool IsDead { get; init; }
}
