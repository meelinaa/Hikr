using Hikr.Api.Entities;

namespace Hikr.Api.Services;

public interface IWaypointService
{
    Task<IEnumerable<Waypoints>> GetAllWaypointsAsync();
    Task<IEnumerable<Waypoints>> GetWaypointsByIdsAsync(IEnumerable<int> ids);
    Task<Waypoints?> GetWaypointByIdAsync(int id);
    Task<Waypoints> CreateWaypointAsync(Waypoints waypoint);
    Task UpdateWaypointAsync(Waypoints waypoint);
    Task DeleteWaypointAsync(int id);
}
