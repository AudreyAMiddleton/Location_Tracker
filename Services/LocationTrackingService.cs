using System.Diagnostics;
using Location_Tracker.Models;
using Microsoft.Maui.Devices.Sensors;

namespace Location_Tracker.Services
{
    /// <summary>
    /// Persists location updates supplied through ILocationProvider.
    /// The provider and database are supplied through dependency injection.
    /// </summary>
    public class LocationTrackingService
    {
        private readonly LocationDatabaseService _locationDatabase;

        public event Action<LocationRecord>? LocationSaved;

        public event Action<string>? LocationSaveFailed;

        public LocationTrackingService(
            ILocationProvider locationProvider,
            LocationDatabaseService locationDatabase)
        {
            _locationDatabase = locationDatabase;

            locationProvider.LocationChanged +=
                OnLocationChanged;
        }

        private async void OnLocationChanged(Location location)
        {
            try
            {
                LocationRecord locationRecord = new()
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Timestamp = location.Timestamp.UtcDateTime
                };

                await _locationDatabase
                    .SaveLocationAsync(locationRecord);

                Debug.WriteLine(
                    "Location record saved successfully.");

                LocationSaved?.Invoke(locationRecord);
            }
            catch (Exception ex)
            {
                string message =
                    $"Location could not be saved: {ex.Message}";

                Debug.WriteLine(message);

                LocationSaveFailed?.Invoke(message);
            }
        }
    }
}