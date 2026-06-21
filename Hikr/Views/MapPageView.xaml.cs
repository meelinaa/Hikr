using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;
using Hikr.Helpers;
using Hikr.Models;
using Hikr.Services;
using Hikr.ViewModels;
using Hikr.Core;
using Color = Microsoft.Maui.Graphics.Color;

namespace Hikr.Views;

/// <summary>
/// Map ContentView – hosts the MapControl and all map overlay panels.
/// </summary>
public partial class MapPageView : ContentView
{
    private readonly WritableLayer _locationLayer = new() { Name = "Location Layer" };
    private readonly WritableLayer _searchLayer   = new() { Name = "Search Layer" };
    private readonly WritableLayer _routeLayer    = new() { Name = "Route Layer" };
    private readonly WritableLayer _ambientRoutesLayer = new() { Name = "Ambient Routes Layer" };
    private readonly WritableLayer _radiusCircleLayer = new() { Name = "Radius Circle Layer" };
    private ILayer? _tileLayer;
    private ILayer? _waymarkedTrailsLayer;
    private MainViewModel _viewModel = null!;
    private IDispatcherTimer? _locationUpdateTimer;
    private DateTime _pressStart;
    private Mapsui.Manipulations.ScreenPosition? _pressPosition;

    /// <summary>Fired when the user taps the search pill – ShellHostPage opens the overlay.</summary>
    public event EventHandler? SearchRequested;

    /// <summary>Exposes the map ViewModel so ShellHostPage can trigger searches.</summary>
    public MainViewModel? ViewModel => _viewModel;

