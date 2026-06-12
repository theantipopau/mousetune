# Development Log

## 2026-06-11

Revised portable scope:

- MouseTune is now focused on inexpensive or generic Bluetooth mice without manufacturer software.
- The active workflow is alias plus effective DPI, saved in portable JSON.
- The app exits fully when the main window closes; no tray, service, installer, scheduled task, startup task, accounts, cloud sync, telemetry, vendor integrations, RGB, macros, firmware, polling-rate controls, or profiles.

Removed or de-scoped:

- Logitech/Razer provider placeholders and hardware DPI provider architecture.
- Separate elevated helper project from the solution.
- Installer script.
- Sidebar navigation, profile page, settings page, tray/startup concepts from the active UI.

Completed changes:

- Replaced the main window with a compact single-screen WPF workflow.
- Added `PortableSettingsService`, `SavedMouseConfiguration`, and `WindowsMouseSettingsSnapshot`.
- Added portable path resolution: executable folder first, `%LocalAppData%\MouseTunePortable\` fallback.
- Added atomic JSON writes with validation and one backup file.
- Added `DeviceIdentityService` for stable matching across interface-path changes.
- Extended mouse discovery with Container ID and serial-like identifier capture where available.
- Updated effective DPI anchors, including `3000 DPI -> Windows speed 18`.
- Added pointer settings read-back verification after apply/reset.
- Removed vendor-provider wiring from app composition.
- Added tests for portable persistence, corrupt JSON recovery, backup creation, device identity matching, deduplication, alias validation, DPI mapping, and pointer read-back verification.
- Added local diagnostics export with tests for report content and JSON creation.

Current technical limitations:

- Windows-friendly-name changes depend on Windows and driver permissions. MouseTune attempts the documented Configuration Manager property write and saves the alias regardless.
- The app does not currently launch an elevated same-exe rename mode. That should only be added if manual testing proves it is needed and safe.
- Device discovery is based on Windows mouse interfaces and metadata. Real generic Bluetooth devices should be tested to confirm Container ID and Bluetooth address availability.
- The project path remains `src/MouseTune.App` in this pass, although the published executable is named `MouseTune.exe`.

Validation completed in this pass:

- `dotnet build MouseTune.sln`
- `dotnet test MouseTune.sln`

## 2026-06-12

Crash fix and UI polish:

- Fixed mouse discovery crash caused by importing `SetupDiGetDeviceProperty` without the exported Unicode entry point name `SetupDiGetDevicePropertyW`.
- Added startup, dispatcher, AppDomain, and unobserved task exception logging so future crashes write to `MouseTune.log` where possible.
- Broadened startup/refresh exception handling so device-enumeration failures surface as app status instead of terminating the process.
- Refreshed the compact UI with a softer blue/teal palette, Segoe UI Variable font usage, rounded button/input templates, status pill styling, and clearer sensitivity summary treatment.
- Added bundled logo/icon assets to the WPF app and updated the README with branding, portable usage, validation, and roadmap details.
- Added parent device instance ID capture during mouse discovery to improve saved-device matching after Bluetooth reconnects.
- Added automatic debounced refresh when Windows broadcasts device-node changes.
- Added a single-instance guard for the portable executable.
- Added persisted System/Light/Dark theme selection.
- Added a visible reapply Windows name action for saved aliases that differ from the currently reported Windows name.
- Reworked the main window toward a compact utility layout with clearer device, naming, sensitivity, recovery, and status/action areas.
- Added transparent logo/icon PNG variants, a clipboard diagnostics summary, and a portable release helper script that creates a `dist` exe, zip, and checksum.
- Published a fresh self-contained single-file `MouseTune.exe`.

Validation completed in this pass:

- `dotnet build MouseTune.sln`
- `dotnet test MouseTune.sln`
- `dotnet publish src\MouseTune.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`

Manual validation still required:

- Run `MouseTune.exe` from a normal folder with no installation.
- Confirm a generic Bluetooth mouse is detected and duplicate HID interfaces are grouped.
- Save alias plus `3000` effective DPI and verify Windows reports pointer speed `18 / 20`.
- Close MouseTune and confirm no process remains.
- Restart Windows and verify Windows pointer speed persists.
- Reopen MouseTune and verify the saved alias and preferred effective DPI are displayed.
- Test Windows-friendly-name behavior on the target mouse and record whether Windows permits the change.
