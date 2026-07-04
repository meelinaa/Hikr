using Hikr.Domain.Entities;
using Hikr.Domain.Enums;

namespace Hikr.Application.Ports.Inbound;

public interface IRouteUseCase
{
    Task<IEnumerable<Route>> GetAllRoutesAsync();
    Task<Route?> GetRouteByIdAsync(int id);
    Task<Route> CreateRouteAsync(Route route);
    Task UpdateRouteAsync(Route route);
    Task DeleteRouteAsync(int id);
    Task<NetTopologySuite.Geometries.Geometry?> CalculateRouteGeometryAsync(List<int> waypointIds, Profiles profile);
}
