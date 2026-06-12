using System.Text.RegularExpressions;
using MouseTune.Models;

namespace MouseTune.Services;

public sealed class DeviceIdentityService
{
    private static readonly Regex SerialRegex = new(@"\\([^\\#&]+)$", RegexOptions.Compiled);

    public string BuildStableId(MouseDevice device)
    {
        if (!string.IsNullOrWhiteSpace(device.ContainerId))
        {
            return $"container:{device.ContainerId}";
        }

        if (!string.IsNullOrWhiteSpace(device.BluetoothAddress))
        {
            return $"bt:{device.BluetoothAddress}";
        }

        if (!string.IsNullOrWhiteSpace(device.ParentDeviceInstanceId))
        {
            return $"parent:{device.ParentDeviceInstanceId}";
        }

        if (!string.IsNullOrWhiteSpace(device.VendorId)
            && !string.IsNullOrWhiteSpace(device.ProductId)
            && !string.IsNullOrWhiteSpace(device.SerialNumber))
        {
            return $"hid:{device.VendorId}:{device.ProductId}:{device.SerialNumber}";
        }

        if (!string.IsNullOrWhiteSpace(device.VendorId) && !string.IsNullOrWhiteSpace(device.ProductId))
        {
            return $"hid:{device.VendorId}:{device.ProductId}:{NormalizeDeviceRoot(device.DeviceInstanceId)}";
        }

        return $"instance:{device.DeviceInstanceId}";
    }

    public SavedMouseConfiguration CreateConfiguration(
        MouseDevice device,
        string alias,
        int effectiveDpi,
        int windowsPointerSpeed,
        bool enhancePointerPrecision)
    {
        return new SavedMouseConfiguration
        {
            StableId = BuildStableId(device),
            ContainerId = device.ContainerId,
            BluetoothAddress = device.BluetoothAddress,
            ParentDeviceInstanceId = device.ParentDeviceInstanceId,
            DeviceInstanceId = device.DeviceInstanceId,
            VendorId = device.VendorId,
            ProductId = device.ProductId,
            SerialNumber = device.SerialNumber,
            OriginalDetectedName = device.OriginalName,
            CustomAlias = DeviceNameValidator.Normalize(alias),
            EffectiveDpi = EffectiveDpiMapper.ClampDpi(effectiveDpi),
            WindowsPointerSpeed = EffectiveDpiMapper.ClampWindowsSpeed(windowsPointerSpeed),
            EnhancePointerPrecision = enhancePointerPrecision,
            LastModifiedUtc = DateTimeOffset.UtcNow
        };
    }

    public SavedMouseConfiguration? FindBestMatch(MouseDevice device, IEnumerable<SavedMouseConfiguration> configurations)
    {
        var stableId = BuildStableId(device);
        return configurations
            .Select(configuration => new
            {
                Configuration = configuration,
                Score = Score(device, stableId, configuration)
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Configuration.LastModifiedUtc)
            .Select(candidate => candidate.Configuration)
            .FirstOrDefault();
    }

    public static string? ExtractSerialNumber(string deviceInstanceId)
    {
        var match = SerialRegex.Match(deviceInstanceId);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static int Score(MouseDevice device, string stableId, SavedMouseConfiguration configuration)
    {
        var score = 0;
        if (EqualsOrdinal(configuration.StableId, stableId))
        {
            score += 100;
        }

        if (EqualsOrdinal(configuration.ContainerId, device.ContainerId))
        {
            score += 80;
        }

        if (EqualsOrdinal(configuration.BluetoothAddress, device.BluetoothAddress))
        {
            score += 70;
        }

        if (EqualsOrdinal(configuration.ParentDeviceInstanceId, device.ParentDeviceInstanceId))
        {
            score += 50;
        }

        if (EqualsOrdinal(configuration.DeviceInstanceId, device.DeviceInstanceId))
        {
            score += 40;
        }

        if (EqualsOrdinal(configuration.VendorId, device.VendorId))
        {
            score += 10;
        }

        if (EqualsOrdinal(configuration.ProductId, device.ProductId))
        {
            score += 10;
        }

        if (EqualsOrdinal(configuration.SerialNumber, device.SerialNumber))
        {
            score += 20;
        }

        return score;
    }

    private static bool EqualsOrdinal(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeDeviceRoot(string deviceInstanceId)
    {
        var index = deviceInstanceId.LastIndexOf('\\');
        return index > 0 ? deviceInstanceId[..index] : deviceInstanceId;
    }
}
