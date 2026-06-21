using System.Collections.Generic;
using Mapsui.Nts;
using NetTopologySuite.Geometries;

namespace Hikr.Models;

/// <summary>
/// Model representing a hiking route fetched from the Overpass API (OSM relation).
/// </summary>
public class HikingRouteModel
{
    /// <summary>
    /// The unique OSM relation ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The name of the hiking route.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The distance tag value from the relation if present.
    /// </summary>
    public string DistanceTag { get; set; } = string.Empty;

    /// <summary>
    /// The actual physical length in meters calculated from its geometry.
    /// </summary>
    public double CalculatedDistanceMeters { get; set; }

    /// <summary>
    /// All metadata tags associated with this relation.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// The Mapsui GeometryFeature representing the route polylines.
    /// </summary>
    public GeometryFeature Feature { get; set; } = null!;

    /// <summary>
    /// The NetTopologySuite Geometry.
    /// </summary>
    public Geometry Geometry { get; set; } = null!;

    /// <summary>
    /// Returns the formatted distance string (either using the tag or the calculated distance).
    /// </summary>
    public string GetFormattedDistance()
    {
        if (!string.IsNullOrWhiteSpace(DistanceTag))
        {
            // If the tag already has units, use it, otherwise format it
            if (DistanceTag.Contains("km") || DistanceTag.Contains("m"))
                return DistanceTag;

            if (double.TryParse(DistanceTag, out var distanceVal))
            {
                return $"{distanceVal:F1} km";
            }
        }

        // Fallback to calculated distance
        double km = CalculatedDistanceMeters / 1000.0;
        return $"{km:F1} km";
    }
}
