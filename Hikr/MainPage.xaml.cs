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

namespace Hikr;

/// <summary>
/// Main View page representing the map canvas and bound controls layer.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly WritableLayer _locationLayer = new() { Name = "Location Layer" };
    private readonly WritableLayer _searchLayer = new() { Name = "Search Layer" };
    private readonly WritableLayer _routeLayer = new() { Name = "Route Layer" };
    private MainViewModel _viewModel = null!;
    private IDispatcherTimer? _locationUpdateTimer;

    /// <summary>
    /// Initializes a new instance of the MainPage class.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await InitializeMapAsync();
    }

    private async Task InitializeMapAsync()
    {
        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load map style assets: {ex.Message}");
        }

        map.Layers.Add(_routeLayer);
        map.Layers.Add(_locationLayer);
        map.Layers.Add(_searchLayer);

        mapView.Map = map;
        mapView.Map.Widgets.Clear();

        // Instantiate ViewModel with shared HttpClient
        _viewModel = new MainViewModel(new HttpClient());
        _viewModel.ViewboxProvider = GetViewboxParam;
        BindingContext = _viewModel;

        // Wire ViewModel Events to View rendering layers
        _viewModel.DestinationSet += OnDestinationSet;
        _viewModel.RoutesCalculated += OnRoutesCalculated;
        _viewModel.NavigationStarted += OnNavigationStarted;
        _viewModel.NavigationExited += OnNavigationExited;
        _viewModel.RecenterNavRequested += OnRecenterNavRequested;
        _viewModel.RecenterGpsRequested += OnRecenterGpsRequested;
        _viewModel.RoutingExited += OnRoutingExited;

        // Subscribes to CompassHeading updates directly on the ViewModel
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
        };

        // Subscribes to GpsPositionUpdated directly on the ViewModel
        _viewModel.GpsPositionUpdated += (mapsuiPosition) =>
        {
            _locationLayer.Clear();
            _locationLayer.Add(new PointFeature(mapsuiPosition));
            _locationLayer.DataHasChanged();

            if (_viewModel.IsInNavigationMode && _viewModel.StartPosition != null)
            {
                mapView.Map.Navigator.CenterOn(_viewModel.StartPosition, 400);

                if (_viewModel.RoutePoints.Count >= 2)
                {
                    UpdateMapRotation();
                }
            }
        };

        // Alternative route tap selector
        mapView.MapTapped += (sender, args) =>
        {
            var mapInfo = mapView.GetMapInfo(args.ScreenPosition, mapView.Map.Layers);
            if (mapInfo?.Feature is GeometryFeature tappedFeature)
            {
                var clickedRoute = _viewModel.CurrentRoutes.FirstOrDefault(r => r.Feature == tappedFeature);
                if (clickedRoute != null)
                {
                    SelectRouteVisual(clickedRoute);
                }
            }
        };

        await ZeigeAktuellenStandortAn();

        // Location polling task
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
        foreach (var r in routes)
        {
            _routeLayer.Add(r.Feature);
        }
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
        
        // Orient the map along the segment immediately on start to prevent first-tick stuttering
        UpdateMapRotation();
    }

    private void OnNavigationExited()
    {
        // Smoothly orient map north and show the full route overview
        mapView.Map.Navigator.RotateTo(0, 600);
        if (_viewModel.CurrentRoutes.Count > 0)
        {
            ZoomToRoute(_viewModel.CurrentRoutes[0]);
            SelectRouteVisual(_viewModel.CurrentRoutes[0]);
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
                    1000
                );
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
            {
                r.Feature.Styles.Add(new VectorStyle
                {
                    Line = new Pen
                    {
                        Color = new Mapsui.Styles.Color(27, 114, 232, 230),
                        Width = 8
                    }
                });
            }
            else
            {
                r.Feature.Styles.Add(new VectorStyle
                {
                    Line = new Pen
                    {
                        Color = new Mapsui.Styles.Color(142, 142, 147, 150),
                        Width = 6
                    }
                });
            }
        }

        _routeLayer.Clear();
        foreach (var r in _viewModel.CurrentRoutes)
        {
            if (r != selectedRoute)
            {
                _routeLayer.Add(r.Feature);
            }
        }
        _routeLayer.Add(selectedRoute.Feature);
        _routeLayer.DataHasChanged();
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
                    mapsuiPosition,
                    mapView.Map.Navigator.Resolutions[14],
                    1000
                );

                if (_viewModel.DestinationPosition != null)
                {
                    await _viewModel.BerechneUndZeigeRouteAsync(true);
                }
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
            {
                await _viewModel.ProcessBackgroundGpsUpdateAsync(mapsuiPosition, routingPanel.IsVisible);
            }
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
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        if (closestIndex < _viewModel.RoutePoints.Count - 1)
        {
            double dx = _viewModel.RoutePoints[closestIndex + 1].X - _viewModel.RoutePoints[closestIndex].X;
            double dy = _viewModel.RoutePoints[closestIndex + 1].Y - _viewModel.RoutePoints[closestIndex].Y;
            double angleRad = Math.Atan2(dx, dy);
            double heading = angleRad * (180.0 / Math.PI);
            if (heading < 0) heading += 360;

            // Smooth 600ms transition to prevent rotation stuttering/jumping
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

                var boundingBox = new MRect(minX, minY, maxX, maxY);
                mapView.Map.Navigator.ZoomToBox(boundingBox, Mapsui.MBoxFit.Fit, 1200);
                return;
            }
            catch { }
        }

        mapView.Map.Navigator.CenterOnAndZoomTo(
            zielPosition,
            mapView.Map.Navigator.Resolutions[14],
            1200
        );
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
                    boundingBox.MinX - paddingX,
                    boundingBox.MinY - paddingY * 2.2,
                    boundingBox.MaxX + paddingX,
                    boundingBox.MaxY + paddingY * 1.2
                );

                mapView.Map.Navigator.ZoomToBox(paddedBox, Mapsui.MBoxFit.Fit, 1000);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ZoomToRoute Error: {ex.Message}");
        }
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
                    double halfWidth = (viewport.Width / 2.0) * viewport.Resolution;
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

    private void OnCompassButtonClicked(object? sender, EventArgs e)
    {
        mapView.Map.Navigator.RotateTo(0, 600);
    }

    private async void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        searchBar.Unfocus();
        _viewModel.IsSuggestionsListVisible = false;
        await _viewModel.SearchCommand.ExecuteAsync(searchBar.Text);
    }

    private void OnSuggestionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not NominatimResult gewaehlterOrt) return;

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }

        searchBar.Unfocus();
        _viewModel.SelectSuggestionCommand.Execute(gewaehlterOrt);
    }

    private void OnRoutingSuggestionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not NominatimResult gewaehlterOrt) return;

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }

        routingSuggestionsList.IsVisible = false;
        routingSuggestionsList.ItemsSource = null;

        double lat = double.Parse(gewaehlterOrt.Lat, System.Globalization.CultureInfo.InvariantCulture);
        double lon = double.Parse(gewaehlterOrt.Lon, System.Globalization.CultureInfo.InvariantCulture);
        var position = SphericalMercator.FromLonLat(lon, lat).ToMPoint();

        if (_viewModel.ActiveSearchField == "start")
        {
            _viewModel.StartPosition = position;
            _viewModel.StartEntryText = gewaehlterOrt.DisplayName;
            
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

    private void OnStartEntryFocused(object? sender, FocusEventArgs e)
    {
        _viewModel.StartEntryFocusedCommand.Execute(null);
    }

    private void OnDestinationEntryFocused(object? sender, FocusEventArgs e)
    {
        _viewModel.DestinationEntryFocusedCommand.Execute(null);
    }
}
/// <summary>
/// Command extensions to allow asynchronous execution on normal ICommand instances.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Extension method to asynchronously execute an ICommand.
    /// </summary>
    public static async Task ExecuteAsync(this System.Windows.Input.ICommand command, object? parameter = null)
    {
        if (command is IAsyncCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(parameter);
        }
        else
        {
            command.Execute(parameter);
        }
    }
}