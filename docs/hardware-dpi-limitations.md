# Hardware DPI Limitations

MouseTune does not change physical sensor DPI for generic Bluetooth mice.

Most inexpensive Bluetooth mice do not expose a documented Windows API for changing sensor DPI. MouseTune therefore provides an effective DPI value that maps to persistent Windows pointer sensitivity.

MouseTune must not:

- send undocumented HID feature reports;
- write random Bluetooth GATT values;
- claim sensor DPI changed when only Windows pointer speed changed;
- install drivers or firmware.

The UI wording should remain explicit:

```text
Effective DPI adjusts Windows pointer sensitivity. It does not change the physical sensor inside the mouse.
```
