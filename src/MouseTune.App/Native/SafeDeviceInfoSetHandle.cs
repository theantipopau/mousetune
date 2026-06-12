using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MouseTune.Native;

public sealed class SafeDeviceInfoSetHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SafeDeviceInfoSetHandle()
        : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle() => SetupApiNative.SetupDiDestroyDeviceInfoList(handle);
}
