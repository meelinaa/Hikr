using System.Collections.ObjectModel;
using System.Windows.Input;
using Hikr.Domain.Enums;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Hikr.Maui.App.ViewModels;

public class WaypointMock
{
    public string Name { get; set; }
}

public class CreateRouteViewModel : BindableObject
{
    public ObservableCollection<WaypointMock> Waypoints { get; set; }

    private Profiles _selectedProfile = Profiles.Foot;
    public Profiles SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (_selectedProfile != value)
            {
                _selectedProfile = value;
                OnPropertyChanged(nameof(SelectedProfile));
                OnPropertyChanged(nameof(FootBackgroundColor));
                OnPropertyChanged(nameof(FootBorderColor));
                OnPropertyChanged(nameof(BikeBackgroundColor));
                OnPropertyChanged(nameof(BikeBorderColor));
                OnPropertyChanged(nameof(CarBackgroundColor));
                OnPropertyChanged(nameof(CarBorderColor));
            }
        }
    }

    public Color FootBackgroundColor => SelectedProfile == Profiles.Foot ? Color.FromArgb("#e8f5e9") : Colors.Transparent;
    public Color FootBorderColor => SelectedProfile == Profiles.Foot ? Color.FromArgb("#1b4d3e") : Colors.LightGray;

    public Color BikeBackgroundColor => SelectedProfile == Profiles.Bike ? Color.FromArgb("#e8f5e9") : Colors.Transparent;
    public Color BikeBorderColor => SelectedProfile == Profiles.Bike ? Color.FromArgb("#1b4d3e") : Colors.LightGray;

    public Color CarBackgroundColor => SelectedProfile == Profiles.Car ? Color.FromArgb("#e8f5e9") : Colors.Transparent;
    public Color CarBorderColor => SelectedProfile == Profiles.Car ? Color.FromArgb("#1b4d3e") : Colors.LightGray;


    public ICommand AddWaypointCommand { get; }
    public ICommand RemoveWaypointCommand { get; }
    public ICommand SelectProfileCommand { get; }
    public ICommand GoBackCommand { get; }
    public ICommand SaveRouteCommand { get; }

    public CreateRouteViewModel()
    {
        Waypoints = new ObservableCollection<WaypointMock>
        {
            new WaypointMock { Name = "" },
            new WaypointMock { Name = "" }
        };

        AddWaypointCommand = new Command(() => Waypoints.Add(new WaypointMock { Name = "" }));
        RemoveWaypointCommand = new Command<WaypointMock>(wp => {
            if (Waypoints.Contains(wp))
                Waypoints.Remove(wp);
        });

        SelectProfileCommand = new Command<Profiles>(profile => SelectedProfile = profile);

        GoBackCommand = new Command(async () => {
            await Shell.Current.Navigation.PopAsync();
        });

        SaveRouteCommand = new Command(async () => {
            await Shell.Current.Navigation.PopAsync();
        });
    }
}
