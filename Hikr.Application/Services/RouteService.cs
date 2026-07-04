using Hikr.Application.Ports.Inbound;
using Hikr.Application.Ports.Outbound;
using Hikr.Domain.Entities;
using Hikr.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Hikr.Application.Services;

public class RouteService(IRouteRepository routeRepository, IWaypointRepository waypointRepository, IOsrmService osrmService) : IRouteUseCase
{
    private readonly IRouteRepository _routeRepository = routeRepository;
    private readonly IWaypointRepository _waypointRepository = waypointRepository;
    private readonly IOsrmService _osrmService = osrmService;

    public async Task<IEnumerable<Route>> GetAllRoutesAsync()
    {
        return await _routeRepository.GetAllAsync();
    }

    public async Task<Route?> GetRouteByIdAsync(int id)
    {
        return await _routeRepository.GetByIdAsync(id);
    }

    public async Task<Route> CreateRouteAsync(Route route)
    {
        route.CreatedAt = DateTime.UtcNow;

        if (Enum.TryParse<Profiles>(route.Transportation, true, out var profile) && route.RouteWaypoints.Any())
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

        return await _routeRepository.AddAsync(route);
    }

    public async Task UpdateRouteAsync(Route route)
    {
        if (Enum.TryParse<Profiles>(route.Transportation, true, out var profile) && route.RouteWaypoints.Any())
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

    public async Task<Geometry?> CalculateRouteGeometryAsync(List<int> waypointIds, Profiles profile)
    {
        var waypoints = await _waypointRepository.GetByIdsAsync(waypointIds);
        
        var orderedWaypoints = waypointIds
            .Select(id => waypoints.FirstOrDefault(w => w.Id == id))
            .Where(w => w != null)
            .Select(w => w!)
            .ToList();

        if (orderedWaypoints.Count < 2)
        {
            return null;
        }

        return await _osrmService.CalculateGeometryAsync(orderedWaypoints, profile, OsrmServices.Route);
    }
}
