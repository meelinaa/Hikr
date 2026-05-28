using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Hikr;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // 1. Dem Fenster erlauben, die System-Begrenzungen zu ignorieren (Edge-to-Edge)
        Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            // Modernes Android: Macht die Navigationsleiste unten komplett transparent
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
        }
    }
}
