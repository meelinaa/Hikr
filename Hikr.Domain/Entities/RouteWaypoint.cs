namespace Hikr.Domain.Entities;

public class RouteWaypoint
{
    public int Id { get; set; }
    
    public int RouteId { get; set; }
    public Routes Route { get; set; } = null!;
    
    public int WaypointId { get; set; }
    public Waypoints Waypoint { get; set; } = null!;
    
    public int Order { get; set; }
}
