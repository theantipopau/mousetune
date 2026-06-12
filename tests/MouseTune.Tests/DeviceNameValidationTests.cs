using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseTune.Services;

namespace MouseTune.Tests;

[TestClass]
public sealed class DeviceNameValidationTests
{
    [TestMethod]
    public void DeviceAliasValidationAllowsOrdinaryNames()
    {
        var result = DeviceNameValidator.Validate(" Office Mouse ");

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual("Office Mouse", DeviceNameValidator.Normalize(" Office Mouse "));
    }

    [TestMethod]
    public void DeviceAliasValidationRejectsUnsafeNames()
    {
        Assert.IsFalse(DeviceNameValidator.Validate("").Succeeded);
        Assert.IsFalse(DeviceNameValidator.Validate(new string('x', 65)).Succeeded);
        Assert.IsFalse(DeviceNameValidator.Validate("Bad\u0001Name").Succeeded);
    }
}
