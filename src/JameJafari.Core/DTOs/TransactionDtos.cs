using System.ComponentModel.DataAnnotations;
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
    IReadOnlyList<TransactionAttachmentDto> Attachments,
    string? TrackingCode,
    string? Description,
    DateTime TransactionDate,
    AuditInfoDto Audit);

public record CreateIncomeTransactionRequest(
    [Range(1, int.MaxValue, ErrorMessage = "شخص الزامی است")]
    int PersonId,

    [Range(1, int.MaxValue, ErrorMessage = "حساب الزامی است")]
    int AccountId,

    [Range(0.01, 999999999, ErrorMessage = "مبلغ باید بیشتر از صفر باشد")]
    decimal Amount,

    PaymentType PaymentType,

    [Range(1, int.MaxValue, ErrorMessage = "نوع هزینه الزامی است")]
    int CostTypeId,

    [StringLength(100, ErrorMessage = "کد رهگیری حداکثر ۱۰۰ کاراکتر")]
    string? TrackingCode,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    DateTime TransactionDate);

public record UpdateIncomeTransactionRequest(
    [Range(1, int.MaxValue, ErrorMessage = "شخص الزامی است")]
    int PersonId,

    [Range(1, int.MaxValue, ErrorMessage = "حساب الزامی است")]
    int AccountId,

    [Range(0.01, 999999999, ErrorMessage = "مبلغ باید بیشتر از صفر باشد")]
    decimal Amount,

    PaymentType PaymentType,

    [Range(1, int.MaxValue, ErrorMessage = "نوع هزینه الزامی است")]
    int CostTypeId,

    [StringLength(100, ErrorMessage = "کد رهگیری حداکثر ۱۰۰ کاراکتر")]
    string? TrackingCode,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    DateTime TransactionDate);

public record CostTransactionDto(
    int Id,
    int AccountId,
    string AccountName,
    decimal Amount,
    int CostTypeId,
    string CostTypeName,
    IReadOnlyList<TransactionAttachmentDto> Attachments,
    string? TrackingCode,
    string? Description,
    DateTime TransactionDate,
    AuditInfoDto Audit);

public record CreateCostTransactionRequest(
    [Range(1, int.MaxValue, ErrorMessage = "حساب الزامی است")]
    int AccountId,

    [Range(0.01, 999999999, ErrorMessage = "مبلغ باید بیشتر از صفر باشد")]
    decimal Amount,

    [Range(1, int.MaxValue, ErrorMessage = "نوع هزینه الزامی است")]
    int CostTypeId,

    [StringLength(100, ErrorMessage = "کد رهگیری حداکثر ۱۰۰ کاراکتر")]
    string? TrackingCode,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    DateTime TransactionDate);

public record UpdateCostTransactionRequest(
    [Range(1, int.MaxValue, ErrorMessage = "حساب الزامی است")]
    int AccountId,

    [Range(0.01, 999999999, ErrorMessage = "مبلغ باید بیشتر از صفر باشد")]
    decimal Amount,

    [Range(1, int.MaxValue, ErrorMessage = "نوع هزینه الزامی است")]
    int CostTypeId,

    [StringLength(100, ErrorMessage = "کد رهگیری حداکثر ۱۰۰ کاراکتر")]
    string? TrackingCode,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    DateTime TransactionDate);
