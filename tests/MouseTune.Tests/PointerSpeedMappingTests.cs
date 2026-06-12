using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseTune.Services;

namespace MouseTune.Tests;

[TestClass]
public sealed class PointerSpeedMappingTests
{
    [TestMethod]
    public void DpiPresetsMapToExpectedWindowsSpeeds()
    {
        Assert.AreEqual(5, EffectiveDpiMapper.ToWindowsSpeed(400));
        Assert.AreEqual(10, EffectiveDpiMapper.ToWindowsSpeed(800));
        Assert.AreEqual(12, EffectiveDpiMapper.ToWindowsSpeed(1200));
        Assert.AreEqual(14, EffectiveDpiMapper.ToWindowsSpeed(1600));
        Assert.AreEqual(16, EffectiveDpiMapper.ToWindowsSpeed(2400));
        Assert.AreEqual(18, EffectiveDpiMapper.ToWindowsSpeed(3000));
        Assert.AreEqual(18, EffectiveDpiMapper.ToWindowsSpeed(3200));
        Assert.AreEqual(19, EffectiveDpiMapper.ToWindowsSpeed(4800));
        Assert.AreEqual(20, EffectiveDpiMapper.ToWindowsSpeed(6400));
    }

    [TestMethod]
    public void WindowsSpeedsMapBackToEffectiveDpi()
    {
        Assert.AreEqual(800, EffectiveDpiMapper.ToEffectiveDpi(10));
        Assert.AreEqual(1600, EffectiveDpiMapper.ToEffectiveDpi(14));
        Assert.AreEqual(6400, EffectiveDpiMapper.ToEffectiveDpi(20));
    }

    [TestMethod]
    public void ValuesAreClamped()
    {
        Assert.AreEqual(200, EffectiveDpiMapper.ClampDpi(-1));
        Assert.AreEqual(6400, EffectiveDpiMapper.ClampDpi(10000));
        Assert.AreEqual(1, EffectiveDpiMapper.ClampWindowsSpeed(-1));
        Assert.AreEqual(20, EffectiveDpiMapper.ClampWindowsSpeed(100));
        Assert.AreEqual(1, EffectiveDpiMapper.ToWindowsSpeed(200));
    }

    [TestMethod]
    public void SavedDpiCanBePreferredWhenSpeedIsAmbiguous()
    {
        Assert.AreEqual(18, EffectiveDpiMapper.ToWindowsSpeed(3000));
        Assert.AreEqual(18, EffectiveDpiMapper.ToWindowsSpeed(3200));
    }
}
