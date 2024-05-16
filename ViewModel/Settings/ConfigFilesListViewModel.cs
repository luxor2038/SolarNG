using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using log4net;
using SolarNG.Sessions;
using SolarNG.UserControls.Settings;

namespace SolarNG.ViewModel.Settings;

public class ConfigFilesListViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ObservableCollection<ConfigFile> AllConfigFiles => App.Sessions.ConfigFiles;
    public ObservableCollection<ConfigFile> FilteredConfigFiles { get; set; }

    private MainWindow MainWindow => SettingsVM.MainWindow;

    private SettingsViewModel SettingsVM;

    private EditConfigFileViewModel EditConfigFileVM;

    public void Init(SettingsViewModel settingsVM, EditConfigFileViewModel editConfigFileVM)
    {
        EditConfigFileVM = editConfigFileVM;
        EditConfigFileVM.ConfigFilesListVM = this;
        SettingsVM = settingsVM;

        FilteredConfigFiles = new ObservableCollection<ConfigFile>(AllConfigFiles.OrderBy((ConfigFile s) => s.Name));

        SelectedObject = FilteredConfigFiles.FirstOrDefault(); 

        AllConfigFiles.CollectionChanged += UpdateConfigFiles;
        ListUpdate();
    }

    public override void Cleanup()
    {
        AllConfigFiles.CollectionChanged -= UpdateConfigFiles;
        EditConfigFileVM.Cleanup();
        base.Cleanup();
    }

    private void UpdateConfigFiles(object sender, EventArgs args)
    {
        FilterObjects();
    }

    public void ListUpdate()
    {
        NotifyPropertyChanged("FilteredConfigFiles");
    }

    public Brush ButtonPanelBackground { get; set; }

    public RelayCommand CreateNewItemCommand { get; set; }
    private void OnCreateNewItem()
    {
        UnSelectItem();
        EnableSaveButton();
        EditConfigFileVM.CreateNewConfigFile();
    }

    public void EnableSaveButton()
    {
        CreatingNew = true;
        NotifyPropertyChanged("SaveButtonVisible");
    }

    public Visibility DeleteButtonVisible =>  (SelectedObjects != null && SelectedObjects.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
    public RelayCommand<object> DeleteItemsCommand { get; set; }
    private void OnDeleteItems(object array)
    {
        DeleteConfirmationDialog deleteConfirmationDialog = new DeleteConfirmationDialog(MainWindow, (Application.Current.Resources["DeleteItem"] as string) + Environment.NewLine + Environment.NewLine) { Topmost = true };
        deleteConfirmationDialog.Focus();
        deleteConfirmationDialog.Closing += delegate(object sender, CancelEventArgs e)
        {
            if ((sender as DeleteConfirmationDialog).Confirmed)
            {
                List<Guid> list = new List<Guid>();
                ListView listView = (ListView)((object[])array)[0];
                if (listView.SelectedItems.Count > 0)
                {
                    foreach (object selectedItem in listView.SelectedItems)
                    {
                        list.Add(((ConfigFile)selectedItem).Id);
                     }
                    foreach (Guid item in list)
                    {
                        DeleteItem(item);
                    }
                }
                SelectedObject = FilteredConfigFiles.FirstOrDefault();
            }
        };
        deleteConfirmationDialog.ShowDialog();
    }

    private void DeleteItem(Guid id)
    {
        ConfigFile configFile = AllConfigFiles.FirstOrDefault((ConfigFile c) => c.Id == id);
        DeleteForeignKey(configFile);
        AllConfigFiles.Remove(configFile);
    }

    private void DeleteForeignKey(ConfigFile configFile)
    {
        if(configFile.Type == "PrivateKey")
        {
            foreach (Credential credential in App.Sessions.Credentials.Where((Credential c) => c.PrivateKeyId == configFile.Id))
            {
                credential.PrivateKeyId = Guid.Empty;
                credential.Passphrase = null;
            }
        }
        else if(configFile.Type == "PuTTY")
        {
            foreach (Session session in App.Sessions.Sessions.Where((Session s) => s.PuTTYSessionId == configFile.Id))
            {
                session.PuTTYSessionId = Guid.Empty;
            }
        }
        else if(configFile.Type == "Script")
        {
            foreach (Session session in App.Sessions.Sessions.Where((Session s) => s.ScriptId == configFile.Id))
            {
                session.ScriptId = Guid.Empty;
            }
        }
        else if(configFile.Type == "RDP")
        {
            foreach (Session session in App.Sessions.Sessions.Where((Session s) => s.MSTSCId == configFile.Id))
            {
                session.MSTSCId = Guid.Empty;
            }
        }
        else if(configFile.Type == "WinSCP")
        {
            foreach (Session session in App.Sessions.Sessions.Where((Session s) => s.WinSCPId == configFile.Id))
            {
                session.WinSCPId = Guid.Empty;
            }
        }
    }

    private bool CreatingNew;
    public Visibility SaveButtonVisible => ((SelectedObjects != null && SelectedObjects.Count == 1) || CreatingNew) ? Visibility.Visible : Visibility.Collapsed;
    public RelayCommand SaveItemCommand { get; set; }
    private void OnSaveItem()
    {
        EditConfigFileVM.SaveCurrent();
    }

    public Visibility OpenTextVisible => (SelectedObjects != null && SelectedObjects.Count == 1) ? Visibility.Visible : Visibility.Collapsed;
    public RelayCommand OpenTextCommand { get; set; }
    private void OnOpenText()
    {
        ConfigFile configFile = SelectedObject;

        try
        {
            Process.Start(App.Config.Notepad.FullPath, "\"" + configFile.StagingPath + "\"");
        }
        catch (Exception ex)
        {
            log.Error("Unable to open the config file" + configFile.Path + " using path \"" + configFile.StagingPath + "\"!", ex);
        }
    }

    public Visibility OpenConfigFileVisible => (SelectedObjects != null && SelectedObjects.Count == 1 && SelectedObject != null && SelectedObject.Type == "RDP") ? Visibility.Visible : Visibility.Collapsed;
    
    public RelayCommand OpenConfigFileCommand { get; set; }
    private void OnOpenConfigFile()
    {
        ConfigFile configFile = SelectedObject;
        try
        {
            if (configFile.Type == "RDP")
            {
                Process.Start(App.Config.MSTSC.FullPath, "/edit \"" + configFile.StagingPath + "\"");
            }
        }
        catch (Exception ex)
        {
            log.Error("Unable to open the config file " + configFile.Name + " using path \"" + configFile.StagingPath + "\"!", ex);
        }
    }

    private string _ByUserTypedName;
    public string ByUserTypedName
    {
        get
        {
            return _ByUserTypedName;
        }
        set
        {
            _ByUserTypedName = value;
            FilterObjects();
            NotifyPropertyChanged("ByUserTypedName");
        }
    }

    private void FilterObjects()
    {
        FilteredConfigFiles.Clear();

        foreach (ConfigFile configFile in AllConfigFiles.OrderBy((ConfigFile c) => c.Name))
        {
            if (configFile.Matches(ByUserTypedName))
            {
                FilteredConfigFiles.Add(configFile);
            }
        }
        ListUpdate();
    }

    private List<ConfigFile> _SelectedObjects;
    public List<ConfigFile> SelectedObjects
    {
        get
        {
            return _SelectedObjects;
        }
        set
        {
            _SelectedObjects = value;
            if (_SelectedObjects != null && _SelectedObjects.Count > 0)
            {
                CreatingNew = false;
                EditConfigFileVM.ShowSelectedConfigFiles(_SelectedObjects);
                ButtonPanelBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
            }
            else
            {
                EditConfigFileVM.HideControl();
                ButtonPanelBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
            }

            NotifyPropertyChanged("ButtonPanelBackground");
            NotifyPropertyChanged("DeleteButtonVisible");
            NotifyPropertyChanged("SaveButtonVisible");
            NotifyPropertyChanged("OpenTextVisible");
            NotifyPropertyChanged("OpenConfigFileVisible");
            NotifyPropertyChanged("SelectedObjects");
            NotifyPropertyChanged("SelectedObject");
        }
    }

    private ConfigFile _SelectedObject;
    public ConfigFile SelectedObject
    {
        get
        {
            return _SelectedObject;
        }
        set
        {
            _SelectedObject = value;

            NotifyPropertyChanged("ButtonPanelBackground");
            NotifyPropertyChanged("DeleteButtonVisible");
            NotifyPropertyChanged("SaveButtonVisible");
            NotifyPropertyChanged("OpenTextVisible");
            NotifyPropertyChanged("OpenConfigFileVisible");
            NotifyPropertyChanged("SelectedObject");
        }
    }

    public void SelectItem(object item)
    {
        CreatingNew = false;
        SelectedObject = (ConfigFile)item;
    }

    public void UnSelectItem()
    {
        SelectItem(null);
    }

    public ConfigFilesListViewModel()
    {
        CreateNewItemCommand = new RelayCommand(OnCreateNewItem);
        DeleteItemsCommand = new RelayCommand<object>(OnDeleteItems);
        SaveItemCommand = new RelayCommand(OnSaveItem);
        OpenTextCommand = new RelayCommand(OnOpenText);
        OpenConfigFileCommand = new RelayCommand(OnOpenConfigFile);
    }


    public new event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(string Property)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
