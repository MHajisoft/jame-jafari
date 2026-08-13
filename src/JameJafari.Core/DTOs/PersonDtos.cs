using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

public record PersonDto(
    int Id,
    string FirstName,
    string? LastName,
    string? NickName,
    Gender Gender,
    int? FatherId,
    string? FatherName,
    int? MotherId,
    string? MotherName,
    string? FatherFirstName,
    string? MotherFirstName,
    string? PicturePath,
    string? Mobile,
    string? Address,
    int? NamePrefixId,
    string? NamePrefixName,
    bool IsDead,
    string DisplayName,
    AuditInfoDto Audit);

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

    bool IsDead);

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

    bool IsDead);
