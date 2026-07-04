using NetTopologySuite.Geometries;

namespace Hikr.Domain.Entities;

public class Routes
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Geometry Geometry { get; set; } = null!;
    public List<RouteWaypoint> RouteWaypoints { get; set; } = new();
}
