using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public record AccountDto(int Id, string Name, string? Description, bool IsActive, AuditInfoDto Audit);

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
