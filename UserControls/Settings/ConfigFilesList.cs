using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SolarNG.Sessions;
using SolarNG.ViewModel.Settings;

namespace SolarNG.UserControls.Settings;

public partial class ConfigFilesList : UserControl
{
    public ConfigFilesList()
    {
        InitializeComponent();
        base.DataContext = new ConfigFilesListViewModel();
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

    private void ListItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(e.RemovedItems.Count > 0 && e.RemovedItems[0] is not ConfigFile)
        {
            return;
        }

        if(e.AddedItems.Count > 0 && e.AddedItems[0] is not ConfigFile)
        {
            return;
        }

        if(ListItemsView.SelectedItems.Count == 1)
        {
            ListItemsView.ScrollIntoView(ListItemsView.SelectedItem);
        }

        List<ConfigFile> SelectedObjects = new List<ConfigFile>();

        foreach (ConfigFile item in ListItemsView.SelectedItems)
        {
            SelectedObjects.Add(item);
        }
        ((ConfigFilesListViewModel)base.DataContext).SelectedObjects = SelectedObjects;
    }
}
