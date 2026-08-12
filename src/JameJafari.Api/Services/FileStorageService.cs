using Microsoft.AspNetCore.Http;

namespace JameJafari.Api.Services;

public class FileStorageService(IWebHostEnvironment env)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx"
    };

    private const long MaxFileBytes = 10 * 1024 * 1024;

    private readonly string _basePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "uploads"));

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("فایل خالی است");
        if (file.Length > MaxFileBytes)
            throw new InvalidOperationException("حجم فایل بیش از ۱۰ مگابایت است");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("نوع فایل مجاز نیست");

        var safeFolder = SanitizeFolder(folder);
        var dir = Path.Combine(_basePath, safeFolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);
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
