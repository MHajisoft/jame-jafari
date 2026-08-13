namespace JameJafari.Core.DTOs;

public record AccountBalanceReportDto(int AccountId, string AccountName, decimal TotalIncome, decimal TotalCost, decimal Balance);
public record CostTypeReportDto(int CostTypeId, string CostTypeName, decimal TotalIncome, decimal TotalCost, decimal Net);
public record DateRangeReportDto(DateTime From, DateTime To, decimal TotalIncome, decimal TotalCost, decimal Balance);
public record PersonIncomeReportDto(int PersonId, string PersonName, string? PersonNickName, decimal TotalAmount, int TransactionCount);
public record MonthlySummaryDto(int Year, int Month, decimal Income, decimal Cost);
public record FoodCostReportDto(int FoodId, string FoodName, DateTime CookDate, int TotalCount, decimal CostPerUnit, decimal TotalCost);

public record DeathAnniversaryPersonDto(
    int PersonId,
    string DisplayName,
    string? NickName,
    string? PicturePath,
    DateTime DeathDate,
    int JalaliDeathYear,
    int JalaliDeathMonth,
    int JalaliDeathDay,
    int YearsSinceDeath);

public record DeathAnniversaryReportDto(
    string Scope,
    DateTime ReferenceDate,
    int JalaliReferenceYear,
    int JalaliReferenceMonth,
    int JalaliReferenceDay,
    int JalaliReferenceSeason,
    string ScopeLabelFa,
    IReadOnlyList<DeathAnniversaryPersonDto> Items);
