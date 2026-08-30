using JameJafari.Core.Enums;

namespace JameJafari.Infrastructure.Bale;

public enum BaleMediaKind
{
    Photo,
    Video,
    Document,
    Audio
}

public static class BaleMediaHelper
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

    public static BaleMediaKind Classify(string relativePath)
    {
        var ext = Path.GetExtension(relativePath);
        if (ImageExtensions.Contains(ext))
            return BaleMediaKind.Photo;
        if (VideoExtensions.Contains(ext))
            return BaleMediaKind.Video;
        if (AudioExtensions.Contains(ext))
            return BaleMediaKind.Audio;
        return BaleMediaKind.Document;
    }

    public static string ApiType(BaleMediaKind kind) => kind switch
    {
        BaleMediaKind.Photo => "photo",
        BaleMediaKind.Video => "video",
        BaleMediaKind.Audio => "audio",
        _ => "document"
    };

    public static void ValidateAttachmentCount(int count)
    {
        if (count is < 1 or > MaxGroupSize)
            throw new InvalidOperationException($"تعداد پیوست باید بین ۱ تا {MaxGroupSize} باشد");
    }

    public static BaleMessageType InferMessageType(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
            return BaleMessageType.Text;

        ValidateAttachmentCount(paths.Count);
        var imageCompose = paths.All(p => Classify(p) is BaleMediaKind.Photo or BaleMediaKind.Video);
        if (paths.Count >= 2)
            ValidateMediaGroup(paths, imageCompose);

        return imageCompose ? BaleMessageType.Photo : BaleMessageType.File;
    }

    public static void ValidateMediaGroup(IReadOnlyList<string> paths, bool imageCompose)
    {
        ValidateAttachmentCount(paths.Count);
        if (paths.Count < 2)
            return;

        var kinds = paths.Select(Classify).Distinct().ToList();
        if (imageCompose)
        {
            if (kinds.Any(k => k is BaleMediaKind.Document or BaleMediaKind.Audio))
                throw new InvalidOperationException("در حالت تصویر فقط تصویر و ویدیو قابل ارسال گروهی هستند");
            return;
        }

        if (kinds.Count > 1)
            throw new InvalidOperationException("فایل، صوت و سند را نمی‌توان در یک آلبوم مخلوط کرد");
    }
}
