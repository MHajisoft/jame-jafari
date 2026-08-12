using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;

namespace JameJafari.Infrastructure.Services;

public static class AuditHelper
{
    public static AuditInfoDto ToDto(AuditableEntity entity) =>
        new(entity.CreatedAt, entity.CreatedBy?.Username, entity.UpdatedAt, entity.UpdatedBy?.Username);
}
