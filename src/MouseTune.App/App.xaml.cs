using System.Windows;
using System.Windows.Threading;
using MouseTune.Services;
using MouseTune.ViewModels;

namespace MouseTune;

public partial class App : Application
{
    internal static AppServices Services { get; } = AppServices.CreateDefault();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Services.Logger.Log("ApplicationStartup", "MouseTune started.");
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Services.Logger.Log("DispatcherUnhandledException", "Unhandled UI exception.", e.Exception);
        MessageBox.Show(
            $"MouseTune hit an unexpected error and logged it to {Services.Paths.LogPath}.\n\n{e.Exception.Message}",
            "MouseTune error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Services.Logger.Log("UnhandledException", "Unhandled application exception.", exception);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Services.Logger.Log("UnobservedTaskException", "Unobserved task exception.", e.Exception);
        e.SetObserved();
    }
}
