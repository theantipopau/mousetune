namespace MouseTune.Models;

public sealed class PortableSettings
{
    public int SchemaVersion { get; set; } = 1;
    public string ApplicationVersion { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public AppTheme Theme { get; set; } = AppTheme.System;
    public WindowsMouseSettingsSnapshot? OriginalWindowsSettings { get; set; }
    public List<SavedMouseConfiguration> SavedDevices { get; set; } = new();
}
