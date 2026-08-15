using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

/// <summary>Person option for PersonSelect (lookup search).</summary>
public class PersonLookupItemResponse
{
    public int Id { get; init; }
    public string FirstName { get; init; } = "";
    public string? LastName { get; init; }
    public string? NickName { get; init; }
    public Gender Gender { get; init; }
    public string? PicturePath { get; init; }
    public string? FatherName { get; init; }
    public string? MotherName { get; init; }
    public string? FatherFirstName { get; init; }
    public string? MotherFirstName { get; init; }
    public bool IsDead { get; init; }
}
