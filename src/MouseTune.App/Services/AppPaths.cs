namespace MouseTune.Services;

public sealed class AppPaths
{
    public AppPaths(string? basePath = null)
    {
        BasePath = basePath ?? ResolvePortableBasePath();
        LogsPath = BasePath;
        SettingsPath = Path.Combine(BasePath, "MouseTune.settings.json");
        PortableSettingsPath = SettingsPath;
        LogPath = Path.Combine(BasePath, "MouseTune.log");
    }

    public string BasePath { get; }
    public string LogsPath { get; }
    public string SettingsPath { get; }
    public string PortableSettingsPath { get; }
    public string LogPath { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(BasePath);
    }

    private static string ResolvePortableBasePath()
    {
        var executableFolder = AppContext.BaseDirectory;
        if (CanWriteTo(executableFolder))
        {
            return executableFolder;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MouseTunePortable");
    }

    private static bool CanWriteTo(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".mousetune-write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
