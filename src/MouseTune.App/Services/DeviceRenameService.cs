using System.Text;
using MouseTune.Models;
using MouseTune.Native;

namespace MouseTune.Services;

public sealed class DeviceRenameService
{
    private readonly AppLogger _logger;

    public DeviceRenameService(AppLogger logger)
    {
        _logger = logger;
    }

    public async Task<OperationResult> ApplyNameAsync(MouseDevice device, string requestedName, CancellationToken cancellationToken)
    {
        var validation = DeviceNameValidator.Validate(requestedName);
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (!IsMouseDeviceInstanceId(device.DeviceInstanceId))
        {
            return OperationResult.Failure("The selected device identifier does not look like a mouse device.", "InvalidDeviceInstanceId");
        }

        var normalized = DeviceNameValidator.Normalize(requestedName);
        var nativeResult = TrySetWindowsFriendlyName(device.DeviceInstanceId, normalized);
        device.CurrentName = normalized;

        _logger.Log("RenameAttempt", $"Rename for {device.DeviceInstanceId}: {nativeResult.Message}");
        if (nativeResult.Succeeded)
        {
            return OperationResult.Success("Windows device name changed successfully.");
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return OperationResult.Failure(
            "Windows did not permit this Bluetooth device name to be changed. The alias has still been saved in MouseTune.",
            nativeResult.ErrorCode,
            nativeResult.RequiresElevation);
    }

    public async Task<OperationResult> RestoreOriginalAsync(MouseDevice device, CancellationToken cancellationToken)
    {
        var nativeResult = TrySetWindowsFriendlyName(device.DeviceInstanceId, device.OriginalName);
        device.CurrentName = device.OriginalName;
        _logger.Log("RenameRestore", $"Restore for {device.DeviceInstanceId}: {nativeResult.Message}");
        await Task.CompletedTask.ConfigureAwait(false);
        return nativeResult.Succeeded
            ? OperationResult.Success("Restored the original local name where Windows permits it.")
            : OperationResult.Failure("The MouseTune alias was restored to the detected name, but Windows did not permit changing the system device name.", nativeResult.ErrorCode);
    }

    private static OperationResult TrySetWindowsFriendlyName(string deviceInstanceId, string friendlyName)
    {
        var locate = ConfigurationManagerNative.CM_Locate_DevNode(out var devInst, deviceInstanceId, 0);
        if (locate != NativeConstants.CrSuccess)
        {
            return OperationResult.Failure("Windows could not locate the selected device node.", $"CM_{locate:X8}");
        }

        var bytes = Encoding.Unicode.GetBytes(friendlyName + '\0');
        var key = ConfigurationManagerNative.DevpkeyDeviceFriendlyName;
        var result = ConfigurationManagerNative.CM_Set_DevNode_Property(
            devInst,
            ref key,
            NativeConstants.DevpropTypeString,
            bytes,
            (uint)bytes.Length,
            0);

        return result == NativeConstants.CrSuccess
            ? OperationResult.Success("Windows friendly-name property updated.")
            : OperationResult.Failure("Windows rejected the friendly-name change.", $"CM_{result:X8}", result is 0x00000033 or 0x00000005);
    }

    private static bool IsMouseDeviceInstanceId(string instanceId)
    {
        return instanceId.Contains("HID\\", StringComparison.OrdinalIgnoreCase)
            || instanceId.Contains("BTH", StringComparison.OrdinalIgnoreCase)
            || instanceId.Contains("VID_", StringComparison.OrdinalIgnoreCase)
            || instanceId.Contains("MI_", StringComparison.OrdinalIgnoreCase);
    }
}
