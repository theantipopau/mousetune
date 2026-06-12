using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseTune.Services;

namespace MouseTune.Tests;

[TestClass]
public sealed class PointerSettingsVerificationTests
{
    [TestMethod]
    public void ApplyReportsSuccessWhenReadBackMatches()
    {
        var native = new FakePointerSettingsNative();
        var service = new PointerSettingsService(native, new AppLogger(CreatePaths()));

        var result = service.ApplyEffectiveDpiVerified(3000, enhancePointerPrecision: false);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(18, native.Current.WindowsPointerSpeed);
    }

    [TestMethod]
    public void ApplyReportsFailureWhenReadBackDoesNotMatch()
    {
        var native = new FakePointerSettingsNative { ForceReadBackSpeed = 10 };
        var service = new PointerSettingsService(native, new AppLogger(CreatePaths()));

        var result = service.ApplyEffectiveDpiVerified(3000, enhancePointerPrecision: false);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("PointerSettingsVerificationFailed", result.ErrorCode);
    }

    private static AppPaths CreatePaths()
    {
        var path = Path.Combine(Path.GetTempPath(), "MouseTuneTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new AppPaths(path);
    }

    private sealed class FakePointerSettingsNative : IPointerSettingsNative
    {
        public PointerSettings Current { get; private set; } = PointerSettings.WindowsDefault;
        public int? ForceReadBackSpeed { get; init; }

        public PointerSettings Read()
        {
            return ForceReadBackSpeed is null
                ? Current
                : Current with { WindowsPointerSpeed = ForceReadBackSpeed.Value };
        }

        public void Apply(PointerSettings settings)
        {
            Current = settings;
        }
    }
}
