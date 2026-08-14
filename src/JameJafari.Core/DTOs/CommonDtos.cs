namespace JameJafari.Core.DTOs;

public record AuditInfoDto(
    DateTime CreatedAt,
    string? CreatedBy,
    string? CreatedByAvatarPath,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    string? UpdatedByAvatarPath);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
