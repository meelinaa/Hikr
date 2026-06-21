using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mapsui.Nts;
using Mapsui.Projections;
using NetTopologySuite.Geometries;
using Hikr.Models;
using Hikr.Helpers;

namespace Hikr.Services;

/// <summary>
/// Service that interfaces directly with the OpenStreetMap Overpass API
/// to query ambient hiking routes in the surroundings.
/// </summary>
public class OverpassHikingService
{
    private readonly HttpClient _httpClient;
    private const string UserAgentHeader = "User-Agent";
    private const string UserAgentValue = "HikrAppMauiNET10";
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";

    public OverpassHikingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        EnsureUserAgent();
    }

    /// <summary>
    /// Fetches all hiking routes within a specified radius around a coordinate.
    /// </summary>
    /// <param name="latitude">Center latitude (WGS84)</param>
    /// <param name="longitude">Center longitude (WGS84)</param>
    /// <param name="radiusMeters">Search radius in meters</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of parsed hiking routes</returns>
    public async Task<List<HikingRouteModel>> FetchRoutesAroundLocationAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        CancellationToken token)
    {
        try
        {
            EnsureUserAgent();

            // Construct Overpass query to find relation routes
            string latStr = latitude.ToString(CultureInfo.InvariantCulture);
            string lonStr = longitude.ToString(CultureInfo.InvariantCulture);
            string radStr = radiusMeters.ToString(CultureInfo.InvariantCulture);

            string query = $"[out:json][timeout:25];" +
                           $"(" +
                           $"  relation[\"type\"=\"route\"][\"route\"=\"hiking\"](around:{radStr},{latStr},{lonStr});" +
                           $");" +
                           $"out geom;";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });

            var response = await _httpClient.PostAsync(OverpassUrl, content, token);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync(token);
            return await Task.Run(() => ParseOverpassJson(jsonString), token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Overpass API Error: {ex.Message}");
            return new List<HikingRouteModel>();
        }
    }

    private List<HikingRouteModel> ParseOverpassJson(string jsonString)
    {
        var routes = new List<HikingRouteModel>();
        var geometryFactory = new GeometryFactory();

        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            if (!doc.RootElement.TryGetProperty("elements", out var elementsProp) ||
                elementsProp.ValueKind != JsonValueKind.Array)
            {
                return routes;
            }

            foreach (var element in elementsProp.EnumerateArray())
            {
                if (!element.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "relation")
                    continue;

                long id = element.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;

                // Parse tags
                var tags = new Dictionary<string, string>();
                if (element.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var tag in tagsProp.EnumerateObject())
                    {
                        tags[tag.Name] = tag.Value.GetString() ?? string.Empty;
                    }
                }

                string name = tags.TryGetValue("name", out var nameVal) ? nameVal : $"Route #{id}";
                string distanceTag = tags.TryGetValue("distance", out var distVal) ? distVal : string.Empty;

                // Parse way member geometries
                var lineStrings = new List<LineString>();
                double calculatedLength = 0;

                if (element.TryGetProperty("members", out var membersProp) &&
                    membersProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var member in membersProp.EnumerateArray())
                    {
                        if (member.TryGetProperty("type", out var memberType) &&
                            memberType.GetString() == "way" &&
                            member.TryGetProperty("geometry", out var geomProp) &&
                            geomProp.ValueKind == JsonValueKind.Array)
                        {
                            var coords = new List<Coordinate>();
                            double prevLat = 0;
                            double prevLon = 0;
                            bool isFirst = true;

                            foreach (var pt in geomProp.EnumerateArray())
                            {
                                double lat = pt.GetProperty("lat").GetDouble();
                                double lon = pt.GetProperty("lon").GetDouble();

                                // Projected coordinates for Mapsui rendering
                                var (mercatorX, mercatorY) = SphericalMercator.FromLonLat(lon, lat);
                                coords.Add(new Coordinate(mercatorX, mercatorY));

                                // Geodesic length using Haversine
                                if (!isFirst)
                                {
                                    calculatedLength += NavigationCalculator.HaversineDistanceMeters(prevLat, prevLon, lat, lon);
                                }
                                prevLat = lat;
                                prevLon = lon;
                                isFirst = false;
                            }

                            if (coords.Count >= 2)
                            {
                                lineStrings.Add(geometryFactory.CreateLineString(coords.ToArray()));
                            }
                        }
                    }
                }

                if (lineStrings.Count == 0)
                    continue; // Skip relations without way coordinates

                // Assemble geometry
                Geometry combinedGeom = lineStrings.Count == 1
                    ? lineStrings[0]
                    : geometryFactory.CreateMultiLineString(lineStrings.ToArray());

                var feature = new GeometryFeature
                {
                    Geometry = combinedGeom
                };

                routes.Add(new HikingRouteModel
                {
                    Id = id,
                    Name = name,
                    DistanceTag = distanceTag,
                    CalculatedDistanceMeters = calculatedLength,
                    Tags = tags,
                    Feature = feature,
                    Geometry = combinedGeom
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse Overpass response: {ex.Message}");
        }

        return routes;
    }

    private void EnsureUserAgent()
    {
        if (!_httpClient.DefaultRequestHeaders.Contains(UserAgentHeader))
        {
            _httpClient.DefaultRequestHeaders.Add(UserAgentHeader, UserAgentValue);
        }
    }
}
