using Hikr.Api.Entities;

namespace Hikr.Api.Repositories;

public interface IRouteRepository
{
    Task<IEnumerable<Routes>> GetAllAsync();
    Task<Routes?> GetByIdAsync(int id);
    Task<Routes> AddAsync(Routes route);
    Task UpdateAsync(Routes route);
    Task DeleteAsync(int id);
}
