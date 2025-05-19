using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ChromeTabs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using log4net;
using SolarNG.Sessions;
using SolarNG.Utilities;
using SolarNG.ViewModel.Settings;

namespace SolarNG.ViewModel;

public class MainWindowViewModel : ViewModelBase
{
    public bool IsWindowMerging;

    public MainWindow mainWindowInstance;

    public bool IsTabsFull { get; set; }

    private bool _CanMoveTabs;
    public bool CanMoveTabs
    {
        get
        {
            return _CanMoveTabs;
        }
        set
        {
            if (_CanMoveTabs != value)
            {
                Set(() => CanMoveTabs, ref _CanMoveTabs, value);
            }
        }
    }

    public ObservableCollection<TabBase> ItemCollection { get; set; }
    private void ItemCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && ItemCollection.Count > 1)
        {
            foreach (TabBase newItem in e.NewItems)
            {
                newItem.TabNumber = ItemCollection.OrderBy((TabBase x) => x.TabNumber).LastOrDefault().TabNumber + 1;
            }
        }
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems[0] is TabBase tabBase && !tabBase.Detaching)
        {
            tabBase.Cleanup();
        }
    }

    private TabBase _SelectedTab;
    public TabBase SelectedTab
    {
        get
        {
            return _SelectedTab;
        }
        set
        {
            if (_SelectedTab == value)
            {
                return;
            }
            _SelectedTab?.Deactivate();
            Set(() => SelectedTab, ref _SelectedTab, value);
            ActivateTab();
            UpdateTitle();
            if (_SelectedTab != null)
            {
                if (_SelectedTab.NeedDisableHotkey)
                {
                    App.hotKeys.HotKeysDisabled = true;
                }
                else
                {
                    App.hotKeys.HotKeysDisabled = !App.Config.GUI.Hotkey;
                }
            }
        }
    }

    public bool CanActivate { get; set; }
    public void ActivateTab()
    {
        if (CanActivate)
        {
            _SelectedTab?.Activate();
        }
    }

    public void UpdateTitle()
    {
        if (mainWindowInstance != null && SelectedTab != null)
        {
            mainWindowInstance.Title = SelectedTab.WindowTitle;
        }
    }

    private bool _ShowAddButton;
    public bool ShowAddButton
    {
        get
        {
            return _ShowAddButton;
        }
        set
        {
            if (_ShowAddButton != value)
            {
                Set(() => ShowAddButton, ref _ShowAddButton, value);
            }
        }
    }


    public TabBase LastClosedTab { get; set; }

    public RelayCommand AddTabCommand { get; set; }
    public void AddOverviewTab()
    {
        AddNewTab(CreateOverviewTab());
    }

    public RelayCommand<TabBase> CloseTabCommand { get; set; }
    public void CloseTabCommandAction(TabBase vm)
    {
        if (vm == null)
        {
            return;
        }
        if (!vm.CloseTab())
        {
            CanActivate = true;
            Task.Run(delegate
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    TabBase selectedTab = SelectedTab;
                    SelectedTab = null;
                    SelectedTab = selectedTab;
                });
            });
            return;
        }
        LastClosedTab = vm;
        CanActivate = true;
        ItemCollection.Remove(vm);
        if (ItemCollection.Count == 0)
        {
            AddOverviewTab();
        }
        else if (ItemCollection.Count == 1)
        {
            ActivateTab();
        }
    }

    public RelayCommand<TabBase> CloseTabNoKillCommand { get; set; }
    private void CloseTabNoKillCommandAction(TabBase vm)
    {
        if (vm != null)
        {
            CanActivate = true;
            ItemCollection.Remove(vm);
        }
    }

    public RelayCommand<TabReorder> ReorderTabsCommand { get; set; }
    internal virtual void ReorderTabsCommandAction(TabReorder reorder)
    {
        ICollectionView defaultView = CollectionViewSource.GetDefaultView(ItemCollection);
        int fromIndex = reorder.FromIndex;
        int toIndex = reorder.ToIndex;
        List<TabBase> list = defaultView.Cast<TabBase>().ToList();
        list[fromIndex].TabNumber = list[toIndex].TabNumber;
        if (toIndex > fromIndex)
        {
            for (int i = fromIndex + 1; i <= toIndex; i++)
            {
                list[i].TabNumber--;
            }
        }
        else if (fromIndex > toIndex)
        {
            for (int j = toIndex; j < fromIndex; j++)
            {
                list[j].TabNumber++;
            }
        }
        defaultView.Refresh();
        SelectedTab?.Activate();
    }

    public RelayCommand NewSessionCommand { get; set; }
    public void AddNewSessionTab()
    {
        AddNewSessionTab(null, null);
    }

    public void AddNewSessionTab(Session session, Credential credential)
    {
        OpenSettingsTab(session, credential, true);
    }

    public string CtrlE => ((!App.hotKeys.HotKeysDisabled) ? "Ctrl+E" : "");

    public RelayCommand AddOverviewTabCommand { get; set; }

    public string CtrlT => ((!App.hotKeys.HotKeysDisabled) ? "Ctrl+T" : "");

    public RelayCommand AddWindowCommand { get; set; }
    public void AddNewWindow()
    {
        mainWindowInstance.CreateNewMainWindow();
    }

    public string CtrlN => ((!App.hotKeys.HotKeysDisabled) ? "Ctrl+N" : "");

    public RelayCommand OpenHistoryTabCommand { get; set; }
    public void OpenHistoryTab()
    {
        TabBase tab = GetSpecialTab("history");
        if (tab == null)
        {
            AddNewTab(CreateOverviewTab("history"));
            return;
        }

        SelectTab(tab);
    }

    public OverviewTabViewModel GetSpecialTab(string type)
    {
        foreach (MainWindow mainWindow in App.mainWindows)
        {
            foreach (TabBase item in mainWindow.MainWindowVM.ItemCollection)
            {
                if (item is OverviewTabViewModel vm && vm.Type == type)
                {
                    return vm;
                }
            }
        }
        return null;
    }

    public string CtrlH => ((!App.hotKeys.HotKeysDisabled) ? "Ctrl+H" : "");

    public RelayCommand OpenShortcutTabCommand { get; set; }
    public void OpenShortcutTab()
    {
        TabBase tab = GetSpecialTab("shortcut");
        if (tab == null)
        {
            AddNewTab(CreateOverviewTab("shortcut"));
            return;
        }

        SelectTab(tab);
    }

    public string CtrlL => ((!App.hotKeys.HotKeysDisabled) ? "Ctrl+L" : "");

    public RelayCommand OpenProcessTabCommand { get; set; }
    public void OpenProcessTab()
    {
        TabBase tab = GetSpecialTab("process");
        if (tab == null)
        {
            AddNewTab(CreateOverviewTab("process"));
            return;
        }

        SelectTab(tab);
    }

    public bool NewProcessTabVisible
    {
        get
        {
            OverviewTabViewModel tab = GetSpecialTab("process");
            if(tab != null)
            {
                return true;
            }
            tab = GetSpecialTab("window");
            if(tab == null)
            {
                return true;
            }

            return false;
        }
    }

    public RelayCommand OpenWindowTabCommand { get; set; }
    public void OpenWindowTab()
    {
        TabBase tab = GetSpecialTab("window");
        if (tab == null)
        {
            AddNewTab(CreateOverviewTab("window"));
            return;
        }

        SelectTab(tab);
    }

    public bool NewWindowTabVisible
    {
        get
        {
            OverviewTabViewModel tab = GetSpecialTab("window");
            if(tab != null)
            {
                return true;
            }

            tab = GetSpecialTab("process");
            if(tab == null)
            {
                return true;
            }

            return false;
        }
    }

    public RelayCommand ImportModelCommand { get; set; }
    private void OnImportModel()
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog() { Filter = "*.json|*.json|*.*|*.*" };
        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        App.Sessions.Import(openFileDialog.FileName);
    }

    public RelayCommand ExportModelCommand { get; set; }
    private void OnExportModel()
    {
        if (App.Config.MasterPassword)
        {
            PromptDialog promptDialog;
            byte[] hash;
            do
            {
                promptDialog = new PromptDialog(null, Application.Current.Resources["InputMasterPassword"] as string, Application.Current.Resources["EnterMasterPassword"] as string, "", password: true) { Topmost = true };
                promptDialog.Focus();
                bool? flag = promptDialog.ShowDialog();
                if (!flag.HasValue || !flag.Value)
                {
                    return;
                }

                hash = Crypto.Argon2dHash(promptDialog.MyPassword.Password, App.passSalt);
            }
            while (!App.passHash.SequenceEqual(hash));
        }
        System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = "*.json|*.json",
            FileName = "SolarNG.json",
            CheckFileExists = false,
            AddExtension = true,
            OverwritePrompt = true
        };
        if (System.Windows.Forms.DialogResult.OK == saveFileDialog.ShowDialog())
        {
            App.Sessions.Export(saveFileDialog.FileName);
        }
    }

    public RelayCommand OpenSettingsCommand { get; set; }
    public void OnOpenSettings()
    {
        OpenSettingsTab(null, null, false);
    }

    public string CtrlS => ((!App.hotKeys.HotKeysDisabled) ? "Ctrl+S" : "");

    public RelayCommand AboutCommand { get; set; }
    private void OnAbout()
    {
        AboutWindow aboutWindow = new AboutWindow(mainWindowInstance) { Topmost = true };
        aboutWindow.Focus();
        aboutWindow.ShowDialog();
    }
   
    public string AltF4 => ((!App.hotKeys.HotKeysDisabled) ? "Alt+F4" : "");

    public MainWindowViewModel(MainWindow mainWindow)
    {
        mainWindowInstance = mainWindow;
        CanMoveTabs = true;
        ItemCollection = new ObservableCollection<TabBase>();
        ItemCollection.CollectionChanged += ItemCollection_CollectionChanged;
        CollectionViewSource.GetDefaultView(ItemCollection).SortDescriptions.Add(new SortDescription("TabNumber", ListSortDirection.Ascending));
        CanActivate = true;
        ShowAddButton = true;
        AddTabCommand = new RelayCommand(AddOverviewTab);
        CloseTabCommand = new RelayCommand<TabBase>(CloseTabCommandAction);
        CloseTabNoKillCommand = new RelayCommand<TabBase>(CloseTabNoKillCommandAction);
        ReorderTabsCommand = new RelayCommand<TabReorder>(ReorderTabsCommandAction);
        NewSessionCommand = new RelayCommand(AddNewSessionTab);
        AddOverviewTabCommand = new RelayCommand(AddOverviewTab);
        AddWindowCommand = new RelayCommand(AddNewWindow);
        OpenHistoryTabCommand = new RelayCommand(OpenHistoryTab);
        OpenShortcutTabCommand = new RelayCommand(OpenShortcutTab);
        OpenProcessTabCommand = new RelayCommand(OpenProcessTab);
        OpenWindowTabCommand = new RelayCommand(OpenWindowTab);
        ImportModelCommand = new RelayCommand(OnImportModel);
        ExportModelCommand = new RelayCommand(OnExportModel);
        OpenSettingsCommand = new RelayCommand(OnOpenSettings);
        AboutCommand = new RelayCommand(OnAbout);
    }

    public override void Cleanup()
    {
        ItemCollection.CollectionChanged -= ItemCollection_CollectionChanged;

        foreach (TabBase tab in ItemCollection)
        {
            tab.Cleanup();
        }

        base.Cleanup();
    }

    public void RemoveTab(TabBase tabBase)
    {
        ItemCollection.Remove(tabBase);
        if (ItemCollection.Count == 0)
        {
            AddNewTab(CreateOverviewTab());
        }
    }

    public void KickAllTab()
    {
        int num = 0;
        while (ItemCollection.Count > num)
        {
            if (ItemCollection[num] is AppTabViewModel appTab)
            {
                appTab.KickTab();
                RemoveTab(appTab);
            }
            else
            {
                num++;
            }
        }
    }

    public void RemoveTabWithExitedProcess(object sender, EventArgs args)
    {
        Process process = sender as Process;
        int num = 0;
        if (args != null)
        {
            try
            {
                num = process.ExitCode;
            }
            catch(Exception)
            {

            }
        }

        TabBase tabBase = ItemCollection.FirstOrDefault((TabBase t) => (t is AppTabViewModel) && (t as AppTabViewModel).AppProcessID == process.Id);
        if (tabBase == null)
        {
            return;
        }
        try
        {
            if (!process.HasExited || (num != 0 && num != -1073741510 && !tabBase.Closed && !tabBase.Killed))
            {
                return;
            }
            Application.Current?.Dispatcher.Invoke(delegate
            {
                RemoveTab(tabBase);
            });
        }
        catch (Exception exception)
        {
            log.Warn("Unable to remove tab with exited process", exception);
        }
    }

    public AppTabViewModel CreateAppTab(Session session, Credential credential, MainWindow mainWindow, string protocol = null)
    {
        return new AppTabViewModel(mainWindow, session, credential, protocol);
    }

    public OverviewTabViewModel CreateOverviewTab(string type = "")
    {
        return new OverviewTabViewModel(mainWindowInstance, type);
    }

    public void OpenSettingsTab(Session session, Credential credential, bool createNewSession)
    {
        SettingsViewModel settingsTab = GetSettingsTab();
        if (settingsTab != null)
        {
            if(session != null || createNewSession)
            {
                settingsTab.SetSession(session, credential, createNewSession);
            }
            SelectTab(settingsTab);
        }
        else
        {
            settingsTab = new SettingsViewModel(mainWindowInstance);
            settingsTab.SetSession(session, credential, createNewSession);
            AddNewTab(settingsTab);
        }
    }

    public SettingsViewModel GetSettingsTab()
    {
        foreach (MainWindow mainWindow in App.mainWindows)
        {
            if (mainWindow.MainWindowVM.ItemCollection.FirstOrDefault((TabBase i) => i is SettingsViewModel) is SettingsViewModel result)
            {
                return result;
            }
        }
        return null;
    }

    public void SelectTab(TabBase tab)
    {
        tab.MainWindow.MainWindowVM.SelectedTab = tab;
        if (tab.MainWindow != null && tab.MainWindow.Handle != IntPtr.Zero && Win32.GetForegroundWindow() != tab.MainWindow.Handle)
        {
            Win32.SetForegroundWindow(tab.MainWindow.Handle);
        }
    }

    public void SwitchTabs(TabBase newTab)
    {
        if (ItemCollection.Count > 0)
        {
            TabBase selectedTab = SelectedTab;
            int index = ItemCollection.IndexOf(selectedTab);
            newTab.TabNumber = selectedTab.TabNumber;
            ItemCollection[index] = newTab;
            SelectedTab = newTab;
            selectedTab.Cleanup();
            return;
        }

        AddNewTab(newTab);
    }

    public void AddNewTab(TabBase newTab)
    {
        if(IsTabsFull)
        {
            foreach (MainWindow mainWindow in App.mainWindows)
            {
                if(!mainWindow.MainWindowVM.IsTabsFull)
                {
                    mainWindow.MainWindowVM.AddNewTab(newTab);
                    return;
                }
            }

            mainWindowInstance.NewMainWindow().MainWindowVM.AddNewTab(newTab);
            return;
        }

        if(newTab.MainWindow != mainWindowInstance)
        {
            if(newTab is AppTabViewModel appTab)
            {
                if (appTab.GetAppProcessExited() != null)
                {
                    appTab.AppProcessExited -= appTab.MainWindow.MainWindowVM.RemoveTabWithExitedProcess;
                    appTab.AppProcessExited += mainWindowInstance.MainWindowVM.RemoveTabWithExitedProcess;
                }
            }
            newTab.MainWindow = mainWindowInstance;
            newTab.DetachCommand = new RelayCommand<TabBase>(newTab.MainWindow.DetachTabToNewWindow);
        }

        ItemCollection.Add(newTab);
        SelectTab(newTab);
    }

    public void Reconnect()
    {
        SelectedTab.ReconnectCommand?.Execute(null);
    }

    public void CloseSelectedTab()
    {
        CloseTabCommandAction(SelectedTab);
    }

    public void CloseAllTab()
    {
        foreach (TabBase item in ItemCollection.ToList())
        {
            CloseTabCommandAction(item);
        }
    }

    public void OpenPreviouslyCloseTab()
    {
        if (LastClosedTab != null)
        {
            if (LastClosedTab is not AppTabViewModel)
            {
                return;
            }

            AppTabViewModel appTab = LastClosedTab as AppTabViewModel;

            AddNewTab(CreateAppTab(appTab.Session, appTab.Credential, mainWindowInstance));

            LastClosedTab = null;
        }
    }

    public void SwitchToSpecifiedTab(HotKey hotKey)
    {
        int count = ItemCollection.Count;
        Key num = hotKey.Key - 34;
        int num2 = (int)(num - 1);
        if (num == Key.KanaMode)
        {
            num2 = count - 1;
        }
        if (num2 < count)
        {
            SelectedTab = ItemCollection.OrderBy((TabBase t) => t.TabNumber).ElementAt(num2);
        }
    }

    public void ShiftSelectedTabRight()
    {
        if (ItemCollection.Count > 1)
        {
            List<int> tabNumbers = (from t in ItemCollection
                orderby t.TabNumber
                select t.TabNumber).ToList();
            int nextTabNumberIndex = tabNumbers.IndexOf(SelectedTab.TabNumber) + 1;
            if (nextTabNumberIndex == tabNumbers.Count)
            {
                nextTabNumberIndex = 0;
            }
            SelectedTab = ItemCollection.First((TabBase t) => t.TabNumber == tabNumbers[nextTabNumberIndex]);
        }
    }

    public void ShiftSelectedTabLeft()
    {
        if (ItemCollection.Count > 1)
        {
            List<int> tabNumbers = (from t in ItemCollection
                orderby t.TabNumber
                select t.TabNumber).ToList();
            int previousTabNumberIndex = tabNumbers.IndexOf(SelectedTab.TabNumber) - 1;
            if (previousTabNumberIndex < 0)
            {
                previousTabNumberIndex = tabNumbers.Count - 1;
            }
            SelectedTab = ItemCollection.First((TabBase t) => t.TabNumber == tabNumbers[previousTabNumberIndex]);
        }
    }

    public void MoveSelectedTabLeft()
    {
        if (ItemCollection.Count > 1)
        {
            List<int> list = (from t in ItemCollection
                orderby t.TabNumber
                select t.TabNumber).ToList();
            int num = list.IndexOf(SelectedTab.TabNumber);
            int num2 = num - 1;
            if (num2 < 0)
            {
                num2 = list.Count - 1;
            }
            ReorderTabsCommandAction(new TabReorder(num, num2));
        }
    }

    public void MoveSelectedTabRight()
    {
        if (ItemCollection.Count > 1)
        {
            List<int> list = (from t in ItemCollection
                orderby t.TabNumber
                select t.TabNumber).ToList();
            int num = list.IndexOf(SelectedTab.TabNumber);
            int num2 = num + 1;
            if (num2 == list.Count)
            {
                num2 = 0;
            }
            ReorderTabsCommandAction(new TabReorder(num, num2));
        }
    }

    public void RegisterWindowResizeEvent()
    {
        if (mainWindowInstance != null && mainWindowInstance.Handle != IntPtr.Zero)
        {
            HwndSource.FromHwnd(mainWindowInstance.Handle)?.AddHook(HwndMessageHook);
        }
    }

    private const int WmEnterSizeMove = 561;
    private const int WmExitSizeMove = 562;
    private IntPtr HwndMessageHook(IntPtr wnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
        case WmExitSizeMove:
            CanActivate = true;
            ActivateTab();
            break;
        case WmEnterSizeMove:
            CanActivate = false;
            break;
        }
        return IntPtr.Zero;
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
