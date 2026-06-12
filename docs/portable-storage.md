# Portable Storage

MouseTune stores normal app configuration outside the registry.

Preferred portable layout:

```text
MouseTune.exe
MouseTune.settings.json
MouseTune.settings.json.bak
MouseTune.log
```

If the executable folder is not writable, MouseTune falls back to:

```text
%LocalAppData%\MouseTunePortable\
```

The settings file contains:

- schema version;
- application version;
- original Windows pointer settings captured before MouseTune changes them;
- saved mouse identities;
- original detected mouse names;
- custom aliases;
- effective DPI values;
- Windows pointer-speed values;
- Enhance pointer precision state;
- last modified date.

Writes are atomic: MouseTune writes a temporary JSON file, validates it by deserializing it, keeps one `.bak` copy, then replaces the active settings file. Missing or malformed settings files recover to a clean default.
