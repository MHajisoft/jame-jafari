using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/food")]
public class FoodController(FoodService service) : ApiControllerBase
{
    [HttpGet("recommendations")]
    [RequirePermission(PermissionCodes.FoodView)]
    public async Task<ActionResult<IReadOnlyList<IngredientPriceRecommendationDto>>> GetRecommendations()
        => Ok(await service.GetRecommendationsAsync());

    [HttpGet]
    [RequirePermission(PermissionCodes.FoodView)]
    public async Task<ActionResult<IReadOnlyList<FoodGenerationDto>>> GetByDate([FromQuery] DateTime date)
        => Ok(await service.GetByDateAsync(date));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.FoodView)]
    public async Task<ActionResult<FoodGenerationDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.FoodManage)]
    public async Task<ActionResult<FoodGenerationDto>> Create([FromBody] CreateFoodGenerationRequest request)
        => Ok(await service.CreateAsync(request, CurrentUserId));
}
