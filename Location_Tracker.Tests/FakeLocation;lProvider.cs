using Location_Tracker.Services;
using Microsoft.Maui.Devices.Sensors;

namespace Location_Tracker.Tests
{
    public class FakeLocationProvider : ILocationProvider
    {
        public bool IsListening { get; private set; }

        public event Action<Location>? LocationChanged;

        public event Action<string>? ListeningFailed;

        public Task<bool> StartListeningAsync()
        {
            IsListening = true;
            return Task.FromResult(true);
        }

        public void StopListening()
        {
            IsListening = false;
        }

        public void SendLocation(
            double latitude,
            double longitude)
        {
            Location location = new(
                latitude,
                longitude);

            LocationChanged?.Invoke(location);
        }

        public void SendFailure(string error)
        {
            ListeningFailed?.Invoke(error);
        }
    }
}