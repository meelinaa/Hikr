using Hikr.Domain.Entities;

namespace Hikr.Application.Repositories;

public interface IRouteRepository
{
    Task<IEnumerable<Routes>> GetAllAsync();
    Task<Routes?> GetByIdAsync(int id);
    Task<Routes> AddAsync(Routes route);
    Task UpdateAsync(Routes route);
    Task DeleteAsync(int id);
}
