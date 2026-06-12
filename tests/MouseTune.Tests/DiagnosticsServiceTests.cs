using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseTune.Models;
using MouseTune.Services;

namespace MouseTune.Tests;

[TestClass]
public sealed class DiagnosticsServiceTests
{
    [TestMethod]
    public void ReportIncludesOnlyMouseTuneDeviceAndSettingsData()
    {
        var paths = CreatePaths();
        var service = new DiagnosticsService(paths, new AppLogger(paths));
        var settings = CreateSettings();
        var device = CreateDevice();

        var report = service.CreateReport(
            settings,
            new PointerSettings(18, true, 6, 10, 1),
            new[] { device },
            new[] { "2026-06-12T00:00:00Z\tTest\tMessage" });

        Assert.AreEqual(1, report.SchemaVersion);
        Assert.AreEqual("0.2.0", report.ApplicationVersion);
        Assert.AreEqual(18, report.CurrentWindowsSettings.WindowsPointerSpeed);
        Assert.AreEqual(1, report.SavedDevices.Count);
        Assert.AreEqual("Office Mouse", report.SavedDevices[0].CustomAlias);
        Assert.AreEqual(1, report.DetectedDevices.Count);
        Assert.AreEqual("BT 5.2 Mouse", report.DetectedDevices[0].OriginalName);
        Assert.IsFalse(Path.IsPathFullyQualified(report.PortableStorage.StorageFolder));
        Assert.IsFalse(string.IsNullOrWhiteSpace(report.PortableStorage.StorageFolder));
    }

    [TestMethod]
    public async Task ExportWritesDiagnosticsJsonBesidePortableSettings()
    {
        var paths = CreatePaths();
        var service = new DiagnosticsService(paths, new AppLogger(paths));

        var path = await service.ExportAsync(
            CreateSettings(),
            new PointerSettings(10, false, 6, 10, 0),
            new[] { CreateDevice() },
            CancellationToken.None);

        Assert.IsTrue(File.Exists(path));
        Assert.IsTrue(Path.GetFileName(path).StartsWith("MouseTune.diagnostics-", StringComparison.Ordinal));

        var json = await File.ReadAllTextAsync(path);
        var report = JsonSerializer.Deserialize<DiagnosticsReport>(json);

        Assert.IsNotNull(report);
        Assert.AreEqual(1, report.DetectedDevices.Count);
        Assert.AreEqual("Office Mouse", report.SavedDevices[0].CustomAlias);
    }

    private static PortableSettings CreateSettings() => new()
    {
        ApplicationVersion = "0.2.0",
        OriginalWindowsSettings = new WindowsMouseSettingsSnapshot
        {
            WindowsPointerSpeed = 10,
            Threshold1 = 6,
            Threshold2 = 10,
            Acceleration = 0
        },
        SavedDevices =
        {
            new SavedMouseConfiguration
            {
                StableId = "container:abc",
                ContainerId = "abc",
                BluetoothAddress = "001122334455",
                CustomAlias = "Office Mouse",
                OriginalDetectedName = "BT 5.2 Mouse",
                EffectiveDpi = 3000,
                WindowsPointerSpeed = 18,
                EnhancePointerPrecision = true
            }
        }
    };

    private static MouseDevice CreateDevice() => new()
    {
        DeviceInstanceId = @"HID\VID_1234&PID_ABCD\7&ABC",
        StableId = "container:abc",
        ContainerId = "abc",
        CurrentName = "Office Mouse",
        OriginalName = "BT 5.2 Mouse",
        Manufacturer = "Generic",
        VendorId = "1234",
        ProductId = "ABCD",
        BluetoothAddress = "001122334455",
        ConnectionType = MouseConnectionType.BluetoothLe,
        IsConnected = true
    };

    private static AppPaths CreatePaths()
    {
        var path = Path.Combine(Path.GetTempPath(), "MouseTuneDiagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new AppPaths(path);
    }
}
