using Hikr.Application.DTOs;
using Hikr.Application.Mappers;
using Hikr.Application.Repositories;
using Hikr.Domain.Enums;

namespace Hikr.Application.Services;

public class RouteService : IRouteService
{
    private readonly IRouteRepository _routeRepository;
    private readonly IWaypointRepository _waypointRepository;
    private readonly IOsrmService _osrmService;

    public RouteService(IRouteRepository routeRepository, IWaypointRepository waypointRepository, IOsrmService osrmService)
    {
        _routeRepository = routeRepository;
        _waypointRepository = waypointRepository;
        _osrmService = osrmService;
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
        route.CreatedAt = DateTime.UtcNow;

        if (Enum.TryParse<Profiles>(createDto.Transportation, true, out var profile) && route.RouteWaypoints.Any())
        {
            var waypoints = await _waypointRepository.GetByIdsAsync(route.RouteWaypoints.Select(rw => rw.WaypointId));
            var orderedWaypoints = route.RouteWaypoints.OrderBy(rw => rw.Order)
                .Select(rw => waypoints.FirstOrDefault(w => w.Id == rw.WaypointId))
                .Where(w => w != null)
                .Select(w => w!)
                .ToList();

            if (orderedWaypoints.Count >= 2)
            {
                route.Geometry = await _osrmService.CalculateGeometryAsync(orderedWaypoints, profile, OsrmServices.Route);
            }
        }

        var createdRoute = await _routeRepository.AddAsync(route);
        return createdRoute.ToDto();
    }

    public async Task UpdateRouteAsync(UpdateRouteDto updateDto)
    {
        var route = await _routeRepository.GetByIdAsync(updateDto.Id);
        if (route == null) return; // Or throw NotFoundException
        
        updateDto.UpdateEntity(route);

        if (Enum.TryParse<Profiles>(updateDto.Transportation, true, out var profile) && route.RouteWaypoints.Any())
        {
            var waypoints = await _waypointRepository.GetByIdsAsync(route.RouteWaypoints.Select(rw => rw.WaypointId));
            var orderedWaypoints = route.RouteWaypoints.OrderBy(rw => rw.Order)
                .Select(rw => waypoints.FirstOrDefault(w => w.Id == rw.WaypointId))
                .Where(w => w != null)
                .Select(w => w!)
                .ToList();

            if (orderedWaypoints.Count >= 2)
            {
                route.Geometry = await _osrmService.CalculateGeometryAsync(orderedWaypoints, profile, OsrmServices.Route);
            }
        }

        await _routeRepository.UpdateAsync(route);
    }

    public async Task DeleteRouteAsync(int id)
    {
        await _routeRepository.DeleteAsync(id);
    }
}
