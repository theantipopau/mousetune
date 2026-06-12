Create a complete, production-ready Windows desktop application called **MouseTune**.

## Project purpose

MouseTune is a lightweight, clean and minimalist Windows utility for managing Bluetooth and USB mice.

The application must allow a user to:

1. View connected and paired mouse devices.
2. Change the local Windows display name of a selected mouse.
3. Adjust the effective mouse sensitivity using an intuitive DPI-style interface.
4. Manage Windows pointer speed and pointer acceleration.
5. Save and restore device-specific profiles.

The application should feel similar to a simplified Logitech G HUB mouse configuration screen, but without unnecessary background services, animations, telemetry, accounts or large dependencies.

## Important technical distinction

The application must clearly distinguish between:

* **Hardware DPI**, which may only be available when a mouse exposes a supported vendor-specific HID or Bluetooth command.
* **Effective DPI**, which is calculated by adjusting Windows pointer speed and acceleration.

For generic mice, the application must not falsely claim to modify the physical sensor DPI.

Display this wording in the interface where appropriate:

> Effective DPI adjusts how quickly the pointer moves in Windows. The physical sensor DPI can only be changed on supported mouse models.

For unsupported devices, the DPI control should operate as an intuitive frontend for Windows mouse speed settings.

## Technology stack

Use:

* C#
* .NET 8
* WPF
* MVVM architecture
* Minimal external NuGet dependencies
* Windows native APIs through P/Invoke where required
* Visual Studio Code compatible project structure
* Standard `dotnet` CLI commands
* Self-contained Windows x64 publishing support

Do not use Electron, WebView2, Tauri, Node.js or a browser-based interface.

The installed application should ideally remain below approximately 30–40 MB when framework-dependent and should use minimal memory and CPU while idle.

## Design requirements

Create a minimalist Windows 11-style interface.

Visual direction:

* Clean white and dark backgrounds depending on theme.
* Rounded cards with subtle borders.
* Minimal shadows.
* Neutral greys with a single blue accent colour.
* Segoe UI or the normal Windows system font.
* No excessive gradients.
* No oversized branding.
* No animated backgrounds.
* Responsive layout suitable for windows from approximately 720 × 500 upwards.
* Support Windows light and dark themes.
* Use clear status indicators and concise wording.

The application should have a single main window with three primary areas:

### Left sidebar

* Application name: MouseTune
* Devices
* Profiles
* Settings
* About

### Device list

Show detected mouse devices with:

* Friendly name
* Device type: Bluetooth, Bluetooth LE, USB or unknown
* Connection status
* Vendor ID
* Product ID
* Bluetooth address when available
* Whether hardware DPI control is supported
* Current saved profile

Use a simple mouse icon for each device.

### Main device panel

For the selected mouse, show:

* Current Windows display name
* Editable custom name
* Apply Name button
* Restore Original Name button
* Sensitivity control
* Effective DPI value
* Windows pointer-speed value
* Enhance pointer precision toggle
* Apply button
* Reset to Windows Default button
* Save Profile button

## Device renaming

Implement local Windows device renaming as safely as possible.

The application should attempt to change the selected mouse’s local Windows-friendly display name from a generic name such as:

* BT 5.2 Mouse
* Bluetooth Mouse
* BLE Mouse
* Wireless Mouse

to a user-defined name such as:

* Logitech G304
* Office Mouse
* Gaming Mouse

Use documented Windows device APIs where possible, including SetupAPI and Configuration Manager APIs.

Investigate and implement appropriate use of:

* `SetupDiGetClassDevs`
* `SetupDiEnumDeviceInfo`
* `SetupDiGetDeviceProperty`
* `SetupDiSetDeviceProperty`
* `CM_Get_DevNode_Property`
* `CM_Set_DevNode_Property`

The program may set properties such as the local device friendly name where Windows permits it.

Do not blindly write arbitrary bytes to the registry.

If direct registry modification is required as a fallback:

* Isolate it in a dedicated service.
* Back up the original value.
* Validate the target device instance ID.
* Only modify the selected device.
* Log the exact change.
* Allow the user to restore the previous value.
* Never modify unrelated Bluetooth devices.
* Never modify driver binaries or firmware.
* Require administrator elevation only for the operation that needs it.

The interface must explain:

> This changes the name shown on this Windows computer. It does not rename the mouse firmware or change the name shown on other devices.

After changing the name:

* Refresh the device list.
* Attempt a device-property refresh.
* Inform the user when a disconnect/reconnect or Windows restart is required.
* Store the preferred alias in the application settings.
* Reapply the alias when the device reconnects if Windows replaces it.

Do not create a permanently running Windows service for the initial version.

Instead, support an optional lightweight startup task that launches the application minimised and reapplies saved aliases.

