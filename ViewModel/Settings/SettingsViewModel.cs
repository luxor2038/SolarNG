using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using SolarNG.Sessions;
using SolarNG.UserControls.Settings;

namespace SolarNG.ViewModel.Settings;

public class SettingsViewModel : TabBase
{
    public Session NewSession;

    public Credential NewCredential;

    public bool CreateNewSession;

    public Session SelectedSession;

    private SettingsGeneralLayout SettingsGeneralLayout;

    public void KickAll()
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            base.MainWindow.MainWindowVM.KickAllTab();
        });
    }

    public void SetSettingsGeneralLayout(SettingsGeneralLayout settingsGeneralLayout)
    {
        SettingsGeneralLayout = settingsGeneralLayout;
    }

    public void SetSession(Session session, Credential credential, bool createNewSession)
    {
        CreateNewSession = createNewSession;
        if(CreateNewSession)
        {
            NewSession = session ?? new Session("ssh");
            NewCredential = credential ?? new Credential();
        }
        else
        {
            SelectedSession = session;
        }

        SettingsGeneralLayout?.SetTab(this);
    }

    private void UpdateTitle(int count)
    {
        base.TabName = (string)Application.Current.Resources["Settings"] + ((count>=0)?" (" + count + ")":"");
    }

    public void UpdateTitle()
    {
        int count = -1;

        if(SettingsGeneralLayout != null)
        {
            count = SettingsGeneralLayout.GetCount();
        }

        UpdateTitle(count);
    }

    public SettingsViewModel(MainWindow mainWindow) : base(mainWindow)
    {
        UpdateTitle(-1);
        base.TabPath = Application.Current.Resources["SettingsPath"] as Geometry;
        base.TabPathVisibility = Visibility.Visible;

        KickAllCommand = new RelayCommand(KickAll);
    }
}
