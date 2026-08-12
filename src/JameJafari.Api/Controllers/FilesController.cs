using JameJafari.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/files")]
public class FilesController(FileStorageService storage) : ApiControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<object>> Upload(IFormFile file)
    {
        try
        {
            var path = await storage.SaveAsync(file, "uploads");
            return Ok(new { path, url = $"/uploads/{path}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{*filePath}")]
    public IActionResult GetFile(string filePath)
    {
        if (!storage.TryResolvePath(filePath, out var fullPath))
            return NotFound();
        return PhysicalFile(fullPath, "application/octet-stream", enableRangeProcessing: true);
    }
}
