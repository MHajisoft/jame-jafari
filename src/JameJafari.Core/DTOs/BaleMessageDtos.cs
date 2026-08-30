using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

public class BaleMessageResponse : ResponseBase
{
    public int Id { get; init; }
    public MessengerKind MessengerKind { get; init; }
    public BaleMessageType MessageType { get; init; }
    public BaleMessageTargetKind TargetKind { get; init; }
    public int? MessageChannelId { get; init; }
    public string? MessageChannelName { get; init; }
    public int? PersonGroupId { get; init; }
    public string? PersonGroupName { get; init; }
    public Guid? BroadcastBatchId { get; init; }
    public string ChatId { get; init; } = "";
    public string? TargetMobile { get; init; }
    public int? BaleMessageId { get; init; }
    public string? Text { get; init; }
    public string? Caption { get; init; }
    public string? LinkUrl { get; init; }
    public string? LinkLabel { get; init; }
    public string? PhotoPath { get; init; }
    public IReadOnlyList<string> AttachmentPaths { get; init; } = [];
    public BaleMessageStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? SentAt { get; init; }
    public int? IncomeTransactionId { get; init; }
    public int? PersonId { get; init; }
    public string? PersonName { get; init; }
    public PersonSummaryResponse? PersonSummary { get; init; }
    public string? BotStartUrl { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDeleteRemote { get; init; }
    public bool CanDeleteLocal { get; init; }
}

public class MessengerInfoResponse
{
    public MessengerKind Kind { get; init; }
    public string Label { get; init; } = "";
    public bool IsConfigured { get; init; }
}

public class BaleConfigResponse
{
    public bool IsConfigured { get; init; }
    public string? BotUsername { get; init; }
    public IReadOnlyList<MessengerInfoResponse> AvailableMessengers { get; init; } = [];
}

public record SendBaleMessageRequest(
    MessengerKind Messenger,
    MessageComposeTarget TargetType,
    int? MessageChannelId,
    int? PersonGroupId,
    int? PersonId,

    [StringLength(4096, ErrorMessage = "متن حداکثر ۴۰۹۶ کاراکتر")]
    string? Text);

public class SendMessageBatchResponse
{
    public Guid BatchId { get; init; }
    public int Total { get; init; }
    public int Sent { get; init; }
    public int Failed { get; init; }
    public int AwaitingContact { get; init; }
    public int SkippedNoMobile { get; init; }
}

public class SendMessageResultResponse
{
    public BaleMessageResponse? Message { get; init; }
    public SendMessageBatchResponse? Batch { get; init; }
}

public class IncomeReceiptSendResult
{
    public bool Sent { get; init; }
    public string? Warning { get; init; }
    public BaleMessageResponse? Message { get; init; }
}

public record UpdateBaleMessageRequest(
    [StringLength(4096, ErrorMessage = "متن حداکثر ۴۰۹۶ کاراکتر")]
    string? Text,

    [StringLength(4096, ErrorMessage = "زیرنویس حداکثر ۴۰۹۶ کاراکتر")]
    string? Caption);

public sealed class BaleMessageDeleteResult
{
    public IReadOnlyList<string> AttachmentPathsToDelete { get; init; } = [];
    public string? PhotoPathToDelete => AttachmentPathsToDelete.FirstOrDefault();
}
