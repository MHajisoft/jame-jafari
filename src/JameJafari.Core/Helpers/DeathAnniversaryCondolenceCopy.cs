namespace JameJafari.Core.Helpers;

public static class DeathAnniversaryCondolenceCopy
{
    public const string OrganizationName = "موسسه مذهبی جامعه جعفری";
    public const string Verse = "إِنَّا لِلَّهِ وَإِنَّا إِلَيْهِ رَاجِعُونَ";
    public const string Title = "یادبود سالگرد رحلت";

    public static string YearsLine(int yearsSinceDeath)
    {
        if (yearsSinceDeath <= 0)
            return "سالگرد امسال";
        return $"سالگرد {ToPersianDigits(yearsSinceDeath)}";
    }

    public static string ChannelText(string displayName, int yearsSinceDeath)
    {
        var years = YearsLine(yearsSinceDeath);
        return $"🕯️ {years} درگذشت {displayName.Trim()}";
    }

    public static string ChannelCaption(string displayName, int yearsSinceDeath)
        => ChannelText(displayName, yearsSinceDeath);

    public static string ToPersianDigits(int value) => ToPersianDigits(value.ToString());

    public static string ToPersianDigits(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '0' and <= '9')
                chars[i] = (char)('۰' + (chars[i] - '0'));
        }
        return new string(chars);
    }

    /// <summary>
    /// ponytail: SkiaSharp.HarfBuzz DrawShapedText reverses digit runs in RTL.
    /// </summary>
    public static string ForRtlShaper(string ltrRun)
    {
        var chars = ltrRun.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    public static void SelfCheck()
    {
        if (YearsLine(0) != "سالگرد امسال")
            throw new InvalidOperationException("الگوی سالگرد نامعتبر است");
        if (YearsLine(5) != "سالگرد ۵")
            throw new InvalidOperationException("الگوی سالگرد نامعتبر است");
        var text = ChannelText("حاج علی محمدی", 3);
        if (!text.Contains("سالگرد ۳", StringComparison.Ordinal)
            || text.Contains("ام", StringComparison.Ordinal)
            || text.Contains("بازه", StringComparison.Ordinal)
            || !text.Contains("حاج علی محمدی", StringComparison.Ordinal))
            throw new InvalidOperationException("الگوی پیام سالگرد نامعتبر است");
    }
}
