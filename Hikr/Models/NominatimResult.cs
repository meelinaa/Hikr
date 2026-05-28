using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hikr.Models;

/// <summary>
/// Model representing address search results from the Nominatim OpenStreetMap API.
/// </summary>
public class NominatimResult
{
    [JsonPropertyName("lat")]
    public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Lon { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("boundingbox")]
    public List<string>? BoundingBox { get; set; }

    /// <summary>
    /// Determines whether the location represents a city, town, village, or regional administrative boundary.
    /// </summary>
    public bool IsCityOrRegion()
    {
        return Class == "boundary" || 
               (Class == "place" && (Type == "city" || Type == "town" || Type == "village" || Type == "municipality" || Type == "administrative" || Type == "suburb" || Type == "county" || Type == "state" || Type == "country"));
    }
}
