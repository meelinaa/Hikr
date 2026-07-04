using Hikr.Application.Ports.Inbound;
using Hikr.Application.Ports.Outbound;
using Hikr.Domain.Entities;

namespace Hikr.Application.Services;

public class WaypointService(IWaypointRepository waypointRepository) : IWaypointUseCase
{
    private readonly IWaypointRepository _waypointRepository = waypointRepository;

    public async Task<IEnumerable<Waypoint>> GetAllWaypointsAsync()
    {
        return await _waypointRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Waypoint>> GetWaypointsByIdsAsync(IEnumerable<int> ids)
    {
        return await _waypointRepository.GetByIdsAsync(ids);
    }

    public async Task<Waypoint?> GetWaypointByIdAsync(int id)
    {
        return await _waypointRepository.GetByIdAsync(id);
    }

    public async Task<Waypoint> CreateWaypointAsync(Waypoint waypoint)
    {
        return await _waypointRepository.AddAsync(waypoint);
    }

    public async Task UpdateWaypointAsync(Waypoint waypoint)
    {
        await _waypointRepository.UpdateAsync(waypoint);
    }

    public async Task DeleteWaypointAsync(int id)
    {
        await _waypointRepository.DeleteAsync(id);
    }
}
