using System.ComponentModel;
using System.Runtime.CompilerServices;
using Location_Tracker.Services;
using Microsoft.Maui.Devices.Sensors;

namespace Location_Tracker.ViewModels
{
    /// <summary>
    /// Provides testable location-tracking state for the user interface.
    /// Location access is supplied through ILocationProvider so a fake
    /// provider can be injected during unit testing.
    /// </summary>
    public class LocationTrackerViewModel : INotifyPropertyChanged
    {
        private readonly ILocationProvider _locationProvider;

        private bool _isTracking;
        private double _currentLatitude;
        private double _currentLongitude;
        private string? _lastError;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsTracking
        {
            get => _isTracking;
            private set
            {
                if (_isTracking == value)
                {
                    return;
                }

                _isTracking = value;
                OnPropertyChanged();
            }
        }

        public double CurrentLatitude
        {
            get => _currentLatitude;
            private set
            {
                if (_currentLatitude == value)
                {
                    return;
                }

                _currentLatitude = value;
                OnPropertyChanged();
            }
        }

        public double CurrentLongitude
        {
            get => _currentLongitude;
            private set
            {
                if (_currentLongitude == value)
                {
                    return;
                }

                _currentLongitude = value;
                OnPropertyChanged();
            }
        }

        public string? LastError
        {
            get => _lastError;
            private set
            {
                if (_lastError == value)
                {
                    return;
                }

                _lastError = value;
                OnPropertyChanged();
            }
        }

        public LocationTrackerViewModel(
            ILocationProvider locationProvider)
        {
            _locationProvider = locationProvider;

            _locationProvider.LocationChanged +=
                OnLocationChanged;

            _locationProvider.ListeningFailed +=
                OnListeningFailed;
        }

        public async Task<bool> StartTrackingAsync()
        {
            LastError = null;

            bool started =
                await _locationProvider.StartListeningAsync();

            IsTracking = started;

            if (!started && string.IsNullOrWhiteSpace(LastError))
            {
                LastError =
                    "Foreground location tracking did not start.";
            }

            return started;
        }

        public void StopTracking()
        {
            _locationProvider.StopListening();

            IsTracking = false;
        }

        private void OnLocationChanged(Location location)
        {
            CurrentLatitude = location.Latitude;
            CurrentLongitude = location.Longitude;
        }

        private void OnListeningFailed(string error)
        {
            IsTracking = false;

            LastError =
                $"Location listening failed: {error}";
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}