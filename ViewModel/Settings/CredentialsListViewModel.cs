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

public class CredentialsListViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ObservableCollection<Credential> AllCredentials => App.Sessions.Credentials;
    public ObservableCollection<Credential> FilteredCredentials { get; set; }

    private MainWindow MainWindow => SettingsVM.MainWindow;

    private SettingsViewModel SettingsVM;

    private EditCredentialViewModel EditCredentialVM;

    public void Init(SettingsViewModel settingsVM, EditCredentialViewModel editCredentialVM)
    {
        EditCredentialVM = editCredentialVM;
        EditCredentialVM.CredentialsListVM = this;
        SettingsVM = settingsVM;

        FilteredCredentials = new ObservableCollection<Credential>(AllCredentials.OrderBy((Credential s) => s.Name));

        SelectedObject = FilteredCredentials.FirstOrDefault(); 

        AllCredentials.CollectionChanged += UpdateCredentials;
        ListUpdate();
    }

    public override void Cleanup()
    {
        AllCredentials.CollectionChanged -= UpdateCredentials;
        EditCredentialVM.Cleanup();
        base.Cleanup();
    }

    private void UpdateCredentials(object sender, EventArgs args)
    {
        FilterObjects();
    }

    public void ListUpdate()
    {
        NotifyPropertyChanged("FilteredCredentials");
    }

    public Brush ButtonPanelBackground { get; set; }

    public RelayCommand CreateNewItemCommand { get; set; }
    private void OnCreateNewItem()
    {
        UnSelectItem();
        EnableSaveButton();
        EditCredentialVM.CreateNewCredential();
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
                        list.Add(((Credential)selectedItem).Id);
                     }
                    foreach (Guid item in list)
                    {
                        DeleteItem(item);
                    }
                }
                SelectedObject = FilteredCredentials.FirstOrDefault();
            }
        };
        deleteConfirmationDialog.ShowDialog();
    }

    private void DeleteItem(Guid id)
    {
        DeleteForeignKey(id);
        AllCredentials.Remove(AllCredentials.FirstOrDefault((Credential s) => s.Id == id));
    }

    private void DeleteForeignKey(Guid id)
    {
        foreach (Session session in App.Sessions.Sessions.Where((Session s) => s.CredentialId == id))
        {
            session.CredentialId = Guid.Empty;
        }
    }

    private bool CreatingNew;
    public Visibility SaveButtonVisible => ((SelectedObjects != null && SelectedObjects.Count > 0) || CreatingNew) ? Visibility.Visible : Visibility.Collapsed;
    public RelayCommand SaveItemCommand { get; set; }
    private void OnSaveItem()
    {
        EditCredentialVM.SaveCurrent();
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
        FilteredCredentials.Clear();

        foreach (Credential credential in AllCredentials.OrderBy((Credential c) => c.Name))
        {
            if (credential.Matches(ByUserTypedName))
            {
                FilteredCredentials.Add(credential);
            }
        }
        ListUpdate();
    }

    private List<Credential> _SelectedObjects;
    public List<Credential> SelectedObjects
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
                EditCredentialVM.ShowSelectedCredentials(_SelectedObjects);
                ButtonPanelBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
            }
            else
            {
                EditCredentialVM.HideControl();
                ButtonPanelBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
            }

            NotifyPropertyChanged("ButtonPanelBackground");
            NotifyPropertyChanged("DeleteButtonVisible");
            NotifyPropertyChanged("SaveButtonVisible");
            NotifyPropertyChanged("SelectedObjects");
            NotifyPropertyChanged("SelectedObject");
        }
    }

    private Credential _SelectedObject;
    public Credential SelectedObject
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
        SelectedObject = (Credential)item;
    }

    public void UnSelectItem()
    {
        SelectItem(null);
    }

    public CredentialsListViewModel()
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
