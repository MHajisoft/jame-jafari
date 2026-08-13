using System.Globalization;
using JameJafari.Core.Enums;

namespace JameJafari.Core.Helpers;

public readonly record struct JalaliParts(int Year, int Month, int Day);

public static class JalaliCalendarHelper
{
    static readonly PersianCalendar Calendar = new();

    public static JalaliParts ToParts(DateTime date)
    {
        var d = date.Date;
        return new JalaliParts(
            Calendar.GetYear(d),
            Calendar.GetMonth(d),
            Calendar.GetDayOfMonth(d));
    }

    /// <summary>1 = بهار (1–3), 2 = تابستان (4–6), 3 = پاییز (7–9), 4 = زمستان (10–12).</summary>
    public static int Season(int jalaliMonth) => (jalaliMonth - 1) / 3 + 1;

    /** Persian week starts on Saturday. */
    public static DateTime StartOfPersianWeek(DateTime date)
    {
        var d = date.Date;
        var daysSinceSaturday = ((int)d.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        return d.AddDays(-daysSinceSaturday);
    }

    /** Jalali (month, day) pairs for Sat–Fri week containing reference — for anniversary matching. */
    public static HashSet<(int Month, int Day)> GetWeekAnniversaryKeys(DateTime referenceDate)
    {
        var start = StartOfPersianWeek(referenceDate);
        var keys = new HashSet<(int, int)>();
        for (var i = 0; i < 7; i++)
        {
            var p = ToParts(start.AddDays(i));
            keys.Add((p.Month, p.Day));
        }
        return keys;
    }

    public static bool MatchesDeathAnniversary(
        DeathAnniversaryScope scope,
        JalaliParts death,
        JalaliParts reference,
        DateTime referenceDate)
    {
        if (scope == DeathAnniversaryScope.Week)
            return GetWeekAnniversaryKeys(referenceDate).Contains((death.Month, death.Day));

        return scope switch
        {
            DeathAnniversaryScope.Day =>
                death.Month == reference.Month && death.Day == reference.Day,
            DeathAnniversaryScope.Month =>
                death.Month == reference.Month,
            DeathAnniversaryScope.Season =>
                Season(death.Month) == Season(reference.Month),
            _ => false
        };
    }

    public static string WeekRangeLabelFa(DateTime referenceDate)
    {
        var start = StartOfPersianWeek(referenceDate);
        var end = start.AddDays(6);
        var s = ToParts(start);
        var e = ToParts(end);
        var sm = MonthNameFa(s.Month);
        var em = MonthNameFa(e.Month);
        if (s.Year == e.Year && s.Month == e.Month)
            return $"{s.Day}–{e.Day} {sm} {s.Year}";
        if (s.Year == e.Year)
            return $"{s.Day} {sm} – {e.Day} {em} {s.Year}";
        return $"{s.Day} {sm} {s.Year} – {e.Day} {em} {e.Year}";
    }

    public static string SeasonNameFa(int season) => season switch
    {
        1 => "بهار",
        2 => "تابستان",
        3 => "پاییز",
        4 => "زمستان",
        _ => ""
    };

    public static string MonthNameFa(int month) => month switch
    {
        1 => "فروردین",
        2 => "اردیبهشت",
        3 => "خرداد",
        4 => "تیر",
        5 => "مرداد",
        6 => "شهریور",
        7 => "مهر",
        8 => "آبان",
        9 => "آذر",
        10 => "دی",
        11 => "بهمن",
        12 => "اسفند",
        _ => ""
    };
}
