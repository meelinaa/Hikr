using NetTopologySuite.Geometries;

namespace Hikr.Api.DTOs.Waypoints;

public class CreateWaypointDto
{
    public Geometry Geometry { get; set; } = null!;
    public string? Name { get; set; }
    public string? Type { get; set; }
}
