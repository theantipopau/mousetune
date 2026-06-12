using System.Reflection;
using MouseTune.Models;

namespace MouseTune.Services;

public sealed class PortableSettingsService
{
    private readonly AppPaths _paths;
    private readonly JsonFileStore _store;
    private readonly AppLogger _logger;

    public PortableSettingsService(AppPaths paths, JsonFileStore store, AppLogger logger)
    {
        _paths = paths;
        _store = store;
        _logger = logger;
    }

    public async Task<PortableSettings> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await _store.ReadAsync(_paths.PortableSettingsPath, CreateDefault(), cancellationToken).ConfigureAwait(false);
        settings.ApplicationVersion = GetVersion();
        settings.SavedDevices ??= new List<SavedMouseConfiguration>();
        return settings;
    }

    public async Task SaveAsync(PortableSettings settings, CancellationToken cancellationToken)
    {
        settings.SchemaVersion = 1;
        settings.ApplicationVersion = GetVersion();
        settings.LastModifiedUtc = DateTimeOffset.UtcNow;
        await _store.WriteAsync(_paths.PortableSettingsPath, settings, cancellationToken).ConfigureAwait(false);
        _logger.Log("PortableSettingsSaved", $"Saved portable settings to {_paths.PortableSettingsPath}.");
    }

    public async Task CaptureOriginalWindowsSettingsIfNeededAsync(
        PortableSettings settings,
        PointerSettings currentWindowsSettings,
        CancellationToken cancellationToken)
    {
        if (settings.OriginalWindowsSettings is not null)
        {
            return;
        }

        settings.OriginalWindowsSettings = WindowsMouseSettingsSnapshot.FromPointerSettings(currentWindowsSettings);
        await SaveAsync(settings, cancellationToken).ConfigureAwait(false);
    }

    private static PortableSettings CreateDefault() => new()
    {
        SchemaVersion = 1,
        ApplicationVersion = GetVersion(),
        LastModifiedUtc = DateTimeOffset.UtcNow,
        SavedDevices = new List<SavedMouseConfiguration>()
    };

    private static string GetVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0";
}
