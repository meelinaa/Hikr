using System.ComponentModel;
using System.Globalization;
using Hikr.Helpers;
using Hikr.Models;
using Hikr.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Hikr.Views;

public partial class ShellHostPage : ContentPage
{
    private const double DrawerWidth = 290;
    private int _activeTab = 0;
    private bool _drawerOpen;
    private bool _viewModelSubscribed;
    private readonly List<HistoryEntry> _historyEntries = new();

    public ShellHostPage()
    {
        InitializeComponent();

        drawerPanel.TranslationX = -DrawerWidth;
        dimOverlay.IsVisible = false;
        dimOverlay.Opacity = 0;

        InitHistoryData();
        historyList.ItemsSource = _historyEntries;
        viewMap.SearchRequested += OnSearchRequested;
    }

    // ─────────────────────────────────────────────────────────────
    // BURGER BUTTON & DRAWER
    // ─────────────────────────────────────────────────────────────

    private void OnBurgerTapped(object? sender, TappedEventArgs e) => OpenDrawer();
    private void OnDimOverlayTapped(object? sender, TappedEventArgs e) => CloseDrawer();
    private void OnDrawerCloseTapped(object? sender, TappedEventArgs e) => CloseDrawer();

    private async void OpenDrawer()
    {
        if (_drawerOpen) return;
        _drawerOpen = true;

        dimOverlay.IsVisible = true;
        dimOverlay.Opacity = 0;
        burgerBtn.IsVisible = false;

        await Task.WhenAll(
            drawerPanel.TranslateToAsync(0, 0, 280, Easing.CubicOut),
            dimOverlay.FadeToAsync(1, 280)
        );
    }

    private async void CloseDrawer()
    {
        if (!_drawerOpen) return;
        _drawerOpen = false;

        await Task.WhenAll(
            drawerPanel.TranslateToAsync(-DrawerWidth, 0, 240, Easing.CubicIn),
            dimOverlay.FadeToAsync(0, 220)
        );

        dimOverlay.IsVisible = false;
        if (!searchOverlay.IsVisible)
            burgerBtn.IsVisible = true;
    }

    // ─────────────────────────────────────────────────────────────
    // NAVIGATION
    // ─────────────────────────────────────────────────────────────

    private async void OnDrawerNavMap(object? sender, TappedEventArgs e)
    {
        CloseDrawer();
        await SwitchTab(0);
    }

    private async void OnDrawerNavExplore(object? sender, TappedEventArgs e)
    {
        CloseDrawer();
        await SwitchTab(1);
    }

    private async void OnDrawerNavFavorites(object? sender, TappedEventArgs e)
    {
        CloseDrawer();
        await SwitchTab(2);
    }

    private async void OnDrawerNavProfile(object? sender, TappedEventArgs e)
    {
        CloseDrawer();
        await SwitchTab(3);
    }

    private async Task SwitchTab(int index)
    {
        if (_activeTab == index) return;
        _activeTab = index;

        var views = new[] { (View)viewMap, viewExplore, viewFavorites, viewProfile };
        foreach (var v in views)
        {
            if (v.IsVisible)
                await v.FadeToAsync(0, 120);
        }

        viewMap.IsVisible       = index == 0;
        viewExplore.IsVisible   = index == 1;
        viewFavorites.IsVisible = index == 2;
        viewProfile.IsVisible   = index == 3;

        foreach (var v in views)
        {
            if (v.IsVisible)
            {
                v.Opacity = 0;
                await v.FadeToAsync(1, 180);
            }
        }

        UpdateDrawerItems(index);
    }

    private void UpdateDrawerItems(int activeIndex)
    {
        SetDrawerItemStyle(drawerItemMap,       drawerIconMap,       drawerLabelMap,       false, "#E8501A");
        SetDrawerItemStyle(drawerItemExplore,   drawerIconExplore,   drawerLabelExplore,   false, "#E8501A");
        SetDrawerItemStyle(drawerItemFavorites, drawerIconFavorites, drawerLabelFavorites, false, "#E8501A");
        SetDrawerItemStyle(drawerItemProfile,   drawerIconProfile,   drawerLabelProfile,   false, "#E8501A");

        (Border item, Label icon, Label label) = activeIndex switch
        {
            0 => (drawerItemMap,       drawerIconMap,       drawerLabelMap),
            1 => (drawerItemExplore,   drawerIconExplore,   drawerLabelExplore),
            2 => (drawerItemFavorites, drawerIconFavorites, drawerLabelFavorites),
            _ => (drawerItemProfile,   drawerIconProfile,   drawerLabelProfile)
        };
        SetDrawerItemStyle(item, icon, label, true, "#E8501A");
    }

