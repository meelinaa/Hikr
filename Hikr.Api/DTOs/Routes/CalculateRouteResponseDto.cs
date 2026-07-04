using NetTopologySuite.Geometries;

namespace Hikr.Api.DTOs.Routes;

public class CalculateRouteResponseDto
{
    public Geometry Geometry { get; set; } = null!;
}
