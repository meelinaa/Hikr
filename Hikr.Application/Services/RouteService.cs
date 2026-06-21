using Hikr.Application.DTOs;
using Hikr.Application.Mappers;
using Hikr.Application.Repositories;

namespace Hikr.Application.Services;

public class RouteService : IRouteService
{
    private readonly IRouteRepository _routeRepository;

    public RouteService(IRouteRepository routeRepository)
    {
        _routeRepository = routeRepository;
    }

    public async Task<IEnumerable<RouteDto>> GetAllRoutesAsync()
    {
        var routes = await _routeRepository.GetAllAsync();
        return routes.Select(r => r.ToDto());
    }

    public async Task<RouteDto?> GetRouteByIdAsync(int id)
    {
        var route = await _routeRepository.GetByIdAsync(id);
        return route?.ToDto();
    }

    public async Task<RouteDto> CreateRouteAsync(CreateRouteDto createDto)
    {
        var route = createDto.ToEntity();
        route.CreatedAt = DateTime.UtcNow; // Example business logic
        var createdRoute = await _routeRepository.AddAsync(route);
        return createdRoute.ToDto();
    }

    public async Task UpdateRouteAsync(UpdateRouteDto updateDto)
    {
        var route = await _routeRepository.GetByIdAsync(updateDto.Id);
        if (route == null) return; // Or throw NotFoundException
        
        updateDto.UpdateEntity(route);
        await _routeRepository.UpdateAsync(route);
    }

    public async Task DeleteRouteAsync(int id)
    {
        await _routeRepository.DeleteAsync(id);
    }
}
