using System.Text.RegularExpressions;

namespace JameJafari.Core.Helpers;

public static partial class BaleChatTargetHelper
{
    [GeneratedRegex(@"^-?\d+$")]
    private static partial Regex NumericPattern();

    [GeneratedRegex(@"^[a-zA-Z][\w]{3,}$")]
    private static partial Regex UsernamePattern();

    public static string Normalize(string? raw)
    {
        var s = raw?.Trim() ?? "";
        if (string.IsNullOrEmpty(s))
            throw new InvalidOperationException("شناسه گفتگو الزامی است");

        if (NumericPattern().IsMatch(s))
        {
            if (s is "0" or "-0")
                throw new InvalidOperationException("شناسه گفتگو نامعتبر است");
            return s;
        }

        var username = s.StartsWith('@') ? s[1..] : s;
        if (!UsernamePattern().IsMatch(username))
            throw new InvalidOperationException("شناسه عددی یا نام کاربری @ وارد کنید");

        return "@" + username;
    }

    public static bool TryParseNumericChatId(string? chatId, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(chatId))
            return false;

        return long.TryParse(chatId.Trim(), out value) && value != 0;
    }
}
