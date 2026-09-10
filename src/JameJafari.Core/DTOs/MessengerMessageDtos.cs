using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

public class MessengerMessageResponse : ResponseBase
{
    public int Id { get; init; }
    public MessengerKind MessengerKind { get; init; }
    public MessengerMessageType MessageType { get; init; }
    public MessengerMessageTargetKind TargetKind { get; init; }
    public int? MessageChannelId { get; init; }
    public string? MessageChannelName { get; init; }
    public int? PersonGroupId { get; init; }
    public string? PersonGroupName { get; init; }
    public Guid? BroadcastBatchId { get; init; }
    public string ChatId { get; init; } = "";
    public string? TargetMobile { get; init; }
    public string? RemoteMessageId { get; init; }
    public string? Text { get; init; }
    public string? Caption { get; init; }
    public string? LinkUrl { get; init; }
    public string? LinkLabel { get; init; }
    public string? PhotoPath { get; init; }
    public IReadOnlyList<string> AttachmentPaths { get; init; } = [];
    public MessengerMessageStatus Status { get; init; }
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

public class MessengerConfigResponse
{
    public bool IsConfigured { get; init; }
    public string? BotUsername { get; init; }
    public IReadOnlyList<MessengerInfoResponse> AvailableMessengers { get; init; } = [];
    /// <summary>Public HTTPS base used to build webhook URLs (Messaging:PublicBaseUrl).</summary>
    public string? PublicBaseUrl { get; init; }
    public string? BaleWebhookUrl { get; init; }
    public string? RubikaWebhookUrl { get; init; }
    public bool CanRegisterWebhooks { get; init; }
}

public class RegisterMessengerWebhooksResponse
{
    public bool BaleRegistered { get; init; }
    public bool RubikaRegistered { get; init; }
    public string? BaleError { get; init; }
    public string? RubikaError { get; init; }
    public string? BaleWebhookUrl { get; init; }
    public string? RubikaWebhookUrl { get; init; }
}

public class SendMessengerMessageRequest
{
    /// <summary>One or more messengers (person / person-group targets). Ignored for channel (taken from each channel).</summary>
    public List<MessengerKind> Messengers { get; set; } = [];

    public MessageComposeTarget TargetType { get; set; }

    /// <summary>
    /// Destination channel ids when <see cref="TargetType"/> is Channel (one or more; messenger taken from each channel).
    /// </summary>
    public List<int> MessageChannelIds { get; set; } = [];

    /// <summary>One or more person groups when <see cref="TargetType"/> is PersonGroup.</summary>
    public List<int> PersonGroupIds { get; set; } = [];

    /// <summary>One or more persons when <see cref="TargetType"/> is Person.</summary>
    public List<int> PersonIds { get; set; } = [];

    [StringLength(4096, ErrorMessage = "متن حداکثر ۴۰۹۶ کاراکتر")]
    public string? Text { get; set; }
}

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
    public MessengerMessageResponse? Message { get; init; }
    public SendMessageBatchResponse? Batch { get; init; }
}

public class IncomeReceiptSendResult
{
    public bool Sent { get; init; }
    public string? Warning { get; init; }
    public MessengerMessageResponse? Message { get; init; }
    public IReadOnlyList<MessengerMessageResponse> Messages { get; init; } = [];
}

public record UpdateMessengerMessageRequest(
    [StringLength(4096, ErrorMessage = "متن حداکثر ۴۰۹۶ کاراکتر")]
    string? Text,

    [StringLength(4096, ErrorMessage = "زیرنویس حداکثر ۴۰۹۶ کاراکتر")]
    string? Caption);

public sealed class MessengerMessageDeleteResult
{
    public IReadOnlyList<string> AttachmentPathsToDelete { get; init; } = [];
    public string? PhotoPathToDelete => AttachmentPathsToDelete.FirstOrDefault();
}
