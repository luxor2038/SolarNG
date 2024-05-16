using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;
using SolarNG.ViewModel.Settings;
using SolarNG.Sessions;

namespace SolarNG.UserControls.Settings;

public partial class SessionsList : UserControl
{
    public SessionsList()
    {
        InitializeComponent();
        base.DataContext = new SessionsListViewModel();
        base.IsVisibleChanged += This_IsVisibleChanged;
    }

    private void This_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            ListItemsView.ScrollIntoView(ListItemsView.SelectedItem);
            base.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)delegate
            {
                TxBoxSearch.Focus();
            });
        }
    }

    private void ListItemsView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if(e.OriginalSource is not FrameworkElement)
        {
            return;
        }

        if(((FrameworkElement) e.OriginalSource).DataContext is not Session)
        {
            return;
        }

        ((SessionsListViewModel)base.DataContext).OnDoubleClick(((FrameworkElement) e.OriginalSource).DataContext);
    }

    private void ListItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(e.RemovedItems.Count > 0 && e.RemovedItems[0] is not Session)
        {
            return;
        }

        if(e.AddedItems.Count > 0 && e.AddedItems[0] is not Session)
        {
            return;
        }

        if(ListItemsView.SelectedItems.Count == 1)
        {
            ListItemsView.ScrollIntoView(ListItemsView.SelectedItem);
        }

        List<Session> SelectedObjects = new List<Session>();

        foreach (Session item in ListItemsView.SelectedItems)
        {
            SelectedObjects.Add(item);
        }
        ((SessionsListViewModel)base.DataContext).SelectedObjects = SelectedObjects;
    }
}
