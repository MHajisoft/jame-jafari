using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public class PersonGroupResponse : ResponseBase
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<int> PersonIds { get; init; } = [];
    public int MemberCount { get; init; }
}

public record CreatePersonGroupRequest(
    [Required(ErrorMessage = "نام گروه الزامی است")]
    [StringLength(200, ErrorMessage = "نام گروه حداکثر ۲۰۰ کاراکتر")]
    string Name,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    bool IsActive,

    IReadOnlyList<int> PersonIds);

public record UpdatePersonGroupRequest(
    [Required(ErrorMessage = "نام گروه الزامی است")]
    [StringLength(200, ErrorMessage = "نام گروه حداکثر ۲۰۰ کاراکتر")]
    string Name,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    bool IsActive,

    IReadOnlyList<int> PersonIds);

public record PersonGroupLookupItemResponse(int Id, string Name, int MemberCount);
