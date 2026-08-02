namespace JameJafari.Core.DTOs;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, IReadOnlyList<string> Permissions);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
