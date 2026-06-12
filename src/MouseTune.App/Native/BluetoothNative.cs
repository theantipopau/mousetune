namespace MouseTune.Native;

public static class BluetoothNative
{
    public static string? ExtractBluetoothAddress(string value)
    {
        var normalized = value.Replace("&", "_", StringComparison.Ordinal).Replace("-", "_", StringComparison.Ordinal);
        var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return parts.FirstOrDefault(part => part.Length == 12 && part.All(Uri.IsHexDigit));
    }
}
