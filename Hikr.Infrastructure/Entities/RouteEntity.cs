using NetTopologySuite.Geometries;

namespace Hikr.Infrastructure.Entities;

public class RouteEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Geometry Geometry { get; set; } = null!;
    public List<RouteWaypointEntity> RouteWaypoints { get; set; } = new();
}
