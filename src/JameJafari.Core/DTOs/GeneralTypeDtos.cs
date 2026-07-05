namespace JameJafari.Core.DTOs;

public record GeneralTypeDto(int Id, string Name, string? Code, string Category, int SortOrder, bool IsActive);
public record CreateGeneralTypeRequest(string Name, string? Code, string Category, int SortOrder, bool IsActive);
public record UpdateGeneralTypeRequest(string Name, string? Code, int SortOrder, bool IsActive);
