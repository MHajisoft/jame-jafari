using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;

namespace JameJafari.Infrastructure.Services;

public static class AuditHelper
{
    public static AuditInfoResponse ToDto(AuditableEntity entity) =>
        FromProjection(
            entity.CreatedAt,
            entity.CreatedBy?.Username,
            entity.CreatedBy?.AvatarPath,
            entity.UpdatedAt,
            entity.UpdatedBy?.Username,
            entity.UpdatedBy?.AvatarPath);

    public static AuditInfoResponse FromProjection(
        DateTime createdAt,
        string? createdBy,
        string? createdByAvatarPath,
        DateTime? updatedAt,
        string? updatedBy,
        string? updatedByAvatarPath) => new()
    {
        CreatedAt = createdAt,
        CreatedBy = createdBy,
        CreatedByAvatarPath = createdByAvatarPath,
        UpdatedAt = updatedAt,
        UpdatedBy = updatedBy,
        UpdatedByAvatarPath = updatedByAvatarPath
    };
}
