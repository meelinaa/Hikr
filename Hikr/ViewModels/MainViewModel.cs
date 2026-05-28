using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using System.Text.Json;
using NetTopologySuite.Geometries;
using Hikr.Helpers;
using Hikr.Models;
using Hikr.Services;
using Hikr.Core;

namespace Hikr.ViewModels;

/// <summary>
/// ViewModel for MainPage, managing search suggestions, routing state, navigation, and reactive UI properties.
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly GeocodingService _geocodingService;
    private readonly RoutingService _routingService;
    private readonly GpsService _gpsService;
    private CancellationTokenSource? _searchCts;
    private bool _isSelectingSuggestion;
    private double _compassHeading;

    // View state fields
    private string _searchBarText = string.Empty;
    private string _startEntryText = "Mein Standort";
    private string _destinationEntryText = string.Empty;
    private List<NominatimResult> _suggestions = new();
    private List<NominatimResult> _routingSuggestions = new();
    private bool _isSuggestionsListVisible;
    private bool _isRoutingSuggestionsListVisible;

    // Panel visibility fields
    private bool _isSingleSearchPanelVisible = true;
    private bool _isRoutingPanelVisible;
    private bool _isPlaceDetailPanelVisible;
    private bool _isRouteInfoPanelVisible;
    private bool _isNavigationBannerVisible;
    private bool _isNavigationFooterVisible;
    private bool _isRecenterNavVisible;

    // Detail properties
    private string _placeTitle = "Zielort";
    private string _placeSubtitle = "Adresse...";
    private string _placeDurationText = "🚶 -- min";
    private string _routeDurationText = "-- min (-- km)";
    private string _routeModeIcon = "🚗";

    // Nav-guidance properties
    private string _navTimeLeftText = "-- Min. verbleibend";
    private string _navDistanceLeftText = "-- km verbleibend";
    private string _navInstructionIcon = "⬆️";
    private string _navInstructionText = "Dem Straßenverlauf folgen";
    private string _navDistanceText = "In 100 Metern";

    // Domain fields
    private MPoint? _startPosition;
    private MPoint? _destinationPosition;
    private string _currentTransportMode = "car";
    private bool _isInNavigationMode;
    private string _activeSearchField = "destination";
    private readonly List<RouteData> _currentRoutes = new();
    private readonly List<MPoint> _routePoints = new();

    /// <summary>
    /// Event triggered when a destination and place are selected.
    /// </summary>
    public event Action<MPoint, NominatimResult>? DestinationSet;

    /// <summary>
    /// Event triggered when routes are calculated. First parameter is routes, second parameter is whether to zoom.
    /// </summary>
    public event Action<List<RouteData>, bool>? RoutesCalculated;

    /// <summary>
    /// Event triggered when navigation mode begins.
    /// </summary>
    public event Action<MPoint>? NavigationStarted;

    /// <summary>
    /// Event triggered when navigation mode ends.
    /// </summary>
    public event Action? NavigationExited;

    /// <summary>
    /// Event triggered when navigation recenter is requested.
    /// </summary>
    public event Action? RecenterNavRequested;

    /// <summary>
    /// Event triggered when general GPS locator centering is requested.
    /// </summary>
    public event Action? RecenterGpsRequested;

    /// <summary>
    /// Event triggered when routing is fully cleared/exited.
    /// </summary>
    public event Action? RoutingExited;

    /// <summary>
    /// Event triggered when background GPS location updates occur.
    /// </summary>
    public event Action<MPoint>? GpsPositionUpdated;

    /// <summary>
    /// Initializes a new instance of the MainViewModel class.
    /// </summary>
    public MainViewModel(HttpClient httpClient)
    {
        _geocodingService = new GeocodingService(httpClient);
        _routingService = new RoutingService(httpClient);
        
        _gpsService = new GpsService();
        _gpsService.CompassReadingChanged += (heading) => CompassHeading = heading;
        _gpsService.StartCompass();

        // Command bindings
        SearchCommand = new RelayCommand<string>(execute: async (query) => await ExecuteSearchAsync(query));
        SelectSuggestionCommand = new RelayCommand<NominatimResult>(execute: async (ort) => await ExecuteSelectSuggestionAsync(ort));
        ExitRoutingCommand = new RelayCommand(execute: ExecuteExitRouting);
        DirectionsCommand = new RelayCommand(execute: ExecuteDirections);
        StartRouteCommand = new RelayCommand(execute: async () => await ExecuteStartRouteAsync());
        StartNavigationCommand = new RelayCommand(execute: ExecuteStartNavigation);
        ExitNavigationCommand = new RelayCommand(execute: ExecuteExitNavigation);
        RecenterNavCommand = new RelayCommand(execute: ExecuteRecenterNav);
        GPSButtonClickedCommand = new RelayCommand(execute: ExecuteGPSButtonClicked);
        TransportModeCommand = new RelayCommand<string>(execute: async (mode) => await ExecuteTransportModeAsync(mode));
        SwapLocationsCommand = new RelayCommand(execute: async () => await ExecuteSwapLocationsAsync());
        StartEntryFocusedCommand = new RelayCommand(execute: () => ActiveSearchField = "start");
        DestinationEntryFocusedCommand = new RelayCommand(execute: () => ActiveSearchField = "destination");
    }

    // Public Commands
    public ICommand SearchCommand { get; }
    public ICommand SelectSuggestionCommand { get; }
    public ICommand ExitRoutingCommand { get; }
    public ICommand DirectionsCommand { get; }
    public ICommand StartRouteCommand { get; }
    public ICommand StartNavigationCommand { get; }
    public ICommand ExitNavigationCommand { get; }
    public ICommand RecenterNavCommand { get; }
    public ICommand GPSButtonClickedCommand { get; }
    public ICommand TransportModeCommand { get; }
    public ICommand SwapLocationsCommand { get; }
    public ICommand StartEntryFocusedCommand { get; }
    public ICommand DestinationEntryFocusedCommand { get; }

    // State properties with Change Notifications
    public double CompassHeading
    {
        get => _compassHeading;
        set => SetProperty(ref _compassHeading, value);
    }

    public string SearchBarText
    {
        get => _searchBarText;
        set
        {
            if (SetProperty(ref _searchBarText, value))
            {
                OnSearchBarTextChanged(value);
            }
        }
    }

    public string StartEntryText
    {
        get => _startEntryText;
        set
        {
            if (SetProperty(ref _startEntryText, value))
            {
                OnStartEntryTextChanged(value);
            }
        }
    }

    public string DestinationEntryText
    {
        get => _destinationEntryText;
        set
        {
            if (SetProperty(ref _destinationEntryText, value))
            {
                OnDestinationEntryTextChanged(value);
            }
        }
    }

    public List<NominatimResult> Suggestions
    {
        get => _suggestions;
        set => SetProperty(ref _suggestions, value);
    }

    public List<NominatimResult> RoutingSuggestions
    {
        get => _routingSuggestions;
        set => SetProperty(ref _routingSuggestions, value);
    }

    public bool IsSuggestionsListVisible
    {
        get => _isSuggestionsListVisible;
        set => SetProperty(ref _isSuggestionsListVisible, value);
    }

    public bool IsRoutingSuggestionsListVisible
    {
        get => _isRoutingSuggestionsListVisible;
        set => SetProperty(ref _isRoutingSuggestionsListVisible, value);
    }

    public bool IsSingleSearchPanelVisible
    {
        get => _isSingleSearchPanelVisible;
        set => SetProperty(ref _isSingleSearchPanelVisible, value);
    }

    public bool IsRoutingPanelVisible
    {
        get => _isRoutingPanelVisible;
        set => SetProperty(ref _isRoutingPanelVisible, value);
    }

    public bool IsPlaceDetailPanelVisible
    {
        get => _isPlaceDetailPanelVisible;
        set => SetProperty(ref _isPlaceDetailPanelVisible, value);
    }

    public bool IsRouteInfoPanelVisible
    {
        get => _isRouteInfoPanelVisible;
        set => SetProperty(ref _isRouteInfoPanelVisible, value);
    }

    public bool IsNavigationBannerVisible
    {
        get => _isNavigationBannerVisible;
        set => SetProperty(ref _isNavigationBannerVisible, value);
    }

    public bool IsNavigationFooterVisible
    {
        get => _isNavigationFooterVisible;
        set => SetProperty(ref _isNavigationFooterVisible, value);
    }

    public bool IsRecenterNavVisible
    {
        get => _isRecenterNavVisible;
        set => SetProperty(ref _isRecenterNavVisible, value);
    }

    public string PlaceTitle
    {
        get => _placeTitle;
        set => SetProperty(ref _placeTitle, value);
    }

    public string PlaceSubtitle
    {
        get => _placeSubtitle;
        set => SetProperty(ref _placeSubtitle, value);
    }

    public string PlaceDurationText
    {
        get => _placeDurationText;
        set => SetProperty(ref _placeDurationText, value);
    }

    public string RouteDurationText
    {
        get => _routeDurationText;
        set => SetProperty(ref _routeDurationText, value);
    }

    public string RouteModeIcon
    {
        get => _routeModeIcon;
        set => SetProperty(ref _routeModeIcon, value);
    }

    public string NavTimeLeftText
    {
        get => _navTimeLeftText;
        set => SetProperty(ref _navTimeLeftText, value);
    }

    public string NavDistanceLeftText
    {
        get => _navDistanceLeftText;
        set => SetProperty(ref _navDistanceLeftText, value);
    }

    public string NavInstructionIcon
    {
        get => _navInstructionIcon;
        set => SetProperty(ref _navInstructionIcon, value);
    }

    public string NavInstructionText
    {
        get => _navInstructionText;
        set => SetProperty(ref _navInstructionText, value);
    }

    public string NavDistanceText
    {
        get => _navDistanceText;
        set => SetProperty(ref _navDistanceText, value);
    }

    // Domain Properties
    public MPoint? StartPosition
    {
        get => _startPosition;
        set => SetProperty(ref _startPosition, value);
    }

    public MPoint? DestinationPosition
    {
        get => _destinationPosition;
        set => SetProperty(ref _destinationPosition, value);
    }

    public bool IsInNavigationMode
    {
        get => _isInNavigationMode;
        set => SetProperty(ref _isInNavigationMode, value);
    }

    public string ActiveSearchField
    {
        get => _activeSearchField;
        set => SetProperty(ref _activeSearchField, value);
    }

    public List<RouteData> CurrentRoutes => _currentRoutes;
    public List<MPoint> RoutePoints => _routePoints;
    public string CurrentTransportMode => _currentTransportMode;

    // Viewbox callback supplied by View to restrict Nominatim results to screen viewport
    public Func<string>? ViewboxProvider { get; set; }

    /// <summary>
    /// Processes coordinate and guidance calculation updates when background GPS position ticks.
    /// </summary>
    public async Task ProcessBackgroundGpsUpdateAsync(MPoint mapsuiPosition, bool isRoutingPanelVisible)
    {
        bool isUsingGpsStart = !isRoutingPanelVisible || string.IsNullOrWhiteSpace(StartEntryText) || StartEntryText == "Mein Standort";
        
        if (isUsingGpsStart)
        {
            _startPosition = mapsuiPosition;
        }

        if (_destinationPosition != null && isRoutingPanelVisible)
        {
            await BerechneUndZeigeRouteAsync(false);
        }

        if (IsInNavigationMode && _startPosition != null)
        {
            UpdateNavigationGuidance();
        }

        GpsPositionUpdated?.Invoke(mapsuiPosition);
    }

    /// <summary>
    /// Retrieves the current location asynchronously using the GpsService.
    /// </summary>
    public async Task<MPoint?> GetCurrentLocationAsync()
    {
        var location = await _gpsService.GetCurrentLocationAsync();
        if (location != null)
        {
            return SphericalMercator.FromLonLat(location.Longitude, location.Latitude).ToMPoint();
        }
        return null;
    }

    /// <summary>
    /// Calculates the route path between start and destination coordinates.
    /// </summary>
    public async Task BerechneUndZeigeRouteAsync(bool shouldZoom = false)
    {
        if (_startPosition == null || _destinationPosition == null) return;

        var routes = await _routingService.CalculateRouteAsync(_startPosition, _destinationPosition, _currentTransportMode);
        _currentRoutes.Clear();
        _currentRoutes.AddRange(routes);

        if (_currentRoutes.Count > 0)
        {
            RoutesCalculated?.Invoke(_currentRoutes, shouldZoom);
        }
    }

    /// <summary>
    /// Calculates and selects the primary active route path.
    /// </summary>
    public void SelectRoute(RouteData selectedRoute)
    {
        RouteDurationText = NavigationCalculator.FormatDuration(selectedRoute.Duration) + $" ({NavigationCalculator.FormatDistance(selectedRoute.Distance)})";
        RouteModeIcon = GetTransportIcon(GetOsrmProfile());
    }

    /// <summary>
    /// Updates all navigation turn-by-turn guidance values.
    /// </summary>
    public void UpdateNavigationGuidance()
    {
        if (_routePoints.Count < 2 || _startPosition == null) return;

        int closestIndex = 0;
        double minDistance = double.MaxValue;

        for (int i = 0; i < _routePoints.Count; i++)
        {
            double dist = NavigationCalculator.Distance(_startPosition, _routePoints[i]);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        double totalMercatorLength = 0;
        for (int i = 0; i < _routePoints.Count - 1; i++)
        {
            totalMercatorLength += NavigationCalculator.Distance(_routePoints[i], _routePoints[i + 1]);
        }

        double remainingMercatorLength = 0;
        if (closestIndex < _routePoints.Count - 1)
        {
            remainingMercatorLength += NavigationCalculator.Distance(_startPosition, _routePoints[closestIndex + 1]);
            for (int i = closestIndex + 1; i < _routePoints.Count - 1; i++)
            {
                remainingMercatorLength += NavigationCalculator.Distance(_routePoints[i], _routePoints[i + 1]);
            }
        }

        double distanceMeters = 0;
        if (totalMercatorLength > 0 && _currentRoutes.Count > 0)
        {
            distanceMeters = (remainingMercatorLength / totalMercatorLength) * _currentRoutes[0].Distance;
        }

        NavDistanceLeftText = NavigationCalculator.FormatDistance(distanceMeters) + " verbleibend";
        
        double speedMps = _currentTransportMode switch
        {
            "car" => 13.8,
            "bike" => 4.2,
            "foot" => 1.4,
            "hike" => 1.1,
            _ => 1.4
        };
        double durationSeconds = distanceMeters / speedMps;
        NavTimeLeftText = NavigationCalculator.FormatDuration(durationSeconds) + " verbleibend";

        string instruction = "Dem Straßenverlauf folgen";
        string emoji = "⬆️";
        double nextTurnDistMeters = distanceMeters;

        int searchLimit = Math.Min(closestIndex + 25, _routePoints.Count);
        double distToTurn = 0;
        
        if (closestIndex < _routePoints.Count - 1)
        {
            distToTurn += NavigationCalculator.Distance(_startPosition, _routePoints[closestIndex + 1]);
        }

        for (int i = closestIndex + 1; i < searchLimit - 1; i++)
        {
            distToTurn += NavigationCalculator.Distance(_routePoints[i], _routePoints[i + 1]);
            
            double b1 = NavigationCalculator.Bearing(_routePoints[i - 1], _routePoints[i]);
            double b2 = NavigationCalculator.Bearing(_routePoints[i], _routePoints[i + 1]);
            double diff = b2 - b1;
            
            while (diff > 180) diff -= 360;
            while (diff < -180) diff += 360;

            if (Math.Abs(diff) > 25)
            {
                if (totalMercatorLength > 0 && _currentRoutes.Count > 0)
                {
                    nextTurnDistMeters = (distToTurn / totalMercatorLength) * _currentRoutes[0].Distance;
                }
                else
                {
                    nextTurnDistMeters = distToTurn * 0.62;
                }

                if (diff < 0)
                {
                    instruction = "Links abbiegen";
                    emoji = "⬅️";
                }
                else
                {
                    instruction = "Rechts abbiegen";
                    emoji = "➡️";
                }
                break;
            }
        }

        if (closestIndex >= _routePoints.Count - 2 || distanceMeters < 15)
        {
            instruction = "Sie haben Ihr Ziel erreicht!";
            emoji = "🏁";
            nextTurnDistMeters = 0;
        }

        NavInstructionIcon = emoji;
        NavInstructionText = instruction;
        NavDistanceText = nextTurnDistMeters > 0 ? $"In {(int)nextTurnDistMeters} Metern" : "Ziel erreicht";
    }

    private async Task ExecuteSearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        IsSuggestionsListVisible = false;

        string viewbox = ViewboxProvider?.Invoke() ?? string.Empty;
        var list = await _geocodingService.SearchAddressAsync(query, viewbox, CancellationToken.None);
        if (list.Count > 0)
        {
            var target = list[0];
            double lat = double.Parse(target.Lat, System.Globalization.CultureInfo.InvariantCulture);
            double lon = double.Parse(target.Lon, System.Globalization.CultureInfo.InvariantCulture);
            var zielPosition = SphericalMercator.FromLonLat(lon, lat).ToMPoint();

            await SetDestinationAndRouteAsync(zielPosition, target);
        }
    }

    private async Task ExecuteSelectSuggestionAsync(NominatimResult gewaehlterOrt)
    {
        if (gewaehlterOrt == null) return;
        _isSelectingSuggestion = true;
        _searchCts?.Cancel();

        try
        {
            IsSuggestionsListVisible = false;
            Suggestions = new List<NominatimResult>();

            _searchBarText = gewaehlterOrt.DisplayName;
            OnPropertyChanged(nameof(SearchBarText));

            double lat = double.Parse(gewaehlterOrt.Lat, System.Globalization.CultureInfo.InvariantCulture);
            double lon = double.Parse(gewaehlterOrt.Lon, System.Globalization.CultureInfo.InvariantCulture);
            var zielPosition = SphericalMercator.FromLonLat(lon, lat).ToMPoint();

            await SetDestinationAndRouteAsync(zielPosition, gewaehlterOrt);
        }
        finally
        {
            _isSelectingSuggestion = false;
        }
    }

    private async Task SetDestinationAndRouteAsync(MPoint zielPosition, NominatimResult gewaehlterOrt)
    {
        _destinationPosition = zielPosition;

        if (_startPosition == null)
        {
            _startPosition = SphericalMercator.FromLonLat(9.4797, 51.3127).ToMPoint();
        }

        var nameParts = gewaehlterOrt.DisplayName.Split(',');
        PlaceTitle = nameParts[0].Trim();
        PlaceSubtitle = nameParts.Length > 1 ? string.Join(",", nameParts.Skip(1)).Trim() : "Keine Detailadresse";

        var profile = GetOsrmProfile();
        string durationText = await EstimateTravelTimeAsync(_startPosition, _destinationPosition, profile);
        PlaceDurationText = $"{GetTransportIcon(profile)} {durationText}";

        IsRoutingPanelVisible = false;
        IsRouteInfoPanelVisible = false;
        IsPlaceDetailPanelVisible = true;

        DestinationSet?.Invoke(zielPosition, gewaehlterOrt);
    }

    private async Task<string> EstimateTravelTimeAsync(MPoint start, MPoint destination, string profile)
    {
        try
        {
            var startLonLat = SphericalMercator.ToLonLat(start.X, start.Y);
            var destLonLat = SphericalMercator.ToLonLat(destination.X, destination.Y);

            string startLon = startLonLat.lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string startLat = startLonLat.lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destLon = destLonLat.lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destLat = destLonLat.lat.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string url = _routingService.GetOsrmUrl($"{startLon},{startLat}", $"{destLon},{destLat}", _currentTransportMode, true);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "HikrAppMauiNET10");
            string responseString = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
            {
                var route = routes[0];
                if (route.TryGetProperty("duration", out var durationProp))
                {
                    double durationSeconds = durationProp.GetDouble();
                    int minutes = (int)Math.Round(durationSeconds / 60.0);
                    return $"{minutes} Min.";
                }
            }
        }
        catch { }
        return "-- Min.";
    }

    private void ExecuteExitRouting()
    {
        IsRoutingPanelVisible = false;
        IsRouteInfoPanelVisible = false;
        IsPlaceDetailPanelVisible = false;
        IsRoutingSuggestionsListVisible = false;
        IsSingleSearchPanelVisible = true;

        _currentRoutes.Clear();
        _destinationPosition = null;

        _isSelectingSuggestion = true;
        SearchBarText = string.Empty;
        StartEntryText = "Mein Standort";
        DestinationEntryText = string.Empty;
        _isSelectingSuggestion = false;

        RoutingExited?.Invoke();
    }

    private void ExecuteDirections()
    {
        IsPlaceDetailPanelVisible = false;
        IsSingleSearchPanelVisible = false;
        IsRoutingPanelVisible = true;
        IsRouteInfoPanelVisible = true;

        DestinationEntryText = PlaceTitle;
        _ = BerechneUndZeigeRouteAsync(true);
    }

    private async Task ExecuteStartRouteAsync()
    {
        IsPlaceDetailPanelVisible = false;
        IsRouteInfoPanelVisible = true;
        await BerechneUndZeigeRouteAsync(true);
    }

    private void ExecuteStartNavigation()
    {
        IsRoutingPanelVisible = false;
        IsRouteInfoPanelVisible = false;
        IsPlaceDetailPanelVisible = false;
        IsSingleSearchPanelVisible = false;

        IsNavigationBannerVisible = true;
        IsNavigationFooterVisible = true;
        IsRecenterNavVisible = true;

        IsInNavigationMode = true;

        _routePoints.Clear();
        if (_currentRoutes.Count > 0 && _currentRoutes[0].Feature.Geometry is LineString lineString)
        {
            foreach (var coord in lineString.Coordinates)
            {
                _routePoints.Add(new MPoint(coord.X, coord.Y));
            }
        }

        if (_startPosition != null)
        {
            NavigationStarted?.Invoke(_startPosition);
        }

        UpdateNavigationGuidance();
    }

    private void ExecuteExitNavigation()
    {
        IsInNavigationMode = false;

        IsNavigationBannerVisible = false;
        IsNavigationFooterVisible = false;
        IsRecenterNavVisible = false;

        IsRoutingPanelVisible = true;
        IsRouteInfoPanelVisible = true;

        NavigationExited?.Invoke();
    }

    private void ExecuteRecenterNav()
    {
        if (_startPosition != null)
        {
            RecenterNavRequested?.Invoke();
            UpdateNavigationGuidance();
        }
    }

    private void ExecuteGPSButtonClicked()
    {
        RecenterGpsRequested?.Invoke();
    }

    private async Task ExecuteTransportModeAsync(string mode)
    {
        _currentTransportMode = mode;
        OnPropertyChanged(nameof(CurrentTransportMode));
        await BerechneUndZeigeRouteAsync(true);
    }

    private async Task ExecuteSwapLocationsAsync()
    {
        var temp = _startPosition;
        _startPosition = _destinationPosition;
        _destinationPosition = temp;

        var tempText = StartEntryText;
        _isSelectingSuggestion = true;
        StartEntryText = DestinationEntryText;
        DestinationEntryText = tempText;
        _isSelectingSuggestion = false;

        await BerechneUndZeigeRouteAsync(true);
    }

    private void OnSearchBarTextChanged(string query)
    {
        if (_isSelectingSuggestion) return;

        if (string.IsNullOrEmpty(query))
        {
            _destinationPosition = null;
            IsPlaceDetailPanelVisible = false;
            IsRouteInfoPanelVisible = false;
            IsSuggestionsListVisible = false;
            Suggestions = new List<NominatimResult>();
            RoutingExited?.Invoke();
            return;
        }

        if (query.Length < 3)
        {
            IsSuggestionsListVisible = false;
            Suggestions = new List<NominatimResult>();
            return;
        }

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(400, token);
            string viewbox = ViewboxProvider?.Invoke() ?? string.Empty;
            var list = await _geocodingService.SearchAddressAsync(query, viewbox, token);
            
            if (!token.IsCancellationRequested)
            {
                Suggestions = list;
                IsSuggestionsListVisible = list.Count > 0;
            }
        });
    }

    private void OnStartEntryTextChanged(string query)
    {
        if (_isSelectingSuggestion || query == "Mein Standort")
        {
            IsRoutingSuggestionsListVisible = false;
            RoutingSuggestions = new List<NominatimResult>();
            return;
        }
        SucheUndZeigeRoutingVorschlaege(query);
    }

    private void OnDestinationEntryTextChanged(string query)
    {
        if (_isSelectingSuggestion) return;
        SucheUndZeigeRoutingVorschlaege(query);
    }

    private void SucheUndZeigeRoutingVorschlaege(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            IsRoutingSuggestionsListVisible = false;
            RoutingSuggestions = new List<NominatimResult>();
            return;
        }

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(400, token);
            string viewbox = ViewboxProvider?.Invoke() ?? string.Empty;
            var list = await _geocodingService.SearchAddressAsync(query, viewbox, token);
            
            if (!token.IsCancellationRequested)
            {
                RoutingSuggestions = list;
                IsRoutingSuggestionsListVisible = list.Count > 0;
            }
        });
    }

    private string GetOsrmProfile()
    {
        return _currentTransportMode switch
        {
            "car" => "driving",
            "bike" => "bicycle",
            "foot" => "foot",
            "hike" => "foot",
            _ => "driving"
        };
    }

    private string GetTransportIcon(string profile)
    {
        return profile switch
        {
            "driving" => "🚗",
            "bicycle" => "🚲",
            "foot" => "🚶",
            _ => "🚶"
        };
    }
}
