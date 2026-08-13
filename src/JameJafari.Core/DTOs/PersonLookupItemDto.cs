using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

/// <summary>Person option for PersonSelect (lookup search).</summary>
public record PersonLookupItemDto(
    int Id,
    string FirstName,
    string? LastName,
    string? NickName,
    Gender Gender,
    string? PicturePath,
    string? FatherName,
    string? MotherName,
    string? FatherFirstName,
    string? MotherFirstName);
