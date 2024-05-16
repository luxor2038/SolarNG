using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using SolarNG.Sessions;
using SolarNG.ViewModel;

namespace SolarNG.UserControls;

public partial class OverviewTab : UserControl, IStyleConnector
{
    public OverviewTab()
    {
        InitializeComponent();
    }

    private void SessionOverviewTab_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (base.DataContext is OverviewTabViewModel overviewTabViewModel && overviewTabViewModel.AllItems != null)
        {
            btnCreateNewSession.Content = (string)Application.Current.Resources["Refresh"];
        }
    }

    private void TextBlockTriple_MouseUp(object sender, MouseButtonEventArgs e)
    {
        ContextMenu ItemMenu = (sender as Border).FindName("ItemMenu") as ContextMenu;
        ItemMenu.IsOpen = true;
    }

    private void ContextMenu_OnLoaded(object sender, RoutedEventArgs e)
    {
        ContextMenu contextMenu = sender as ContextMenu;
        contextMenu.DataContext = base.DataContext;
        if (base.DataContext is OverviewTabViewModel overviewTabViewModel)
        {
            if (overviewTabViewModel.Type == "process" || overviewTabViewModel.Type == "window")
            {
                (contextMenu.Items[0] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[1] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
            }
            else if (overviewTabViewModel.Type == "history")
            {
                (contextMenu.Items[1] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[3] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[4] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[5] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[6] as MenuItem).Visibility = Visibility.Collapsed;
            }
            else
            {
                overviewTabViewModel.PinOrUnpin(contextMenu.Items[1] as MenuItem);
                (contextMenu.Items[3] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[4] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[5] as MenuItem).Visibility = Visibility.Collapsed;
                (contextMenu.Items[6] as MenuItem).Visibility = Visibility.Collapsed;
            }
        }
    }

    private void FrequentSessions_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            FrequentSessions.SelectedIndex = 0;
            FrequentSessions.UpdateLayout();
            if (FrequentSessions.ItemContainerGenerator.ContainerFromIndex(FrequentSessions.SelectedIndex) is ListViewItem listViewItem)
            {
                listViewItem.Focus();
            }
        }
    }

    private void FrequentSessions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if(e.OriginalSource is not FrameworkElement)
        {
            return;
        }

        if(((FrameworkElement) e.OriginalSource).DataContext is not Session)
        {
            return;
        }

        ((OverviewTabViewModel)base.DataContext).OnDoubleClick(((FrameworkElement)e.OriginalSource).DataContext as Session);
    }
}
