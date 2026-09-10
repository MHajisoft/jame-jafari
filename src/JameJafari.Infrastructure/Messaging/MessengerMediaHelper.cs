using JameJafari.Core.Enums;

namespace JameJafari.Infrastructure.Messaging;

public enum MessengerMediaKind
{
    Photo,
    Video,
    Document,
    Audio
}

public static class MessengerMediaHelper
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".heic", ".heif"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".ogg", ".wav", ".m4a", ".aac", ".flac"
    };

    public const int MaxGroupSize = 10;

    public static MessengerMediaKind Classify(string relativePath)
    {
        var ext = Path.GetExtension(relativePath);
        if (ImageExtensions.Contains(ext))
            return MessengerMediaKind.Photo;
        if (VideoExtensions.Contains(ext))
            return MessengerMediaKind.Video;
        if (AudioExtensions.Contains(ext))
            return MessengerMediaKind.Audio;
        return MessengerMediaKind.Document;
    }

    public static string ApiType(MessengerMediaKind kind) => kind switch
    {
        MessengerMediaKind.Photo => "photo",
        MessengerMediaKind.Video => "video",
        MessengerMediaKind.Audio => "audio",
        _ => "document"
    };

    public static void ValidateAttachmentCount(int count)
    {
        if (count is < 1 or > MaxGroupSize)
            throw new InvalidOperationException($"تعداد پیوست باید بین ۱ تا {MaxGroupSize} باشد");
    }

    public static MessengerMessageType InferMessageType(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
            return MessengerMessageType.Text;

        ValidateAttachmentCount(paths.Count);
        var imageCompose = paths.All(p => Classify(p) is MessengerMediaKind.Photo or MessengerMediaKind.Video);
        if (paths.Count >= 2)
            ValidateMediaGroup(paths, imageCompose);

        return imageCompose ? MessengerMessageType.Photo : MessengerMessageType.File;
    }

    public static void ValidateMediaGroup(IReadOnlyList<string> paths, bool imageCompose)
    {
        ValidateAttachmentCount(paths.Count);
        if (paths.Count < 2)
            return;

        var kinds = paths.Select(Classify).Distinct().ToList();
        if (imageCompose)
        {
            if (kinds.Any(k => k is MessengerMediaKind.Document or MessengerMediaKind.Audio))
                throw new InvalidOperationException("در حالت تصویر فقط تصویر و ویدیو قابل ارسال گروهی هستند");
            return;
        }

        if (kinds.Count > 1)
            throw new InvalidOperationException("فایل، صوت و سند را نمی‌توان در یک آلبوم مخلوط کرد");
    }
}
