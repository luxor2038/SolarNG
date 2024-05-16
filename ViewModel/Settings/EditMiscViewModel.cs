using System.ComponentModel;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SolarNG.Sessions;
using SolarNG.Utilities;

namespace SolarNG.ViewModel.Settings;

public class EditMiscViewModel : ViewModelBase, INotifyPropertyChanged
{
    public bool ShortcutsDisabled
    {
        get
        {
            return !App.Config.GUI.Hotkey;
        }
        set
        {
            App.Config.GUI.Hotkey = !value;
            NotifyPropertyChanged("ShortcutsDisabled");
        }
    }

    public RelayCommand SaveCommand { get; set; }
    private void OnSaveSession()
    {
        App.Sessions.Save(App.DataFilePath);
        App.Histories.Save(App.DataFilePath);
    }

    public EditMiscViewModel()
    {
        SaveCommand = new RelayCommand(OnSaveSession);        
    }

    public new event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(string Property)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));
    }
}
