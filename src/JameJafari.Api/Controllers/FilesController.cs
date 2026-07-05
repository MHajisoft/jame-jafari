using JameJafari.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/files")]
public class FilesController(IWebHostEnvironment env) : ApiControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<object>> Upload(IFormFile file, [FromServices] FileStorageService storage)
    {
        var path = await storage.SaveAsync(file, "uploads");
        return Ok(new { path, url = $"/uploads/{path}" });
    }

    [HttpGet("{*filePath}")]
    [AllowAnonymous]
    public IActionResult GetFile(string filePath)
    {
        var fullPath = Path.Combine(env.ContentRootPath, "uploads", filePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath)) return NotFound();
        return PhysicalFile(fullPath, "application/octet-stream", enableRangeProcessing: true);
    }
}