## Mouse discovery

Create a `MouseDeviceDiscoveryService` that identifies likely mouse devices.

Use:

* Windows SetupAPI
* HID device enumeration
* Bluetooth device interfaces
* Device instance properties
* HID usage page and usage information where available

Do not classify every HID device as a mouse.

Try to identify devices using:

* HID Usage Page `0x01`
* HID Usage `0x02`
* Mouse-compatible device classes
* Bluetooth device metadata
* Parent and child device relationships

Avoid showing:

* Keyboards
* Touchscreens
* Consumer-control devices
* Game controllers
* Virtual HID devices unless enabled in Settings

Create a device model containing:

```csharp
public sealed class MouseDevice
{
    public string DeviceInstanceId { get; init; }
    public string InterfacePath { get; init; }
    public string CurrentName { get; set; }
    public string OriginalName { get; init; }
    public string Manufacturer { get; init; }
    public string VendorId { get; init; }
    public string ProductId { get; init; }
    public string BluetoothAddress { get; init; }
    public MouseConnectionType ConnectionType { get; init; }
    public bool IsConnected { get; init; }
    public bool SupportsHardwareDpi { get; init; }
}
```

Handle missing properties safely.

## Effective DPI system

Create an intuitive DPI-style control for generic mice.

The user should be able to:

* Type a numeric effective DPI value.
* Use a horizontal slider.
* Select common presets.
* Adjust Windows pointer speed.
* Enable or disable pointer acceleration.

Provide presets:

* 400
* 800
* 1200
* 1600
* 2400
* 3200
* 6400

For generic devices, map effective DPI values to Windows pointer-speed settings.

Use Windows `SystemParametersInfo` with:

* `SPI_GETMOUSESPEED`
* `SPI_SETMOUSESPEED`
* `SPI_GETMOUSE`
* `SPI_SETMOUSE`

Use:

* `SPIF_UPDATEINIFILE`
* `SPIF_SENDCHANGE`

Windows pointer speed typically uses an integer range from 1 to 20.

Create a transparent and deterministic mapping between the DPI-style value and Windows speed.

Use 800 effective DPI as the baseline corresponding to the normal Windows pointer-speed position.

A reasonable initial mapping is:

```text
400 DPI  -> Windows speed 5
800 DPI  -> Windows speed 10
1200 DPI -> Windows speed 12
1600 DPI -> Windows speed 14
2400 DPI -> Windows speed 16
3200 DPI -> Windows speed 18
6400 DPI -> Windows speed 20
```

Use interpolation for values between presets.

Clamp values safely:

* Minimum effective DPI: 200
* Maximum effective DPI: 6400
* Windows speed: 1–20

The interface must always show both values:

```text
Effective DPI: 1600
Windows pointer speed: 14 / 20
```

Do not imply that a generic mouse’s sensor has physically changed.

## Pointer acceleration

Provide a toggle labelled:

> Enhance pointer precision

Read and modify the underlying Windows mouse acceleration settings.

Preserve the original settings before making changes.

Allow the user to reset:

* Pointer speed
* Mouse thresholds
* Acceleration settings

Use safe defaults corresponding as closely as possible to normal Windows defaults.

## Hardware DPI support architecture

Build an extensible architecture for future hardware DPI support.

Create:

```csharp
public interface IMouseHardwareProvider
{
    string ProviderName { get; }

    bool CanHandle(MouseDevice device);

    Task<MouseCapabilities> GetCapabilitiesAsync(
        MouseDevice device,
        CancellationToken cancellationToken);

    Task<int?> GetHardwareDpiAsync(
        MouseDevice device,
        CancellationToken cancellationToken);

    Task<OperationResult> SetHardwareDpiAsync(
        MouseDevice device,
        int dpi,
        CancellationToken cancellationToken);
}
```

Provide an initial:

```text
GenericMouseProvider
```

The generic provider should:

* Report that hardware DPI is unsupported.
* Use the effective DPI system.
* Never send undocumented HID feature reports.
* Never send random Bluetooth GATT writes.

Create the folder structure so future providers could be added for known devices.

Example:

```text
Providers/
    GenericMouseProvider.cs
    LogitechMouseProvider.cs
    RazerMouseProvider.cs
```

Only implement placeholder classes for unsupported vendor providers. Do not pretend they work.

## Profiles

Allow profiles to be saved per device.

Each profile should contain:

```csharp
public sealed class MouseProfile
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public string DeviceInstanceId { get; set; }
    public string PreferredDeviceName { get; set; }
    public int EffectiveDpi { get; set; }
    public int WindowsPointerSpeed { get; set; }
    public bool EnhancePointerPrecision { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
}
```

Support:

* Creating profiles
* Renaming profiles
* Applying profiles
* Deleting profiles
* Setting a default profile for a device
* Automatically applying a profile when a recognised mouse reconnects

