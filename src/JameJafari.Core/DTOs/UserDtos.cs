namespace JameJafari.Core.DTOs;

public record UserDto(
    int Id,
    string Username,
    string? Email,
    string? Mobile,
    string? AvatarPath,
    bool IsActive,
    IReadOnlyList<string> Roles,
    AuditInfoDto Audit);

public record CreateUserRequest(
    string Username,
    string Password,
    string? Email,
    string? Mobile,
    bool IsActive,
    IReadOnlyList<int> RoleIds);

public record UpdateUserRequest(
    string? Email,
    string? Mobile,
    bool IsActive,
    IReadOnlyList<int> RoleIds,
    string? NewPassword);
