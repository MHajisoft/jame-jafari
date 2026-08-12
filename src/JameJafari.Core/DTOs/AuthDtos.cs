using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Validation;

namespace JameJafari.Core.DTOs;

public record LoginRequest(
    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [StringLength(100, ErrorMessage = "نام کاربری حداکثر ۱۰۰ کاراکتر")]
    string Username,

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [StringLength(100, ErrorMessage = "رمز عبور حداکثر ۱۰۰ کاراکتر")]
    string Password);

public record LoginResponse(
    string Token,
    int Id,
    string Username,
    string? Email,
    string? Mobile,
    string? AvatarPath,
    IReadOnlyList<string> Permissions);

public record ChangePasswordRequest(
    [Required(ErrorMessage = "رمز عبور فعلی الزامی است")]
    string CurrentPassword,

    [Required(ErrorMessage = "رمز عبور جدید الزامی است")]
    [PasswordStrength]
    string NewPassword);

public record ProfileDto(
    int Id,
    string Username,
    string? Email,
    string? Mobile,
    string? AvatarPath,
    IReadOnlyList<string> Permissions);

public record UpdateProfileRequest(
    [EmailAddress(ErrorMessage = "فرمت ایمیل نامعتبر است")]
    [StringLength(200, ErrorMessage = "ایمیل حداکثر ۲۰۰ کاراکتر")]
    string? Email,

    [StringLength(20, ErrorMessage = "موبایل حداکثر ۲۰ کاراکتر")]
    string? Mobile);
