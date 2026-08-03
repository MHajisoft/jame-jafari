using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public record CostTypeDto(
    int Id,
    string Name,
    string? Description,
    bool IsIngredient,
    int? UnitId,
    string? UnitName,
    bool IsActive,
    AuditInfoDto Audit);

public record CreateCostTypeRequest(
    [Required(ErrorMessage = "نام الزامی است")]
    [StringLength(200, ErrorMessage = "نام حداکثر ۲۰۰ کاراکتر")]
    string Name,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    bool IsIngredient,

    int? UnitId,

    bool IsActive);

public record UpdateCostTypeRequest(
    [Required(ErrorMessage = "نام الزامی است")]
    [StringLength(200, ErrorMessage = "نام حداکثر ۲۰۰ کاراکتر")]
    string Name,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    bool IsIngredient,

    int? UnitId,

    bool IsActive);
