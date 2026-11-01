# Location Tracker

Location Tracker is a cross-platform .NET MAUI application that records device location data, stores location history in SQLite, visualizes movement history, and calculates total traveled distance.

The application uses Android as the primary platform and Windows as an additional .NET MAUI target.

The Android version provides live GPS tracking and heat-map visualization. The Windows version displays persisted location history in a scrolling list because .NET MAUI Maps on Windows requires an Azure Maps resource that was not available for this project environment.

## Project Purpose

The purpose of this project is to demonstrate multiplatform software development using .NET MAUI while applying software engineering concepts including:

- Separation of responsibilities
- Dependency inversion
- Service-based architecture
- Dependency injection and shared service registration
- Persistent data storage
- Event-driven application updates
- Secure handling of sensitive location data
- Location permission handling
- Automated unit testing
- Platform-specific application behavior
- Maintainable application design

## Main Features

- Current device location display using .NET MAUI geolocation
- Start Tracking and Stop Tracking controls
- Continuous foreground location tracking
- Live location updates while tracking is active
- Persistent location history stored in SQLite
- Saved history retained after the application is closed and reopened
- Android map visualization
- Application-specific HeatMap control derived from the .NET MAUI Map control
- Heat-map visualization based on saved location readings
- Live heat-map updates while tracking is active
- Windows saved-location history displayed in a scrolling list
- Total traveled-distance calculation
- Distance displayed in miles
- Live distance-statistics updates while tracking is active
- User-visible tracking status
- User-visible location and tracking error messages
- Graceful location-permission handling
- Unit testing through a fake location provider
- Android support as the primary application target
- Windows support as an additional .NET MAUI target

## Application Architecture

The application separates location acquisition, tracking, persistence, visualization, statistics, and user-interface responsibilities into dedicated components.

### User Interface and ViewModel Flow

```text
MainPage
   ↓
LocationTrackerViewModel
   ↓
ILocationProvider
   ↓
MauiLocationProvider
```

`MainPage` provides the application interface and coordinates the display of tracking information.

`LocationTrackerViewModel` manages tracking state and receives location updates through the `ILocationProvider` abstraction.

`ILocationProvider` separates the application from the concrete .NET MAUI geolocation implementation.

`MauiLocationProvider` provides real device-location functionality during normal application use.

### Location Persistence Flow

```text
MauiLocationProvider
   ↓
LocationTrackingService
   ↓
LocationDatabaseService
   ↓
SQLite Database
```

`LocationTrackingService` receives location updates from `ILocationProvider`, converts them into persistent `LocationRecord` objects, and saves them through `LocationDatabaseService`.

`LocationDatabaseService` manages the SQLite database and provides access to saved location records.

### Live UI Update Flow

```text
LocationTrackingService
   ↓
LocationSaved event
   ↓
MainPage
   ↓
Refresh saved location history
   ↓
HeatMapService
DistanceStatisticsService
```

After a location is saved successfully, `LocationTrackingService` raises a `LocationSaved` event.

`MainPage` responds to that event by retrieving the updated location history and refreshing the user interface.

On Android, the heat map and total-distance statistics update while tracking remains active. The user does not have to press Stop Tracking before seeing updated movement data.

### Dependency Registration

Application services and the ViewModel are registered in `MauiProgram.cs`.

Registered components include:

- `LocationDatabaseService`
- `ILocationProvider`
- `MauiLocationProvider`
- `LocationTrackingService`
- `LocationTrackerViewModel`
- `HeatMapService`
- `DistanceStatisticsService`

`MainPage` resolves these registered dependencies from the application service provider rather than constructing separate service instances directly.

This allows the application to share the same registered service instances across the running application.

## Location Provider Architecture

The application uses the `ILocationProvider` interface to separate location behavior from the .NET MAUI geolocation implementation.

```text
ILocationProvider
      ↓
MauiLocationProvider
```

During normal application use, `MauiLocationProvider` receives location information from .NET MAUI geolocation services.

The provider uses foreground location listening and current-location checks while tracking is active so that changing device coordinates continue to reach the application.

The provider exposes:

- Current listening state
- Location update events
- Listening-failure events
- Start-listening behavior
- Stop-listening behavior

This design keeps platform-specific geolocation behavior outside the ViewModel and tracking service.

## Testable ViewModel Architecture

The same `ILocationProvider` abstraction also allows the ViewModel to be tested without requiring a physical device or emulator movement.

### Normal Application Use

```text
LocationTrackerViewModel
   ↓
ILocationProvider
   ↓
MauiLocationProvider
```

### Automated Testing

```text
LocationTrackerViewModel
   ↓
ILocationProvider
   ↓
FakeLocationProvider
```