Store configuration as JSON in:

```text
%LocalAppData%\MouseTune\
```

Example files:

```text
settings.json
profiles.json
device-aliases.json
logs\
```

Use atomic file writes to reduce the risk of corruption.

## Permissions

The application should run without administrator rights for normal sensitivity controls.

Only request elevation when a rename operation genuinely requires it.

Do not require the entire application to always run as administrator.

Create a small elevated helper process if needed for protected device-property operations.

The helper must:

* Accept narrowly scoped commands.
* Validate all input.
* Only change the explicitly selected device.
* Return a structured result.
* Exit immediately after completing the operation.

## Logging

Implement lightweight structured logging.

Log:

* Application startup
* Device enumeration
* Rename attempts
* Property changes
* Pointer-speed changes
* Profile application
* Errors

Do not log:

* User documents
* Keystrokes
* Mouse movement
* Personal account information
* Unrelated Bluetooth devices beyond basic enumeration metadata

Provide an Export Diagnostics button that creates a text or JSON file containing:

* Application version
* Windows version
* Detected mouse metadata
* Relevant error messages
* Supported capabilities

Do not include secrets or unrelated device information.

## Reliability and safety

The application must:

* Handle disconnected devices.
* Handle stale device paths.
* Handle permission failures.
* Handle unsupported Windows properties.
* Handle missing Bluetooth addresses.
* Handle duplicate HID interfaces belonging to one physical mouse.
* Prevent repeated rename operations.
* Avoid UI freezes.
* Use cancellation tokens for device enumeration.
* Keep native handles scoped and disposed correctly.
* Use `SafeHandle` types where practical.
* Validate all native API return values.
* Show understandable error messages.

Never:

* Flash firmware.
* Install unsigned drivers.
* Disable Windows security.
* Change random registry values.
* Send undocumented commands to unknown hardware.
* Claim hardware DPI support when unavailable.

## Architecture

Use this project structure:

```text
MouseTune.sln

src/
    MouseTune.App/
        App.xaml
        App.xaml.cs
        MainWindow.xaml
        MainWindow.xaml.cs

        Views/
            DevicesView.xaml
            ProfilesView.xaml
            SettingsView.xaml
            AboutView.xaml

        ViewModels/
            MainViewModel.cs
            DevicesViewModel.cs
            ProfilesViewModel.cs
            SettingsViewModel.cs
            DeviceViewModel.cs

        Models/
            MouseDevice.cs
            MouseProfile.cs
            MouseCapabilities.cs
            OperationResult.cs
            AppSettings.cs

        Services/
            MouseDeviceDiscoveryService.cs
            DeviceRenameService.cs
            PointerSettingsService.cs
            ProfileService.cs
            SettingsService.cs
            StartupService.cs
            DiagnosticsService.cs

        Native/
            SetupApiNative.cs
            ConfigurationManagerNative.cs
            User32Native.cs
            BluetoothNative.cs
            HidNative.cs
            NativeConstants.cs
            SafeDeviceInfoSetHandle.cs

        Providers/
            IMouseHardwareProvider.cs
            GenericMouseProvider.cs

        Converters/
        Commands/
        Resources/
        Themes/

    MouseTune.ElevatedHelper/
        Program.cs
        RenameCommand.cs
        RenameCommandValidator.cs

tests/
    MouseTune.Tests/
        PointerSpeedMappingTests.cs
        ProfileServiceTests.cs
        RenameCommandValidatorTests.cs
        DeviceDeduplicationTests.cs
```

## MVVM requirements

Use proper MVVM separation.

Do not place operating-system logic inside code-behind files.

Code-behind should only contain unavoidable window or UI integration.

Implement:

* `ObservableObject`
* `RelayCommand`
* `AsyncRelayCommand`

These may be implemented internally to avoid adding a large MVVM dependency.

All long-running actions should be asynchronous.

Expose user-facing operation states:

* Idle
* Scanning
* Applying
* Successful
* Failed

## Main screen behaviour

On startup:

1. Load settings and profiles.
2. Read current Windows pointer settings.
3. Scan for mouse devices.
4. Select the first connected mouse.
5. Display its saved alias and profile.
6. Show whether hardware DPI is supported.

The selected-device screen should contain:

```text
Device
[ BT 5.2 Mouse                         Connected ]

Device name
[ Logitech G304                              ]
[ Apply name ] [ Restore original ]

Sensitivity
Effective DPI
[ 400 ]---[ 800 ]---[ 1600 ]---[ 3200 ]---[ 6400 ]
                         1600

Windows pointer speed
14 / 20

[ ] Enhance pointer precision

[ Apply settings ] [ Reset ]

Profile
[ Everyday ▼ ]
[ Save profile ]
```

