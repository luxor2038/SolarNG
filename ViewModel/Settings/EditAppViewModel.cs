using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SolarNG.Configs;
using SolarNG.Sessions;
using SolarNG.Utilities;

namespace SolarNG.ViewModel.Settings;

public class EditAppViewModel : ViewModelBase, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public AppsListViewModel AppsListVM;

    public Brush TitleBackground { get; set; }

    public bool BatchMode { get; set; }

    public bool NewMode { get; set; }

    public bool EditMode => !BatchMode && !NewMode;

    public bool ControlVisible { get; set; }

    private Session EditedApp { get; set; } = new Session("app");

    public string Name
    {
        get
        {
            return EditedApp.Name;
        }
        set
        {
            EditedApp.Name = value;
            NotifyPropertyChanged("Name");
        }
    }

    public string ExePath
    {
        get
        {
            return EditedApp.Program.Path;
        }
        set
        {
            EditedApp.Program.Path = value;
            NotifyPropertyChanged("ExePath");
        }
    }

    public RelayCommand OpenExeFileCommand { get; set; }
    private void OnOpenExeFile()
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            ExePath = openFileDialog.FileName;
        }
    }

    public string DisplayName
    {
        get
        {
            return EditedApp.Program.DisplayName;
        }
        set
        {
            EditedApp.Program.DisplayName = value;
            NotifyPropertyChanged("DisplayName");
        }
    }

    public string Arch
    {
        get
        {
            if(string.IsNullOrEmpty(EditedApp.Program.Arch))
            {
                return "default";
            }
                
            return EditedApp.Program.Arch;
        }
        set
        {
            if(value == "default")
            {
                EditedApp.Program.Arch = null;
            }
            else
            {
                EditedApp.Program.Arch = value;
            }
            NotifyPropertyChanged("Arch");
        }
    }

    private ObservableCollection<ComboBoxOne> _ArchList;
    public ObservableCollection<ComboBoxOne> ArchList
    {
        get
        {
            return _ArchList;
        }
        set
        {
            _ArchList = value;
            NotifyPropertyChanged("ArchList");
        }
    }

    public string CommandLine
    {
        get
        {
            return EditedApp.Program.CommandLine;
        }
        set
        {
            EditedApp.Program.CommandLine = value;
            NotifyPropertyChanged("CommandLine");
        }
    }

    public string WorkingDir
    {
        get
        {
            return EditedApp.Program.WorkingDir;
        }
        set
        {
            EditedApp.Program.WorkingDir = value;
            NotifyPropertyChanged("WorkingDir");
        }
    }

    private string _ProcessMode;
    public string ProcessMode
    {
        get
        {
            if(string.IsNullOrEmpty(_ProcessMode))
            {
                _ProcessMode = "child";
            }

            return _ProcessMode;
        }
        set
        {
            _ProcessMode = value;
            NotifyPropertyChanged("ProcessMode");
        }
    }
    private string GetProcessMode(List<string> flags)
    {
        if(flags == null)
        {
            return "child";
        }

        if(flags.Contains("nonchild"))
        {
            return "nonchild";
        }
        
        if(flags.Contains("singleton"))
        {
            return "singleton";
        }

        return "child";
    }

    private ObservableCollection<ComboBoxOne> _ProcessModeList;
    public ObservableCollection<ComboBoxOne> ProcessModeList
    {
        get
        {
            return _ProcessModeList;
        }
        set
        {
            _ProcessModeList = value;
            NotifyPropertyChanged("ProcessModeList");
        }
    }
    private string RemoveProcessMode(List<string> flags)
    {
        if(flags == null )
        {
            return "";
        }

        List<string> flags2 = new List<string>(flags);

        flags2.Remove("child");
        flags2.Remove("nonchild");
        flags2.Remove("singleton");

        return string.Join("|", flags2.ToArray());
    }

    public string ProcessName
    {
        get
        {
            return EditedApp.Program.ProcessName;
        }
        set
        {
            EditedApp.Program.ProcessName = value;
            NotifyPropertyChanged("ProcessName");
        }
    }

    private string _ClassName;
    public string ClassName
    {
        get
        {
            return _ClassName;
        }
        set
        {
            _ClassName = value;
            NotifyPropertyChanged("ClassName");
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

            return (EditedApp.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != 0;
        }
        set
        {
            _NotInOverviewCheck = value;

            if (value == true)
            {
                EditedApp.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
            }
            else
            {
                EditedApp.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
            }
            NotifyPropertyChanged("NotInOverviewCheck");
        }
    }

    private bool _UsedForSessionCheckThree = false;
    public bool UsedForSessionCheckThree => BatchMode && _UsedForSessionCheckThree;

    private Nullable<bool> _UsedForSessionCheck;
    public Nullable<bool> UsedForSessionCheck
    {
        get
        {
            if(BatchMode)
            {
                return _UsedForSessionCheck;
            }

            return (EditedApp.Program.iFlags & ProgramConfig.FLAG_USED_FOR_SESSION) != 0;
        }
        set
        {
            _UsedForSessionCheck = value;

            if (value == true)
            {
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_USED_FOR_SESSION;
            }
            else
            {
                EditedApp.Program.iFlags &= ~ProgramConfig.FLAG_USED_FOR_SESSION;
            }
            NotifyPropertyChanged("UsedForSessionCheck");
        }
    }

    public string Args
    {
        get
        {
            return EditedApp.Program.Args;
        }
        set
        {
            EditedApp.Program.Args = value;
            NotifyPropertyChanged("Args");
        }
    }

    private string _AuthClassName;
    public string AuthClassName
    {
        get
        {
            return _AuthClassName;
        }
        set
        {
            _AuthClassName = value;
            NotifyPropertyChanged("AuthClassName");
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

            return (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTINTAB) == 0;
        }
        set
        {
            _OpenInTabCheck = value;

            if (value == true)
            {
                EditedApp.Program.iFlags &= ~ProgramConfig.FLAG_NOTINTAB;
            }
            else
            {
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_NOTINTAB;
            }
            NotifyPropertyChanged("OpenInTabCheck");
        }
    }

    private bool _SyncTitleCheckThree;
    public bool SyncTitleCheckThree => BatchMode && _SyncTitleCheckThree; 

    private Nullable<bool> _SyncTitleCheck;
    public Nullable<bool> SyncTitleCheck
    {
        get
        {
            if(BatchMode)
            {
                return _SyncTitleCheck;
            }

            return (EditedApp.Program.iFlags & ProgramConfig.FLAG_SYNCTITLE) != 0;
        }
        set
        {
            _SyncTitleCheck = value;

            if (value == true)
            {
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_SYNCTITLE;
            }
            else
            {
                EditedApp.Program.iFlags &= ~ProgramConfig.FLAG_SYNCTITLE;
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

            return (EditedApp.Program.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY) != 0;
        }
        set
        {
            _EnableHotkeyCheck = value;
            if (value == true)
            {
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_ENABLEHOTKEY;
            }
            else
            {
                EditedApp.Program.iFlags &= ~ProgramConfig.FLAG_ENABLEHOTKEY;
            }
            NotifyPropertyChanged("EnableHotkeyCheck");
        }
    }

    public string Method
    {
        get
        {
            switch(EditedApp.Program.iFlags & ProgramConfig.FLAG_CLOSE_MASK)
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
            EditedApp.Program.iFlags &= ~ProgramConfig.FLAG_CLOSE_MASK;

            switch (value)
            {
            case "Kill":
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KILL;
                break;
            case "Kick":
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KICK;
                break;
            case "WM_QUIT":
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;
                break;
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

    private string _Flags;
    public string Flags
    {
        get
        {
            if(string.IsNullOrEmpty(_Flags) || (OpenInTabCheck == false))
            {
                return "default";
            }
            return _Flags;
        }
        set
        {
            _Flags = value;
            NotifyPropertyChanged("Flags");
        }
    }

    private ObservableCollection<ComboBoxOne> _FlagsList;
    public ObservableCollection<ComboBoxOne> FlagsList
    {
        get
        {
            return _FlagsList;
        }
        set
        {
            _FlagsList = value;
            NotifyPropertyChanged("FlagsList");
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

            return (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTCLOSEIME) == 0;
        }
        set
        {
            _CloseIMECheck = value;
            if (value == true)
            {
                EditedApp.Program.iFlags &= ~ProgramConfig.FLAG_NOTCLOSEIME;
            }
            else
            {
                EditedApp.Program.iFlags |= ProgramConfig.FLAG_NOTCLOSEIME;
            }
            NotifyPropertyChanged("CloseIMECheck");
        }
    }

    public string Monitor
    {
        get
        {
            if(OpenInTabCheck == false) { 
                return EditedApp.Monitor;
            }

            return null;
        }
        set
        {
            EditedApp.Monitor = value;
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
            if(SelectedApp != null && SelectedApp.Tags != null && SelectedApp.Tags.Contains(tag.Name))
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
            return EditedApp.Comment;
        }
        set
        {
            EditedApp.Comment = value;
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
                EditedApp.Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));
            }
            NotifyPropertyChanged("SaveSessionColorCheck");
        }
    }

    private Brush _SelectedColor;
    public Brush SelectedColor
    {
        get
        {
            if (_SelectedColor == null)
            {
                return EditedApp.Color;
            }
            return _SelectedColor;
        }
        set
        {
            _SelectedColor = value;
            NotifyPropertyChanged("SelectedColor");
        }
    }

    private void UpdateColorListVisibility()
    {
        SolidColorBrush solidColorBrush = (SolidColorBrush)SelectedColor;
        if (solidColorBrush == null)
        {
            SaveSessionColorCheck = false;
        }
        else
        {
            SaveSessionColorCheck = solidColorBrush.Color != (Application.Current.Resources["t9"] as SolidColorBrush).Color;
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
    private void OnSaveApp()
    {
        if (!InputIsValid())
        {
            return;
        }

        if(NewMode)
        {
            Session session = SaveApp();

            AppsListVM.SelectItem(session);
            AppsListVM.ListUpdate();
            return;
        }

        if(BatchMode)
        {
            SaveApps();
        }
        else
        {
            SaveApp();
        }

        CreateTags();
    }

    public RelayCommand SaveNewCommand { get; set; }
    private void OnSaveNewApp()
    {
        SelectedApp = null;
        EditedApp.Id = Guid.NewGuid();
        NewMode = true;
        OnSaveApp();
        NewMode = false;
    }

    public EditAppViewModel()
    {
        OpenExeFileCommand = new RelayCommand(OnOpenExeFile);
        ArchList = new ObservableCollection<ComboBoxOne>
        {
            new ComboBoxOne("default"),
            new ComboBoxOne("x86"),
            new ComboBoxOne("x64"),
        };
        MethodsList = new ObservableCollection<ComboBoxOne>
        {
            new ComboBoxOne("Kick"),
            new ComboBoxOne("WM_CLOSE"),
            new ComboBoxOne("WM_QUIT"),
            new ComboBoxOne("Kill")
        };
        FlagsList = new ObservableCollection<ComboBoxOne>
        {
            new ComboBoxOne("default"),
            new ComboBoxOne("noresize"),
            new ComboBoxOne("nomaximize"),
            new ComboBoxOne("keepws"),
            new ComboBoxOne("keepparent"),
            new ComboBoxOne("keepparent&noresize"),
            new ComboBoxOne("mintty")
        };
        ProcessModeList = new ObservableCollection<ComboBoxOne>
        {
            new ComboBoxOne("child"),
            new ComboBoxOne("nonchild"),
            new ComboBoxOne("singleton"),
        };

        CreateMonitors();

        DeleteAssignedTagCommand = new RelayCommand<string>(OnDeleteAssignedTag);
        AssignCommand = new RelayCommand(OnAssignTag);

        SaveCommand = new RelayCommand(OnSaveApp);
        SaveNewCommand = new RelayCommand(OnSaveNewApp);

        App.Sessions.Sessions.CollectionChanged += UpdateSessions;
        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged += UpdateSessions2;
            tag.NameChange += UpdateTag;
        }

        UpdateGUI(Visibility.Hidden);
    }

    public override void Cleanup()
    {
        App.Sessions.Sessions.CollectionChanged -= UpdateSessions;

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

            if(SelectedApp != null && SelectedApp.Tags != null && SelectedApp.Tags.Contains(tag.Name))
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
        if(MethodsList.ElementAt(0).Key == "!NoChange!")
        {
            MethodsList.RemoveAt(0);
        }
        if(ProcessModeList.ElementAt(0).Key == "!NoChange!")
        {
            ProcessModeList.RemoveAt(0);
        }
        if(FlagsList.ElementAt(0).Key == "!NoChange!")
        {
            FlagsList.RemoveAt(0);
        }
        if(Monitors.ElementAt(0).Key == "!NoChange!")
        {
            Monitors.RemoveAt(0);
        }
    }

    private List<Session> SelectedApps;
    public void ShowSelectedApps(List<Session> sessions)
    {
        SelectedApps = sessions;

        RemoveNoChangeFromLists();

        if(sessions.Count == 1)
        {
            ShowSelectedApp(sessions[0]);
            return;
        }
        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = true;
        NewMode = false;

        SelectedApp = new Session("app");

        EditedApp = null;

        foreach(Session app in sessions)
        {
            if(EditedApp == null)
            {
                EditedApp = new Session("app")
                { 
                    Monitor = string.IsNullOrEmpty(app.Monitor) ? null : app.Monitor,
                    Program = app.Program.Clone(),
                    Comment = app.Comment
                };

                SelectedApp.Tags = (app.Tags != null) ? new ObservableCollection<string>(app.Tags) : new ObservableCollection<string>();

                switch(app.Program.iFlags & ProgramConfig.FLAG_CLOSE_MASK)
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

                ClassName = EditedApp.Program.ClassName != null ? string.Join("|", EditedApp.Program.ClassName.ToArray()) : null;
                AuthClassName = EditedApp.Program.AuthClassName != null ? string.Join("|", EditedApp.Program.AuthClassName.ToArray()):null;
                ProcessMode = GetProcessMode(EditedApp.Program.Flags);
                Flags = RemoveProcessMode(EditedApp.Program.Flags).Replace('|', '&');

                _NotInOverviewCheck = (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != 0;
                _NotInOverviewCheckThree = false;
                _UsedForSessionCheck = (EditedApp.Program.iFlags & ProgramConfig.FLAG_USED_FOR_SESSION) != 0;
                _UsedForSessionCheckThree = false;
                _OpenInTabCheck = (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTINTAB) == 0;
                _OpenInTabCheckThree = false;
                _SyncTitleCheck = (OpenInTabCheck == true) && (EditedApp.Program.iFlags & ProgramConfig.FLAG_SYNCTITLE) != 0;
                _SyncTitleCheckThree = false;
                _EnableHotkeyCheck = (OpenInTabCheck == true) && (EditedApp.Program.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY) != 0;
                _EnableHotkeyCheckThree = false;
                _CloseIMECheck = (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTCLOSEIME) == 0;
                _CloseIMECheckThree = false;
                continue;
            }

            if(NotInOverviewCheck != null && (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != (app.Program.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW))
            {
                _NotInOverviewCheck = null;
                _NotInOverviewCheckThree = true;
            }

            if(UsedForSessionCheck != null && (EditedApp.Program.iFlags & ProgramConfig.FLAG_USED_FOR_SESSION) != (app.Program.iFlags & ProgramConfig.FLAG_USED_FOR_SESSION))
            {
                _UsedForSessionCheck = null;
                _UsedForSessionCheckThree = true;
            }

            if(OpenInTabCheck != null && (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTINTAB) != (app.Program.iFlags & ProgramConfig.FLAG_NOTINTAB))
            {
                _OpenInTabCheck = null;
                _OpenInTabCheckThree = true;
            }

            if(OpenInTabCheck == true && SyncTitleCheck != null && (EditedApp.Program.iFlags & ProgramConfig.FLAG_SYNCTITLE) != (app.Program.iFlags & ProgramConfig.FLAG_SYNCTITLE))
            {
                _SyncTitleCheck = null;
                _SyncTitleCheckThree = true;
            }

            if(OpenInTabCheck == true && EnableHotkeyCheck != null && (EditedApp.Program.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY) != (app.Program.iFlags & ProgramConfig.FLAG_ENABLEHOTKEY))
            {
                _EnableHotkeyCheck = null;
                _EnableHotkeyCheckThree = true;
            }

            if(OpenInTabCheck == true && Method != "!NoChange!" && (EditedApp.Program.iFlags & ProgramConfig.FLAG_CLOSE_MASK) != (app.Program.iFlags & ProgramConfig.FLAG_CLOSE_MASK))
            {
                if(MethodsList.ElementAt(0).Key != "!NoChange!")
                {
                    MethodsList.Insert(0, new ComboBoxOne("!NoChange!"));
                }
                Method = "!NoChange!";
            }

            if(CloseIMECheck != null && (EditedApp.Program.iFlags & ProgramConfig.FLAG_NOTCLOSEIME) != (app.Program.iFlags & ProgramConfig.FLAG_NOTCLOSEIME))
            {
                _CloseIMECheck = null;
                _CloseIMECheckThree = true;
            }

            if(ExePath != null && ExePath != app.Program.Path)
            {
                ExePath = null;
            }

            if(DisplayName != "!NoChange!" && DisplayName != app.Program.DisplayName)
            {
                DisplayName = "!NoChange!";
            }

            if(Arch != "!NoChange!" && (string.IsNullOrEmpty(app.Program.Arch) ? Arch != "default" : Arch != app.Program.Arch))
            {
                if(ArchList.ElementAt(0).Key != "!NoChange!")
                {
                    ArchList.Insert(0, new ComboBoxOne("!NoChange!"));
                }
                Arch = "!NoChange!";
            }

            if(CommandLine != "!NoChange!" && CommandLine != app.Program.CommandLine)
            {
                CommandLine = "!NoChange!";
            }

            if(WorkingDir != "!NoChange!" && WorkingDir != app.Program.WorkingDir)
            {
                WorkingDir = "!NoChange!";
            }

            if(ProcessName != "!NoChange!" && ProcessName != app.Program.ProcessName)
            {
                ProcessName = "!NoChange!";
            }

            if(ClassName != "!NoChange!")
            {
                string classname = app.Program.ClassName != null ? string.Join("|", app.Program.ClassName.ToArray()) : null;
                if(ClassName != classname)
                {
                    ClassName = "!NoChange!";
                }
            }

            if(Args != "!NoChange!" && Args != app.Program.Args)
            {
                Args = "!NoChange!";
            }

            if(AuthClassName != "!NoChange!")
            {
                string str = app.Program.AuthClassName != null ? string.Join("|", app.Program.AuthClassName.ToArray()):null;
                if(AuthClassName != str)
                {
                    AuthClassName = "!NoChange!";
                }
            }

            if(ProcessMode != "!NoChange!")
            {
                string processMode = GetProcessMode(app.Program.Flags);
                if(ProcessMode != processMode)
                {
                    if(ProcessModeList.ElementAt(0).Key != "!NoChange!")
                    {
                        ProcessModeList.Insert(0, new ComboBoxOne("!NoChange!"));
                    }

                    ProcessMode = "!NoChange!";
                }
            }

            if(Flags != "!NoChange!")
            {
                string flags = RemoveProcessMode(app.Program.Flags).Replace('|', '&');

                if(FlagsList.ElementAt(0).Key != "!NoChange!")
                {
                    FlagsList.Insert(0, new ComboBoxOne("!NoChange!"));
                }

                Flags = "!NoChange!";
            }

            if(OpenInTabCheck == false && Monitor != "!NoChange!" && Monitor != app.Monitor)
            {
                if(Monitors.ElementAt(0).Key != "!NoChange!")
                {
                    Monitors.Insert(0, new ComboBoxTwo("!NoChange!", "!NoChange!"));
                }
                Monitor = "!NoChange!";
            }

            if(app.Tags != null)
            {
                foreach(string tag in SelectedApp.Tags.ToList())
                {
                    if(!app.Tags.Contains(tag))
                    {
                        SelectedApp.Tags.Remove(tag);
                    }
                }
            }
            else
            {
                SelectedApp.Tags.Clear();
            }

            if(Comment != "!NoChange!" && Comment != app.Comment)
            {
                Comment = "!NoChange!";
            }
        }

        CreateTags();

        UpdateGUI();
        HideNotifications();
    }

    public void ShowSelectedApp(Session session)
    {
        TitleBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
        BatchMode = false;
        NewMode = false;

        EditedApp = LoadSelectedApp(session);
        _SelectedColor = null;
        CreateTags();
        UpdateGUI();
        HideNotifications();
    }

    private Session SelectedApp;
    private Session LoadSelectedApp(Session session)
    {
        SelectedApp = session;
        EditedApp = new Session("app")
        {
            Id = session.Id,
            Name = session.Name,
            Monitor = string.IsNullOrEmpty(session.Monitor) ? null : session.Monitor,
            Program = session.Program.Clone(),
            Comment = session.Comment,
            Color =  App.SessionColors.FirstOrDefault((Brush x) => x.ToString() == (session.Color ?? App.SessionColors[0]).ToString())
        };

        if (EditedApp.Color == null)
        {
            EditedApp.Color = session.Color;
        }

        switch(EditedApp.Program.iFlags & ProgramConfig.FLAG_CLOSE_MASK)
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

        ClassName = EditedApp.Program.ClassName != null ? string.Join("|", EditedApp.Program.ClassName.ToArray()) : null;
        AuthClassName = EditedApp.Program.AuthClassName != null ? string.Join("|", EditedApp.Program.AuthClassName.ToArray()):null;
        ProcessMode = GetProcessMode(EditedApp.Program.Flags);
        Flags = RemoveProcessMode(EditedApp.Program.Flags).Replace('|', '&');

        return EditedApp;
    }

    public void CreateNewApp(Session session)
    {
        EditedApp = session;

        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = false;
        NewMode = true;
        SelectedApp = null;
        SelectedApps = null;

        DisplayName = "";
        OpenInTabCheck = true;
        Method = "WM_CLOSE";
        ClassName = null;
        ProcessName = null;
        Flags = "default";
        SelectedColor = null;
        SaveSessionColorCheck = true;
        RemoveNoChangeFromLists();
        CreateTags();
        UpdateGUI();
        HideNotifications();
    }

    public void SaveCurrent()
    {
        OnSaveApp();
    }

    public void SaveNewCurrent()
    {
        OnSaveNewApp();
    }

    private void SaveApps()
    {
        foreach(Session app in SelectedApps)
        {
            if(NotInOverviewCheck != null)
            {
                if(NotInOverviewCheck.Value)
                {
                    app.Program.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
                }
                else
                {
                    app.Program.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
                }
            }

            if(OpenInTabCheck != null)
            {
                if(OpenInTabCheck.Value)
                {
                    app.Program.iFlags &= ~ProgramConfig.FLAG_NOTINTAB;

                    if(SyncTitleCheck != null)
                    {
                        if(SyncTitleCheck.Value)
                        {
                            app.Program.iFlags |= ProgramConfig.FLAG_SYNCTITLE;
                        }
                        else
                        {
                            app.Program.iFlags &= ~ProgramConfig.FLAG_SYNCTITLE;
                        }
                    }

                    if(EnableHotkeyCheck != null)
                    {
                        if(EnableHotkeyCheck.Value)
                        {
                            app.Program.iFlags |= ProgramConfig.FLAG_ENABLEHOTKEY;
                        }
                        else
                        {
                            app.Program.iFlags &= ~ProgramConfig.FLAG_ENABLEHOTKEY;
                        }
                    }

                    if(Method != "!NoChange!")
                    {
                        app.Program.iFlags &= ~ProgramConfig.FLAG_CLOSE_MASK;

                        switch (Method)
                        {
                        case "Kill":
                            app.Program.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KILL;
                            break;
                        case "Kick":
                            app.Program.iFlags |= ProgramConfig.FLAG_CLOSE_BY_KICK;
                            break;
                        case "WM_QUIT":
                            app.Program.iFlags |= ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;
                            break;
                        }
                    }
                }
                else
                {
                    app.Program.iFlags |= ProgramConfig.FLAG_NOTINTAB;
                    app.Program.iFlags &= ~(ProgramConfig.FLAG_SYNCTITLE | ProgramConfig.FLAG_ENABLEHOTKEY | ProgramConfig.FLAG_CLOSE_MASK);

                    if(Monitor != "!NoChange!")
                    {
                        app.Monitor = string.IsNullOrEmpty(Monitor)?null:Monitor;
                    }
                }
            }

            if(CloseIMECheck != null)
            {
                if(CloseIMECheck.Value)
                {
                    app.Program.iFlags &= ~ProgramConfig.FLAG_NOTCLOSEIME;
                }
                else
                {
                    app.Program.iFlags |= ProgramConfig.FLAG_NOTCLOSEIME;
                }
            }

            if(!string.IsNullOrWhiteSpace(EditedApp.Program.Path))
            {
                app.Program.Path = EditedApp.Program.Path;
            }

            if(EditedApp.Program.DisplayName != "!NoChange!")
            {
                app.Program.DisplayName = string.IsNullOrWhiteSpace(EditedApp.Program.DisplayName) ? null : EditedApp.Program.DisplayName;
            }

            if(EditedApp.Program.Arch != "!NoChange!")
            {
                app.Program.Arch = (EditedApp.Program.Arch == "default") ? null : EditedApp.Program.Arch;
            }

            if(EditedApp.Program.CommandLine != "!NoChange!")
            {
                app.Program.CommandLine = EditedApp.Program.CommandLine;
            }

            if(EditedApp.Program.WorkingDir != "!NoChange!")
            {
                app.Program.WorkingDir = EditedApp.Program.WorkingDir;
            }

            if(Flags != "!NoChange!" || ProcessMode != "!NoChange!")
            {
                string processMode = GetProcessMode(app.Program.Flags);
                string flags = RemoveProcessMode(app.Program.Flags).Replace('|', '&');

                if(Flags != "!NoChange!")
                {
                    flags = Flags.Replace('&', '|');
                    if(flags == "default")
                    {
                        flags = "";
                    }
                }

                if(ProcessMode != "!NoChange!")
                { 
                    processMode = ProcessMode;
                }

                if(processMode != "child")
                {
                    if(string.IsNullOrWhiteSpace(flags))
                    {
                        flags = processMode;
                    }
                    else 
                    {
                        flags += "|" + processMode;
                    }
                }

                if(string.IsNullOrWhiteSpace(flags))
                {
                    app.Program.Flags = null;
                }
                else
                {
                    app.Program.Flags = new List<string>(flags.Split('|'));
                }
            }

            if(EditedApp.Program.ProcessName != "!NoChange!")
            {
                app.Program.ProcessName = EditedApp.Program.ProcessName;
            }

            if(ClassName != "!NoChange!")
            {
                if(string.IsNullOrEmpty(ClassName))
                {
                    app.Program.ClassName = null;
                }
                else
                {
                    app.Program.ClassName =  new List<string>(ClassName.Split('|'));
                }
            }

            if(UsedForSessionCheck != null)
            {
                if(UsedForSessionCheck.Value)
                {
                    if(EditedApp.Program.Args != "!NoChange!")
                    {
                        app.Program.Args = string.IsNullOrWhiteSpace(EditedApp.Program.Args) ? null : EditedApp.Program.Args;
                    }

                    if(AuthClassName != "!NoChange!")
                    {
                        if(string.IsNullOrEmpty(AuthClassName))
                        {
                            app.Program.AuthClassName = null;
                        }
                        else
                        {
                            app.Program.AuthClassName = new List<string>(AuthClassName.Split('|'));
                        }
                    }
                }
            }

            app.Tags ??= new ObservableCollection<string>();
            foreach (string tagName in AddedTags.Values)
            {
                if(!app.Tags.Contains(tagName))
                {
                    app.Tags.Add(tagName);
                }
            }
            foreach (string tagName in RemovedTags.Values)
            {
                app.Tags.Remove(tagName);
            }
            if (app.Tags.Count == 0)
            {
                app.Tags = null;
            }

            foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
            {
                tag.ChildSessions.CollectionChanged -= UpdateSessions2;

                if (AddedTags.ContainsKey(tag.RuntimeId))
                {
                    if (!tag.ChildSessions.Contains(app))
                    {
                        tag.ChildSessions.Add(app);
                        tag.OnPropertyChanged("CredentialName");
                    }
                }

                if (RemovedTags.ContainsKey(tag.RuntimeId))
                {
                    tag.ChildSessions.Remove(app);
                    tag.OnPropertyChanged("CredentialName");
                }

                tag.ChildSessions.CollectionChanged += UpdateSessions2;
            }

            if(EditedApp.Comment != "!NoChange!")
            {
                app.Comment = string.IsNullOrWhiteSpace(EditedApp.Comment) ? null : EditedApp.Comment.Trim();
            }

            app.OnPropertyChanged("DisplayName");
            app.OnPropertyChanged("CredentialName");
            app.OnPropertyChanged("NameTooltip");
            app.SessionHistory?.OnPropertyChanged("DisplayName");
        }

        SelectedApp.Tags.Clear();
        foreach(string tagName in Tags.Values)
        {
            SelectedApp.Tags.Add(tagName);
        }

        App.RefreshOverview();
    }


    private Session SaveApp()
    {
        Session app = SelectedApp ?? new Session("app");

        if (_SelectedColor == null)
        {
            if (SaveSessionColorCheck)
            {
                if (NewMode)
                {
                    EditedApp.Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));
                }
            }
            else
            {
                EditedApp.Color = Application.Current.Resources["t9"] as SolidColorBrush;
            }
        }
        else
        {
            EditedApp.Color = _SelectedColor;
        }

        app.Name = Name;  
        app.Monitor = Monitor;
        app.Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();
        app.Color = EditedApp.Color;
        app.Program = EditedApp.Program.Clone();

        if(string.IsNullOrWhiteSpace(app.Program.ProcessName))
        {
            app.Program.ProcessName = null;
        }

        if(string.IsNullOrEmpty(ClassName))
        {
            app.Program.ClassName = null;
        }
        else
        {
            app.Program.ClassName = new List<string>(ClassName.Split('|'));
        }

        if(NotInOverviewCheck == true)
        {
            app.Program.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
        }
        else
        {
            app.Program.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
        }

        if(UsedForSessionCheck == true)
        {
            if(string.IsNullOrEmpty(AuthClassName))
            {
                app.Program.AuthClassName = null;
            }
            else
            {
                app.Program.AuthClassName = new List<string>(AuthClassName.Split('|'));
            }

            if(string.IsNullOrWhiteSpace(app.Program.Args))
            {
                app.Program.Args = null;
            }
        }
        else
        {
            app.Program.Args = null;
            app.Program.AuthClassName = null;
        }

        if (OpenInTabCheck == false)
        {
            app.Program.iFlags &= ~(ProgramConfig.FLAG_SYNCTITLE | ProgramConfig.FLAG_ENABLEHOTKEY | ProgramConfig.FLAG_CLOSE_MASK);
        }

        if(string.IsNullOrWhiteSpace(app.Program.DisplayName))
        {
            app.Program.DisplayName = null;
        }

        if(string.IsNullOrEmpty(app.Program.Arch) || app.Program.Arch == "default")
        {
            app.Program.Arch = null;
        }

        if(string.IsNullOrWhiteSpace(app.Program.CommandLine))
        {
            app.Program.CommandLine = null;
        }

        if(string.IsNullOrWhiteSpace(app.Program.WorkingDir))
        {
            app.Program.WorkingDir = null;
        }

        string flags = Flags.Replace('&', '|');
        if(flags == "default")
        {
            flags = null;
        }

        if(ProcessMode != "child")
        {
            if(string.IsNullOrWhiteSpace(flags))
            {
                flags = ProcessMode;
            } else
            {
                flags += "|" + ProcessMode;
            }
        }

        if(string.IsNullOrWhiteSpace(flags))
        {
            app.Program.Flags = null;
        }
        else
        {
            app.Program.Flags = new List<string>(flags.Split('|'));
        }

        if(SelectedApp == null)
        {
            App.Sessions.Sessions.Add(app);
        }
        else
        {
            SelectedApp.OnPropertyChanged("DisplayName");
            SelectedApp.OnPropertyChanged("CredentialName");
            SelectedApp.OnPropertyChanged("NameTooltip");
            SelectedApp.SessionHistory?.OnPropertyChanged("DisplayName");
            App.RefreshOverview();			
        }

        JumpListManager.SetNewJumpList(App.Sessions.Sessions);

        return app;
    }

    private bool NameHasExisted(string name)
    {
        return App.Sessions.Sessions.FirstOrDefault((Session s) => s.Name == name && s.Type == "app" && s.Id != EditedApp.Id) != null;
    }

    private bool InputIsValid()
    {
        if (!BatchMode && string.IsNullOrWhiteSpace(ExePath))
        {
            AddError("ExePath", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["Path"]));
            return !HasErrors;
        }
        RemoveError("ExePath");

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = null;
        }
        else
        {
            DisplayName = DisplayName.Trim();
        }

        if(BatchMode)
        {
            RemoveError("Name");
            return !HasErrors; 
        }

        ExePath = ExePath.Trim();

        if (string.IsNullOrWhiteSpace(Name))
        {
            int num = 2;
            string applicationName = Path.GetFileNameWithoutExtension(ExePath).ToLower();
            if(string.IsNullOrWhiteSpace(applicationName))
            {
                applicationName = ExePath.ToLower();
            }
            string name = applicationName;

            while (NameHasExisted(name))
            {
                name = applicationName + " (" + num + ")";
                num++;
            }

            Name = name;

            RemoveError("ApplicationName");
            return true;
        }

        Name = Name.Trim();
        if (NameHasExisted(Name))
        {
            if (!NewMode)
            {
                AddError("Name", string.Format(Application.Current.Resources["Exist"] as string, Application.Current.Resources["AppName"]));
                return !HasErrors;
            }
            else
            {
                string name = Name;
                int num = 2;
                while (NameHasExisted(name))
                {
                    name = Name + " (" + num + ")";
                    num++;
                }
                Name = name;
            }
        }
        RemoveError("Name");

        return true;
    }

    private void HideNotifications()
    {
        RemoveError("Name");
        RemoveError("ExePath");
        RemoveError("ApplicationName");
    }

    public void UpdateGUI(Visibility controlVisibility = Visibility.Visible)
    {
        ControlVisible = controlVisibility == Visibility.Visible;
        NotifyPropertyChanged("TitleBackground");
        NotifyPropertyChanged("NewMode");
        NotifyPropertyChanged("BatchMode");
        NotifyPropertyChanged("EditMode");
        NotifyPropertyChanged("ControlVisible");
        NotifyPropertyChanged("Name");
        NotifyPropertyChanged("ExePath");
        NotifyPropertyChanged("DisplayName");
        NotifyPropertyChanged("ArchList");
        NotifyPropertyChanged("Arch");
        NotifyPropertyChanged("CommandLine");
        NotifyPropertyChanged("WorkingDir");
        NotifyPropertyChanged("ProcessModeList");
        NotifyPropertyChanged("ProcessMode");
        NotifyPropertyChanged("ProcessName");
        NotifyPropertyChanged("ClassName");
        NotifyPropertyChanged("NotInOverviewCheckThree");
        NotifyPropertyChanged("NotInOverviewCheck");
        NotifyPropertyChanged("UsedForSessionCheckThree");
        NotifyPropertyChanged("UsedForSessionCheck");
        NotifyPropertyChanged("Args");
        NotifyPropertyChanged("AuthClassName");
        NotifyPropertyChanged("OpenInTabCheckThree");
        NotifyPropertyChanged("OpenInTabCheck");
        NotifyPropertyChanged("SyncTitleCheckThree");
        NotifyPropertyChanged("SyncTitleCheck");
        NotifyPropertyChanged("EnableHotkeyCheckThree");
        NotifyPropertyChanged("EnableHotkeyCheck");
        NotifyPropertyChanged("MethodList");
        NotifyPropertyChanged("Method");
        NotifyPropertyChanged("FlagsList");
        NotifyPropertyChanged("Flags");
        NotifyPropertyChanged("CloseIMECheckThree");
        NotifyPropertyChanged("CloseIMECheck");
        NotifyPropertyChanged("Monitors");
        NotifyPropertyChanged("Monitor");
        NotifyPropertyChanged("Comment");
        NotifyPropertyChanged("SelectedColor");
        UpdateColorListVisibility();
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
}