This dependency inversion allows `FakeLocationProvider` to simulate tracking behavior during automated tests.

## Project Structure

Major project components include:

```text
Location_Tracker
│
├── Controls
│   └── HeatMap.cs
│
├── Models
│   └── LocationRecord.cs
│
├── Services
│   ├── DistanceStatisticsService.cs
│   ├── HeatMapService.cs
│   ├── ILocationProvider.cs
│   ├── LocationDatabaseService.cs
│   ├── LocationPermissionService.cs
│   ├── LocationTrackingService.cs
│   └── MauiLocationProvider.cs
│
├── ViewModels
│   └── LocationTrackerViewModel.cs
│
├── Platforms
│   ├── Android
│   ├── iOS
│   ├── MacCatalyst
│   └── Windows
│
├── MainPage.xaml
├── MainPage.xaml.cs
├── MauiProgram.cs
└── Location_Tracker.csproj

Location_Tracker.Tests
│
├── FakeLocationProvider.cs
├── UnitTest1.cs
└── Location_Tracker.Tests.csproj
```

## Location Permissions

Location Tracker requires location permission because the application must access device-position information to:

- Display the current position
- Track movement
- Save location history
- Update the Android heat map
- Calculate total traveled distance

Before requesting location permission, the application explains why location access is required.

If permission is denied:

- The application remains open
- Tracking does not begin
- Tracking controls are disabled when appropriate
- The user receives a visible status message
- The application displays a user-facing permission message

If location permission has already been granted, the application opens normally without repeatedly requesting permission.

## SQLite Storage

Location readings are converted into `LocationRecord` objects and stored in the application's SQLite database.

Each record contains:

- Latitude
- Longitude
- Timestamp

The SQLite database is stored inside the application's private local application-data directory using:

```text
FileSystem.AppDataDirectory
```

Saved location records remain available after the application is closed and reopened.

The same database is used by the application for:

- Location history
- Heat-map visualization
- Windows history display
- Distance calculations

## Android Visualization

Android displays saved location history through an application-specific `HeatMap` control.

`HeatMap` derives from the .NET MAUI `Map` control:

```text
HeatMap
   ↓
.NET MAUI Map
```

Saved location readings are passed to `HeatMapService`, which groups nearby readings and creates visual location-density indicators.

The heat-map visualization uses different visual densities to represent areas containing different concentrations of saved location readings.

While tracking is active:

1. A new location is received.
2. `LocationTrackingService` saves the reading.
3. `LocationSaved` is raised.
4. `MainPage` reloads saved records.
5. `HeatMapService` redraws the visualization.
6. `DistanceStatisticsService` recalculates traveled distance.
7. The interface updates without requiring the user to stop tracking.

This provides live movement visualization during foreground tracking.

## Windows Visualization

The Windows version displays saved location history in a scrolling list.

Each saved record displays:

- Timestamp
- Latitude
- Longitude

Windows uses the same persistent SQLite database as the Android version.

A list is used on Windows because the Windows implementation of .NET MAUI Maps requires an Azure Maps resource that was not available in the project environment.

This allows the Windows version to satisfy the requirement that persisted location history be displayed as either a map or a list.

## Distance Statistics

`DistanceStatisticsService` calculates the distance between consecutive saved location readings.

The individual distances are combined to determine total traveled distance.

The application converts the calculated distance from kilometers to miles for display.

Example:

```text
Total Distance: 12.70 mi
```

While Android foreground tracking is active, total distance updates as new movement data is received and saved.

Distance statistics are also available from persisted records after the application is reopened.

## Tracking Status

The application displays visible tracking status information.

Possible states include:

```text
Status: Ready
Status: Tracking
Status: Tracking stopped.
```

Tracking or location errors are also displayed to the user instead of being written only to diagnostic output.

## Unit Testing

The solution contains a separate automated test project:

```text
Location_Tracker.Tests
```

The tests use `FakeLocationProvider` instead of the real device geolocation service.

`LocationTrackerViewModel` receives location functionality through `ILocationProvider`, allowing the fake implementation to be injected during testing.

Current automated tests verify that:

- `LocationTrackerViewModel` starts tracking using `FakeLocationProvider`
- `LocationTrackerViewModel` stops tracking using `FakeLocationProvider`
- A simulated location update changes the ViewModel latitude and longitude values

Current test result:

```text
3 Passed
0 Failed
0 Skipped
```

## Android Runtime Testing

The primary Android test environment is the Pixel 7 emulator.

Android Extended Controls can simulate device GPS movement.

### Continuous Route Test

To verify live foreground tracking:

