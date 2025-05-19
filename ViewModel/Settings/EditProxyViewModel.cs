using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

public class EditProxyViewModel : ViewModelBase, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public ProxiesListViewModel ProxiesListVM;


    public Brush TitleBackground { get; set; }

    public bool BatchMode { get; set; }

    public bool NewMode { get; set; }

    public bool EditMode => !BatchMode && !NewMode;

    public bool ControlVisible { get; set; }

    public Session EditedProxy { get; set; } = new Session("proxy");
    public Credential EditedCredential { get; set; } = new Credential();

    public string Name
    {
        get
        {
            return EditedProxy.Name;
        }
        set
        {
            EditedProxy.Name = value;
            NotifyPropertyChanged("Name");
        }
    }

    public string ProxyType
    {
        get
        {
            return EditedProxy.ProxyType;
        }
        set
        {
            EditedProxy.ProxyType = value;
            if(value == "ssh" && !NotInOverviewCheckThree)
            {
                NotInOverviewCheck = false;
            }

            NotifyPropertyChanged("ProxyType");
            NotifyPropertyChanged("SSHValid");
            NotifyPropertyChanged("SSHInvalid");
            NotifyPropertyChanged("SSHSessionId");
        }
    }

    private ObservableCollection<ComboBoxTwo> _ProxyTypeList;
    public ObservableCollection<ComboBoxTwo> ProxyTypeList
    {
        get
        {
            return _ProxyTypeList;
        }
        set
        {
            _ProxyTypeList = value;
            NotifyPropertyChanged("ProxyTypeList");
        }
    }

    public string Ip
    {
        get
        {
            return EditedProxy.Ip;
        }
        set
        {
            EditedProxy.Ip = value;
            NotifyPropertyChanged("Ip");
        }
    }

    [Range(-1, 65535, ErrorMessage = "0-65535")]
    public int Port
    {
        get
        {
            return EditedProxy.Port;
        }
        set
        {
            EditedProxy.Port = value;
            ValidateProperty("Port", value);
            NotifyPropertyChanged("Port");
        }
    }

    private Guid NoChangeId = Guid.NewGuid();

    public Guid CredentialId
    {
        get
        {
            if(SSHValid)
            {
                return Guid.Empty;
            }

            return EditedProxy.CredentialId;
        }
        set
        {
            EditedProxy.CredentialId = value;
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

        if(EditedProxy.CredentialId == NoChangeId)
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


    private bool _SSHValid;
    public bool SSHValid => BatchMode ? _SSHValid : (ProxyType == "ssh");

    private bool _SSHInvalid;
    public bool SSHInvalid => BatchMode ? _SSHInvalid : (ProxyType != "ssh");

    public Guid SSHSessionId
    {
        get
        {
            if(SSHValid)
            {
                return EditedProxy.SSHSessionId;
            }

            return Guid.Empty;
        }
        set
        {
            EditedProxy.SSHSessionId = value;
            NotifyPropertyChanged("SSHSessionId");
        }
    }

    public ObservableCollection<ComboBoxGuid> _SSHSessionList;
    public ObservableCollection<ComboBoxGuid> SSHSessionList
    {
        get
        {
            return _SSHSessionList;
        }
        set
        {
            _SSHSessionList = value;
            NotifyPropertyChanged("SSHSessionList");
        }
    }

    private void CreateSSHSessionList()
    {
        SSHSessionList = new ObservableCollection<ComboBoxGuid>();

        foreach (Session ssh in from s in App.Sessions.Sessions where s.Type == "ssh" orderby s.Name select s)
        {
            SSHSessionList.Add(new ComboBoxGuid(ssh.Id, ssh.Name));
        }

        if(SSHSessionId == Guid.Empty)
        {
            SSHSessionId = SSHSessionList.ElementAt(0).Key;
        } 
    }

    public Guid ProxyId
    {
        get
        {
            if(SSHValid)
            {
                return Guid.Empty;
            }
            return EditedProxy.ProxyId;
        }
        set
        {
            EditedProxy.ProxyId = value;
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
            if(SelectedProxy != null && IsParentProxy(proxy))
            {
                continue;
            }

            ProxiesList.Add(new ComboBoxGuid(proxy.Id, "[" + proxy.ProxyType + "] " + proxy.Name));
        }

        foreach (Session proxy in from s in App.Sessions.Sessions where s.Type == "ssh" && s.CredentialId != Guid.Empty orderby s.Name select s)
        {
            ProxiesList.Add(new ComboBoxGuid(proxy.Id, "[ssh] " + proxy.Name));
        }
    }

    private bool IsParentProxy(Session proxy)
    {
        if(proxy.Id == SelectedProxy.Id)
        {
            return true;
        }

        if(proxy.ProxyId == Guid.Empty)
        {
            return false;
        }

        if(proxy.ProxyId == SelectedProxy.Id)
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

    public string ListenIp
    {
        get
        {
            return EditedProxy.ListenIp;
        }
        set
        {
            EditedProxy.ListenIp = value;
            NotifyPropertyChanged("ListenIp");
        }
    }

    [Range(-1, 65535, ErrorMessage = "0-65535")]
    public int ListenPort
    {
        get
        {
            return EditedProxy.ListenPort;
        }
        set
        {
            EditedProxy.ListenPort = value;
            NotifyPropertyChanged("ListenPort");
            ValidateProperty("ListenPort", value);
        }
    }

    public string RemoteIp
    {
        get
        {
            return EditedProxy.RemoteIp;
        }
        set
        {
            EditedProxy.RemoteIp = value;
            NotifyPropertyChanged("RemoteIp");
        }
    }

    [Range(-1, 65535, ErrorMessage = "0-65535")]
    public int RemotePort
    {
        get
        {
            return EditedProxy.RemotePort;
        }
        set
        {
            EditedProxy.RemotePort = value;
            NotifyPropertyChanged("RemotePort");
            ValidateProperty("RemotePort", value);
        }
    }

    public string Additional
    {
        get
        {
            return EditedProxy.Additional;
        }
        set
        {
            EditedProxy.Additional = value;
            NotifyPropertyChanged("Additional");
        }
    }

    private bool _NotInOverviewCheckThree = false;
    public bool NotInOverviewCheckThree => BatchMode && _NotInOverviewCheckThree;

    private Nullable<bool> _NotInOverviewCheck;
    public Nullable<bool> NotInOverviewCheck
    {
        get
        {
            if (BatchMode)
            {
                return _NotInOverviewCheck;
            }

            return (EditedProxy.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != 0;
        }
        set
        {
            _NotInOverviewCheck = value;

            if (value == true)
            {
                EditedProxy.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
            }
            else
            {
                EditedProxy.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
            }
            NotifyPropertyChanged("NotInOverviewCheck");
        }
    }

    private bool _OpenInTabCheckThree = false;
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

            return (EditedProxy.iFlags & ProgramConfig.FLAG_NOTINTAB) == 0;
        }
        set
        {
            _OpenInTabCheck = value;

            if (value == true)
            {
                EditedProxy.iFlags &= ~ProgramConfig.FLAG_NOTINTAB;
            }
            else
            {
                EditedProxy.iFlags |= ProgramConfig.FLAG_NOTINTAB;
            }
            NotifyPropertyChanged("OpenInTabCheck");
            NotifyPropertyChanged("MonitorValid");
        }
    }

    public bool MonitorValid => (OpenInTabCheck == false);
    public string Monitor
    {
        get
        {
            if(MonitorValid) { 
                return EditedProxy.Monitor;
            }

            return null;
        }
        set
        {
            EditedProxy.Monitor = value;
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
            if(SelectedProxy != null && SelectedProxy.Tags != null && SelectedProxy.Tags.Contains(tag.Name))
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
            return EditedProxy.Comment;
        }
        set
        {
            EditedProxy.Comment = value;
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
                EditedProxy.Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));
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
    private void OnSaveProxy()
    {
        if (!InputIsValid())
        {
            return;

        }

        if (WantToSaveCredential())
        {
            EditedCredential.Name = EditedCredential.Name.Trim();
            App.Sessions.Credentials.Add(EditedCredential);
            EditedProxy.CredentialId = EditedCredential.Id;
            EditedCredential = new Credential();
        }

        if(NewMode)
        {
            Session session = SaveProxy();

            ProxiesListVM.SelectItem(session);
            ProxiesListVM.ListUpdate();
            return;
        }

        if(BatchMode)
        {
            SaveProxies();
        }
        else
        {
            SaveProxy();
        }

        CreateTags();
    }

    public RelayCommand SaveNewCommand { get; set; }
    private void OnSaveNewProxy()
    {
        SelectedProxy = null;
        EditedProxy.Id = Guid.NewGuid();
        NewMode = true;
        OnSaveProxy();
        NewMode = false;
    }

    public EditProxyViewModel()
    {
        ProxyTypeList = new ObservableCollection<ComboBoxTwo>
        {
            new ComboBoxTwo("socks5", "SOCKS5"),
            new ComboBoxTwo("socks4", "SOCKS4"),
            new ComboBoxTwo("http", "HTTP"),
            new ComboBoxTwo("ssh", "SSH")
        };        

        UpdateCredentialList(null, null);

        CreateMonitors();

        DeleteAssignedTagCommand = new RelayCommand<string>(OnDeleteAssignedTag);
        AssignCommand = new RelayCommand(OnAssignTag);

        SaveCommand = new RelayCommand(OnSaveProxy);
        SaveNewCommand = new RelayCommand(OnSaveNewProxy);

        App.Sessions.Sessions.CollectionChanged += UpdateSessions;
        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged += UpdateSessions2;
            tag.NameChange += UpdateTag;
        }

        App.Sessions.Credentials.CollectionChanged += UpdateCredentialList;

        UpdateGUI(Visibility.Hidden);
    }

    public override void Cleanup()
    {
        App.Sessions.Sessions.CollectionChanged -= UpdateSessions;
        App.Sessions.Credentials.CollectionChanged -= UpdateCredentialList;

        foreach (Session tag in App.Sessions.Sessions.Where(s => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged -= UpdateSessions2;
            if(tag.GetNameChange() != null)
            {
                tag.NameChange -= UpdateTag;
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

            if(SelectedProxy != null && SelectedProxy.Tags != null && SelectedProxy.Tags.Contains(tag.Name))
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
        if(SSHSessionList != null && SSHSessionList.ElementAt(0).Key == NoChangeId)
        {
            SSHSessionList.RemoveAt(0);
        }
        if(ProxiesList != null && ProxiesList.ElementAt(0).Key == NoChangeId)
        {
            ProxiesList.RemoveAt(0);
        }
        if(Monitors.ElementAt(0).Key == "!NoChange!")
        {
            Monitors.RemoveAt(0);
        }
    }

    private List<Session> SelectedProxies;
    public void ShowSelectedProxies(List<Session> proxies)
    {
        SelectedProxies = proxies;

        RemoveNoChangeFromLists();

        if(proxies.Count == 1)
        {
            ShowSelectedProxy(proxies[0]);
            return;
        }

        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = true;
        NewMode = false;
        SelectedProxy = new Session("proxy");

        CreateSSHSessionList();

        SSHSessionId = Guid.Empty;

        EditedProxy = null;

        foreach(Session proxy in proxies)
        {
            if(EditedProxy == null)
            {
                EditedProxy = new Session("proxy")
                { 
                    Ip = proxy.Ip,
                    Port = proxy.Port,
                    iFlags = proxy.iFlags,
                    CredentialId = proxy.CredentialId,

                    Monitor = string.IsNullOrEmpty(proxy.Monitor) ? null : proxy.Monitor,
                    ProxyId = proxy.ProxyId,
                    Additional = proxy.Additional,

                    ProxyType = proxy.ProxyType,
                    ListenIp = proxy.ListenIp,
                    ListenPort = proxy.ListenPort,
                    RemoteIp = proxy.RemoteIp,
                    RemotePort = proxy.RemotePort,
                    SSHSessionId = proxy.SSHSessionId,

                    Comment = proxy.Comment
                };

                SelectedProxy.Tags = (proxy.Tags != null) ? new ObservableCollection<string>(proxy.Tags) : new ObservableCollection<string>();

                if(EditedProxy.ProxyType == "ssh")
                {
                    _SSHValid = true;
                    _SSHInvalid = false;
                }
                else
                {
                    _SSHValid = false;
                    _SSHInvalid = true;
                }

                _NotInOverviewCheck = (EditedProxy.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != 0;
                _NotInOverviewCheckThree = false;

                _OpenInTabCheck = (EditedProxy.iFlags & ProgramConfig.FLAG_NOTINTAB) == 0;
                _OpenInTabCheckThree = false;

                continue;
            }

            if((SSHValid || SSHInvalid) &&  ProxyType != proxy.ProxyType)
            {
                _SSHValid = false;
                _SSHInvalid = false;
            }

            if(SSHValid && SSHSessionId != NoChangeId && SSHSessionId != proxy.ProxyId)
            {
                if(SSHSessionList.ElementAt(0).Key != NoChangeId)
                {
                    SSHSessionList.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
                }
                SSHSessionId = NoChangeId;
            }

            if(SSHInvalid && !string.IsNullOrEmpty(Ip) && Ip != proxy.Ip)
            {
                Ip = null;
            }

            if(SSHInvalid && Port != -1 && Port != proxy.Port)
            {
                Port = -1;
            }

            if(NotInOverviewCheck != null && (EditedProxy.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != (proxy.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW))
            {
                _NotInOverviewCheck = null;
                _NotInOverviewCheckThree = true;
            }

            if(OpenInTabCheck != null && (EditedProxy.iFlags & ProgramConfig.FLAG_NOTINTAB) != (proxy.iFlags & ProgramConfig.FLAG_NOTINTAB))
            {
                _OpenInTabCheck = null;
                _OpenInTabCheckThree = true;
            }

            if(proxy.Tags != null)
            {
                foreach(string tag in SelectedProxy.Tags.ToList())
                {
                    if(!proxy.Tags.Contains(tag))
                    {
                        SelectedProxy.Tags.Remove(tag);
                    }
                }
            }
            else
            {
                SelectedProxy.Tags.Clear();
            }

            if(SSHInvalid && CredentialId != NoChangeId && CredentialId != proxy.CredentialId)
            {
                if(CredentialList.ElementAt(0).Key != NoChangeId)
                {
                    CredentialList.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
                }
                CredentialId = NoChangeId;
            }

            if(ProxyId != NoChangeId && ProxyId != proxy.ProxyId)
            {
                ProxyId = NoChangeId;
            }

            if(ListenIp != "!NoChange!" && ListenIp !=  proxy.ListenIp)
            {
                ListenIp = "!NoChange!";
            }

            if(ListenPort != -1 && ListenPort != proxy.ListenPort)
            {
                ListenPort = -1;
            }

            if(RemoteIp != "!NoChange!" && RemoteIp !=  proxy.RemoteIp)
            {
                RemoteIp = "!NoChange!";
            }

            if(RemotePort != -1 && RemotePort != proxy.RemotePort)
            {
                RemotePort = -1;
            }

            if(Additional != "!NoChange!" && Additional !=  proxy.Additional)
            {
                Additional = "!NoChange!";
            }

            if(MonitorValid && Monitor != "!NoChange!" && Monitor != proxy.Monitor)
            {
                if(Monitors.ElementAt(0).Key == "!NoChange!")
                {
                    Monitors.Insert(0, new ComboBoxTwo("!NoChange!", "!NoChange!"));
                }
                Monitor = "!NoChange!";
            }

            if(Comment != "!NoChange!" && Comment != proxy.Comment)
            {
                Comment = "!NoChange!";
            }
        }

        CreateProxiesList(ProxyId == NoChangeId);
        CreateTags();

        UpdateGUI();
        HideNotifications();
    }

    public void ShowSelectedProxy(Session session)
    {
        TitleBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
        BatchMode = false;
        NewMode = false;
        SSHSessionId = Guid.Empty;
        EditedProxy = LoadSelectedProxy(session);
        if(EditedProxy.Color != null)
        {
            SelectedColor = App.SessionColors.FirstOrDefault((Brush x) => x.ToString() == EditedProxy.Color.ToString());
            _SaveSessionColorCheck = ((SolidColorBrush)EditedProxy.Color).Color != (Application.Current.Resources["t9"] as SolidColorBrush).Color;
        }
        else
        {
            SaveSessionColorCheck = true;
            SelectedColor = null;
        }
        CreateSSHSessionList();
        CreateProxiesList();
        CreateTags();
        UpdateGUI();
        HideNotifications();
    }

    private Session SelectedProxy;
    private Session LoadSelectedProxy(Session proxy)
    {
        SelectedProxy = proxy;
        EditedProxy = new Session("proxy") 
        { 
            Id = proxy.Id,
            Name = proxy.Name,
            Ip = proxy.Ip,
            Port = proxy.Port,
            iFlags = proxy.iFlags,
            CredentialId = proxy.CredentialId,

            Monitor = string.IsNullOrEmpty(proxy.Monitor) ? null : proxy.Monitor,
            ProxyId = proxy.ProxyId,
            Additional = proxy.Additional,
            ProxyType = proxy.ProxyType,
            ListenIp = proxy.ListenIp,
            ListenPort = proxy.ListenPort,
            RemoteIp = proxy.RemoteIp,
            RemotePort = proxy.RemotePort,
            SSHSessionId = proxy.SSHSessionId,
           
            Comment = proxy.Comment,
            Color = proxy.Color
        };

        EditedCredential = new Credential();

        return EditedProxy;
    }

    public void CreateNewSession(Session session, Credential credential)
    {
        EditedProxy = session;
        EditedCredential = credential;

        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = false;
        NewMode = true;
        SelectedProxy = null;
        SelectedProxies = null;
		
        OpenInTabCheck = true;
        SelectedColor = null;
        SaveSessionColorCheck = true;
        RemoveNoChangeFromLists();
        CreateTags();
        SSHSessionId = Guid.Empty;
        CreateSSHSessionList();
        CreateProxiesList();
        UpdateGUI();
        HideNotifications();
    }

    public void SaveCurrent()
    {
        OnSaveProxy();
    }

    public void SaveNewCurrent()
    {
        OnSaveNewProxy();
    }

    private void SaveProxies()
    {
        foreach(Session proxy in SelectedProxies)
        {
            if(OpenInTabCheck != null)
            {
                if(OpenInTabCheck.Value)
                {
                    proxy.iFlags &= ~ProgramConfig.FLAG_NOTINTAB;
                }
                else
                {
                    proxy.iFlags |= ProgramConfig.FLAG_NOTINTAB;
                }
            }

            if(NotInOverviewCheck != null)
            {
                if(NotInOverviewCheck.Value)
                {
                    proxy.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
                }
                else
                {
                    proxy.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
                }
            }

            proxy.Tags ??= new ObservableCollection<string>();
            foreach (string tagName in AddedTags.Values)
            {
                if(!proxy.Tags.Contains(tagName))
                {
                    proxy.Tags.Add(tagName);
                }
            }
            foreach (string tagName in RemovedTags.Values)
            {
                proxy.Tags.Remove(tagName);
            }
            if (proxy.Tags.Count == 0)
            {
                proxy.Tags = null;
            }

            foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
            {
                tag.ChildSessions.CollectionChanged -= UpdateSessions2;

                if (AddedTags.ContainsKey(tag.RuntimeId))
                {
                    if (!tag.ChildSessions.Contains(proxy))
                    {
                        tag.ChildSessions.Add(proxy);
                        tag.OnPropertyChanged("CredentialName");
                    }
                }

                if (RemovedTags.ContainsKey(tag.RuntimeId))
                {
                    tag.ChildSessions.Remove(proxy);
                    tag.OnPropertyChanged("CredentialName");
                }

                tag.ChildSessions.CollectionChanged += UpdateSessions2;
            }

            if(SSHValid && EditedProxy.SSHSessionId != NoChangeId)
            {
                proxy.SSHSessionId = EditedProxy.SSHSessionId;
            }

            if(SSHInvalid)
            {
                if(!string.IsNullOrEmpty(EditedProxy.Ip))
                {
                    proxy.Ip = EditedProxy.Ip;
                }
                if(EditedProxy.Port != -1)
                {
                    proxy.Port = EditedProxy.Port;
                }
                if(EditedProxy.CredentialId != NoChangeId)
                {
                    proxy.CredentialId = EditedProxy.CredentialId;
                }
                if(EditedProxy.ProxyId != NoChangeId)
                {
                    proxy.ProxyId = EditedProxy.ProxyId;
                }
            }

            if(EditedProxy.ListenIp != "!NoChange!")
            {
                proxy.ListenIp = EditedProxy.ListenIp;
            }
            if(EditedProxy.ListenPort != -1)
            {
                proxy.ListenPort = EditedProxy.ListenPort;
            }

            if(EditedProxy.RemoteIp != "!NoChange!")
            {
                proxy.RemoteIp = EditedProxy.RemoteIp;
            }
            if(EditedProxy.RemotePort != -1)
            {
                proxy.RemotePort = EditedProxy.RemotePort;
            }

            if(EditedProxy.Additional != "!NoChange!")
            {
                proxy.Additional = string.IsNullOrWhiteSpace(EditedProxy.Additional) ? null : EditedProxy.Additional.Trim();
            }

            if(MonitorValid && EditedProxy.Monitor != "!NoChange!")
            {
                proxy.Monitor = string.IsNullOrEmpty(Monitor)?null:Monitor;
            }

            if(EditedProxy.Comment != "!NoChange!")
            {
                proxy.Comment = string.IsNullOrWhiteSpace(EditedProxy.Comment) ? null : EditedProxy.Comment.Trim();
            }

            proxy.OnPropertyChanged("DisplayName");
            proxy.OnPropertyChanged("CredentialName");
            proxy.OnPropertyChanged("NameTooltip");
            proxy.SessionHistory?.OnPropertyChanged("DisplayName");
        }

        SelectedProxy.Tags.Clear();
        foreach(string tagName in Tags.Values)
        {
            SelectedProxy.Tags.Add(tagName);
        }

        App.RefreshOverview();
    }

    private Session SaveProxy()
    {
        Session proxy = SelectedProxy ?? new Session("proxy");

        if (_SelectedColor == null)
        {
            if (SaveSessionColorCheck)
            {
                if (NewMode)
                {
                    EditedProxy.Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));
                }
            }
            else
            {
                EditedProxy.Color = Application.Current.Resources["t9"] as SolidColorBrush;
            }
        }
        else
        {
            EditedProxy.Color = _SelectedColor;
        }

        proxy.Name = Name;

        if(OpenInTabCheck == true)
        {
            proxy.iFlags &= ~ProgramConfig.FLAG_NOTINTAB;
        }
        else
        {
            proxy.iFlags |= ProgramConfig.FLAG_NOTINTAB;
        }

        if(NotInOverviewCheck == true)
        {
            proxy.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
        }
        else
        {
            proxy.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
        }

        proxy.Tags ??= new ObservableCollection<string>();
        foreach(string tagName in AddedTags.Values)
        {
            proxy.Tags.Add(tagName);
        }
        foreach(string tagName in RemovedTags.Values)
        {
            proxy.Tags.Remove(tagName);
        }
        if(proxy.Tags.Count == 0)
        {
            proxy.Tags = null;
        }

        foreach(Session tag in App.Sessions.Sessions.Where((Session s)=> s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged -= UpdateSessions2;

            if(AddedTags.ContainsKey(tag.RuntimeId))
            {
                if(!tag.ChildSessions.Contains(proxy))
                {
                    tag.ChildSessions.Add(proxy);
                    tag.OnPropertyChanged("CredentialName");
                }
            }

            if(RemovedTags.ContainsKey(tag.RuntimeId))
            {
                tag.ChildSessions.Remove(proxy);
                tag.OnPropertyChanged("CredentialName");
            }

            tag.ChildSessions.CollectionChanged += UpdateSessions2;
        }

        proxy.Ip = SSHInvalid ? Ip.Trim() : null;
        proxy.Port =  SSHInvalid ? Port : 0;
        proxy.CredentialId = CredentialId;
        proxy.ProxyId = ProxyId;
        proxy.ProxyType = ProxyType;
        proxy.ListenIp = string.IsNullOrWhiteSpace(ListenIp) ? null : ListenIp.Trim();
        proxy.ListenPort = ListenPort;
        proxy.RemoteIp = string.IsNullOrWhiteSpace(RemoteIp) ? null : RemoteIp.Trim();
        proxy.RemotePort = RemotePort;
        proxy.SSHSessionId = SSHSessionId;

        proxy.Additional = string.IsNullOrWhiteSpace(Additional) ? null : Additional.Trim();
        proxy.Monitor = Monitor;

        proxy.Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();
        proxy.Color = EditedProxy.Color;

        if (SelectedProxy == null)
        { 
            App.Sessions.Sessions.Add(proxy);
        }
        else
        {
            SelectedProxy.OnPropertyChanged("DisplayName");
            SelectedProxy.OnPropertyChanged("CredentialName");
            SelectedProxy.OnPropertyChanged("NameTooltip");
            SelectedProxy.SessionHistory?.OnPropertyChanged("DisplayName");
            App.RefreshOverview();
        }

        JumpListManager.SetNewJumpList(App.Sessions.Sessions);

        return proxy;
    }

    private bool WantToSaveCredential()
    {
        if (!string.IsNullOrWhiteSpace(Username) || !SafeString.IsNullOrEmpty(EditedCredential.Password))
        {
            return CredentialId == Guid.Empty;
        }
        return false;
    }

    private bool NameHasExisted(string name)
    {
        return App.Sessions.Sessions.FirstOrDefault((Session s) => s.Name == name && s.Type == "proxy" && s.Id != EditedProxy.Id) != null;
    }

    private bool CredentialNameHasExisted(string name)
    {
        return App.Sessions.Credentials.FirstOrDefault((Credential c) => c.Name == name && c.Id != EditedCredential.Id) != null;
    }

    private bool InputIsValid()
    {
        if (!BatchMode && !SSHValid && string.IsNullOrWhiteSpace(Ip))
        {
            AddError("Ip", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["Address"]));
            return !HasErrors;
        }

        RemoveError("Ip");
        RemoveError("SSHSessionId");

        if(SSHInvalid && (Port == 0 || (!BatchMode && Port < 0)))
        {
            AddError("Port", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["Port"]));
        }
        else
        {
            RemoveError("Port");
        }

        if (!BatchMode)
        {
            if(!SSHValid)
            {
                Ip = Ip.Trim();
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                if(SSHValid)
                {
                    if(EditedProxy.SSHSession != null)
                    {
                        string text = EditedProxy.SSHSession.Name + " (proxy)";
                        int num = 2;

                        while (NameHasExisted(text))
                        {
                            text = EditedProxy.SSHSession.Name + " (proxy)" + " (" + num + ")";
                            num++;
                        }
                        Name = text;
                    }
                    else
                    {
                        AddError("SSHSessionId", string.Format(Application.Current.Resources["NotExist"] as string, "SSH" + Application.Current.Resources["Session2"]));
                        return !HasErrors;
                    }
                }
                else
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
            }

            Name = Name.Trim();
           
            if (NameHasExisted(Name))
            {
                if (!NewMode)
                {
                    AddError("Name", string.Format(Application.Current.Resources["Exist"] as string, Application.Current.Resources["ProxyName"]));
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

        if(NotInOverviewCheck == false)
        {
            if(ListenPort == 0 || (!BatchMode && ListenPort < 0))
            {
                AddError("ListenPort", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["ListenPort"]));
            }
            else
            {
                RemoveError("ListenPort");
            }

            if(SSHValid)
            {
                RemoveError("RemoteIp");
                RemoveError("RemotePort");
                return !HasErrors;
            }

            if(string.IsNullOrWhiteSpace(RemoteIp))
            {
                AddError("RemoteIp", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["RemoteAddress"]));
            }
            else
            {
                RemoveError("RemoteIp");
            }

            if(RemotePort == 0 || (!BatchMode && RemotePort < 0))
            {
                AddError("RemotePort", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["RemotePort"]));
            }
            else
            {
                RemoveError("RemotePort");
            }
        }
        else
        {
            RemoveError("ListenPort");
            RemoveError("RemoteIp");
            RemoveError("RemotePort");
        }

        return !HasErrors;
    }

    private void HideNotifications()
    {
        RemoveError("Name");
        RemoveError("Ip");
        RemoveError("Port");
        RemoveError("Username");
        RemoveError("SSHSessionId");
        RemoveError("ListenPort");
        RemoveError("RemoteIp");
        RemoveError("RemotePort");
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
        NotifyPropertyChanged("ProxyType");
        NotifyPropertyChanged("Ip");
        NotifyPropertyChanged("Port");
        NotifyPropertyChanged("CredentialList");
        NotifyPropertyChanged("CredentialId");
        NotifyPropertyChanged("Username");
        NotifyPropertyChanged("Password");
        NotifyPropertyChanged("CredentialName");
        NotifyPropertyChanged("SSHValid");
        NotifyPropertyChanged("SSHInvalid");
        NotifyPropertyChanged("SSHSessionList");
        NotifyPropertyChanged("SSHSessionId");
        NotifyPropertyChanged("ProxiesList");
        NotifyPropertyChanged("ProxyId");
        NotifyPropertyChanged("ListenIp");
        NotifyPropertyChanged("ListenPort");
        NotifyPropertyChanged("RemoteIp");
        NotifyPropertyChanged("RemotePort");
        NotifyPropertyChanged("Additional");
        NotifyPropertyChanged("NotInOverviewCheckThree");
        NotifyPropertyChanged("NotInOverviewCheck");
        NotifyPropertyChanged("OpenInTabCheckThree");
        NotifyPropertyChanged("OpenInTabCheck");
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
