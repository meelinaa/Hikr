using Hikr.Domain.Entities;
using Hikr.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Hikr.Application.Services;

public interface IOsrmService
{
    Task<Geometry> CalculateGeometryAsync(IEnumerable<Waypoints> waypoints, Profiles profile, OsrmServices service = OsrmServices.Route);
}
