namespace Hikr.Api.DTOs.Routes;

public class CalculateRouteDto
{
    public List<int> WaypointIds { get; set; } = [];
    public string Transportation { get; set; } = string.Empty;
}
