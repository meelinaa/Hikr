namespace Hikr.Api.DTOs.Routes;

public class UpdateRouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public List<int> WaypointIds { get; set; } = [];
}
