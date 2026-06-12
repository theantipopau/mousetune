namespace MouseTune.Models;

public sealed class MouseDevice
{
    public required string DeviceInstanceId { get; init; }
    public string? InterfacePath { get; init; }
    public string? StableId { get; set; }
    public string? ContainerId { get; init; }
    public string? ParentDeviceInstanceId { get; init; }
    public string? SerialNumber { get; init; }
    public string CurrentName { get; set; } = "Mouse";
    public required string OriginalName { get; init; }
    public string? Manufacturer { get; init; }
    public string? VendorId { get; init; }
    public string? ProductId { get; init; }
    public string? BluetoothAddress { get; init; }
    public MouseConnectionType ConnectionType { get; init; }
    public bool IsConnected { get; init; }
    public bool SupportsHardwareDpi { get; init; }
    public string? CurrentProfileName { get; set; }
}
