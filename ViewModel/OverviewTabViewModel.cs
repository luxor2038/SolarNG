using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using log4net;
using SolarNG.Configs;
using SolarNG.Sessions;
using SolarNG.Utilities;


namespace SolarNG.ViewModel;

public static class StringExtensions
{
    public static string Replace(this string source, string oldValue, string newValue, StringComparison comparisonType)
    {
        if (source == null) return null;
        if (string.IsNullOrEmpty(oldValue)) return source;
        
        int index = source.IndexOf(oldValue, comparisonType);
        if (index < 0) return source;
        
        StringBuilder result = new StringBuilder();
        int pos = 0;
        
        while (index >= 0)
        {
            result.Append(source, pos, index - pos);
            result.Append(newValue);
            pos = index + oldValue.Length;
            index = source.IndexOf(oldValue, pos, comparisonType);
        }
        
        result.Append(source, pos, source.Length - pos);
        return result.ToString();
    }
}

public class OverviewTabViewModel : TabBase
{
    public override bool OrderByCommandVisible => string.IsNullOrEmpty(Type);
    
    public void OrderBy()
    {
        App.Config.GUI.OverviewOrderBy = OverviewOrderBy = NextOverviewOrderBy;

        NextOverviewOrderBy = GetNextOverviewOrderBy(NextOverviewOrderBy);

        RaisePropertyChanged("Menu_OrderBy");

        RefreshOverview(null, null);
    }

    private string OverviewOrderBy;
    private string NextOverviewOrderBy;
    private string GetNextOverviewOrderBy(string OverviewOrderBy)
    {
        if (OverviewOrderBy == "Name")
        {
            return "Counter";
        } 
        else if (OverviewOrderBy == "Counter")
        {
            return "OpenTime";
        }

        return "Name";
    }

    public override string Menu_OrderBy => string.Format(Application.Current.Resources["OrderBy"] as string, NextOverviewOrderBy);

