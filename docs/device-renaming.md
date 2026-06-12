# Device Renaming

MouseTune has two naming levels.

Level 1 is the MouseTune alias. This always works when settings can be saved. The alias is stored in `MouseTune.settings.json` and is matched to the same mouse later using the best available identity data: Container ID, Bluetooth address, parent/device instance ID, VID/PID, and serial-like identifiers.

Level 2 is the Windows friendly name. MouseTune attempts to set the local Windows friendly-name property through Configuration Manager. Windows may reject the write for Bluetooth devices or driver-owned properties.

User-facing result text must distinguish these outcomes:

```text
Windows device name changed successfully
```

```text
Windows did not permit this Bluetooth device name to be changed. The alias has still been saved in MouseTune.
```

MouseTune changes the name displayed by this Windows computer where Windows permits it. It does not rewrite the Bluetooth name stored in the mouse firmware.

Alias validation:

- trims whitespace;
- rejects empty names;
- rejects control characters;
- limits names to 64 characters;
- allows ordinary letters, numbers, spaces, and punctuation.

MouseTune does not blindly edit registry bytes, install drivers, flash firmware, or require a background process to maintain an alias.
