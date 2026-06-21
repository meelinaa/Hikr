using Hikr.Api.Entities;
using Hikr.Api.Repositories;

namespace Hikr.Api.Services;

public class RouteService : IRouteService
{
    private readonly IRouteRepository _routeRepository;

    public RouteService(IRouteRepository routeRepository)
    {
        _routeRepository = routeRepository;
    }

    public async Task<IEnumerable<Routes>> GetAllRoutesAsync()
    {
        return await _routeRepository.GetAllAsync();
    }

    public async Task<Routes?> GetRouteByIdAsync(int id)
    {
        return await _routeRepository.GetByIdAsync(id);
    }

    public async Task<Routes> CreateRouteAsync(Routes route)
    {
        route.CreatedAt = DateTime.UtcNow; // Example business logic
        return await _routeRepository.AddAsync(route);
    }

    public async Task UpdateRouteAsync(Routes route)
    {
        await _routeRepository.UpdateAsync(route);
    }

    public async Task DeleteRouteAsync(int id)
    {
        await _routeRepository.DeleteAsync(id);
    }
}
