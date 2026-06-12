using System.Windows;
using System.Windows.Interop;
using MouseTune.ViewModels;

namespace MouseTune;

public partial class MainWindow : Window
{
    private const int WmDeviceChange = 0x0219;
    private const int DbtDevnodesChanged = 0x0007;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(App.Services);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.AddHook(WndProc);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmDeviceChange && wParam.ToInt32() == DbtDevnodesChanged && DataContext is MainViewModel viewModel)
        {
            viewModel.QueueDeviceChangeRefresh();
        }

        return IntPtr.Zero;
    }
}
