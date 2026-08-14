using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public record GeneralTypeDto(
    int Id,
    string Name,
    string? Code,
    string Category,
    int SortOrder,
    bool IsActive,
    AuditInfoDto Audit);

public record CreateGeneralTypeRequest(
    [Required(ErrorMessage = "نام الزامی است")]
    [StringLength(100, ErrorMessage = "نام حداکثر ۱۰۰ کاراکتر")]
    string Name,

    [StringLength(50, ErrorMessage = "کد حداکثر ۵۰ کاراکتر")]
    string? Code,

    string Category,

    int SortOrder,

    bool IsActive);

public record UpdateGeneralTypeRequest(
    [Required(ErrorMessage = "نام الزامی است")]
    [StringLength(100, ErrorMessage = "نام حداکثر ۱۰۰ کاراکتر")]
    string Name,

    [StringLength(50, ErrorMessage = "کد حداکثر ۵۰ کاراکتر")]
    string? Code,

    int SortOrder,

    bool IsActive);
