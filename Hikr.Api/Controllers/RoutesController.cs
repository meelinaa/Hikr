using Hikr.Api.DTOs.Routes;
using Hikr.Application.Ports.Inbound;
using Hikr.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Hikr.Api.Controllers;

[ApiController]
[Route("routes")]
public class RoutesController(IRouteUseCase routeUseCase) : ControllerBase
{
    private readonly IRouteUseCase _routeUseCase = routeUseCase;

    [HttpGet]
    public async Task<IActionResult> GetAllRoutes()
    {
        var routes = await _routeUseCase.GetAllRoutesAsync();
        return Ok(routes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRouteById(int id)
    {
        var route = await _routeUseCase.GetRouteByIdAsync(id);
        if (route == null) return NotFound();
        return Ok(route);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] Hikr.Domain.Entities.Route routeDto)
    {
        var createdRoute = await _routeUseCase.CreateRouteAsync(routeDto);
        return CreatedAtAction(nameof(GetRouteById), new { id = createdRoute.Id }, createdRoute);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoute(int id, [FromBody] Hikr.Domain.Entities.Route routeDto)
    {
        if (id != routeDto.Id) return BadRequest();

        await _routeUseCase.UpdateRouteAsync(routeDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        await _routeUseCase.DeleteRouteAsync(id);
        return NoContent();
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateRoute([FromBody] CalculateRouteRequestDto calculateDto)
    {
        var response = await _routeUseCase.CalculateRouteGeometryAsync(calculateDto.WaypointIds, calculateDto.Profile);
        if (response == null) return BadRequest("Could not calculate route.");
        return Ok(new { Geometry = response });
    }
}
