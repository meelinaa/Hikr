using Hikr.Application.DTOs;
using Hikr.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hikr.Api.Controllers;

[ApiController]
[Route("routes")]
public class RoutesController : ControllerBase
{
    private readonly IRouteService _routeService;

    public RoutesController(IRouteService routeService)
    {
        _routeService = routeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRoutes()
    {
        var routes = await _routeService.GetAllRoutesAsync();
        return Ok(routes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRouteById(int id)
    {
        var route = await _routeService.GetRouteByIdAsync(id);
        if (route == null) return NotFound();
        return Ok(route);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteDto routeDto)
    {
        var createdRoute = await _routeService.CreateRouteAsync(routeDto);
        return CreatedAtAction(nameof(GetRouteById), new { id = createdRoute.Id }, createdRoute);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoute(int id, [FromBody] UpdateRouteDto routeDto)
    {
        if (id != routeDto.Id) return BadRequest();
        
        await _routeService.UpdateRouteAsync(routeDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        await _routeService.DeleteRouteAsync(id);
        return NoContent();
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateRoute([FromBody] CalculateRouteDto calculateDto)
    {
        var response = await _routeService.CalculateRouteAsync(calculateDto);
        if (response == null) return BadRequest("Could not calculate route. Ensure valid transportation profile and at least 2 valid waypoint IDs.");
        return Ok(response);
    }
}
