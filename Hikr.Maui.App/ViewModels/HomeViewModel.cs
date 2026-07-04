using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hikr.Maui.App.ViewModels;

public class RouteMock
{
    public string Title { get; set; }
    public string Distance { get; set; }
    public string Duration { get; set; }
    public string TransportType { get; set; }
    public string TransportIcon { get; set; }
}

public class HomeViewModel
{
    public string HeaderText { get; set; } = "Meine routen.";
    public string SubtitleText { get; set; } = "13 gespeicherte Routen";

    public ObservableCollection<RouteMock> Routes { get; set; }

    public ICommand CreateRouteCommand { get; }

    public HomeViewModel()
    {
        Routes = new ObservableCollection<RouteMock>
        {
            new RouteMock { Title = "Alpenüberquerung", Distance = "120 km", Duration = "6 Tage", TransportType = "Wandern", TransportIcon = "⛰️" },
            new RouteMock { Title = "Stadtwald Runde", Distance = "5 km", Duration = "1 Std", TransportType = "Zu Fuß", TransportIcon = "🚶" },
            new RouteMock { Title = "Panoramastraße", Distance = "45 km", Duration = "45 Min", TransportType = "Auto", TransportIcon = "🚗" },
            new RouteMock { Title = "Rheinsteig Etappe 1", Distance = "14 km", Duration = "4 Std", TransportType = "Wandern", TransportIcon = "⛰️" },
            new RouteMock { Title = "Sonntagsspaziergang", Distance = "3 km", Duration = "45 Min", TransportType = "Zu Fuß", TransportIcon = "🚶" },
            new RouteMock { Title = "See-Umrundung", Distance = "8 km", Duration = "2 Std", TransportType = "Fahrrad", TransportIcon = "🚲" },
            new RouteMock { Title = "Gipfeltour Zugspitze", Distance = "21 km", Duration = "10 Std", TransportType = "Wandern", TransportIcon = "⛰️" },
            new RouteMock { Title = "City Tour Berlin", Distance = "12 km", Duration = "3 Std", TransportType = "Zu Fuß", TransportIcon = "🚶" },
            new RouteMock { Title = "Roadtrip Schwarzwald", Distance = "150 km", Duration = "3 Tage", TransportType = "Auto", TransportIcon = "🚗" },
            new RouteMock { Title = "Weinberg-Wanderung", Distance = "9 km", Duration = "2,5 Std", TransportType = "Wandern", TransportIcon = "🍇" },
            new RouteMock { Title = "Feierabend-Runde", Distance = "6 km", Duration = "1,5 Std", TransportType = "Zu Fuß", TransportIcon = "🚶" },
            new RouteMock { Title = "Küstenstraße Sylt", Distance = "30 km", Duration = "1 Std", TransportType = "Auto", TransportIcon = "🚗" },
            new RouteMock { Title = "Nachtwanderung", Distance = "4 km", Duration = "1 Std", TransportType = "Wandern", TransportIcon = "🦉" }
        };

        CreateRouteCommand = new Command(async () => {
            await Shell.Current.Navigation.PushAsync(new Hikr.Maui.App.Views.CreateRoutePage());
        });
    }
}
