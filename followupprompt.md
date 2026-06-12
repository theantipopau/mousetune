Refine the existing MouseTune project scope based on the following requirements. Do not restart the project or replace working functionality. Update the current architecture, interface, documentation and implementation plan to reflect this narrower and more practical purpose.

## Revised product scope

MouseTune is specifically for inexpensive or generic Bluetooth mice that do not have their own manufacturer software.

Examples include devices reported by Windows as:

* BT 5.0 Mouse
* BT 5.1 Mouse
* BT 5.2 Mouse
* Bluetooth Mouse
* BLE Mouse
* Wireless Mouse
* Generic Mouse

It is not intended to replace or integrate with:

* Logitech G HUB
* Razer Synapse
* Corsair iCUE
* SteelSeries GG
* Other manufacturer-specific mouse software

Remove vendor-specific provider plans, placeholder Logitech or Razer integrations, gaming functionality, hardware-monitoring concepts and unnecessary plugin architecture.

The app should remain focused on two practical functions:

1. Assigning a local custom name to a generic Bluetooth mouse.
2. Providing a numeric DPI-style control that changes the underlying persistent Windows pointer settings.

## Portable application requirement

Convert the project into a portable, single-purpose Windows application.

The final distribution should be a single executable:

```text
MouseTune.exe
```

The user should be able to:

1. Place the executable in any folder.
2. Launch it without installing it.
3. Detect a connected generic Bluetooth mouse.
4. Change its local name.
5. Set an effective DPI value.
6. Press Save and Apply.
7. Close the application completely.
8. Restart Windows without losing the applied Windows pointer settings.
9. Open MouseTune again later.
10. See the previously saved device alias and effective DPI.
11. Modify or restore the settings.

Do not require:

* An installer
* A Windows service
* A permanently running process
* A tray application
* A scheduled task
* A startup application
* A custom driver
* Internet access
* Telemetry
* User accounts
* Cloud storage

The application must exit fully when its window is closed.

## Target workflow

The intended workflow is:

```text
Detected device: BT 5.2 Mouse

Custom name:
Office Mouse

Effective DPI:
3000

Windows pointer speed:
18 / 20

[ Save and apply ]
```

After saving:

```text
Saved successfully

Device: Office Mouse
Effective DPI: 3000
Windows pointer speed: 18 / 20

The Windows pointer settings will remain active after MouseTune closes.
```

After a Windows restart, the user can reopen MouseTune and see:

```text
Office Mouse
Connected

Saved effective DPI: 3000
Current Windows pointer speed: 18 / 20
Settings active
```

## Important technical behaviour

The application must clearly distinguish between saved configuration and actual Windows state.

For generic Bluetooth mice:

* The app does not change the physical sensor DPI.
* The numeric DPI value is an intuitive representation of Windows sensitivity.
* The resulting Windows pointer speed is generally system-wide.
* The app remembers the preferred DPI value for the selected mouse.
* The actual Windows pointer speed may also affect other connected standard mice.

Use this wording in the interface:

> Effective DPI adjusts Windows pointer sensitivity. It does not change the physical sensor inside the mouse.

Also display:

> Windows applies this sensitivity setting system-wide. MouseTune remembers the preferred value for this mouse.

Do not imply that the physical mouse sensor has been changed.

## Persistent Windows sensitivity

Continue using the Windows `SystemParametersInfo` API.

Support:

* `SPI_GETMOUSESPEED`
* `SPI_SETMOUSESPEED`
* `SPI_GETMOUSE`
* `SPI_SETMOUSE`

When applying settings, use:

* `SPIF_UPDATEINIFILE`
* `SPIF_SENDCHANGE`

This is required so the setting persists after:

* MouseTune closes
* Sign-out
* Windows restart

After applying:

1. Read the value back from Windows.
2. Verify that the requested pointer-speed value is active.
3. Only report success after verification.
4. Save the configuration after successful application.
5. Handle partial success if the rename fails but sensitivity succeeds.

Do not automatically reapply settings when the application launches.

On launch, compare:

* Saved pointer-speed value
* Current Windows pointer-speed value

Show one of:

```text
Settings active
```

```text
Windows pointer settings have changed since MouseTune last ran
```

```text
Saved settings are not currently applied
```

Provide an Apply button when the values differ.

## Effective DPI mapping

Keep the DPI-style input but make it clear that it maps to Windows pointer speed.

Supported range:

```text
200–6400
```

Include presets:

```text
400
800
1200
1600
2400
3000
3200
4800
6400
```

Use these anchor points:

