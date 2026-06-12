# Technical Limitations

- Effective DPI is Windows pointer sensitivity, not physical sensor DPI.
- Windows pointer speed is generally system-wide and can affect other standard mice.
- Windows-friendly-name changes may be rejected for Bluetooth devices; MouseTune still saves its own alias.
- Stable device recognition depends on metadata Windows exposes for the device. Container ID is preferred, with Bluetooth address and instance metadata as fallbacks.
- MouseTune does not automatically reapply settings on launch. It compares saved settings to current Windows state and lets the user apply when they differ.
- Restart persistence of pointer settings must be manually verified on a physical Windows desktop because it depends on Windows honoring `SPIF_UPDATEINIFILE`.
