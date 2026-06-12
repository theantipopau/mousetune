using MouseTune.Models;

namespace MouseTune.ViewModels;

public sealed class DeviceViewModel : ObservableObject
{
    private readonly MouseDevice _device;

    public DeviceViewModel(MouseDevice device)
    {
        _device = device;
    }

    public MouseDevice Model => _device;
    public string DeviceInstanceId => _device.DeviceInstanceId;
    public string CurrentName
    {
        get => _device.CurrentName;
        set
        {
            if (_device.CurrentName != value)
            {
                _device.CurrentName = value;
                OnPropertyChanged();
            }
        }
    }

    public string OriginalName => _device.OriginalName;
    public string Manufacturer => _device.Manufacturer ?? "Unknown";
    public string VendorId => _device.VendorId ?? "Unknown";
    public string ProductId => _device.ProductId ?? "Unknown";
    public string BluetoothAddress => _device.BluetoothAddress ?? "Not available";
    public MouseConnectionType ConnectionType => _device.ConnectionType;
    public string ConnectionTypeText => _device.ConnectionType.ToString();
    public string ConnectionStatus => _device.IsConnected ? "Connected" : "Disconnected";
    public string HardwareDpiStatus => _device.SupportsHardwareDpi ? "Supported" : "Effective DPI only";
    public string CurrentProfileName => _device.CurrentProfileName ?? "No profile";
}
