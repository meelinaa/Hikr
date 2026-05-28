using System;
using System.Linq;
using Mapsui;

namespace Hikr.Helpers;

/// <summary>
/// Stateless helper class containing all calculations for map coordinate distances, bearings, and formatters.
/// </summary>
public static class NavigationCalculator
{
    private const double SafeZoomResolution = 0.25;
    private const int SafeResolutionIndex = 17;

    /// <summary>
    /// Calculates the Euclidean distance between two points in Spherical Mercator projection.
    /// </summary>
    public static double Distance(MPoint p1, MPoint p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates the bearing (angle) between two points in degrees.
    /// </summary>
    public static double Bearing(MPoint p1, MPoint p2)
    {
        return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180.0 / Math.PI;
    }

    /// <summary>
    /// Formats route duration in seconds to a human-readable string.
    /// </summary>
    public static string FormatDuration(double durationSeconds)
    {
        if (durationSeconds <= 0) return "0 Min.";
        double minutes = durationSeconds / 60.0;
        if (minutes < 1)
        {
            return "1 Min.";
        }
        if (minutes < 60)
        {
            return $"{(int)Math.Round(minutes)} Min.";
        }
        int hours = (int)(minutes / 60);
        int remainingMinutes = (int)Math.Round(minutes % 60);
        return $"{hours} Std. {remainingMinutes} Min.";
    }

    /// <summary>
    /// Formats route distance in meters to a human-readable string.
    /// </summary>
    public static string FormatDistance(double distanceMeters)
    {
        if (distanceMeters < 1000)
        {
            return $"{(int)Math.Round(distanceMeters)} m";
        }
        double km = distanceMeters / 1000.0;
        return $"{km:F1} km";
    }

    /// <summary>
    /// Returns the safest maximum close-up zoom resolution.
    /// </summary>
    public static double GetClosestZoomResolution(Mapsui.Map? map)
    {
        try
        {
            var resolutions = map?.Navigator?.Resolutions;
            if (resolutions != null && resolutions.Count > 0)
            {
                return resolutions.Count > SafeResolutionIndex ? resolutions[SafeResolutionIndex] : resolutions.Last();
            }
        }
        catch { }
        return SafeZoomResolution;
    }
}
