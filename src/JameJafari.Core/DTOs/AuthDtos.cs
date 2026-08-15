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

public class LoginResponse
{
    public string Token { get; init; } = "";
    public int Id { get; init; }
    public string Username { get; init; } = "";
    public string? Email { get; init; }
    public string? Mobile { get; init; }
    public string? AvatarPath { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public record ChangePasswordRequest(
    [Required(ErrorMessage = "رمز عبور فعلی الزامی است")]
    string CurrentPassword,

    [Required(ErrorMessage = "رمز عبور جدید الزامی است")]
    [PasswordStrength]
    string NewPassword);

public class ProfileResponse
{
    public int Id { get; init; }
    public string Username { get; init; } = "";
    public string? Email { get; init; }
    public string? Mobile { get; init; }
    public string? AvatarPath { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public record UpdateProfileRequest(
    [EmailAddress(ErrorMessage = "فرمت ایمیل نامعتبر است")]
    [StringLength(200, ErrorMessage = "ایمیل حداکثر ۲۰۰ کاراکتر")]
    string? Email,

    [StringLength(20, ErrorMessage = "موبایل حداکثر ۲۰ کاراکتر")]
    string? Mobile);