    private static void SetDrawerItemStyle(Border item, Label icon, Label label, bool active, string accentHex)
    {
        if (active)
        {
            item.BackgroundColor  = Color.FromArgb("#1AE8501A");
            label.TextColor       = Color.FromArgb(accentHex);
            label.FontAttributes  = FontAttributes.Bold;
            icon.Opacity          = 1.0;
        }
        else
        {
            item.BackgroundColor  = Colors.Transparent;
            label.TextColor       = Color.FromArgb("#44000000");
            label.FontAttributes  = FontAttributes.None;
            icon.Opacity          = 0.45;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // SUCH-OVERLAY
    // ─────────────────────────────────────────────────────────────

    private void OnSearchRequested(object? sender, EventArgs e)
    {
        if (_drawerOpen)
            CloseDrawer();

        OpenSearchOverlay();
    }

    private void OpenSearchOverlay()
    {
        burgerBtn.IsVisible = false;
        searchOverlay.IsVisible = true;
        searchOverlayEntry.Text = string.Empty;
        searchClearBtn.IsVisible = false;
        historyPanel.IsVisible = true;
        searchResultsList.IsVisible = false;
        searchResultsList.ItemsSource = null;

        RefreshHistoryDistances();
        EnsureViewModelSubscription();

        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(80);
            searchOverlayEntry.Focus();
        });
    }

    private void CloseSearchOverlay()
    {
        searchOverlay.IsVisible = false;
        searchOverlayEntry.Text = string.Empty;
        searchClearBtn.IsVisible = false;
        historyPanel.IsVisible = true;
        searchResultsList.IsVisible = false;
        searchResultsList.ItemsSource = null;

        if (!_drawerOpen)
            burgerBtn.IsVisible = true;
    }

    private void OnSearchClose(object? sender, TappedEventArgs e) => CloseSearchOverlay();

    private void OnSearchClear(object? sender, TappedEventArgs e)
    {
        searchOverlayEntry.Text = string.Empty;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? string.Empty;
        searchClearBtn.IsVisible = !string.IsNullOrEmpty(text);

        var showHistory = string.IsNullOrEmpty(text) || text.Length < 3;
        historyPanel.IsVisible = showHistory;
        searchResultsList.IsVisible = !showHistory;

        if (showHistory)
            searchResultsList.ItemsSource = null;

        EnsureViewModelSubscription();
        if (viewMap.ViewModel != null)
            viewMap.ViewModel.SearchBarText = text;
    }

