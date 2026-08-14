using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;

namespace JameJafari.Infrastructure.Services;

public static class AuditHelper
{
    public static AuditInfoDto ToDto(AuditableEntity entity) =>
        new(
            entity.CreatedAt,
            entity.CreatedBy?.Username,
            entity.CreatedBy?.AvatarPath,
            entity.UpdatedAt,
            entity.UpdatedBy?.Username,
            entity.UpdatedBy?.AvatarPath);
}
