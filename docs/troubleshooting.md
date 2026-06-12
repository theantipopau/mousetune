# Troubleshooting

## No Mouse Devices Appear

Use the Refresh button. MouseTune currently focuses on generic Bluetooth-style mice and filters obvious virtual devices.

## Name Does Not Change Everywhere

Windows may reject protected Bluetooth device friendly-name changes or cache device metadata. MouseTune still stores the preferred local alias and displays it inside the app. Disconnect/reconnect the mouse or restart Windows if you want to check whether Windows refreshed the system name.

## Saved Settings Are Not Active

MouseTune does not auto-apply settings on launch. If Windows pointer settings changed since MouseTune last ran, press Save and apply to apply the saved effective DPI again.

## Sensitivity Feels Wrong

Use Restore settings from before MouseTune or Restore standard Windows defaults, then apply a preset such as 800 or 1600 effective DPI. Effective DPI changes Windows pointer speed, not physical sensor DPI.

## Build Restore Fails

Install the .NET 8 SDK or allow `dotnet restore` to download the .NET 8 reference packs and test packages from NuGet.

## Export Diagnostics

Use Export diagnostics to create a local `MouseTune.diagnostics-*.json` file beside the settings file. It includes MouseTune settings, detected mouse metadata, current Windows pointer settings, and recent MouseTune log lines.
