using Mapsui.Nts;

namespace Hikr.Models;

/// <summary>
/// Model representing calculated route data including distance, duration, and the visual geometry feature.
/// </summary>
public class RouteData
{
    /// <summary>
    /// Gets or sets the Mapsui GeometryFeature representing the route coordinates.
    /// </summary>
    public GeometryFeature Feature { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total route distance in meters.
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Gets or sets the total route duration in seconds.
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Gets or sets the list of navigation steps for this route.
    /// </summary>
    public List<NavigationStep> Steps { get; set; } = new();
}
