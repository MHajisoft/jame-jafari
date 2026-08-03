using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public record LoginRequest(
    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [StringLength(100, ErrorMessage = "نام کاربری حداکثر ۱۰۰ کاراکتر")]
    string Username,

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [StringLength(100, ErrorMessage = "رمز عبور حداکثر ۱۰۰ کاراکتر")]
    string Password);

public record LoginResponse(string Token, string Username, IReadOnlyList<string> Permissions);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(4), StringLength(100)] string NewPassword);
