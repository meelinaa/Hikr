using NetTopologySuite.Geometries;

namespace Hikr.Api.DTOs.Routes;

public class RouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Geometry GeoJson { get; set; } = null!;
    public List<int> WaypointIds { get; set; } = [];
}
