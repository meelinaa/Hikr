namespace Hikr.Api.DTOs.Routes;

public class CreateRouteDto
{
    public string Name { get; set; } = string.Empty;
    public string Transportation { get; set; } = string.Empty;
    public List<int> WaypointIds { get; set; } = [];
}
