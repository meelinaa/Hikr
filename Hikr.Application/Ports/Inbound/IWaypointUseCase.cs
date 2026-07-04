using Hikr.Domain.Entities;

namespace Hikr.Application.Ports.Inbound;

public interface IWaypointUseCase
{
    Task<IEnumerable<Waypoint>> GetAllWaypointsAsync();
    Task<IEnumerable<Waypoint>> GetWaypointsByIdsAsync(IEnumerable<int> ids);
    Task<Waypoint?> GetWaypointByIdAsync(int id);
    Task<Waypoint> CreateWaypointAsync(Waypoint waypoint);
    Task UpdateWaypointAsync(Waypoint waypoint);
    Task DeleteWaypointAsync(int id);
}
