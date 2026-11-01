using Location_Tracker.Services;
using Location_Tracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Location_Tracker
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

            // Shared application services.
            builder.Services.AddSingleton<LocationDatabaseService>();
            builder.Services.AddSingleton<ILocationProvider, MauiLocationProvider>();
            builder.Services.AddSingleton<LocationTrackingService>();
            builder.Services.AddSingleton<LocationTrackerViewModel>();
            builder.Services.AddSingleton<HeatMapService>();
            builder.Services.AddSingleton<DistanceStatisticsService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            MauiApp app = builder.Build();

            Services = app.Services;

            return app;
        }
    }
}