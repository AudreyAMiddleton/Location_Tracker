using Location_Tracker.ViewModels;
using Xunit;

namespace Location_Tracker.Tests
{
    public class LocationTrackerViewModelTests
    {
        [Fact]
        public async Task StartTrackingAsync_StartsTrackingWithFakeLocationProvider()
        {
            FakeLocationProvider fakeProvider = new();

            LocationTrackerViewModel viewModel =
                new(fakeProvider);

            bool started =
                await viewModel.StartTrackingAsync();

            Assert.True(started);
            Assert.True(fakeProvider.IsListening);
            Assert.True(viewModel.IsTracking);
        }

        [Fact]
        public async Task StopTracking_StopsTrackingWithFakeLocationProvider()
        {
            FakeLocationProvider fakeProvider = new();

            LocationTrackerViewModel viewModel =
                new(fakeProvider);

            await viewModel.StartTrackingAsync();

            viewModel.StopTracking();

            Assert.False(fakeProvider.IsListening);
            Assert.False(viewModel.IsTracking);
        }

        [Fact]
        public async Task FakeLocationUpdate_UpdatesViewModelCurrentPosition()
        {
            FakeLocationProvider fakeProvider = new();

            LocationTrackerViewModel viewModel =
                new(fakeProvider);

            await viewModel.StartTrackingAsync();

            fakeProvider.SendLocation(
                36.9685,
                -86.4808);

            Assert.Equal(
                36.9685,
                viewModel.CurrentLatitude,
                4);

            Assert.Equal(
                -86.4808,
                viewModel.CurrentLongitude,
                4);
        }
    }
}