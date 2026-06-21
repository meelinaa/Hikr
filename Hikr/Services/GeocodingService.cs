using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Hikr.Models;

namespace Hikr.Services;

/// <summary>
/// Service that interfaces with the Nominatim OpenStreetMap API for geocoding and autocomplete suggestions.
/// </summary>
public class GeocodingService
{
    private readonly HttpClient _httpClient;
    private const string UserAgentHeader = "User-Agent";
    private const string UserAgentValue = "HikrAppMauiNET10";

    /// <summary>
    /// Initializes a new instance of the GeocodingService class.
    /// </summary>
    public GeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        EnsureUserAgent();
    }

    /// <summary>
    /// Performs address query lookup on Nominatim and returns suggestions.
    /// </summary>
    public async Task<List<NominatimResult>> SearchAddressAsync(
        string query,
        string viewboxParam,
        string? countryCode,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<NominatimResult>();
        }

        try
        {
            EnsureUserAgent();
            // Viewbox ohne bounded=1: lokale Treffer bevorzugen, Städte im Land bleiben möglich
            string countryParam = string.IsNullOrWhiteSpace(countryCode)
                ? string.Empty
                : $"&countrycodes={Uri.EscapeDataString(countryCode.ToLowerInvariant())}";

            string url =
                $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}" +
                $"&format=json&limit=10&addressdetails=1&dedupe=1&accept-language=de" +
                $"{viewboxParam}{countryParam}";

            var results = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url, token);
            return results ?? new List<NominatimResult>();
        }
        catch (TaskCanceledException)
        {
            return new List<NominatimResult>();
        }
        catch (Exception)
        {
            return new List<NominatimResult>();
        }
    }

    /// <summary>
    /// Resolves the ISO 3166-1 alpha-2 country code for a coordinate via reverse geocoding.
    /// </summary>
    public async Task<string?> ReverseGeocodeCountryCodeAsync(double lat, double lon, CancellationToken token)
    {
        try
        {
            EnsureUserAgent();
            string url =
                $"https://nominatim.openstreetmap.org/reverse?lat={lat.ToString(CultureInfo.InvariantCulture)}" +
                $"&lon={lon.ToString(CultureInfo.InvariantCulture)}&format=json";

            var result = await _httpClient.GetFromJsonAsync<NominatimReverseResult>(url, token);
            var code = result?.Address?.CountryCode;
            return string.IsNullOrWhiteSpace(code) ? null : code.ToLowerInvariant();
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Reverse-geocodes a coordinate via Nominatim API and returns full details as NominatimResult.
    /// </summary>
    public async Task<NominatimResult?> ReverseGeocodeAsync(double lat, double lon, CancellationToken token)
    {
        try
        {
            EnsureUserAgent();
            string url =
                $"https://nominatim.openstreetmap.org/reverse?lat={lat.ToString(CultureInfo.InvariantCulture)}" +
                $"&lon={lon.ToString(CultureInfo.InvariantCulture)}&format=json&accept-language=de";

            return await _httpClient.GetFromJsonAsync<NominatimResult>(url, token);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void EnsureUserAgent()
    {
        if (!_httpClient.DefaultRequestHeaders.Contains(UserAgentHeader))
        {
            _httpClient.DefaultRequestHeaders.Add(UserAgentHeader, UserAgentValue);
        }
    }
}
