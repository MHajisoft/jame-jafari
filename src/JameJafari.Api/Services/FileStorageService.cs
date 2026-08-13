using Microsoft.AspNetCore.Http;

namespace JameJafari.Api.Services;

public class FileStorageService(IWebHostEnvironment env, ImageProcessingService imageProcessing)
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".heic", ".heif"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".heic", ".heif",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx"
    };

    private const long MaxFileBytes = 10 * 1024 * 1024;

    private readonly string _basePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "uploads"));

    /// <summary>Mobile cameras often send empty Content-Type; accept known image extensions.</summary>
    public static bool IsImageUpload(IFormFile file)
    {
        if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return true;
        var ext = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(ext) && ImageExtensions.Contains(ext);
    }

    public Task<string> SaveAsync(IFormFile file, string folder)
        => SaveAsync(file, folder, profile: null);

    public async Task<string> SaveAsync(IFormFile file, string folder, ImageProcessProfile? profile)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("فایل خالی است");
        if (file.Length > MaxFileBytes)
            throw new InvalidOperationException("حجم فایل بیش از ۱۰ مگابایت است");

        var ext = ResolveExtension(file);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("نوع فایل مجاز نیست");

        var safeFolder = SanitizeFolder(folder);
        var dir = Path.Combine(_basePath, safeFolder);
        Directory.CreateDirectory(dir);

        var shouldProcess = profile.HasValue && imageProcessing.CanProcess(file);
        var fileName = shouldProcess ? $"{Guid.NewGuid():N}.jpg" : $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(dir, fileName);

        if (shouldProcess)
        {
            try
            {
                await using var input = file.OpenReadStream();
                await using var processed = await imageProcessing.ProcessToJpegAsync(input, profile!.Value);
                await using var stream = File.Create(fullPath);
                await processed.CopyToAsync(stream);
            }
            catch
            {
                var fallbackName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
                var fallbackPath = Path.Combine(dir, fallbackName);
                await using var stream = File.Create(fallbackPath);
                await file.CopyToAsync(stream);
                return $"{safeFolder}/{fallbackName}".Replace('\\', '/');
            }
        }
        else
        {
            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);
        }

        return $"{safeFolder}/{fileName}".Replace('\\', '/');
    }

    public bool TryDelete(string relativePath)
    {
        if (!TryResolvePath(relativePath, out var fullPath))
            return false;

        try
        {
            File.Delete(fullPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryResolvePath(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var combined = Path.GetFullPath(Path.Combine(
            _basePath,
            relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));

        if (!combined.StartsWith(_basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, _basePath, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!File.Exists(combined))
            return false;

        fullPath = combined;
        return true;
    }

    private static string ResolveExtension(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!string.IsNullOrWhiteSpace(ext))
            return ext.ToLowerInvariant();

        var contentType = file.ContentType ?? string.Empty;
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase)) return ".png";
            if (contentType.Contains("webp", StringComparison.OrdinalIgnoreCase)) return ".webp";
            if (contentType.Contains("heic", StringComparison.OrdinalIgnoreCase)) return ".heic";
            if (contentType.Contains("heif", StringComparison.OrdinalIgnoreCase)) return ".heif";
            return ".jpg";
        }

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return ".pdf";

        return string.Empty;
    }

    private static string SanitizeFolder(string folder)
    {
        var parts = folder
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p is not "." and not "..")
            .ToArray();
        return parts.Length == 0 ? "misc" : string.Join(Path.DirectorySeparatorChar, parts);
    }
}
