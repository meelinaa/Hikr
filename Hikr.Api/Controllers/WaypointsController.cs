using Hikr.Application.DTOs;
using Hikr.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hikr.Api.Controllers;

[ApiController]
[Route("waypoints")]
public class WaypointsController : ControllerBase
{
    private readonly IWaypointService _waypointService;

    public WaypointsController(IWaypointService waypointService)
    {
        _waypointService = waypointService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllWaypoints()
    {
        var waypoints = await _waypointService.GetAllWaypointsAsync();
        return Ok(waypoints);
    }



    [HttpGet("{id}")]
    public async Task<IActionResult> GetWaypointById(int id)
    {
        var waypoint = await _waypointService.GetWaypointByIdAsync(id);
        if (waypoint == null) return NotFound();
        return Ok(waypoint);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWaypoint([FromBody] CreateWaypointDto waypointDto)
    {
        var createdWaypoint = await _waypointService.CreateWaypointAsync(waypointDto);
        return CreatedAtAction(nameof(GetWaypointById), new { id = createdWaypoint.Id }, createdWaypoint);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWaypoint(int id, [FromBody] UpdateWaypointDto waypointDto)
    {
        if (id != waypointDto.Id) return BadRequest();
        
        await _waypointService.UpdateWaypointAsync(waypointDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWaypoint(int id)
    {
        await _waypointService.DeleteWaypointAsync(id);
        return NoContent();
    }
}
