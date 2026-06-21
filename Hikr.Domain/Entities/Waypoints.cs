using NetTopologySuite.Geometries;

namespace Hikr.Domain.Entities;

public class Waypoints
{
    public int Id { get; set; }
    
    /// <summary>
    /// The location of the Waypoint (Point).
    /// </summary>
    public Geometry Geometry { get; set; } = null!;
    
    /// <summary>
    /// Name of the POI, e.g. "Aussichtspunkt Turm"
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Category/Type of the Waypoint, e.g. "Viewpoint", "Hut", "Navigation"
    /// </summary>
    public string? Type { get; set; }

    public List<RouteWaypoint> RouteWaypoints { get; set; } = new();
}
