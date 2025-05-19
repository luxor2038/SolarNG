using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using SolarNG.Configs;
using System.Collections.ObjectModel;
using SolarNG.ViewModel;
using SolarNG.Utilities;

namespace SolarNG.Sessions;

public class Session : INotifyPropertyChanged
{
    [DataMember]
    public Guid Id = Guid.NewGuid();

    public Guid RuntimeId = Guid.NewGuid();

    public SessionType SessionType;
    public uint SessionTypeFlags
    {
        get
        {
            if(SessionType == null)
            {
                return 0;
            }

            return SessionType.iFlags;
        }
    }

    public bool SessionTypeIsNormal => (SessionTypeFlags & SessionType.FLAG_SPECIAL_TYPE)==0;
    
    [DataMember]
    public string Type
    {
        get
        {
            if(SessionType == null)
            {
                return "";
            }

            return SessionType.Name;
        }
        set
        {
            if(App.Sessions != null)
            {
                SessionType = App.Sessions.SessionTypes.FirstOrDefault((SessionType t) => t.Name == value);
                if(SessionType != null)
                {
                    return;
                }
            }

            SessionType = App.BuiltinSessionTypes.FirstOrDefault((SessionType t) => t.Name == value);
            if(SessionType != null)
            {
                return;
            }

            SessionType = new SessionType(value);

            App.Sessions.SessionTypes.Add(SessionType);
        }
    }

