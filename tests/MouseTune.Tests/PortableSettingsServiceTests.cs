using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseTune.Models;
using MouseTune.Services;

namespace MouseTune.Tests;

[TestClass]
public sealed class PortableSettingsServiceTests
{
    [TestMethod]
    public async Task PortableSettingsPersistBesideConfiguredBasePath()
    {
        var paths = CreatePaths();
        var service = new PortableSettingsService(paths, new JsonFileStore(), new AppLogger(paths));
        var settings = new PortableSettings
        {
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
                    StableId = "bt:001122334455",
                    CustomAlias = "Office Mouse",
                    OriginalDetectedName = "BT 5.2 Mouse",
                    EffectiveDpi = 3000,
                    WindowsPointerSpeed = 18
                }
            }
        };

        await service.SaveAsync(settings, CancellationToken.None);
        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.IsTrue(File.Exists(paths.PortableSettingsPath));
        Assert.AreEqual(1, loaded.SavedDevices.Count);
        Assert.AreEqual("Office Mouse", loaded.SavedDevices[0].CustomAlias);
        Assert.AreEqual(3000, loaded.SavedDevices[0].EffectiveDpi);
    }

    [TestMethod]
    public async Task CorruptPortableJsonIsRecoveredWithFallback()
    {
        var paths = CreatePaths();
        paths.EnsureCreated();
        await File.WriteAllTextAsync(paths.PortableSettingsPath, "{broken json");
        var service = new PortableSettingsService(paths, new JsonFileStore(), new AppLogger(paths));

        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.AreEqual(0, loaded.SavedDevices.Count);
        Assert.AreEqual(1, Directory.GetFiles(paths.BasePath, "MouseTune.settings.json.corrupt-*").Length);
    }

    [TestMethod]
    public async Task AtomicWriteKeepsBackup()
    {
        var paths = CreatePaths();
        var service = new PortableSettingsService(paths, new JsonFileStore(), new AppLogger(paths));

        await service.SaveAsync(new PortableSettings(), CancellationToken.None);
        await service.SaveAsync(new PortableSettings
        {
            SavedDevices =
            {
                new SavedMouseConfiguration
                {
                    StableId = "container:a",
                    CustomAlias = "Travel Mouse",
                    OriginalDetectedName = "Bluetooth Mouse"
                }
            }
        }, CancellationToken.None);

        Assert.IsTrue(File.Exists($"{paths.PortableSettingsPath}.bak"));
    }

    private static AppPaths CreatePaths()
    {
        var path = Path.Combine(Path.GetTempPath(), "MouseTuneTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new AppPaths(path);
    }
}
