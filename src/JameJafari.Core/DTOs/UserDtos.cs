namespace JameJafari.Core.DTOs;

public record UserDto(
    int Id,
    string Username,
    string? Email,
    string? Mobile,
    string? AvatarPath,
    bool IsActive,
    IReadOnlyList<string> Permissions,
    AuditInfoDto Audit);

public record CreateUserRequest(
    string Username,
    string Password,
    string? Email,
    string? Mobile,
    bool IsActive,
    IReadOnlyList<int> PermissionIds);

public record UpdateUserRequest(
    string? Email,
    string? Mobile,
    bool IsActive,
    IReadOnlyList<int> PermissionIds,
    string? NewPassword);
