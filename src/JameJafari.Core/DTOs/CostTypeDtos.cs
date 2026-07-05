namespace JameJafari.Core.DTOs;

public record CostTypeDto(
    int Id,
    string Name,
    string? Description,
    bool IsIngredient,
    int? UnitId,
    string? UnitName,
    bool IsActive,
    AuditInfoDto Audit);

public record CreateCostTypeRequest(string Name, string? Description, bool IsIngredient, int? UnitId, bool IsActive);
public record UpdateCostTypeRequest(string Name, string? Description, bool IsIngredient, int? UnitId, bool IsActive);
