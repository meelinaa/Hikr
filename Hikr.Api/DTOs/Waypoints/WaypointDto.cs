using NetTopologySuite.Geometries;

namespace Hikr.Api.DTOs.Waypoints;

public class WaypointDto
{
    public int Id { get; set; }
    public Geometry Geometry { get; set; } = null!;
    public string? Name { get; set; }
    public string? Type { get; set; }
}
