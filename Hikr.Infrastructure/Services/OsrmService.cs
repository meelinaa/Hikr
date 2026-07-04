using System.Text.Json;
using Hikr.Application.Ports.Outbound;
using Hikr.Domain.Entities;
using Hikr.Domain.Enums;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Hikr.Infrastructure.Services;

public class OsrmService : IOsrmService
{
    private readonly HttpClient _httpClient;
    private readonly string _routeUrlTemplate;

    public OsrmService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _routeUrlTemplate = configuration["OsrmSettings:RouteUrl"] 
            ?? "http://router.project-osrm.org/route/v1/{profile}/{coordinates}";
    }

    public async Task<Geometry> CalculateGeometryAsync(List<Waypoint> waypoints, Profiles profile, OsrmServices service = OsrmServices.Route)
    {
        var coordinatesList = waypoints
            .Where(w => w.Geometry is Point)
            .Select(w =>
            {
                var pt = (Point)w.Geometry;
                return $"{pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            })
            .ToList();

        if (coordinatesList.Count < 2)
            throw new InvalidOperationException("At least two valid waypoints are required for routing.");

        var coordinatesStr = string.Join(";", coordinatesList);
        var profileStr = profile.ToString().ToLower();

        var url = _routeUrlTemplate
            .Replace("{profile}", profileStr)
            .Replace("{coordinates}", coordinatesStr);

        if (!url.Contains("geometries=geojson"))
        {
            url += url.Contains("?") ? "&geometries=geojson" : "?geometries=geojson";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jsonStr = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(jsonStr);
        var root = document.RootElement;
        
        if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
        {
            var firstRoute = routes[0];
            if (firstRoute.TryGetProperty("geometry", out var geometry))
            {
                var geoJsonStr = geometry.GetRawText();
                var reader = new GeoJsonReader();
                return reader.Read<Geometry>(geoJsonStr);
            }
        }

        throw new Exception("Failed to extract geometry from OSRM response.");
    }
}
