namespace JameJafari.Core.DTOs;

public record AccountBalanceReportResponse(int AccountId, string AccountName, decimal TotalIncome, decimal TotalCost, decimal Balance);
public record CostTypeReportResponse(int CostTypeId, string CostTypeName, decimal TotalIncome, decimal TotalCost, decimal Net);
public record DateRangeReportResponse(DateTime From, DateTime To, decimal TotalIncome, decimal TotalCost, decimal Balance);
public record PersonIncomeReportResponse(int PersonId, string PersonName, string? PersonNickName, decimal TotalAmount, int TransactionCount);
public record MonthlySummaryResponse(int Year, int Month, decimal Income, decimal Cost);

public class FoodCostReportResponse
{
    public int FoodId { get; init; }
    public string FoodName { get; init; } = "";
    public DateTime CookDate { get; init; }
    public int TotalCount { get; init; }
    public decimal CostPerUnit { get; init; }
    public decimal TotalCost { get; init; }
}

public class DeathAnniversaryPersonResponse
{
    public int PersonId { get; init; }
    public string DisplayName { get; init; } = "";
    public string? NickName { get; init; }
    public string? PicturePath { get; init; }
    public DateTime DeathDate { get; init; }
    public int JalaliDeathYear { get; init; }
    public int JalaliDeathMonth { get; init; }
    public int JalaliDeathDay { get; init; }
    public int YearsSinceDeath { get; init; }
}

public class DeathAnniversaryReportResponse
{
    public string Scope { get; init; } = "";
    public DateTime ReferenceDate { get; init; }
    public int JalaliReferenceYear { get; init; }
    public int JalaliReferenceMonth { get; init; }
    public int JalaliReferenceDay { get; init; }
    public int JalaliReferenceSeason { get; init; }
    public string ScopeLabelFa { get; init; } = "";
    public IReadOnlyList<DeathAnniversaryPersonResponse> Items { get; init; } = [];
}
