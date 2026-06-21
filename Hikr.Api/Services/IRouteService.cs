using Hikr.Api.Entities;

namespace Hikr.Api.Services;

public interface IRouteService
{
    Task<IEnumerable<Routes>> GetAllRoutesAsync();
    Task<Routes?> GetRouteByIdAsync(int id);
    Task<Routes> CreateRouteAsync(Routes route);
    Task UpdateRouteAsync(Routes route);
    Task DeleteRouteAsync(int id);
}
