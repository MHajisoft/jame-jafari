using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public class GeneralTypeResponse : ResponseBase
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Code { get; init; }
    public string Category { get; init; } = "";
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

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
