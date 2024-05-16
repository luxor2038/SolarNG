using System;
using System.Windows;
using System.Windows.Media.Imaging;
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

    public SettingsViewModel(MainWindow mainWindow) : base(mainWindow)
    {
        base.TabName = System.Windows.Application.Current.Resources["Settings"] as string;
        base.TabIcon = new BitmapImage(new Uri("/SolarNG;component/Images/gear.png", UriKind.Relative));
        base.TabIconVisibility = Visibility.Visible;
    }
}
