using Hikr.Application.Ports.Inbound;
using Hikr.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Hikr.Api.Controllers;

[ApiController]
[Route("waypoints")]
public class WaypointsController(IWaypointUseCase waypointUseCase) : ControllerBase
{
    private readonly IWaypointUseCase _waypointUseCase = waypointUseCase;

    [HttpGet]
    public async Task<IActionResult> GetAllWaypoints()
    {
        var waypoints = await _waypointUseCase.GetAllWaypointsAsync();
        return Ok(waypoints);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWaypointById(int id)
    {
        var waypoint = await _waypointUseCase.GetWaypointByIdAsync(id);
        if (waypoint == null) return NotFound();
        return Ok(waypoint);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWaypoint([FromBody] Waypoint waypointDto)
    {
        var createdWaypoint = await _waypointUseCase.CreateWaypointAsync(waypointDto);
        return CreatedAtAction(nameof(GetWaypointById), new { id = createdWaypoint.Id }, createdWaypoint);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWaypoint(int id, [FromBody] Waypoint waypointDto)
    {
        if (id != waypointDto.Id) return BadRequest();
        
        await _waypointUseCase.UpdateWaypointAsync(waypointDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWaypoint(int id)
    {
        await _waypointUseCase.DeleteWaypointAsync(id);
        return NoContent();
    }
}
