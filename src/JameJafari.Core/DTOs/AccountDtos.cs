using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public class AccountResponse : ResponseBase
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public record CreateAccountRequest(
    [Required(ErrorMessage = "نام حساب الزامی است")]
    [StringLength(200, ErrorMessage = "نام حساب حداکثر ۲۰۰ کاراکتر")]
    string Name,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    bool IsActive);

public record UpdateAccountRequest(
    [Required(ErrorMessage = "نام حساب الزامی است")]
    [StringLength(200, ErrorMessage = "نام حساب حداکثر ۲۰۰ کاراکتر")]
    string Name,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    bool IsActive);
