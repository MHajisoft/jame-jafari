namespace JameJafari.Core.DTOs;

public record AccountDto(int Id, string Name, string? Description, bool IsActive, AuditInfoDto Audit);
public record CreateAccountRequest(string Name, string? Description, bool IsActive);
public record UpdateAccountRequest(string Name, string? Description, bool IsActive);
