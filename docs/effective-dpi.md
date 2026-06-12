# Effective DPI

MouseTune uses effective DPI as a friendly number for Windows pointer sensitivity. It is not physical sensor DPI for generic Bluetooth mice.

The current mapping anchors are:

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

Values between anchors use linear interpolation. Effective DPI is clamped to `200-6400`; Windows pointer speed is clamped to `1-20`.

When applying settings, MouseTune writes through `SystemParametersInfo` with `SPIF_UPDATEINIFILE` and `SPIF_SENDCHANGE`, then reads the Windows value back. Success is only reported when the read-back values match the requested pointer speed and acceleration settings.

MouseTune prefers the saved effective DPI when reopening. For example, both `3000` and `3200` map to Windows speed `18`, so a saved value of `3000` stays visible as `3000`.
