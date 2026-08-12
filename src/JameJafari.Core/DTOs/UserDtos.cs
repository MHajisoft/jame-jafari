using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Validation;

namespace JameJafari.Core.DTOs;

public record UserDto(
    int Id,
    string Username,
    string? Email,
    string? Mobile,
    string? AvatarPath,
    bool IsActive,
    bool IsSystemAdmin,
    IReadOnlyList<string> Permissions,
    AuditInfoDto Audit);

public record CreateUserRequest(
    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [StringLength(100, ErrorMessage = "نام کاربری حداکثر ۱۰۰ کاراکتر")]
    string Username,

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [PasswordStrength]
    string Password,

    [EmailAddress(ErrorMessage = "فرمت ایمیل نامعتبر است")]
    [StringLength(200, ErrorMessage = "ایمیل حداکثر ۲۰۰ کاراکتر")]
    string? Email,

    [StringLength(20, ErrorMessage = "موبایل حداکثر ۲۰ کاراکتر")]
    string? Mobile,

    bool IsActive,

    IReadOnlyList<int> PermissionIds);

public record ChangeUserPasswordRequest(
    [Required(ErrorMessage = "رمز عبور جدید الزامی است")]
    [PasswordStrength]
    string NewPassword);

public record UpdateUserRequest(
    [EmailAddress(ErrorMessage = "فرمت ایمیل نامعتبر است")]
    [StringLength(200, ErrorMessage = "ایمیل حداکثر ۲۰۰ کاراکتر")]
    string? Email,

    [StringLength(20, ErrorMessage = "موبایل حداکثر ۲۰ کاراکتر")]
    string? Mobile,

    bool IsActive,

    IReadOnlyList<int> PermissionIds);
