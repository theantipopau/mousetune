namespace MouseTune.Services;

public sealed class AppServices
{
    private AppServices(
        AppPaths paths,
        AppLogger logger,
        PortableSettingsService portableSettings,
        DeviceIdentityService deviceIdentity,
        MouseDeviceDiscoveryService discovery,
        DeviceRenameService rename,
        PointerSettingsService pointerSettings,
        DiagnosticsService diagnostics)
    {
        Paths = paths;
        Logger = logger;
        PortableSettings = portableSettings;
        DeviceIdentity = deviceIdentity;
        Discovery = discovery;
        Rename = rename;
        PointerSettings = pointerSettings;
        Diagnostics = diagnostics;
    }

    public AppPaths Paths { get; }
    public AppLogger Logger { get; }
    public PortableSettingsService PortableSettings { get; }
    public DeviceIdentityService DeviceIdentity { get; }
    public MouseDeviceDiscoveryService Discovery { get; }
    public DeviceRenameService Rename { get; }
    public PointerSettingsService PointerSettings { get; }
    public DiagnosticsService Diagnostics { get; }

    public static AppServices CreateDefault()
    {
        var paths = new AppPaths();
        paths.EnsureCreated();
        var store = new JsonFileStore();
        var logger = new AppLogger(paths);
        var portableSettings = new PortableSettingsService(paths, store, logger);
        var identity = new DeviceIdentityService();
        var discovery = new MouseDeviceDiscoveryService(logger);
        var rename = new DeviceRenameService(logger);
        var pointer = new PointerSettingsService(new WindowsPointerSettingsNative(), logger);
        var diagnostics = new DiagnosticsService(paths, logger);
        return new AppServices(
            paths,
            logger,
            portableSettings,
            identity,
            discovery,
            rename,
            pointer,
            diagnostics);
    }
}
