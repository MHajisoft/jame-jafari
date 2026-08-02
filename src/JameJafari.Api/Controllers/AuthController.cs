using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Route("api/auth")]
public class AuthController(AuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return result is null ? Unauthorized(new { message = "نام کاربری یا رمز عبور اشتباه است" }) : Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        var permissions = User.FindAll("permission").Select(c => c.Value).ToList();
        return Ok(new
        {
            Username = User.Identity?.Name,
            Permissions = permissions
        });
    }
}
