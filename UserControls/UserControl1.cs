using System.Timers;
using System.Windows;
using System.Windows.Controls;
using SolarNG.ViewModel;

namespace SolarNG.UserControls;

public partial class UserControl1 : UserControl
{
    private AppTabViewModel appTabViewModel;

    private readonly Timer resizeTimer = new Timer(100.0)
    {
        Enabled = false
    };

    public UserControl1()
    {
        InitializeComponent();
        base.SizeChanged += UserControl1_SizeChanged;
        resizeTimer.Elapsed += ResizingDone;
    }

    private void AppTab_OnLoaded(object sender, RoutedEventArgs e)
    {
        appTabViewModel = base.DataContext as AppTabViewModel;
    }

    private void UserControl1_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        resizeTimer.Stop();
        resizeTimer.Start();
    }

    private void ResizeApp()
    {
        appTabViewModel?.Resize();
    }

    private void ResizingDone(object sender, ElapsedEventArgs e)
    {
        resizeTimer.Stop();
        ResizeApp();
    }
}
