using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Hikr
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp() // Maps
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Fullscreen für alle Plattformen konfigurieren
            builder.ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android => android
                    .OnCreate((activity, bundle) =>
                    {
                        // Edge-to-Edge Mode aktivieren
                        activity.Window?.SetDecorFitsSystemWindows(false);
                    }));
#elif IOS
                events.AddiOS(ios => ios
                    .FinishedLaunching((app, options) =>
                    {
                        // Status Bar transparent machen
                        UIKit.UIApplication.SharedApplication.SetStatusBarStyle(
                            UIKit.UIStatusBarStyle.LightContent, false);
                        return true;
                    }));
#endif
            }); // <-- Hier schließt der Lifecycle-Block sauber

            return builder.Build();
        }
    }
}