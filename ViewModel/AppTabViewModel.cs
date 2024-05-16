using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using log4net;
using SolarNG.Configs;
using SolarNG.Sessions;
using SolarNG.Utilities;

namespace SolarNG.ViewModel;

public class AppTabViewModel : TabBase
{
    public override bool DuplicateTabCommandVisible => (Session.SessionTypeIsNormal || Session.Type == "app" || Session.Type == "proxy") && !IsDisconnected;
    public void DuplicateTab()
    {
        MainWindow.MainWindowVM.AddNewTab( MainWindow.MainWindowVM.CreateAppTab(ProxySession??Session, Credential, base.MainWindow, Protocol));
    }

    public override bool ReconnectCommandVisible => Session.SessionTypeIsNormal;
    public void Reconnect()
    {
        if (IsSSHv2ShareMainTab())
        {
            ConfirmationDialog confirmationDialog = new ConfirmationDialog(base.MainWindow, System.Windows.Application.Current.Resources["Reconnect"] as string, string.Format(System.Windows.Application.Current.Resources["Reconnecting"] as string, base.TabName)) { Topmost = true };
            confirmationDialog.Focus();
            confirmationDialog.ShowDialog();
            if (!confirmationDialog.Confirmed)
            {
                return;
            }
        }
        Task.Run(delegate
        {
            System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                Disconnect();
                Connect(BuildArguments());
            });
        });
    }

    public override bool EditCommandVisible => true;
    public void EditSession()
    {
        if(!App.Sessions.Sessions.Contains(Session))
        {
            return;
        }

        base.MainWindow.MainWindowVM.OpenSettingsTab(Session, null, false);
    }

    public override bool SFTPCommandVisible => (Session.Type == "ssh") && string.IsNullOrEmpty(Protocol) && ProxySession == null && File.Exists(App.Config.WinSCP.FullPath);
    public void WinSCP_SFTP()
    {
        NewWinSCPTab("sftp");
    }

    private void NewWinSCPTab(string protocol)
    {
        MainWindow.MainWindowVM.AddNewTab(MainWindow.MainWindowVM.CreateAppTab(Session, Credential, base.MainWindow, protocol));
    }

    public override bool SCPCommandVisible => (Session.Type == "ssh") && string.IsNullOrEmpty(Protocol) && ProxySession == null && File.Exists(App.Config.WinSCP.FullPath);
    public void WinSCP_SCP()
    {
        NewWinSCPTab("scp");
    }

    public override bool SwitchWindowTitleBarCommandVisible
    {
        get
        {
            if (AppProcess == null)
            {
                return false;
            }
            if (CurProgram.Flags.Contains("keepparent"))
            {
                return false;
            }
            if (!AppProcess.HasExited && IsInTab)
            {
                return true;
            }
            return false;
        }
    }
    public void SwitchWindowTitleBar()
    {
        _SwitchWindowTitleBar();
    }

    private bool _SwitchWindowTitleBar()
    {
        if (AppProcess.HasExited || !IsInTab || CurProgram.WindowStyleMask == 0 || AppWin == IntPtr.Zero || CurProgram.Flags.Contains("keepparent"))
        {
            return true;
        }
        uint num = Win32.GetWindowLong(AppWin, Win32.GWL_STYLE);
        if ((num & Win32.WS_MINIMIZE) != 0)
        {
            return true;
        }
        if ((originalWindowStyle & Win32.WS_MAXIMIZE) == 0)
        {
            num &= ~Win32.WS_MAXIMIZE;
        }
        num = ((num != originalWindowStyle) ? originalWindowStyle : (originalWindowStyle & ~CurProgram.WindowStyleMask));
        if (!CurProgram.Flags.Contains("nomaximize") && !CurProgram.Flags.Contains("noresize") && (originalWindowStyle & Win32.WS_THICKFRAME) != 0)
        {
            num |= Win32.WS_MAXIMIZE;
        }
        int num2;
        uint num3 = Win32.SetWindowLong(AppWin, Win32.GWL_STYLE, num);
        for (num2 = 0; num2 < App.Config.GUI.WaitTimeout; num2++)
        {
            num3 = Win32.SetWindowLong(AppWin, Win32.GWL_STYLE, num);
            if (num3 == num)
            {
                break;
            }
            Thread.Sleep(100);
        }
        if (num3 != num)
        {
            return false;
        }
        if ((originalWindowStyle & Win32.WS_THICKFRAME) == 0 || CurProgram.Flags.Contains("noresize"))
        {
            Win32.SetWindowPos(AppWin, IntPtr.Zero, 1, 0, WindowMaxHeight, WindowMaxHeight, Win32.SWP_SHOWWINDOW);
            if (winEventHook == IntPtr.Zero)
            {
                Win32.SetWindowPos(AppWin, IntPtr.Zero, 0, 0, WindowMaxHeight, WindowMaxHeight, Win32.SWP_SHOWWINDOW);
            }
            return true;
        }
        Win32.SetWindowPos(AppWin, IntPtr.Zero, 1, 0, (Panel.Width > WindowMaxWidth) ? WindowMaxWidth : Panel.Width, (Panel.Height > WindowMaxHeight) ? WindowMaxHeight : Panel.Height, Win32.SWP_SHOWWINDOW);
        if (winEventHook == IntPtr.Zero)
        {
            Win32.SetWindowPos(AppWin, IntPtr.Zero, 0, 0, (Panel.Width > WindowMaxWidth) ? WindowMaxWidth : Panel.Width, (Panel.Height > WindowMaxHeight) ? WindowMaxHeight : Panel.Height, Win32.SWP_SHOWWINDOW);
        }
        return true;
    }

    public override bool KickCommandVisible
    {
        get
        {
            if (AppProcess == null)
            {
                return false;
            }
            if (!AppProcess.HasExited && IsInTab)
            {
                return true;
            }
            return false;
        }
    }
    public void Kick()
    {
        if (!AppProcess.HasExited && IsInTab)
        {
            KickTab();
            System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                base.MainWindow.MainWindowVM.RemoveTab(this);
            });
        }
    }

    public void KickTab()
    {
        if (AppProcess != null && !AppProcess.HasExited && IsInTab && AppProcessID != 0)
        {
            _KickTab();
            ProcessName = null;
            child = false;
            AppProcessID = 0;
        }
    }

    private bool kicked;
    private void _KickTab()
    {
        int num;

        if (CurProgram.Flags.Contains("keepparent"))
        {
            kicked = true;
            HideWin(false);
            ((Win32.ITaskbarList4)App.TaskbarList).AddTab(AppWin);
            return;
        }

        if ((Win32.GetWindowLong(AppWin, Win32.GWL_STYLE) & Win32.WS_MINIMIZE) == 0)
        {
            Win32.SetWindowLong(AppWin, Win32.GWL_STYLE, originalWindowStyle);
            for (num = 0; num < App.Config.GUI.WaitTimeout; num++)
            {
                if (Win32.SetWindowLong(AppWin, Win32.GWL_STYLE, originalWindowStyle) == originalWindowStyle)
                {
                    break;
                }
                Thread.Sleep(100);
            }
        }
        for (num = 0; num < App.Config.GUI.WaitTimeout; num++)
        {
            if (Win32.SetParent(AppWin, IntPtr.Zero) != IntPtr.Zero)
            {
                break;
            }
            Thread.Sleep(100);
        }
        kicked = true;
    }

    public void KickAll()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(delegate
        {
            base.MainWindow.MainWindowVM.KickAllTab();
        });
    }

    public override string WindowTitle => base.TabName;

    public bool IsConnected => State == ConnectionState.Connected;

    public Panel Panel { get; set; } = new Panel();
    private void Panel_Disposed(object sender, EventArgs e)
    {
        Cleanup();
    }

    public bool IsDisconnected => State == ConnectionState.Disconnected;

    private ConnectionState _State;
    private ConnectionState State
    {
        get
        {
            return _State;
        }
        set
        {
            _State = value;
            RaisePropertyChanged("IsConnected");
            RaisePropertyChanged("IsDisconnected");
            RaisePropertyChanged("IsConnecting");
            RaisePropertyChanged("DuplicateTabCommandVisible");
            RaisePropertyChanged("SwitchWindowTitleBarCommandVisible");
            RaisePropertyChanged("KickCommandVisible");
        }
    }

    public string ErrorMessage { get; set; } = "";

    private int _ExitCode;
    private int ExitCode
    {
        get
        {
            return _ExitCode;
        }
        set
        {
            _ExitCode = value;
            switch (_ExitCode)
            {
            case -1073741510:
            case 0:
                ErrorMessage = System.Windows.Application.Current.Resources["Error0"] as string;
                break;
            case 1:
                ErrorMessage = System.Windows.Application.Current.Resources["Error1"] as string;
                break;
            default:
                ErrorMessage = string.Format(System.Windows.Application.Current.Resources["Error9"] as string, _ExitCode);
                break;
            }
            RaisePropertyChanged("ErrorMessage");
        }
    }

    public RelayCommand OpenOverviewCommand { get; set; }
    private void OpenOverview()
    {
        MainWindow.MainWindowVM.SwitchTabs(MainWindow.MainWindowVM.CreateOverviewTab());
    }

    public bool IsConnecting => State == ConnectionState.Connecting;


    public event EventHandler AppProcessExited;
    public EventHandler GetAppProcessExited()
    {
        return AppProcessExited;
    }

    private void AppProcess_Exited(object sender, EventArgs e)
    {
        if (AppProcessExited != null && (ProcessName == null || IsInTab))
        {
            AppProcessExited(sender, e);
        }
    }

    public Session Session;

    public Credential Credential;

    private Session ProxySession;

    private string Protocol;

    private ProgramConfig CurProgram;

    private uint iFlags;

    private string Monitor;

    private Session HistorySession = new Session("history");

    public AppTabViewModel(MainWindow mainWindow, Session newSession, Credential newCredential, string protocol)
        : base(mainWindow)
    {
        Session = newSession;
        Credential = newCredential;
        Protocol = protocol;

        base.DuplicateTabCommand = new RelayCommand(DuplicateTab);
        base.ReconnectCommand = new RelayCommand(Reconnect);
        base.EditCommand = new RelayCommand(EditSession);
        base.SFTPCommand = new RelayCommand(WinSCP_SFTP);
        base.SCPCommand = new RelayCommand(WinSCP_SCP);
        base.SwitchWindowTitleBarCommand = new RelayCommand(SwitchWindowTitleBar);
        base.KickCommand = new RelayCommand(Kick);
        base.KickAllCommand = new RelayCommand(KickAll);
        OpenOverviewCommand = new RelayCommand(OpenOverview);

        Panel.Disposed += Panel_Disposed;
        AppProcessExited += MainWindow.MainWindowVM.RemoveTabWithExitedProcess;

        Session.OpenCounter++;
        Session.OpenTime = DateTime.Now;

        if (Session.Type == "rdp" || Session.Type == "vnc")
        {
            NeedDisableHotkey = true;
        }

        iFlags = Session.iFlags2;
        Monitor = Session.Monitor;

        if (Session.Type == "app")
        {
            CurProgram = Session.Program.Clone();
        }
        else if (Session.Type == "process")
        {
            CurProgram = new ProgramConfig("Process") { Flags = Session.Flags };
        }
        else if (Session.Type == "window")
        {
            CurProgram = new ProgramConfig("Window") { Flags = Session.Flags };
        }
        else if (Session.Type == "rdp")
        {
            CurProgram = App.Config.MSTSC.Clone();
        }
        else if (Session.Type == "vnc")
        {
            CurProgram = App.Config.VNCViewer.Clone();
        }
        else if (Session.Type == "scp" || Session.Type == "sftp" || Session.Type == "ftp")
        {
            CurProgram = App.Config.WinSCP.Clone();
            Protocol = Session.Type;
        }
        else if (Session.Type == "proxy" && Session.ProxyType != "ssh")
        {
            CurProgram = App.Config.PlinkX.Clone();

            iFlags |= CurProgram.iFlags;
        }
        else if (protocol == null)
        {
            CurProgram = App.Config.PuTTY.Clone();

            if(Session.Type == "proxy" && Session.ProxyType == "ssh")
            {
                ProxySession = Session;
                Session = Session.SSHSession;
                Credential = Session.Credential;
                iFlags = Session.iFlags2 & (~(ProgramConfig.FLAG_NOTINTAB | ProgramConfig.FLAG_CLOSE_MASK));
                iFlags |= ProxySession.iFlags2 & ProgramConfig.FLAG_NOTINTAB;
                iFlags |= CurProgram.iFlags & ProgramConfig.FLAG_CLOSE_MASK;
            }
        } 
        else
        {
            CurProgram = App.Config.WinSCP.Clone();
            iFlags = CurProgram.iFlags;
        }

        FlagsCompletion();

        if(CurProgram.WindowStyleMask == 0)
        {
            CurProgram.WindowStyleMask = App.Config.GUI.WindowStyleMask;
        }

        CurProgram.AuthClassName ??=new List<string>();
        CurProgram.ClassName ??= new List<string>();

        if(Session.Type == "window")
        {
            base.TabName = Session.Title;
        } 
        else if(Session.Type == "process")
        {
            base.TabName = Session.Name;
        }
        else if(Session.Type == "app" && !string.IsNullOrWhiteSpace(Session.Program.DisplayName))
        {
            base.TabName = Session.Program.DisplayName +" (" + Session.OpenCounter + ")";
        }
        else
        {
            if(ProxySession == null)
            {
                base.TabName = Session.Name +" (" + Session.OpenCounter + ")";
            }
            else
            {
                base.TabName = ProxySession.Name +" (" + ProxySession.OpenCounter + ")";
            }
        }

        base.TabColor = Session.Color;
        if (Session.Type == "ssh")
        {
            if (!string.IsNullOrEmpty(Protocol))
            {
                base.TabColorName = Protocol.ToUpper();
                base.TabColorNameVisibility = Visibility.Visible;
            }
            else if(ProxySession != null)
            {
                base.TabColorName = System.Windows.Application.Current.Resources[ProxySession.SessionType.AbbrDisplayName] as string;
                base.TabColorNameVisibility = Visibility.Visible;
            }
            else
            {
                base.TabColorVisibility = Visibility.Visible;
            }
        }
        else
        {
            base.TabColorName =  Session.SessionTypeIsNormal ? Session.SessionType.AbbrDisplayName : System.Windows.Application.Current.Resources[Session.SessionType.AbbrDisplayName] as string;
            base.TabColorNameVisibility = Visibility.Visible;
        }

        HistorySession.Tab = this;
        HistorySession.HistorySession = ProxySession ?? Session;

        if(HistorySession.HistorySession.OpenCounter == 1 && (Session.SessionTypeIsNormal || Session.Type == "app" || Session.Type == "proxy"))
        {
            Session historySession = new Session("history")
            {
                History = HistorySession.HistorySession.History,
                HistorySession = HistorySession.HistorySession
            };

            HistorySession.HistorySession.SessionHistory = historySession;

            App.HistorySessions.Add(historySession);
        } 

        HistorySession.HistoryName = HistorySession.HistorySession.Name;
        HistorySession.HistoryDisplayName = HistorySession.HistorySession.DisplayName;
        HistorySession.OpenCounter = HistorySession.HistorySession.OpenCounter;
        HistorySession.OpenTime = HistorySession.HistorySession.OpenTime;

        if(!string.IsNullOrEmpty(Protocol))
        {
            HistorySession.HistoryDisplayName = Protocol + HistorySession.HistoryDisplayName.Substring(3);
        }

        App.HistorySessions.Add(HistorySession);

        Task.Run(delegate
        {
            Connect(BuildArguments());
        });
    }

    private List<PipeServer> PipeServers = new List<PipeServer>();

    public override void Cleanup()
    {
        Panel.Disposed -= Panel_Disposed;
        AppProcessExited -= MainWindow.MainWindowVM.RemoveTabWithExitedProcess;

        UnregisterWindowsEventHook();

        foreach(PipeServer server in PipeServers)
        {
            server.Close();
        }
        PipeServers.Clear();

        WaitAppExitTaskCancel.Cancel();
        MonitorNewAppMainWindowTaskCancel.Cancel();

        App.WindowHandleManager.Remove(AppWin);

        if (CurProgram.Flags.Contains("keepparent") && !kicked)
        {
            HideWin(false);
            ((Win32.ITaskbarList4)App.TaskbarList).AddTab(AppWin);
        }

        try
        {
            if (((child && ProcessName != null) || ProxyByPipe) && !ProcessNotExit && (iFlags & ProgramConfig.FLAG_NOTINTAB) == 0)
            {
                ProcessHelper.KillProcessByEnv(new List<string> { InstanceId });
            }
        }
        catch (Exception exception)
        {
            log.Warn("Unable to kill process", exception);
        }
        Task.Run(delegate
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(delegate
            {
                RemoveSSHv2ShareTabs();
            });
        });

        App.HistorySessions.Remove(HistorySession);

        base.Cleanup();
    }

    private bool child;
    private void FlagsCompletion()
    {
        if (CurProgram.Flags == null) 
        {
            CurProgram.Flags = new List<string>();
            return;
        }

        if (CurProgram.Flags.Contains("keepparent"))
        {
            if (!CurProgram.Flags.Contains("keepws"))
            {
                CurProgram.Flags.Add("keepws");
            }
        }

        if (CurProgram.Flags.Contains("keepparent") || CurProgram.Flags.Contains("mstsc"))
        {
            if (!CurProgram.Flags.Contains("nomaximize")) { 
                CurProgram.Flags.Add("nomaximize");
            }
        }

        child = !CurProgram.Flags.Contains("nonchild") && !CurProgram.Flags.Contains("singleton");
    }

    private string InstanceId = Guid.NewGuid().ToString();
    private bool ByProxy;
    private bool ProxyByPipe = false;
    private bool UseHook;
    private List<string> CurProgram_Config = new List<string>();
    private SafeString PasswordToPump;

    private string Ip;
    private int Port;

    public string ProcessName;

    private string ShareKey;

    private bool SSHv2Share;

    private string BuildArguments()
    {
        string cmdline = "{0}";
        Ip = FixIpv6Address(Session.Ip);
        Port = Session.Port;
        ByProxy = CheckProxy(Session);

        if(Ip == "0.0.0.0")
        {
            System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                while (Win32.GetForegroundWindow() != base.MainWindow.Handle)
                {
                    Win32.SetForegroundWindow(base.MainWindow.Handle);
                    Thread.Sleep(100);
                }
                PromptDialog promptDialog = new PromptDialog(base.MainWindow, System.Windows.Application.Current.Resources["Connect"] as string, System.Windows.Application.Current.Resources["InputAddress"] as string, "") { Topmost = true };
                promptDialog.Focus();
                bool? flag2 = promptDialog.ShowDialog();
                if (!flag2.HasValue || !flag2.Value)
                {
                    return;
                }

                Ip = promptDialog.InputTextBox.Text;
            });

            if (SessionParser.IsPortSpecified(Ip))
            {
                Port = SessionParser.ParsePort(Ip);
                Ip = Ip.Substring(0, Ip.LastIndexOf(":", StringComparison.Ordinal));
            }

            Ip = FixIpv6Address(Ip);

            if(string.IsNullOrWhiteSpace(Ip) || Ip == "0.0.0.0")
            {
                CloseSelf();
                return null;
            }
        }

        if(CurProgram.Config != null)
        {
            CurProgram_Config = new List<string>(CurProgram.Config);
        }

        UseHook = CurProgram.UseHook;

        if(UseHook)
        {
            string exeloader = App.Config.ExeLoader.GetFullPath(CurProgram.Arch);
                
            if(!File.Exists(exeloader) || !File.Exists(Path.Combine(Path.GetDirectoryName(exeloader), CurProgram.HookDll + ".DLL")))
            {
                UseHook = false;
            }
        }

        string args = "";
        if (Session.Type == "process" || Session.Type == "window")
        {
            return args;
        }
        if (Session.Type == "app")
        {
            if (!string.IsNullOrWhiteSpace(CurProgram.ProcessName))
            {
                ProcessName = CurProgram.ProcessName;
            }
            if (!string.IsNullOrWhiteSpace(CurProgram.CommandLine))
            {
                args = ProgramConfig.ExpandEnvironmentVariables(CurProgram.CommandLine);
            }
            return args;
        }

        if (!string.IsNullOrWhiteSpace(CurProgram.CommandLine))
        {
            cmdline = CurProgram.CommandLine.Replace("%%", "{0}");
        }

        if(Session.Type == "proxy")
        {
            if(Session.ListenPort == 0 || (string.IsNullOrWhiteSpace(Session.RemoteIp) || Session.RemotePort == 0 && Session.ProxyType != "ssh"))
            {
                return "";
            }

            string local = Session.ListenIp;
            if(string.IsNullOrWhiteSpace(local))
            {
                local = "localhost";
            }

            string af = string.Empty;
            string aa = string.Empty;

            if(IPAddress.TryParse(local, out IPAddress ip))
            {
                if(!IPAddress.IsLoopback(ip))
                {
                    aa = " -localport-acceptall";
                }

                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    af = "4";
                }
                if(ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    af = "6";
                }
            }

            args = string.Format(cmdline, "-portfwd 0 -L" + af + " " + FixIpv6Address(local) + ":" + Session.ListenPort + ":" + FixIpv6Address(Session.RemoteIp) + ":" + Session.RemotePort + GetProxyCmd(Session.Id) + aa);

            if (!string.IsNullOrWhiteSpace(Session.Additional))
            {
                args += " " + Session.Additional;
            }
            return args;
        }

        if (Session.Type == "vnc")
        {
            args = CurProgram.Args.Replace("%host", Ip).Replace("%port", Port.ToString());
            PasswordToPump = Credential?.Password;

            args = string.Format(cmdline, args);

            if (!string.IsNullOrWhiteSpace(Session.Additional))
            {
                args += " " + Session.Additional;
            }
            return args;
        }

        if (CurProgram.Name == App.Config.MSTSC.Name)
        {
            args = CurProgram.Args.Replace("%host", Ip).Replace("%port", Port.ToString());
            string rdpFile = null;
            string rdpFileData = "\r\n";
            ConfigFile configFile = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile s) => s.Id == Session.MSTSCId);
            if (configFile != null)
            {
                rdpFileData += configFile.Data;
            }
            rdpFileData += "\r\nkeyboardhook:i:" + Session.KeyboardHook;
            if(!string.IsNullOrWhiteSpace(Session.SelectedMonitors))
            {
                rdpFileData += "\r\nselectedmonitors:s:" + Session.SelectedMonitors.Trim();
            }

            if (!string.IsNullOrEmpty(Credential?.Username))
            {
                rdpFileData += "\r\nusername:s:" + Credential.Username + (SafeString.IsNullOrEmpty(Credential?.Password) ? "" : "\r\npassword 51:b:" + BitConverter.ToString(ProtectedData.Protect(Encoding.Unicode.GetBytes(Credential.Password.ToString()), null, DataProtectionScope.CurrentUser)).Replace("-", string.Empty));
            }

            if(CurProgram.UsePipe && UseHook)
            {
                string id = InstanceId.ToString();
                rdpFile = "\\\\.\\pipe\\SolarNG.RDP." + id.Substring(id.Length - 12);

                PipeServers.Add(new PipeServer(rdpFile, rdpFileData, 1));
            }
            else
            {
                bool rdpFileExists = false;
                int num = BitConverter.ToInt32(App.UserHash, 0);
                string user_id = Credential.Id.ToString();
                user_id = "_" + user_id.Substring(user_id.Length - 12);

                if (configFile != null)
                {
                    rdpFileExists = configFile.RealPathExists;
                    rdpFile = configFile.RealPath;
                }

                if (string.IsNullOrEmpty(rdpFile))
                {
                    user_id = "SolarNG" + user_id;
                }
                else
                {
                    user_id = Path.GetFileNameWithoutExtension(rdpFile) + user_id;
                }

                rdpFile = Path.Combine(App.DataFilePath, "Temp", user_id + "_" + num.ToString("x") + ".rdp");
                try
                {
                    if (!File.Exists(rdpFile) || !rdpFileExists)
                    {
                        string directoryName = Path.GetDirectoryName(rdpFile);
                        if (!Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }
                        using FileStream stream = File.Open(rdpFile, FileMode.Create);
                        using StreamWriter streamWriter = new StreamWriter(stream);
                        if (!string.IsNullOrWhiteSpace(rdpFileData))
                        {
                            streamWriter.Write(rdpFileData);
                        }
                    }
                }
                catch (Exception message)
                {
                    log.Error(message);
                    try
                    {
                        File.Delete(rdpFile);
                    }
                    catch (Exception)
                    {
                    }
                    return "";
                }
            }

            if (!string.IsNullOrEmpty(rdpFile))
            {
                args = "\"" + rdpFile + "\" " + args;
            }

            if(Session.FullScreen)
            {
                if(Session.MultiMonitors)
                {
                    args += " /multimon";
                }
                else
                {
                    args += " /f";
                }
            }

            args = string.Format(cmdline, args);

            if (!string.IsNullOrWhiteSpace(Session.Additional))
            {
                args += " " + Session.Additional;
            }

            IEnumerable<string> Args = null;
            IEnumerable<string> FullScreenArgs = null;

            Args = CommandLine.Split(args);

            if (Args != null)
            {
                FullScreenArgs = App.Config.MSTSC.FullScreen;
            }

            int width = Session.Width;
            int height = Session.Height;
            if ((FullScreenArgs != null && FullScreenArgs.Intersect(Args).Count() == 0) && (width == 0 || height == 0))
            {
                int num3 = 0;
                while (Panel.DisplayRectangle.Width == 200 && Panel.DisplayRectangle.Height == 100)
                {
                    Thread.Sleep(100);
                    if (num3 > App.Config.GUI.WaitTimeout)
                    {
                        break;
                    }
                    num3++;
                }
                width = Panel.DisplayRectangle.Width;
                height = Panel.DisplayRectangle.Height;
                width += App.Config.MSTSC.WidthDelta;
                height += App.Config.MSTSC.HeightDelta;
            }

            args = args.Replace("%width", width.ToString()).Replace("%height", height.ToString());

            return args;
        }

        if(CurProgram.Name == App.Config.WinSCP.Name) 
        { 
            string session_settings = "";
            if (ByProxy && Session.Type != "ftp")
            {
                ProxyByPipe = true;

                string plinkx_cmdline = App.Config.PlinkX.CommandLine.Replace("%%", "{0}");
                plinkx_cmdline = string.Format(plinkx_cmdline, "\"" + App.Config.PlinkX.FullPath + "\" -raw " + FixIpv6Address(Session.Ip) + " -P " + Session.Port + GetProxyCmd(Session.ProxyId));

                session_settings = Uri.EscapeDataString(plinkx_cmdline);
                session_settings = ";x-proxymethod=5;x-proxytelnetcommand=" + session_settings.Replace("%5C", "\\\\");
            }
            args = Protocol + "://";
            string text5 = "";
            if (!string.IsNullOrEmpty(Credential?.Username))
            {
                if(CurProgram.UsePipe && App.Config.WinSCP.PasswordByPipe)
                {
                    string id = InstanceId.ToString();
                    string pwFile = "\\\\.\\pipe\\SolarNG.WinSCP.PW." + id.Substring(id.Length - 12);

                    if (!SafeString.IsNullOrEmpty(Credential?.Password))
                    {
                        PipeServers.Add(new PipeServer(pwFile, Credential.Password.ToString(), 1));

                        text5 = " /passwordsfromfiles /password=" + pwFile;
                    }
                    if (!SafeString.IsNullOrEmpty(Credential?.Passphrase))
                    {
                        PipeServers.Add(new PipeServer(pwFile, Credential.Passphrase.ToString(), 1));

                        text5 = " /passwordsfromfiles /passphrase=" + pwFile;
                    }
                }
                else
                {
                    PasswordToPump = Credential?.Password;

                    if (!SafeString.IsNullOrEmpty(Credential?.Passphrase))
                    {
                        PasswordToPump = Credential.Passphrase;
                    }
                }

                args += Credential.Username + session_settings + "@";
            }
            else if (!string.IsNullOrEmpty(session_settings))
            {
                args += session_settings + "@";
            }
            args += Ip + ":" + Port + "/" + text5;
            if (!string.IsNullOrEmpty(Credential?.PrivateKeyPath))
            {
                if (CurProgram.UsePipe)
                {
                    string id = InstanceId.ToString();
                    string pkFile = "\\\\.\\pipe\\SolarNG.WinSCP.PK." + id.Substring(id.Length - 12);

                    PipeServers.Add(new PipeServer(pkFile, Credential.PrivateKeyContent, UseHook?1:8));

                    args += " /privatekey=" + pkFile;
                } 
                else
                {
                    if (!string.IsNullOrEmpty(Credential.RealPrivateKeyPath))
                    {
                        args += " /privatekey=\"" + Credential.RealPrivateKeyPath + "\"";
                    }
                    else
                    {
                        PasswordToPump = null;
                        log.WarnFormat("Skipping private key '" + Credential.PrivateKeyPath + "' execution because the file does not exist.");
                    }

                }
            }
            string iniFile = null;
            if (CurProgram.Name == App.Config.WinSCP.Name)
            {
                ConfigFile configFile = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile s) => s.Id == Session.WinSCPId);
                iniFile = configFile?.RealPath;
            }

            if(string.IsNullOrEmpty(iniFile))
            {
                iniFile = Path.Combine(App.DataFilePath, "Temp", "WinSCP.ini");
                if(!File.Exists(iniFile))
                {
                    try
                    {
                        string directoryName = Path.GetDirectoryName(iniFile);
                        if (!Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        File.Create(iniFile).Close();
                    }
                    catch(Exception)
                    {

                    }
                }
            }
            args += " /ini=\"" + iniFile + "\"";

            args = string.Format(cmdline, args);

            if (Session.Type != "ssh" && !string.IsNullOrWhiteSpace(Session.Additional))
            {
                args += " " + Session.Additional;
            }

            return args;
        }

        if(CurProgram.Name != App.Config.PuTTY.Name)
        {
            return "";
        }

        if(Session.PuTTYSessionId != Guid.Empty)
        {
            if(!UseHook)
            {
                ConfigFile puttySession = App.Sessions.ConfigFiles.FirstOrDefault(s => s.Id == Session.PuTTYSessionId);
                if(puttySession != null && !string.IsNullOrWhiteSpace(puttySession.Data)) 
                {
                    string id = puttySession.Id.ToString();
                    string sessionName = "~SolarNG." + id.Substring(id.Length - 12);

                    if(ConfigFile.SetPuTTYSession(sessionName, puttySession.Data, false))
                    {
                         args = " -load \"" + sessionName + "\"";
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(Session.PuTTYSession))
        {
            args = " -load \"" + Session.PuTTYSession.Replace("\"", "\"\"") + "\"";
        }

        string addr = " " + ((string.IsNullOrEmpty(Credential?.Username) || Session.Type == "telnet") ? Ip : (Credential.Username + "@" + Ip)) + " -P " + Port;
        ShareKey = string.Empty;
        if (!string.IsNullOrEmpty(Credential?.Username))
        {
            ShareKey = Credential.Username + "@" + Ip;
        }
        else
        {
            ShareKey = Ip;
        }
        if (Port != 22)
        {
            ShareKey = ShareKey + ":" + Port;
        }
        if (Session.Type == "ssh" && (iFlags & ProgramConfig.FLAG_SSHV2SHARE) != 0)
        {
            SSHv2Share = true;
        }
        string additional = string.Empty;
        if (!string.IsNullOrWhiteSpace(Session.Additional))
        {
            IEnumerable<string> source = CommandLine.Split(Session.Additional);
            if (Session.Type == "ssh" && source.Contains("-share"))
            {
                SSHv2Share = true;
            }
        }
        bool SSHv2ShareMain = false;
        if (SSHv2Share)
        {
            SSHv2ShareMain = !CheckSSHv2Share(ShareKey);

            if(ProxySession != null && SSHv2ShareMain)
            {
                SSHv2ShareMain = false;
                SSHv2Share = false;
            }
            else if (!App.SSHv2ShareManager.ContainsKey(ShareKey))
            {
                if (SSHv2ShareMain)
                {
                    App.SSHv2ShareManager.Add(ShareKey, new ObservableCollection<TabBase>());
                }
                else if (App.Config.PuTTY.StrictSSHv2Share)
                {
                    SSHv2Share = false;
                }
                else
                {
                    App.SSHv2ShareManager.Add(ShareKey, new ObservableCollection<TabBase> { null });
                }
            }
            else if (SSHv2ShareMain)
            {
                App.SSHv2ShareManager[ShareKey].Clear();
            }
            else if (App.Config.PuTTY.StrictSSHv2Share)
            {
                if (App.SSHv2ShareManager[ShareKey][0] == null)
                {
                    SSHv2Share = false;
                }
                else if ((App.SSHv2ShareManager[ShareKey][0] as AppTabViewModel).Session.Id != Session.Id)
                {
                    SSHv2Share = false;
                }
            }
            if (SSHv2Share)
            {
                App.SSHv2ShareManager[ShareKey].Add(this);
                additional += " -share";
                if(!SSHv2ShareMain)
                {
                    ByProxy = false;
                }
            }
        }
        if (Session.Type != "telnet" && !SSHv2Share)
        {
            additional += " -noshare";
        }
        string pwfile = string.Empty;
        string pkfile = string.Empty;
        if (Session.Type == "telnet" || !SSHv2Share || SSHv2ShareMain)
        {
            if (ByProxy)
            {
                ProxyByPipe = true;

                string plinkx_cmdline = App.Config.PlinkX.CommandLine.Replace("%%", "{0}");
                plinkx_cmdline = string.Format(plinkx_cmdline, "\"" + App.Config.PlinkX.FullPath + "\" -raw " + FixIpv6Address(Session.Ip) + " -P " + Session.Port + GetProxyCmd(Session.ProxyId));

                additional += " -proxycmd \"" + plinkx_cmdline.Replace("\\", "\\\\").Replace("\"", "\"\"") + "\"";
            }
            if (!string.IsNullOrEmpty(Credential?.Username))
            {
                if (!SafeString.IsNullOrEmpty(Credential?.Passphrase))
                {
                    PasswordToPump = Credential.Passphrase;
                } 
                else if (!SafeString.IsNullOrEmpty(Credential?.Password))
                {
                    if (CurProgram.UsePipe && App.Config.PuTTY.SSHPasswordByPipe && !App.Config.PuTTY.SSHPasswordByHook && Session.Type != "telnet")
                    {
                        string id = InstanceId.ToString();
                        string pwFile = "\\\\.\\pipe\\SolarNG.PuTTY.PW." + id.Substring(id.Length - 12);

                        PipeServers.Add(new PipeServer(pwFile, Credential.Password.ToString(), 1));

                        pwfile = " -pwfile " + pwFile;
                    }
                    else
                    {
                        PasswordToPump = Credential.Password;
                    }
                }
            }
            if (!string.IsNullOrEmpty(Credential?.PrivateKeyPath))
            {
                if (CurProgram.UsePipe)
                {
                    string id = InstanceId.ToString();
                    string pkFile = "\\\\.\\pipe\\SolarNG.PuTTY.PK." + id.Substring(id.Length - 12);

                    PipeServers.Add(new PipeServer(pkFile, Credential.PrivateKeyContent, UseHook?1:4));

                    pkfile = " -i " + pkFile;
                } 
                else
                {
                    if (!string.IsNullOrEmpty(Credential.RealPrivateKeyPath))
                    {
                        pkfile = " -i \"" + Credential.RealPrivateKeyPath + "\"";
                    }
                    else
                    {
                        PasswordToPump = null;
                        log.WarnFormat("Skipping private key '" + Credential.PrivateKeyPath + "' execution because the file does not exist.");
                    }
                }
            }
        }

        string logfile = string.Empty;
        if (Session.Logging)
        {
            if (Directory.Exists(LogsFolderDestination))
            {
                logfile = " -sessionlog \"" + Path.Combine(LogsFolderDestination, ValidateFileName(string.Concat(str2: DateTime.Now.ToString("yyyyMMddhhmmss", CultureInfo.InvariantCulture), str0: Session.Name, str1: "_", str3: ".log"))) + "\"";
            }
            else
            {
                log.Warn("Skipping session logging because of non existing log folder destination '" + LogsFolderDestination + "'");
            }
        }

        if (Session.Type == "telnet")
        {
            args = string.Format(cmdline, args + " -telnet" + addr + logfile + additional);
        }
        else
        {
            args += " -ssh -2";

            if(ProxySession != null)
            {
                string local = ProxySession.ListenIp;
                if(string.IsNullOrWhiteSpace(local))
                {
                    local = "localhost";
                }

                args += " -N ";

                if(string.IsNullOrWhiteSpace(ProxySession.RemoteIp) || ProxySession.RemotePort == 0)
                {
                    args += "-D " + FixIpv6Address(local) + ":" + ProxySession.ListenPort;
                }
                else
                {
                    args += "-L " + FixIpv6Address(local) + ":" + ProxySession.ListenPort + ":" + FixIpv6Address(ProxySession.RemoteIp) + ":" + ProxySession.RemotePort;
                }

                if(IPAddress.TryParse(local, out IPAddress ip))
                {
                    if(!IPAddress.IsLoopback(ip))
                    {
                        CurProgram_Config.Add("LocalPortAcceptAll=1");
                    }
                }
            }

            args = string.Format(cmdline, args + addr + pwfile + pkfile + logfile + additional);
        }

        if (!string.IsNullOrWhiteSpace(Session.Additional))
        {
            args += " " + Session.Additional;
        }
        return args;
    }

    private string FixIpv6Address(string ip)
    {
        if(string.IsNullOrWhiteSpace(ip))
        {
            return "";
        }

        ip = ip.Trim();

        if(ip.Contains(":") && !ip.Contains("["))
        {
            return "[" + ip + "]";
        }

        return ip;
    }

    private string ProxyInstanceId;
    private bool ProxyStarted;
    private bool CheckProxy(Session session)
    {
        if ((session.SessionTypeFlags & SessionType.FLAG_PROXY_CONSUMER)==0)
        {
            return false;
        }
        if (session.ProxyId == Guid.Empty)
        {
            return false;
        }
        Session proxy = App.Sessions.Sessions.FirstOrDefault((Session s) => s.Id == session.ProxyId);
        if (proxy == null || (proxy.SessionTypeFlags & SessionType.FLAG_PROXY_PROVIDER)==0)
        {
            return false;
        }
        if ((session.SessionTypeFlags & SessionType.FLAG_SSH_PROXY)!=0)
        {
            return true;
        }
        ProxyStarted = false;
        using (SHA256 sha256 = SHA256.Create())
        {
            ProxyInstanceId = new Guid(sha256.ComputeHash(session.Id.ToByteArray().Concat(session.ProxyId.ToByteArray()).ToArray()).Take(16).ToArray()).ToString();
        }
        byte[] hash = null;
        using (SHA256 sha256 = SHA256.Create())
        {
            hash = sha256.ComputeHash(session.Id.ToByteArray());
        }
        hash[0] = 127;
        if (hash[3] == 0 || hash[3] == byte.MaxValue)
        {
            hash[3] = 1;
        }
        Port = BitConverter.ToUInt16(hash, 4);
        if (Port == 0)
        {
            Port = session.Port;
        }
        try
        {
            IPAddress addr = IPAddress.Loopback;
            if(App.Config.PlinkX.RandomLoopbackAddress)
            {
                addr = new IPAddress(BitConverter.ToUInt32(hash, 0));
            }
            Socket socket = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            IPAddress addrV6 = IPAddress.IPv6Loopback;
            Socket socketV6 = new Socket(addrV6.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            while (true)
            {
                try
                {
                    socket.Bind(new IPEndPoint(addr, Port));
                    socket.Close();
                    if(App.Config.PlinkX.RandomLoopbackAddress)
                    {
                        Ip = addr.ToString();
                    }
                    else
                    {
                        socketV6.Bind(new IPEndPoint(addrV6, Port));
                        socketV6.Close();

                        Ip = "localhost";
                    }

                }
                catch (Exception)
                {
                    Process[] processesByName = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(App.Config.PlinkX.FullPath));
                    foreach (Process process in processesByName)
                    {
                        string id;
                        try
                        {
                            id = SolarNGX.GetProcessEnvironmentVariable(process.Id, "SolarNG-id");
                            if (id == null)
                            {
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        if (ProxyInstanceId == id)
                        {
                            socket.Close();
                            if(App.Config.PlinkX.RandomLoopbackAddress)
                            {
                                Ip = addr.ToString();
                            }
                            else
                            {
                                socketV6.Close();
                                Ip = "localhost";
                            }
                            ProxyStarted = true;
                            return true;
                        }
                    }
                    Port++;
                    if (Port == 0)
                    {
                        Port = 1;
                    }
                    continue;
                }
                break;
            }
        }
        catch (Exception message)
        {
            log.Error(message);
            Port = session.Port;
            return false;
        }
        return true;
    }

    private bool GetProxyCmdError;

    private Dictionary<string, int> SSHv2ShareKeys = new Dictionary<string, int>();

    private string GetProxyCmd(Guid proxyId)
    {
        bool quit = false;
        GetProxyCmdError = false;
        string proxy_lines = "";
        while (proxyId != Guid.Empty && !quit)
        {
            string proxy_line;
            Session proxy = App.Sessions.Sessions.FirstOrDefault((Session s) => s.Id == proxyId);
            if (proxy == null)
            {
                break;
            }
                    
            if(proxy.Type == "proxy" && proxy.ProxyType == "ssh")
            {
                proxy = proxy.SSHSession;
            }

            string username = "";
            SafeString password = null;
            string privatekey = "";
            if (proxy.CredentialId != Guid.Empty)
            {
                Credential credential = proxy.Credential;
                if (credential != null)
                {
                    username = credential.Username;
                    privatekey = credential.RealPrivateKeyPath;
                    if (!string.IsNullOrEmpty(privatekey))
                    {
                        password = credential.Passphrase;
                    }
                    else
                    {
                        password = credential.Password;
                    }
                }
            }

            if(proxy.Type == "proxy")
            {
                proxy_line = proxy.ProxyType + " " + FixIpv6Address(proxy.Ip) + " " + proxy.Port + " " + username;

                if(password != null)
                {
                    if(password.ToString().Contains(" "))
                    {
                        password = new SafeString("\"" + password.ToString().Replace("\"", "\"\"") + "\"");
                    }

                    proxy_line += " " + password;
                }
            } 
            else
            {
                string sk = string.Empty;
                if (string.IsNullOrEmpty(username))
                {
                    GetProxyCmdError = true;
                    return "";
                }

                sk = username + "@" + FixIpv6Address(proxy.Ip);

                if (proxy.Port != 22)
                {
                    sk = sk + ":" + proxy.Port;
                }

                if (CheckSSHv2Share(sk) && (!App.Config.PuTTY.StrictSSHv2Share || (App.SSHv2ShareManager.ContainsKey(sk) && App.SSHv2ShareManager[sk][0] != null && (App.SSHv2ShareManager[sk][0] as AppTabViewModel).Session.Id == proxy.Id)))
                {
                    if (!SSHv2ShareKeys.ContainsKey(sk))
                    {
                        SSHv2ShareKeys[sk] = 0;
                        App.SSHv2ShareManager[sk].Add(this);
                    }

                    proxy_line = "sshs " + FixIpv6Address(proxy.Ip) + " " + proxy.Port + " " + username;
                    quit = true;
                }
                else
                {
                    if (SafeString.IsNullOrEmpty(password))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(delegate
                        {
                            while (Win32.GetForegroundWindow() != base.MainWindow.Handle)
                            {
                                Win32.SetForegroundWindow(base.MainWindow.Handle);
                                Thread.Sleep(100);
                            }
                            if (SafeString.IsNullOrEmpty(password))
                            {
                                string key = "Password2";
                                if (!string.IsNullOrEmpty(privatekey))
                                {
                                    key = "Passphrase2";
                                }
                                string text5 = FixIpv6Address(proxy.Ip);
                                if (proxy.Port != 22)
                                {
                                    text5 = text5 + ":" + proxy.Port;
                                }
                                PromptDialog promptDialog = new PromptDialog(base.MainWindow, System.Windows.Application.Current.Resources["Login"] as string, string.Format(System.Windows.Application.Current.Resources[key] as string, text5), "", password: true) { Topmost = true };
                                promptDialog.Focus();
                                bool? flag2 = promptDialog.ShowDialog();
                                if (!flag2.HasValue || !flag2.Value)
                                {
                                    return;
                                }
                                password = new SafeString(promptDialog.MyPassword.SecurePassword);
                            }
                        });
                    }
                    if (SafeString.IsNullOrEmpty(password))
                    {
                        GetProxyCmdError = true;
                        return "";
                    }

                    if(password.ToString().Contains(" "))
                    {
                        password = new SafeString("\"" + password.ToString().Replace("\"", "\"\"") + "\"");
                    }

                    proxy_line = "ssh " + FixIpv6Address(proxy.Ip) + " " + proxy.Port + " " + username + " " + password;
                    
                    if(!string.IsNullOrEmpty(privatekey))
                    {
                       proxy_line += " \"" + privatekey + "\"";
                    }
                }
            }

            proxy_lines += proxy_line + "\n";
            proxyId = proxy.ProxyId;
        }

        string id = InstanceId.ToString();
        string proxyFile = "\\\\.\\pipe\\SolarNG.PlinkX.Proxy." + id.Substring(id.Length - 12);

        PipeServers.Add(new PipeServer(proxyFile, proxy_lines, 1));

        return " -proxycmd \"proxychain " + proxyFile + "\"";
    }

    private bool StartProxy()
    {
        if(!ByProxy || ProxyStarted)
        {
            return true;
        }

        if(GetProxyCmdError)
        {
            return false;
        }

        if (!File.Exists(App.Config.PlinkX.FullPath))
        {
            return false;
        }

        if(ProxyByPipe)
        {
            return true;
        }

        string plinkx_cmdline = App.Config.PlinkX.CommandLine.Replace("%%", "{0}");
        plinkx_cmdline = string.Format(plinkx_cmdline, "-portfwd " + App.Config.PlinkX.IdleTimeout + " -L " + Ip + ":" + Port + ":" + FixIpv6Address(Session.Ip) + ":" + Session.Port + GetProxyCmd(Session.ProxyId));

        Environment.SetEnvironmentVariable("SolarNG-Id", ProxyInstanceId);

        ProcessStartInfo processStartInfo = new ProcessStartInfo(App.Config.PlinkX.FullPath)
        {
            Arguments = plinkx_cmdline,
            CreateNoWindow = App.Config.PlinkX.CreateNoWindow,
            UseShellExecute = false
        };

        return Process.Start(processStartInfo) != null;
    }

    private bool CheckSSHv2Share(string sk)
    {
        byte[] sharekeyForEncrypt = new byte[(sk.Length + 15) / 16 * 16];
        byte[] sharekey = Encoding.ASCII.GetBytes(sk);
        Array.Copy(sharekey, sharekeyForEncrypt, sharekey.Length);
        ProtectedMemory.Protect(sharekeyForEncrypt, MemoryProtectionScope.CrossProcess);
        byte[] sharekeyForEncrypt_len = BitConverter.GetBytes((uint)sharekeyForEncrypt.Length);
        Array.Reverse(sharekeyForEncrypt_len);
        sharekey = sharekeyForEncrypt_len.Concat(sharekeyForEncrypt).ToArray();
        StringBuilder hex = new StringBuilder(64);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] array2 = sha256.ComputeHash(sharekey);
            for (int i = 0; i < array2.Length; i++)
            {
                hex.AppendFormat("{0:x2}", array2[i]);
            }
        }
        string pipeName = "putty-connshare." + Environment.UserName + "." + hex;
        try
        {
            using NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
            if (namedPipeServerStream != null)
            {
                return false;
            }
        }
        catch (Exception)
        {
        }
        return true;
    }

    private string _LogsFolderDestination;
    private string LogsFolderDestination
    {
        get
        {
            if (_LogsFolderDestination == null)
            {
                string path = Path.Combine(App.DataFilePath, "SessionLogs");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                _LogsFolderDestination = path;
            }
            return _LogsFolderDestination;
        }
    }

    private string ValidateFileName(string fileName)
    {
        StringBuilder stringBuilder = new StringBuilder(fileName);
        char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidFileNameChars.Length; i++)
        {
            stringBuilder.Replace(invalidFileNameChars[i], '_');
        }
        return stringBuilder.ToString();
    }

    private bool Created = false;

    public bool IsInTab;

    private void Connect(string args)
    {
        if(args == null)
        {
            return;
        }
        if (!Created)
        {
            IsInTab = false;
            State = ConnectionState.Connecting;
            base.ResetUnderlineColor(this);
            if (!StartProxy())
            {
                State = ConnectionState.Disconnected;
                return;
            }
            if (!StartApp(args))
            {
                State = ConnectionState.Disconnected;
                base.ProcessExited(this);
                return;
            }
            Created = true;
            Task.Run(delegate
            {
                MonitorAppMainWindow();
            });
        }
        State = ConnectionState.Connected;
    }

    public int AppProcessID ;
    private Process AppProcess;

    private bool StartApp(string AppArgs)
    {
        try
        {
            if (Session.Type == "process" || Session.Type == "window")
            {
                AppProcess = Process.GetProcessById(Session.Pid);
                AppProcessID = AppProcess.Id;
                return true;
            }

            Environment.SetEnvironmentVariable("SolarNG-Id", InstanceId);
            if (CurProgram.Flags.Contains("mintty"))
            {
                Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("Path") + ";SolarNG-Id-" + InstanceId);
            }

            Process fakeProcess = new Process();
            ProcessStartInfo processStartInfo = null;

            if (UseHook)
            {
                try
                {
                    if(CurProgram.Name == App.Config.PuTTY.Name)
                    {
                        string conf = string.Empty;
                        if(Session.PuTTYSessionId != Guid.Empty)
                        {
                            ConfigFile puttySession = App.Sessions.ConfigFiles.FirstOrDefault(s => s.Id == Session.PuTTYSessionId);
                            if(puttySession != null && !string.IsNullOrWhiteSpace(puttySession.Data)) 
                            {
                                foreach(string line in puttySession.Data.Split('\n'))
                                {
                                    if(string.IsNullOrWhiteSpace(line))
                                    {
                                        continue;
                                    }

                                    int i = line.IndexOf('=');
                                    if(i == -1)
                                    {
                                        continue;
                                    }

                                    string name = line.Substring(0, i).Trim().Trim('"');
                                    string value = line.Substring(i + 1).Trim();

                                    bool found = false;
                                    foreach(string config in CurProgram_Config)
                                    {
                                        if(config.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if(found)
                                    {
                                        continue;
                                    }

                                    if (name.Equals("SessionName", StringComparison.OrdinalIgnoreCase))
                                    {
                                        conf += string.Join("\0", CurProgram_Config) + "\0";
                                    }

                                    conf += name + "=" + value + "\0";
                                }
                            }
                        }

                        conf += string.Join("\0", CurProgram_Config) + "\0";

                        if (App.Config.PuTTY.SSHPasswordByHook && !SafeString.IsNullOrEmpty(PasswordToPump))
                        {
                            conf = "Password=\"" + PasswordToPump.ToString() + "\"\0" + conf; 
                            PasswordToPump = null;
                        }

                        PipeServers.Add(new PipeServer("\\\\.\\pipe\\SolarNG.PuTTY." + InstanceId, conf));
                    }

                    processStartInfo = new ProcessStartInfo(App.Config.ExeLoader.GetFullPath(CurProgram.Arch))
                    {
                        Arguments = CurProgram.HookDll + " \"" + (Session.Type == "rdp" ? CurProgram.NativeFullPath:CurProgram.FullPath) + "\" " + AppArgs,
                        WorkingDirectory = CurProgram.FullWorkingDir,
                        UseShellExecute = false
                    };
                    AppProcess = Process.Start(processStartInfo);

                    AppProcess.WaitForExit();
                    int pid = AppProcess.ExitCode;
                    if (pid == 0 || pid == -1)
                    {
                        AppProcess = null;
                    }
                    else
                    {
                        AppProcess = Process.GetProcessById(pid);
                    }

                }
                catch (Exception)
                {
                    AppProcess = null;
                }
            }
            else
            {
                processStartInfo = new ProcessStartInfo(CurProgram.FullPath)
                {
                    Arguments = AppArgs,
                    WorkingDirectory = CurProgram.FullWorkingDir,
                    UseShellExecute = true
                };
                AppProcess = Process.Start(processStartInfo);

                try
                {
                    Uri uri = new Uri(CurProgram.FullPath);
                    if(!uri.IsFile && AppProcess == null)
                    {
                        AppProcess = fakeProcess;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            if (CurProgram.Flags.Contains("mintty"))
            {
                string environmentVariable = Environment.GetEnvironmentVariable("Path");
                int start = environmentVariable.IndexOf(";SolarNG-Id-");
                if (start != -1)
                {
                    Environment.SetEnvironmentVariable("Path", environmentVariable.Substring(0, start));
                }
            }
            if (AppProcess != null)
            {
                if (ProcessName == null && !CurProgram.Flags.Contains("singleton") && AppProcess != fakeProcess)
                {
                    AppProcess.EnableRaisingEvents = true;
                    AppProcess.Exited += AppProcess_Exited;
                    AppProcessID = AppProcess.Id;
                }

                if(!NeedInTab())
                {
                    while (!TestPipeServers())
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            else
            {
                log.Error($"Failed to start \"{processStartInfo.FileName}\"");
            }
        }
        catch (Exception exception)
        {
            log.Error("Failed to start", exception);
        }

        return AppProcess != null;
    }

    private bool TestPipeServers()
    {
        foreach(PipeServer server in PipeServers)
        {
            int rc = server.Test();
            if(rc>0)
            {
                return false;
            }
        }

        return true;
    }

    private Win32.WinEventDelegate winEventDelegate;

    private IntPtr winEventHook = IntPtr.Zero;
    private IntPtr winEventHook2 = IntPtr.Zero;
    private IntPtr winEventHook3 = IntPtr.Zero;
    private void RegisterWindowsEventHook()
    {
        UnregisterWindowsEventHook();
        winEventDelegate ??= WinEventProc;

        winEventHook = Win32.SetWinEventHook(Win32.EVENT_OBJECT_LOCATIONCHANGE, Win32.EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, winEventDelegate, isConsoleWindow?0u:(uint)AppProcess.Id, 0u, 0u);
        winEventHook2 = Win32.SetWinEventHook(Win32.EVENT_OBJECT_DESTROY, Win32.EVENT_OBJECT_DESTROY, IntPtr.Zero, winEventDelegate, isConsoleWindow?0u:(uint)AppProcess.Id, 0u, 0u);
        if(!CurProgram.Flags.Contains("keepparent"))
        {
		    winEventHook3 = Win32.SetWinEventHook(Win32.EVENT_SYSTEM_CAPTUREEND, Win32.EVENT_SYSTEM_CAPTUREEND, IntPtr.Zero, winEventDelegate, isConsoleWindow?0u:(uint)AppProcess.Id, 0U, 0U);
        }
        UpdateTabTitle();
    }

    private void UnregisterWindowsEventHook()
    {
        if (winEventHook != IntPtr.Zero)
        {
            Win32.UnhookWinEvent(winEventHook);
            winEventHook = IntPtr.Zero;
        }
        if (winEventHook2 != IntPtr.Zero)
        {
            Win32.UnhookWinEvent(winEventHook2);
            winEventHook2 = IntPtr.Zero;
        }
        if (winEventHook3 != IntPtr.Zero)
        {
            Win32.UnhookWinEvent(winEventHook3);
            winEventHook3 = IntPtr.Zero;
        }
    }

    private List<IntPtr> hWnds = new List<IntPtr>();
    private bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
    {
        hWnds.Add(hWnd);
        return true;
    }

    private IntPtr GetAppOtherMainWindow(IntPtr win, Process process)
    {
        if (CurProgram.ClassName.Count == 0)
        {
            return win;
        }
        StringBuilder ClassName = new StringBuilder(256);
        string classname = "";
        if (Win32.GetClassName(win, ClassName, ClassName.Capacity) != 0)
        {
            classname = ClassName.ToString();
        }
        if (CurProgram.ClassName.Contains(classname))
        {
            return win;
        }
        hWnds.Clear();
        try
        {
            Win32.EnumThreadWindows((uint)process.Threads[0].Id, EnumThreadCallback, IntPtr.Zero);
        }
        catch (Exception exception)
        {
            log.Error("", exception);
        }
        foreach (IntPtr hWnd in hWnds)
        {
            classname = "";
            if (Win32.GetClassName(hWnd, ClassName, ClassName.Capacity) != 0)
            {
                classname = ClassName.ToString();
            }
            if (CurProgram.ClassName.Contains(classname) || CurProgram.AuthClassName.Contains(classname))
            {
                return hWnd;
            }
        }
        return win;
    }

    private void CloseSelf(bool notkill=false)
    {
        ExitCode = 0;
        Created = false;

        System.Windows.Application.Current.Dispatcher.Invoke(delegate
        {
            base.MainWindow.MainWindowVM.RemoveTab(this);
        });
    }

    private bool EnumAFWThreadWindowCallback(IntPtr hWnd, IntPtr lParam)
    {
        StringBuilder ClassName = new StringBuilder(256);
        if (Win32.GetClassName(hWnd, ClassName, ClassName.Capacity) != 0)
        {
            string classname = ClassName.ToString();

            if(classname == "ApplicationFrameWindow")
            {
                hWnds.Add(hWnd);
                return false;
            }
        }

        return true;
    }

    private List<IntPtr> hWnds2 = new List<IntPtr>();
    private bool EnumAFWChildWindowCallback(IntPtr hWnd, IntPtr lParam)
    {
        StringBuilder ClassName = new StringBuilder(256);
        if (Win32.GetClassName(hWnd, ClassName, ClassName.Capacity) != 0)
        {
            string classname = ClassName.ToString();

            if(classname == "Windows.UI.Core.CoreWindow")
            {
                hWnds2.Add(hWnd);
                return false;
            }

        }

        return true;
    }
	
    private bool EnumSingleTonThreadCallback(IntPtr hWnd, IntPtr lParam)
    {
        IntPtr window = Win32.GetTopWindow(IntPtr.Zero);
        IntPtr hwnd = Win32.GetParent(hWnd);

        if(hwnd != IntPtr.Zero && hwnd != window)
        {
            return true;
        }

        uint ws = Win32.GetWindowLong(hWnd, Win32.GWL_STYLE);

        if((ws & Win32.WS_VISIBLE) == 0 || (ws & Win32.WS_POPUP) != 0)
        {
            return true;
        }

        if(!Win32.GetWindowRect(hWnd, out var lpRect))
        {
            return true;
        }

        if(lpRect.Top == lpRect.Bottom || lpRect.Left == lpRect.Right)
        {
            return true;
        }
                   
        hWnds.Add(hWnd);
        return true;
    }

    public IntPtr AppWin;

    private bool MonitorAppMainWindow_done;

	private bool ProcessNotExit = false;

    private bool AutoInputUsername => (Session.Type == "telnet" || Session.Type == "vnc") && !string.IsNullOrEmpty(Credential?.Username);

    private void MonitorAppMainWindow()
    {
        if ((iFlags & ProgramConfig.FLAG_NOTINTAB) != 0 && (iFlags & ProgramConfig.FLAG_NOTCLOSEIME) != 0 && Session.Type == "app" && string.IsNullOrEmpty(Monitor))
        {
            CloseSelf();
            return;
        }
        bool needAuth = (!SafeString.IsNullOrEmpty(PasswordToPump) && CurProgram.AuthClassName.Count > 0) || AutoInputUsername;
        bool AuthDone = CurProgram.AuthClassName.Count == 0;

        bool foundProcess = ProcessName == null;
        bool isAFW = CurProgram.ClassName.Contains("ApplicationFrameWindow") && Session.Type == "app";
        bool getWindowByProcess = true;
        IntPtr CurAppWin = IntPtr.Zero;
        Process appProcess = AppProcess;
        int num = 0;

        if(isAFW)
        {
            getWindowByProcess = false;

            if(ProcessName == null || child)
            {
                CloseSelf();
                return;
            }

            Process[] processesByName = Process.GetProcessesByName("ApplicationFrameHost");
            if(processesByName.Length != 1)
            {
                CloseSelf();
                return;
            }

            AppProcess = processesByName[0];

            num = 0;

            while (num < App.Config.GUI.WaitTimeout)
            {
                hWnds.Clear();
                try
                {
                    foreach(ProcessThread thread in AppProcess.Threads)
                    {
                        Win32.EnumThreadWindows((uint)thread.Id, EnumAFWThreadWindowCallback, IntPtr.Zero);
                    }
                }
                catch (Exception exception)
                {
                    log.Error("", exception);
                }

                foreach(IntPtr hwnd in hWnds)
                {
                    hWnds2.Clear();

                    try
                    {
                        Win32.EnumChildWindows(hwnd, EnumAFWChildWindowCallback, IntPtr.Zero);
                    }
                    catch (Exception exception)
                    {
                        log.Error("", exception);
                    }

                    if (hWnds2.Count == 1)
                    {
                        IntPtr hwndCoreWindow = hWnds2.First();

                        Win32.GetWindowThreadProcessId(hwndCoreWindow, out uint pid);

                        processesByName = Process.GetProcessesByName(ProcessName);
                        foreach(Process process in processesByName)
                        {
                            if(pid == process.Id)
                            {
                                if (!App.WindowHandleManager.ContainsKey(hwnd))
                                {
                                    CurAppWin = hwnd;
                                    appProcess = process;
                                    foundProcess = true;
                                    ProcessNotExit = true;
                                    break;
                                }
                            }
                        }

                        if (foundProcess)
                        {
                            break;
                        }
                    }
                }

                if (foundProcess)
                {
                    break;
                }

                num++;                
                Thread.Sleep(100);
                AppProcess.Refresh();
            }

            if(!foundProcess)
            {
                CloseSelf();
                return;
            }
        } 
        else if(CurProgram.Flags.Contains("singleton"))
        {
            getWindowByProcess = false;

            ProcessName ??= Path.GetFileNameWithoutExtension(CurProgram.FullPath);

            num = 0;
            foundProcess = false;

            while (num < App.Config.GUI.WaitTimeout)
            {
                Process[] processesByName = Process.GetProcessesByName(ProcessName);

                foreach(Process process in processesByName)
                {
                    hWnds.Clear();
                    try
                    {
                        foreach(ProcessThread thread in process.Threads)
                        {
                            Win32.EnumThreadWindows((uint)thread.Id, EnumSingleTonThreadCallback, IntPtr.Zero);
                        }
                    }
                    catch (Exception exception)
                    {
                        log.Error("", exception);
                    }

                    foreach(IntPtr hwnd in hWnds)
                    {
                        if (!App.WindowHandleManager.ContainsKey(hwnd))
                        {
                            CurAppWin = hwnd;
                            appProcess = AppProcess = process;
                            foundProcess = true;
                            ProcessNotExit = true;
                            break;
                        }
                    }

                    if (foundProcess)
                    {
                        break;
                    }
                }

                if (foundProcess)
                {
                    break;
                }

                num++;                
                Thread.Sleep(100);
            }


            if(!foundProcess)
            {
                CloseSelf();
                return;
            }
        }

        if(Session.Type == "window")
        {
            CurAppWin = Session.hWindow;
            getWindowByProcess = false;
            foundProcess = true;
        }

        MonitorAppMainWindow_done = false;

        while (!appProcess.HasExited || !foundProcess)
        {
            if (!foundProcess)
            {
                num = 0;
                while ((appProcess.HasExited || !foundProcess) && num < App.Config.GUI.WaitTimeout)
                {
                    Process[] processesByName = Process.GetProcessesByName(ProcessName);
                    foreach (Process process in processesByName)
                    {
                        if (!child)
                        {
                            if (process.Handle != IntPtr.Zero && process.MainWindowHandle != IntPtr.Zero)
                            {
                                appProcess = AppProcess = process;
                                foundProcess = true;
                                break;
                            }
                            continue;
                        }
                        string id;
                        try
                        {
                            id = SolarNGX.GetProcessEnvironmentVariable(process.Id, "SolarNG-id");
                            if (id == null)
                            {
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        if (InstanceId == id)
                        {
                            try
                            {
                                CurAppWin = process.MainWindowHandle;
                            }
                            catch (Exception)
                            {
                                CurAppWin = IntPtr.Zero;
                            }
                            if (CurAppWin != IntPtr.Zero)
                            {
                                appProcess = (AppProcess = process);
                                foundProcess = true;
                                break;
                            }
                        }
                    }

                    if(foundProcess)
                    {
                        break;
                    }

                    num++;
                    Thread.Sleep(100);
                    AppProcess.Refresh();
                }
                if ((appProcess.HasExited || !foundProcess) && num >= App.Config.GUI.WaitTimeout)
                {
                    break;
                }
                num = 0;
            }
            num++;
            if(getWindowByProcess)
            {
                try
                {
                    CurAppWin = appProcess.MainWindowHandle;
                }
                catch (Exception)
                {
                    CurAppWin = IntPtr.Zero;
                }
            }

            if (CurAppWin != IntPtr.Zero && AppWin != CurAppWin)
            {
                int WindowWidth = 0;
                int WindowHeight = 0;
                if (Win32.GetWindowRect(CurAppWin, out var AppRect))
                {
                    WindowWidth = AppRect.Right - AppRect.Left;
                    WindowHeight = AppRect.Bottom - AppRect.Top;
                }
                if (WindowWidth == 0 || WindowHeight == 0)
                {
                    IntPtr appOtherMainWindow = GetAppOtherMainWindow(CurAppWin, appProcess);
                    if (appOtherMainWindow != CurAppWin)
                    {
                        CurAppWin = appOtherMainWindow;
                        if (Win32.GetWindowRect(CurAppWin, out AppRect))
                        {
                            WindowWidth = AppRect.Right - AppRect.Left;
                            WindowHeight = AppRect.Bottom - AppRect.Top;
                        }
                    }
                }
                if (WindowWidth != 0 && WindowHeight != 0 && Win32.IsWindowVisible(CurAppWin))
                {
                    AppWin = CurAppWin;
                    MoveAppMainWindow(CurAppWin);
                    if (NeedInTab() && !CurProgram.Flags.Contains("mstsc") && !PutInTab())
                    {
                        CloseSelf();
                        break;
                    }
                }
            }
            if (AppWin != IntPtr.Zero)
            {
                StringBuilder ClassName = new StringBuilder(256);
                if (Win32.GetClassName(AppWin, ClassName, ClassName.Capacity) != 0)
                {
                    string classname = ClassName.ToString();
                    if (!AuthDone && CurProgram.AuthClassName.Contains(classname) && (!CurProgram.Flags.Contains("winscp") || num % 5 == 0))
                    {
                        if (needAuth)
                        {
                            TypePassword();

                            if (AppProcess.HasExited)
                            {
                                break;
                            }

                            if (!CurProgram.Flags.Contains("winscp"))
                            {
                                AuthDone = true;
                            }
                        }
                        else
                        {
                            AuthDone = true;
                        }
                    }
                    if (CurProgram.ClassName.Count == 0 || CurProgram.ClassName.Contains(classname))
                    {
                        TypeScript();

                        if (AppProcess.HasExited)
                        {
                            break;
                        }

                        if((iFlags & ProgramConfig.FLAG_NOTCLOSEIME) == 0) 
                        {
                            if(isAFW)
                            {
                                while (Win32.GetForegroundWindow() != AppWin)
                                {
                                    AppProcess.Refresh();
                                    if (AppProcess.HasExited)
                                    {
                                        break;
                                    }
                                    Thread.Sleep(100);
                                    Win32.SetForegroundWindow(AppWin);
                                }
                                if (AppProcess.HasExited)
                                {
                                    return;
                                }

                                SendInputHelper.TypeShift();
                            }
                            else
                            {
                                MainWindow.CloseIME(AppWin);
                            }
                        }

                        if (string.IsNullOrEmpty(Protocol) && SSHv2Share && App.SSHv2ShareManager[ShareKey][0] == this)
                        {
                            base.TabColorName = System.Windows.Application.Current.Resources["MAIN"] as string;
                            base.TabColorNameVisibility = Visibility.Visible;
                            base.TabColorVisibility = Visibility.Collapsed;
                        }
                        else if (string.IsNullOrEmpty(Protocol) && (Session.Type == "ssh") && ProxySession == null)
                        {
                            base.TabColorNameVisibility = Visibility.Collapsed;
                            base.TabColorVisibility = Visibility.Visible;
                        }
                        if (CurProgram.Flags.Contains("mstsc") && NeedInTab())
                        {
                            if (!PutInTab())
                            {
                                CloseSelf();
                            }
                        }
                        else if (!NeedInTab())
                        {
                            CloseSelf();
                        }
                        break;
                    }
                }
            }
            Thread.Sleep(100);
            appProcess.Refresh();
        }

        MonitorAppMainWindow_done = true;
    }

    private void MoveAppMainWindow(IntPtr CurAppWin)
    {
        if(string.IsNullOrEmpty(Monitor))
        {
            return;
        }

        int WindowWidth = 0;
        int WindowHeight = 0;
        if (Win32.GetWindowRect(CurAppWin, out var AppRect))
        {
            WindowWidth = AppRect.Right - AppRect.Left;
            WindowHeight = AppRect.Bottom - AppRect.Top;
        }

        System.Windows.Forms.Screen monitor = null;

        if(Monitor == "*")
        {
            monitor = System.Windows.Forms.Screen.PrimaryScreen;
        }

        if (monitor == null)
        {
            int n = Int32.Parse(Monitor);

            if(n < 0 || n >= System.Windows.Forms.Screen.AllScreens.Length)
            {
                n = System.Windows.Forms.Screen.AllScreens.Length -1;
            }

            monitor = System.Windows.Forms.Screen.AllScreens[n];
        }

        AppRect.Left = monitor.WorkingArea.Left;

        if(monitor.WorkingArea.Width >= WindowWidth)
        {
            AppRect.Left += (monitor.WorkingArea.Width - WindowWidth) / 2;
        }

        AppRect.Top = monitor.WorkingArea.Top;

        if(monitor.WorkingArea.Height >= WindowHeight)
        {
            AppRect.Top += (monitor.WorkingArea.Height - WindowHeight) / 2;
        }

        Win32.SetWindowPos(CurAppWin, IntPtr.Zero, AppRect.Left, AppRect.Top, WindowWidth, WindowHeight, Win32.SWP_SHOWWINDOW);
      }

    private void MonitorNewAppMainWindow()
    {
        IntPtr CurAppWin;
        Process appProcess = AppProcess;
        while (!appProcess.HasExited)
        {
            try
            {
                CurAppWin = appProcess.MainWindowHandle;
            }
            catch (Exception)
            {
                CurAppWin = IntPtr.Zero;
            }
            if (CurAppWin != IntPtr.Zero && AppWin != CurAppWin)
            {
                int WindowWidth = 0;
                int WindowHeight = 0;
                if (Win32.GetWindowRect(CurAppWin, out var AppRect))
                {
                    WindowWidth = AppRect.Right - AppRect.Left;
                    WindowHeight = AppRect.Bottom - AppRect.Top;
                }
                if (WindowWidth == 0 || WindowHeight == 0)
                {
                    IntPtr hWnd = GetAppOtherMainWindow(CurAppWin, appProcess);
                    if (hWnd != CurAppWin)
                    {
                        CurAppWin = hWnd;
                        if (Win32.GetWindowRect(CurAppWin, out AppRect))
                        {
                            WindowWidth = AppRect.Right - AppRect.Left;
                            WindowHeight = AppRect.Bottom - AppRect.Top;
                        }
                    }
                }
                if (WindowWidth != 0 && WindowHeight != 0 && Win32.IsWindowVisible(CurAppWin))
                {
                    AppWin = CurAppWin;
                    if (!PutInTab())
                    {
                        CloseSelf();
                    }
                    return;
                }
            }
            Thread.Sleep(100);
            appProcess.Refresh();
        }
        try
        {
            ExitCode = appProcess.ExitCode;
        }
        catch (Exception)
        {
            ExitCode = 0;
        }
        if (Session.Type == "process" || Session.Type == "window")
        {
            base.MainWindow.MainWindowVM.RemoveTabWithExitedProcess(appProcess, null);
            return;
        }
        State = ConnectionState.Disconnected;
        base.ProcessExited(this);
    }

    private bool isConsoleWindow = false;
    private IntPtr hwndPanel = IntPtr.Zero;
    private CancellationTokenSource WaitAppExitTaskCancel = new CancellationTokenSource();
    private int WindowMaxWidth;
    private int WindowMaxHeight;
    private uint originalWindowStyle;
    private bool PutInTab()
    {
        try
        {
            int count = 0;
            bool AppNoGUI = false;
            try
            {
                AppProcess.WaitForInputIdle();
            }
            catch (Exception)
            {
                AppNoGUI = true;
            }
            if (!Win32.GetWindowRect(AppWin, out var lpRect))
            {
                return false;
            }
            int WindowWidth = lpRect.Right - lpRect.Left;
            int WindowHeight = lpRect.Bottom - lpRect.Top;
            if (WindowWidth == 0 || WindowHeight == 0)
            {
                return true;
            }

            originalWindowStyle = Win32.GetWindowLong(AppWin, Win32.GWL_STYLE);
            if((CurProgram.iFlags & ProgramConfig.FLAG_FULLSCREEN_CHECK) != 0)
            { 
                if((originalWindowStyle & Win32.WS_CAPTION)==0)
                {
                    return false;
                }
            }

            if (App.WindowHandleManager.ContainsKey(AppWin))
            {
                return false;
            }

            IntPtr hwd = IntPtr.Zero;
            if (hwndPanel == IntPtr.Zero)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(delegate
                {
                    hwndPanel = Panel.Handle;
                });
            }

            if (CurProgram.Flags.Contains("keepparent"))
            {
                if((originalWindowStyle & Win32.WS_MAXIMIZE)!=0)
                {
                    Win32.ShowWindow(AppWin, Win32.WindowShowStyle.Restore);
                    originalWindowStyle &= ~Win32.WS_MAXIMIZE;
                }
            }
            else
            {
                if(CurProgram.Flags.Contains("mstsc") && (Session.Width == 0 || Session.Height == 0))
                {
                    int width = Panel.DisplayRectangle.Width;
                    int height = Panel.DisplayRectangle.Height;

                    for (count = 0; count < App.Config.GUI.WaitTimeout; count++)
                    {
                        if(WindowWidth >= width && WindowHeight >= height)
                        {
                            break;
                        }

                        Thread.Sleep(100);

                        if (!Win32.GetWindowRect(AppWin, out lpRect))
                        {
                            break;
                        }
                        WindowWidth = lpRect.Right - lpRect.Left;
                        WindowHeight = lpRect.Bottom - lpRect.Top;
                    }
                }

                for (count = 0; count < App.Config.GUI.WaitTimeout; count++)
                {
                    hwd = Win32.SetParent(AppWin, hwndPanel);
                    if (hwd != IntPtr.Zero)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                if (hwd == IntPtr.Zero)
                {
                    return false;
                }
            }

            App.WindowHandleManager[AppWin] = this;

            if (CurProgram.Flags.Contains("mstsc") && CurProgram.WindowStyleMask != 0 && !CurProgram.Flags.Contains("keepparent"))
            {
                originalWindowStyle |= Win32.WS_VISIBLE;
                uint num3 = originalWindowStyle & ~CurProgram.WindowStyleMask;
                Win32.SetWindowLong(AppWin, Win32.GWL_STYLE, num3);
                for (count = 0; count < App.Config.GUI.WaitTimeout; count++)
                {
                    if (Win32.SetWindowLong(AppWin, Win32.GWL_STYLE, num3) == num3)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
            if (!IsInTab)
            {
                IsInTab = true;
                if (ProcessName != null)
                {
                    AppProcess.Exited += AppProcess_Exited;
                    AppProcessID = AppProcess.Id;
                }
                RaisePropertyChanged("SwitchWindowTitleBarCommandVisible");
                RaisePropertyChanged("KickCommandVisible");
            }

            StringBuilder ClassName = new StringBuilder(256);
            if (Win32.GetClassName(AppWin, ClassName, ClassName.Capacity) != 0 && ClassName.ToString() == "ConsoleWindowClass")
            {
                isConsoleWindow = true;
            }

            if (CurProgram.Flags.Count == 0 && isConsoleWindow)
            {
                Version win10 = new Version(10, 0);
                if(App.OSVersion < win10 || !RegistryHelper.IsConsoleV2())
                {
                    CurProgram.Flags = new List<string> { "keepws", "nomaximize" };
                }
            }

            WindowMaxWidth = 65536;
            WindowMaxHeight = 65536;
            if ((originalWindowStyle & Win32.WS_THICKFRAME) == 0 || CurProgram.Flags.Contains("noresize"))
            {
                WindowMaxWidth = WindowWidth;
                WindowMaxHeight = WindowHeight;
            }
            if (CurProgram.Flags.Contains("mstsc"))
            {
                Win32.ShowWindow(AppWin, Win32.WindowShowStyle.ShowMaximized);
                if (Win32.GetWindowRect(AppWin, out lpRect))
                {
                    WindowMaxWidth = lpRect.Right - lpRect.Left;
                    WindowMaxHeight = lpRect.Bottom - lpRect.Top;
                }
                Win32.ShowWindow(AppWin, Win32.WindowShowStyle.Restore);
            }
            if (!CurProgram.Flags.Contains("nomaximize") && !CurProgram.Flags.Contains("noresize") && (originalWindowStyle & Win32.WS_THICKFRAME) != 0)
            {
                Win32.ShowWindow(AppWin, Win32.WindowShowStyle.ShowMaximized);
            }

            W32Rect PanelRect;
            PanelRect.Left = PanelRect.Top = 0;

            if (CurProgram.Flags.Contains("keepparent"))
            { 
                ((Win32.ITaskbarList4)App.TaskbarList).DeleteTab(AppWin);
                Win32.GetWindowRect(hwndPanel, out PanelRect);

                while (Win32.GetForegroundWindow() != AppWin)
                {
                    AppProcess.Refresh();
                    if (AppProcess.HasExited)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                    Win32.SetForegroundWindow(AppWin);
                }
            }

            if ((originalWindowStyle & Win32.WS_THICKFRAME) == 0 || CurProgram.Flags.Contains("noresize"))
            {
                int Left = Panel.Left;
                if(Panel.Width > WindowMaxWidth)
                {
                    Left += (Panel.Width - WindowMaxWidth) / 2;
                }

                int Top = Panel.Top;
                if(Panel.Height > WindowMaxHeight)
                {
                    Top += (Panel.Height - WindowMaxHeight) / 2;
                }

                Win32.SetWindowPos(AppWin, IntPtr.Zero, PanelRect.Left + Left, PanelRect.Top + Top, WindowMaxWidth, WindowMaxHeight, Win32.SWP_SHOWWINDOW);
            }
            else
            {
                Win32.SetWindowPos(AppWin, IntPtr.Zero,  PanelRect.Left + 1, PanelRect.Top, (Panel.Width > WindowMaxWidth) ? WindowMaxWidth : Panel.Width, (Panel.Height > WindowMaxHeight) ? WindowMaxHeight : Panel.Height, Win32.SWP_SHOWWINDOW);
                if (winEventHook == IntPtr.Zero)
                {
                    Win32.SetWindowPos(AppWin, IntPtr.Zero, PanelRect.Left, PanelRect.Top, (Panel.Width > WindowMaxWidth) ? WindowMaxWidth : Panel.Width, (Panel.Height > WindowMaxHeight) ? WindowMaxHeight : Panel.Height, Win32.SWP_SHOWWINDOW);
                }
            }

            if (!AppNoGUI || isConsoleWindow)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(RegisterWindowsEventHook);
            }
            else
            {
                Task.Run(delegate
                {
                    Process appProcess = AppProcess;

                    try
                    {
                        using (WaitAppExitTaskCancel.Token.Register(Thread.CurrentThread.Abort))
                        {
                            appProcess.WaitForExit();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }

                    try
                    {
                        ExitCode = appProcess.ExitCode;
                    }
                    catch (Exception)
                    {
                        ExitCode = 0;
                    }
                    if (Session.Type == "process" || Session.Type == "window")
                    {
                        base.MainWindow.MainWindowVM.RemoveTabWithExitedProcess(appProcess, null);
                    }
                    else
                    {
                        State = ConnectionState.Disconnected;
                        base.ProcessExited(this);
                    }
                }, WaitAppExitTaskCancel.Token);
            }

            if ((originalWindowStyle & Win32.WS_THICKFRAME) != 0 && !CurProgram.Flags.Contains("noresize") && !CurProgram.Flags.Contains("keepws") && CurProgram.WindowStyleMask != 0 && !CurProgram.Flags.Contains("mstsc"))
            {
                Task.Run(delegate
                {
                    bool failed = true;
                    count = 0;
                    while (failed && count < 60)
                    {
                        count++;
                        System.Windows.Application.Current.Dispatcher.Invoke(delegate
                        {
                            failed = !_SwitchWindowTitleBar();
                        });
                        if (AppProcess == null || AppProcess.HasExited || AppWin == IntPtr.Zero)
                        {
                            break;
                        }
                        uint num6 = Win32.GetWindowLong(AppWin, Win32.GWL_STYLE);
                        if ((originalWindowStyle & Win32.WS_MAXIMIZE) == 0)
                        {
                            num6 &= ~Win32.WS_MAXIMIZE;
                        }
                        if (num6 == originalWindowStyle)
                        {
                            failed = true;
                        }
                    }
                });
            }
            return true;
        }
        catch (Exception exception)
        {
            log.Error("PutInTab failed", exception);
        }
        return false;
    }

    private bool NeedInTab()
    {
        if ((iFlags & ProgramConfig.FLAG_NOTINTAB) != 0)
        {
            if (!SSHv2Share)
            {
                return false;
            }
            iFlags &= ~ProgramConfig.FLAG_CLOSE_MASK;
            iFlags |= ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;
        }
        return true;
    }

    private void Disconnect()
    {
        if (Created)
        {
            foreach(PipeServer server in PipeServers)
            {
                server.Close();
            }
            PipeServers.Clear();

            if (AppProcess != null && !AppProcess.HasExited)
            {
                AppProcess.Exited -= AppProcess_Exited;
                CloseApp();
                UnregisterWindowsEventHook();
                try
                {
                    if ((child && ProcessName != null) || ProxyByPipe)
                    {
                        ProcessHelper.KillProcessByEnv(new List<string> { InstanceId });
                    }
                }
                catch (Exception exception)
                {
                    log.Warn("Unable to kill process", exception);
                }
                if ((iFlags & ProgramConfig.FLAG_CLOSE_MASK) != ProgramConfig.FLAG_CLOSE_BY_KICK || SSHv2Share)
                {
                    AppProcess.WaitForExit();
                    RemoveSSHv2ShareTabs();
                }
                AppProcess = null;
                IsInTab = false;
                AppProcessID = 0;
            }
            Created = false;
        }
        UnregisterWindowsEventHook();
    }

    private bool IsSSHv2ShareMainTab()
    {
        if (Session.Type != "ssh" || !string.IsNullOrEmpty(Protocol) || !SSHv2Share)
        {
            return false;
        }
        if (!App.SSHv2ShareManager.ContainsKey(ShareKey) || App.SSHv2ShareManager[ShareKey].Count == 0)
        {
            return false;
        }
        ObservableCollection<TabBase> observableCollection = App.SSHv2ShareManager[ShareKey];
        if (observableCollection[0] == this && observableCollection.Count > 1)
        {
            return true;
        }
        return false;
    }

    private void RemoveSSHv2ShareTabs()
    {
        foreach (KeyValuePair<string, int> sshv2ShareKey in SSHv2ShareKeys)
        {
            if (App.SSHv2ShareManager.ContainsKey(sshv2ShareKey.Key))
            {
                App.SSHv2ShareManager[sshv2ShareKey.Key].Remove(this);
            }
        }
        if (Session.Type != "ssh" || !string.IsNullOrEmpty(Protocol) || !SSHv2Share || !App.SSHv2ShareManager.ContainsKey(ShareKey) || App.SSHv2ShareManager[ShareKey].Count == 0)
        {
            return;
        }
        ObservableCollection<TabBase> observableCollection = App.SSHv2ShareManager[ShareKey];
        if (observableCollection[0] == this)
        {
            if (kicked)
            {
                observableCollection[0] = null;
                return;
            }
            while (observableCollection.Count > 1)
            {
                observableCollection[1].CloseTab(noconfirm: true);
                observableCollection.Remove(observableCollection[1]);
            }
        }
        observableCollection.Remove(this);
    }

    public override bool ConfirmClosingTab()
    {
        if (AppProcess == null || AppProcess.HasExited || !IsInTab || AppProcessID == 0)
        {
            return false;
        }
        if (Session.Type == "process" || Session.Type == "window")
        {
            return false;
        }
        if (IsSSHv2ShareMainTab())
        {
            return true;
        }
        if (App.Config.GUI.ConfirmClosingTab)
        {
            return true;
        }
        if (App.Config.GUI.ConfirmClosingWindow)
        {
            return true;
        }
        return false;
    }

    private const uint WM_QUIT = 0x0012;
    private const uint WM_CLOSE = 0x0010;
    private void CloseApp()
    {
        switch(iFlags & ProgramConfig.FLAG_CLOSE_MASK)
        { 
        case ProgramConfig.FLAG_CLOSE_BY_KILL:
            if (!ProcessNotExit)
            {
                try
                {
                    AppProcess.Kill();
                    Killed = true;
                    return;
                }
                catch (Exception exception)
                {
                    log.Warn("Unable to kill process", exception);
                    return;
                }
            }
            break;
        case ProgramConfig.FLAG_CLOSE_BY_KICK:
            if (IsInTab)
            {
                if (SSHv2Share)
                {
                    Win32.PostMessage(AppWin, WM_QUIT, 0, 0);
                }
                else
                {
                    _KickTab();
                }
            }
            break;
        case ProgramConfig.FLAG_CLOSE_BY_WM_QUIT:
            Win32.PostMessage(AppWin, WM_QUIT, 0, 0);
            break;
        case ProgramConfig.FLAG_CLOSE_BY_WM_CLOSE:
            Win32.PostMessage(AppWin, WM_CLOSE, 0, 0);
            break;
        }
    }

    public override bool CloseTab(bool noconfirm = false)
    {
        if (AppProcess == null || AppProcess.HasExited || !IsInTab || AppProcessID == 0)
        {
            Closed = true;
            return true;
        }
        if (Session.Type == "process" || Session.Type == "window")
        {
            KickTab();
            Closed = true;
            return true;
        }
        if (!noconfirm && (App.Config.GUI.ConfirmClosingTab || IsSSHv2ShareMainTab()))
        {
            string title = string.Format(System.Windows.Application.Current.Resources["ClosingTab"] as string, base.TabName);
            if (IsSSHv2ShareMainTab())
            {
                int num = 0;
                string text = "\n";
                foreach (TabBase item in App.SSHv2ShareManager[ShareKey])
                {
                    if (item == null || item == this)
                    {
                        continue;
                    }
                    if (num > 0)
                    {
                        text += ", ";
                        if (num % 4 == 0)
                        {
                            text += "\n";
                        }
                    }
                    num++;
                    text = text + "\"" + item.TabName + "\"";
                }
                title = string.Format(System.Windows.Application.Current.Resources["ClosingTab2"] as string, base.TabName, text);
            }
            ConfirmationDialog confirmationDialog = new ConfirmationDialog(base.MainWindow, System.Windows.Application.Current.Resources["CloseTab"] as string, title) { Topmost = true };
            confirmationDialog.Focus();
            confirmationDialog.ShowDialog();
            if (!confirmationDialog.Confirmed)
            {
                return false;
            }
        }
        if ((iFlags & ProgramConfig.FLAG_CLOSE_MASK) == ProgramConfig.FLAG_CLOSE_BY_KICK && !SSHv2Share)
        {
            noconfirm = true;
        }
        if (noconfirm)
        {
            _KickTab();
        }
        CloseApp();
        Closed = noconfirm;
        if(ProcessNotExit)
        {
            Closed = true;
        }
        return Closed;
    }

    private void TypePassword()
    {
        try
        {
            while (Win32.GetForegroundWindow() != AppWin)
            {
                AppProcess.Refresh();
                if (AppProcess.HasExited)
                {
                    break;
                }
                Thread.Sleep(100);
                Win32.SetForegroundWindow(AppWin);
            }
            if (AppProcess.HasExited)
            {
                return;
            }

            if(AutoInputUsername && (iFlags & ProgramConfig.FLAG_PASSWORD_ONLY) == 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(Session.WaitSeconds));
                SendInputHelper.Type(Credential.Username, true);
            }

            if (PasswordToPump != null)
            {
                Thread.Sleep(TimeSpan.FromSeconds(Session.WaitSeconds));
                SendInputHelper.Type(PasswordToPump.ToString(), true);
            }
        }
        catch (Exception exception)
        {
            log.Error("Error typing password.", exception);
        }
    }

    private void TypeScript()
    {
        if (Session.ScriptId == Guid.Empty || !string.IsNullOrEmpty(Protocol) || ProxySession != null)
        {
            return;
        }

        if(string.IsNullOrEmpty(Credential?.Username))
        {
            return;
        }

        if (!string.IsNullOrEmpty(Credential?.PrivateKeyPath))
        {
            if(SafeString.IsNullOrEmpty(Credential?.Passphrase))
            {
                return;
            }
        }
        else if(SafeString.IsNullOrEmpty(Credential?.Password))
        {
            return;
        }

        try
        {
            while (Win32.GetForegroundWindow() != AppWin)
            {
                AppProcess.Refresh();
                if (AppProcess.HasExited)
                {
                    break;
                }
                Thread.Sleep(100);
                Win32.SetForegroundWindow(AppWin);
            }
            if (AppProcess.HasExited)
            {
                return;
            }

            ConfigFile script = App.Sessions.ConfigFiles.FirstOrDefault(s => s.Id == Session.ScriptId);
            if(script != null && !string.IsNullOrEmpty(script.Data)) 
            {
                foreach(string line in Regex.Split(script.Data, @"(?<=[\n])"))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(Session.WaitSeconds));
                    SendInputHelper.Type(line);
                }
            }
        }
        catch (Exception exception)
        {
            log.Error("Error typing password.", exception);
        }
    }

    private double Panel_Width;
    private double Panel_Height;

    private bool notResizeAgain;
    public void Resize()
    {
        int num = 0;
        if ((double)Panel.Width == Panel_Width && (double)Panel.Height == Panel_Height && !CurProgram.Flags.Contains("keepparent"))
        {
            return;
        }
        Panel_Width = Panel.Width;
        Panel_Height = Panel.Height;

        if (hwndPanel == IntPtr.Zero)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                hwndPanel = Panel.Handle;
            });
        }
        W32Rect PanelRect;
        PanelRect.Left = PanelRect.Top = 0;
        if (CurProgram.Flags.Contains("keepparent"))
        {
            Win32.GetWindowRect(hwndPanel, out PanelRect);
        }

        if ((originalWindowStyle & Win32.WS_THICKFRAME) == 0 || CurProgram.Flags.Contains("noresize"))
        {
            if (WindowMaxWidth == 0 || WindowMaxHeight == 0)
            {
                return;
            }
            int Left = Panel.Left;
            if(Panel.Width > WindowMaxWidth)
            {
                Left += (Panel.Width - WindowMaxWidth) / 2;
            }
            int Top = Panel.Top;
            if(Panel.Height > WindowMaxHeight)
            {
                Top += (Panel.Height - WindowMaxHeight) / 2;
            }

            notResizeAgain = (winEventHook != IntPtr.Zero);

            if (CurProgram.Flags.Contains("keepparent"))
            {
                Win32.GetWindowRect(AppWin, out var AppRect);
                if((PanelRect.Left + Left) == AppRect.Left && (PanelRect.Top + Top) == AppRect.Top)
                {
                    notResizeAgain = false;
                }
            }

            Win32.SetWindowPos(AppWin, IntPtr.Zero, PanelRect.Left + Left, PanelRect.Top + Top, WindowMaxWidth, WindowMaxHeight, Win32.SWP_SHOWWINDOW);
            num = 0;
            while (notResizeAgain)
            {
                Thread.Sleep(10);
                num++;
                if (num > 200)
                {
                    break;
                }
            }
            return;
        }

        notResizeAgain = !CurProgram.Flags.Contains("keepparent") && (winEventHook != IntPtr.Zero);

        Win32.SetWindowPos(AppWin, IntPtr.Zero, PanelRect.Left, PanelRect.Top, (Panel.Width > WindowMaxWidth) ? WindowMaxWidth : Panel.Width, (Panel.Height > WindowMaxHeight) ? WindowMaxHeight : Panel.Height, Win32.SWP_SHOWWINDOW);
        num = 0;
        while (notResizeAgain)
        {
            Thread.Sleep(10);
            num++;
            if (num > 200)
            {
                break;
            }
        }
        if (!CurProgram.Flags.Contains("winscp"))
        {
            return;
        }
        StringBuilder ClassName = new StringBuilder(256);
        if (Win32.GetClassName(AppWin, ClassName, ClassName.Capacity) == 0)
        {
            return;
        }
        string classname = ClassName.ToString();
        if (!App.Config.WinSCP.ClassName.Contains(classname))
        {
            return;
        }
        notResizeAgain = !CurProgram.Flags.Contains("keepparent") && (winEventHook != IntPtr.Zero);
        Win32.SetWindowPos(AppWin, IntPtr.Zero, PanelRect.Left, PanelRect.Top, (Panel.Width > WindowMaxWidth) ? WindowMaxWidth : Panel.Width, (Panel.Height > WindowMaxHeight) ? WindowMaxHeight : Panel.Height, Win32.SWP_SHOWWINDOW);
        num = 0;
        while (notResizeAgain)
        {
            Thread.Sleep(10);
            num++;
            if (num > 200)
            {
                break;
            }
        }
    }

    private void UpdateTabTitle()
    {
        StringBuilder Title = new StringBuilder(1024);
        if (Win32.GetWindowText(AppWin, Title, Title.Capacity) != 0)
        {
            string title = Title.ToString();
            if ((iFlags & ProgramConfig.FLAG_SYNCTITLE) != 0)
            {
                base.TabName = title;
                base.MainWindow.MainWindowVM.UpdateTitle();
            }
            
            if (string.IsNullOrEmpty(Protocol) && title == "PuTTY (inactive)")
            {
                base.UnderlineColor = (System.Windows.Application.Current.Resources["g0"] as SolidColorBrush).Color;
            }
        }
    }

    private CancellationTokenSource MonitorNewAppMainWindowTaskCancel = new CancellationTokenSource();

    private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (eventType == Win32.EVENT_SYSTEM_CAPTUREEND)
        {
            if(hwnd == IntPtr.Zero)
            {
                return;
            }

            IntPtr window = Win32.GetTopWindow(IntPtr.Zero);

            while (AppWin != hwnd && hwnd != window && hwnd != IntPtr.Zero)
            {
                hwnd = Win32.GetParent(hwnd);
            }

            if(AppWin != hwnd)
            {
                return;
            }

            if(!Win32.GetWindowRect(AppWin, out var AppRect))
            {
                ActivateMainWindow();
                return;
            }

            do
            {
                if(window == base.MainWindow.Handle)
                {
                    return;
                }

                if(Win32.IsWindowVisible(window))
                {
                    if(!Win32.GetWindowRect(window, out var lpRect))
                    {
                        break;
                    }
                   
                    if(AppRect.Top < lpRect.Bottom && AppRect.Bottom > lpRect.Top && AppRect.Left < lpRect.Right && AppRect.Right > lpRect.Left)
                    { 
                        IntPtr owner = Win32.GetWindow(window, Win32.GW_OWNER);
                        if(owner != base.MainWindow.Handle)
                        {
                            break;
                        }
                    }
                }

                window = Win32.GetWindow(window, Win32.GW_HWNDNEXT);
            }
            while (window != IntPtr.Zero);

            ActivateMainWindow();
            return;
        }

        if (AppWin != hwnd)
        {
            return;
        }
        switch (eventType)
        {
        case Win32.EVENT_OBJECT_LOCATIONCHANGE:
            if (idObject != Win32.OBJID_WINDOW)
            {
                break;
            }
            if (notResizeAgain)
            {
                notResizeAgain = false;
            }
            else
            {
                uint style = Win32.GetWindowLong(AppWin, Win32.GWL_STYLE);

                if((style & Win32.WS_MINIMIZE) != 0)
                {
                    ActivateMainWindow();
                    return;
                }

                if ((CurProgram.Flags.Contains("mstsc") && (style & Win32.WS_MAXIMIZE) != 0) || !Win32.GetWindowRect(Panel.Handle, out var PanelRect) || !Win32.GetWindowRect(AppWin, out var AppRect))
                {
                    break;
                }

                W32Rect PanelFixRect;
                PanelFixRect.Left = PanelFixRect.Top = 0;

                if(CurProgram.Flags.Contains("keepparent"))
                {
                    PanelFixRect = PanelRect;
                }

                if ((originalWindowStyle & Win32.WS_THICKFRAME) == 0 || CurProgram.Flags.Contains("noresize"))
                {
                    WindowMaxWidth = AppRect.Right - AppRect.Left;
                    WindowMaxHeight = AppRect.Bottom - AppRect.Top;
                    int Left = Panel.Left;
                    if(Panel.Width > WindowMaxWidth)
                    {
                        Left += (Panel.Width - WindowMaxWidth) / 2;
                    }
                    int Top = Panel.Top;
                    if(Panel.Height > WindowMaxHeight)
                    {
                        Top += (Panel.Height - WindowMaxHeight) / 2;
                    }

                    if (AppRect.Left != Panel.Left + Left || AppRect.Top != Panel.Top + Top)
                    {
                        Win32.SetWindowPos(AppWin, IntPtr.Zero, PanelFixRect.Left + Left, PanelFixRect.Top + Top, WindowMaxWidth, WindowMaxHeight, Win32.SWP_SHOWWINDOW);
                    }
                }
                else if (AppRect.Left != PanelRect.Left || AppRect.Top != PanelRect.Top || ((AppRect.Bottom != PanelRect.Bottom || AppRect.Right != PanelRect.Right) && (WindowMaxWidth != AppRect.Right - AppRect.Left || WindowMaxHeight != AppRect.Bottom - AppRect.Top)))
                {
                    Win32.SetWindowPos(AppWin, IntPtr.Zero, PanelFixRect.Left, PanelFixRect.Top, (Panel.Width > WindowMaxWidth) ? WindowMaxWidth : Panel.Width, (Panel.Height > WindowMaxHeight) ? WindowMaxHeight : Panel.Height, Win32.SWP_SHOWWINDOW);
                }
            }
            break;
        case Win32.EVENT_OBJECT_NAMECHANGE:
            UpdateTabTitle();
            break;
        case Win32.EVENT_OBJECT_DESTROY:
            if (idObject != Win32.OBJID_WINDOW)
            {
                break;
            }
            UnregisterWindowsEventHook();
            App.WindowHandleManager.Remove(AppWin);
            if (MonitorAppMainWindow_done && NeedInTab())
            {
                MonitorNewAppMainWindowTaskCancel.Cancel();
                MonitorNewAppMainWindowTaskCancel = new CancellationTokenSource();
                Task.Run(delegate
                {
                    try
                    {
                        using (MonitorNewAppMainWindowTaskCancel.Token.Register(Thread.CurrentThread.Abort))
                        {
                            MonitorNewAppMainWindow();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                }, MonitorNewAppMainWindowTaskCancel.Token);
            }
            break;
        }
    }

    protected override void ActivateTab()
    {
        if (AppWin != IntPtr.Zero && Win32.GetForegroundWindow() != AppWin)
        {
            if ((Win32.GetWindowLong(AppWin, Win32.GWL_STYLE) & Win32.WS_MINIMIZE) != 0)
            {
                Win32.ShowWindow(AppWin, Win32.WindowShowStyle.Restore);
            }

            if (CurProgram.Flags.Contains("keepparent"))
            {
                Resize();
            }

            if (!Win32.SetForegroundWindow(AppWin))
            {
                log.Warn("Unable to activate App.");
            }
            else
            {
                FlashWindow.Stop(AppWin);
            }
        }
    }

    protected override void DeactivateTab()
    {
        HideWin(true);
    }

    private void ActivateMainWindow()
    {
        base.MainWindow.Topmost = true;
        base.MainWindow.Activate();
        base.MainWindow.Topmost = false;
        FlashWindow.Stop(base.MainWindow.Handle);
    }

    public void HideWin(bool bHide)
    {
        if (!CurProgram.Flags.Contains("keepparent"))
        {
            return;
        }

        if (kicked || Closed)
        {
            bHide = false;
        }

        Win32.ShowWindow(AppWin, bHide ? Win32.WindowShowStyle.Hide: Win32.WindowShowStyle.Show);
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
