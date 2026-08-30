namespace JameJafari.Core.Entities;

public class PersonGroup : AuditableEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<PersonGroupMember> Members { get; set; } = [];
}
