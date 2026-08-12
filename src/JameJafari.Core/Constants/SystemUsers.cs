namespace JameJafari.Core.Constants;

public static class SystemUsers
{
    public const string AdminUsername = "admin";

    public static bool IsSystemAdmin(string? username) =>
        string.Equals(username, AdminUsername, StringComparison.OrdinalIgnoreCase);
}
