using Hikr.Domain.Enums;

namespace Hikr.Api.DTOs.Routes;

public class CalculateRouteRequestDto
{
    public List<int> WaypointIds { get; set; } = [];
    public Profiles Profile { get; set; }
}
