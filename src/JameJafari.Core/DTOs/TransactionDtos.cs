using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

public class IncomeTransactionResponse : AttachmentResponseBase
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public string PersonName { get; init; } = "";
    public string? PersonNickName { get; init; }
    public int AccountId { get; init; }
    public string AccountName { get; init; } = "";
    public decimal Amount { get; init; }
    public PaymentType PaymentType { get; init; }
    public int CostTypeId { get; init; }
    public string CostTypeName { get; init; } = "";
    public string? TrackingCode { get; init; }
    public string? Description { get; init; }
    public DateTime TransactionDate { get; init; }
}

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

public class CostTransactionResponse : AttachmentResponseBase
{
    public int Id { get; init; }
    public int AccountId { get; init; }
    public string AccountName { get; init; } = "";
    public decimal Amount { get; init; }
    public int CostTypeId { get; init; }
    public string CostTypeName { get; init; } = "";
    public string? TrackingCode { get; init; }
    public string? Description { get; init; }
    public DateTime TransactionDate { get; init; }
}

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
