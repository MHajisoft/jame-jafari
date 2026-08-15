namespace JameJafari.Core.Helpers;

/// <summary>Single source for person display-name formatting (prefix + first + last).</summary>
public static class PersonDisplayNameHelper
{
    public static string Format(string? firstName, string? lastName, string? namePrefixName = null)
    {
        var name = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(namePrefixName) ? name : $"{namePrefixName} {name}";
    }

    public static string? FormatOrNull(string? firstName, string? lastName, string? namePrefixName = null)
    {
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            return null;
        return Format(firstName, lastName, namePrefixName);
    }
}
