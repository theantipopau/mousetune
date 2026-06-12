# Architecture

MouseTune is a small WPF app using MVVM and service composition through `AppServices`.

Active portable workflow:

- `MainWindow.xaml` is the single compact UI.
- `MainViewModel` coordinates device refresh, alias editing, effective DPI preview, save/apply, and reset actions.
- `MainWindow` listens for Windows device-change notifications and asks the view model to refresh after a short debounce.
- `MouseDeviceDiscoveryService` enumerates likely mouse devices through Windows mouse device interfaces and SetupAPI metadata.
- `DeviceIdentityService` builds stable matching keys from Container ID, Bluetooth address, parent/device instance ID, VID/PID, and serial-like values.
- `DeviceRenameService` saves the local alias and attempts a Windows friendly-name property update where permitted.
- `PointerSettingsService` reads, writes, and verifies Windows pointer settings through `SystemParametersInfo`.
- `PortableSettingsService` stores `MouseTune.settings.json` beside the executable when possible.
- `JsonFileStore` performs atomic writes, validation, backup, and corrupt-file recovery.
- `ThemeService` applies System, Light, or Dark theme dictionaries and persists the choice in portable settings.
- App startup uses a local mutex so only one MouseTune instance runs at a time.

Native interop remains isolated under `Native/` for SetupAPI, Configuration Manager, User32, HID, and Bluetooth helpers.

The old broad-scope profile/settings views, vendor provider placeholders, installer script, and separate elevated helper project have been removed from the solution/source.