    private async void OnHistoryItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not HistoryEntry entry) return;
        historyList.SelectedItem = null;

        if (entry.Lat.HasValue && entry.Lon.HasValue)
        {
            await SelectLocationAndClose(CreateNominatimResult(entry));
            return;
        }

        var query = entry.SearchQuery ?? entry.PrimaryText;
        if (!string.IsNullOrWhiteSpace(query))
        {
            searchOverlayEntry.Text = query;
            searchOverlayEntry.Focus();
        }
    }

    private async void OnSearchResultSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SearchResultEntry entry || entry.NominatimResult == null)
            return;

        searchResultsList.SelectedItem = null;
        await SelectLocationAndClose(entry.NominatimResult);
    }

    private async void OnShortcutZuhauseTapped(object? sender, TappedEventArgs e)
    {
        await SelectLocationAndClose(CreateNominatimResult(
            "Kölnische Straße 147",
            "Kassel-Vorderer Westen, Kassel",
            51.3162, 9.4254));
    }

    private async void OnShortcutArbeitTapped(object? sender, TappedEventArgs e)
    {
        await SelectLocationAndClose(CreateNominatimResult(
            "Landfelder Straße",
            "Kassel",
            51.3054, 9.4651));
    }

    private async Task SelectLocationAndClose(NominatimResult result)
    {
        CloseSearchOverlay();

        if (_activeTab != 0)
            await SwitchTab(0);

        EnsureViewModelSubscription();
        if (viewMap.ViewModel != null)
            await viewMap.ViewModel.SelectSuggestionCommand.ExecuteAsync(result);
    }

    private void EnsureViewModelSubscription()
    {
        if (_viewModelSubscribed || viewMap.ViewModel == null) return;

        viewMap.ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModelSubscribed = true;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Suggestions))
        {
            MainThread.BeginInvokeOnMainThread(UpdateSearchResults);
        }
    }

    private void UpdateSearchResults()
    {
        if (!searchResultsList.IsVisible) return;

        var vm = viewMap.ViewModel;
        if (vm == null) return;

        var entries = vm.Suggestions
            .Select(result => ToSearchResultEntry(result, vm.StartPosition))
            .ToList();

        searchResultsList.ItemsSource = entries;
    }

    private static SearchResultEntry ToSearchResultEntry(NominatimResult result, MPoint? userPosition)
    {
        var entry = new SearchResultEntry
        {
            NominatimResult = result,
            Icon = result.GetResultIcon(),
            PrimaryText = result.GetPrimaryLabel(),
            SecondaryText = result.GetSecondaryLabel()
        };

        if (userPosition != null &&
            double.TryParse(result.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(result.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            var (userLon, userLat) = SphericalMercator.ToLonLat(userPosition.X, userPosition.Y);
            var meters = NavigationCalculator.HaversineDistanceMeters(userLat, userLon, lat, lon);
            entry.DistanceText = NavigationCalculator.FormatDistance(meters, useGermanFormat: true);
        }

        return entry;
    }

    private void InitHistoryData()
    {
        _historyEntries.Clear();
        _historyEntries.AddRange(new[]
        {
            new HistoryEntry
            {
                Icon = "📍",
                PrimaryText = "Ihringshausen",
                SecondaryText = "34233 Fuldatal",
                Lat = 51.3670,
                Lon = 9.6830
            },
            new HistoryEntry
            {
                Icon = "📍",
                PrimaryText = "Bahnhof Kassel-Wilhelmshöhe",
                SecondaryText = "Willy-Brandt-Platz 1, 34131 Kassel",
                Lat = 51.3127,
                Lon = 9.4423
            },
            new HistoryEntry { Icon = "🕐", PrimaryText = "arzt", SearchQuery = "arzt" },
            new HistoryEntry
            {
                Icon = "🕐",
                PrimaryText = "Kölnische Straße 147",
                SecondaryText = "Kassel-Vorderer Westen",
                SearchQuery = "Kölnische Straße 147"
            },
            new HistoryEntry
            {
                Icon = "🕐",
                PrimaryText = "K9, Kassel-Vorderer Westen",
                SearchQuery = "K9, Kassel-Vorderer Westen"
            },
            new HistoryEntry
            {
                Icon = "🕐",
                PrimaryText = "der ärztliche bereitschaftsdienst",
                SearchQuery = "der ärztliche bereitschaftsdienst"
            },
            new HistoryEntry
            {
                Icon = "🕐",
                PrimaryText = "zahnarzt kassenpatient",
                SearchQuery = "zahnarzt kassenpatient"
            }
        });
    }

    private void RefreshHistoryDistances()
    {
        var userPosition = viewMap.ViewModel?.StartPosition;
        if (userPosition == null) return;

        var (userLon, userLat) = SphericalMercator.ToLonLat(userPosition.X, userPosition.Y);

        foreach (var entry in _historyEntries)
        {
            if (!entry.Lat.HasValue || !entry.Lon.HasValue)
            {
                entry.DistanceText = string.Empty;
                continue;
            }

            var meters = NavigationCalculator.HaversineDistanceMeters(
                userLat, userLon, entry.Lat.Value, entry.Lon.Value);
            entry.DistanceText = NavigationCalculator.FormatDistance(meters, useGermanFormat: true);
        }

        historyList.ItemsSource = null;
        historyList.ItemsSource = _historyEntries;
    }

    private static NominatimResult CreateNominatimResult(HistoryEntry entry)
        => CreateNominatimResult(entry.PrimaryText, entry.SecondaryText, entry.Lat!.Value, entry.Lon!.Value);

    private static NominatimResult CreateNominatimResult(string title, string subtitle, double lat, double lon)
        => new()
        {
            Lat = lat.ToString(CultureInfo.InvariantCulture),
            Lon = lon.ToString(CultureInfo.InvariantCulture),
            DisplayName = string.IsNullOrEmpty(subtitle) ? title : $"{title}, {subtitle}"
        };
}
