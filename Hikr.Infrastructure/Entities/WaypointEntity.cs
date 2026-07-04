using NetTopologySuite.Geometries;

namespace Hikr.Infrastructure.Entities;

public class WaypointEntity
{
    public int Id { get; set; }
    public Geometry Geometry { get; set; } = null!;
    public string? Name { get; set; }
    public string? Type { get; set; }
    public List<RouteWaypointEntity> RouteWaypoints { get; set; } = [];
}
