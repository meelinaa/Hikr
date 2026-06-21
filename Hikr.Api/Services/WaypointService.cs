using Hikr.Api.Entities;
using Hikr.Api.Repositories;

namespace Hikr.Api.Services;

public class WaypointService : IWaypointService
{
    private readonly IWaypointRepository _waypointRepository;

    public WaypointService(IWaypointRepository waypointRepository)
    {
        _waypointRepository = waypointRepository;
    }

    public async Task<IEnumerable<Waypoints>> GetAllWaypointsAsync()
    {
        return await _waypointRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Waypoints>> GetWaypointsByIdsAsync(IEnumerable<int> ids)
    {
        return await _waypointRepository.GetByIdsAsync(ids);
    }

    public async Task<Waypoints?> GetWaypointByIdAsync(int id)
    {
        return await _waypointRepository.GetByIdAsync(id);
    }

    public async Task<Waypoints> CreateWaypointAsync(Waypoints waypoint)
    {
        return await _waypointRepository.AddAsync(waypoint);
    }

    public async Task UpdateWaypointAsync(Waypoints waypoint)
    {
        await _waypointRepository.UpdateAsync(waypoint);
    }

    public async Task DeleteWaypointAsync(int id)
    {
        await _waypointRepository.DeleteAsync(id);
    }
}
