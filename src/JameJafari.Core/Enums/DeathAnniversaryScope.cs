namespace JameJafari.Core.Enums;

/// <summary>Filter for death-anniversary report against Jalali calendar.</summary>
public enum DeathAnniversaryScope
{
    /// <summary>Same Jalali month and day as reference (today by default).</summary>
    Day,
    /// <summary>Death anniversary Jalali month/day falls in the Persian week (Sat–Fri) containing reference.</summary>
    Week,
    /// <summary>Same Jalali month as reference.</summary>
    Month,
    /// <summary>Same Jalali season (3-month block) as reference.</summary>
    Season
}
