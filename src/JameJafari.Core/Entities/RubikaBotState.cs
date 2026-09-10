namespace JameJafari.Core.Entities;

public class RubikaBotState
{
    public int Id { get; set; }
    public string LastOffsetId { get; set; } = "";

    /// <summary>JSON array of recently seen Group/Channel chats (chat_id is not visible in Rubika UI).</summary>
    public string? KnownGroupChatsJson { get; set; }
}
