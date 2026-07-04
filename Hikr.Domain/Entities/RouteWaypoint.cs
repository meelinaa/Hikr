namespace Hikr.Domain.Entities;

public class RouteWaypoint
{
    public int Id { get; set; }
    
    public int RouteId { get; set; }
    public Route Route { get; set; } = null!;
    
    public int WaypointId { get; set; }
    public Waypoint Waypoint { get; set; } = null!;
    
    public int Order { get; set; }
}