```text
200 DPI  -> Windows speed 1
400 DPI  -> Windows speed 5
800 DPI  -> Windows speed 10
1200 DPI -> Windows speed 12
1600 DPI -> Windows speed 14
2400 DPI -> Windows speed 16
3000 DPI -> Windows speed 18
3200 DPI -> Windows speed 18
4800 DPI -> Windows speed 19
6400 DPI -> Windows speed 20
```

Use linear interpolation between anchor points.

Clamp values to:

```text
Effective DPI: 200–6400
Windows pointer speed: 1–20
```

The user must be able to:

* Enter DPI numerically.
* Use a slider.
* Select common presets.
* See the mapped Windows pointer speed before applying.
* Enable or disable Enhance pointer precision.
* Reset to the previous Windows settings.

Prefer the saved DPI value when reopening the application, because several DPI values may map to the same Windows pointer-speed value.

For example, if the user saved 3000 DPI and Windows reports speed 18, continue displaying 3000 rather than converting it to 3200.

## Device naming behaviour

The custom device name should work at two levels.

### Level 1: MouseTune alias

This must always work.

Save a custom alias such as:

```text
Office Mouse
Travel Mouse
Classroom Mouse
```

Associate it with a stable device identity.

When the app is reopened, detect the same mouse and display the saved alias even if Windows still reports the original name.

Example:

```text
Office Mouse
Reported by Windows as: BT 5.2 Mouse
```

### Level 2: Windows-friendly name

Attempt to update the local Windows device-friendly name using documented Windows APIs where possible.

Continue investigating:

* `SetupDiGetDeviceProperty`
* `SetupDiSetDeviceProperty`
* `CM_Get_DevNode_Property`
* `CM_Set_DevNode_Property`

Do not blindly edit arbitrary registry bytes.

Clearly distinguish the result:

```text
Windows device name changed successfully
```

or:

```text
Windows did not permit this Bluetooth device name to be changed. The alias has still been saved in MouseTune.
```

Use this wording:

> MouseTune changes the name displayed by this Windows computer where Windows permits it. It does not rewrite the Bluetooth name stored in the mouse firmware.

If Windows later restores the original reported name, retain the MouseTune alias and show:

```text
Saved alias: Office Mouse
Windows name: BT 5.2 Mouse

[ Reapply Windows name ]
```

Do not require the app to stay open to maintain the name.

## Stable device recognition

Ensure the application recognises the same mouse after:

* MouseTune restart
* Windows restart
* Bluetooth disconnect and reconnect
* HID interface-path changes

Do not rely only on the interface path.

Build the stable device identity from the best available combination of:

* Windows Container ID
* Bluetooth address
* Parent Bluetooth device ID
* Device instance ID
* VID
* PID
* Serial number where available

Prefer Container ID for grouping duplicate HID interfaces belonging to the same physical mouse.

Store enough fallback identifiers to match the device if one identifier changes.

The application should not lose the saved alias merely because Windows regenerates a HID interface path.

## Portable settings storage

Remove installer-based or profile-based persistence.

Store settings beside the executable where possible:

```text
MouseTune.exe
MouseTune.settings.json
MouseTune.log
```

If the executable folder is not writable, fall back to:

```text
%LocalAppData%\MouseTunePortable\
```

Store:

* Schema version
* Application version
* Original Windows mouse settings
* Saved mouse identities
* Original detected mouse names
* Custom aliases
* Effective DPI values
* Windows pointer-speed values
* Enhance pointer precision state
* Last modified date

Use atomic writes:

1. Write a temporary JSON file.
2. Validate it.
3. Replace the current settings file.
4. Keep one backup copy.

Recover gracefully from a missing or malformed settings file.

Do not use the registry for normal app configuration.

## Original and reset settings

On the first successful run, capture the existing Windows mouse settings before changing them.

Preserve:

* Pointer speed
* Mouse threshold 1
* Mouse threshold 2
* Acceleration value

Provide two distinct reset options:

```text
Restore settings from before MouseTune
```

and:

```text
Restore standard Windows defaults
```

Require confirmation before restoring.

After restoring, read the settings back from Windows and verify them.

## Simplified interface

Remove the large sidebar and multi-page structure from the initial design.

Use one compact main window.

Suggested size:

```text
680 × 520
```

Suggested layout:

