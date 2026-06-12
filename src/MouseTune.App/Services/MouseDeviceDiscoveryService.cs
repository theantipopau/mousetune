using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using MouseTune.Models;
using MouseTune.Native;

namespace MouseTune.Services;

public sealed class MouseDeviceDiscoveryService
{
    private static readonly Regex VidRegex = new(@"VID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PidRegex = new(@"PID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly AppLogger _logger;

    public MouseDeviceDiscoveryService(AppLogger logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<MouseDevice>> GetMouseDevicesAsync(bool includeVirtualDevices, CancellationToken cancellationToken) =>
        Task.Run(() => Enumerate(includeVirtualDevices, cancellationToken), cancellationToken);

    public static IReadOnlyList<MouseDevice> Deduplicate(IEnumerable<MouseDevice> devices)
    {
        return devices
            .GroupBy(GetDeduplicationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(device => device.IsConnected)
                .ThenBy(device => device.InterfacePath is null)
                .First())
            .OrderByDescending(device => device.IsConnected)
            .ThenBy(device => device.CurrentName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<MouseDevice> Enumerate(bool includeVirtualDevices, CancellationToken cancellationToken)
    {
        var devices = new List<MouseDevice>();
        var guid = SetupApiNative.GuidDeviceInterfaceMouse;
        using var set = SetupApiNative.SetupDiGetClassDevs(
            ref guid,
            null,
            IntPtr.Zero,
            NativeConstants.DigcfPresent | NativeConstants.DigcfDeviceInterface);

        if (set.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        for (uint index = 0; ; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var interfaceData = new SetupApiNative.SpDeviceInterfaceData
            {
                CbSize = Marshal.SizeOf<SetupApiNative.SpDeviceInterfaceData>()
            };

            if (!SetupApiNative.SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, index, ref interfaceData))
            {
                if (Marshal.GetLastWin32Error() == NativeConstants.ErrorNoMoreItems)
                {
                    break;
                }

                continue;
            }

            if (TryCreateDevice(set, ref interfaceData, includeVirtualDevices, out var device))
            {
                devices.Add(device);
            }
        }

        var deduped = Deduplicate(devices);
        _logger.Log("DeviceEnumeration", $"Detected {deduped.Count} mouse device(s).");
        return deduped;
    }

    private static bool TryCreateDevice(
        SafeDeviceInfoSetHandle set,
        ref SetupApiNative.SpDeviceInterfaceData interfaceData,
        bool includeVirtualDevices,
        out MouseDevice device)
    {
        device = default!;
        var devInfo = new SetupApiNative.SpDevinfoData
        {
            CbSize = Marshal.SizeOf<SetupApiNative.SpDevinfoData>()
        };

        _ = SetupApiNative.SetupDiGetDeviceInterfaceDetail(set, ref interfaceData, IntPtr.Zero, 0, out var requiredSize, ref devInfo);
        if (requiredSize <= 0)
        {
            return false;
        }

        var detailBuffer = Marshal.AllocHGlobal(requiredSize);
        try
        {
            Marshal.WriteInt32(detailBuffer, IntPtr.Size == 8 ? 8 : 6);
            if (!SetupApiNative.SetupDiGetDeviceInterfaceDetail(set, ref interfaceData, detailBuffer, requiredSize, out _, ref devInfo))
            {
                return false;
            }

            var interfacePath = Marshal.PtrToStringUni(IntPtr.Add(detailBuffer, 4));
            var instanceId = GetDeviceInstanceId(devInfo.DevInst);
            if (string.IsNullOrWhiteSpace(interfacePath) || string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            if (!includeVirtualDevices && IsLikelyVirtual(instanceId, interfacePath))
            {
                return false;
            }

            var friendlyName = GetRegistryProperty(set, ref devInfo, NativeConstants.SpdrpFriendlyname)
                ?? GetRegistryProperty(set, ref devInfo, NativeConstants.SpdrpDevicedesc)
                ?? "Mouse";
            var manufacturer = GetRegistryProperty(set, ref devInfo, NativeConstants.SpdrpMfg);
            var containerId = GetGuidProperty(set, ref devInfo, ConfigurationManagerNative.DevpkeyDeviceContainerId)?.ToString("D");
            var parentDeviceInstanceId = GetParentDeviceInstanceId(devInfo.DevInst);
            var bluetoothAddress = BluetoothNative.ExtractBluetoothAddress(instanceId) ?? BluetoothNative.ExtractBluetoothAddress(interfacePath);
            var vendorId = MatchValue(VidRegex, instanceId) ?? MatchValue(VidRegex, interfacePath);
            var productId = MatchValue(PidRegex, instanceId) ?? MatchValue(PidRegex, interfacePath);

            device = new MouseDevice
            {
                DeviceInstanceId = instanceId,
                InterfacePath = interfacePath,
                ContainerId = containerId,
                ParentDeviceInstanceId = parentDeviceInstanceId,
                SerialNumber = DeviceIdentityService.ExtractSerialNumber(instanceId),
                CurrentName = friendlyName,
                OriginalName = friendlyName,
                Manufacturer = manufacturer,
                VendorId = vendorId,
                ProductId = productId,
                BluetoothAddress = bluetoothAddress,
                ConnectionType = InferConnectionType(instanceId, interfacePath),
                IsConnected = true,
                SupportsHardwareDpi = false
            };
            device.StableId = new DeviceIdentityService().BuildStableId(device);
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(detailBuffer);
        }
    }

    private static string? GetDeviceInstanceId(uint devInst)
    {
        var buffer = new char[1024];
        var result = ConfigurationManagerNative.CM_Get_Device_ID(devInst, buffer, buffer.Length, 0);
        if (result != NativeConstants.CrSuccess)
        {
            return null;
        }

        return new string(buffer).TrimEnd('\0');
    }

    private static string? GetParentDeviceInstanceId(uint devInst)
    {
        var result = ConfigurationManagerNative.CM_Get_Parent(out var parentDevInst, devInst, 0);
        return result == NativeConstants.CrSuccess ? GetDeviceInstanceId(parentDevInst) : null;
    }

    private static string? GetRegistryProperty(SafeDeviceInfoSetHandle set, ref SetupApiNative.SpDevinfoData devInfo, int property)
    {
        var buffer = new byte[1024];
        if (!SetupApiNative.SetupDiGetDeviceRegistryProperty(set, ref devInfo, property, out _, buffer, buffer.Length, out var required)
            || required <= 2)
        {
            return null;
        }

        return Encoding.Unicode.GetString(buffer, 0, required).TrimEnd('\0');
    }

    private static Guid? GetGuidProperty(
        SafeDeviceInfoSetHandle set,
        ref SetupApiNative.SpDevinfoData devInfo,
        ConfigurationManagerNative.DevPropKey propertyKey)
    {
        var key = propertyKey;
        var buffer = new byte[16];
        try
        {
            if (!SetupApiNative.SetupDiGetDeviceProperty(
                    set,
                    ref devInfo,
                    ref key,
                    out var propertyType,
                    buffer,
                    buffer.Length,
                    out var required,
                    0)
                || propertyType != NativeConstants.DevpropTypeGuid
                || required != 16)
            {
                return null;
            }

            return new Guid(buffer);
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static MouseConnectionType InferConnectionType(string instanceId, string interfacePath)
    {
        var combined = $"{instanceId} {interfacePath}";
        if (combined.Contains("BTHLE", StringComparison.OrdinalIgnoreCase))
        {
            return MouseConnectionType.BluetoothLe;
        }

        if (combined.Contains("BTH", StringComparison.OrdinalIgnoreCase))
        {
            return MouseConnectionType.Bluetooth;
        }

        if (combined.Contains("VID_", StringComparison.OrdinalIgnoreCase))
        {
            return MouseConnectionType.Usb;
        }

        return MouseConnectionType.Unknown;
    }

    private static bool IsLikelyVirtual(string instanceId, string interfacePath)
    {
        var combined = $"{instanceId} {interfacePath}";
        return combined.Contains("ROOT\\", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("VIRTUAL", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("RDP", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDeduplicationKey(MouseDevice device)
    {
        if (!string.IsNullOrWhiteSpace(device.StableId))
        {
            return device.StableId;
        }

        if (!string.IsNullOrWhiteSpace(device.ContainerId))
        {
            return $"container:{device.ContainerId}";
        }

        if (!string.IsNullOrWhiteSpace(device.BluetoothAddress))
        {
            return $"bt:{device.BluetoothAddress}";
        }

        if (!string.IsNullOrWhiteSpace(device.VendorId) && !string.IsNullOrWhiteSpace(device.ProductId))
        {
            return $"usb:{device.VendorId}:{device.ProductId}:{device.Manufacturer}";
        }

        var id = device.DeviceInstanceId;
        var separator = id.IndexOf('&', StringComparison.Ordinal);
        return separator > 0 ? id[..separator] : id;
    }

    private static string? MatchValue(Regex regex, string value)
    {
        var match = regex.Match(value);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }
}
