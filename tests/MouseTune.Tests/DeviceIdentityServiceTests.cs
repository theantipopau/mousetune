using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseTune.Models;
using MouseTune.Services;

namespace MouseTune.Tests;

[TestClass]
public sealed class DeviceIdentityServiceTests
{
    [TestMethod]
    public void ContainerIdIsPreferredForStableIdentity()
    {
        var service = new DeviceIdentityService();
        var device = CreateDevice(containerId: "b2b5c2b2-34da-4f1c-a599-b819599ffeed");

        Assert.AreEqual("container:b2b5c2b2-34da-4f1c-a599-b819599ffeed", service.BuildStableId(device));
    }

    [TestMethod]
    public void SavedDeviceMatchesAfterInterfacePathChanges()
    {
        var service = new DeviceIdentityService();
        var saved = new SavedMouseConfiguration
        {
            StableId = "container:abc",
            ContainerId = "abc",
            DeviceInstanceId = @"HID\VID_1234&PID_ABCD\old",
            CustomAlias = "Office Mouse"
        };
        var reconnected = CreateDevice(containerId: "abc", deviceInstanceId: @"HID\VID_1234&PID_ABCD\new");

        var match = service.FindBestMatch(reconnected, new[] { saved });

        Assert.IsNotNull(match);
        Assert.AreEqual("Office Mouse", match.CustomAlias);
    }

    [TestMethod]
    public void BluetoothAddressMatchesWhenContainerIdIsMissing()
    {
        var service = new DeviceIdentityService();
        var saved = new SavedMouseConfiguration
        {
            StableId = "bt:aabbccddeeff",
            BluetoothAddress = "aabbccddeeff",
            CustomAlias = "Travel Mouse"
        };
        var reconnected = CreateDevice(containerId: null, bluetoothAddress: "aabbccddeeff");

        var match = service.FindBestMatch(reconnected, new[] { saved });

        Assert.IsNotNull(match);
        Assert.AreEqual("Travel Mouse", match.CustomAlias);
    }

    private static MouseDevice CreateDevice(
        string? containerId,
        string? bluetoothAddress = null,
        string deviceInstanceId = @"HID\VID_1234&PID_ABCD\7&ABC") =>
        new()
        {
            DeviceInstanceId = deviceInstanceId,
            InterfacePath = "path",
            ContainerId = containerId,
            BluetoothAddress = bluetoothAddress,
            VendorId = "1234",
            ProductId = "ABCD",
            CurrentName = "BT 5.2 Mouse",
            OriginalName = "BT 5.2 Mouse",
            IsConnected = true
        };
}
