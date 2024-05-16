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

public class SessionsListViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ObservableCollection<Session> AllSessions => App.Sessions.Sessions;
    public ObservableCollection<Session> FilteredSessions { get; set; }

    private MainWindow MainWindow => SettingsVM.MainWindow;

    private SettingsViewModel SettingsVM;

    private EditSessionViewModel EditSessionVM;

    public void Init(SettingsViewModel settingsVM, EditSessionViewModel editSessionVM)
    {
        EditSessionVM = editSessionVM;
        EditSessionVM.SessionsListVM = this;
        SettingsVM = settingsVM;

        FilteredSessions = new ObservableCollection<Session>(from s in AllSessions
                                                            orderby s.Name
                                                            where s.SessionTypeIsNormal
                                                            select s);

        SelectedObject = FilteredSessions.FirstOrDefault();

        AllSessions.CollectionChanged += UpdateSessions;
        ListUpdate();
    }

    public void Update()
    {
        if(SettingsVM.CreateNewSession && SettingsVM.NewSession.SessionTypeIsNormal)
        {
            CreateNewItem(SettingsVM.NewSession, SettingsVM.NewCredential);
        }
        else if(SettingsVM.SelectedSession != null && SettingsVM.SelectedSession.SessionTypeIsNormal)
        {
            SelectedObject = SettingsVM.SelectedSession;
        }
        else
        {
            SelectedObject = FilteredSessions.FirstOrDefault();
        }
    }

    public override void Cleanup()
    {
        AllSessions.CollectionChanged -= UpdateSessions;
        EditSessionVM.Cleanup();
        base.Cleanup();
    }

    private void UpdateSessions(object sender, EventArgs args)
    {
        FilterObjects();
    }

    public void ListUpdate()
    {
        NotifyPropertyChanged("FilteredSessions");
    }

    public Brush ButtonPanelBackground { get; set; }

    public RelayCommand CreateNewItemCommand { get; set; }
    private void OnCreateNewItem()
    {
        CreateNewItem(new Session("ssh"), new Credential());
    }

    private void CreateNewItem(Session session, Credential credential)
    {
        UnSelectItem();
        EnableSaveButton();
        EditSessionVM.CreateNewSession(session, credential);
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
                SelectedObject = FilteredSessions.FirstOrDefault(); 
            }
        };
        deleteConfirmationDialog.ShowDialog();
    }

    private void DeleteItem(Guid id)
    {
        Session session = AllSessions.FirstOrDefault((Session s) => s.Id == id);

        if((session.SessionTypeFlags & SessionType.FLAG_PROXY_PROVIDER)!=0)
        {
            foreach(Session item in AllSessions.Where(s => s.ProxyId == id))
            {
                item.ProxyId = Guid.Empty;
            }
        }

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
        EditSessionVM.SaveCurrent();
    }

    public Visibility SaveNewButtonVisible => (SelectedObjects != null && SelectedObjects.Count == 1) ? Visibility.Visible : Visibility.Collapsed;
    public RelayCommand SaveNewItemCommand { get; set; }
    private void OnSaveNewItem()
    {
        EditSessionVM.SaveNewCurrent();
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
        FilteredSessions.Clear();

        foreach (Session session in AllSessions.OrderBy((Session s) => s.Name).Where((Session s) => s.SessionTypeIsNormal))
        {
            if (session.Matches(ByUserTypedName))
            {
                FilteredSessions.Add(session);
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
            if (SelectedObjects != null && SelectedObjects.Count > 0)
            {
                CreatingNew = false;
                EditSessionVM.ShowSelectedSessions(SelectedObjects);
                ButtonPanelBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
            }
            else
            {
                EditSessionVM.HideControl();
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
            MainWindow.MainWindowVM.AddNewTab(MainWindow.MainWindowVM.CreateAppTab(session, session.Credential, MainWindow));
        }
    }

    public SessionsListViewModel()
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
