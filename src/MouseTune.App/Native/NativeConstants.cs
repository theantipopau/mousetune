namespace MouseTune.Native;

public static class NativeConstants
{
    public const int DigcfPresent = 0x00000002;
    public const int DigcfDeviceInterface = 0x00000010;

    public const int SpdrpDevicedesc = 0x00000000;
    public const int SpdrpMfg = 0x0000000B;
    public const int SpdrpFriendlyname = 0x0000000C;

    public const int ErrorNoMoreItems = 259;
    public const int ErrorInsufficientBuffer = 122;

    public const uint SpiGetMouse = 0x0003;
    public const uint SpiSetMouse = 0x0004;
    public const uint SpiGetMouseSpeed = 0x0070;
    public const uint SpiSetMouseSpeed = 0x0071;
    public const uint SpifUpdateIniFile = 0x0001;
    public const uint SpifSendChange = 0x0002;

    public const uint CrSuccess = 0x00000000;
    public const uint DevpropTypeString = 0x00000012;
    public const uint DevpropTypeGuid = 0x0000000D;
}
