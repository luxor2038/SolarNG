using System.Windows;
using System.Windows.Controls;
using SolarNG.ViewModel;
using SolarNG.ViewModel.Settings;

namespace SolarNG.UserControls.Settings;

public partial class SettingsGeneralLayout : UserControl
{
    public SettingsGeneralLayout()
    {
        InitializeComponent();
        base.Loaded += TabLoaded;
        base.Unloaded += TabUnloaded;
    }

    private void TabLoaded(object sender, RoutedEventArgs args)
    {
        SettingsViewModel SettingsVM = (SettingsViewModel)base.DataContext;

        ((SessionsListViewModel)SessionsList.DataContext).Init(SettingsVM, (EditSessionViewModel)EditSessionControl.DataContext);
        ((CredentialsListViewModel)CredentialsList.DataContext).Init(SettingsVM, (EditCredentialViewModel)EditCredentialControl.DataContext);
        ((ConfigFilesListViewModel)ConfigFilesList.DataContext).Init(SettingsVM, (EditConfigFileViewModel)EditConfigFileControl.DataContext);
        ((ProxiesListViewModel)ProxiesList.DataContext).Init(SettingsVM, (EditProxyViewModel)EditProxyControl.DataContext);
        ((TagsListViewModel)TagsList.DataContext).Init(SettingsVM, (EditTagViewModel)EditTagControl.DataContext);
        ((AppsListViewModel)AppsList.DataContext).Init(SettingsVM, (EditAppViewModel)EditAppControl.DataContext);

        SettingsVM.SetSettingsGeneralLayout(this);
        SetTab(SettingsVM);
    }

    public void SetTab(SettingsViewModel SettingsVM)
    {
        MainTabControl.SelectedItem = TabMisc;

        if (SettingsVM.CreateNewSession)
        {
            if(SettingsVM.NewSession.Type == "app")
            {
                ((AppsListViewModel)AppsList.DataContext).Update();
                MainTabControl.SelectedItem = TabApps;
                return;
            }
        }
        else if(SettingsVM.SelectedSession != null)
        {
            if(SettingsVM.SelectedSession.Type == "proxy")
            {
                ((ProxiesListViewModel)ProxiesList.DataContext).Update();
                MainTabControl.SelectedItem = TabProxies;
                return;
            }

            if(SettingsVM.SelectedSession.Type == "tag")
            {
                ((TagsListViewModel)TagsList.DataContext).Update();
                MainTabControl.SelectedItem = TabTags;
                return;
            }
            
            if(SettingsVM.SelectedSession.Type == "app")
            {
                ((AppsListViewModel)AppsList.DataContext).Update();
                MainTabControl.SelectedItem = TabApps;
                return;
            }
        }
        ((SessionsListViewModel)SessionsList.DataContext).Update();
        MainTabControl.SelectedItem = TabSessions;
    }

    private void TabUnloaded(object sender, RoutedEventArgs args)
    {
        ((SessionsListViewModel)SessionsList.DataContext).Cleanup();
        ((CredentialsListViewModel)CredentialsList.DataContext).Cleanup();
        ((ConfigFilesListViewModel)ConfigFilesList.DataContext).Cleanup();
        ((ProxiesListViewModel)ProxiesList.DataContext).Cleanup();
        ((TagsListViewModel)TagsList.DataContext).Cleanup();
        ((AppsListViewModel)AppsList.DataContext).Cleanup();
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource is not TabControl tabControl)
        {
            return;
        }
        TabBase tabBase = (DataContext is not SettingsViewModel settingsViewModel) ? null : settingsViewModel.MainWindow?.MainWindowVM?.SelectedTab;
        if (tabBase != null)
        {
            tabBase.CanActivate = true;
        }
        else if (tabControl.SelectedItem == TabMisc)
        {
            if (tabBase != null)
            {
                tabBase.CanActivate = false;
            }
        }
    }
}
