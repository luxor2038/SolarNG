using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SolarNG.ViewModel.Settings;

namespace SolarNG.UserControls.Settings;

public partial class EditProxy : UserControl
{
    public EditProxy()
    {
        InitializeComponent();
        base.DataContext = new EditProxyViewModel();
        base.IsVisibleChanged += This_IsVisibleChanged;
    }

    private void This_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue)
        {
            return;
        }
        base.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)delegate
        {
            if (((EditProxyViewModel)base.DataContext).NewMode)
            {
                TxbIP.Focus();
            }
        });
    }

    private void ListViewTags_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ListView && !e.Handled)
        {
            e.Handled = true;
            MouseWheelEventArgs mouseWheelEventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) {RoutedEvent = UIElement.MouseWheelEvent, Source = sender };
            (((Control)sender).Parent as UIElement).RaiseEvent(mouseWheelEventArgs);
        }
    }
}