    public void KickAll()
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            base.MainWindow.MainWindowVM.KickAllTab();
        });
    }

    public ObservableCollection<Session> AllSessions => App.Sessions.Sessions;

    public ObservableCollection<Credential> AllCredentials => App.Sessions.Credentials;

    private CancellationTokenSource _cancellationTokenSource;
    private readonly object _lockObject = new object();

    private async void AyncFilterSessions(string str)
    {
        try
        {
            lock (_lockObject)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            await Task.Delay(200, cancellationToken);

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    FilterSessions(str);
                });
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }
    }

    private string _ByUserTypedSession;
    public string ByUserTypedSession
    {
        get
        {
            return _ByUserTypedSession;
        }
        set
        {
            _ByUserTypedSession = value;
            RaisePropertyChanged("ByUserTypedSession");

            AyncFilterSessions(ByUserTypedSession);
            
            if (base.HasErrors)
            {
                ValidateProperty("ByUserTypedSession", value);
            }
        }
    }

    private SynchronizationContext uiContext;
    public RelayCommand TypedSessionOpenCommand { get; set; }
    private void TypedSessionOpen()
    {
        if (SelectedSession != null)
        {
            SelectedSessionOpen();
            return;
        }
        ValidateProperty("ByUserTypedSession", ByUserTypedSession);

        if (base.HasErrors || string.IsNullOrWhiteSpace(ByUserTypedSession) || !string.IsNullOrEmpty(Type))
        {
            return;
        }

        if (!base.HasErrors && !string.IsNullOrWhiteSpace(ByUserTypedSession) && ByUserTypedSession.ToLower() == "settings")
        {
            base.MainWindow.MainWindowVM.OnOpenSettings();
            return;
        }

        Session newSession = SessionParser.TryParseSession(ByUserTypedSession);
        if (newSession == null)
        {
            return;
        }

        int num = 2;
        string name0 = string.IsNullOrEmpty(newSession.Name) ? newSession.Ip : newSession.Name;
        string name = name0;
        while (App.Sessions.Sessions.FirstOrDefault((Session s) => s.Name == name && s.Type == newSession.Type) != null)
        {
            name = name0 + " (" + num + ")";
            num++;
        }
        newSession.Name = name;

        Credential credential = null;
        if((newSession.SessionTypeFlags & SessionType.FLAG_CREDENTIAL)!=0)
        {
            credential = SessionParser.ParseCredential(ByUserTypedSession);
            if (credential != null)
            {
                num = 2;
                string Name = credential.Username + "@" + newSession.Ip;
                name = Name;
                while (AllCredentials.FirstOrDefault((Credential c) => c.Name == name) != null)
                {
                    name = Name + " (" + num + ")";
                    num++;
                }
                credential.Name = name;

                newSession.CredentialId = credential.Id;
            }
        }

        if(App.Config.GUI.AutoSaveQuickNew)
        {
            if (credential != null)
            {
                AllCredentials.Add(credential);
            }
            AllSessions.Add(newSession);
            JumpListManager.SetNewJumpList(AllSessions);
        }
        AppTabViewModel appTabViewModel = MainWindow.MainWindowVM.CreateAppTab(newSession, credential, base.MainWindow);

        if(App.Config.GUI.AutoSaveQuickNew)
        { 
            uiContext = SynchronizationContext.Current;
            appTabViewModel.AppProcessExited += delegate(object sender, EventArgs e)
            {
                RemoveWrongSession(sender, e, newSession);
            };
        }

        if (App.Config.GUI.AutoCloseOverview)
        {
            MainWindow.MainWindowVM.SwitchTabs(appTabViewModel);
        }
        else
        {
            MainWindow.MainWindowVM.AddNewTab(appTabViewModel);
        }
     }

    private void RemoveWrongSession(object sender, EventArgs args, Session newSession)
    {
        if (sender is Process process && process.ExitCode > 0)
        {
            uiContext.Send(delegate
            {
                AllSessions.Remove(newSession);
                JumpListManager.SetNewJumpList(AllSessions);
            }, null);
        }
    }

    public RelayCommand SessionAddCommand { get; set; }
    private void SessionAdd()
    {
        if (string.IsNullOrEmpty(Type))
        {
            Session session = SessionParser.TryParseSession(ByUserTypedSession);
            Credential credential = null;
            if(session != null && (session.SessionTypeFlags & SessionType.FLAG_CREDENTIAL) != 0)
            {
                credential = SessionParser.ParseCredential(ByUserTypedSession);
            }

            base.MainWindow.MainWindowVM.AddNewSessionTab(session, credential);
            return;
        }

        if(Type == "process")
        { 
            GetAllProcesses();
        }
        else if(Type == "window")
        {
            GetAllWindows();
        }
        else if(Type == "shortcut")
        {
            GetAllShortcuts();
        }

        FilterSessions(ByUserTypedSession);
    }

    private Session _SelectedSession;
    public Session SelectedSession
    {
        get
        {
            _SelectedSession ??= FilteredSessions.FirstOrDefault();
            return _SelectedSession;
        }
        set
        {
            _SelectedSession = value;
        }
    }

    public RelayCommand SelectedSessionOpenCommand { get; set; }
    private void SelectedSessionOpen()
    {
        if(SelectedSession.Type == "tag")
        {
            if(SelectedSession.Name == "..")
            {
                currentTag = TagStack.Pop();
            } 
            else
            {
                TagStack.Push(currentTag);
                currentTag = SelectedSession;
            }

            RefreshAllSessionsByTag();

            if((SelectedSession.Name == ".." || SelectedSession.Name == "...") && (ByUserTypedSession == ".." || ByUserTypedSession == "..."))
            {
                ByUserTypedSession = "";
            }
            else
            {
                FilterSessions(ByUserTypedSession);
            }
            return;
        }

        Session session = SelectedSession;
        if(SelectedSession.Type == "lnk")
        {
            try
            {
                string programfiles = ProgramConfig.ExpandEnvironmentVariables("%programfilesnative%");
                if(!programfiles.EndsWith("\\"))
                {
                    programfiles += "\\";
                }
                if (!string.IsNullOrEmpty(session.Program.DisplayName) && (session.Program.DisplayName.StartsWith(programfiles, StringComparison.OrdinalIgnoreCase) || session.Program.DisplayName.IndexOf("\\System32\\", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    string explorer = ProgramConfig.ExpandEnvironmentVariables("%windir%\\explorer.exe");
                    ProcessStartInfo processStartInfo = new ProcessStartInfo(explorer)
                    {
                        Arguments = "\"" + session.Program.Path + "\"",
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }
                else
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo(session.Program.Path)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }

            }
            catch (Exception exception)
            {
                log.Warn("Failed to start process", exception);
            }
            return;
        }

        if(SelectedSession.Type == "history")
        {
            if(SelectedSession.Tab != null)
            {
                MainWindow.MainWindowVM.SelectTab(SelectedSession.Tab);
                return;
            }
            else
            {
                session = SelectedSession.HistorySession;
            }
        }

        Credential credential = AllCredentials.FirstOrDefault((Credential c) => c.Id == session.CredentialId);
        TabBase tab = MainWindow.MainWindowVM.CreateAppTab(session, credential, base.MainWindow);
        if(App.Config.GUI.AutoCloseOverview && SelectedSession.Type != "history")
        {
            MainWindow.MainWindowVM.SwitchTabs(tab);
        }
        else
        {
            MainWindow.MainWindowVM.AddNewTab(tab);
        }
    }

    public void OnDoubleClick(Session session)
    {
        SelectedSession = session;
        SelectedSessionOpen();
    }

    public RelayCommand DeleteSessionCommand { get; set; }
    private void DeleteSession()
    {
        MainWindow mainWindow = base.MainWindow;
        Session session = SelectedSession;
        string sessionType = Application.Current.Resources["Session"] as string;
        if(session.Type == "app")
        {
            sessionType = Application.Current.Resources["Application"] as string;
        } 
        else if(session.Type == "tag")
        {
            sessionType = Application.Current.Resources["Tag"] as string;
        }
        else if(session.Type == "proxy")
        {
            sessionType = Application.Current.Resources["Proxy"] as string;
        }

        DeleteConfirmationDialog deleteConfirmationDialog = new DeleteConfirmationDialog(mainWindow, string.Format(Application.Current.Resources["DeleteSession"] as string, (session != null) ? session.Name : "", sessionType.ToLower())) { Topmost = true };
        deleteConfirmationDialog.Focus();
        deleteConfirmationDialog.Closing += delegate(object sender, CancelEventArgs e)
        {
            if ((sender as DeleteConfirmationDialog).Confirmed && SelectedSession != null)
            {
                if(SelectedSession.Type == "tag")
                {
                    foreach (Session session in SelectedSession.ChildSessions)
                    {
                        session.Tags.Remove(SelectedSession.Name);
                        if(session.Tags.Count == 0)
                        {
                            session.Tags = null;
                        }
                    }
                }

                if(SelectedSession.Type == "proxy" || SelectedSession.Type == "ssh")
                {
                    if((session.SessionTypeFlags & SessionType.FLAG_PROXY_PROVIDER)!=0)
                    {
                        foreach(Session item in AllSessions.Where(s => s.ProxyId == SelectedSession.Id))
                        {
                            item.ProxyId = Guid.Empty;
                        }
                    }
                }

                foreach(Session tag in AllSessions.Where(s => s.ChildSessions.Contains(SelectedSession)))
                {
                    tag.ChildSessions.Remove(SelectedSession);
                }

                if(session.SessionHistory != null)
                {
                    App.HistorySessions.Remove(session.SessionHistory);
                }

                AllSessions.Remove(SelectedSession);
                JumpListManager.SetNewJumpList(AllSessions);
            }
        };
        deleteConfirmationDialog.ShowDialog();
    }

    public RelayCommand<object> DeleteItemsCommand { get; set; }
    private void OnDeleteItems(object array)
    {
        DeleteSession();
    }

    public RelayCommand EditSessionCommand { get; set; }
    private void EditSession()
    {
        Session session = SelectedSession;
        if(SelectedSession.Type == "lnk")
        { 
            session = new Session("app");
            session.Name = SelectedSession.Name;
            session.Program.Path = SelectedSession.Program.DisplayName;
            session.Program.WorkingDir = SelectedSession.Program.WorkingDir;
            session.Program.CommandLine = SelectedSession.Program.CommandLine;

            if(!string.IsNullOrEmpty(session.Program.Path))
            {
                string programfiles = ProgramConfig.ExpandEnvironmentVariables("%programfilesnative%");
                if(!programfiles.EndsWith("\\"))
                {
                    programfiles += "\\";
                }
                if(session.Program.Path.StartsWith(programfiles, StringComparison.OrdinalIgnoreCase))
                {
                    session.Program.Path = "%programfilesnative%\\" + session.Program.Path.Substring(programfiles.Length);
                }
                if(session.Program.Path.IndexOf("\\System32\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    session.Program.Path = session.Program.Path.Replace("\\System32\\", "\\%sysnative%\\", StringComparison.OrdinalIgnoreCase);
                }
            }

            base.MainWindow.MainWindowVM.OpenSettingsTab(session, null, true);
            return;
        }

        if(SelectedSession.Type == "history" && !SelectedSession.HistorySession.SessionTypeIsNormal && SelectedSession.HistorySession.Type != "app" && SelectedSession.HistorySession.Type != "proxy" )
        {
            return;
        }

        if(SelectedSession.Type == "history")
        {
            session = SelectedSession.HistorySession;
        }
        base.MainWindow.MainWindowVM.OpenSettingsTab(session, null, false);
    }

    public void PinOrUnpin(MenuItem item)
    {
        if ((SelectedSession.iFlags & ProgramConfig.FLAG_PINNED) == 0)
        {
            item.Header = Application.Current.Resources["Pin"];
        }
        else
        {
            item.Header = Application.Current.Resources["Unpin"];
        }
    }

    public RelayCommand PinSessionCommand { get; set; }
    private void PinSession()
    {
        if ((SelectedSession.iFlags & ProgramConfig.FLAG_PINNED) == 0)
        {
            SelectedSession.iFlags |= ProgramConfig.FLAG_PINNED;
        }
        else
        {
            SelectedSession.iFlags &= ~ProgramConfig.FLAG_PINNED;
        }
        FilterSessions(ByUserTypedSession);
    }

    public RelayCommand NoMaximizeCommand { get; set; }
    private void NoMaximize()
    {
        SelectedSession.Flags = new List<string>
        {
            "nomaximize"
        };
        SelectedSessionOpen();
    }

    public RelayCommand NoResizeCommand { get; set; }
    private void NoResize()
    {
        SelectedSession.Flags = new List<string>
        {
            "noresize"
        };
        SelectedSessionOpen();
    }

    public RelayCommand KeepParentCommand { get; set; }
    private void KeepParent()
    {
        SelectedSession.Flags = new List<string>
        {
            "keepparent"
        };
        SelectedSessionOpen();
    }

    public RelayCommand KeepParentNoResizeCommand { get; set; }
    private void KeepParentNoResize()
    {
        SelectedSession.Flags = new List<string>
        {
            "keepparent",
            "noresize"
        };
        SelectedSessionOpen();
    }

    private Session currentTag;
    private Stack<Session> TagStack = new Stack<Session>();

    private ObservableCollection<Session> _AllSessionsByTag;
    private ObservableCollection<Session> AllSessionsByTag
    {
        get
        {
            return _AllSessionsByTag;
        }
        set
        {
            _AllSessionsByTag = value;
            base.TabName = (string)Application.Current.Resources["Overview"] + " (" + value?.Count + ")";
        }
    }

    private void RefreshAllSessionsByTag()
    {
        if(currentTag == null)
        {
            AllSessionsByTag = AllSessions;
            return;
        }

        if(!AllSessions.Contains(currentTag))
        {
            AllSessionsByTag = AllSessions;
            currentTag = null;
            TagStack.Clear();
            return;
        }

        AllSessionsByTag = new ObservableCollection<Session>(currentTag.ChildSessions);
    }

    public ObservableCollection<Session> FilteredSessions { get; set; }
    private void FilterSessions(string text)
    {
        FilteredSessions = new ObservableCollection<Session>();

        if (Type == "process" || Type == "window")
        {
            if(string.IsNullOrWhiteSpace(text))
            {
                FilteredSessions = AllItems;
            }
            else
            {
                foreach (Session session in AllItems)
                {
                    if (session.Matches(text))
                    {
                        FilteredSessions.Add(session);
                    }
                }
            }

            RaisePropertyChanged("FilteredSessions");
            RaisePropertyChanged("SelectedSession");
            return;
        }

        if (Type == "history")
        {
            if(string.IsNullOrWhiteSpace(text))
            {
                FilteredSessions = AllItems;
            }
            else
            {
                foreach (Session session in AllItems)
                {
                    if (session.Matches(text))
                    {
                        FilteredSessions.Add(session);
                    }
                }
            }

            RaisePropertyChanged("FilteredSessions");
            RaisePropertyChanged("SelectedSession");
            return;
        }

        if (Type == "shortcut")
        {
            if(string.IsNullOrWhiteSpace(text))
            {
                FilteredSessions = AllItems;
            }
            else
            {
                foreach (Session session in AllItems)
                {
                    if (session.Matches(text))
                    {
                        FilteredSessions.Add(session);
                    }
                }
            }
            
            RaisePropertyChanged("FilteredSessions");
            RaisePropertyChanged("SelectedSession");
            return;
        }

        if(currentTag != null)
        {
            Session session  = new Session("tag") { Id = Guid.Empty, Name = ".." };

            Session upLevelTag = TagStack.Peek();
            if(upLevelTag != null)
            {
                session.UpLevelTag = upLevelTag;
            }
            session.Tags = new ObservableCollection<string>();

            Stack<Session> stack = new Stack<Session>(TagStack);

            while(stack.Count > 0)
            {
                Session tag = stack.Pop();
                if(tag != null)
                {
                    session.Tags.Add(tag.Name);
                }
            }

            session.Tags.Add(currentTag.Name);

            if (session.Matches(text))
            {
                FilteredSessions.Add(session);
            }
        }

        if (OverviewOrderBy == "Counter")
        {
            foreach (Session session in from s in AllSessionsByTag
                where (s.iFlags2 & ProgramConfig.FLAG_NOTINOVERVIEW) == 0 && (s.Type != "tag" || (s.Type == "tag" && s.ChildSessionsCount > 0))
                orderby (s.iFlags & ProgramConfig.FLAG_PINNED) == 0, s.Type!="tag", s.OpenCounter descending, s.Name
                select s)
            {
                if (session.Matches(text))
                {
                    FilteredSessions.Add(session);
                }
            }

            RaisePropertyChanged("FilteredSessions");
            RaisePropertyChanged("SelectedSession");
            return;
        }

        if (OverviewOrderBy == "OpenTime")
        {
            foreach (Session session in from s in AllSessionsByTag
                where (s.iFlags2 & ProgramConfig.FLAG_NOTINOVERVIEW) == 0 && (s.Type != "tag" || (s.Type == "tag" && s.ChildSessionsCount > 0))
                orderby (s.iFlags & ProgramConfig.FLAG_PINNED) == 0, s.Type!="tag", s.OpenTime descending, s.Name
                select s)
            {
                if (session.Matches(text))
                {
                    FilteredSessions.Add(session);
                }
            }

            RaisePropertyChanged("FilteredSessions");
            RaisePropertyChanged("SelectedSession");
            return;
        }

        foreach (Session session in from s in AllSessionsByTag
                                    where (s.iFlags2 & ProgramConfig.FLAG_NOTINOVERVIEW) == 0 && (s.Type != "tag" || (s.Type == "tag" && s.ChildSessionsCount > 0))
                                    orderby (s.iFlags & ProgramConfig.FLAG_PINNED) == 0, s.Type != "tag", s.Name
                                    select s)
        {
            if (session.Matches(text))
            {
                FilteredSessions.Add(session);
            }
        }

        RaisePropertyChanged("FilteredSessions");
        RaisePropertyChanged("SelectedSession");
    }

    public ObservableCollection<Session> AllItems { get; set; }
    private void GetAllProcesses()
    {
        AllItems = new ObservableCollection<Session>();
        Process[] processes = Process.GetProcesses();
        foreach (Process process in processes)
        {
            try
            {
                if (!(process.Handle != IntPtr.Zero) || !(process.MainWindowHandle != IntPtr.Zero) || process.Threads[0].WaitReason == ThreadWaitReason.Suspended)
                {
                    continue;
                }
                StringBuilder stringBuilder = new StringBuilder(256);
                if (Win32.GetClassName(process.MainWindowHandle, stringBuilder, stringBuilder.Capacity) == 0)
                {
                    continue;
                }
                string fileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
                string text = stringBuilder.ToString();
                if ((!text.Contains(fileName) || !text.Contains("HwndWrapper[")) && !App.Config.GUI.ExcludeClasses.Contains(text) && (Win32.GetWindowLong(process.MainWindowHandle, Win32.GWL_STYLE) & Win32.WS_VISIBLE) != 0 && Win32.GetWindowRect(process.MainWindowHandle, out var lpRect) && lpRect.Right != lpRect.Left && lpRect.Bottom != lpRect.Top)
                {
                    if (App.WindowHandleManager.ContainsKey(process.MainWindowHandle))
                    {
                        continue;
                    }

                    Session session = new Session("process")
                    {
                        Name = process.ProcessName + "(" + process.Id + ")",
                        Pid = process.Id,
                        ClassName = stringBuilder.ToString(),
                        Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true))
                    };

                    if(session.ClassName == "ApplicationFrameWindow")
                    {
                        session.Flags = new List<string>
                        {
                            "keepparent"
                        };
                    }

                    if (Win32.GetWindowText(process.MainWindowHandle, stringBuilder, stringBuilder.Capacity) != 0)
                    {
                        session.Title = stringBuilder.ToString();
                    }
                    AllItems.Add(session);
                }
            }
            catch (Exception)
            {
            }
        }
        AllItems = new ObservableCollection<Session>(AllItems.OrderBy((Session s) => s.Name));
        FilteredSessions = AllItems;
        base.TabName = (string)Application.Current.Resources["Process"] + " (" + AllItems.Count + ")";
    }

    private void GetAllWindows()
    {
        AllItems = new ObservableCollection<Session>();
        IntPtr window = Win32.GetTopWindow(IntPtr.Zero);

        while(window != IntPtr.Zero)
        {
            window = Win32.GetWindow(window, Win32.GW_HWNDNEXT);
            if(window == IntPtr.Zero)
            {
                break;
            }

            uint ws = Win32.GetWindowLong(window, Win32.GWL_STYLE);

            if((ws & Win32.WS_VISIBLE) == 0 || (ws & Win32.WS_POPUP) != 0)
            {
                continue;
            }
            if(!Win32.GetWindowRect(window, out var lpRect))
            {
                continue;
            }
            if(lpRect.Left == lpRect.Right || lpRect.Top == lpRect.Bottom)
            {
                continue;
            }

            Win32.GetWindowThreadProcessId(window, out uint pid);

            try 
            { 
                Process process = Process.GetProcessById((int)pid);

                if (process.Handle == IntPtr.Zero || process.Threads[0].WaitReason == ThreadWaitReason.Suspended)
                {
                    continue;
                }
                StringBuilder stringBuilder = new StringBuilder(256);
                if (Win32.GetClassName(window, stringBuilder, stringBuilder.Capacity) == 0)
                {
                    continue;
                }
                string fileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
                string text = stringBuilder.ToString();
                if ((text.Contains(fileName) && text.Contains("HwndWrapper[")) || App.Config.GUI.ExcludeClasses.Contains(text))
                {
                    continue;
                }

                if (App.WindowHandleManager.ContainsKey(window))
                {
                    continue;
                }

                Session session = new Session("window")
                {
                    Name = process.ProcessName + "(" + process.Id + ")-" + (uint)window,
                    Pid = process.Id,
                    ClassName = stringBuilder.ToString(),
                    hWindow = window,
                    Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true))
                };

                if(session.ClassName == "ApplicationFrameWindow")
                {
                    session.Flags = new List<string>
                    {
                        "keepparent"
                    };
                }

                if(session.ClassName == "CabinetWClass")
                {
                    Version win10 = new Version(10, 0);
                    if (App.OSVersion >= win10) 
                    {
                        session.Flags = new List<string>
                        {
                            "keepparent"
                        };
                    }
                }

                if (Win32.GetWindowText(window, stringBuilder, stringBuilder.Capacity) != 0)
                {
                    session.Title = stringBuilder.ToString();
                }
                AllItems.Add(session);
            }
            catch (Exception)
            {
            }
        }

        AllItems = new ObservableCollection<Session>(AllItems.OrderBy((Session s) => s.Name));
        FilteredSessions = AllItems;
        base.TabName = (string)Application.Current.Resources["Window"] + " (" + AllItems.Count + ")";
    }

    private void GetAllHistories()
    {
        AllItems = App.HistorySessions;
        AllItems = new ObservableCollection<Session>(AllItems.OrderBy((Session s) => s.Tab == null).ThenByDescending((Session s) => s.OpenTime));
        FilteredSessions = AllItems;
        base.TabName = (string)Application.Current.Resources["History"] + " (" + AllItems.Count + ")";
    }

    private void GetAllShortcuts()
    {
        AllItems = new ObservableCollection<Session>();
        ShortcutScanner scanner = new ShortcutScanner();

        foreach (ShortcutInfo shortcutInfo in scanner.ScanAllLocations())
        {
            if(shortcutInfo.IsDirectory)
            {
                continue;
            }
            bool isExcluded = false;
            foreach (string str in App.Config.GUI.ExcludeShortcuts)
            {
                if(shortcutInfo.Name.IndexOf(str, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    isExcluded = true;
                    break;
                }
                if(shortcutInfo.AppPath.IndexOf(str, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    isExcluded = true;
                    break;
                }
            }
            if(isExcluded)
            {
                continue;
            }

            Session session = new Session("lnk");
            session.Name = shortcutInfo.Name;
            session.Program.Path = shortcutInfo.Path;
            session.Program.CommandLine = shortcutInfo.Arguments;
            session.Program.WorkingDir = shortcutInfo.WorkingDirectory;
            session.Program.DisplayName = shortcutInfo.AppPath;
            session.Color = (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));

            AllItems.Add(session);
        }
        AllItems = new ObservableCollection<Session>(AllItems.OrderBy((Session s) => s.Name));
        FilteredSessions = AllItems;
        base.TabName = (string)Application.Current.Resources["Shortcut"] + " (" + AllItems.Count + ")";
    }

    public string Type;

    public OverviewTabViewModel(MainWindow mainWindow, string type = "") : base(mainWindow)
    {
        OverviewOrderBy = App.Config.GUI.OverviewOrderBy;
        NextOverviewOrderBy = GetNextOverviewOrderBy(OverviewOrderBy);
        OrderByCommand = new RelayCommand(OrderBy);
        KickAllCommand = new RelayCommand(KickAll);
        TypedSessionOpenCommand = new RelayCommand(TypedSessionOpen);
        SessionAddCommand = new RelayCommand(SessionAdd);
        SelectedSessionOpenCommand = new RelayCommand(SelectedSessionOpen);
        DeleteSessionCommand = new RelayCommand(DeleteSession);
        DeleteItemsCommand = new RelayCommand<object>(OnDeleteItems);
        EditSessionCommand = new RelayCommand(EditSession);
        PinSessionCommand = new RelayCommand(PinSession);
        NoMaximizeCommand = new RelayCommand(NoMaximize);
        NoResizeCommand = new RelayCommand(NoResize);
        KeepParentCommand = new RelayCommand(KeepParent);
        KeepParentNoResizeCommand = new RelayCommand(KeepParentNoResize);

        Type = type;

        base.TabPathVisibility = Visibility.Visible;

        if (Type == "process")
        {
            base.TabPath = Application.Current.Resources["ProcessPath"] as Geometry;
            GetAllProcesses();
            return;
        }

        if (Type == "window")
        {
            base.TabPath = Application.Current.Resources["WindowPath"] as Geometry;
            GetAllWindows();
            return;
        }

        if (Type == "history")
        {
            base.TabPath = Application.Current.Resources["HistoryPath"] as Geometry;
            GetAllHistories();

            App.HistorySessions.CollectionChanged += UpdateHistorySessions;
            return;
        }

        if (Type == "shortcut")
        {
            base.TabPath = Application.Current.Resources["ShortcutPath"] as Geometry;
            GetAllShortcuts();
            return;
        }

        base.TabPath = Application.Current.Resources["OverviewPath"] as Geometry;

        UpdateSessions(null, null);
        
        AllSessions.CollectionChanged += UpdateSessions;

        App.RefreshOverviewHandler += RefreshOverview;
    }

    public override void Cleanup()
    {
        if (Type == "history") 
        {
            App.HistorySessions.CollectionChanged -= UpdateHistorySessions;
        }
        else if(string.IsNullOrEmpty(Type))
        {
            App.RefreshOverviewHandler -= RefreshOverview;
            AllSessions.CollectionChanged -= UpdateSessions;

            foreach (Session tag in AllSessions.Where(s => s.Type == "tag"))
            {
                tag.ChildSessions.CollectionChanged -= UpdateSessions2;
            }
        }
        base.Cleanup();
    }

    private void UpdateHistorySessions(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        GetAllHistories();
        FilterSessions(ByUserTypedSession);
    }

    private void UpdateSessions(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        if(string.IsNullOrEmpty(Type))
        {
            foreach (Session tag in AllSessions.Where(s => s.Type == "tag"))
            {
                tag.ChildSessions.CollectionChanged -= UpdateSessions2;
                tag.ChildSessions.CollectionChanged += UpdateSessions2;
            }
        }

        UpdateSessions2(null, null);
    }

    private void UpdateSessions2(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        RefreshAllSessionsByTag();
        FilterSessions(ByUserTypedSession);
    }
	
    private void RefreshOverview(object sender, EventArgs e)
    {
        UpdateSessions2(null, null);
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