    private string _Name;
    [DataMember]
    public string Name
    {
        get
        {
            if(Type == "history")
            {
                if(Tab==null)
                {
                    return HistorySession.Name;
                }

                return HistoryName + "["  + Tab.TabName + "]";
            }

            return _Name;
        }
        set
        {
            _Name = value;
            OnPropertyChanged("Name");
            NameChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler NameChange;
    public EventHandler GetNameChange()
    {
        return NameChange;
    }

    public string NameTooltip
    {
        get
        {
            if(string.IsNullOrWhiteSpace(Comment))
            {
                return Name;
            }

            return Name + "\n" + Application.Current?.Resources["Comment"] + ":\n" + Comment;
        }
    }

    public string DisplayName
    {
        get
        {
            if (Type == "tag")
            {
                if(Name == "..") { 
                    return Application.Current.Resources["BackUpOneLevel"] as string;
                }

                if(Name == "...") { 
                    return Application.Current.Resources["BackUpTop"] as string;
                }

                return "tag";
            }

            if(Type == "process" || Type == "window")
            {
                return ClassName;
            }

            if(Type == "history")
            {
                if(Tab != null)
                {
                    return HistoryDisplayName;
                }

                return HistorySession.DisplayName;
            }

            if (Type == "lnk")
            {
                return Program.Path;
            }

            string str = Type + "://";

            if (Type == "app")
            {
                string path = Program.Path;
                try
                {
                    Uri uri = new Uri(Program.FullPath);
                    if(uri.IsFile)
                    {
                        path = Path.GetFileName(Program.Path);
                    }
                }
                catch(Exception)
                {
                }

                if(string.IsNullOrWhiteSpace(Program.DisplayName))
                {
                    return str + path;
                }

                return str + path + " (" + Program.DisplayName + ")";
            }

            if(Type == "proxy")
            {
                string proxy = ProxyType + "://";

                if(ProxyType == "ssh")
                {
                    if(string.IsNullOrWhiteSpace(RemoteIp) || RemotePort == 0)
                    {
                        str = "socks5://";
                    }

                    if(SSHSession != null)
                    {
                        proxy += SSHSession.Ip;
                        if(SSHSession.Port != SSHSession.DefaultPort) 
                        {
                            proxy += ":" + SSHSession.Port;
                        }
                    }
                }
                else
                {
                    proxy += Ip;
                    if(Port != DefaultPort) 
                    {
                        proxy += ":" + Port;
                    }
                }

                if(ListenPort == 0 || ((string.IsNullOrWhiteSpace(RemoteIp) || RemotePort == 0) && ProxyType != "ssh"))
                {
                    return proxy;
                }

                string local = ListenIp;
                if(string.IsNullOrEmpty(local))
                {
                    local = "localhost";
                }

                if(ProxyType == "ssh" && (string.IsNullOrWhiteSpace(RemoteIp) || RemotePort == 0))
                {
                    return str + local + ":" + ListenPort + " (" + proxy + ")";
                }
                 
                return str + local + ":" + ListenPort + " => " + RemoteIp + ":" + RemotePort + " (" + proxy + ")";

            }

            if(SessionTypeIsNormal)
            {
                if(Port != DefaultPort) 
                {
                    return str + Ip + ":" + Port;
                }
            }
            return str + Ip;
        }
    }

    private ObservableCollection<string> _Tags;
    [DataMember]
    public ObservableCollection<string> Tags
    {
        get
        {
            return _Tags;
        }
        set
        {
            _Tags = value;
            OnPropertyChanged("Tags");
        }
    }

    public SuspendableObservableCollection<Session> ChildSessions = new SuspendableObservableCollection<Session>();

    public int ChildSessionsCount
    {
        get
        {
            return ChildSessions.Where(s => s.Type != "tag" || (s.Type == "tag" && s.ChildSessionsCount > 0)).Count();
        }
    }

    public Session UpLevelTag;

    [DataMember]
    public uint iFlags;

    public uint iFlags2
    {
        get
        {
            if(Type == "app" && Program != null)
            {
                return iFlags | Program.iFlags;
            }

            return iFlags;
        }
    }

    [DataMember]
    public List<string> Flags;

    public List<string> Flags2
    {
        get
        {
            if(Type == "app" && Program != null)
            {
                return Program.Flags;
            }

            return Flags;
        }
    }

    [DataMember]
    public string Monitor;

    private Brush _Color = null;
    [DataMember]
    public Brush Color
    {
        get
        {
            if(Type == "history")
            {
                return HistorySession.Color;
            }

            return _Color;
        }
        set
        {
            _Color = value;
            OnPropertyChanged("Color");
            OnPropertyChanged("Color2");
        }
    }

    public Brush Color2
    {
        get
        {
            if(Type == "tag")
            {
                return Application.Current.Resources["fg2"] as SolidColorBrush;
            }

            return Color;
        }
    }

    [DataMember]
    public string Additional;

    [DataMember]
    public string Comment;

    private History _History;
    public History History
    {
        get
        {
            if(_History != null)
            {
                return _History;
            }

            if(Type == "history")
            {
                _History = new History();
                return _History;
            }

            _History = App.Histories.Histories.FirstOrDefault(h => h.SessionId == Id);
            if(_History == null)
            {
                _History = new History()
                {
                    SessionId = Id
                };
                App.Histories.Histories.Add(_History);
            }
            return _History;
        }
        set
        {
            _History = value;
        }
    }

    [DataMember]
    public DateTime OpenTime
    {
        get
        {
            if(App.IsSaving)
            {
                return default;
            }

            return History.OpenTime;
        }
        set
        {
            History.OpenTime = value;
        }
    }

    [DataMember]
    public int OpenCounter
    {
        get
        {
            if(App.IsSaving)
            {
                return default;
            }

            return History.OpenCounter;
        }
        set
        {
            History.OpenCounter = value;
        }
    }

    //Network

    [DataMember]
    public string Ip;

    [DataMember]
    public int Port;

    private int DefaultPort
    {
        get
        {
            if(SessionType == null)
            {
                return 0;
            }

            return SessionType.Port;
        }
    }

    private Guid _CredentialId = Guid.Empty;
    [DataMember]
    public Guid CredentialId
    {
        get
        {
            return _CredentialId;
        }
        set
        {
            _CredentialId = value;
            _Credential = null;
            OnPropertyChanged("CredentialId");
            OnPropertyChanged("CredentialId2");
            OnPropertyChanged("CredentialName");
        }
    }

    public Guid CredentialId2 =>  ((SessionTypeFlags & SessionType.FLAG_CREDENTIAL)!=0 && Type != "proxy" && ProxyType != "ssh") ? CredentialId : Guid.NewGuid();

    private Credential _Credential;
    public Credential Credential
    {
        get 
        { 
            if(_Credential != null)
            {
                return _Credential;
            }

            if (CredentialId == Guid.Empty)
            {
                return null;
            }

            _Credential = App.Sessions.Credentials.FirstOrDefault(c => c.Id == CredentialId);

            return _Credential; 
        }
    }

    public string CredentialName
    {
        get
        {
            if (Type == "tag")
            {
                int count;
                if(Name == "..")
                {
                    if(UpLevelTag != null)
                    {
                        count =  UpLevelTag.ChildSessionsCount;
                    }
                    else
                    {
                        count = App.Sessions.Sessions.Where(s => s.Type != "tag" || (s.Type == "tag" && s.ChildSessionsCount > 0)).Count();
                    }
                }
                else if(Name == "...")
                {
                    count = App.Sessions.Sessions.Where(s => s.Type != "tag" || (s.Type == "tag" && s.ChildSessionsCount > 0)).Count();
                }
                else
                {
                    count = ChildSessions.Count;
                }

                return count.ToString();    
            }

            if (Type == "lnk")
            {
                if (string.IsNullOrWhiteSpace(Program.CommandLine))
                {
                    return Program.DisplayName;
                }
                return Program.DisplayName + " " + Program.CommandLine;
            }

            if (Type == "app")
            {
                if (string.IsNullOrWhiteSpace(Program.CommandLine))
                {
                    return Program.Path;
                }
                return Program.Path + " " + Program.CommandLine;
            }

            if(Type == "proxy" && ProxyType == "ssh")
            {
                return null;
            }

            if(Type == "process" ||  Type == "window")
            {
                return Title;
            }

            if(Type == "history")
            {
                return OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + " (" + OpenCounter + ")";
            }

            return Credential?.Name;
        }
    }

    [DataMember]
    public Guid ProxyId = Guid.Empty;

    //SSH/Telnet

    [DataMember]
    public bool Logging = false;

    [DataMember]
    public string PuTTYSession;

    [DataMember]
    public Guid PuTTYSessionId = Guid.Empty;

    [DataMember]
    public Guid ScriptId = Guid.Empty;

    [DataMember]
    public int WaitSeconds;

    //SFTP/SCTP/FTP

    [DataMember]
    public string RemoteDirectory;

    [DataMember]
    public Guid WinSCPId = Guid.Empty;

    //RDP
    private int _KeyboardHook = 1;
    [DataMember]
    public int KeyboardHook
    {
        get
        {
            if(Type == "rdp")
            {
                return _KeyboardHook;
            }

            return 0;
        }
        set
        {
            _KeyboardHook = value;
        }
    }
    public int KeyboardHook2;

    [DataMember]
    public bool FullScreen;

    [DataMember]
    public bool MultiMonitors;

    [DataMember]
    public string SelectedMonitors;

    [DataMember]
    public int Width;

    [DataMember]
    public int Height;

    [DataMember]
    public string ShellPath;

    [DataMember]
    public string ShellWorkingDir;

    [DataMember]
    public string RemoteAppPath;

    [DataMember]
    public string RemoteAppCmdline;

    [DataMember]
    public Guid MSTSCId = Guid.Empty;

    //Application

    [DataMember]
    public ProgramConfig Program;
   
    //process
    [DataMember]
    public string ClassName = null;

    public string Title = null;

    public int Pid;

    public IntPtr hWindow;

    //Proxy
    [DataMember]
    public string ProxyType;

    [DataMember]
    public string ListenIp;

    [DataMember]
    public int ListenPort;

    [DataMember]
    public string RemoteIp;

    [DataMember]
    public int RemotePort;

    //SSH Proxy
    private Guid _SSHSessionId = Guid.Empty;
    [DataMember]
    public Guid SSHSessionId
    {
        get
        {
            return _SSHSessionId;
        }
        set
        {
            _SSHSessionId = value;
            _SSHSession = null;
        }
    }

    private Session _SSHSession;
    public Session SSHSession
    {
        get
        {
            if(_SSHSession == null && SSHSessionId != Guid.Empty)
            {
                _SSHSession = App.Sessions.Sessions.FirstOrDefault(s => s.Id == SSHSessionId);
            }

            return _SSHSession;
        }
    }

    //history
    public TabBase Tab;

    public string HistoryName;

    public string HistoryDisplayName;

    public Session HistorySession;

    public Session SessionHistory;

    public Visibility TabVisibility => (Type=="history" && Tab != null) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PinVisibility => ((iFlags & ProgramConfig.FLAG_PINNED) != 0) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility TagVisibility => (Type=="tag") ? Visibility.Visible : Visibility.Collapsed;

    public Visibility MenuVisibility => (Type=="tag" && (Name==".." || Name=="...")) ? Visibility.Collapsed : Visibility.Visible;

    public Session(string type)
    {
        Type = type;

        Port = SessionType.Port;

        if(type == "app" || type == "lnk")
        {
            Program = new ProgramConfig("");
        }

        if(type == "proxy")
        {
            ProxyType = "socks5";
            iFlags = ProgramConfig.FLAG_NOTINOVERVIEW;
        }
    }

    public Session()
    {
        Type = "";
    }

    public bool Matches(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        text = text.Trim().ToLower();

        if (Name.ToLower().Contains(text))
        {
            return true;
        }

        if(DisplayName.ToLower().Contains(text))
        {
            return true;
        }

        if(!string.IsNullOrEmpty(CredentialName))
        {
            if (CredentialName.ToLower().Contains(text))
            {
                return true;
            }
        }

        if(Tags != null)
        {
            foreach (string tag in Tags)
            {
                if(tag.ToLower().Contains(text))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public string GetRDPFile()
    {
        if(SessionType.ProgramName != "MSTSC")
        {
            return null;
        }

        ConfigFile configFile = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile s) => s.Id == MSTSCId);

        string file_id = Id.ToString();
        file_id = "_" + file_id.Substring(file_id.Length - 12);

        if(!string.IsNullOrEmpty(Credential?.Username) && !SafeString.IsNullOrEmpty(Credential?.Password))
        {
            file_id += "_" + App.UserId.Substring(App.UserId.Length - 12);
        }

        string rdpFile = configFile?.Path;

        if (string.IsNullOrEmpty(rdpFile))
        {
            rdpFile = "SolarNG" + file_id + ".rdp";
        }
        else
        {
            rdpFile = Path.GetFileNameWithoutExtension(rdpFile) + file_id + Path.GetExtension(rdpFile);
        }

        rdpFile = Path.Combine(App.DataFilePath, "Temp", rdpFile);

        return rdpFile;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
