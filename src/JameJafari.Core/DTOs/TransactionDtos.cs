using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

public record IncomeTransactionDto(
    int Id,
    int PersonId,
    string PersonName,
    int AccountId,
    string AccountName,
    decimal Amount,
    PaymentType PaymentType,
    int CostTypeId,
    string CostTypeName,
    string? DocumentPath,
    string? TrackingCode,
    string? Description,
    DateTime TransactionDate,
    AuditInfoDto Audit);

public record CreateIncomeTransactionRequest(
    int PersonId,
    int AccountId,
    decimal Amount,
    PaymentType PaymentType,
    int CostTypeId,
    string? TrackingCode,
    string? Description,
    DateTime TransactionDate);

public record CostTransactionDto(
    int Id,
    int AccountId,
    string AccountName,
    decimal Amount,
    int CostTypeId,
    string CostTypeName,
    string? DocumentPath,
    string? TrackingCode,
    string? Description,
    DateTime TransactionDate,
    AuditInfoDto Audit);

public record CreateCostTransactionRequest(
    int AccountId,
    decimal Amount,
    int CostTypeId,
    string? TrackingCode,
    string? Description,
    DateTime TransactionDate);
