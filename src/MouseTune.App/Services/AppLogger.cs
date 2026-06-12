namespace MouseTune.Services;

public sealed class AppLogger
{
    private readonly AppPaths _paths;
    private readonly object _sync = new();

    public AppLogger(AppPaths paths)
    {
        _paths = paths;
    }

    public void Log(string eventName, string message, Exception? exception = null)
    {
        try
        {
            _paths.EnsureCreated();
            var line = $"{DateTimeOffset.UtcNow:O}\t{eventName}\t{message}";
            if (exception is not null)
            {
                line += $"\t{exception.GetType().Name}: {exception.Message}";
            }

            lock (_sync)
            {
                File.AppendAllLines(_paths.LogPath, new[] { line });
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
