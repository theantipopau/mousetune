using System.Runtime.InteropServices;

namespace MouseTune.Native;

public static partial class ConfigurationManagerNative
{
    public static readonly DevPropKey DevpkeyDeviceFriendlyName = new(
        new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
        14);

    public static readonly DevPropKey DevpkeyDeviceContainerId = new(
        new Guid("8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c"),
        2);

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DevPropKey
    {
        public DevPropKey(Guid fmtid, uint pid)
        {
            FmtId = fmtid;
            Pid = pid;
        }

        public Guid FmtId { get; }
        public uint Pid { get; }
    }

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_Device_ID(
        uint dnDevInst,
        char[] buffer,
        int bufferLen,
        uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern uint CM_Locate_DevNode(
        out uint pdnDevInst,
        string pDeviceID,
        uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern uint CM_Set_DevNode_Property(
        uint dnDevInst,
        ref DevPropKey propertyKey,
        uint propertyType,
        byte[] propertyBuffer,
        uint propertyBufferSize,
        uint ulFlags);
}
