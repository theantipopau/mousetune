namespace MouseTune.Services;

public sealed record PointerSettings(int WindowsPointerSpeed, bool EnhancePointerPrecision, int Threshold1, int Threshold2, int Acceleration)
{
    public static PointerSettings WindowsDefault { get; } = new(10, false, 6, 10, 0);
}
