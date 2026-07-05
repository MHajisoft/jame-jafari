namespace JameJafari.Core.DTOs;

public record AccountBalanceReportDto(int AccountId, string AccountName, decimal TotalIncome, decimal TotalCost, decimal Balance);
public record CostTypeReportDto(int CostTypeId, string CostTypeName, decimal TotalIncome, decimal TotalCost, decimal Net);
public record DateRangeReportDto(DateTime From, DateTime To, decimal TotalIncome, decimal TotalCost, decimal Balance);
public record PersonIncomeReportDto(int PersonId, string PersonName, decimal TotalAmount, int TransactionCount);
public record MonthlySummaryDto(int Year, int Month, decimal Income, decimal Cost);
public record FoodCostReportDto(int FoodId, string FoodName, DateTime CookDate, int TotalCount, decimal CostPerUnit, decimal TotalCost);
