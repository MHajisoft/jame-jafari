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
    [RequirePermission(
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<IReadOnlyList<IngredientPriceRecommendationDto>>> GetRecommendations()
        => Ok(await service.GetRecommendationsAsync());

    [HttpGet]
    [RequirePermission(
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<IReadOnlyList<FoodGenerationDto>>> GetByDate([FromQuery] DateTime date)
    {
        return Ok(ApplyAuditVisibility(await service.GetByDateAsync(date, OwnRecordsFilter(PermissionCodes.FoodView))));
    }

    [HttpGet("{id:int}")]
    [RequirePermission(
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<FoodGenerationDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id, OwnRecordsFilter(PermissionCodes.FoodView));
        return item is null ? NotFound() : Ok(ApplyAuditVisibility(item));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.FoodCreate)]
    public async Task<ActionResult<FoodGenerationDto>> Create([FromBody] CreateFoodGenerationRequest request)
        => Ok(ApplyAuditVisibility(await service.CreateAsync(request, CurrentUserId)));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<FoodGenerationDto>> Update(int id, [FromBody] UpdateFoodGenerationRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId, OwnRecordsFilter(PermissionCodes.FoodView));
        return item is null ? NotFound() : Ok(ApplyAuditVisibility(item));
    }
}
