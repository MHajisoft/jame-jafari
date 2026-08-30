namespace JameJafari.Core.Entities;

public class PersonGroupMember
{
    public int PersonGroupId { get; set; }
    public PersonGroup PersonGroup { get; set; } = null!;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
}
