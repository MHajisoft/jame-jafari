using System.Globalization;
using JameJafari.Core.Enums;

namespace JameJafari.Core.Helpers;

public static class IncomeReceiptCopy
{
    public const string OrganizationName = "موسسه مذهبی جامعه جعفری";
    public const string Bismillah = "به نام خدا";

    public static string Greeting(Gender gender, string? firstName, string? lastName, string? namePrefix)
    {
        var honorific = gender == Gender.Female ? "سرکار خانم" : "جناب آقای";
        var name = PersonDisplayNameHelper.Format(firstName, lastName, namePrefix);
        return string.IsNullOrWhiteSpace(name) ? honorific : $"{honorific} {name}";
    }

    public static string Body(decimal amount, PaymentType paymentType, string costTypeName)
    {
        var reason = string.IsNullOrWhiteSpace(costTypeName) ? "کمک" : costTypeName.Trim();
        return $"بدین‌وسیله اعلام می‌گردد مبلغ {ForRtlShaper(FormatAmount(amount))} ریال به صورت {Quote(PaymentTypeLabel(paymentType))} از جنابعالی بابت {Quote(reason)} دریافت گردید.";
    }

    public static string PaymentTypeLabel(PaymentType type) => type switch
    {
        PaymentType.Cash => "نقدی",
        PaymentType.Pos => "کارتخوان",
        PaymentType.Cheque => "چک",
        PaymentType.BankTransference => "انتقال بانکی",
        _ => type.ToString()
    };

    public static string FormatAmount(decimal amount)
    {
        var raw = amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '٬');
        return ToPersianDigits(raw);
    }

    public static string FormatJalali(DateTime date)
    {
        var p = JalaliCalendarHelper.ToParts(date);
        return ToPersianDigits($"{p.Year:0000}/{p.Month:00}/{p.Day:00}");
    }

    /// <summary>
    /// ponytail: SkiaSharp.HarfBuzz DrawShapedText ignores LRI and reverses digit runs in RTL.
    /// Pre-flip so the visual order is LTR. Upgrade: shape numeric runs with an LTR HarfBuzz buffer.
    /// </summary>
    public static string ForRtlShaper(string ltrRun)
    {
        var chars = ltrRun.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>ponytail: fails if greeting/body/number-direction template breaks.</summary>
    public static void SelfCheck()
    {
        if (Greeting(Gender.Male, "علی", "محمدی", "حاج") != "جناب آقای حاج علی محمدی")
            throw new InvalidOperationException("الگوی متن رسید نامعتبر است");
        if (Greeting(Gender.Female, "زهرا", "احمدی", null) != "سرکار خانم زهرا احمدی")
            throw new InvalidOperationException("الگوی متن رسید نامعتبر است");

        var body = Body(1_500_000, PaymentType.Cash, " pentest ");
        if (!body.Contains("بدین‌وسیله اعلام می‌گردد", StringComparison.Ordinal)
            || !body.Contains("به صورت «نقدی»", StringComparison.Ordinal)
            || !body.Contains("بابت «pentest»", StringComparison.Ordinal)
            || !body.Contains("دریافت گردید", StringComparison.Ordinal)
            || body.Contains("در تاریخ", StringComparison.Ordinal))
            throw new InvalidOperationException("الگوی متن رسید نامعتبر است");

        if (FormatAmount(1_500_000) != "۱٬۵۰۰٬۰۰۰")
            throw new InvalidOperationException("الگوی مبلغ رسید نامعتبر است");
        if (ForRtlShaper("۱۵٬۰۰۰٬۰۰۰") != "۰۰۰٬۰۰۰٬۵۱")
            throw new InvalidOperationException("جهت اعداد مبلغ رسید نامعتبر است");

        var jalali = FormatJalali(new DateTime(2024, 3, 20));
        if (jalali.Length != 10 || jalali[4] != '/' || jalali[7] != '/')
            throw new InvalidOperationException("الگوی تاریخ عددی رسید نامعتبر است");
        if (ForRtlShaper("۱۴۰۵/۰۶/۰۸") != "۸۰/۶۰/۵۰۴۱")
            throw new InvalidOperationException("جهت اعداد تاریخ رسید نامعتبر است");
    }

    static string Quote(string value) => $"«{value.Replace(' ', '\u00A0')}»";

    static string ToPersianDigits(string value)
    {
        Span<char> chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '0' and <= '9')
                chars[i] = (char)('\u06F0' + (chars[i] - '0'));
        }
        return new string(chars);
    }
}
