using System.Reflection;
using System.Text.Json;
using MouseTune.Models;

namespace MouseTune.Services;

public sealed class DiagnosticsService
{
    private const int RecentLogLineLimit = 50;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppPaths _paths;
    private readonly AppLogger _logger;

    public DiagnosticsService(AppPaths paths, AppLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<string> ExportAsync(
        PortableSettings settings,
        PointerSettings currentWindowsSettings,
        IEnumerable<MouseDevice> detectedDevices,
        CancellationToken cancellationToken)
    {
        _paths.EnsureCreated();
        var report = CreateReport(settings, currentWindowsSettings, detectedDevices, ReadRecentLogLines());
        var path = CreateExportPath();
        var json = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
        _logger.Log("DiagnosticsExported", $"Exported diagnostics to {path}.");
        return path;
    }

    public DiagnosticsReport CreateReport(
        PortableSettings settings,
        PointerSettings currentWindowsSettings,
        IEnumerable<MouseDevice> detectedDevices,
        IReadOnlyList<string>? recentLogLines = null)
    {
        return new DiagnosticsReport
        {
            ApplicationVersion = settings.ApplicationVersion.Length > 0
                ? settings.ApplicationVersion
                : GetVersion(),
            WindowsVersion = Environment.OSVersion.VersionString,
            PortableStorage = new PortableStorageDiagnostics
            {
                SettingsFileExists = File.Exists(_paths.PortableSettingsPath),
                LogFileExists = File.Exists(_paths.LogPath),
                StorageFolder = Path.GetFileName(_paths.BasePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            },
            CurrentWindowsSettings = WindowsMouseSettingsSnapshot.FromPointerSettings(currentWindowsSettings),
            OriginalWindowsSettings = settings.OriginalWindowsSettings,
            SavedDevices = settings.SavedDevices.Select(SavedDeviceDiagnostics.FromSavedConfiguration).ToList(),
            DetectedDevices = detectedDevices.Select(DeviceDiagnostics.FromMouseDevice).ToList(),
            RecentLogLines = recentLogLines?.ToList() ?? new List<string>()
        };
    }

    private IReadOnlyList<string> ReadRecentLogLines()
    {
        try
        {
            if (!File.Exists(_paths.LogPath))
            {
                return Array.Empty<string>();
            }

            return File.ReadLines(_paths.LogPath)
                .TakeLast(RecentLogLineLimit)
                .ToList();
        }
        catch (IOException)
        {
            return Array.Empty<string>();
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    private string CreateExportPath()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(_paths.BasePath, $"MouseTune.diagnostics-{timestamp}.json");
    }

    private static string GetVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0";
}

public sealed class DiagnosticsReport
{
    public int SchemaVersion { get; init; } = 1;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string ApplicationVersion { get; init; } = string.Empty;
    public string WindowsVersion { get; init; } = string.Empty;
    public PortableStorageDiagnostics PortableStorage { get; init; } = new();
    public WindowsMouseSettingsSnapshot CurrentWindowsSettings { get; init; } = new();
    public WindowsMouseSettingsSnapshot? OriginalWindowsSettings { get; init; }
    public List<SavedDeviceDiagnostics> SavedDevices { get; init; } = new();
    public List<DeviceDiagnostics> DetectedDevices { get; init; } = new();
    public List<string> RecentLogLines { get; init; } = new();
}

public sealed class PortableStorageDiagnostics
{
    public string StorageFolder { get; init; } = string.Empty;
    public bool SettingsFileExists { get; init; }
    public bool LogFileExists { get; init; }
}

public sealed class SavedDeviceDiagnostics
{
    public string StableId { get; init; } = string.Empty;
    public string? ContainerId { get; init; }
    public string? BluetoothAddress { get; init; }
    public string? ParentDeviceInstanceId { get; init; }
    public string? VendorId { get; init; }
    public string? ProductId { get; init; }
    public string OriginalDetectedName { get; init; } = string.Empty;
    public string CustomAlias { get; init; } = string.Empty;
    public int EffectiveDpi { get; init; }
    public int WindowsPointerSpeed { get; init; }
    public bool EnhancePointerPrecision { get; init; }
    public DateTimeOffset LastModifiedUtc { get; init; }

    public static SavedDeviceDiagnostics FromSavedConfiguration(SavedMouseConfiguration configuration) => new()
    {
        StableId = configuration.StableId,
        ContainerId = configuration.ContainerId,
        BluetoothAddress = configuration.BluetoothAddress,
        ParentDeviceInstanceId = configuration.ParentDeviceInstanceId,
        VendorId = configuration.VendorId,
        ProductId = configuration.ProductId,
        OriginalDetectedName = configuration.OriginalDetectedName,
        CustomAlias = configuration.CustomAlias,
        EffectiveDpi = configuration.EffectiveDpi,
        WindowsPointerSpeed = configuration.WindowsPointerSpeed,
        EnhancePointerPrecision = configuration.EnhancePointerPrecision,
        LastModifiedUtc = configuration.LastModifiedUtc
    };
}

public sealed class DeviceDiagnostics
{
    public string DeviceInstanceId { get; init; } = string.Empty;
    public string? StableId { get; init; }
    public string? ContainerId { get; init; }
    public string? ParentDeviceInstanceId { get; init; }
    public string? SerialNumber { get; init; }
    public string CurrentName { get; init; } = string.Empty;
    public string OriginalName { get; init; } = string.Empty;
    public string? Manufacturer { get; init; }
    public string? VendorId { get; init; }
    public string? ProductId { get; init; }
    public string? BluetoothAddress { get; init; }
    public MouseConnectionType ConnectionType { get; init; }
    public bool IsConnected { get; init; }
    public bool SupportsHardwareDpi { get; init; }

    public static DeviceDiagnostics FromMouseDevice(MouseDevice device) => new()
    {
        DeviceInstanceId = device.DeviceInstanceId,
        StableId = device.StableId,
        ContainerId = device.ContainerId,
        ParentDeviceInstanceId = device.ParentDeviceInstanceId,
        SerialNumber = device.SerialNumber,
        CurrentName = device.CurrentName,
        OriginalName = device.OriginalName,
        Manufacturer = device.Manufacturer,
        VendorId = device.VendorId,
        ProductId = device.ProductId,
        BluetoothAddress = device.BluetoothAddress,
        ConnectionType = device.ConnectionType,
        IsConnected = device.IsConnected,
        SupportsHardwareDpi = device.SupportsHardwareDpi
    };
}
