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
using Microsoft.Maui.ApplicationModel;

namespace Hikr.ViewModels;

/// <summary>
/// ViewModel for MainPage, managing search suggestions, routing state, navigation, and reactive UI properties.
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly GeocodingService _geocodingService;
    private readonly RoutingService _routingService;
    private readonly GpsService _gpsService;
    private readonly OverpassHikingService _overpassHikingService;
    private CancellationTokenSource? _searchCts;
    private bool _isSelectingSuggestion;
    private double _compassHeading;
    private string? _countryCode;

    // Hiking specific private fields
    private bool _isHikingModeActive;
    private double _searchRadiusKm = 5;
    private string _customRadiusText = string.Empty;
    private bool _showWaymarkedTrails;
    private HikingRouteModel? _selectedHikingRoute;
    private bool _isHikingDetailPanelVisible;
    private bool _isHikingModalVisible;
    private List<KeyValuePair<string, string>> _hikingRouteTags = new();
    private readonly List<HikingRouteModel> _ambientHikingRoutes = new();
    private MPoint? _lastFetchPosition;

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

    // Hiking specific events
    public event Action? HikingModeChanged;
    public event Action? RadiusCircleChanged;
    public event Action<List<HikingRouteModel>>? HikingRoutesLoaded;
    public event Action<HikingRouteModel?>? SelectedHikingRouteChanged;

    /// <summary>
    /// Initializes a new instance of the MainViewModel class.
    /// </summary>
    public MainViewModel(HttpClient httpClient)
    {
        _geocodingService = new GeocodingService(httpClient);
        _routingService = new RoutingService(httpClient);
        _overpassHikingService = new OverpassHikingService(httpClient);
        
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

        ToggleHikingModeCommand = new RelayCommand(execute: ToggleHikingMode);
        SetRadiusCommand = new RelayCommand<double>(execute: SetRadius);
        ToggleWaymarkedTrailsCommand = new RelayCommand(execute: ToggleWaymarkedTrails);
        SelectHikingRouteCommand = new RelayCommand<HikingRouteModel>(execute: SelectHikingRoute);
        StartHikingRouteCommand = new RelayCommand(execute: async () => await ExecuteStartHikingRouteAsync());
        ShowMoreHikingDetailsCommand = new RelayCommand(execute: ShowMoreHikingDetails);
        CloseHikingModalCommand = new RelayCommand(execute: CloseHikingModal);
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

    // Hiking specific commands
    public ICommand ToggleHikingModeCommand { get; }
    public ICommand SetRadiusCommand { get; }
    public ICommand ToggleWaymarkedTrailsCommand { get; }
    public ICommand SelectHikingRouteCommand { get; }
    public ICommand StartHikingRouteCommand { get; }
    public ICommand ShowMoreHikingDetailsCommand { get; }
    public ICommand CloseHikingModalCommand { get; }

    // State properties with Change Notifications
    public double CompassHeading
    {
        get => _compassHeading;
        set => SetProperty(ref _compassHeading, value);
    }

    // Hiking specific public properties
    public bool IsHikingModeActive
    {
        get => _isHikingModeActive;
        set
        {
            if (SetProperty(ref _isHikingModeActive, value))
            {
                HikingModeChanged?.Invoke();
                RadiusCircleChanged?.Invoke();
                if (value)
                {
                    _ = FetchAmbientHikingRoutesAsync();
                }
                else
                 {
                    _ambientHikingRoutes.Clear();
                    SelectedHikingRoute = null;
                    HikingRoutesLoaded?.Invoke(_ambientHikingRoutes);
                }
            }
        }
    }

    public double SearchRadiusKm
    {
        get => _searchRadiusKm;
        set
        {
            if (SetProperty(ref _searchRadiusKm, value))
            {
                RadiusCircleChanged?.Invoke();
                if (IsHikingModeActive)
                {
                    _ = FetchAmbientHikingRoutesAsync();
                }
            }
        }
    }

    public string CustomRadiusText
    {
        get => _customRadiusText;
        set
        {
            if (SetProperty(ref _customRadiusText, value))
            {
                if (double.TryParse(value, out var parsedKm) && parsedKm > 0)
                {
                    SearchRadiusKm = parsedKm;
                }
            }
        }
    }

    public bool ShowWaymarkedTrails
    {
        get => _showWaymarkedTrails;
        set
        {
            if (SetProperty(ref _showWaymarkedTrails, value))
            {
                OnPropertyChanged(nameof(ShowWaymarkedTrails));
            }
        }
    }

    public HikingRouteModel? SelectedHikingRoute
    {
        get => _selectedHikingRoute;
        set
        {
            if (SetProperty(ref _selectedHikingRoute, value))
            {
                SelectedHikingRouteChanged?.Invoke(value);
                if (value != null)
                {
                    PlaceTitle = value.Name;
                    PlaceSubtitle = value.GetFormattedDistance();
                    IsHikingDetailPanelVisible = true;
                }
                else
                {
                    IsHikingDetailPanelVisible = false;
                }
            }
        }
    }

    public bool IsHikingDetailPanelVisible
    {
        get => _isHikingDetailPanelVisible;
        set => SetProperty(ref _isHikingDetailPanelVisible, value);
    }

    public bool IsHikingModalVisible
    {
        get => _isHikingModalVisible;
        set => SetProperty(ref _isHikingModalVisible, value);
    }

    public List<KeyValuePair<string, string>> HikingRouteTags
    {
        get => _hikingRouteTags;
        set => SetProperty(ref _hikingRouteTags, value);
    }

    public List<HikingRouteModel> AmbientHikingRoutes => _ambientHikingRoutes;

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
        set
        {
            if (SetProperty(ref _startPosition, value))
                _ = EnsureCountryCodeAsync(value);
        }
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

    private RouteData? _selectedRoute;

    public RouteData? SelectedRoute
    {
        get => _selectedRoute;
        set => SetProperty(ref _selectedRoute, value);
    }

    public List<RouteData> CurrentRoutes => _currentRoutes;
    public List<MPoint> RoutePoints => _routePoints;
    public string CurrentTransportMode => _currentTransportMode;

    // Viewbox callback supplied by View to restrict Nominatim results to screen viewport
    public Func<string>? ViewboxProvider { get; set; }

    private DateTime _lastRerouteTime = DateTime.MinValue;

    /// <summary>
    /// Processes coordinate and guidance calculation updates when background GPS position ticks.
    /// </summary>
    public async Task ProcessBackgroundGpsUpdateAsync(MPoint mapsuiPosition, bool isRoutingPanelVisible)
    {
        bool isUsingGpsStart = !isRoutingPanelVisible || string.IsNullOrWhiteSpace(StartEntryText) || StartEntryText == "Mein Standort";
        
        if (isUsingGpsStart)
        {
            _startPosition = mapsuiPosition;
            _ = EnsureCountryCodeAsync(mapsuiPosition);
        }

        if (IsHikingModeActive && _startPosition != null)
        {
            if (_lastFetchPosition == null || NavigationCalculator.Distance(_lastFetchPosition, _startPosition) > 500)
            {
                _lastFetchPosition = _startPosition;
                _ = FetchAmbientHikingRoutesAsync();
            }
        }

        if (_destinationPosition != null && isRoutingPanelVisible)
        {
            await BerechneUndZeigeRouteAsync(false);
        }

        if (IsInNavigationMode && _startPosition != null)
        {
            UpdateNavigationGuidance();

            // Check if user strayed from route for automatic rerouting
            if (_routePoints.Count >= 2)
            {
                double minDistance = double.MaxValue;
                for (int i = 0; i < _routePoints.Count; i++)
                {
                    double dist = NavigationCalculator.Distance(_startPosition, _routePoints[i]);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                    }
                }

                // minDistance is in Mercator meters. Threshold of 100 (approx 50-70 real meters depending on latitude).
                // Throttle rerouting to max once every 10 seconds.
                if (minDistance > 100 && (DateTime.UtcNow - _lastRerouteTime).TotalSeconds > 10)
                {
                    _lastRerouteTime = DateTime.UtcNow;
                    await BerechneUndZeigeRouteAsync(false);
                }
            }
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
            SelectedRoute = _currentRoutes[0];
            RoutesCalculated?.Invoke(_currentRoutes, shouldZoom);
        }
    }

    /// <summary>
    /// Calculates and selects the primary active route path.
    /// </summary>
    public void SelectRoute(RouteData selectedRoute)
    {
        SelectedRoute = selectedRoute;
        RouteDurationText = NavigationCalculator.FormatDuration(selectedRoute.Duration) + $" ({NavigationCalculator.FormatDistance(selectedRoute.Distance)})";
        RouteModeIcon = GetTransportIcon(GetOsrmProfile());
    }

    /// <summary>
    /// Updates all navigation turn-by-turn guidance values.
    /// </summary>
    public void UpdateNavigationGuidance()
    {
        var activeRoute = _selectedRoute ?? (_currentRoutes.Count > 0 ? _currentRoutes[0] : null);
        if (activeRoute == null || _routePoints.Count < 2 || _startPosition == null) return;

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

        double distanceMeters = totalMercatorLength > 0 ? (remainingMercatorLength / totalMercatorLength) * activeRoute.Distance : 0;

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

        NavigationStep? upcomingStep = null;
        double nextTurnDistMeters = distanceMeters;
        
        if (activeRoute.Steps != null && activeRoute.Steps.Count > 0)
        {
            for (int s = 0; s < activeRoute.Steps.Count; s++)
            {
                var step = activeRoute.Steps[s];
                if (step.Location == null) continue;
                
                int stepIndex = 0;
                double minStepDist = double.MaxValue;
                for (int i = 0; i < _routePoints.Count; i++)
                {
                    double d = NavigationCalculator.Distance(step.Location, _routePoints[i]);
                    if (d < minStepDist)
                    {
                        minStepDist = d;
                        stepIndex = i;
                    }
                }
                
                if (stepIndex > closestIndex && (step.ManeuverType != "depart" || s > 0))
                {
                    double distToStep = 0;
                    if (closestIndex < _routePoints.Count - 1)
                    {
                        distToStep += NavigationCalculator.Distance(_startPosition, _routePoints[closestIndex + 1]);
                        for (int i = closestIndex + 1; i < stepIndex; i++)
                        {
                            distToStep += NavigationCalculator.Distance(_routePoints[i], _routePoints[i + 1]);
                        }
                    }
                    
                    if (totalMercatorLength > 0)
                    {
                        nextTurnDistMeters = (distToStep / totalMercatorLength) * activeRoute.Distance;
                    }
                    else
                    {
                        nextTurnDistMeters = distToStep * 0.62;
                    }
                    
                    upcomingStep = step;
                    break;
                }
            }
            
            if (upcomingStep == null)
            {
                upcomingStep = activeRoute.Steps[^1];
                nextTurnDistMeters = distanceMeters;
            }
        }

        string instruction = "Dem Straßenverlauf folgen";
        string emoji = "⬆️";

        if (upcomingStep != null)
        {
            instruction = upcomingStep.FormattedInstruction;
            emoji = upcomingStep.Emoji;
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
        await EnsureCountryCodeAsync(_startPosition);
        var list = await _geocodingService.SearchAddressAsync(query, viewbox, _countryCode, CancellationToken.None);
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

        PlaceTitle = gewaehlterOrt.GetPrimaryLabel();
        PlaceSubtitle = gewaehlterOrt.GetSecondaryLabel();
        if (string.IsNullOrWhiteSpace(PlaceSubtitle))
            PlaceSubtitle = "Keine Detailadresse";

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
        var activeRoute = _selectedRoute ?? (_currentRoutes.Count > 0 ? _currentRoutes[0] : null);
        if (activeRoute != null && activeRoute.Feature.Geometry is LineString lineString)
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

    public async Task HandleMapLongClickAsync(double lat, double lon)
    {
        if (_destinationPosition != null) return;

        var targetPosition = SphericalMercator.FromLonLat(lon, lat).ToMPoint();
        
        NominatimResult? resolvedPlace = null;
        try
        {
            resolvedPlace = await _geocodingService.ReverseGeocodeAsync(lat, lon, CancellationToken.None);
        }
        catch {}

        if (resolvedPlace == null)
        {
            var latString = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lonString = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            resolvedPlace = new NominatimResult
            {
                Name = "Markierter Ort",
                DisplayName = $"{lat:F4}, {lon:F4}",
                Lat = latString,
                Lon = lonString
            };
        }

        _destinationPosition = targetPosition;

        if (_startPosition == null)
        {
            _startPosition = SphericalMercator.FromLonLat(9.4797, 51.3127).ToMPoint();
        }

        PlaceTitle = resolvedPlace.GetPrimaryLabel();
        PlaceSubtitle = resolvedPlace.GetSecondaryLabel();
        if (string.IsNullOrWhiteSpace(PlaceSubtitle))
            PlaceSubtitle = "Keine Detailadresse";

        var profile = GetOsrmProfile();
        string durationText = await EstimateTravelTimeAsync(_startPosition, _destinationPosition, profile);
        PlaceDurationText = $"{GetTransportIcon(profile)} {durationText}";

        IsSingleSearchPanelVisible = false;
        IsPlaceDetailPanelVisible = false;
        IsRoutingPanelVisible = true;
        IsRouteInfoPanelVisible = true;

        DestinationEntryText = PlaceTitle;

        DestinationSet?.Invoke(targetPosition, resolvedPlace);

        await BerechneUndZeigeRouteAsync(true);
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

        if (IsHikingModeActive)
        {
            var filtered = _ambientHikingRoutes
                .Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(route => {
                    var centroid = route.Geometry.Centroid;
                    var (lon, lat) = SphericalMercator.ToLonLat(centroid.X, centroid.Y);
                    return new NominatimResult
                    {
                        DisplayName = $"{route.Name} ({route.GetFormattedDistance()})",
                        Name = route.Name,
                        Lat = lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Lon = lon.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Class = "route",
                        Type = "hiking"
                    };
                }).ToList();

            Suggestions = filtered;
            IsSuggestionsListVisible = filtered.Count > 0;
            return;
        }

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(400, token);
            string viewbox = ViewboxProvider?.Invoke() ?? string.Empty;
            await EnsureCountryCodeAsync(_startPosition);
            var list = await _geocodingService.SearchAddressAsync(query, viewbox, _countryCode, token);

            if (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    Suggestions = list;
                    IsSuggestionsListVisible = list.Count > 0;
                });
            }
        });
    }

    private async Task EnsureCountryCodeAsync(MPoint? position)
    {
        if (!string.IsNullOrEmpty(_countryCode) || position == null)
            return;

        var (lon, lat) = SphericalMercator.ToLonLat(position.X, position.Y);
        _countryCode = await _geocodingService.ReverseGeocodeCountryCodeAsync(lat, lon, CancellationToken.None);
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
            await EnsureCountryCodeAsync(_startPosition);
            var list = await _geocodingService.SearchAddressAsync(query, viewbox, _countryCode, token);

            if (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    RoutingSuggestions = list;
                    IsRoutingSuggestionsListVisible = list.Count > 0;
                });
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

    // Hiking command and helper implementations
    public void ToggleHikingMode()
    {
        IsHikingModeActive = !IsHikingModeActive;
    }

    private void SetRadius(double radiusKm)
    {
        SearchRadiusKm = radiusKm;
    }

    private void ToggleWaymarkedTrails()
    {
        ShowWaymarkedTrails = !ShowWaymarkedTrails;
    }

    private void SelectHikingRoute(HikingRouteModel? route)
    {
        SelectedHikingRoute = route;
    }

    private async Task ExecuteStartHikingRouteAsync()
    {
        if (SelectedHikingRoute == null || _startPosition == null) return;
        
        _currentTransportMode = "hike";
        OnPropertyChanged(nameof(CurrentTransportMode));

        // 1. Find the point on the hiking route closest to the user's current position
        Coordinate closestCoord = SelectedHikingRoute.Geometry.Coordinates[0];
        double minDistance = double.MaxValue;
        foreach (var coord in SelectedHikingRoute.Geometry.Coordinates)
        {
            double dist = NavigationCalculator.Distance(_startPosition, new MPoint(coord.X, coord.Y));
            if (dist < minDistance)
            {
                minDistance = dist;
                closestCoord = coord;
            }
        }

        var closestMPoint = new MPoint(closestCoord.X, closestCoord.Y);

        // 2. Calculate the connecting route from user position to the hiking path
        var connectionRoutes = await _routingService.CalculateRouteAsync(_startPosition, closestMPoint, "foot");

        // 3. Combine connection route coordinates and hiking route coordinates
        var combinedCoords = new List<Coordinate>();
        double totalDistance = SelectedHikingRoute.CalculatedDistanceMeters;

        if (connectionRoutes != null && connectionRoutes.Count > 0 && connectionRoutes[0].Feature.Geometry != null)
        {
            combinedCoords.AddRange(connectionRoutes[0].Feature.Geometry.Coordinates);
            totalDistance += connectionRoutes[0].Distance;
        }

        combinedCoords.AddRange(SelectedHikingRoute.Geometry.Coordinates);

        var geometryFactory = new NetTopologySuite.Geometries.GeometryFactory();
        var combinedLineString = geometryFactory.CreateLineString(combinedCoords.ToArray());

        var combinedFeature = new Mapsui.Nts.GeometryFeature
        {
            Geometry = combinedLineString
        };

        // 4. Set as the active route and clear other ambient hiking routes
        _currentRoutes.Clear();
        var routeData = new RouteData
        {
            Feature = combinedFeature,
            Distance = totalDistance,
            Duration = totalDistance / 1.1 // Pedestrian speed roughly 4 km/h (1.1 m/s)
        };
        _currentRoutes.Add(routeData);

        // 5. Hide/clear all other ambient hiking routes from map
        _ambientHikingRoutes.Clear();
        HikingRoutesLoaded?.Invoke(_ambientHikingRoutes);

        var centroid = SelectedHikingRoute.Geometry.Centroid;
        _destinationPosition = new MPoint(centroid.X, centroid.Y);
        
        IsHikingDetailPanelVisible = false;
        IsSingleSearchPanelVisible = false;
        
        ExecuteStartNavigation();
    }

    private void ShowMoreHikingDetails()
    {
        if (SelectedHikingRoute == null) return;
        
        var tagsList = SelectedHikingRoute.Tags
            .Select(t => new KeyValuePair<string, string>(t.Key, t.Value))
            .ToList();
            
        HikingRouteTags = tagsList;
        IsHikingModalVisible = true;
    }

    private void CloseHikingModal()
    {
        IsHikingModalVisible = false;
    }

    public async Task FetchAmbientHikingRoutesAsync()
    {
        if (_startPosition == null) return;
        
        var (lon, lat) = SphericalMercator.ToLonLat(_startPosition.X, _startPosition.Y);
        double radiusMeters = SearchRadiusKm * 1000.0;
        
        var routes = await _overpassHikingService.FetchRoutesAroundLocationAsync(lat, lon, radiusMeters, CancellationToken.None);
        
        _ambientHikingRoutes.Clear();
        _ambientHikingRoutes.AddRange(routes);
        
        HikingRoutesLoaded?.Invoke(_ambientHikingRoutes);
    }
}
