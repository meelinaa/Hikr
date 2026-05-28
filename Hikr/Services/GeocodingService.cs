using System;
using System.Collections.Generic;
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
    public async Task<List<NominatimResult>> SearchAddressAsync(string query, string viewboxParam, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<NominatimResult>();
        }

        try
        {
            EnsureUserAgent();
            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=5&addressdetails=1{viewboxParam}";
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

    private void EnsureUserAgent()
    {
        if (!_httpClient.DefaultRequestHeaders.Contains(UserAgentHeader))
        {
            _httpClient.DefaultRequestHeaders.Add(UserAgentHeader, UserAgentValue);
        }
    }
}
