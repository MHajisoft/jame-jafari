namespace JameJafari.Core.DTOs;

/// <summary>Minimal id/name pair for select lists (lookups API).</summary>
public record LookupItemDto(int Id, string Name);

/// <summary>Cost type option for transaction/food forms.</summary>
public record CostTypeLookupItemDto(int Id, string Name, string? UnitName);
