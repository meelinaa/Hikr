using NetTopologySuite.Geometries;

namespace Hikr.Application.DTOs;

public class RouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Geometry GeoJson { get; set; } = null!;
    public List<int> WaypointIds { get; set; } = new List<int>();
}

public class CreateRouteDto
{
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public List<int> WaypointIds { get; set; } = new List<int>();
}

public class UpdateRouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public List<int> WaypointIds { get; set; } = new List<int>();
}

public class CalculateRouteDto
{
    public List<int> WaypointIds { get; set; } = new List<int>();
    public string Transportation { get; set; } = string.Empty;
}

public class CalculateRouteResponseDto
{
    public Geometry Geometry { get; set; } = null!;
}
