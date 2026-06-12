using System.ComponentModel;
using MouseTune.Native;

namespace MouseTune.Services;

public sealed class WindowsPointerSettingsNative : IPointerSettingsNative
{
    public PointerSettings Read()
    {
        var speed = 0;
        if (!User32Native.SystemParametersInfo(NativeConstants.SpiGetMouseSpeed, 0, ref speed, 0))
        {
            throw new Win32Exception();
        }

        var mouse = new int[3];
        if (!User32Native.SystemParametersInfo(NativeConstants.SpiGetMouse, 0, mouse, 0))
        {
            throw new Win32Exception();
        }

        return new PointerSettings(
            EffectiveDpiMapper.ClampWindowsSpeed(speed),
            mouse[2] > 0,
            mouse[0],
            mouse[1],
            mouse[2]);
    }

    public void Apply(PointerSettings settings)
    {
        var speed = EffectiveDpiMapper.ClampWindowsSpeed(settings.WindowsPointerSpeed);
        if (!User32Native.SystemParametersInfo(
                NativeConstants.SpiSetMouseSpeed,
                0,
                ref speed,
                NativeConstants.SpifUpdateIniFile | NativeConstants.SpifSendChange))
        {
            throw new Win32Exception();
        }

        var mouse = new[] { settings.Threshold1, settings.Threshold2, settings.Acceleration };
        if (!User32Native.SystemParametersInfo(
                NativeConstants.SpiSetMouse,
                0,
                mouse,
                NativeConstants.SpifUpdateIniFile | NativeConstants.SpifSendChange))
        {
            throw new Win32Exception();
        }
    }
}
