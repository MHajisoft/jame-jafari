using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public class CostTypeResponse : ResponseBase
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsIngredient { get; init; }
    public int? UnitId { get; init; }
    public string? UnitName { get; init; }
    public bool IsActive { get; init; }
}

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
