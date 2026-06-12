using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseTune.Models;
using MouseTune.Services;

namespace MouseTune.Tests;

[TestClass]
public sealed class DeviceDeduplicationTests
{
    [TestMethod]
    public void InterfacesForSameVidPidAreDeduplicated()
    {
        var devices = new[]
        {
            new MouseDevice
            {
                DeviceInstanceId = @"HID\VID_1234&PID_ABCD&MI_00\1",
                InterfacePath = "a",
                CurrentName = "A",
                OriginalName = "A",
                VendorId = "1234",
                ProductId = "ABCD",
                IsConnected = true
            },
            new MouseDevice
            {
                DeviceInstanceId = @"HID\VID_1234&PID_ABCD&MI_01\2",
                InterfacePath = "b",
                CurrentName = "B",
                OriginalName = "B",
                VendorId = "1234",
                ProductId = "ABCD",
                IsConnected = true
            }
        };

        Assert.AreEqual(1, MouseDeviceDiscoveryService.Deduplicate(devices).Count);
    }

    [TestMethod]
    public void InterfacesForSameContainerIdAreDeduplicated()
    {
        var devices = new[]
        {
            new MouseDevice
            {
                DeviceInstanceId = @"HID\VID_1234&PID_ABCD&MI_00\1",
                InterfacePath = "a",
                ContainerId = "abc",
                CurrentName = "A",
                OriginalName = "A",
                IsConnected = true
            },
            new MouseDevice
            {
                DeviceInstanceId = @"HID\VID_1234&PID_ABCD&MI_01\2",
                InterfacePath = "b",
                ContainerId = "abc",
                CurrentName = "B",
                OriginalName = "B",
                IsConnected = true
            }
        };

        Assert.AreEqual(1, MouseDeviceDiscoveryService.Deduplicate(devices).Count);
    }
}
