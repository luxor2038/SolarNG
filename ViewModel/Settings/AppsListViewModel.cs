using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SolarNG.Sessions;
using SolarNG.Utilities;

namespace SolarNG.ViewModel.Settings;

public class AppsListViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ObservableCollection<Session> AllSessions => App.Sessions.Sessions;
    public ObservableCollection<Session> FilteredApplications { get; set; }

    private MainWindow MainWindow => SettingsVM.MainWindow;

    private SettingsViewModel SettingsVM;

    private EditAppViewModel EditAppVM;

    public void Init(SettingsViewModel settingsVM, EditAppViewModel editAppVM)
    {
        EditAppVM = editAppVM;
        EditAppVM.AppsListVM = this;
        SettingsVM = settingsVM;

        FilteredApplications = new ObservableCollection<Session>(from s in AllSessions
                                                                orderby s.Name
                                                                where s.Type == "app"
                                                                select s);

        SelectedObject = FilteredApplications.FirstOrDefault();

        AllSessions.CollectionChanged += UpdateApplications;
        ListUpdate();
    }

    public void Update()
    {
        if(SettingsVM.CreateNewSession && SettingsVM.NewSession.Type == "app")
        {
            CreateNewItem(SettingsVM.NewSession);
        }
        else if(SettingsVM.SelectedSession != null && SettingsVM.SelectedSession.Type == "app")
        {
            SelectedObject = SettingsVM.SelectedSession;
        }
        else
        {
            SelectedObject = FilteredApplications.FirstOrDefault();
        }
    }

    public override void Cleanup()
    {
        AllSessions.CollectionChanged -= UpdateApplications;
        EditAppVM.Cleanup();
        base.Cleanup();
    }

    private void UpdateApplications(object sender, EventArgs args)
    {
        FilterObjects();
    }

    public void ListUpdate()
    {
        NotifyPropertyChanged("FilteredApplications");
    }

    public Brush ButtonPanelBackground { get; set; }

    public RelayCommand CreateNewItemCommand { get; set; }
    private void OnCreateNewItem()
    {
        CreateNewItem(new Session("app"));
    }

    private void CreateNewItem(Session session)
    {
        UnSelectItem();
        EnableSaveButton();
        EditAppVM.CreateNewApp(session);
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
                        list.Add(((Session)selectedItem).Id);
                     }
                    foreach (Guid item in list)
                    {
                        DeleteItem(item);
                    }
                }
                JumpListManager.SetNewJumpList(AllSessions);
                SelectedObject = FilteredApplications.FirstOrDefault(); 
            }
        };
        deleteConfirmationDialog.ShowDialog();
    }

    private void DeleteItem(Guid id)
    {
        Session session = AllSessions.FirstOrDefault((Session s) => s.Id == id);

        foreach(Session tag in AllSessions.Where(s => s.ChildSessions.Contains(session)))
        {
            tag.ChildSessions.Remove(session);
        }

        if(session.SessionHistory != null)
        {
            App.HistorySessions.Remove(session.SessionHistory);
        }
        AllSessions.Remove(session);
    }

    private bool CreatingNew;
    public Visibility SaveButtonVisible => ((SelectedObjects != null && SelectedObjects.Count > 0) || CreatingNew) ? Visibility.Visible : Visibility.Collapsed;
    public RelayCommand SaveItemCommand { get; set; }
    private void OnSaveItem()
    {
        EditAppVM.SaveCurrent();
    }

    public Visibility SaveNewButtonVisible => (SelectedObjects != null && SelectedObjects.Count == 1) ? Visibility.Visible : Visibility.Collapsed;

    public RelayCommand SaveNewItemCommand { get; set; }

    private void OnSaveNewItem()
    {
        EditAppVM.SaveNewCurrent();
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
        FilteredApplications.Clear();

        foreach (Session session in AllSessions.OrderBy((Session s) => s.Name).Where((Session s) => s.Type == "app"))
        {
            if (session.Matches(ByUserTypedName))
            {
                FilteredApplications.Add(session);
            }
        }
        ListUpdate();
    }

    private List<Session> _SelectedObjects;
    public List<Session> SelectedObjects
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
                EditAppVM.ShowSelectedApps(_SelectedObjects);
                ButtonPanelBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
            }
            else
            {
                EditAppVM.HideControl();
                ButtonPanelBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
            }

            NotifyPropertyChanged("ButtonPanelBackground");
            NotifyPropertyChanged("DeleteButtonVisible");
            NotifyPropertyChanged("SaveButtonVisible");
            NotifyPropertyChanged("SaveNewButtonVisible");
            NotifyPropertyChanged("SelectedObjects");
            NotifyPropertyChanged("SelectedObject");
        }
    }

    private Session _SelectedObject;
    public Session SelectedObject
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
            NotifyPropertyChanged("SaveNewButtonVisible");
            NotifyPropertyChanged("SelectedObject");
        }
    }

    public void SelectItem(object item)
    {
        CreatingNew = false;
        SelectedObject = (Session)item;
    }

    public void UnSelectItem()
    {
        SelectItem(null);
    }

    public void OnDoubleClick(object selectedItem)
    {
        if (selectedItem != null && selectedItem is Session)
        {
            Session session = selectedItem as Session;
            MainWindow.MainWindowVM.AddNewTab(MainWindow.MainWindowVM.CreateAppTab(session, null, MainWindow));
        }
    }

    public AppsListViewModel()
    {
        CreateNewItemCommand = new RelayCommand(OnCreateNewItem);
        DeleteItemsCommand = new RelayCommand<object>(OnDeleteItems);
        SaveItemCommand = new RelayCommand(OnSaveItem);
        SaveNewItemCommand = new RelayCommand(OnSaveNewItem);
    }


    public new event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(string Property)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));
    }
}
