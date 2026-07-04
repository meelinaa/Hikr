using Hikr.Domain.Entities;

namespace Hikr.Application.Ports.Outbound;

public interface IWaypointRepository
{
    Task<IEnumerable<Waypoint>> GetAllAsync();
    Task<IEnumerable<Waypoint>> GetByIdsAsync(IEnumerable<int> ids);
    Task<Waypoint?> GetByIdAsync(int id);
    Task<Waypoint> AddAsync(Waypoint waypoint);
    Task UpdateAsync(Waypoint waypoint);
    Task DeleteAsync(int id);
}
