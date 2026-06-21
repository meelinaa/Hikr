using Hikr.Application.DTOs;
using Hikr.Application.Mappers;
using Hikr.Application.Repositories;

namespace Hikr.Application.Services;

public class WaypointService : IWaypointService
{
    private readonly IWaypointRepository _waypointRepository;

    public WaypointService(IWaypointRepository waypointRepository)
    {
        _waypointRepository = waypointRepository;
    }

    public async Task<IEnumerable<WaypointDto>> GetAllWaypointsAsync()
    {
        var waypoints = await _waypointRepository.GetAllAsync();
        return waypoints.Select(w => w.ToDto());
    }

    public async Task<IEnumerable<WaypointDto>> GetWaypointsByIdsAsync(IEnumerable<int> ids)
    {
        var waypoints = await _waypointRepository.GetByIdsAsync(ids);
        return waypoints.Select(w => w.ToDto());
    }

    public async Task<WaypointDto?> GetWaypointByIdAsync(int id)
    {
        var waypoint = await _waypointRepository.GetByIdAsync(id);
        return waypoint?.ToDto();
    }

    public async Task<WaypointDto> CreateWaypointAsync(CreateWaypointDto createDto)
    {
        var waypoint = createDto.ToEntity();
        var createdWaypoint = await _waypointRepository.AddAsync(waypoint);
        return createdWaypoint.ToDto();
    }

    public async Task UpdateWaypointAsync(UpdateWaypointDto updateDto)
    {
        var waypoint = await _waypointRepository.GetByIdAsync(updateDto.Id);
        if (waypoint == null) return; // Or throw NotFoundException
        
        updateDto.UpdateEntity(waypoint);
        await _waypointRepository.UpdateAsync(waypoint);
    }

    public async Task DeleteWaypointAsync(int id)
    {
        await _waypointRepository.DeleteAsync(id);
    }
}
