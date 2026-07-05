using JameJafari.Core.Enums;

namespace JameJafari.Core.Entities;

public class GeneralType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public GeneralTypeCategory Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
