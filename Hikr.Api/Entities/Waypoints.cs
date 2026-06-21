namespace Hikr.Api.Entities;

public class Waypoints
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public int Reihenfolge { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Name { get; set; }

    public Routes Route { get; set; } = null!;
}
