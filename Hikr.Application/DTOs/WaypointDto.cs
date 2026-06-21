using NetTopologySuite.Geometries;

namespace Hikr.Application.DTOs;

public class WaypointDto
{
    public int Id { get; set; }
    public Geometry Geometry { get; set; } = null!;
    public string? Name { get; set; }
    public string? Type { get; set; }
}

public class CreateWaypointDto
{
    public Geometry Geometry { get; set; } = null!;
    public string? Name { get; set; }
    public string? Type { get; set; }
}

public class UpdateWaypointDto
{
    public int Id { get; set; }
    public Geometry Geometry { get; set; } = null!;
    public string? Name { get; set; }
    public string? Type { get; set; }
}
