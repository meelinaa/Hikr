using Hikr.Domain.Entities;

namespace Hikr.Application.Repositories;

public interface IWaypointRepository
{
    Task<IEnumerable<Waypoints>> GetAllAsync();
    Task<IEnumerable<Waypoints>> GetByIdsAsync(IEnumerable<int> ids);
    Task<Waypoints?> GetByIdAsync(int id);
    Task<Waypoints> AddAsync(Waypoints waypoint);
    Task UpdateAsync(Waypoints waypoint);
    Task DeleteAsync(int id);
}
