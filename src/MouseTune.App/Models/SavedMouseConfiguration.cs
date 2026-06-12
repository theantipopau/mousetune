namespace MouseTune.Models;

public sealed class SavedMouseConfiguration
{
    public string StableId { get; set; } = string.Empty;
    public string? ContainerId { get; set; }
    public string? BluetoothAddress { get; set; }
    public string? ParentDeviceInstanceId { get; set; }
    public string? DeviceInstanceId { get; set; }
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public string? SerialNumber { get; set; }
    public string OriginalDetectedName { get; set; } = "Mouse";
    public string CustomAlias { get; set; } = "Mouse";
    public int EffectiveDpi { get; set; } = 800;
    public int WindowsPointerSpeed { get; set; } = 10;
    public bool EnhancePointerPrecision { get; set; }
    public DateTimeOffset LastModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
}
