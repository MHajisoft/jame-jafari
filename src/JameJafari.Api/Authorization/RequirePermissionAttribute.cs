using Microsoft.AspNetCore.Authorization;

namespace JameJafari.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public string[] Permissions { get; }

    public string Permission => Permissions.Length > 0 ? Permissions[0] : string.Empty;

    public RequirePermissionAttribute(params string[] permissions)
    {
        Permissions = permissions ?? [];
    }

    public RequirePermissionAttribute() : this([]) { }
}
