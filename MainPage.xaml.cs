using System.ComponentModel;
using System.Diagnostics;
using Location_Tracker.Models;
using Location_Tracker.Services;
using Location_Tracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace Location_Tracker
{
    /// <summary>
    /// Coordinates the Location Tracker user interface while delegating
    /// tracking, persistence, visualization, and distance calculations
    /// to their respective services.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly LocationTrackerViewModel _viewModel;
        private readonly LocationTrackingService _locationTrackingService;
        private readonly LocationDatabaseService _locationDatabase;
        private readonly HeatMapService _heatMapService;
        private readonly DistanceStatisticsService _distanceStatisticsService;

        private bool _eventsSubscribed;

        public MainPage()
        {
            InitializeComponent();

            _viewModel =
                MauiProgram.Services
                    .GetRequiredService<LocationTrackerViewModel>();

            _locationTrackingService =
                MauiProgram.Services
                    .GetRequiredService<LocationTrackingService>();

            _locationDatabase =
                MauiProgram.Services
                    .GetRequiredService<LocationDatabaseService>();

            _heatMapService =
                MauiProgram.Services
                    .GetRequiredService<HeatMapService>();

            _distanceStatisticsService =
                MauiProgram.Services
                    .GetRequiredService<DistanceStatisticsService>();

            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            SubscribeToEvents();

            try
            {
                PermissionStatus status =
                    await LocationPermissionService
                        .CheckAndRequestLocationPermissionAsync();

                if (status != PermissionStatus.Granted)
                {
                    CurrentPositionLabel.Text =
                        "Current Position: Location permission not granted.";

                    StatusLabel.Text =
                        "Status: Location permission required.";

                    StartTrackingButton.IsEnabled = false;
                    StopTrackingButton.IsEnabled = false;

                    await DisplayAlert(
                        "Location Permission",
                        "Location permission is required to track and save location data.",
                        "OK");

                    return;
                }

                await UpdateCurrentPositionAsync();

                List<LocationRecord> savedLocations =
                    await _locationDatabase.GetLocationsAsync();

                Debug.WriteLine(
                    $"Location history loaded: {savedLocations.Count} saved record(s).");

                UpdateSavedLocationDisplay(savedLocations);

                StartTrackingButton.IsEnabled =
                    !_viewModel.IsTracking;

                StopTrackingButton.IsEnabled =
                    _viewModel.IsTracking;

                StatusLabel.Text =
                    _viewModel.IsTracking
                        ? "Status: Tracking"
                        : "Status: Ready";
            }
            catch (Exception ex)
            {
                StatusLabel.Text =
                    "Status: Unable to load location data.";

                Debug.WriteLine(
                    $"Location Tracker could not load normally: {ex.Message}");

                await DisplayAlert(
                    "Location Tracker",
                    "Location Tracker could not load normally.",
                    "OK");
            }
        }

        private async void OnStartTrackingClicked(
            object sender,
            EventArgs e)
        {
            try
            {
                PermissionStatus status =
                    await LocationPermissionService
                        .CheckAndRequestLocationPermissionAsync();

                if (status != PermissionStatus.Granted)
                {
                    StatusLabel.Text =
                        "Status: Location permission required.";

                    await DisplayAlert(
                        "Location Permission",
                        "Location permission is required before tracking can start.",
                        "OK");

                    return;
                }

                await _viewModel.StartTrackingAsync();

                StartTrackingButton.IsEnabled =
                    !_viewModel.IsTracking;

                StopTrackingButton.IsEnabled =
                    _viewModel.IsTracking;

                if (_viewModel.IsTracking)
                {
                    StatusLabel.Text =
                        "Status: Tracking";

                    Debug.WriteLine(
                        "Location tracking started.");
                }
                else
                {
                    string message =
                        _viewModel.LastError
                        ?? "Foreground location tracking did not start.";

                    StatusLabel.Text =
                        $"Status: {message}";

                    await DisplayAlert(
                        "Location Tracking",
                        message,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text =
                    "Status: Tracking could not start.";

                Debug.WriteLine(
                    $"Location tracking could not start: {ex.Message}");

                await DisplayAlert(
                    "Location Tracking",
                    "Location tracking could not start.",
                    "OK");
            }
        }

        private async void OnStopTrackingClicked(
            object sender,
            EventArgs e)
        {
            _viewModel.StopTracking();

            StartTrackingButton.IsEnabled = true;
            StopTrackingButton.IsEnabled = false;

            StatusLabel.Text =
                "Status: Tracking stopped.";

            Debug.WriteLine(
                "Location tracking stopped.");

            await UpdateCurrentPositionAsync();
            await RefreshSavedLocationDisplayAsync();
        }

        private async Task UpdateCurrentPositionAsync()
        {
            try
            {
                GeolocationRequest request =
                    new(
                        GeolocationAccuracy.High,
                        TimeSpan.FromSeconds(10));

                Location? currentLocation =
                    await Geolocation.GetLocationAsync(request);

                if (currentLocation is null)
                {
                    CurrentPositionLabel.Text =
                        "Current Position: Not available";

                    return;
                }

                CurrentPositionLabel.Text =
                    $"Current Position: " +
                    $"{currentLocation.Latitude:F6}, " +
                    $"{currentLocation.Longitude:F6}";
            }
            catch (Exception ex)
            {
                CurrentPositionLabel.Text =
                    "Current Position: Not available";

                Debug.WriteLine(
                    $"Current position could not be retrieved: {ex.Message}");
            }
        }

        private void UpdateDistanceDisplay(
            List<LocationRecord> locations)
        {
            double totalDistanceKilometers =
                _distanceStatisticsService
                    .CalculateTotalDistanceKilometers(locations);

            double totalDistanceMiles =
                totalDistanceKilometers * 0.621371;

            DistanceLabel.Text =
                $"Total Distance: {totalDistanceMiles:F2} mi";
        }

        private void UpdateSavedLocationDisplay(
            List<LocationRecord> savedLocations)
        {
            UpdateDistanceDisplay(savedLocations);

            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                LocationHistoryList.ItemsSource = null;
                LocationHistoryList.ItemsSource = savedLocations;
            }
            else
            {
                _heatMapService.RenderHeatMap(
                    LocationMap,
                    savedLocations);

                if (savedLocations.Count > 0)
                {
                    LocationRecord latestLocation =
                        savedLocations[^1];

                    Location center = new(
                        latestLocation.Latitude,
                        latestLocation.Longitude);

                    LocationMap.MoveToRegion(
                        MapSpan.FromCenterAndRadius(
                            center,
                            Distance.FromKilometers(1)));
                }
            }
        }

        private async Task RefreshSavedLocationDisplayAsync()
        {
            try
            {
                List<LocationRecord> savedLocations =
                    await _locationDatabase.GetLocationsAsync();

                UpdateSavedLocationDisplay(savedLocations);
            }
            catch (Exception ex)
            {
                StatusLabel.Text =
                    "Status: Saved locations could not be refreshed.";

                Debug.WriteLine(
                    $"Saved location display could not be refreshed: {ex.Message}");
            }
        }

        private void OnLocationSaved(
            LocationRecord location)
        {
            MainThread.BeginInvokeOnMainThread(
                async () =>
                {
                    CurrentPositionLabel.Text =
                        $"Current Position: " +
                        $"{location.Latitude:F6}, " +
                        $"{location.Longitude:F6}";

                    StatusLabel.Text =
                        "Status: Tracking";

                    await RefreshSavedLocationDisplayAsync();
                });
        }

        private void OnLocationSaveFailed(
            string message)
        {
            MainThread.BeginInvokeOnMainThread(
                async () =>
                {
                    StatusLabel.Text =
                        $"Status: {message}";

                    await DisplayAlert(
                        "Location Save Error",
                        message,
                        "OK");
                });
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                async () =>
                {
                    if (e.PropertyName ==
                        nameof(LocationTrackerViewModel.IsTracking))
                    {
                        StartTrackingButton.IsEnabled =
                            !_viewModel.IsTracking;

                        StopTrackingButton.IsEnabled =
                            _viewModel.IsTracking;
                    }

                    if (e.PropertyName ==
                            nameof(LocationTrackerViewModel.LastError) &&
                        !string.IsNullOrWhiteSpace(_viewModel.LastError))
                    {
                        StatusLabel.Text =
                            $"Status: {_viewModel.LastError}";

                        await DisplayAlert(
                            "Location Tracking",
                            _viewModel.LastError,
                            "OK");
                    }
                });
        }

        private void SubscribeToEvents()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            _locationTrackingService.LocationSaved +=
                OnLocationSaved;

            _locationTrackingService.LocationSaveFailed +=
                OnLocationSaveFailed;

            _viewModel.PropertyChanged +=
                OnViewModelPropertyChanged;

            _eventsSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            _locationTrackingService.LocationSaved -=
                OnLocationSaved;

            _locationTrackingService.LocationSaveFailed -=
                OnLocationSaveFailed;

            _viewModel.PropertyChanged -=
                OnViewModelPropertyChanged;

            _eventsSubscribed = false;
        }

        protected override void OnDisappearing()
        {
            _viewModel.StopTracking();

            UnsubscribeFromEvents();

            StartTrackingButton.IsEnabled = true;
            StopTrackingButton.IsEnabled = false;

            base.OnDisappearing();
        }
    }
}