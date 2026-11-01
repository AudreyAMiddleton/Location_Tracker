using Microsoft.Maui.Devices.Sensors;

namespace Location_Tracker.Services
{
    /// <summary>
    /// Provides foreground location updates through .NET MAUI.
    /// Uses the normal MAUI LocationChanged event and also checks
    /// the current location while tracking so location updates
    /// continue to reach the application reliably.
    /// </summary>
    public class MauiLocationProvider : ILocationProvider
    {
        private CancellationTokenSource? _monitorCancellation;
        private Location? _lastPublishedLocation;
        private bool _isListening;

        public bool IsListening => _isListening;

        public event Action<Location>? LocationChanged;
        public event Action<string>? ListeningFailed;

        public async Task<bool> StartListeningAsync()
        {
            if (_isListening)
            {
                return true;
            }

            try
            {
                RemoveEventSubscriptions();

                Geolocation.LocationChanged +=
                    OnGeolocationLocationChanged;

                Geolocation.ListeningFailed +=
                    OnGeolocationListeningFailed;

                GeolocationListeningRequest listeningRequest =
                    new(GeolocationAccuracy.High);

                bool started =
                    await Geolocation
                        .StartListeningForegroundAsync(
                            listeningRequest);

                if (!started)
                {
                    RemoveEventSubscriptions();

                    ListeningFailed?.Invoke(
                        "Foreground location tracking did not start.");

                    return false;
                }

                _isListening = true;

                _monitorCancellation =
                    new CancellationTokenSource();

                CancellationToken token =
                    _monitorCancellation.Token;

                // Immediately request the current position so the
                // application receives an initial location instead
                // of waiting indefinitely for a movement event.
                await PublishCurrentLocationAsync(token);

                // Continue checking while foreground tracking is active.
                _ = MonitorCurrentLocationAsync(token);

                return true;
            }
            catch (Exception ex)
            {
                StopListening();

                ListeningFailed?.Invoke(
                    $"Location tracking could not start: {ex.Message}");

                return false;
            }
        }

        public void StopListening()
        {
            _isListening = false;

            if (_monitorCancellation is not null)
            {
                _monitorCancellation.Cancel();
                _monitorCancellation.Dispose();
                _monitorCancellation = null;
            }

            RemoveEventSubscriptions();

            try
            {
                if (Geolocation.IsListeningForeground)
                {
                    Geolocation.StopListeningForeground();
                }
            }
            catch
            {
                // Tracking is already being stopped.
            }
        }

        private void OnGeolocationLocationChanged(
            object? sender,
            GeolocationLocationChangedEventArgs e)
        {
            PublishLocation(e.Location);
        }

        private void OnGeolocationListeningFailed(
            object? sender,
            GeolocationListeningFailedEventArgs e)
        {
            _isListening = false;

            if (_monitorCancellation is not null)
            {
                _monitorCancellation.Cancel();
                _monitorCancellation.Dispose();
                _monitorCancellation = null;
            }

            RemoveEventSubscriptions();

            ListeningFailed?.Invoke(
                $"Location listening failed: {e.Error}");
        }

        private async Task MonitorCurrentLocationAsync(
            CancellationToken cancellationToken)
        {
            while (_isListening &&
                   !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        cancellationToken);

                    await PublishCurrentLocationAsync(
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isListening)
                    {
                        ListeningFailed?.Invoke(
                            $"Current location could not be retrieved: {ex.Message}");
                    }
                }
            }
        }

        private async Task PublishCurrentLocationAsync(
            CancellationToken cancellationToken)
        {
            GeolocationRequest request =
                new(
                    GeolocationAccuracy.High,
                    TimeSpan.FromSeconds(5));

            Location? location =
                await Geolocation.GetLocationAsync(
                    request,
                    cancellationToken);

            if (location is not null)
            {
                PublishLocation(location);
            }
        }

        private void PublishLocation(
            Location location)
        {
            if (_lastPublishedLocation is not null)
            {
                bool sameLatitude =
                    Math.Abs(
                        _lastPublishedLocation.Latitude -
                        location.Latitude) < 0.000001;

                bool sameLongitude =
                    Math.Abs(
                        _lastPublishedLocation.Longitude -
                        location.Longitude) < 0.000001;

                if (sameLatitude && sameLongitude)
                {
                    return;
                }
            }

            _lastPublishedLocation = location;

            LocationChanged?.Invoke(location);
        }

        private void RemoveEventSubscriptions()
        {
            Geolocation.LocationChanged -=
                OnGeolocationLocationChanged;

            Geolocation.ListeningFailed -=
                OnGeolocationListeningFailed;
        }
    }
}