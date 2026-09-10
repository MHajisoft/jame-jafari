using System.ComponentModel.DataAnnotations;
using JameJafari.Core.Enums;

namespace JameJafari.Core.DTOs;

public class MessageChannelResponse : ResponseBase
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public MessengerKind MessengerKind { get; init; }
    public string ExternalChatId { get; init; } = "";
    public bool IsActive { get; init; }
}

public record CreateMessageChannelRequest(
    [Required(ErrorMessage = "نام کانال الزامی است")]
    [StringLength(200, ErrorMessage = "نام کانال حداکثر ۲۰۰ کاراکتر")]
    string Name,

    MessengerKind MessengerKind,

    [Required(ErrorMessage = "شناسه گفتگو الزامی است")]
    [StringLength(100, ErrorMessage = "شناسه گفتگو حداکثر ۱۰۰ کاراکتر")]
    string ExternalChatId,

    bool IsActive);

public record UpdateMessageChannelRequest(
    [Required(ErrorMessage = "نام کانال الزامی است")]
    [StringLength(200, ErrorMessage = "نام کانال حداکثر ۲۰۰ کاراکتر")]
    string Name,

    MessengerKind MessengerKind,

    [Required(ErrorMessage = "شناسه گفتگو الزامی است")]
    [StringLength(100, ErrorMessage = "شناسه گفتگو حداکثر ۱۰۰ کاراکتر")]
    string ExternalChatId,

    bool IsActive);

public record MessageChannelLookupItemResponse(int Id, string Name, MessengerKind MessengerKind);

/// <summary>
/// Rubika group/channel discovered from bot updates (opaque chat_id is not shown in the Rubika UI).
/// </summary>
public class RubikaDiscoveredChatResponse
{
    public string ChatId { get; init; } = "";
    public string? Title { get; init; }
    public string ChatType { get; init; } = "";
    public string? Username { get; init; }
}

public class RubikaChannelSyncResult
{
    public int Discovered { get; init; }
    public int Added { get; init; }
    public int Skipped { get; init; }
    public IReadOnlyList<string> AddedNames { get; init; } = [];
}
