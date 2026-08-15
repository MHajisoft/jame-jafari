using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

public class PersonResponse : ResponseBase
{
    public int Id { get; init; }
    public string FirstName { get; init; } = "";
    public string? LastName { get; init; }
    public string? NickName { get; init; }
    public Gender Gender { get; init; }
    public int? FatherId { get; init; }
    public string? FatherName { get; init; }
    public int? MotherId { get; init; }
    public string? MotherName { get; init; }
    public string? FatherFirstName { get; init; }
    public string? MotherFirstName { get; init; }
    public string? PicturePath { get; init; }
    public string? Mobile { get; init; }
    public string? Address { get; init; }
    public int? NamePrefixId { get; init; }
    public string? NamePrefixName { get; init; }
    public bool IsDead { get; init; }
    public DateTime? DeathDate { get; init; }
    public string DisplayName { get; init; } = "";
    public PersonSummaryResponse? FatherSummary { get; init; }
    public PersonSummaryResponse? MotherSummary { get; init; }
}

public record CreatePersonRequest(
    [Required(ErrorMessage = "نام الزامی است")]
    [StringLength(100, ErrorMessage = "نام حداکثر ۱۰۰ کاراکتر")]
    string FirstName,

    [StringLength(100, ErrorMessage = "نام خانوادگی حداکثر ۱۰۰ کاراکتر")]
    string? LastName,

    [StringLength(100)]
    string? NickName,

    Gender Gender,

    int? FatherId,
    int? MotherId,

    [StringLength(20, ErrorMessage = "موبایل حداکثر ۲۰ کاراکتر")]
    [Phone(ErrorMessage = "فرمت موبایل نامعتبر است")]
    string? Mobile,

    [StringLength(500, ErrorMessage = "آدرس حداکثر ۵۰۰ کاراکتر")]
    string? Address,

    int? NamePrefixId,

    bool IsDead,
    DateTime? DeathDate);

public record UpdatePersonRequest(
    [Required(ErrorMessage = "نام الزامی است")]
    [StringLength(100, ErrorMessage = "نام حداکثر ۱۰۰ کاراکتر")]
    string FirstName,

    [StringLength(100, ErrorMessage = "نام خانوادگی حداکثر ۱۰۰ کاراکتر")]
    string? LastName,

    [StringLength(100)]
    string? NickName,

    Gender Gender,

    int? FatherId,
    int? MotherId,

    [StringLength(20, ErrorMessage = "موبایل حداکثر ۲۰ کاراکتر")]
    [Phone(ErrorMessage = "فرمت موبایل نامعتبر است")]
    string? Mobile,

    [StringLength(500, ErrorMessage = "آدرس حداکثر ۵۰۰ کاراکتر")]
    string? Address,

    int? NamePrefixId,

    bool IsDead,
    DateTime? DeathDate);
