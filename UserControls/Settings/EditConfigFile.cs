using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SolarNG.ViewModel.Settings;

namespace SolarNG.UserControls.Settings;

public partial class EditConfigFile : UserControl
{
    public EditConfigFile()
    {
        InitializeComponent();
        base.DataContext = new EditConfigFileViewModel();
    }

    private void ListViewSessions_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ListView && !e.Handled)
        {
            e.Handled = true;
            MouseWheelEventArgs mouseWheelEventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) {RoutedEvent = UIElement.MouseWheelEvent, Source = sender };
            (((Control)sender).Parent as UIElement).RaiseEvent(mouseWheelEventArgs);
        }
    }
}
