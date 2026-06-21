using System.Text.Json.Serialization;

namespace Hikr.Models;

/// <summary>Reverse-geocoding response from Nominatim (country lookup).</summary>
public class NominatimReverseResult
{
    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }
}

public class NominatimAddress
{
    [JsonPropertyName("house_number")]
    public string HouseNumber { get; set; } = string.Empty;

    [JsonPropertyName("road")]
    public string Road { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("town")]
    public string Town { get; set; } = string.Empty;

    [JsonPropertyName("village")]
    public string Village { get; set; } = string.Empty;

    [JsonPropertyName("suburb")]
    public string Suburb { get; set; } = string.Empty;

    [JsonPropertyName("postcode")]
    public string Postcode { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = string.Empty;
}