1. Open the application in the Pixel 7 Android emulator.
2. Confirm the application displays `Status: Ready`.
3. Select `Start Tracking`.
4. Confirm the application displays:

```text
Status: Tracking
```

5. Open Android Emulator Extended Controls.
6. Select `Location`.
7. Select `Routes`.
8. Select or create a saved route.
9. Click `Play Route`.
10. Leave Location Tracker running while the simulated device moves.

While the route is playing and the application still displays `Status: Tracking`, verify that:

- Current Position changes as the emulator moves
- New location readings are saved
- The heat-map visualization updates
- The map follows the updated location history
- Total Distance increases
- Stop Tracking remains available

The Android route test verifies the complete runtime path:

```text
Android Emulator GPS
        ↓
.NET MAUI Geolocation
        ↓
MauiLocationProvider
        ↓
LocationTrackingService
        ↓
LocationDatabaseService
        ↓
SQLite
        ↓
LocationSaved event
        ↓
MainPage
        ↓
HeatMapService
DistanceStatisticsService
```

After the route test:

1. Stop the emulator route.
2. Select `Stop Tracking`.
3. Close the application.
4. Reopen the application.
5. Verify that saved history remains available.

## Windows Runtime Testing

The Windows version can be launched using `Windows Machine` in Visual Studio.

Windows testing verifies that:

- The application builds successfully
- The application launches successfully
- Current position can be displayed
- Start Tracking begins tracking
- Stop Tracking ends tracking
- Location records are stored in SQLite
- Saved location history appears in the Windows list
- Saved records remain available after the application is closed and reopened
- Distance statistics are calculated from saved records

## Google Maps API Key

A valid Google Maps API key is required for Android map functionality.

The real API key must not be committed to the public GitHub repository.

The Android manifest uses the placeholder:

```text
${MAPS_API_KEY}
```

The real API key must be configured locally before building and running the Android version.

## Security Considerations

Location information is treated as sensitive application data.

Security-related implementation decisions include:

- SQLite data is stored inside the application's private local application-data directory
- Android application backup is disabled
- Raw latitude and longitude values are not written to diagnostic logs when saved
- API keys are not committed to the public repository
- The Android manifest contains only a sanitized API-key placeholder
- Location permission is requested only when needed
- Tracking begins only after the user selects Start Tracking
- Tracking can be stopped by the user
- User-visible error handling is provided for permission and location failures

Android backup is disabled using:

```text
android:allowBackup="false"
```

This reduces the possibility of stored SQLite location history being included in normal Android application backups.

## Build and Run

### Android

1. Open the solution in Visual Studio.
2. Restore required NuGet packages.
3. Configure the Google Maps API key locally.
4. Select the Pixel 7 Android emulator or another supported Android device.
5. Build the solution.
6. Run the Location Tracker project.
7. Review the location-permission explanation.
8. Grant location permission.
9. Select `Start Tracking`.
10. Confirm `Status: Tracking`.
11. Simulate or physically generate movement.
12. Verify live location, heat-map, and distance updates.
13. Select `Stop Tracking` when tracking is complete.

### Windows

1. Open the solution in Visual Studio.
2. Select `Windows Machine`.
3. Build the solution.
4. Run the Location Tracker project.
5. Verify the current position.
6. Select `Start Tracking`.
7. Allow location readings to be collected.
8. Select `Stop Tracking`.
9. Verify saved records in `Saved Location History`.
10. Close and reopen the application to verify persistence.

## Build Verification

The solution has been rebuilt successfully after the architecture and live-tracking updates.

The final verification process includes:

- Successful solution rebuild
- Zero compilation errors
- Successful Android deployment
- Android runtime launch
- Foreground tracking start
- Simulated route movement
- Live current-position updates
- Live heat-map updates
- Live distance-statistics updates
- Tracking stop
- SQLite persistence
- Successful automated test execution
- 3 passing automated tests

## Technologies

- .NET MAUI
- C#
- XAML
- SQLite
- Google Maps
- .NET MAUI Geolocation
- Dependency Injection
- Event-driven programming
- xUnit
- Visual Studio
- Android Emulator
- Windows

## Software Engineering Concepts Demonstrated

This project applies several software-engineering concepts, including:

- Multiplatform application development
- Separation of concerns
- Dependency inversion
- Dependency injection
- Interface-based design
- ViewModel architecture
- Service-based architecture
- Event-driven application updates
- Persistent data storage
- Asynchronous programming
- Platform-specific behavior
- Permission management
- Error handling
- Automated unit testing
- Secure configuration management
- Sensitive-data handling
- Runtime verification

## Repository

This repository contains the complete Location Tracker application, supporting services, platform-specific configuration, automated test project, and project documentation.
