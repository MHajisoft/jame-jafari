namespace JameJafari.Core.DTOs;

/// <summary>Minimal id/name pair for select lists (lookups API).</summary>
public record LookupItemResponse(int Id, string Name);

/// <summary>Cost type option for transaction/food forms.</summary>
public record CostTypeLookupItemResponse(int Id, string Name, string? UnitName);