    public MapPageView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await InitializeMapAsync();
    }

    private async Task InitializeMapAsync()
    {
        var map = new Mapsui.Map();
        _tileLayer = OpenStreetMap.CreateTileLayer();
        map.Layers.Add(_tileLayer);

        try
        {
            _locationLayer.Style = new ImageStyle
            {
                Image = "embedded://Hikr.Resources.Images.hikr_gps_standort.png",
                SymbolScale = 0.06,
                RelativeOffset = new RelativeOffset(0, 0),
                RotateWithMap = true
            };

            _searchLayer.Style = new ImageStyle
            {
                Image = "embedded://Hikr.Resources.Images.hikr_such_standort.png",
                SymbolScale = 0.02,
                RelativeOffset = new RelativeOffset(0, 0.5)
            };

            _routeLayer.Style = new VectorStyle
            {
                Line = new Pen
                {
                    Color = new Mapsui.Styles.Color(74, 144, 226, 180),
                    Width = 6
                }
            };

            _radiusCircleLayer.Style = new VectorStyle
            {
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(46, 204, 113, 30)),
                Outline = new Pen
                {
                    Color = new Mapsui.Styles.Color(46, 204, 113, 180),
                    Width = 2,
                    PenStyle = PenStyle.Solid
                }
            };

            _ambientRoutesLayer.Style = new VectorStyle
            {
                Line = new Pen
                {
                    Color = new Mapsui.Styles.Color(39, 174, 96, 120),
                    Width = 3
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load map style assets: {ex.Message}");
        }

        map.Layers.Add(_radiusCircleLayer);
        map.Layers.Add(_ambientRoutesLayer);
        map.Layers.Add(_routeLayer);
        map.Layers.Add(_locationLayer);
        map.Layers.Add(_searchLayer);

        mapView.Map = map;
        mapView.Map.Widgets.Clear();

        _viewModel = new MainViewModel(new HttpClient());
        _viewModel.ViewboxProvider = GetViewboxParam;
        BindingContext = _viewModel;

        _viewModel.DestinationSet      += OnDestinationSet;
        _viewModel.RoutesCalculated    += OnRoutesCalculated;
        _viewModel.NavigationStarted   += OnNavigationStarted;
        _viewModel.NavigationExited    += OnNavigationExited;
        _viewModel.RecenterNavRequested += OnRecenterNavRequested;
        _viewModel.RecenterGpsRequested += OnRecenterGpsRequested;
        _viewModel.RoutingExited       += OnRoutingExited;

        _viewModel.HikingRoutesLoaded += OnHikingRoutesLoaded;
        _viewModel.RadiusCircleChanged += UpdateRadiusCircle;
        _viewModel.HikingModeChanged += () => {
            UpdateRadiusCircle();
            if (!_viewModel.IsHikingModeActive)
            {
                _ambientRoutesLayer.Clear();
                _ambientRoutesLayer.DataHasChanged();
            }
        };

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CompassHeading))
            {
                if (_locationLayer.Style is ImageStyle imageStyle)
                {
                    imageStyle.SymbolRotation = _viewModel.CompassHeading;
                    _locationLayer.DataHasChanged();
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.ShowWaymarkedTrails))
            {
                UpdateWaymarkedTrailsLayer();
            }
        };

        _viewModel.GpsPositionUpdated += (mapsuiPosition) =>
        {
            _locationLayer.Clear();
            _locationLayer.Add(new PointFeature(mapsuiPosition));
            _locationLayer.DataHasChanged();

            UpdateRadiusCircle();

            if (_viewModel.IsInNavigationMode && _viewModel.StartPosition != null)
            {
                mapView.Map.Navigator.CenterOn(_viewModel.StartPosition, 400);
                if (_viewModel.RoutePoints.Count >= 2)
                    UpdateMapRotation();
            }
        };

        mapView.MapTapped += (sender, args) =>
        {
            if (_viewModel.IsHikingModeActive)
            {
                var viewport = mapView.Map.Navigator.Viewport;
                if (viewport.Width > 0 && viewport.Height > 0)
                {
                    double tapWorldX = viewport.CenterX + (args.ScreenPosition.X - viewport.Width / 2.0) * viewport.Resolution;
                    double tapWorldY = viewport.CenterY - (args.ScreenPosition.Y - viewport.Height / 2.0) * viewport.Resolution;

                    var factory = new NetTopologySuite.Geometries.GeometryFactory();
                    var tapPoint = factory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(tapWorldX, tapWorldY));

                    // 25 pixels tolerance converted to world units
                    double maxTolerance = 25.0 * viewport.Resolution;
                    HikingRouteModel? closestRoute = null;
                    double minDistance = double.MaxValue;

                    foreach (var route in _viewModel.AmbientHikingRoutes)
                    {
                        if (route.Geometry != null)
                        {
                            double dist = route.Geometry.Distance(tapPoint);
                            if (dist <= maxTolerance && dist < minDistance)
                            {
                                minDistance = dist;
                                closestRoute = route;
                            }
                        }
                    }

                    if (closestRoute != null)
                    {
                        HighlightAmbientRouteVisual(closestRoute);
                        _viewModel.SelectedHikingRoute = closestRoute;
                        
                        // Clear search details to prevent visual overlap
                        _viewModel.IsSingleSearchPanelVisible = false;
                        _viewModel.IsPlaceDetailPanelVisible = false;
                        _viewModel.IsRoutingPanelVisible = false;
                        _viewModel.IsRouteInfoPanelVisible = false;
                        return;
                    }
                }
            }

            var generalMapInfo = mapView.GetMapInfo(args.ScreenPosition, new[] { _routeLayer });
            if (generalMapInfo?.Feature is GeometryFeature tappedFeature)
            {
                var clickedRoute = _viewModel.CurrentRoutes.FirstOrDefault(r => r.Feature == tappedFeature);
                if (clickedRoute != null)
                    SelectRouteVisual(clickedRoute);
            }
        };

        mapView.MapPointerPressed += (sender, args) =>
        {
            _pressStart = DateTime.UtcNow;
            _pressPosition = args.ScreenPosition;
        };

        mapView.MapPointerReleased += async (sender, args) =>
        {
            if (_pressPosition == null || args.WorldPosition == null) return;

            var elapsed = DateTime.UtcNow - _pressStart;
            var dx = args.ScreenPosition.X - _pressPosition.Value.X;
            var dy = args.ScreenPosition.Y - _pressPosition.Value.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            _pressPosition = null;

            if (elapsed.TotalMilliseconds >= 500 && distance < 15)
            {
                var (lon, lat) = SphericalMercator.ToLonLat(args.WorldPosition.X, args.WorldPosition.Y);
                await _viewModel.HandleMapLongClickAsync(lat, lon);
            }
        };

        await ZeigeAktuellenStandortAn();

        _locationUpdateTimer = Dispatcher.CreateTimer();
        _locationUpdateTimer.Interval = TimeSpan.FromSeconds(5);
        _locationUpdateTimer.Tick += async (s, e) => await AktualisiereGPSPositionImHintergrundAsync();
        _locationUpdateTimer.Start();
    }

    private void OnDestinationSet(MPoint zielPosition, NominatimResult gewaehlterOrt)
    {
        _searchLayer.Clear();
        _searchLayer.Add(new PointFeature(zielPosition));
        _searchLayer.DataHasChanged();
        ZoomToLocation(gewaehlterOrt, zielPosition);
    }

    private void OnRoutesCalculated(List<RouteData> routes, bool shouldZoom)
    {
        _routeLayer.Clear();
        foreach (var r in routes) _routeLayer.Add(r.Feature);
        _routeLayer.DataHasChanged();

        if (routes.Count > 0)
        {
            SelectRouteVisual(routes[0]);
            if (shouldZoom)
            {
                if (_viewModel.IsInNavigationMode)
                {
                    if (_viewModel.StartPosition != null)
                    {
                        mapView.Map.Navigator.CenterOn(_viewModel.StartPosition);
                        mapView.Map.Navigator.ZoomTo(NavigationCalculator.GetClosestZoomResolution(mapView.Map));
                    }
                }
                else
                {
                    ZoomToRoute(routes[0]);
                }
            }
        }
    }

    private void OnNavigationStarted(MPoint start)
    {
        mapView.Map.Navigator.CenterOn(start);
        mapView.Map.Navigator.ZoomTo(NavigationCalculator.GetClosestZoomResolution(mapView.Map));
        UpdateMapRotation();

        var selectedRoute = _viewModel.SelectedRoute ?? (_viewModel.CurrentRoutes.Count > 0 ? _viewModel.CurrentRoutes[0] : null);
        if (selectedRoute != null)
        {
            SelectRouteVisual(selectedRoute);
        }
    }

    private void OnNavigationExited()
    {
        mapView.Map.Navigator.RotateTo(0, 600);
        var selectedRoute = _viewModel.SelectedRoute ?? (_viewModel.CurrentRoutes.Count > 0 ? _viewModel.CurrentRoutes[0] : null);
        if (selectedRoute != null)
        {
            ZoomToRoute(selectedRoute);
            SelectRouteVisual(selectedRoute);
        }
    }

    private void OnRecenterNavRequested()
    {
        if (_viewModel.StartPosition != null)
        {
            mapView.Map.Navigator.CenterOn(_viewModel.StartPosition);
            mapView.Map.Navigator.ZoomTo(NavigationCalculator.GetClosestZoomResolution(mapView.Map));
        }
    }

    private async void OnRecenterGpsRequested()
    {
        try
        {
            var mapsuiPosition = await _viewModel.GetCurrentLocationAsync();
            if (mapsuiPosition != null)
            {
                _viewModel.StartPosition = mapsuiPosition;
                _locationLayer.Clear();
                _locationLayer.Add(new PointFeature(mapsuiPosition));
                _locationLayer.DataHasChanged();
                mapView.Map.Navigator.CenterOnAndZoomTo(
                    mapsuiPosition,
                    NavigationCalculator.GetClosestZoomResolution(mapView.Map),
                    1000);
            }
        }
        catch { }
    }

    private void OnRoutingExited()
    {
        _routeLayer.Clear();
        _routeLayer.DataHasChanged();
        _searchLayer.Clear();
        _searchLayer.DataHasChanged();
    }

    private void SelectRouteVisual(RouteData selectedRoute)
    {
        _viewModel.SelectRoute(selectedRoute);
        foreach (var r in _viewModel.CurrentRoutes)
        {
            r.Feature.Styles.Clear();
            if (r == selectedRoute)
                r.Feature.Styles.Add(new VectorStyle { Line = new Pen { Color = new Mapsui.Styles.Color(27, 114, 232, 230), Width = 8 } });
            else
                r.Feature.Styles.Add(new VectorStyle { Line = new Pen { Color = new Mapsui.Styles.Color(142, 142, 147, 150), Width = 6 } });
        }
        _routeLayer.Clear();
        if (_viewModel.IsInNavigationMode)
        {
            _routeLayer.Add(selectedRoute.Feature);
        }
        else
        {
            foreach (var r in _viewModel.CurrentRoutes)
            {
                if (r != selectedRoute) _routeLayer.Add(r.Feature);
            }
            _routeLayer.Add(selectedRoute.Feature);
        }
        _routeLayer.DataHasChanged();
        mapView.Refresh();
    }

    private async Task ZeigeAktuellenStandortAn()
    {
        try
        {
            var mapsuiPosition = await _viewModel.GetCurrentLocationAsync();
            if (mapsuiPosition != null)
            {
                _viewModel.StartPosition = mapsuiPosition;
                _locationLayer.Clear();
                _locationLayer.Add(new PointFeature(mapsuiPosition));
                _locationLayer.DataHasChanged();
                mapView.Map.Navigator.CenterOnAndZoomTo(
                    mapsuiPosition, mapView.Map.Navigator.Resolutions[14], 1000);
                if (_viewModel.DestinationPosition != null)
                    await _viewModel.BerechneUndZeigeRouteAsync(true);
            }
        }
        catch { }
    }

    private async Task AktualisiereGPSPositionImHintergrundAsync()
    {
        try
        {
            var mapsuiPosition = await _viewModel.GetCurrentLocationAsync();
            if (mapsuiPosition != null)
                await _viewModel.ProcessBackgroundGpsUpdateAsync(mapsuiPosition, routingPanel.IsVisible);
        }
        catch { }
    }

    private void UpdateMapRotation()
    {
        if (_viewModel.RoutePoints.Count < 2 || _viewModel.StartPosition == null) return;
        int closestIndex = 0;
        double minDistance = double.MaxValue;
        for (int i = 0; i < _viewModel.RoutePoints.Count; i++)
        {
            double dist = NavigationCalculator.Distance(_viewModel.StartPosition, _viewModel.RoutePoints[i]);
            if (dist < minDistance) { minDistance = dist; closestIndex = i; }
        }
        if (closestIndex < _viewModel.RoutePoints.Count - 1)
        {
            double dx = _viewModel.RoutePoints[closestIndex + 1].X - _viewModel.RoutePoints[closestIndex].X;
            double dy = _viewModel.RoutePoints[closestIndex + 1].Y - _viewModel.RoutePoints[closestIndex].Y;
            double heading = Math.Atan2(dx, dy) * (180.0 / Math.PI);
            if (heading < 0) heading += 360;
            mapView.Map.Navigator.RotateTo(-heading, 600);
        }
    }

    private void ZoomToLocation(NominatimResult gewaehlterOrt, MPoint zielPosition)
    {
        if (gewaehlterOrt.IsCityOrRegion() && gewaehlterOrt.BoundingBox != null && gewaehlterOrt.BoundingBox.Count == 4)
        {
            try
            {
                double minLat = double.Parse(gewaehlterOrt.BoundingBox[0], System.Globalization.CultureInfo.InvariantCulture);
                double maxLat = double.Parse(gewaehlterOrt.BoundingBox[1], System.Globalization.CultureInfo.InvariantCulture);
                double minLon = double.Parse(gewaehlterOrt.BoundingBox[2], System.Globalization.CultureInfo.InvariantCulture);
                double maxLon = double.Parse(gewaehlterOrt.BoundingBox[3], System.Globalization.CultureInfo.InvariantCulture);
                var (minX, minY) = SphericalMercator.FromLonLat(minLon, minLat);
                var (maxX, maxY) = SphericalMercator.FromLonLat(maxLon, maxLat);
                mapView.Map.Navigator.ZoomToBox(new MRect(minX, minY, maxX, maxY), Mapsui.MBoxFit.Fit, 1200);
                return;
            }
            catch { }
        }
        mapView.Map.Navigator.CenterOnAndZoomTo(zielPosition, mapView.Map.Navigator.Resolutions[14], 1200);
    }

    private void OnRouteInfoSwipedUp(object? sender, SwipedEventArgs e)
    {
        // Animate panel sliding up to full screen
        stepsScrollView.IsVisible = true;
        stepsScrollView.Opacity = 0;

        var fullHeight = this.Height;

        // Slide panel upward with HeightRequest animation
        var heightAnim = new Animation(v => routeInfoPanel.HeightRequest = v,
            routeInfoPanel.Height, fullHeight);
        heightAnim.Commit(routeInfoPanel, "ExpandHeight", 16, 320, Easing.CubicOut);

        // Fade in steps list
        stepsScrollView.FadeTo(1, 300, Easing.CubicOut);

        // Switch to fully opaque white
        routeInfoPanel.BackgroundColor = Colors.White;
        routeInfoPanel.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 0 };
    }

    private void OnRouteInfoSwipedDown(object? sender, SwipedEventArgs e)
    {
        // Fade out steps list first
        stepsScrollView.FadeTo(0, 150, Easing.CubicIn).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() => stepsScrollView.IsVisible = false));

        // Animate panel shrinking back to auto height
        var compactHeight = 160.0;
        var heightAnim = new Animation(v => routeInfoPanel.HeightRequest = v,
            routeInfoPanel.Height, compactHeight);
        heightAnim.Commit(routeInfoPanel, "CollapseHeight", 16, 280, Easing.CubicIn, (_, __) =>
        {
            routeInfoPanel.HeightRequest = -1; // reset to auto after animation
        });

        // Restore frosted glass look
        routeInfoPanel.BackgroundColor = Color.FromArgb("#D4FFFFFF");
        routeInfoPanel.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(24, 24, 0, 0)
        };
    }

    private void ZoomToRoute(RouteData route)
    {
        if (route?.Feature?.Geometry == null) return;
        try
        {
            var envelope = route.Feature.Geometry.EnvelopeInternal;
            if (envelope != null && envelope.Width > 0 && envelope.Height > 0)
            {
                var boundingBox = new MRect(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);
                double paddingX = boundingBox.Width * 0.20;
                double paddingY = boundingBox.Height * 0.20;
                var paddedBox = new MRect(
                    boundingBox.MinX - paddingX, boundingBox.MinY - paddingY * 2.2,
                    boundingBox.MaxX + paddingX, boundingBox.MaxY + paddingY * 1.2);
                mapView.Map.Navigator.ZoomToBox(paddedBox, Mapsui.MBoxFit.Fit, 1000);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ZoomToRoute Error: {ex.Message}"); }
    }

    private string GetViewboxParam()
    {
        try
        {
            var viewportOpt = mapView.Map?.Navigator?.Viewport;
            if (viewportOpt.HasValue)
            {
                var viewport = viewportOpt.Value;
                if (viewport.Width > 0 && viewport.Height > 0)
                {
                    double halfWidth  = (viewport.Width  / 2.0) * viewport.Resolution;
                    double halfHeight = (viewport.Height / 2.0) * viewport.Resolution;
                    double minX = viewport.CenterX - halfWidth;
                    double minY = viewport.CenterY - halfHeight;
                    double maxX = viewport.CenterX + halfWidth;
                    double maxY = viewport.CenterY + halfHeight;
                    var (minLon, minLat) = SphericalMercator.ToLonLat(minX, minY);
                    var (maxLon, maxLat) = SphericalMercator.ToLonLat(maxX, maxY);
                    return $"&viewbox={minLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{maxLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{maxLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{minLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                }
            }
        }
        catch { }
        return string.Empty;
    }

    private void OnCompassButtonClicked(object? sender, EventArgs e) =>
        mapView.Map.Navigator.RotateTo(0, 600);

    private void OnRoutingSuggestionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not NominatimResult gewaehlterOrt) return;
        if (sender is CollectionView cv) cv.SelectedItem = null;
        routingSuggestionsList.IsVisible = false;
        routingSuggestionsList.ItemsSource = null;

        double lat = double.Parse(gewaehlterOrt.Lat, System.Globalization.CultureInfo.InvariantCulture);
        double lon = double.Parse(gewaehlterOrt.Lon, System.Globalization.CultureInfo.InvariantCulture);
        var position = SphericalMercator.FromLonLat(lon, lat).ToMPoint();

        if (_viewModel.ActiveSearchField == "start")
        {
            _viewModel.StartPosition    = position;
            _viewModel.StartEntryText   = gewaehlterOrt.DisplayName;
            _locationLayer.Clear();
            _locationLayer.Add(new PointFeature(_viewModel.StartPosition));
            _locationLayer.DataHasChanged();
            startEntry.Unfocus();
        }
        else
        {
            _viewModel.DestinationPosition = position;
            _viewModel.DestinationEntryText = gewaehlterOrt.DisplayName;
            _searchLayer.Clear();
            _searchLayer.Add(new PointFeature(_viewModel.DestinationPosition));
            _searchLayer.DataHasChanged();
            destinationEntry.Unfocus();
        }
        _ = _viewModel.BerechneUndZeigeRouteAsync(true);
    }

    private void OnStartEntryFocused(object? sender, FocusEventArgs e) =>
        _viewModel.StartEntryFocusedCommand.Execute(null);

    private void OnDestinationEntryFocused(object? sender, FocusEventArgs e) =>
        _viewModel.DestinationEntryFocusedCommand.Execute(null);

    private void OnSearchPillTapped(object? sender, TappedEventArgs e)
        => SearchRequested?.Invoke(this, EventArgs.Empty);

    private void OnZoomToSelectedHikingRouteClicked(object? sender, EventArgs e)
    {
        if (_viewModel.SelectedHikingRoute != null)
        {
            ZoomToRoute(new RouteData { Feature = _viewModel.SelectedHikingRoute.Feature });
            HighlightAmbientRouteVisual(_viewModel.SelectedHikingRoute);
        }
    }

    private void OnZoomInClicked(object? sender, EventArgs e)  => mapView.Map.Navigator.ZoomIn(400);
    private void OnZoomOutClicked(object? sender, EventArgs e) => mapView.Map.Navigator.ZoomOut(400);

    private async void OnMapStyleButtonClicked(object? sender, EventArgs e)
    {
        string action = await Application.Current!.Windows[0].Page!.DisplayActionSheet(
            "Kartenansicht ändern", "Abbrechen", null,
            "Standard (OpenStreetMap)", "Satellit (ArcGIS)", "Topografisch (OpenTopoMap)");
        if      (action == "Standard (OpenStreetMap)")  SetMapStyle("Standard");
        else if (action == "Satellit (ArcGIS)")         SetMapStyle("Satellite");
        else if (action == "Topografisch (OpenTopoMap)") SetMapStyle("Topo");
    }

    private void SetMapStyle(string styleName)
    {
        if (mapView.Map == null) return;
        if (_tileLayer != null) mapView.Map.Layers.Remove(_tileLayer);
        _tileLayer = styleName switch
        {
            "Satellite" => CreateSatelliteTileLayer(),
            "Topo"      => CreateTopoTileLayer(),
            _           => OpenStreetMap.CreateTileLayer()
        };
        mapView.Map.Layers.Insert(0, _tileLayer);
        mapView.Refresh();
    }

    private static ILayer CreateSatelliteTileLayer()
    {
        var tileSource = new BruTile.Web.HttpTileSource(
            new BruTile.Predefined.GlobalSphericalMercator(),
            "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
            new[] { "a", "b", "c" }, name: "Satellite");
        return new Mapsui.Tiling.Layers.TileLayer(tileSource) { Name = "Satellite" };
    }

    private static ILayer CreateTopoTileLayer()
    {
        var tileSource = new BruTile.Web.HttpTileSource(
            new BruTile.Predefined.GlobalSphericalMercator(),
            "https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png",
            new[] { "a", "b", "c" }, name: "Topo");
        return new Mapsui.Tiling.Layers.TileLayer(tileSource) { Name = "Topo" };
    }

    private static ILayer CreateWaymarkedTrailsTileLayer()
    {
        var tileSource = new BruTile.Web.HttpTileSource(
            new BruTile.Predefined.GlobalSphericalMercator(),
            "https://tile.waymarkedtrails.org/hiking/{z}/{x}/{y}.png",
            name: "Waymarked Trails");
        return new Mapsui.Tiling.Layers.TileLayer(tileSource) { Name = "WaymarkedTrails" };
    }

    private void UpdateWaymarkedTrailsLayer()
    {
        if (mapView.Map == null) return;

        if (_viewModel.ShowWaymarkedTrails)
        {
            if (_waymarkedTrailsLayer == null)
            {
                _waymarkedTrailsLayer = CreateWaymarkedTrailsTileLayer();
            }
            if (!mapView.Map.Layers.Contains(_waymarkedTrailsLayer))
            {
                mapView.Map.Layers.Insert(1, _waymarkedTrailsLayer);
            }
        }
        else
        {
            if (_waymarkedTrailsLayer != null && mapView.Map.Layers.Contains(_waymarkedTrailsLayer))
            {
                mapView.Map.Layers.Remove(_waymarkedTrailsLayer);
            }
        }
        mapView.Refresh();
    }

    private void UpdateRadiusCircle()
    {
        _radiusCircleLayer.Clear();
        if (_viewModel.IsHikingModeActive && _viewModel.StartPosition != null)
        {
            var center = new Coordinate(_viewModel.StartPosition.X, _viewModel.StartPosition.Y);
            double radiusInMeters = _viewModel.SearchRadiusKm * 1000.0;

            var (lon, lat) = SphericalMercator.ToLonLat(_viewModel.StartPosition.X, _viewModel.StartPosition.Y);
            double correctionFactor = 1.0 / Math.Cos(lat * Math.PI / 180.0);
            double correctedRadius = radiusInMeters * correctionFactor;

            var geometryFactory = new GeometryFactory();
            var point = geometryFactory.CreatePoint(center);
            var circlePolygon = point.Buffer(correctedRadius, 32);

            _radiusCircleLayer.Add(new GeometryFeature { Geometry = circlePolygon });
        }
        _radiusCircleLayer.DataHasChanged();
        mapView.Refresh();
    }

    private void OnHikingRoutesLoaded(List<HikingRouteModel> routes)
    {
        _ambientRoutesLayer.Clear();
        foreach (var route in routes)
        {
            _ambientRoutesLayer.Add(route.Feature);
        }
        _ambientRoutesLayer.DataHasChanged();
        mapView.Refresh();
    }

    private void HighlightAmbientRouteVisual(HikingRouteModel selectedRoute)
    {
        foreach (var r in _viewModel.AmbientHikingRoutes)
        {
            r.Feature.Styles.Clear();
            if (r == selectedRoute)
            {
                r.Feature.Styles.Add(new VectorStyle
                {
                    Line = new Pen
                    {
                        Color = new Mapsui.Styles.Color(46, 204, 113, 230),
                        Width = 6
                    }
                });
            }
            else
            {
                r.Feature.Styles.Add(new VectorStyle
                {
                    Line = new Pen
                    {
                        Color = new Mapsui.Styles.Color(39, 174, 96, 120),
                        Width = 3
                    }
                });
            }
        }
        _ambientRoutesLayer.DataHasChanged();
        mapView.Refresh();
    }
}

/// <summary>Command extensions for async ICommand execution.</summary>
public static class CommandExtensions
{
    public static async Task ExecuteAsync(this System.Windows.Input.ICommand command, object? parameter = null)
    {
        if (command is IAsyncCommand asyncCommand)
            await asyncCommand.ExecuteAsync(parameter);
        else
            command.Execute(parameter);
    }
}
