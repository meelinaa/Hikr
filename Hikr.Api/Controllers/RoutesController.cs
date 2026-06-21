using Hikr.Api.Entities;
using Hikr.Api.Services;
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
    public async Task<IActionResult> CreateRoute([FromBody] Routes route)
    {
        var createdRoute = await _routeService.CreateRouteAsync(route);
        return CreatedAtAction(nameof(GetRouteById), new { id = createdRoute.Id }, createdRoute);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoute(int id, [FromBody] Routes route)
    {
        if (id != route.Id) return BadRequest();
        
        await _routeService.UpdateRouteAsync(route);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        await _routeService.DeleteRouteAsync(id);
        return NoContent();
    }
}
