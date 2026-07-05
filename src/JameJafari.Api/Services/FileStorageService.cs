namespace JameJafari.Api.Services;

public class FileStorageService(IWebHostEnvironment env)
{
    private readonly string _basePath = Path.Combine(env.ContentRootPath, "uploads");

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var dir = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);
        return $"{folder}/{fileName}".Replace('\\', '/');
    }
}
