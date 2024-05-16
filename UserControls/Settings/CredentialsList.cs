using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SolarNG.Sessions;
using SolarNG.ViewModel.Settings;

namespace SolarNG.UserControls.Settings;

public partial class CredentialsList : UserControl
{
    public CredentialsList()
    {
        InitializeComponent();
        base.DataContext = new CredentialsListViewModel();
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
        if(e.RemovedItems.Count > 0 && e.RemovedItems[0] is not Credential)
        {
            return;
        }

        if(e.AddedItems.Count > 0 && e.AddedItems[0] is not Credential)
        {
            return;
        }

        if(ListItemsView.SelectedItems.Count == 1)
        {
            ListItemsView.ScrollIntoView(ListItemsView.SelectedItem);
        }

        List<Credential> SelectedObjects = new List<Credential>();

        foreach (Credential item in ListItemsView.SelectedItems)
        {
            SelectedObjects.Add(item);
        }
        ((CredentialsListViewModel)base.DataContext).SelectedObjects = SelectedObjects;
    }
}
