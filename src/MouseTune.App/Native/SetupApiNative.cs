using System.Runtime.InteropServices;

namespace MouseTune.Native;

public static partial class SetupApiNative
{
    public static readonly Guid GuidDeviceInterfaceMouse = new("378de44c-56ef-11d1-bc8c-00a0c91405dd");

    [StructLayout(LayoutKind.Sequential)]
    public struct SpDeviceInterfaceData
    {
        public int CbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpDevinfoData
    {
        public int CbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeDeviceInfoSetHandle SetupDiGetClassDevs(
        ref Guid classGuid,
        string? enumerator,
        IntPtr hwndParent,
        int flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInterfaces(
        SafeDeviceInfoSetHandle deviceInfoSet,
        IntPtr deviceInfoData,
        ref Guid interfaceClassGuid,
        uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        int deviceInterfaceDetailDataSize,
        out int requiredSize,
        ref SpDevinfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevinfoData deviceInfoData,
        int property,
        out uint propertyRegDataType,
        byte[] propertyBuffer,
        int propertyBufferSize,
        out int requiredSize);

    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDevicePropertyW", SetLastError = true)]
    public static extern bool SetupDiGetDeviceProperty(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevinfoData deviceInfoData,
        ref ConfigurationManagerNative.DevPropKey propertyKey,
        out uint propertyType,
        byte[] propertyBuffer,
        int propertyBufferSize,
        out int requiredSize,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
}