Use a live preview of the mapped Windows pointer-speed value, but do not apply changes until the user presses Apply unless the user enables Live Apply in Settings.

## Settings

Include:

* Launch at Windows startup
* Start minimised
* Minimise to tray
* Reapply saved device aliases
* Automatically apply default device profile
* Live sensitivity preview
* Include virtual mouse devices
* Application theme: System, Light or Dark
* Logging level
* Reset all settings

Do not enable startup or background behaviour by default.

## Tray behaviour

Provide an optional tray icon.

Tray menu:

* Open MouseTune
* Current device
* Current effective DPI
* Select profile
* Reapply settings
* Exit

When tray mode is disabled, closing the window should exit normally.

## Testing

Add unit tests for:

* DPI-to-Windows-speed mapping
* Windows-speed-to-effective-DPI mapping
* Clamping
* Profile persistence
* JSON recovery
* Device deduplication
* Elevated helper command validation
* Device alias validation

Device-name validation should:

* Trim whitespace
* Reject empty names
* Reject control characters
* Limit names to 64 characters
* Allow ordinary letters, numbers, spaces and punctuation

Native Windows operations should be behind interfaces so they can be mocked in tests.

## Documentation

Create:

```text
README.md
docs/
    architecture.md
    device-renaming.md
    effective-dpi.md
    hardware-dpi-limitations.md
    privacy.md
    troubleshooting.md
```

The README must include:

* Project purpose
* Feature list
* Technical limitations
* Build requirements
* Development commands
* Publishing commands
* Administrator permission explanation
* Screenshots placeholder
* Roadmap

Clearly document:

> MouseTune changes the device name stored or displayed by Windows where supported. It does not alter the Bluetooth name programmed into the mouse firmware.

Also document:

> Effective DPI is a user-friendly representation of Windows pointer sensitivity. It is not physical sensor DPI unless a supported hardware provider is active.

## Build commands

Ensure these work:

```powershell
dotnet restore
dotnet build MouseTune.sln
dotnet test MouseTune.sln
dotnet run --project src/MouseTune.App
```

Add publishing support:

```powershell
dotnet publish src/MouseTune.App `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true
```

Also provide an optional self-contained command.

## Installer

Add an installer folder with either:

* Inno Setup configuration, or
* WiX configuration

Prefer Inno Setup for simplicity.

Installer requirements:

* Per-user installation by default
* Optional desktop shortcut
* Optional startup entry
* Proper uninstall support
* No bundled advertising
* No telemetry
* No background service
* Clear version information

## Implementation sequence

Complete the project in these stages.

### Stage 1

* Create the solution and projects.
* Implement the minimalist WPF interface.
* Add MVVM infrastructure.
* Add settings and profile persistence.
* Add Windows pointer-speed reading and writing.
* Add effective DPI mapping.
* Add unit tests.

### Stage 2

* Add HID and Bluetooth mouse enumeration.
* Deduplicate interfaces belonging to the same physical mouse.
* Display device metadata.
* Handle refresh and disconnection.

### Stage 3

* Implement safe Windows-friendly-name changes.
* Add backup and restore.
* Add elevated helper only when necessary.
* Add clear error handling.
* Test behaviour after reconnect and restart.

### Stage 4

* Add tray support.
* Add startup behaviour.
* Add automatic profile and alias reapplication.
* Add diagnostics export.

### Stage 5

* Add installer.
* Complete documentation.
* Run all validation.
* Review the application for security, performance and accessibility.

## Coding expectations

Generate real implementation code, not pseudocode.

Do not leave core functionality as TODO comments.

Where Windows does not permit a property change, return an honest structured error rather than pretending it succeeded.

Use nullable reference types.

Enable warnings as errors where practical.

Use XML documentation for native interop methods and public services.

Keep methods focused and reasonably small.

Use dependency injection without adding a heavy framework. A simple internal service container is acceptable.

Keep the UI responsive and avoid unnecessary polling.

Do not repeatedly enumerate all Bluetooth devices. Refresh on demand and when Windows device-change notifications are received.

## Final validation

Before considering the project complete:

1. Build the entire solution.
2. Run all tests.
3. Resolve compiler warnings.
4. Confirm pointer-speed changes work.
5. Confirm reset restores the original pointer settings.
6. Confirm an unsupported mouse is labelled as effective DPI only.
7. Confirm device renaming failures are clearly reported.
8. Confirm settings survive application restart.
9. Confirm the app does not require administrator rights at startup.
10. Confirm no telemetry or network communication is present.

Begin by creating the complete folder structure and Stage 1 implementation. Continue through each stage without replacing working functionality with mock data. Maintain a `docs/development-log.md` file describing completed work, limitations and remaining validation requirements.
