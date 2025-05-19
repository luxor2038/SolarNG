using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using log4net;
using SolarNG.Configs;
using SolarNG.Sessions;
using SolarNG.Utilities;

namespace SolarNG.ViewModel.Settings;

public class EditSessionViewModel : ViewModelBase, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public SessionsListViewModel SessionsListVM;


    public Brush TitleBackground { get; set; }

    public bool BatchMode { get; set; }

    public bool NewMode { get; set; }

    public bool EditMode => !BatchMode && !NewMode;

    public bool ControlVisible { get; set; }

    public Session EditedSession { get; set; } = new Session("ssh");
    public Credential EditedCredential { get; set; } = new Credential();

    public string Name
    {
        get
        {
            return EditedSession.Name;
        }
        set
        {
            EditedSession.Name = value;
            NotifyPropertyChanged("Name");
        }
    }

    public string _SessionType
    {
        get
        {
            return EditedSession.Type;
        }
        set
        {
            if(value == null)
            {
                return;
            }

            if (App.Sessions.SessionTypesDict[EditedSession.Type].Port == Port)
            {
                Port = App.Sessions.SessionTypesDict[value].Port;
            }

            EditedSession.Type = value;

            iFlags = EditedSession.SessionType.Program.iFlags | (iFlags & (ProgramConfig.FLAG_NOTINTAB | ProgramConfig.FLAG_PINNED | ProgramConfig.FLAG_SYNCTITLE | ProgramConfig.FLAG_ENABLEHOTKEY | ProgramConfig.FLAG_SSHV2SHARE));

            PasswordOnlyCheck = VNCSelected;

            NotifyPropertyChanged("_SessionType");
            NotifyPropertyChanged("PrivateKeyValid");
            NotifyPropertyChanged("ProxyValid");
            NotifyPropertyChanged("UsePuTTY");
            NotifyPropertyChanged("UseMSTSC");
            NotifyPropertyChanged("UseWinSCP");
            NotifyPropertyChanged("VNCSelected");
            NotifyPropertyChanged("SSHv2ShareValid");
            NotifyPropertyChanged("PasswordOnlyValid");
            NotifyPropertyChanged("UseScriptCheck");
            NotifyPropertyChanged("StartRemoteAppCheck");
            NotifyPropertyChanged("RemoteAppValid");
            NotifyPropertyChanged("RemoteAppNotValid");
            NotifyPropertyChanged("StartShellValid");
            NotifyPropertyChanged("ShellWorkingDirValid");
            NotifyPropertyChanged("RemoteAppNotValid");
            NotifyPropertyChanged("WindowsKeyCombinationsCheck");
            NotifyPropertyChanged("MultiMonitorsValid");
            NotifyPropertyChanged("WidthHeightValid");
            NotifyPropertyChanged("MonitorValid");
        }
    }

    private ObservableCollection<ComboBoxTwo> _SessionTypeList;
    public ObservableCollection<ComboBoxTwo> SessionTypeList
    {
        get
        {
            return _SessionTypeList;
        }
        set
        {
            _SessionTypeList = value;
            NotifyPropertyChanged("SessionTypeList");
        }
    }

    private void CreateSessionTypeList()
    {
        SessionTypeList = new ObservableCollection<ComboBoxTwo>();

        foreach(SessionType type in App.Sessions.SessionTypes) 
        { 
            if((type.iFlags & SessionType.FLAG_SPECIAL_TYPE)==0)
            {
                ComboBoxTwo t = new ComboBoxTwo(type.Name, type.DisplayName + (File.Exists(type.Program.FullPath)?"":" " + Application.Current.Resources["NA"] as string));
                SessionTypeList.Add(t);
            }
        }
    }

    private bool _UsePuTTY = false;
    public bool UsePuTTY => BatchMode ? _UsePuTTY : EditedSession.SessionType.ProgramName == "PuTTY";

    private bool _UseMSTSC = false;
    public bool UseMSTSC => BatchMode ? _UseMSTSC : EditedSession.SessionType.ProgramName == "MSTSC";

    private bool _UseWinSCP = false;
    public bool UseWinSCP => BatchMode ? _UseWinSCP : EditedSession.SessionType.ProgramName == "WinSCP";

    private bool _VNCSelected = false;
    public bool VNCSelected => BatchMode ? _VNCSelected : _SessionType == "vnc";

    public string Ip
    {
        get
        {
            return EditedSession.Ip;
        }
        set
        {
            EditedSession.Ip = value;
            NotifyPropertyChanged("Ip");
        }
    }

    [Range(-1, 65535, ErrorMessage = "0-65535")]
    public int Port
    {
        get
        {
            return EditedSession.Port;
        }
        set
        {
            EditedSession.Port = value;
            NotifyPropertyChanged("Port");
            ValidateProperty("Port", value);
        }
    }

    private Guid NoChangeId = Guid.NewGuid();

    public Guid CredentialId
    {
        get
        {
            return EditedSession.CredentialId;
        }
        set
        {
            EditedSession.CredentialId = value;
            NotifyPropertyChanged("CredentialId");
        }
    }

    private ObservableCollection<ComboBoxGuid> _CredentialList;
    public ObservableCollection<ComboBoxGuid> CredentialList
    {
        get
        {
            return _CredentialList;
        }
        set
        {
            _CredentialList = value;
            NotifyPropertyChanged("CredentialList");
        }
    }

    private void UpdateCredentialList(object sender, EventArgs args)
    {
        CredentialList = new ObservableCollection<ComboBoxGuid>
        {
            new ComboBoxGuid(Guid.Empty, Application.Current.Resources["CreateCredential_"] as string)
        };

        if(EditedSession.CredentialId == NoChangeId)
        {
            CredentialList.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
        }

        foreach (Credential item in App.Sessions.Credentials.OrderBy((Credential x) => x.Name))
        {
            CredentialList.Add(new ComboBoxGuid(item.Id, item.Name));
        }
        NotifyPropertyChanged("CredentialList");
        NotifyPropertyChanged("CredentialId");
    }


    public string Username
    {
        get
        {
            if (PasswordOnlyCheck == true && !SafeString.IsNullOrEmpty(EditedCredential.Password))
            {
                EditedCredential.Username = _SessionType;
            }
            return EditedCredential.Username;
        }
        set
        {
            EditedCredential.Username = value;
            NotifyPropertyChanged("Username");
        }
    }

    public SecureString Password
    {
        get
        {
            return EditedCredential.Password?.ToSecureString();
        }
        set
        {
            EditedCredential.Password = new SafeString(value);
            NotifyPropertyChanged("Password");
        }
    }

    public SecureString Passphrase
    {
        get
        {
            return EditedCredential.Passphrase?.ToSecureString();
        }
        set
        {
            EditedCredential.Passphrase = new SafeString(value);
            NotifyPropertyChanged("Passphrase");
        }
    }

    private bool _PrivateKeyValid;
    public bool PrivateKeyValid => BatchMode ? _PrivateKeyValid : (_SessionType == "ssh" || _SessionType == "scp" || _SessionType == "sftp");

    public Guid PrivateKeyId
    {
        get
        {
            return EditedCredential.PrivateKeyId;
        }
        set
        {
            EditedCredential.PrivateKeyId = value;
            NotifyPropertyChanged("PrivateKeyId");
        }
    }

    public ObservableCollection<ComboBoxGuid> _PrivateKeys;
    public ObservableCollection<ComboBoxGuid> PrivateKeys 
    {
        get
        {
            return _PrivateKeys;
        }
        set
        {
            _PrivateKeys = value;
            NotifyPropertyChanged("PrivateKeys");
        }
    }

    public RelayCommand ImportPrivateKeyCommand { get; set; }
    private void OnImportPrivateKey()
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog() { Filter = "*.ppk|*.ppk|*.*|*.*" };
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            ConfigFile configFile = new ConfigFile("PrivateKey") { RealPath = openFileDialog.FileName };

            int num = 2;
            string text;
            string text2 = (text = Path.GetFileName(configFile.Path));
            while (App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile c) => c.Name == text) != null)
            {
                text = text2 + " (" + num + ")";
                num++;
            }
            configFile.Name = text;
            App.Sessions.ConfigFiles.Add(configFile);
            PrivateKeyId = configFile.Id;
            NotifyPropertyChanged("PrivateKeys");
        }
    }

    public string CredentialName
    {
        get
        {
            return EditedCredential.Name;
        }
        set
        {
            EditedCredential.Name = value;
            NotifyPropertyChanged("CredentialName");
        }
    }

    private bool _ProxyValid;
    public bool ProxyValid => BatchMode ? _ProxyValid : (EditedSession.SessionTypeFlags & SessionType.FLAG_PROXY_CONSUMER) != 0;

    public Guid ProxyId
    {
        get
        {
            if(ProxyValid) 
            { 
                return EditedSession.ProxyId;
            }

            return Guid.Empty;
        }
        set
        {
            EditedSession.ProxyId = value;
            NotifyPropertyChanged("ProxyId");
        }
    }

    public ObservableCollection<ComboBoxGuid> _ProxiesList;
    public ObservableCollection<ComboBoxGuid> ProxiesList
    {
        get
        {
            return _ProxiesList;
        }
        set
        {
            _ProxiesList = value;
            NotifyPropertyChanged("ProxiesList");
        }
    }

    private void CreateProxiesList(bool AddNoChange=false)
    {
        ProxiesList = new ObservableCollection<ComboBoxGuid>
        {
            new ComboBoxGuid(Guid.Empty, Application.Current.Resources["ChooseProxy"] as string)
        };

        if(AddNoChange)
        {
            ProxiesList.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
        }

        foreach (Session proxy in from s in App.Sessions.Sessions where (s.SessionTypeFlags & SessionType.FLAG_PROXY_PROVIDER) != 0 && (s.SessionTypeFlags & SessionType.FLAG_SSH_PROXY) == 0 && s.ProxyType != "ssh"  orderby s.Name select s)
        {
            ProxiesList.Add(new ComboBoxGuid(proxy.Id, "[" + proxy.ProxyType + "] " + proxy.Name));
        }

        foreach (Session proxy in from s in App.Sessions.Sessions where s.Type == "ssh" && s.CredentialId != Guid.Empty orderby s.Name select s)
        {
            if(SelectedSession != null && SelectedSession.Type == "ssh" && IsParentProxy(proxy))
            {
                continue;
            }

            ProxiesList.Add(new ComboBoxGuid(proxy.Id, "[ssh] " + proxy.Name));
        }
    }

    private bool IsParentProxy(Session proxy)
    {
        if(proxy.Id == SelectedSession.Id)
        {
            return true;
        }

        if(proxy.ProxyId == Guid.Empty)
        {
            return false;
        }

        if(proxy.ProxyId == SelectedSession.Id)
        {
            return true;
        }

        Session parentProxy = App.Sessions.Sessions.FirstOrDefault(s => s.Id == proxy.ProxyId);
        if(parentProxy == null)
        {
            return false;
        }

        return IsParentProxy(parentProxy);
    }

    private bool _AdditionalValid = false;
    public bool AdditionalValid => !BatchMode || _AdditionalValid;

    public string Additional
    {
        get
        {
            return EditedSession.Additional;
        }
        set
        {
            EditedSession.Additional = value;
            NotifyPropertyChanged("Additional");
        }
    }

    public Guid PuTTYSessionId
    {
        get
        {
            return EditedSession.PuTTYSessionId;
        }
        set
        {
            EditedSession.PuTTYSessionId = value;
            NotifyPropertyChanged("PuTTYSessionId");
            NotifyPropertyChanged("PuTTYRegSessionValid");
        }
    }

    private ObservableCollection<ComboBoxGuid> _PuTTYSessionList;
    public ObservableCollection<ComboBoxGuid> PuTTYSessionList
    {
        get
        {
            return _PuTTYSessionList;
        }
        set
        {
            _PuTTYSessionList = value;
            NotifyPropertyChanged("PuTTYSessionList");
        }
    }

    public RelayCommand ImportPuTTYSessionCommand { get; set; }
    private void OnImportPuTTYSession()
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog() { Filter = "*.ini;*.reg|*.ini;*.reg|*.*|*.*" };
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            ConfigFile configFile = new ConfigFile("PuTTY") { RealPath = openFileDialog.FileName };

            int num = 2;
            string text;
            string text2 = (text = Path.GetFileName(configFile.Path));
            while (App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile c) => c.Name == text) != null)
            {
                text = text2 + " (" + num + ")";
                num++;
            }
            configFile.Name = text;
            App.Sessions.ConfigFiles.Add(configFile);
            PuTTYSessionId = configFile.Id;
        }
    }

    public bool PuTTYRegSessionValid => PuTTYSessionId == Guid.Empty;

    public RelayCommand PuTTYConfigCommand { get; set; }
    private void OnPuTTYConfig()
    {
        try
        {
            Process.Start(App.Config.PuTTY.FullPath);
        }
        catch (Exception ex)
        {
            log.Error($"Unable to start \"{App.Config.PuTTY.FullPath}\", {ex}");
        }
    }

    public string PuTTYSession
    {
        get
        {
            if(!PuTTYRegSessionValid || string.IsNullOrEmpty(EditedSession.PuTTYSession))
            {
                return "Default Settings";
            }

            return EditedSession.PuTTYSession;
        }
        set
        {
            EditedSession.PuTTYSession = value;
            NotifyPropertyChanged("PuTTYSession");
        }
    }

    private ObservableCollection<ComboBoxOne> _PuTTYRegSessionList;
    public ObservableCollection<ComboBoxOne> PuTTYRegSessionList
    {
        get
        {
            return _PuTTYRegSessionList;
        }
        set
        {
            _PuTTYRegSessionList = value;
            NotifyPropertyChanged("PuTTYRegSessionList");
        }
    }

    private void CreatePuTTYRegSessionList()
    {
        PuTTYRegSessionList = new ObservableCollection<ComboBoxOne>();
        foreach (string puttySession in RegistryHelper.GetPuttySessions())
        {
            PuTTYRegSessionList.Add(new ComboBoxOne(Uri.UnescapeDataString(puttySession)));
        }
    }

    [Range(-1, 60, ErrorMessage = "0-60")]
    public int WaitSeconds
    {
        get
        {
            return EditedSession.WaitSeconds;
        }
        set
        {
            EditedSession.WaitSeconds = value;
            NotifyPropertyChanged("WaitSeconds");
            ValidateProperty("WaitSeconds", WaitSeconds);
        }
    }

    public Guid MSTSCId
    {
        get
        {
            if(UseMSTSC)
            {
                return EditedSession.MSTSCId;
            }

            return Guid.Empty;
        }
        set
        {
            if(UseMSTSC)
            {
                EditedSession.MSTSCId = value;
            }
            NotifyPropertyChanged("MSTSCId");
        }
    }

    public ObservableCollection<ComboBoxGuid> RDPFiles { get; set; }

    public RelayCommand ImportRDPFileCommand { get; set; }
    private void OnImportRDPFile()
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog() { Filter = "*.rdp|*.rdp|*.*|*.*" };
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            ConfigFile configFile = new ConfigFile("RDP") { RealPath = openFileDialog.FileName };

            int num = 2;
            string text;
            string text2 = (text = Path.GetFileName(configFile.Path));
            while (App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile c) => c.Name == text) != null)
            {
                text = text2 + " (" + num + ")";
                num++;
            }
            configFile.Name = text;
            App.Sessions.ConfigFiles.Add(configFile);
            MSTSCId = configFile.Id;
        }
    }

    public string RemoteDirectory
    {
        get
        {
            if(UseWinSCP)
            {
                return EditedSession.RemoteDirectory;
            }

            return null;
        }
        set
        {
            EditedSession.RemoteDirectory = value;
            NotifyPropertyChanged("RemoteDirectory");
        }
    }

    public Guid WinSCPId
    {
        get
        {
            if(UseWinSCP)
            {
                return EditedSession.WinSCPId;
            }

            return Guid.Empty;
        }
        set
        {
            if(UseWinSCP)
            {
                EditedSession.WinSCPId = value;
            }
            NotifyPropertyChanged("WinSCPId");
        }
    }

    public ObservableCollection<ComboBoxGuid> WinSCPInis { get; set; }

    public RelayCommand ImportWinSCPIniCommand { get; set; }
    private void OnImportWinSCPIni()
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog() { Filter = "*.ini|*.ini|*.*|*.*" };
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            ConfigFile configFile = new ConfigFile("WinSCP") { RealPath = openFileDialog.FileName };

            int num = 2;
            string text;
            string text2 = (text = Path.GetFileName(configFile.Path));
            while (App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile c) => c.Name == text) != null)
            {
                text = text2 + " (" + num + ")";
                num++;
            }
            configFile.Name = text;
            App.Sessions.ConfigFiles.Add(configFile);
            WinSCPId = configFile.Id;
        }
    }

    public ObservableCollection<ComboBoxGuid> ScriptFiles { get; set; }
    private void UpdateConfigFiles(object sender, NotifyCollectionChangedEventArgs e)
    {
        PrivateKeys = new ObservableCollection<ComboBoxGuid>
        {
            new ComboBoxGuid(Guid.Empty, Application.Current.Resources["ChoosePrivateKey"] as string)
        };

        foreach (ConfigFile privateKey in from s in App.Sessions.ConfigFiles where s.Type == "PrivateKey" orderby s.Name select s)
        {
            PrivateKeys.Add(new ComboBoxGuid(privateKey.Id, privateKey.Name));

            if(privateKey.GetNameChange() != null)
            {
                privateKey.NameChange -= UpdateConfigFile;
            }
            privateKey.NameChange += UpdateConfigFile;
        }
        
        NotifyPropertyChanged("PrivateKeys");
        if(PrivateKeys.FirstOrDefault(s => s.Key == PrivateKeyId) == null)
        {
            PrivateKeyId = Guid.Empty;
        }
        NotifyPropertyChanged("PrivateKeyId");

        PuTTYSessionList = new ObservableCollection<ComboBoxGuid>
        {
            new ComboBoxGuid(Guid.Empty, Application.Current.Resources["ChoosePuTTYSession"] as string)
        };

        foreach (ConfigFile configFile in from s in App.Sessions.ConfigFiles where s.Type == "PuTTY" orderby s.Name select s)
        {
            PuTTYSessionList.Add(new ComboBoxGuid(configFile.Id, configFile.Name));

            if(configFile.GetNameChange() != null)
            {
                configFile.NameChange -= UpdateConfigFile;
            }
            configFile.NameChange += UpdateConfigFile;
        }

        NotifyPropertyChanged("PuTTYSessionList");
        if(PuTTYSessionList.FirstOrDefault(s => s.Key == PuTTYSessionId) == null)
        {
            PuTTYSessionId = Guid.Empty;
        }
        NotifyPropertyChanged("PuTTYSessionId");
  
        RDPFiles = new ObservableCollection<ComboBoxGuid>()
        {
            new ComboBoxGuid(Guid.Empty, Application.Current.Resources["ChooseRDPFile"] as string)
        };

        foreach (ConfigFile configFile in from s in App.Sessions.ConfigFiles where s.Type == "RDP" orderby s.Name select s)
        {
            RDPFiles.Add(new ComboBoxGuid(configFile.Id, configFile.Name));

            if(configFile.GetNameChange() != null)
            {
                configFile.NameChange -= UpdateConfigFile;
            }
            configFile.NameChange += UpdateConfigFile;
        }

        NotifyPropertyChanged("RDPFiles");
        if(RDPFiles.FirstOrDefault(s => s.Key == MSTSCId) == null)
        {
            MSTSCId = Guid.Empty;
        }
        NotifyPropertyChanged("MSTSCId");

        WinSCPInis = new ObservableCollection<ComboBoxGuid>()
        {
            new ComboBoxGuid(Guid.Empty, Application.Current.Resources["ChooseWinSCPIni"] as string)
        };

        foreach (ConfigFile configFile in from s in App.Sessions.ConfigFiles where s.Type == "WinSCP" orderby s.Name select s)
        {
            WinSCPInis.Add(new ComboBoxGuid(configFile.Id, configFile.Name));

            if(configFile.GetNameChange() != null)
            {
                configFile.NameChange -= UpdateConfigFile;
            }
            configFile.NameChange += UpdateConfigFile;
        }

        NotifyPropertyChanged("WinSCPInis");
        if(WinSCPInis.FirstOrDefault(s => s.Key == WinSCPId) == null)
        {
            WinSCPId = Guid.Empty;
        }
        NotifyPropertyChanged("WinSCPId");

        ScriptFiles = new ObservableCollection<ComboBoxGuid>();

        foreach (ConfigFile configFile in from s in App.Sessions.ConfigFiles where s.Type == "Script" orderby s.Name select s)
        {
            ScriptFiles.Add(new ComboBoxGuid(configFile.Id, configFile.Name));

            if (configFile.GetNameChange() != null)
            {
                configFile.NameChange -= UpdateConfigFile;
            }
            configFile.NameChange += UpdateConfigFile;
        }

        NotifyPropertyChanged("ScriptFiles");
        if(ScriptFiles.FirstOrDefault(s => s.Key == ScriptId) == null)
        {
            UseScriptCheck = false;
            NotifyPropertyChanged("UseScriptCheck");
        }
        NotifyPropertyChanged("ScriptId");

    }

    private void UpdateConfigFile(object sender, EventArgs e)
    {
        UpdateConfigFiles(null, null);
    }

    private bool _SSHv2ShareValid = false;
    public bool SSHv2ShareValid => (OpenInTabCheck == true) && (BatchMode ? _SSHv2ShareValid : _SessionType == "ssh");

    private bool _SSHv2ShareCheckThree;
    public bool SSHv2ShareCheckThree => BatchMode && _SSHv2ShareCheckThree;

    public Nullable<bool> _SSHv2ShareCheck;
    public Nullable<bool> SSHv2ShareCheck
    {
        get
        {
            if (BatchMode)
            {
                return _SSHv2ShareCheck;
            }

            if(SSHv2ShareValid)
            {
                return (EditedSession.iFlags & ProgramConfig.FLAG_SSHV2SHARE) != 0;
            }

            return false;
        }
        set
        {
            _SSHv2ShareCheck = value;

            if (value == true)
            {
                EditedSession.iFlags |= ProgramConfig.FLAG_SSHV2SHARE;
            }
            else
            {
                EditedSession.iFlags &= ~ProgramConfig.FLAG_SSHV2SHARE;
            }
            NotifyPropertyChanged("SSHv2ShareCheck");
        }
    }

    private bool _PasswordOnlyValid = false;
    public bool PasswordOnlyValid => BatchMode ? _PasswordOnlyValid : (_SessionType == "telnet" || _SessionType == "vnc");

    private bool _PasswordOnlyCheckThree = false;
    public bool PasswordOnlyCheckThree => BatchMode && _PasswordOnlyCheckThree;

    public Nullable<bool> _PasswordOnlyCheck;
    public Nullable<bool> PasswordOnlyCheck
    {
        get
        {
            if (BatchMode)
            {
                return _PasswordOnlyCheck;
            }

            if(PasswordOnlyValid)
            {
                return (EditedSession.iFlags & ProgramConfig.FLAG_PASSWORD_ONLY) != 0;
            }

            return false;
        }
        set
        {
            _PasswordOnlyCheck = value;

            if (value == true)
            {
                EditedSession.iFlags |= ProgramConfig.FLAG_PASSWORD_ONLY;
            }
            else
            {
                EditedSession.iFlags &= ~ProgramConfig.FLAG_PASSWORD_ONLY;
            }
            NotifyPropertyChanged("PasswordOnlyCheck");
        }
    }

    private bool _LoggingCheckThree;
    public bool LoggingCheckThree => BatchMode && _LoggingCheckThree;

    private Nullable<bool> _LoggingCheck;
    public Nullable<bool> LoggingCheck
    {
        get
        {
            if (BatchMode)
            {
                return _LoggingCheck;
            }

            return EditedSession.Logging;
        }
        set
        {
            _LoggingCheck = value;

            if(value != null)
            {
                EditedSession.Logging = value.Value;
            }
            NotifyPropertyChanged("LoggingCheck");
        }
    }

    private bool _UseScriptCheckThree;
    public bool UseScriptCheckThree => BatchMode && _UseScriptCheckThree;

    private Nullable<bool> _UseScriptCheck;
    public Nullable<bool> UseScriptCheck
    {
        get
        {
            if (UsePuTTY)
            {
                return _UseScriptCheck;
            }
            return false;
        }
        set
        {
            _UseScriptCheck = value;
            if ((value == false) && UsePuTTY)
            {
                ScriptId = Guid.Empty;
            }
            NotifyPropertyChanged("UseScriptCheck");
        }
    }

    public Guid ScriptId
    {
        get
        {
            if(UsePuTTY)
            {
                return EditedSession.ScriptId;
            }

            return Guid.Empty;
        }
        set
        {
            if(UsePuTTY)
            {
                EditedSession.ScriptId = value;
            }
            NotifyPropertyChanged("ScriptId");
        }
    }

    public RelayCommand ImportScriptCommand { get; set; }
    private void OnImportScript()
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog() { Filter = "*.sh;*.txt|*.sh;*.txt|*.*|*.*" };
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            ConfigFile configFile = new ConfigFile("Script") { RealPath = openFileDialog.FileName };

            int num = 2;
            string text;
            string text2 = (text = Path.GetFileName(configFile.Path));
            while (App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile c) => c.Name == text) != null)
            {
                text = text2 + " (" + num + ")";
                num++;
            }
            configFile.Name = text;
            App.Sessions.ConfigFiles.Add(configFile);
            ScriptId = configFile.Id;
        }
    }

    private bool _StartRemoteAppCheckThree;
    public bool StartRemoteAppCheckThree => BatchMode && _StartRemoteAppCheckThree;

    private Nullable<bool> _StartRemoteAppCheck;
    public Nullable<bool> StartRemoteAppCheck
    {
        get
        {
            return _StartRemoteAppCheck;
        }
        set
        {
            _StartRemoteAppCheck = value;

            _RemoteAppValid = _StartRemoteAppCheck == true;
            _RemoteAppNotValid = _StartRemoteAppCheck == false;
            if(_RemoteAppValid)
            {
                _StartShellValid = false;
                _ShellWorkingDirValid = true;
            }

            if(_RemoteAppNotValid)
            {
                _StartShellValid = _ShellWorkingDirValid = StartShellCheck == true; 
            }

            if(_StartRemoteAppCheck == null)
            {
                _StartShellValid = false;
                _ShellWorkingDirValid = false;
            }

            NotifyPropertyChanged("StartRemoteAppCheck");
            NotifyPropertyChanged("RemoteAppValid");
            NotifyPropertyChanged("RemoteAppNotValid");
            NotifyPropertyChanged("StartShellValid");
            NotifyPropertyChanged("ShellWorkingDirValid");
        }
    }

    private bool _RemoteAppValid = false;
    public bool RemoteAppValid => BatchMode ? _RemoteAppValid : (UseMSTSC && (_StartRemoteAppCheck == true));

    private bool _RemoteAppNotValid = false;
    public bool RemoteAppNotValid => BatchMode ? _RemoteAppNotValid : (!UseMSTSC || (_StartRemoteAppCheck == false));

    private bool _StartShellValid = false;
    public bool StartShellValid => BatchMode ? _StartShellValid : (UseMSTSC && (_StartShellCheck == true && _StartRemoteAppCheck != true));

    private bool _StartShellCheckThree;
    public bool StartShellCheckThree => BatchMode && _StartShellCheckThree;

    private Nullable<bool> _StartShellCheck;
    public Nullable<bool> StartShellCheck
    {
        get
        {
             return _StartShellCheck;
        }
        set
        {
            _StartShellCheck = value;
            _StartShellValid = _ShellWorkingDirValid = _StartShellCheck == true;

            NotifyPropertyChanged("StartShellCheck");
            NotifyPropertyChanged("StartShellValid");
            NotifyPropertyChanged("ShellWorkingDirValid");
        }
    }

    public string ShellPath
    {
        get
        {
            if(UseMSTSC)
            {
                return EditedSession.ShellPath;
            }

            return null;
        }
        set
        {
            EditedSession.ShellPath = value;

            NotifyPropertyChanged("ShellPath");
        }
    }

    public string RemoteAppPath
    {
        get
        {
            if(UseMSTSC)
            {
                return EditedSession.RemoteAppPath;
            }

            return null;
        }
        set
        {
            EditedSession.RemoteAppPath = value;

            NotifyPropertyChanged("RemoteAppPath");
        }
    }

    public string RemoteAppCmdline
    {
        get
        {
            if(UseMSTSC)
            {
                return EditedSession.RemoteAppCmdline;
            }

            return null;
        }
        set
        {
            EditedSession.RemoteAppCmdline = value;
            NotifyPropertyChanged("RemoteAppCmdline");
        }
    }

    private bool _ShellWorkingDirValid = false;
    public bool ShellWorkingDirValid => BatchMode ? _ShellWorkingDirValid : (RemoteAppValid || StartShellValid);

    public string ShellWorkingDir
    {
        get
        {
            if(UseMSTSC)
            {
                return EditedSession.ShellWorkingDir;
            }

            return null;
        }
        set
        {
            EditedSession.ShellWorkingDir = value;
            NotifyPropertyChanged("ShellWorkingDir");
        }
    }

    private bool _WindowsKeyCombinationsCheckThree;
    public bool WindowsKeyCombinationsCheckThree => BatchMode && _WindowsKeyCombinationsCheckThree;

    private Nullable<bool> _WindowsKeyCombinationsCheck;
    public Nullable<bool> WindowsKeyCombinationsCheck
    {
        get
        {
            if(BatchMode)
            {
                return _WindowsKeyCombinationsCheck;
            }

            if (UseMSTSC)
            {
                return EditedSession.KeyboardHook == 1;
            }
            return false;
        }
        set
        {
            _WindowsKeyCombinationsCheck = value;

            EditedSession.KeyboardHook = (value == true) ? 1 : 2;
            NotifyPropertyChanged("WindowsKeyCombinationsCheck");
        }
    }

    private bool _OpenInTabCheckThree;
    public bool OpenInTabCheckThree => BatchMode && _OpenInTabCheckThree;

    private Nullable<bool> _OpenInTabCheck;
    public Nullable<bool> OpenInTabCheck
    {
        get
        {
            if (BatchMode)
            {
                return _OpenInTabCheck;
            }

            return (EditedSession.iFlags & ProgramConfig.FLAG_NOTINTAB) == 0;
        }
        set
        {
            _OpenInTabCheck = value;

            if (value == true)
            {
                EditedSession.iFlags &= ~ProgramConfig.FLAG_NOTINTAB;
                EditedSession.iFlags |= EditedSession.SessionType.Program.iFlags & ProgramConfig.FLAG_CLOSE_MASK;
            }
            else
            {
                EditedSession.iFlags |= ProgramConfig.FLAG_NOTINTAB;
            }
            NotifyPropertyChanged("OpenInTabCheck");
            NotifyPropertyChanged("SSHv2ShareValid");
            NotifyPropertyChanged("MonitorValid");
            NotifyPropertyChanged("WidthHeightValid");
            NotifyPropertyChanged("RDSizeModeValid");
            NotifyPropertyChanged("RDSizeModesList");
            NotifyPropertyChanged("RDSizeMode");
            NotifyPropertyChanged("Method");
        }
    }

    private bool _SyncTitleCheckThree;
    public bool SyncTitleCheckThree => BatchMode && _SyncTitleCheckThree;  

    private Nullable<bool> _SyncTitleCheck;
    public Nullable<bool> SyncTitleCheck
    {
        get
        {
            if (BatchMode)
            {
                return _SyncTitleCheck;
            }

            return (EditedSession.iFlags & ProgramConfig.FLAG_SYNCTITLE) != 0;
        }
        set
        {
            _SyncTitleCheck = value;
            if (value == true)
            {
                EditedSession.iFlags |= ProgramConfig.FLAG_SYNCTITLE;
            }
            else
            {
                EditedSession.iFlags &= ~ProgramConfig.FLAG_SYNCTITLE;
            }
            NotifyPropertyChanged("SyncTitleCheck");
        }
    }

    private bool _EnableHotkeyCheckThree;
    public bool EnableHotkeyCheckThree => BatchMode && _EnableHotkeyCheckThree;  

    private Nullable<bool> _EnableHotkeyCheck;
    public Nullable<bool> EnableHotkeyCheck
    {
        get
        {
            if (BatchMode)
            {
                return _EnableHotkeyCheck;
            }

            return (EditedSession.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY) != 0;
        }
        set
        {
            _EnableHotkeyCheck = value;
            if (value == true)
            {
                EditedSession.iFlags |= ProgramConfig.FLAG_ENABLEHOTKEY;
            }
            else
            {
                EditedSession.iFlags &= ~ProgramConfig.FLAG_ENABLEHOTKEY;
            }
            NotifyPropertyChanged("EnableHotkeyCheck");
        }
    }

    public uint iFlags
    {
        get
        {
            return EditedSession.iFlags;
        }
        set
        {
            EditedSession.iFlags = value;

            switch(value & ProgramConfig.FLAG_CLOSE_MASK)
            {
            case ProgramConfig.FLAG_CLOSE_BY_KICK:
                Method = "Kick";
                break;
            case ProgramConfig.FLAG_CLOSE_BY_KILL:
                Method = "Kill";
                break;
            case ProgramConfig.FLAG_CLOSE_BY_WM_QUIT:
                Method = "WM_QUIT";
                break;
            case ProgramConfig.FLAG_CLOSE_BY_WM_CLOSE:
                Method = "WM_CLOSE";
                break;
            }
        }
    }

    private string _Method;
    public string Method
    {
        get
        {
            if(BatchMode)
            {
                return _Method;
            }

            switch(EditedSession.iFlags & ProgramConfig.FLAG_CLOSE_MASK)
            {
            case ProgramConfig.FLAG_CLOSE_BY_KICK:
                return "Kick";
            case ProgramConfig.FLAG_CLOSE_BY_KILL:
                return "Kill";
            case ProgramConfig.FLAG_CLOSE_BY_WM_QUIT:
                return "WM_QUIT";
            }
            return "WM_CLOSE";
        }
        set
        {
            _Method = value;
            if(!BatchMode)
            {
                EditedSession.iFlags &= ~ProgramConfig.FLAG_CLOSE_MASK;

                switch (value)
                {
                case "Kill":
                    EditedSession.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KILL;
                    break;
                case "Kick":
                    EditedSession.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KICK;
                    break;
                case "WM_QUIT":
                    EditedSession.iFlags |= ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;
                    break;
                }
            }
            NotifyPropertyChanged("Method");
        }
    }

    private ObservableCollection<ComboBoxOne> _MethodsList;
    public ObservableCollection<ComboBoxOne> MethodsList
    {
        get
        {
            return _MethodsList;
        }
        set
        {
            _MethodsList = value;
            NotifyPropertyChanged("MethodsList");
        }
    }

    private bool _CloseIMECheckThree;
    public bool CloseIMECheckThree => BatchMode && _CloseIMECheckThree;  

    private Nullable<bool> _CloseIMECheck;
    public Nullable<bool> CloseIMECheck
    {
        get
        {
            if (BatchMode)
            {
                return _CloseIMECheck;
            }

            return (EditedSession.iFlags & ProgramConfig.FLAG_NOTCLOSEIME) == 0;
        }
        set
        {
            _CloseIMECheck = value;
            if (value == true)
            {
                EditedSession.iFlags &= ~ProgramConfig.FLAG_NOTCLOSEIME;
            }
            else
            {
                EditedSession.iFlags |= ProgramConfig.FLAG_NOTCLOSEIME;
            }
            NotifyPropertyChanged("CloseIMECheck");
        }
    }

    public bool RDSizeModeValid => UseMSTSC && (OpenInTabCheck != null);

    private string _RDSizeMode;
    public string RDSizeMode
    {
        get
        {
            if (OpenInTabCheck != false)
            {
                if(string.IsNullOrEmpty(_RDSizeMode) || _RDSizeMode=="F" || _RDSizeMode=="M")
                {
                    return "A";
                }
            }
            else
            {
                if(string.IsNullOrEmpty(_RDSizeMode) || _RDSizeMode=="A")
                {
                    return "F";
                }
            }

            return _RDSizeMode;
        }
        set
        {
            _RDSizeMode = value;

            if(_RDSizeMode == "C")
            {
                SetDefaultWidthHeight();
            }

            NotifyPropertyChanged("RDSizeMode");
            NotifyPropertyChanged("MonitorValid");
            NotifyPropertyChanged("MultiMonitorsValid");
            NotifyPropertyChanged("WidthHeightValid");
            NotifyPropertyChanged("Width");
            NotifyPropertyChanged("Height");
        }
    }

    private void SetDefaultWidthHeight()
    {
        if(Width <= 0)
        {
            Width = 1024;
        }

        if(Height <= 0)
        {
            Height = 768;
        }
    }

    private ObservableCollection<ComboBoxTwo> _RDSizeModesList;
    private ObservableCollection<ComboBoxTwo> _RDSizeModesList2;
    public ObservableCollection<ComboBoxTwo> RDSizeModesList
    {
        get
        {
            if(OpenInTabCheck != false)
            {
                return _RDSizeModesList;
            }

            return _RDSizeModesList2;
        }
        set
        {
            _RDSizeModesList = value;
            NotifyPropertyChanged("RDSizeModesList");
        }
    }

    public bool MultiMonitorsValid => UseMSTSC && (RDSizeMode == "M");

    public string SelectMonitors
    {
        get
        {
            string text = "";
            int monitor = 0;
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                if(!string.IsNullOrEmpty(text))
                {
                    text += "\n";
                }
                text += monitor + ":" + (screen.Primary?"*":" ") + screen.Bounds.Width + " x " + screen.Bounds.Height + "; (" + screen.Bounds.Left + ", " + screen.Bounds.Top + ", " + (screen.Bounds.Right - 1)  + ", " + (screen.Bounds.Bottom-1) + ")";
                monitor++;
            }
            return text;
        }
    }

    public string SelectedMonitors
    {
        get
        {
            return EditedSession.SelectedMonitors;
        }
        set
        {
            EditedSession.SelectedMonitors = value;
            NotifyPropertyChanged("SelectedMonitors");
        }
    }

    public bool WidthHeightValid =>  UseMSTSC && (RDSizeMode == "C");

    [Range(-1, 8192, ErrorMessage = "200-8192")]
    public int Width
    {
        get
        {
            return EditedSession.Width;
        }
        set
        {
            EditedSession.Width = value;
            NotifyPropertyChanged("Width");
            ValidateProperty("Width", value);
        }
    }

    [Range(-1, 8192, ErrorMessage = "200-8192")]
    public int Height
    {
        get
        {
            return EditedSession.Height;
        }
        set
        {
            EditedSession.Height = value;
            NotifyPropertyChanged("Height");
            ValidateProperty("Height", value);
        }
    }

    public bool MonitorValid => (OpenInTabCheck == false) && (RDSizeMode == "C");
    public string Monitor
    {
        get
        {
            if(MonitorValid) { 
                return EditedSession.Monitor;
            }

            return null;
        }
        set
        {
            EditedSession.Monitor = value;
            NotifyPropertyChanged("Monitor");
        }
    }

    public ObservableCollection<ComboBoxTwo> Monitors { get; set; }

    private void CreateMonitors()
    {
        Monitors = new ObservableCollection<ComboBoxTwo>
        {
            new ComboBoxTwo(null, "default"),
            new ComboBoxTwo("*", Application.Current.Resources["MainMonitor"] as string)
        };

        int monitor = 0;
        foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
        {
            Monitors.Add(new ComboBoxTwo(monitor.ToString(), monitor + ":" + (screen.Primary?"*":" ") + screen.Bounds.Width + " x " + screen.Bounds.Height + "; (" + screen.Bounds.Left + ", " + screen.Bounds.Top + ", " + (screen.Bounds.Right - 1)  + ", " + (screen.Bounds.Bottom-1) + ")"));
            monitor++;
        }
    }

    private Dictionary<Guid, string> InitialTags = new Dictionary<Guid, string>();
    private Dictionary<Guid, string> Tags = new Dictionary<Guid, string>();
    private Dictionary<Guid, string> AddedTags = new Dictionary<Guid, string>();
    private Dictionary<Guid, string> RemovedTags = new Dictionary<Guid, string>();

    public ObservableCollection<ComboBoxOne> AssignedTags { get; set; } = new ObservableCollection<ComboBoxOne>();
    private void CreateTags()
    {
        InitialTags.Clear();
        Tags.Clear();
        AddedTags.Clear();
        RemovedTags.Clear();
        AssignedTags.Clear();
        UnassignedTags.Clear();

        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            if(SelectedSession != null && SelectedSession.Tags != null && SelectedSession.Tags.Contains(tag.Name))
            {
                AssignedTags.Add(new ComboBoxOne(tag.Name));
                InitialTags[tag.RuntimeId] = tag.Name;
                Tags[tag.RuntimeId] = tag.Name;
            }
            else
            {
                UnassignedTags.Add(new ComboBoxOne(tag.Name));
            }
        }

        AssignedTags = OrderList(AssignedTags);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag");
    }

    private ObservableCollection<ComboBoxOne> OrderList(ObservableCollection<ComboBoxOne> list)
    {
        if(list.Count == 0)
        {
            return list;
        }

        return new ObservableCollection<ComboBoxOne>(list.OrderBy((ComboBoxOne s) => s.Key));
    }

    public RelayCommand<string> DeleteAssignedTagCommand { get; set; }
    private void OnDeleteAssignedTag(string tagName)
    {
        Session tagSession = App.Sessions.Sessions.FirstOrDefault(s => s.Type == "tag" && s.Name == tagName);

        foreach(Session childSession in tagSession.ChildSessions.Where(s => s.Type == "tag"))
        {
            if(Tags.ContainsKey(childSession.RuntimeId))
            {
                return;
            }
        }
        
        if(InitialTags.ContainsKey(tagSession.RuntimeId))
        {
            RemovedTags[tagSession.RuntimeId] = tagName;
        }
        AddedTags.Remove(tagSession.RuntimeId);
        Tags.Remove(tagSession.RuntimeId);

        ComboBoxOne item = AssignedTags.FirstOrDefault(s => s.Key == tagName);
        AssignedTags.Remove(item);
        UnassignedTags.Add(item);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag");
    }

    public ComboBoxOne SelectedTag { get; set; }

    public ObservableCollection<ComboBoxOne> UnassignedTags { get; set; } = new ObservableCollection<ComboBoxOne>();

    public RelayCommand AssignCommand { get; set; }
    private void OnAssignTag()
    {
        if (SelectedTag == null)
        {
            return;
        }

        Session tagSession = App.Sessions.Sessions.FirstOrDefault(s => s.Type == "tag" && s.Name == SelectedTag.Key);
        Dictionary<Guid, string> tags = new Dictionary<Guid, string>();

        tags = GetParentTags(tagSession, tags);

        foreach(Guid tagId in tags.Keys)
        {
            if(!InitialTags.ContainsKey(tagId))
            {
                AddedTags[tagId] = tags[tagId];
            }
            RemovedTags.Remove(tagId);

            if(!Tags.ContainsKey(tagId))
            {
                Tags[tagId] = tags[tagId];
            }

            ComboBoxOne item = UnassignedTags.FirstOrDefault(t => t.Key == tags[tagId]);
            if(item != null)
            {
                UnassignedTags.Remove(item);
                AssignedTags.Add(item);
            }
        }

        AssignedTags = OrderList(AssignedTags);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag");
    }

    private Dictionary<Guid, string> GetParentTags(Session tag, Dictionary<Guid, string> tags)
    {
        tags[tag.RuntimeId] = tag.Name;
        
        foreach(Session parentTag in App.Sessions.Sessions.Where(s => s.ChildSessions.Contains(tag)))
        {
            tags = GetParentTags(parentTag, tags);
        }

        return tags;
    }

    public string Comment
    {
        get
        {
            return EditedSession.Comment;
        }
        set
        {
            EditedSession.Comment = value;
            NotifyPropertyChanged("Comment");
        }
    }

    private bool _SaveSessionColorCheck;
    public bool SaveSessionColorCheck
    {
        get
        {
            return _SaveSessionColorCheck;
        }
        set
        {
            _SaveSessionColorCheck = value;
            if (!_SaveSessionColorCheck)
            {
                SelectedColor = null;
            } 
            else
            {
                EditedSession.Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));
            }
            NotifyPropertyChanged("SaveSessionColorCheck");
        }
    }

    private Brush _SelectedColor;
    public Brush SelectedColor
    {
        get
        {
            return _SelectedColor;
        }
        set
        {
            _SelectedColor = value;
            NotifyPropertyChanged("SelectedColor");
        }
    }

    public ObservableCollection<Brush> ColorList => App.SessionColors;

    private Visibility _OkValidationVisibility;
    public Visibility OkValidationVisibility
    {
        get
        {
            return _OkValidationVisibility;
        }
        set
        {
            _OkValidationVisibility = value;
            NotifyPropertyChanged("OkValidationVisibility");
        }
    }

    public RelayCommand SaveCommand { get; set; }
    private void OnSaveSession()
    {
        if (!InputIsValid())
        {
            return;

        }

        if (WantToSaveCredential())
        {
            EditedCredential.Name = EditedCredential.Name.Trim();
            App.Sessions.Credentials.Add(EditedCredential);
            CredentialId = EditedCredential.Id;
            EditedCredential = new Credential();
        }

        if(NewMode)
        {
            Session session = SaveSession();

            SessionsListVM.SelectItem(session);
            SessionsListVM.ListUpdate();
            return;
        }

        if(BatchMode)
        {
            SaveSessions();
        }
        else
        {
            SaveSession();
        }

        CreateTags();
    }

    public RelayCommand SaveNewCommand { get; set; }
    private void OnSaveNewSession()
    {
        SelectedSession = null;
        EditedSession.Id = Guid.NewGuid();
        NewMode = true;
        OnSaveSession();
        NewMode = false;
    }

    public EditSessionViewModel()
    {
        CreateSessionTypeList();
        UpdateCredentialList(null, null);
        ImportPrivateKeyCommand = new RelayCommand(OnImportPrivateKey);
        ImportPuTTYSessionCommand = new RelayCommand(OnImportPuTTYSession);
        PuTTYConfigCommand = new RelayCommand(OnPuTTYConfig);

        UpdateConfigFiles(null, null);
        ImportRDPFileCommand = new RelayCommand(OnImportRDPFile);
        ImportWinSCPIniCommand = new RelayCommand(OnImportWinSCPIni);
        ImportScriptCommand = new RelayCommand(OnImportScript);

        MethodsList = new ObservableCollection<ComboBoxOne>
        {
            new ComboBoxOne("Kick"),
            new ComboBoxOne("WM_CLOSE"),
            new ComboBoxOne("WM_QUIT"),
            new ComboBoxOne("Kill")
        };

        _RDSizeModesList2 = new ObservableCollection<ComboBoxTwo>
        {
            new ComboBoxTwo("F", Application.Current.Resources["FullScreen"] as string),
            new ComboBoxTwo("M", Application.Current.Resources["MultiMonitors"] as string),
            new ComboBoxTwo("C", Application.Current.Resources["Custom"] as string)
        };

        RDSizeModesList = new ObservableCollection<ComboBoxTwo>
        {
            new ComboBoxTwo("A", Application.Current.Resources["AutoFit"] as string),
            new ComboBoxTwo("C", Application.Current.Resources["Custom"] as string)
        };

        CreateMonitors();

        DeleteAssignedTagCommand = new RelayCommand<string>(OnDeleteAssignedTag);
        AssignCommand = new RelayCommand(OnAssignTag);

        SaveCommand = new RelayCommand(OnSaveSession);
        SaveNewCommand = new RelayCommand(OnSaveNewSession);

        App.Sessions.Sessions.CollectionChanged += UpdateSessions;
        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged += UpdateSessions2;
            tag.NameChange += UpdateTag;
        }

        App.Sessions.Credentials.CollectionChanged += UpdateCredentialList;
        App.Sessions.ConfigFiles.CollectionChanged += UpdateConfigFiles;

        UpdateGUI(Visibility.Hidden);
    }

    public override void Cleanup()
    {
        App.Sessions.Sessions.CollectionChanged -= UpdateSessions;
        App.Sessions.Credentials.CollectionChanged -= UpdateCredentialList;
        App.Sessions.ConfigFiles.CollectionChanged -= UpdateConfigFiles;

        foreach (Session tag in App.Sessions.Sessions.Where(s => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged -= UpdateSessions2;
            if(tag.GetNameChange() != null)
            {
                tag.NameChange -= UpdateTag;
            }
        }

        foreach (ConfigFile configFile in App.Sessions.ConfigFiles)
        {
            if(configFile.GetNameChange() != null)
            {
                configFile.NameChange -= UpdateConfigFile;
            }
        }

        base.Cleanup();
    }

    private void UpdateSessions(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        foreach (Session tag in App.Sessions.Sessions.Where(s => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged -= UpdateSessions2;
            tag.ChildSessions.CollectionChanged += UpdateSessions2;

            if(tag.GetNameChange() != null)
            {
                tag.NameChange -= UpdateTag;
            }
            tag.NameChange += UpdateTag;
        }
        UpdateSessions2(sender, notifyCollectionChangedEventArgs);
    }

    private void UpdateTag(object sender, EventArgs e)
    {
        UpdateSessions2(sender, null);
    }

    private void UpdateSessions2(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        Dictionary<Guid, string> tags = new Dictionary<Guid, string>(Tags);
        Dictionary<Guid, string> addedTags = new Dictionary<Guid, string>(AddedTags);
        Dictionary<Guid, string> removedTags = new Dictionary<Guid, string>(RemovedTags);

        InitialTags.Clear();
        Tags.Clear();
        AddedTags.Clear();
        RemovedTags.Clear();
        AssignedTags.Clear();
        UnassignedTags.Clear();

        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            if(tags.ContainsKey(tag.RuntimeId))
            {
                Tags[tag.RuntimeId] = tag.Name;
            }

            if(addedTags.ContainsKey(tag.RuntimeId))
            {
                AddedTags[tag.RuntimeId] = tag.Name;
            }

            if(removedTags.ContainsKey(tag.RuntimeId))
            {
                RemovedTags[tag.RuntimeId] = tag.Name;
            }

            if(SelectedSession != null && SelectedSession.Tags != null && SelectedSession.Tags.Contains(tag.Name))
            {
                AssignedTags.Add(new ComboBoxOne(tag.Name));
                InitialTags[tag.RuntimeId] = tag.Name;
            }
            else
            {
                UnassignedTags.Add(new ComboBoxOne(tag.Name));
            }
        }

        AssignedTags = OrderList(AssignedTags);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag");
    }

    private void RemoveNoChangeFromLists()
    {
        if(CredentialList.ElementAt(0).Key == NoChangeId)
        {
            CredentialList.RemoveAt(0);
        }
        if(ProxiesList != null && ProxiesList.ElementAt(0).Key == NoChangeId)
        {
            ProxiesList.RemoveAt(0);
        }
        if(PuTTYSessionList != null && PuTTYSessionList.ElementAt(0).Key == NoChangeId)
        {
            PuTTYSessionList.RemoveAt(0);
        }
        if(RDPFiles != null && RDPFiles.ElementAt(0).Key == NoChangeId)
        {
            RDPFiles.RemoveAt(0);
        }
        if(WinSCPInis != null && WinSCPInis.ElementAt(0).Key == NoChangeId)
        {
            WinSCPInis.RemoveAt(0);
        }
        if(MethodsList.ElementAt(0).Key == "!NoChange!")
        {
            MethodsList.RemoveAt(0);
        }
        if(_RDSizeModesList.ElementAt(0).Key == "!NoChange!")
        {
            _RDSizeModesList.RemoveAt(0);
        }
        if(_RDSizeModesList2.ElementAt(0).Key == "!NoChange!")
        {
            _RDSizeModesList2.RemoveAt(0);
        }
        if(Monitors.ElementAt(0).Key == "!NoChange!")
        {
            Monitors.RemoveAt(0);
        }
    }

    private List<Session> SelectedSessions;
    public void ShowSelectedSessions(List<Session> sessions)
    {
        SelectedSessions = sessions;

        RemoveNoChangeFromLists();

        if(sessions.Count == 1)
        {
            ShowSelectedSession(sessions[0]);
            return;
        }

        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = true;
        NewMode = false;
        _UsePuTTY = true;
        _UseMSTSC = true;
        _UseWinSCP = true;
        _PrivateKeyValid = true;
        _ProxyValid = true;
        _SSHv2ShareValid = true;
        _PasswordOnlyValid = true;
        _AdditionalValid = true;
        _RemoteAppValid = true;
        _RemoteAppNotValid = true;
        _StartShellValid = true;
        _ShellWorkingDirValid = true;

        SelectedSession = new Session("");

        CreatePuTTYRegSessionList();

        string ProgramName = null;
        uint iFlags = 0;
        bool hasRemoteApp = false;
        bool hasRemoteApp1 = false;
        foreach(Session session in sessions)
        {
            if(string.IsNullOrEmpty(ProgramName))
            {
                ProgramName = session.SessionType.ProgramName;
                iFlags = session.iFlags;
                _OpenInTabCheck = (iFlags & ProgramConfig.FLAG_NOTINTAB) == 0;
                _OpenInTabCheckThree = false;
                hasRemoteApp1 = (session.SessionType.ProgramName == "MSTSC") && !string.IsNullOrEmpty(session.RemoteAppPath);
            }

            if(session.SessionType.ProgramName != "PuTTY" )
            {
                _UsePuTTY = false;
            }

            if(session.SessionType.ProgramName != "MSTSC" )
            {
                _UseMSTSC = false;
                _RemoteAppValid = false;
                _RemoteAppNotValid = !hasRemoteApp;
                _StartShellValid = false;
                _ShellWorkingDirValid = false;
            }
            else
            {
                if(string.IsNullOrEmpty(session.ShellPath))
                {
                    _StartShellValid = false;
                    if(string.IsNullOrEmpty(session.RemoteAppPath))
                    {
                        _ShellWorkingDirValid = false;
                    }
                }

                if (!string.IsNullOrEmpty(session.RemoteAppPath))
                {
                    _RemoteAppNotValid = false;
                    _RemoteAppValid = hasRemoteApp1;
                    hasRemoteApp  = true;
                }
                else
                {
                    _RemoteAppValid = false;
                    _RemoteAppNotValid = !hasRemoteApp1;
                }
            }

            if(session.SessionType.ProgramName != "WinSCP" )
            {
                _UseWinSCP = false;
            }

            if(session.Type != "vnc")
            {
                _VNCSelected = false;
                if(session.Type != "telnet")
                {
                    _PasswordOnlyValid = false;
                }
            }

            if((session.SessionTypeFlags & SessionType.FLAG_PROXY_CONSUMER) == 0)
            {
                _ProxyValid = false;
            }

            if(session.Type != "ssh")
            {
                _SSHv2ShareValid = false;
                if(session.Type != "scp" && session.Type != "sftp")
                {
                    _PrivateKeyValid = false;
                }
            }

            if(session.SessionType.ProgramName != ProgramName)
            {
                _AdditionalValid = false;
            }

            if(OpenInTabCheck != null && (iFlags & ProgramConfig.FLAG_NOTINTAB) != (session.iFlags & ProgramConfig.FLAG_NOTINTAB))
            {
                _OpenInTabCheck = null;
                _OpenInTabCheckThree = true;
            }
        }

        EditedSession = null;

        foreach(Session session in sessions)
        {
            if(EditedSession == null)
            {
                EditedSession = new Session("")
                { 
                    Ip = session.Ip,
                    Port = session.Port,
                    CredentialId = session.CredentialId,

                    Monitor = string.IsNullOrEmpty(session.Monitor) ? null : session.Monitor,
                    ProxyId = session.ProxyId,
                    Additional = session.Additional,

                    PuTTYSession = session.PuTTYSession,
                    WaitSeconds = session.WaitSeconds,
                    Logging = session.Logging,
                    ScriptId = session.ScriptId,
                    WinSCPId = session.WinSCPId,
                    RemoteDirectory = session.RemoteDirectory,

                    ShellPath = session.ShellPath,
                    ShellWorkingDir = session.ShellWorkingDir,
                    RemoteAppPath = session.RemoteAppPath,
                    RemoteAppCmdline = session.RemoteAppCmdline,
                    MSTSCId = session.MSTSCId,
                    KeyboardHook2 = session.KeyboardHook,
                    Width = session.Width,
                    Height = session.Height,
                    FullScreen = session.FullScreen,
                    MultiMonitors = session.MultiMonitors,
                    SelectedMonitors = session.SelectedMonitors,

                    Comment = session.Comment
                };

                SelectedSession.Tags = (session.Tags != null) ? new ObservableCollection<string>(session.Tags) : new ObservableCollection<string>();
                iFlags = session.iFlags;

                _SyncTitleCheck = (OpenInTabCheck == true) && (EditedSession.iFlags & ProgramConfig.FLAG_SYNCTITLE) != 0;
                _SyncTitleCheckThree = false;
                _EnableHotkeyCheck = (OpenInTabCheck == true) && (EditedSession.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY) != 0;
                _EnableHotkeyCheckThree = false;
                _CloseIMECheck = (EditedSession.iFlags & ProgramConfig.FLAG_NOTCLOSEIME) == 0;
                _CloseIMECheckThree = false;

                _SSHv2ShareCheck = (EditedSession.iFlags & ProgramConfig.FLAG_SSHV2SHARE) != 0;
                _SSHv2ShareCheckThree = false;

                _PasswordOnlyCheck = (EditedSession.iFlags & ProgramConfig.FLAG_PASSWORD_ONLY) != 0;
                _PasswordOnlyCheckThree = false;

                _LoggingCheck = EditedSession.Logging;
                _LoggingCheckThree = false;

                _UseScriptCheck = EditedSession.ScriptId != Guid.Empty;
                _UseScriptCheckThree = false;

                _StartRemoteAppCheck = !string.IsNullOrEmpty(EditedSession.RemoteAppPath);
                _StartRemoteAppCheckThree = false;

                _StartShellCheck = !string.IsNullOrEmpty(EditedSession.ShellPath);
                _StartShellCheckThree = false;

                _WindowsKeyCombinationsCheck = (EditedSession.KeyboardHook2 == 1);
                _WindowsKeyCombinationsCheckThree = false;
                continue;
            }

            if(!string.IsNullOrEmpty(Ip) && Ip != session.Ip)
            {
                Ip = null;
            }

            if(Port != -1 && Port != session.Port)
            {
                Port = -1;
            }

            if(OpenInTabCheck == true && SyncTitleCheck != null && (EditedSession.iFlags & ProgramConfig.FLAG_SYNCTITLE) != (session.iFlags & ProgramConfig.FLAG_SYNCTITLE))
            {
                _SyncTitleCheck = null;
                _SyncTitleCheckThree = true;
            }

            if(OpenInTabCheck == true && EnableHotkeyCheck != null && (EditedSession.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY) != (session.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY))
            {
                _EnableHotkeyCheck = null;
                _EnableHotkeyCheckThree = true;
            }

            if(OpenInTabCheck == true && Method != "!NoChange!" && (EditedSession.iFlags & ProgramConfig.FLAG_CLOSE_MASK) != (session.iFlags & ProgramConfig.FLAG_CLOSE_MASK))
            {
                if(MethodsList.ElementAt(0).Key != "!NoChange!")
                {
                    MethodsList.Insert(0, new ComboBoxOne("!NoChange!"));
                }
                Method = "!NoChange!";
            }

            if(CloseIMECheck != null && (EditedSession.iFlags & ProgramConfig.FLAG_NOTCLOSEIME) != (session.iFlags & ProgramConfig.FLAG_NOTCLOSEIME))
            {
                _CloseIMECheck = null;
                _CloseIMECheckThree = true;
            }

            if(SSHv2ShareValid && SSHv2ShareCheck != null && (EditedSession.iFlags & ProgramConfig.FLAG_SSHV2SHARE) != (session.iFlags & ProgramConfig.FLAG_SSHV2SHARE))
            {
                _SSHv2ShareCheck = null;
                _SSHv2ShareCheckThree = true;
            }

            if(PasswordOnlyValid && PasswordOnlyCheck != null && (EditedSession.iFlags & ProgramConfig.FLAG_PASSWORD_ONLY) != (session.iFlags & ProgramConfig.FLAG_PASSWORD_ONLY))
            {
                _PasswordOnlyCheck = null;
                _PasswordOnlyCheckThree = true;
            }

            if(session.Tags != null)
            {
                foreach(string tag in SelectedSession.Tags.ToList())
                {
                    if(!session.Tags.Contains(tag))
                    {
                        SelectedSession.Tags.Remove(tag);
                    }
                }
            }
            else
            {
                SelectedSession.Tags.Clear();
            }

            if(CredentialId != NoChangeId && CredentialId != session.CredentialId)
            {
                if(CredentialList.ElementAt(0).Key != NoChangeId)
                {
                    CredentialList.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
                }
                CredentialId = NoChangeId;
            }

            if(ProxyId != NoChangeId && ProxyId != session.ProxyId)
            {
                ProxyId = NoChangeId;
            }

            if(AdditionalValid && Additional != "!NoChange!" && Additional !=  session.Additional)
            {
                Additional = "!NoChange!";
            }

            if(UsePuTTY)
            {
                if(PuTTYSessionId != NoChangeId && PuTTYSessionId != session.PuTTYSessionId)
                {
                    if(PuTTYSessionList.ElementAt(0).Key != NoChangeId)
                    {
                        PuTTYSessionList.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
                    }
                    PuTTYSessionId = NoChangeId;
                }

                if(PuTTYRegSessionValid && PuTTYSession != "!NoChange!" && (string.IsNullOrEmpty(session.PuTTYSession) ? PuTTYSession != "Default Settings" : PuTTYSession != session.PuTTYSession))
                {
                    if(PuTTYRegSessionList.ElementAt(0).Key != "!NoChange!")
                    {
                        PuTTYRegSessionList.Insert(0, new ComboBoxOne("!NoChange!"));
                    }
                    PuTTYSession = "!NoChange!";
                }

                if(WaitSeconds != -1 && WaitSeconds != session.WaitSeconds)
                {
                    WaitSeconds = -1;
                }

                if(LoggingCheck !=null && EditedSession.Logging != session.Logging)
                {
                    _LoggingCheck = null;
                    _LoggingCheckThree = true;
                }

                if(UseScriptCheck != null && EditedSession.ScriptId != session.ScriptId)
                {
                    _UseScriptCheck = null;
                    _UseScriptCheckThree = true;
                }
            }

            if(UseWinSCP)
            {
                if(RemoteDirectory != "!NoChange!" && RemoteDirectory !=  session.RemoteDirectory)
                {
                    RemoteDirectory = "!NoChange!";
                }

                if(WinSCPId != NoChangeId && WinSCPId != session.WinSCPId)
                {
                    if(WinSCPInis.ElementAt(0).Key != NoChangeId)
                    {
                        WinSCPInis.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
                    }
                    WinSCPId = NoChangeId;
                }
            }

            if(UseMSTSC)
            {
                if(MSTSCId != NoChangeId && MSTSCId != session.MSTSCId)
                {
                    if(RDPFiles.ElementAt(0).Key != NoChangeId)
                    {
                        RDPFiles.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
                    }
                    MSTSCId = NoChangeId;
                }

                if(StartRemoteAppCheck != null && string.IsNullOrEmpty(EditedSession.RemoteAppPath) != string.IsNullOrEmpty(session.RemoteAppPath))
                {
                    _StartRemoteAppCheck = null;
                    _StartRemoteAppCheckThree = true;
                    _ShellWorkingDirValid = false;
                    RemoteAppPath = null;
                    RemoteAppCmdline = null;
                    ShellWorkingDir = null;
                    _OpenInTabCheck = null;
                    _OpenInTabCheckThree = true;
                }

                if(StartShellCheck != null && string.IsNullOrEmpty(EditedSession.ShellPath) != string.IsNullOrEmpty(session.ShellPath))
                {
                    _StartShellCheck = null;
                    _StartShellCheckThree = true;
                    _ShellWorkingDirValid = false;
                    ShellPath = null;
                    ShellWorkingDir = null;
                }

                if(StartShellValid && ShellPath != "!NoChange!" && ShellPath != session.ShellPath)
                {
                    ShellPath = "!NoChange!";
                }

                if(RemoteAppValid)
                {
                    if(RemoteAppPath != "!NoChange!" && RemoteAppPath != session.RemoteAppPath)
                    {
                        RemoteAppPath = "!NoChange!";
                    }

                    if(RemoteAppCmdline != "!NoChange!" && RemoteAppCmdline != session.RemoteAppCmdline)
                    {
                        RemoteAppCmdline = "!NoChange!";
                    }
                }

                if((RemoteAppValid || StartShellValid) && ShellWorkingDir != "!NoChange!" && ShellWorkingDir != session.ShellWorkingDir)
                {
                    ShellWorkingDir = "!NoChange!";
                }

                if(WindowsKeyCombinationsCheck != null && EditedSession.KeyboardHook2 != session.KeyboardHook)
                {
                    _WindowsKeyCombinationsCheck = null;
                    _WindowsKeyCombinationsCheckThree = true;
                }

                if(RDSizeModeValid)
                {
                    string rdsizemode;
                    if(session.FullScreen)
                    {
                        rdsizemode = session.MultiMonitors ? "M":"F";
                    }
                    else
                    {
                        rdsizemode = (session.Width != 0 && session.Height != 0) ? "C":"A";
                    }
                    
                    if(RDSizeMode != "!NoChange!" && (RDSizeMode != rdsizemode))
                    {
                        if(OpenInTabCheck == true)
                        {
                            if(_RDSizeModesList.ElementAt(0).Key != "!NoChange!")
                            {
                                _RDSizeModesList.Insert(0, new ComboBoxTwo("!NoChange!","!NoChange!"));
                            }
                        }
                        else
                        {
                            if(_RDSizeModesList2.ElementAt(0).Key != "!NoChange!")
                            {
                                _RDSizeModesList2.Insert(0, new ComboBoxTwo("!NoChange!","!NoChange!"));
                            }
                        }
                        
                        RDSizeMode = "!NoChange!";
                    }
                }

                if(MultiMonitorsValid && SelectedMonitors != "!NoChange!" && SelectedMonitors != session.SelectedMonitors)
                {
                    SelectedMonitors = "!NoChange!";
                }

                if(WidthHeightValid)
                {
                    if(Width != -1 && Width != session.Width)
                    {
                        Width = -1;
                    }

                    if(Height != -1 && Height != session.Height)
                    {
                        Height = -1;
                    }
                }
            }

            if(MonitorValid && Monitor != "!NoChange!" && Monitor != session.Monitor)
            {
                if(Monitors.ElementAt(0).Key == "!NoChange!")
                {
                    Monitors.Insert(0, new ComboBoxTwo("!NoChange!", "!NoChange!"));
                }
                Monitor = "!NoChange!";
            }

            if(Comment != "!NoChange!" && Comment != session.Comment)
            {
                Comment = "!NoChange!";
            }
        }

        CreateProxiesList(ProxyId == NoChangeId);
        CreateTags();

        UpdateGUI();
        HideNotifications();
    }

    public void ShowSelectedSession(Session session)
    {
        TitleBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
        BatchMode = false;
        NewMode = false;
        CreatePuTTYRegSessionList();
        EditedSession = LoadSelectedSession(session);

        if(UseMSTSC)
        {
            StartRemoteAppCheck = !string.IsNullOrEmpty(EditedSession.RemoteAppPath);
            StartShellCheck = !string.IsNullOrEmpty(EditedSession.ShellPath);

            if(EditedSession.FullScreen)
            {
                RDSizeMode = EditedSession.MultiMonitors ? "M":"F";
            }
            else
            {
                RDSizeMode = (EditedSession.Width != 0 && EditedSession.Height != 0) ? "C":"A";
            }
        }

        if(EditedSession.Color != null)
        {
            SelectedColor = App.SessionColors.FirstOrDefault((Brush x) => x.ToString() == EditedSession.Color.ToString());
            _SaveSessionColorCheck = ((SolidColorBrush)EditedSession.Color).Color != (Application.Current.Resources["t9"] as SolidColorBrush).Color;
        }
        else
        {
            SaveSessionColorCheck = true;
            SelectedColor = null;
        }
        _UseScriptCheck = EditedSession.ScriptId != Guid.Empty;
        CreateProxiesList();
        CreateTags();
        UpdateGUI();
        HideNotifications();
    }

    private Session SelectedSession;
    private Session LoadSelectedSession(Session session)
    {
        SelectedSession = session;
        EditedSession = new Session(session.Type) 
        { 
            Id = session.Id,
            Name = session.Name,
            Ip = session.Ip,
            Port = session.Port,
            CredentialId = session.CredentialId,

            Monitor = string.IsNullOrEmpty(session.Monitor) ? null : session.Monitor,
            ProxyId = session.ProxyId,
            Additional = session.Additional,

            PuTTYSession = session.PuTTYSession,
            PuTTYSessionId = session.PuTTYSessionId,
            WaitSeconds = session.WaitSeconds,
            Logging = session.Logging,
            ScriptId = session.ScriptId,

            RemoteDirectory = session.RemoteDirectory,
            WinSCPId = session.WinSCPId,

            ShellPath = session.ShellPath,
            ShellWorkingDir = session.ShellWorkingDir,
            RemoteAppPath = session.RemoteAppPath,
            RemoteAppCmdline = session.RemoteAppCmdline,
            MSTSCId = session.MSTSCId,
            KeyboardHook = session.KeyboardHook,
            Width = session.Width,
            Height = session.Height,
            FullScreen = session.FullScreen,
            MultiMonitors = session.MultiMonitors,
            SelectedMonitors = session.SelectedMonitors,
            
            Comment = session.Comment,
            Color = session.Color
        };

        _SessionType = session.Type;

        iFlags = session.iFlags;

        EditedCredential = new Credential();

        return EditedSession;
    }

    public void CreateNewSession(Session session, Credential credential)
    {
        EditedSession = session;
        EditedCredential = credential;

        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = false;
        NewMode = true;
        SelectedSession = null;
        SelectedSessions = null;
		
        _SessionType = session.Type;
        StartRemoteAppCheck  = false;
        UseScriptCheck = false;
        OpenInTabCheck = true;
        SelectedColor = null;
        SaveSessionColorCheck = true;
        RemoveNoChangeFromLists();
        CreateTags();
        CreateProxiesList();
        CreatePuTTYRegSessionList();
        UpdateGUI();
        HideNotifications();
    }

    public void SaveCurrent()
    {
        OnSaveSession();
    }

    public void SaveNewCurrent()
    {
        OnSaveNewSession();
    }

    private void SaveSessions()
    {
        foreach(Session session in SelectedSessions)
        {
            if(OpenInTabCheck != null)
            {
                if(OpenInTabCheck.Value)
                {
                    session.iFlags &= ~ProgramConfig.FLAG_NOTINTAB;

                    if(SyncTitleCheck != null)
                    {
                        if(SyncTitleCheck.Value)
                        {
                            session.iFlags |= ProgramConfig.FLAG_SYNCTITLE;
                        }
                        else
                        {
                            session.iFlags &= ~ProgramConfig.FLAG_SYNCTITLE;
                        }
                    }

                    if(EnableHotkeyCheck != null)
                    {
                        if(EnableHotkeyCheck.Value)
                        {
                            session.iFlags |= ProgramConfig.FLAG_ENABLEHOTKEY;
                        }
                        else
                        {
                            session.iFlags &= ~ProgramConfig.FLAG_ENABLEHOTKEY;
                        }
                    }

                    if(Method != "!NoChange!")
                    {
                        session.iFlags &= ~ProgramConfig.FLAG_CLOSE_MASK;

                        switch (Method)
                        {
                        case "Kill":
                            session.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KILL;
                            break;
                        case "Kick":
                            session.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KICK;
                            break;
                        case "WM_QUIT":
                            session.iFlags |= ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;
                            break;
                        }
                    }
                }
                else
                {
                    session.iFlags |= ProgramConfig.FLAG_NOTINTAB;
                    session.iFlags &= ~(ProgramConfig.FLAG_SYNCTITLE | ProgramConfig.FLAG_ENABLEHOTKEY | ProgramConfig.FLAG_CLOSE_MASK);
                }
            }

            if(CloseIMECheck != null)
            {
                if(CloseIMECheck.Value)
                {
                    session.iFlags &= ~ProgramConfig.FLAG_NOTCLOSEIME;
                }
                else
                {
                    session.iFlags |= ProgramConfig.FLAG_NOTCLOSEIME;
                }
            }

            if(SSHv2ShareCheck != null)
            {
                if(SSHv2ShareCheck.Value)
                {
                    session.iFlags |= ProgramConfig.FLAG_SSHV2SHARE;
                }
                else
                {
                    session.iFlags &= ~ProgramConfig.FLAG_SSHV2SHARE;
                }
            }

            if(PasswordOnlyCheck != null)
            {
                if(PasswordOnlyCheck.Value)
                {
                    session.iFlags |= ProgramConfig.FLAG_PASSWORD_ONLY;
                }
                else
                {
                    session.iFlags &= ~ProgramConfig.FLAG_PASSWORD_ONLY;
                }
            }

            session.Tags ??= new ObservableCollection<string>();
            foreach (string tagName in AddedTags.Values)
            {
                if(!session.Tags.Contains(tagName))
                {
                    session.Tags.Add(tagName);
                }
            }
            foreach (string tagName in RemovedTags.Values)
            {
                session.Tags.Remove(tagName);
            }
            if (session.Tags.Count == 0)
            {
                session.Tags = null;
            }

            foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
            {
                tag.ChildSessions.CollectionChanged -= UpdateSessions2;

                if (AddedTags.ContainsKey(tag.RuntimeId))
                {
                    if (!tag.ChildSessions.Contains(session))
                    {
                        tag.ChildSessions.Add(session);
                        tag.OnPropertyChanged("CredentialName");
                    }
                }

                if (RemovedTags.ContainsKey(tag.RuntimeId))
                {
                    tag.ChildSessions.Remove(session);
                    tag.OnPropertyChanged("CredentialName");
                }

                tag.ChildSessions.CollectionChanged += UpdateSessions2;
            }

            if (!string.IsNullOrEmpty(EditedSession.Ip))
            {
                session.Ip = EditedSession.Ip;
            }
            if(EditedSession.Port != -1)
            {
                session.Port = EditedSession.Port;
            }

            if(EditedSession.CredentialId != NoChangeId)
            {
                session.CredentialId = EditedSession.CredentialId;
            }
            if(ProxyValid && EditedSession.ProxyId != NoChangeId)
            {
                session.ProxyId = EditedSession.ProxyId;
            }
            if(MonitorValid && EditedSession.Monitor != "!NoChange!")
            {
                session.Monitor = string.IsNullOrEmpty(Monitor)?null:Monitor;
            }
            if(AdditionalValid && EditedSession.Additional != "!NoChange!")
            {
                session.Additional = string.IsNullOrWhiteSpace(EditedSession.Additional) ? null : EditedSession.Additional.Trim();
            }

            if (UsePuTTY)
            {
                if(EditedSession.PuTTYSession != "!NoChange!")
                {
                    session.PuTTYSession = (EditedSession.PuTTYSession == "Default Settings") ? null : EditedSession.PuTTYSession;
                }

                if(EditedSession.WaitSeconds != -1)
                {
                    session.WaitSeconds = EditedSession.WaitSeconds;
                }

                if(LoggingCheck != null)
                {
                    session.Logging = EditedSession.Logging;
                }

                if(UseScriptCheck != null)
                {
                    session.ScriptId = UseScriptCheck.Value ? EditedSession.ScriptId : Guid.Empty;
                }
            }

            if (UseWinSCP)
            {
                if(EditedSession.RemoteDirectory != "!NoChange!")
                {
                    session.RemoteDirectory = string.IsNullOrEmpty(EditedSession.RemoteDirectory) ? null: EditedSession.RemoteDirectory;
                }

                if(WinSCPId != NoChangeId)
                {
                    session.WinSCPId = WinSCPId;
                }
            }

            if(UseMSTSC)
            {
                if(MSTSCId != NoChangeId)
                {
                    session.MSTSCId = MSTSCId;
                }

                if(RemoteAppValid) 
                { 
                    if(EditedSession.RemoteAppPath != "!NoChange!")
                    {
                        session.RemoteAppPath = string.IsNullOrEmpty(EditedSession.RemoteAppPath) ? null: EditedSession.RemoteAppPath;
                    }

                    if(EditedSession.RemoteAppCmdline != "!NoChange!")
                    {
                        session.RemoteAppCmdline = string.IsNullOrWhiteSpace(EditedSession.RemoteAppCmdline) ? null: EditedSession.RemoteAppCmdline;
                    }

                    if(EditedSession.ShellWorkingDir != "!NoChange!")
                    {
                        session.ShellWorkingDir = string.IsNullOrEmpty(EditedSession.ShellWorkingDir) ? null: EditedSession.ShellWorkingDir;
                    }

                    session.iFlags &= ~(ProgramConfig.FLAG_SYNCTITLE | ProgramConfig.FLAG_CLOSE_MASK | ProgramConfig.FLAG_SSHV2SHARE | ProgramConfig.FLAG_PASSWORD_ONLY | ProgramConfig.FLAG_NOTCLOSEIME | ProgramConfig.FLAG_ENABLEHOTKEY);
                    session.iFlags |= ProgramConfig.FLAG_NOTINTAB;

                    session.KeyboardHook = 0;
                    session.ShellPath = null;
                    session.FullScreen = false;
                    session.Width = 0;
                    session.Height = 0;
                    session.MultiMonitors = false;
                    session.SelectedMonitors = null;
                }

                if(StartShellValid)
                {
                    if(EditedSession.ShellPath != "!NoChange!")
                    {
                        session.ShellPath = string.IsNullOrEmpty(EditedSession.ShellPath) ? null: EditedSession.ShellPath;
                    }

                    if(EditedSession.ShellWorkingDir != "!NoChange!")
                    {
                        session.ShellWorkingDir = string.IsNullOrEmpty(EditedSession.ShellWorkingDir) ? null: EditedSession.ShellWorkingDir;
                    }
                }
                else
                {
                    session.ShellPath = null;
                    session.ShellWorkingDir = null;
                }

                if(RemoteAppNotValid)
                {
                    session.RemoteAppPath = null;
                    session.RemoteAppCmdline = null;

                    if(WindowsKeyCombinationsCheck != null)
                    {
                        session.KeyboardHook = EditedSession.KeyboardHook;
                    }

                    if(RDSizeModeValid && RDSizeMode != "!NoChange!")
                    {
                        session.FullScreen = (RDSizeMode == "F" || RDSizeMode == "M");

                        if(!session.FullScreen)
                        {
                            if(EditedSession.Width != -1)
                            {
                                session.Width = EditedSession.Width;
                            }

                            if(EditedSession.Height != -1)
                            {
                                session.Height = EditedSession.Height;
                            }
                        }
                        else
                        {
                            session.Width = 0;
                            session.Height = 0;
                        }

                        session.MultiMonitors = RDSizeMode == "M";

                        if(session.MultiMonitors)
                        {
                            if(EditedSession.SelectedMonitors != "!NoChange!")
                            {
                                session.SelectedMonitors = string.IsNullOrWhiteSpace(EditedSession.SelectedMonitors) ? null : EditedSession.SelectedMonitors.Trim();
                            }
                        }
                        else
                        {
                            session.SelectedMonitors = null;
                        }
                    }
                }

                RemoveRDPFile(session);
            }

            if(EditedSession.Comment != "!NoChange!")
            {
                session.Comment = string.IsNullOrWhiteSpace(EditedSession.Comment) ? null : EditedSession.Comment.Trim();
            }

            session.OnPropertyChanged("DisplayName");
            session.OnPropertyChanged("CredentialName");
            session.OnPropertyChanged("NameTooltip");
            session.SessionHistory?.OnPropertyChanged("DisplayName");
        }

        SelectedSession.Tags.Clear();
        foreach(string tagName in Tags.Values)
        {
            SelectedSession.Tags.Add(tagName);
        }
    }

    private Session SaveSession()
    {
        Session session = SelectedSession ?? new Session("ssh");

        if (_SelectedColor == null)
        {
            if (SaveSessionColorCheck)
            {
                if (NewMode)
                {
                    EditedSession.Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));
                }
            }
            else
            {
                EditedSession.Color = Application.Current.Resources["t9"] as SolidColorBrush;
            }
        }
        else
        {
            EditedSession.Color = _SelectedColor;
        }

        if (!SSHv2ShareValid)
        {
            SSHv2ShareCheck = false;
        }
        if (OpenInTabCheck == false)
        {
            SyncTitleCheck = false;
            Method = "WM_CLOSE";
        }

        if (!PasswordOnlyValid)
        {
            PasswordOnlyCheck = false;
        }

        session.Type = _SessionType;
        session.Name = Name;

        session.Tags ??= new ObservableCollection<string>();
        foreach(string tagName in AddedTags.Values)
        {
            session.Tags.Add(tagName);
        }
        foreach(string tagName in RemovedTags.Values)
        {
            session.Tags.Remove(tagName);
        }
        if(session.Tags.Count == 0)
        {
            session.Tags = null;
        }

        foreach(Session tag in App.Sessions.Sessions.Where((Session s)=> s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged -= UpdateSessions2;

            if(AddedTags.ContainsKey(tag.RuntimeId))
            {
                if(!tag.ChildSessions.Contains(session))
                {
                    tag.ChildSessions.Add(session);
                    tag.OnPropertyChanged("CredentialName");
                }
            }

            if(RemovedTags.ContainsKey(tag.RuntimeId))
            {
                tag.ChildSessions.Remove(session);
                tag.OnPropertyChanged("CredentialName");
            }

            tag.ChildSessions.CollectionChanged += UpdateSessions2;
        }

        session.Ip = Ip;
        session.Port = Port;
        session.CredentialId = CredentialId;
        session.ProxyId = ProxyId;
        session.Monitor = Monitor;
        session.Additional = string.IsNullOrWhiteSpace(Additional) ? null : Additional.Trim();
        session.PuTTYSessionId = PuTTYSessionId;
        session.PuTTYSession = (PuTTYSession == "Default Settings") ? null : PuTTYSession;
        session.WaitSeconds = WaitSeconds;
        session.iFlags = iFlags;
        session.Logging = UsePuTTY && EditedSession.Logging;
        session.ScriptId = ScriptId;

        session.RemoteDirectory = (UseWinSCP && !string.IsNullOrEmpty(RemoteDirectory)) ? RemoteDirectory: null;
        session.WinSCPId = WinSCPId;

        session.KeyboardHook = UseMSTSC ? EditedSession.KeyboardHook : 0;
        if(RemoteAppValid)
        {
            session.iFlags &= ~(ProgramConfig.FLAG_SYNCTITLE | ProgramConfig.FLAG_CLOSE_MASK | ProgramConfig.FLAG_SSHV2SHARE | ProgramConfig.FLAG_PASSWORD_ONLY | ProgramConfig.FLAG_NOTCLOSEIME | ProgramConfig.FLAG_ENABLEHOTKEY);
            session.iFlags |= ProgramConfig.FLAG_NOTINTAB;
            session.KeyboardHook = 0;
        }
        session.FullScreen = UseMSTSC && (RDSizeMode == "F" || RDSizeMode == "M");
        session.MultiMonitors = UseMSTSC && (RDSizeMode == "M");
        session.SelectedMonitors = session.MultiMonitors ? (string.IsNullOrWhiteSpace(SelectedMonitors) ? null : SelectedMonitors.Trim()) : null;
        session.Width = (UseMSTSC && !session.FullScreen) ? Width : 0;
        session.Height = (UseMSTSC && !session.FullScreen) ? Height : 0;
        session.ShellPath = StartShellValid ? ShellPath: null;
        session.ShellWorkingDir = ((StartShellValid || RemoteAppValid) && !string.IsNullOrEmpty(ShellWorkingDir)) ? ShellWorkingDir: null;
        session.RemoteAppPath = RemoteAppValid ? RemoteAppPath: null;
        session.RemoteAppCmdline = (RemoteAppValid && !string.IsNullOrWhiteSpace(RemoteAppCmdline)) ? RemoteAppCmdline: null;
        session.MSTSCId = MSTSCId;

        session.Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();
        session.Color = EditedSession.Color;

        if(UseMSTSC)
        {
            RemoveRDPFile(session);
        }

        if (SelectedSession == null)
        { 
            App.Sessions.Sessions.Add(session);
        }
        else
        {
            SelectedSession.OnPropertyChanged("DisplayName");
            SelectedSession.OnPropertyChanged("CredentialName");
            SelectedSession.OnPropertyChanged("NameTooltip");
            SelectedSession.SessionHistory?.OnPropertyChanged("DisplayName");
        }

        JumpListManager.SetNewJumpList(App.Sessions.Sessions);

        return session;
    }

    private void RemoveRDPFile(Session session)
    {
        string rdpFile = session.GetRDPFile();

        try
        {
            File.Delete(rdpFile);
        }
        catch (Exception)
        {
        }
    }

    private bool WantToSaveCredential()
    {
        if (!string.IsNullOrWhiteSpace(Username) || !SafeString.IsNullOrEmpty(EditedCredential.Password) || !SafeString.IsNullOrEmpty(EditedCredential.Passphrase))
        {
            return CredentialId == Guid.Empty;
        }
        return false;
    }

    private bool NameHasExisted(string name)
    {
        return App.Sessions.Sessions.FirstOrDefault((Session s) => s.Name == name && s.SessionTypeIsNormal && s.Id != EditedSession.Id) != null;
    }

    private bool CredentialNameHasExisted(string name)
    {
        return App.Sessions.Credentials.FirstOrDefault((Credential c) => c.Name == name && c.Id != EditedCredential.Id) != null;
    }

    private bool InputIsValid()
    {
        if (!BatchMode && string.IsNullOrWhiteSpace(Ip))
        {
            AddError("Ip", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["Address"]));
            return !HasErrors;
        }

        RemoveError("Ip");

        if(Port == 0 || (!BatchMode && Port < 0))
        {
            AddError("Port", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["Port"]));
        }
        else
        {
            RemoveError("Port");
        }

        if (!BatchMode)
        {
            Ip = Ip.Trim();

            if (string.IsNullOrWhiteSpace(Name))
            {
                int num = 2;
                string text = Ip;
                while (NameHasExisted(text))
                {
                    text = Ip + " (" + num + ")";
                    num++;
                }
                Name = text;
            }

            Name = Name.Trim();
           
            if (NameHasExisted(Name))
            {
                if (!NewMode)
                {
                    AddError("Name", string.Format(Application.Current.Resources["Exist"] as string, Application.Current.Resources["SessionName"]));
                }
                else
                {
                    string text = Name;
                    int num = 2;
                    while (NameHasExisted(text))
                    {
                        text = Name + " (" + num + ")";
                        num++;
                    }
                    Name = text;
                    RemoveError("Name");
                }
            }
            else
            {
                RemoveError("Name");
            }
        }

        if (WantToSaveCredential())
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                AddError("Username", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["Username"]));
            }
            else
            {
                RemoveError("Username");

                Username = Username.Trim();

                if (string.IsNullOrWhiteSpace(CredentialName))
                {
                    int num = 2;
                    string text = Username + "@" + Ip;
                    string text2 = text;
                    while (CredentialNameHasExisted(text2))
                    {
                        text2 = text + " (" + num + ")";
                        num++;
                    }
                    CredentialName = text2;
                }

                CredentialName = CredentialName.Trim();
                if (CredentialNameHasExisted(CredentialName))
                {
                    string text = CredentialName;
                    int num = 2;
                    while (CredentialNameHasExisted(text))
                    {
                        text = CredentialName + " (" + num + ")";
                        num++;
                    }
                    CredentialName = text;
                }
            }
        }
        else
        {
            RemoveError("Username");
        }

        if (WidthHeightValid)
        {
            if((Width >=200 && Width <= 8192) || Width == -1)
            {
                RemoveError("Width");
            }
            else
            {
                AddError("Width", "200-8192");
            }

            if((Height >=200 && Height <= 8192) || Height == -1)
            {
                RemoveError("Height");
            }
            else
            {
                AddError("Height", "200-8192");
            }
        } 
        else
        {
            RemoveError("Width");
            RemoveError("Height");
        }

        if(RemoteAppValid && string.IsNullOrEmpty(RemoteAppPath))
        {
            AddError("RemoteAppPath", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["ProgramPath"]));
        }
        else
        {
            RemoveError("RemoteAppPath");
        }

        if(StartShellValid && string.IsNullOrEmpty(ShellPath))
        {
            AddError("ShellPath", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["ProgramPath"]));
        }
        else
        {
            RemoveError("ShellPath");
        }

        if (UseScriptCheck == true && ScriptId == Guid.Empty)
        {
            AddError("ScriptId", string.Format(Application.Current.Resources["Required"] as string, "Script"));
        }
        else
        {
            RemoveError("ScriptId");
        }

        return !HasErrors;
    }

    private void HideNotifications()
    {
        RemoveError("Name");
        RemoveError("Ip");
        RemoveError("Port");
        RemoveError("Username");
        RemoveError("Width");
        RemoveError("Height");
        RemoveError("RemoteAppPath");
        RemoveError("ShellPath");
        RemoveError("ScriptId");
    }

    public void UpdateGUI(Visibility controlVisibility = Visibility.Visible)
    {
        ControlVisible = controlVisibility == Visibility.Visible;

        NotifyPropertyChanged("TitleBackground");
        NotifyPropertyChanged("BatchMode");
        NotifyPropertyChanged("NewMode");
        NotifyPropertyChanged("EditMode");
        NotifyPropertyChanged("ControlVisible");
        NotifyPropertyChanged("Name");
        NotifyPropertyChanged("_SessionType");
        NotifyPropertyChanged("UsePuTTY");
        NotifyPropertyChanged("UseMSTSC");
        NotifyPropertyChanged("UseWinSCP");
        NotifyPropertyChanged("VNCSelected");
        NotifyPropertyChanged("PrivateKeyValid");
        NotifyPropertyChanged("Ip");
        NotifyPropertyChanged("Port");
        NotifyPropertyChanged("CredentialList");
        NotifyPropertyChanged("CredentialId");
        NotifyPropertyChanged("VNCSelected");        
        NotifyPropertyChanged("Username");
        NotifyPropertyChanged("Password");
        NotifyPropertyChanged("PrivateKeyValid");
        NotifyPropertyChanged("PrivateKeys");
        NotifyPropertyChanged("PrivateKeyId");
        NotifyPropertyChanged("Passphrase");
        NotifyPropertyChanged("CredentialName");
        NotifyPropertyChanged("ProxyValid");
        NotifyPropertyChanged("ProxiesList");
        NotifyPropertyChanged("ProxyId");
        NotifyPropertyChanged("AdditionalValid");
        NotifyPropertyChanged("Additional");
        NotifyPropertyChanged("PuTTYSessionList");
        NotifyPropertyChanged("PuTTYSessionId");
        NotifyPropertyChanged("PuTTYRegSessionValid");
        NotifyPropertyChanged("PuTTYRegSessionList");
        NotifyPropertyChanged("PuTTYSession");
        NotifyPropertyChanged("WaitSeconds");
        NotifyPropertyChanged("RDPFiles");
        NotifyPropertyChanged("MSTSCId");
        NotifyPropertyChanged("RemoteDirectory");
        NotifyPropertyChanged("WinSCPInis");
        NotifyPropertyChanged("WinSCPId");
        NotifyPropertyChanged("ScriptFiles");
        NotifyPropertyChanged("ScriptId");
        NotifyPropertyChanged("SSHv2ShareValid");
        NotifyPropertyChanged("SSHv2ShareCheckThree");
        NotifyPropertyChanged("SSHv2ShareCheck");
        NotifyPropertyChanged("PasswordOnlyValid");
        NotifyPropertyChanged("PasswordOnlyCheckThree");
        NotifyPropertyChanged("PasswordOnlyCheck");
        NotifyPropertyChanged("LoggingCheckThree");
        NotifyPropertyChanged("LoggingCheck");
        NotifyPropertyChanged("UseScriptCheckThree");
        NotifyPropertyChanged("UseScriptCheck");
        NotifyPropertyChanged("StartRemoteAppCheckThree");
        NotifyPropertyChanged("StartRemoteAppCheck");
        NotifyPropertyChanged("RemoteAppValid");
        NotifyPropertyChanged("RemoteAppNotValid");
        NotifyPropertyChanged("StartShellCheckThree");
        NotifyPropertyChanged("StartShellCheck");
        NotifyPropertyChanged("StartShellValid");
        NotifyPropertyChanged("ShellPath");
        NotifyPropertyChanged("RemoteAppPath");
        NotifyPropertyChanged("RemoteAppCmdline");
        NotifyPropertyChanged("ShellWorkingDirValid");
        NotifyPropertyChanged("ShellWorkingDir");
        NotifyPropertyChanged("WindowsKeyCombinationsCheckThree");
        NotifyPropertyChanged("WindowsKeyCombinationsCheck");
        NotifyPropertyChanged("OpenInTabCheckThree");
        NotifyPropertyChanged("OpenInTabCheck");
        NotifyPropertyChanged("SyncTitleCheckThree");
        NotifyPropertyChanged("SyncTitleCheck");
        NotifyPropertyChanged("EnableHotkeyCheckThree");
        NotifyPropertyChanged("EnableHotkeyCheck");
        NotifyPropertyChanged("Method");
        NotifyPropertyChanged("CloseIMECheckThree");
        NotifyPropertyChanged("CloseIMECheck");
        NotifyPropertyChanged("RDSizeModeValid");
        NotifyPropertyChanged("RDSizeModesList");
        NotifyPropertyChanged("RDSizeMode");
        NotifyPropertyChanged("MultiMonitorsValid");
        NotifyPropertyChanged("SelectedMonitors");
        NotifyPropertyChanged("WidthHeightValid");
        NotifyPropertyChanged("Width");
        NotifyPropertyChanged("Height");
        NotifyPropertyChanged("MonitorValid");
        NotifyPropertyChanged("Monitor");
        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag");
        NotifyPropertyChanged("Comment");
        NotifyPropertyChanged("SaveSessionColorCheck");
        NotifyPropertyChanged("SelectedColor");
        OkValidationVisibility = Visibility.Collapsed;
    }

    public void HideControl()
    {
        ControlVisible = false;
        NotifyPropertyChanged("ControlVisible");
    }

    private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
    public bool HasErrors => errors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    public IEnumerable GetErrors(string propertyName)
    {
        if (!errors.ContainsKey(propertyName))
        {
            return null;
        }
        return errors[propertyName];
    }

    protected void ValidateProperty<T>(string propertyName, T value)
    {
        List<System.ComponentModel.DataAnnotations.ValidationResult> list = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        ValidationContext validationContext = new ValidationContext(this) { MemberName = propertyName };
        Validator.TryValidateProperty(value, validationContext, list);
        if (list.Any())
        {
            errors[propertyName] = list.Select((System.ComponentModel.DataAnnotations.ValidationResult r) => r.ErrorMessage).ToList();
        }
        else
        {
            errors.Remove(propertyName);
        }
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void AddError(string propertyName, string error)
    {
        errors[propertyName] = new List<string> { error };
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void RemoveError(string propertyName)
    {
        errors.Remove(propertyName);
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    public new event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(string Property)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
