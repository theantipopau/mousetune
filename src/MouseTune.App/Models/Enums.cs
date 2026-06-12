namespace MouseTune.Models;

public enum MouseConnectionType
{
    Unknown,
    Usb,
    Bluetooth,
    BluetoothLe
}

public enum OperationState
{
    Idle,
    Scanning,
    Applying,
    Successful,
    Failed
}

public enum AppTheme
{
    System,
    Light,
    Dark
}

public enum AppLogLevel
{
    Error,
    Warning,
    Information,
    Debug
}