```text
MouseTune

Generic Bluetooth mouse
┌─────────────────────────────────────────────┐
│ Office Mouse                     Connected  │
│ Bluetooth LE                                │
│ Originally detected as: BT 5.2 Mouse        │
└─────────────────────────────────────────────┘

Device name
[ Office Mouse                              ]
[ Apply name ] [ Restore detected name ]

Sensitivity
Effective DPI
[ 3000                                     ]

400    800    1600    3000    4800    6400
──────────────●──────────────────────────────

Windows pointer speed: 18 / 20

[ ] Enhance pointer precision

[ Save and apply ]

Changes remain active after MouseTune is closed.

[ Restore previous settings ]       [ Refresh ]
```

If multiple generic Bluetooth mice are detected, add a compact dropdown at the top.

Do not add separate pages for:

* Profiles
* Vendor providers
* Gaming features
* RGB
* Macros
* Polling rate
* Firmware
* Accounts
* Cloud synchronisation
* Hardware monitoring

## Simplified architecture

Refactor the project structure toward:

```text
MouseTune.sln

src/
    MouseTune/
        MouseTune.csproj
        App.xaml
        App.xaml.cs
        MainWindow.xaml
        MainWindow.xaml.cs

        Models/
            MouseDevice.cs
            PortableSettings.cs
            SavedMouseConfiguration.cs
            WindowsMouseSettingsSnapshot.cs
            OperationResult.cs

        ViewModels/
            MainViewModel.cs
            MouseDeviceViewModel.cs

        Services/
            MouseDeviceDiscoveryService.cs
            DeviceIdentityService.cs
            DeviceRenameService.cs
            PointerSettingsService.cs
            EffectiveDpiMapper.cs
            PortableSettingsService.cs
            DiagnosticsService.cs

        Native/
            SetupApiNative.cs
            ConfigurationManagerNative.cs
            User32Native.cs
            HidNative.cs
            NativeConstants.cs
            SafeDeviceInfoSetHandle.cs

        Commands/
            RelayCommand.cs
            AsyncRelayCommand.cs

        Infrastructure/
            ObservableObject.cs
            AppPaths.cs
            SingleInstanceManager.cs

        Resources/
            Themes/
                Light.xaml
                Dark.xaml

tests/
    MouseTune.Tests/
        EffectiveDpiMapperTests.cs
        PortableSettingsServiceTests.cs
        DeviceIdentityServiceTests.cs
        DeviceNameValidationTests.cs
        PointerSettingsVerificationTests.cs

docs/
    technical-limitations.md
    device-renaming.md
    effective-dpi.md
    portable-storage.md
    development-log.md
```

Remove the separate elevated-helper project unless testing proves it is required.

To preserve a single-executable release, use the same executable in a narrowly scoped elevated mode if elevation is needed:

```text
MouseTune.exe --elevated-rename "<request-file>"
```

The elevated mode must:

* Accept only a rename request.
* Validate the target device identity.
* Validate the alias.
* Reject arbitrary commands.
* Use a short-lived request file or named pipe.
* Return a structured result.
* Exit immediately.
* Delete temporary files.

The main application must not always run as administrator.

## Publishing

Configure the project for a self-contained, single-file x64 release.

The expected output should be:

```text
MouseTune.exe
```

Use a command similar to:

```powershell
dotnet publish src/MouseTune `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Confirm whether WPF resources and native libraries behave correctly in single-file mode.

Do not enable aggressive trimming unless the complete app is tested and remains functional.

Add a framework-dependent single-file publishing option as a smaller alternative, but make the self-contained build the default portable release.

## Validation requirements

Update the implementation and tests to verify:

1. The app runs without installation.
2. The final distribution is a single executable.
3. The app exits fully when closed.
4. No tray process remains.
5. No service is installed.
6. The current generic Bluetooth mouse is detected.
7. Duplicate HID interfaces are grouped.
8. A custom alias is saved.
9. The alias is shown again after reopening the app.
10. The saved device is matched after a Windows restart.
11. A value of 3000 effective DPI maps to Windows speed 18.
12. Windows pointer-speed changes persist after the app closes.
13. Windows pointer-speed changes persist after a restart.
14. The app verifies Windows settings after applying.
15. The original settings can be restored.
16. Rename failure does not prevent sensitivity from being saved.
17. The UI never describes effective DPI as physical sensor DPI.
18. The app makes no network connections.
19. No vendor-specific software integration remains.
20. Settings JSON writes are atomic and recoverable.

Update `docs/development-log.md` with:

* The revised portable scope
* Removed functionality
* Completed changes
* Current technical limitations
* Actual behaviour of Windows-friendly-name changes
* Manual restart-testing results

Continue from the existing implementation. Preserve working Stage 1 functionality where it still fits, refactor rather than restart, and complete the simplified portable version.
