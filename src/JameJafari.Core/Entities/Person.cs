using JameJafari.Core.Enums;

namespace JameJafari.Core.Entities;

public class Person : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? NickName { get; set; }
    public Gender Gender { get; set; }
    public int? FatherId { get; set; }
    public Person? Father { get; set; }
    public int? MotherId { get; set; }
    public Person? Mother { get; set; }
    public string? PicturePath { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }
    public int? NamePrefixId { get; set; }
    public GeneralType? NamePrefix { get; set; }
    public bool IsDead { get; set; }

    public ICollection<Person> ChildrenAsFather { get; set; } = [];
    public ICollection<Person> ChildrenAsMother { get; set; } = [];
    public ICollection<IncomeTransaction> IncomeTransactions { get; set; } = [];
}
