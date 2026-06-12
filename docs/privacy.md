# Privacy

MouseTune does not include telemetry, accounts, analytics, cloud storage, or network communication in the application code.

Local files are stored beside `MouseTune.exe` when that folder is writable:

- `MouseTune.settings.json`
- `MouseTune.settings.json.bak`
- `MouseTune.log`
- `MouseTune.diagnostics-*.json`

If the executable folder is not writable, MouseTune falls back to `%LocalAppData%\MouseTunePortable\`.

MouseTune stores only mouse identity metadata needed to recognize the same device later, the local alias, effective DPI, Windows pointer-speed values, Enhance pointer precision state, and the original Windows mouse settings snapshot used for restore.

It does not store user documents, keystrokes, mouse movement, account details, secrets, or unrelated Bluetooth device details.

Diagnostics exports contain only MouseTune version/storage status, Windows mouse setting values, detected mouse metadata, saved MouseTune aliases/settings, and recent MouseTune log lines.
