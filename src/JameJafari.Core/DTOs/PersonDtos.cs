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
    string? PicturePath,
    string? Mobile,
    string? Address,
    int? TravelPrefixId,
    string? TravelPrefixName,
    bool IsDead,
    string DisplayName,
    AuditInfoDto Audit);

public record CreatePersonRequest(
    string FirstName,
    string? LastName,
    string? NickName,
    Gender Gender,
    int? FatherId,
    int? MotherId,
    string? Mobile,
    string? Address,
    int? TravelPrefixId,
    bool IsDead);

public record UpdatePersonRequest(
    string FirstName,
    string? LastName,
    string? NickName,
    Gender Gender,
    int? FatherId,
    int? MotherId,
    string? Mobile,
    string? Address,
    int? TravelPrefixId,
    bool IsDead);
