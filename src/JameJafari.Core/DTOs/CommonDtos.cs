namespace JameJafari.Core.DTOs;

public class AuditInfoResponse
{
    public static AuditInfoResponse Empty { get; } = new();

    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? CreatedByAvatarPath { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public string? UpdatedByAvatarPath { get; init; }
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
