using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using NetTopologySuite.Geometries;
using Hikr.Models;

namespace Hikr.Services;

/// <summary>
/// Service that interfaces with OpenStreetMap's OSRM routing service to retrieve route paths and geometries.
/// </summary>
public class RoutingService
{
    private readonly HttpClient _httpClient;
    private const string UserAgentHeader = "User-Agent";
    private const string UserAgentValue = "HikrAppMauiNET10";

    /// <summary>
    /// Initializes a new instance of the RoutingService class.
    /// </summary>
    public RoutingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        EnsureUserAgent();
    }

    /// <summary>
    /// Builds the OSRM URL based on selected transport profile.
    /// </summary>
    public string GetOsrmUrl(string startCoords, string destCoords, string mode, bool isEstimate)
    {
        string subDomain = mode switch
        {
            "car" => "routed-car",
            "bike" => "routed-bike",
            "foot" => "routed-foot",
            "hike" => "routed-foot",
            _ => "routed-car"
        };

        string profile = mode switch
        {
            "car" => "driving",
            "bike" => "bicycle",
            "foot" => "foot",
            "hike" => "foot",
            _ => "driving"
        };

        string overview = isEstimate ? "false" : "full";
        string alternatives = isEstimate ? "false" : "true";

        return $"https://routing.openstreetmap.de/{subDomain}/route/v1/{profile}/{startCoords};{destCoords}?overview={overview}&geometries=geojson&alternatives={alternatives}";
    }

    /// <summary>
    /// Calculates the route alternatives from a start to a destination coordinates in Spherical Mercator.
    /// </summary>
    public async Task<List<RouteData>> CalculateRouteAsync(MPoint start, MPoint destination, string transportMode)
    {
        var routesList = new List<RouteData>();
        try
        {
            var startLonLat = SphericalMercator.ToLonLat(start.X, start.Y);
            var destLonLat = SphericalMercator.ToLonLat(destination.X, destination.Y);

            string startLon = startLonLat.lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string startLat = startLonLat.lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destLon = destLonLat.lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destLat = destLonLat.lat.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string url = GetOsrmUrl($"{startLon},{startLat}", $"{destLon},{destLat}", transportMode, false);
            EnsureUserAgent();

            string responseString = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
            {
                for (int i = 0; i < routes.GetArrayLength(); i++)
                {
                    var route = routes[i];
                    if (route.TryGetProperty("geometry", out var geometry) && geometry.TryGetProperty("coordinates", out var coords))
                    {
                        var coordinates = new List<Coordinate>();
                        foreach (var coord in coords.EnumerateArray())
                        {
                            double lon = coord[0].GetDouble();
                            double lat = coord[1].GetDouble();
                            var mercator = SphericalMercator.FromLonLat(lon, lat);
                            coordinates.Add(new Coordinate(mercator.x, mercator.y));
                        }

                        if (coordinates.Count > 1)
                        {
                            var lineString = new LineString(coordinates.ToArray());
                            var routeFeature = new GeometryFeature(lineString);

                            double distance = route.TryGetProperty("distance", out var distProp) ? distProp.GetDouble() : 0;
                            double duration = route.TryGetProperty("duration", out var durProp) ? durProp.GetDouble() : 0;

                            routesList.Add(new RouteData
                            {
                                Feature = routeFeature,
                                Distance = distance,
                                Duration = duration
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OSRM Route Calculation Error: {ex.Message}");
        }

        return routesList;
    }

    private void EnsureUserAgent()
    {
        if (!_httpClient.DefaultRequestHeaders.Contains(UserAgentHeader))
        {
            _httpClient.DefaultRequestHeaders.Add(UserAgentHeader, UserAgentValue);
        }
    }
}
