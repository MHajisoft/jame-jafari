using JameJafari.Api.Authorization;
using JameJafari.Api.Services;
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
    [RequirePermission(
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<IReadOnlyList<IngredientPriceRecommendationResponse>>> GetRecommendations()
        => Ok(await service.GetRecommendationsAsync());

    [HttpGet]
    [RequirePermission(
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<IReadOnlyList<FoodGenerationResponse>>> GetByDate([FromQuery] DateTime date)
        => Ok(ResponseVisibility.Apply(await service.GetByDateAsync(date, OwnRecordsFilter(PermissionCodes.FoodView)), User));

    [HttpGet("{id:int}")]
    [RequirePermission(
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<FoodGenerationResponse>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id, OwnRecordsFilter(PermissionCodes.FoodView));
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.FoodCreate)]
    public async Task<ActionResult<FoodGenerationResponse>> Create([FromBody] CreateFoodGenerationRequest request)
        => Ok(ResponseVisibility.Apply(await service.CreateAsync(request, CurrentUserId), User));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<FoodGenerationResponse>> Update(int id, [FromBody] UpdateFoodGenerationRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId, OwnRecordsFilter(PermissionCodes.FoodView));
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }
}
