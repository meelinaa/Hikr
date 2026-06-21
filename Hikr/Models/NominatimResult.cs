using System.Collections.Generic;
using System.Linq;
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

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("boundingbox")]
    public List<string>? BoundingBox { get; set; }

    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }

    /// <summary>
    /// Determines whether the location represents a city, town, village, or regional administrative boundary.
    /// </summary>
    public bool IsCityOrRegion()
    {
        return Class == "boundary" ||
               (Class == "place" && (Type == "city" || Type == "town" || Type == "village" ||
                Type == "municipality" || Type == "administrative" || Type == "suburb" ||
                Type == "county" || Type == "state" || Type == "country"));
    }

    public bool IsStreetAddress()
    {
        return Address != null && !string.IsNullOrWhiteSpace(Address.Road);
    }

    /// <summary>Primary label for list display (name, street, or city).</summary>
    public string GetPrimaryLabel()
    {
        if (!string.IsNullOrWhiteSpace(Name))
            return Name.Trim();

        if (IsStreetAddress())
        {
            var street = Address!.Road.Trim();
            if (!string.IsNullOrWhiteSpace(Address.HouseNumber))
                street += " " + Address.HouseNumber.Trim();
            return street;
        }

        var locality = GetLocality();
        if (!string.IsNullOrWhiteSpace(locality))
            return locality;

        if (!string.IsNullOrWhiteSpace(DisplayName))
            return DisplayName.Split(',')[0].Trim();

        return "Unbekannter Ort";
    }

    /// <summary>Secondary label (area, postcode, region).</summary>
    public string GetSecondaryLabel()
    {
        var parts = new List<string>();

        if (IsStreetAddress() && Address != null)
        {
            var area = FirstNonEmpty(Address.Suburb, Address.City, Address.Town, Address.Village);
            if (!string.IsNullOrWhiteSpace(area))
                parts.Add(area);
            if (!string.IsNullOrWhiteSpace(Address.Postcode))
                parts.Add(Address.Postcode);
        }
        else if (IsCityOrRegion())
        {
            if (Address != null)
            {
                if (!string.IsNullOrWhiteSpace(Address.State))
                    parts.Add(Address.State);
                if (!string.IsNullOrWhiteSpace(Address.Postcode))
                    parts.Add(Address.Postcode);
            }
        }
        else
        {
            if (Address != null)
            {
                var area = FirstNonEmpty(Address.Suburb, Address.City, Address.Town, Address.Village);
                if (!string.IsNullOrWhiteSpace(area))
                    parts.Add(area);
                if (!string.IsNullOrWhiteSpace(Address.Road) &&
                    !GetPrimaryLabel().Contains(Address.Road, System.StringComparison.OrdinalIgnoreCase))
                    parts.Add(Address.Road);
            }
            else if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                var segments = DisplayName.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Skip(1)
                    .Take(2);
                parts.AddRange(segments);
            }
        }

        return string.Join(", ", parts.Distinct());
    }

    /// <summary>Icon reflecting result type: city, address, or place.</summary>
    public string GetResultIcon()
    {
        if (IsCityOrRegion())
            return "🏙️";
        if (IsStreetAddress())
            return "🏠";
        if (Class is "railway" or "aeroway")
            return "🚉";
        if (Class is "amenity" or "tourism" or "shop" or "leisure" or "historic")
            return "📍";
        return "📍";
    }

    private string GetLocality()
        => Address == null
            ? string.Empty
            : FirstNonEmpty(Address.City, Address.Town, Address.Village, Address.Suburb);

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
