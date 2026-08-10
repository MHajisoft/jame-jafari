using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using JameJafari.Api.Authorization;

namespace JameJafari.Api.Filters;

public class PermissionFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>().FirstOrDefault();
        if (endpoint is null || endpoint.Permissions.Length == 0)
            return Task.CompletedTask;

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        var allowed = endpoint.Permissions.Any(p => user.HasClaim("permission", p));
        if (!allowed)
            context.Result = new ForbidResult();

        return Task.CompletedTask;
    }
}
