using System.Text;

namespace JameJafari.Core.Helpers;

public static class PhoneNormalizeHelper
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new StringBuilder();
        foreach (var ch in value.Trim())
        {
            if (ch is >= '0' and <= '9')
            {
                digits.Append(ch);
                continue;
            }

            if (ch is '۰' or '۱' or '۲' or '۳' or '۴' or '۵' or '۶' or '۷' or '۸' or '۹')
            {
                digits.Append((char)('0' + (ch - '۰')));
                continue;
            }

            if (ch is '٠' or '١' or '٢' or '٣' or '٤' or '٥' or '٦' or '٧' or '٨' or '٩')
                digits.Append((char)('0' + (ch - '٠')));
        }

        var raw = digits.ToString();
        if (raw.Length == 0)
            return null;

        if (raw.StartsWith("0098", StringComparison.Ordinal))
            raw = raw[4..];
        else if (raw.StartsWith("98", StringComparison.Ordinal) && raw.Length >= 12)
            raw = raw[2..];

        if (raw.StartsWith('0') && raw.Length == 11)
            raw = raw[1..];

        if (raw.Length != 10 || !raw.StartsWith('9'))
            return $"+98{raw}";

        return $"+98{raw}";
    }
}
