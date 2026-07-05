using Microsoft.AspNetCore.Authorization;

namespace JameJafari.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute(string permission) : AuthorizeAttribute
{
    public string Permission { get; } = permission;

    public RequirePermissionAttribute() : this(string.Empty) { }
}
