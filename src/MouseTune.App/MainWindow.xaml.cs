using System.Windows;
using MouseTune.ViewModels;

namespace MouseTune;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(App.Services);
    }
}
