namespace MouseTune.Services;

public static class EffectiveDpiMapper
{
    private static readonly (int Dpi, int Speed)[] Points =
    {
        (200, 1),
        (400, 5),
        (800, 10),
        (1200, 12),
        (1600, 14),
        (2400, 16),
        (3000, 18),
        (3200, 18),
        (4800, 19),
        (6400, 20)
    };

    public const int MinimumDpi = 200;
    public const int MaximumDpi = 6400;
    public const int MinimumWindowsSpeed = 1;
    public const int MaximumWindowsSpeed = 20;
    public const int DefaultDpi = 800;
    public const int DefaultWindowsSpeed = 10;

    public static int ClampDpi(int dpi) => Math.Clamp(dpi, MinimumDpi, MaximumDpi);

    public static int ClampWindowsSpeed(int speed) => Math.Clamp(speed, MinimumWindowsSpeed, MaximumWindowsSpeed);

    public static int ToWindowsSpeed(int effectiveDpi)
    {
        var dpi = ClampDpi(effectiveDpi);
        for (var i = 0; i < Points.Length - 1; i++)
        {
            var left = Points[i];
            var right = Points[i + 1];
            if (dpi >= left.Dpi && dpi <= right.Dpi)
            {
                return Interpolate(left.Dpi, left.Speed, right.Dpi, right.Speed, dpi);
            }
        }

        return MaximumWindowsSpeed;
    }

    public static int ToEffectiveDpi(int windowsSpeed)
    {
        var speed = ClampWindowsSpeed(windowsSpeed);
        for (var i = 0; i < Points.Length - 1; i++)
        {
            var left = Points[i];
            var right = Points[i + 1];
            if (speed >= left.Speed && speed <= right.Speed)
            {
                return Interpolate(left.Speed, left.Dpi, right.Speed, right.Dpi, speed);
            }
        }

        return MaximumDpi;
    }

    private static int Interpolate(int x1, int y1, int x2, int y2, int x)
    {
        if (x2 == x1)
        {
            return y1;
        }

        var ratio = (double)(x - x1) / (x2 - x1);
        return (int)Math.Round(y1 + ((y2 - y1) * ratio), MidpointRounding.AwayFromZero);
    }
}
