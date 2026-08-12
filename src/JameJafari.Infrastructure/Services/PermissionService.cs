using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class PermissionService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync()
    {
        return await cache.GetOrSetAsync(
            CacheKeys.PermissionsAll,
            async _ =>
            {
                var rows = await db.Permissions
                    .AsNoTracking()
                    .OrderBy(p => p.Code)
                    .Select(p => new { p.Id, p.Code, p.Name, p.Description })
                    .ToListAsync();

                return rows
                    .Select(p =>
                    {
                        var dot = p.Code.IndexOf('.');
                        var module = dot > 0 ? p.Code[..dot] : p.Code;
                        return new PermissionDto(p.Id, p.Code, p.Name, p.Description, module);
                    })
                    .ToList();
            },
            options => options.SetDuration(LookupCache.PermissionsDuration));
    }
}
