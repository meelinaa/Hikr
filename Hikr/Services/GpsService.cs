using System;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;

namespace Hikr.Services;

/// <summary>
/// Service encapsulating GPS location retrieval and Compass reading updates using MAUI platform sensors.
/// </summary>
public class GpsService
{
    /// <summary>
    /// Event triggered when a new magnetic compass reading is available.
    /// </summary>
    public event Action<double>? CompassReadingChanged;

    /// <summary>
    /// Retrieves the current system GPS location coordinates.
    /// </summary>
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Subscribes to the device compass sensor.
    /// </summary>
    public void StartCompass()
    {
        try
        {
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring)
                {
                    Compass.Default.ReadingChanged += OnCompassReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Compass start failure: {ex.Message}");
        }
    }

    /// <summary>
    /// Unsubscribes from the device compass sensor.
    /// </summary>
    public void StopCompass()
    {
        try
        {
            if (Compass.Default.IsSupported && Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged -= OnCompassReadingChanged;
                Compass.Default.Stop();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Compass stop failure: {ex.Message}");
        }
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        CompassReadingChanged?.Invoke(e.Reading.HeadingMagneticNorth);
    }
}
