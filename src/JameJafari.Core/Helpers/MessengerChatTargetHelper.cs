using System.Text.RegularExpressions;
using JameJafari.Core.Enums;

namespace JameJafari.Core.Helpers;

public static partial class MessengerChatTargetHelper
{
    [GeneratedRegex(@"^-?\d+$")]
    private static partial Regex NumericPattern();

    [GeneratedRegex(@"^[a-zA-Z][\w]{3,}$")]
    private static partial Regex UsernamePattern();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{4,100}$")]
    private static partial Regex RubikaChatIdPattern();

    public static string Normalize(MessengerKind messenger, string? raw) => messenger switch
    {
        MessengerKind.Rubika => NormalizeRubika(raw),
        _ => Normalize(raw)
    };

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

    public static string NormalizeRubika(string? raw)
    {
        var s = raw?.Trim() ?? "";
        if (string.IsNullOrEmpty(s))
            throw new InvalidOperationException("شناسه گفتگو الزامی است");
        if (!RubikaChatIdPattern().IsMatch(s))
            throw new InvalidOperationException("شناسه گفتگوی روبیکا نامعتبر است");
        return s;
    }

    public static bool TryParseNumericChatId(string? chatId, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(chatId))
            return false;

        return long.TryParse(chatId.Trim(), out value) && value != 0;
    }
}
