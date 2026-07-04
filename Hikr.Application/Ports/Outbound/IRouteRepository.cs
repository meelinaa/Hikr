using Hikr.Domain.Entities;

namespace Hikr.Application.Ports.Outbound;

public interface IRouteRepository
{
    Task<IEnumerable<Route>> GetAllAsync();
    Task<Route?> GetByIdAsync(int id);
    Task<Route> AddAsync(Route route);
    Task UpdateAsync(Route route);
    Task DeleteAsync(int id);
}
