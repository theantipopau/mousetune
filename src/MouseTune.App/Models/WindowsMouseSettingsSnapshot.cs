namespace MouseTune.Models;

using MouseTune.Services;

public sealed class WindowsMouseSettingsSnapshot
{
    public int WindowsPointerSpeed { get; set; }
    public int Threshold1 { get; set; }
    public int Threshold2 { get; set; }
    public int Acceleration { get; set; }
    public bool EnhancePointerPrecision { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static WindowsMouseSettingsSnapshot FromPointerSettings(PointerSettings settings) => new()
    {
        WindowsPointerSpeed = settings.WindowsPointerSpeed,
        Threshold1 = settings.Threshold1,
        Threshold2 = settings.Threshold2,
        Acceleration = settings.Acceleration,
        EnhancePointerPrecision = settings.EnhancePointerPrecision,
        CapturedAtUtc = DateTimeOffset.UtcNow
    };

    public PointerSettings ToPointerSettings() => new(
        WindowsPointerSpeed,
        EnhancePointerPrecision,
        Threshold1,
        Threshold2,
        Acceleration);
}
