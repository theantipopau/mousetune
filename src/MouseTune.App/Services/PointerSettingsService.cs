using MouseTune.Models;

namespace MouseTune.Services;

public sealed class PointerSettingsService
{
    private readonly IPointerSettingsNative _native;
    private readonly AppLogger _logger;
    private PointerSettings? _originalSettings;

    public PointerSettingsService(IPointerSettingsNative native, AppLogger logger)
    {
        _native = native;
        _logger = logger;
    }

    public PointerSettings ReadCurrent()
    {
        var settings = _native.Read();
        _originalSettings ??= settings;
        return settings;
    }

    public OperationResult ApplyEffectiveDpi(int effectiveDpi, bool enhancePointerPrecision)
    {
        var speed = EffectiveDpiMapper.ToWindowsSpeed(effectiveDpi);
        var settings = new PointerSettings(
            speed,
            enhancePointerPrecision,
            enhancePointerPrecision ? 6 : 0,
            enhancePointerPrecision ? 10 : 0,
            enhancePointerPrecision ? 1 : 0);

        _native.Apply(settings);
        _logger.Log("PointerSpeedChanged", $"Applied effective DPI {effectiveDpi}, Windows speed {speed}.");
        return OperationResult.Success($"Applied Effective DPI {effectiveDpi} with Windows pointer speed {speed} / 20.");
    }

    public OperationResult ApplyEffectiveDpiVerified(int effectiveDpi, bool enhancePointerPrecision)
    {
        var speed = EffectiveDpiMapper.ToWindowsSpeed(effectiveDpi);
        var settings = BuildSettings(speed, enhancePointerPrecision);
        return ApplyAndVerify(settings, $"Effective DPI {effectiveDpi}", expectedEffectiveDpi: effectiveDpi);
    }

    public OperationResult ResetToWindowsDefault()
    {
        return ApplyAndVerify(PointerSettings.WindowsDefault, "standard Windows defaults");
    }

    public OperationResult RestoreOriginal()
    {
        if (_originalSettings is null)
        {
            return ResetToWindowsDefault();
        }

        _native.Apply(_originalSettings);
        _logger.Log("PointerSettingsRestored", "Restored original pointer settings.");
        return OperationResult.Success("Original pointer settings restored.");
    }

    public OperationResult RestoreSnapshot(WindowsMouseSettingsSnapshot snapshot)
    {
        return ApplyAndVerify(snapshot.ToPointerSettings(), "settings from before MouseTune");
    }

    public OperationResult ApplyAndVerify(PointerSettings settings, string description, int? expectedEffectiveDpi = null)
    {
        try
        {
            _native.Apply(settings);
            var actual = _native.Read();
            if (actual.WindowsPointerSpeed != settings.WindowsPointerSpeed
                || actual.Threshold1 != settings.Threshold1
                || actual.Threshold2 != settings.Threshold2
                || actual.Acceleration != settings.Acceleration)
            {
                return OperationResult.Failure(
                    $"Windows accepted the request for {description}, but the read-back values did not match.",
                    "PointerSettingsVerificationFailed");
            }

            var dpiText = expectedEffectiveDpi is null ? string.Empty : $" ({expectedEffectiveDpi} effective DPI)";
            _logger.Log("PointerSettingsChanged", $"Applied {description}{dpiText}, Windows speed {settings.WindowsPointerSpeed}.");
            return OperationResult.Success($"Applied {description}. Windows pointer speed is {settings.WindowsPointerSpeed} / 20.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            _logger.Log("PointerSettingsFailed", $"Failed to apply {description}.", ex);
            return OperationResult.Failure(ex.Message, ex.GetType().Name);
        }
    }

    private static PointerSettings BuildSettings(int windowsPointerSpeed, bool enhancePointerPrecision) =>
        new(
            windowsPointerSpeed,
            enhancePointerPrecision,
            enhancePointerPrecision ? 6 : 0,
            enhancePointerPrecision ? 10 : 0,
            enhancePointerPrecision ? 1 : 0);
}
