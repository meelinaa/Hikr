using NetTopologySuite.Geometries;

namespace Hikr.Api.Entities;

public class Routes
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transportmittel { get; set; } = string.Empty;
    public DateTime ErstelltAm { get; set; }
    public Geometry GeoJson { get; set; } = null!;

    public ICollection<Waypoints> Waypoints { get; set; } = new List<Waypoints>();
}
