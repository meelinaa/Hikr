using Hikr.Domain.Entities;
using Hikr.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Hikr.Application.Ports.Outbound;

public interface IOsrmService
{
    Task<Geometry> CalculateGeometryAsync(List<Waypoint> waypoints, Profiles profile, OsrmServices service);
}
