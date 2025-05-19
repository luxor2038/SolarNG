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

namespace SolarNG.ViewModel.Settings;

public class TagsListViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ObservableCollection<Session> AllSessions => App.Sessions.Sessions;
    private ObservableCollection<Session> AllSortedTags = new ObservableCollection<Session>();
    public ObservableCollection<Session> FilteredTags { get; set; } = new ObservableCollection<Session>();

    private MainWindow MainWindow => SettingsVM.MainWindow;

    private SettingsViewModel SettingsVM;

    private EditTagViewModel EditTagVM;

    public void Init(SettingsViewModel settingsVM, EditTagViewModel editTagVM)
    {
        EditTagVM = editTagVM;
        EditTagVM.TagsListVM = this;
        SettingsVM = settingsVM;

        UpdateTags(null, null);

        SelectedObject = FilteredTags.FirstOrDefault();

        AllSessions.CollectionChanged += UpdateTags;
        ListUpdate();
    }

    public void Update()
    {
        if(SettingsVM.SelectedSession != null && SettingsVM.SelectedSession.Type == "tag")
        {
            SelectedObject = SettingsVM.SelectedSession;
        }
        else
        {
            SelectedObject = FilteredTags.FirstOrDefault();
        }
    }

    public override void Cleanup()
    {
        AllSessions.CollectionChanged -= UpdateTags;
        EditTagVM.Cleanup();
        base.Cleanup();
    }

    public int GetCount()
    {
        return AllSortedTags.Count;
    }

    private void UpdateTags(object sender, EventArgs args)
    {
        AllSortedTags = new ObservableCollection<Session>(AllSessions.OrderBy((Session s) => s.Name).Where((Session s) => s.Type == "tag"));
        SettingsVM.UpdateTitle();
        FilterObjects();
    }

    public void ListUpdate()
    {
        NotifyPropertyChanged("FilteredTags");
    }

    public Brush ButtonPanelBackground { get; set; }

    public RelayCommand CreateNewItemCommand { get; set; }
    private void OnCreateNewItem()
    {
        UnSelectItem();
        EnableSaveButton();
        EditTagVM.CreateNewTag();
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
                ListView listView = (ListView)((object[])array)[0];
                if (listView.SelectedItems.Count > 0)
                {
                    int nextSelectedIndex = GetNextSelectedIndex(listView);

                    List<Guid> list = new List<Guid>();
                    foreach (object selectedItem in listView.SelectedItems)
                    {
                        list.Add(((Session)selectedItem).Id);
                    }
                    foreach (Guid item in list)
                    {
                        DeleteItem(item);
                    }

                    SelectedObject = FilteredTags.ElementAtOrDefault(nextSelectedIndex);
                }
            }
        };
        deleteConfirmationDialog.ShowDialog();
    }

    private void DeleteItem(Guid id)
    {
        Session tag = AllSessions.FirstOrDefault((Session s) => s.Id == id);

        foreach (Session session in tag.ChildSessions)
        {
            session.Tags.Remove(tag.Name);
            if(session.Tags.Count == 0)
            {
                session.Tags = null;
            }
        }

        foreach(Session tag2 in AllSessions.Where(s => s.ChildSessions.Contains(tag)))
        {
            tag2.ChildSessions.Remove(tag);
        }

        AllSessions.Remove(tag);
    }

    private int GetNextSelectedIndex(ListView listView)
    {
        int lastIndex = 0;
        foreach(Session selectedItem in listView.SelectedItems)
        {
            int i = FilteredTags.IndexOf(selectedItem);
            if(lastIndex < i)
            {
                lastIndex = i;
            }
        }

        if(lastIndex < (FilteredTags.Count - 1))
        {
            return (lastIndex - listView.SelectedItems.Count + 1);
        }

        return (FilteredTags.Count - listView.SelectedItems.Count - 1);
    }

    private bool CreatingNew;
    public Visibility SaveButtonVisible => ((SelectedObjects != null && SelectedObjects.Count > 0) || CreatingNew) ? Visibility.Visible : Visibility.Collapsed;
    public RelayCommand SaveItemCommand { get; set; }
    private void OnSaveItem()
    {
        EditTagVM.SaveCurrent();
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
        FilteredTags.Clear();

        foreach (Session session in AllSortedTags)
        {
            if (session.Matches(ByUserTypedName))
            {
                FilteredTags.Add(session);
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
                EditTagVM.ShowSelectedTags(_SelectedObjects);
                ButtonPanelBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
            }
            else
            {
                EditTagVM.HideControl();
                ButtonPanelBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
            }

            NotifyPropertyChanged("ButtonPanelBackground");
            NotifyPropertyChanged("DeleteButtonVisible");
            NotifyPropertyChanged("SaveButtonVisible");
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

    public TagsListViewModel()
    {
        CreateNewItemCommand = new RelayCommand(OnCreateNewItem);
        DeleteItemsCommand = new RelayCommand<object>(OnDeleteItems);
        SaveItemCommand = new RelayCommand(OnSaveItem);
    }


    public new event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(string Property)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));
    }
}
