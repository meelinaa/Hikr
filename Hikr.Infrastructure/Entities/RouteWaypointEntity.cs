namespace Hikr.Infrastructure.Entities;

public class RouteWaypointEntity
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public RouteEntity Route { get; set; } = null!;
    public int WaypointId { get; set; }
    public WaypointEntity Waypoint { get; set; } = null!;
    public int Order { get; set; }
}
