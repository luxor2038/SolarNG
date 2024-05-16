using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using log4net;
using log4net.Config;
using SolarNG.Sessions;
using SolarNG.Utilities;
using SolarNG.ViewModel;

namespace SolarNG;

public partial class App : Application
{
    private static ILog log;

    internal static SolarNG.Configs.Config Config;
    internal static ExportModel Sessions;
    internal static HistoryModel Histories;
    internal static bool IsSaving;

    public static HotKeys hotKeys;

    public static ObservableCollection<Session> HistorySessions = new ObservableCollection<Session>();
    public static Dictionary<string, ObservableCollection<TabBase>> SSHv2ShareManager = new Dictionary<string, ObservableCollection<TabBase>>();
    public static Dictionary<IntPtr, TabBase> WindowHandleManager = new Dictionary<IntPtr, TabBase>();

    public static byte[] passHash;
    public static byte[] passSalt;

    public static string DataFilePath;

    public static Version OSVersion;

    public static object TaskbarList;

    public static void main(string[] args)
    {
        try
        {
            ArrayList msg = (ArrayList)XmlConfigurator.Configure();

            if(!LogManager.GetRepository().Configured)
            {
                throw new Exception(msg[0].ToString());
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message);

            Application.Current.Shutdown();
        }

        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

        GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;

        log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        log.Info("VERSION: " + GetCurrentVersion());

        if (!SingleGlobalInstance(1000))
        {
            if (PassParameters(args))
            {
                log.Info("Successfully passed parameters to another window, exiting.");
            }
            else
            {
                log.Warn("Unable to pass parameters to another window, exiting.");
            }

            Application.Current.Shutdown();
            return;
        }

        FillSessionColorsList();

        Win32.OSVERSIONINFOEX oSVERSIONINFOEX = default;
        oSVERSIONINFOEX.OSVersionInfoSize = Marshal.SizeOf(typeof(Win32.OSVERSIONINFOEX));
        Win32.OSVERSIONINFOEX versionInfo = oSVERSIONINFOEX;
        Win32.RtlGetVersion(ref versionInfo);
        OSVersion = new Version(versionInfo.MajorVersion, versionInfo.MinorVersion);

        DataFilePath = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["DataFilePath"] ?? "");
        if (!Path.IsPathRooted(DataFilePath))
        {
            DataFilePath = Path.Combine(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).LocalPath, DataFilePath);
        }
        try
        {
            if (!Directory.Exists(DataFilePath))
            {
                Directory.CreateDirectory(DataFilePath);
            }
        }
        catch (Exception exception)
        {
            log.Error("Failed to create data file path!", exception);
            Application.Current.Shutdown();
        }

        Config = SolarNG.Configs.Config.Load(DataFilePath);
        if(Config == null)
        {
            Application.Current.Shutdown();
        }

        SetLanguageDictionary(App.Config.GUI.Language);
        SetThemeDictionary(App.Config.GUI.Theme);

        hotKeys = new HotKeys
        {
            HotKeysDisabled = !App.Config.GUI.Hotkey
        };

        AddBuiltinTypes();

        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        if (!File.Exists(App.Config.PuTTY.FullPath))
        {
            ConfirmationDialog confirmationDialog = new ConfirmationDialog(null, "SolarNG", string.Format(System.Windows.Application.Current.Resources["PuttyNotFound"] as string, "\"" + App.Config.PuTTY.FullPath + "\"", "\n"), hideCancelButton: true)
            {
                Topmost = true,
            };
            confirmationDialog.Focus();
            confirmationDialog.ShowDialog();
        }

        if(App.Config.MasterPassword && !ExportModel.IsEncrypted(DataFilePath))
        {
            passSalt = Crypto.Argon2dSalt();

            PromptDialog promptDialog;
            promptDialog = new PromptDialog(null, System.Windows.Application.Current.Resources["InputMasterPassword"] as string, System.Windows.Application.Current.Resources["EnterMasterPassword"] as string, "", password: true) { Topmost = true };
            promptDialog.Focus();
            bool? flag = promptDialog.ShowDialog();
            if (!flag.HasValue || !flag.Value)
            {
                Application.Current.Shutdown();
            }

            passHash = Crypto.Argon2dHash(promptDialog.MyPassword.Password, passSalt);

            Sessions = ExportModel.Load(DataFilePath);
            if (Sessions == null)
            {
                Application.Current.Shutdown();
            }

            if(!Sessions.Save(DataFilePath, true))
            {
                Application.Current.Shutdown();
            }
        }
        else
        {
            Sessions = ExportModel.Load(DataFilePath);
            if (Sessions == null)
            {
                Application.Current.Shutdown();
            }
        }

        Sessions.AddBuiltinTypes();

        Histories = HistoryModel.Load(DataFilePath);

        Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;

        JumpListManager.SetNewJumpList(Sessions.Sessions);

        try
        {
            PipeServer.Start();
        }
        catch(Exception ex)
        {
            log.Error(ex);
            Application.Current.Shutdown();
        }

        TaskbarList = (Win32.ITaskbarList4) new Win32.CTaskbarList();
        ((Win32.ITaskbarList4)TaskbarList).HrInit();

        RunApplicationWithOptions(ParseParameters(args));
    }

    private static void SetLanguageDictionary(string Language)
    {
        ResourceDictionary resourceDictionary = new ResourceDictionary();

        if(string.IsNullOrEmpty(Language))
        { 
            Language = Thread.CurrentThread.CurrentCulture.ToString();
        }

        try
        {
            resourceDictionary.Source = new Uri("/SolarNG;component/strings." + Language + ".xaml", UriKind.Relative);
        }
        catch(Exception)
        {
            resourceDictionary.Source = new Uri("/SolarNG;component/strings.xaml", UriKind.Relative);
        }

        Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
    }

    private static void SetThemeDictionary(string Theme)
    {
        ResourceDictionary resourceDictionary = new ResourceDictionary();
    
        if(string.IsNullOrEmpty(Theme))
        {
            Theme = RegistryHelper.AppsUseLightTheme() ? "" : "dark";
        }

        try
        {
            resourceDictionary.Source = new Uri("/SolarNG;component/colors." + Theme + ".xaml", UriKind.Relative);
        }
        catch (Exception)
        {
            resourceDictionary.Source = new Uri("/SolarNG;component/colors.xaml", UriKind.Relative);
        }

        Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
    }

    public static byte[] UserHash;

    private static Mutex mutex;
    private static void InitMutex()
    {
        string id = string.Empty;

        ManagementClass mc = new ManagementClass("Win32_ComputerSystemProduct");
        ManagementObjectCollection moc = mc.GetInstances();

        foreach (ManagementBaseObject mo in moc)
        {
            id = mo.Properties["UUID"].Value.ToString();
            break;
        }

        id += Environment.UserDomainName + Environment.UserName;

        using (SHA256 sha256 = SHA256.Create())
        {
            UserHash = sha256.ComputeHash(Encoding.ASCII.GetBytes(id)).Take(16).ToArray();
        }

        mutex = new Mutex(initiallyOwned: false, "Global\\SolarNG_" + new Guid(UserHash).ToString());
        MutexAccessRule rule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
        MutexSecurity mutexSecurity = new MutexSecurity();
        mutexSecurity.AddAccessRule(rule);
        mutex.SetAccessControl(mutexSecurity);
    }

    private static bool hasMutexHandle;
    private static bool SingleGlobalInstance(int timeOut)
    {
        InitMutex();
        try
        {
            hasMutexHandle = mutex.WaitOne((timeOut < 0) ? (-1) : timeOut, exitContext: false);
        }
        catch (AbandonedMutexException)
        {
            hasMutexHandle = true;
        }
        return hasMutexHandle;
    }

    public static Options ParseParameters(string[] args)
    {
        Options options = new Options();
        if (args.Length >= 2 && args[0] == "-i")
        {
            options.SessionIDs = new string[1] { args[1] };
        }
        return options;
    }

    private static bool PassParameters(string[] args)
    {
        try
        {
            using (ChannelFactory<IWcfService> channelFactory = new ChannelFactory<IWcfService>(new NetNamedPipeBinding(), "net.pipe://localhost/SolarNG-WCF/" + WindowsIdentity.GetCurrent().Name.Replace('\\', '-') + "/" + WindowsIdentity.GetCurrent().Name.Replace('\\', '-')))
            {
                channelFactory.CreateChannel().PassArguments(args);
            }
            return true;
        }
        catch (Exception)
        {
        }
        return false;
    }

    private static void HostWCF()
    {
        while (true)
        {
            ServiceHost serviceHost = new ServiceHost(typeof(WcfService), new Uri("net.pipe://localhost/SolarNG-WCF/" + WindowsIdentity.GetCurrent().Name.Replace('\\', '-')));
            try
            {
                ServiceMetadataBehavior serviceMetadataBehavior = serviceHost.Description.Behaviors.Find<ServiceMetadataBehavior>() ?? new ServiceMetadataBehavior();
                serviceMetadataBehavior.HttpsGetEnabled = false;
                serviceMetadataBehavior.HttpGetEnabled = false;
                serviceMetadataBehavior.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                serviceHost.Description.Behaviors.Add(serviceMetadataBehavior);
                serviceHost.AddServiceEndpoint("IMetadataExchange", MetadataExchangeBindings.CreateMexNamedPipeBinding(), "mex");
                serviceHost.AddServiceEndpoint(typeof(IWcfService), new NetNamedPipeBinding(), WindowsIdentity.GetCurrent().Name.Replace('\\', '-'));
                serviceHost.Open();
                break;
            }
            catch (Exception)
            {
                serviceHost.Abort();
                Thread.Sleep(5000);
            }
        }
    }

    private static void RunApplicationWithOptions(Options options)
    {
        new Task(HostWCF).Start();
        RunOptionsAndReturnExitCode(options);
    }

    public static List<MainWindow> mainWindows = new List<MainWindow>();
    private static void RunOptionsAndReturnExitCode(Options opts)
    {
        MainWindow mainWindow = new MainWindow(opts);
        mainWindows.Add(mainWindow);
        if (mainWindow.MainWindowVM.ItemCollection.Count == 0)
        {
            mainWindow.MainWindowVM.AddOverviewTab();
        }
        mainWindow.Show();
    }

    private static string GetCurrentVersion()
    {
        return (from a in AppDomain.CurrentDomain.GetAssemblies()
            select a.GetName()).FirstOrDefault((AssemblyName a) => a.Name == "SolarNG")?.Version.ToString();
    }

    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        log.FatalFormat("Ex: {0}", e.ExceptionObject);
        Cleanup();
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        main(e.Args);
    }

    private void App_OnExit(object sender, ExitEventArgs e)
    {
        Cleanup();
    }

    private static void Cleanup()
    {
        hotKeys?.Dispose();
        if (mutex != null)
        {
            if (hasMutexHandle)
            {
                mutex.ReleaseMutex();
            }
            mutex.Close();
        }
    }

    public static Random random = new Random();

    public static string GetColor(bool random = false)
    {
        int num = 0x176998;
        string color = GetColor(num);
        while (App.Config.GUI.RandomColor || random)
        {
            num = App.random.Next(1, 0xFFFFFE);
            color = GetColor(num);
            if(color != Application.Current.Resources["t9"].ToString())
            {
                break;
            }
        }
        return color;
    }

    private static string GetColor(int color)
    {
        return "#" + color.ToString("x6");
    }

    public static ObservableCollection<Brush> SessionColors { get; set; }
    private static void FillSessionColorsList()
    {
        int[] colors = new int[39]
        {
            0, 150, 255, 38891, 48033, 65280, 65535, 79494, 1539439, 3180784,
            4329504, 5592405, 6291626, 6710976, 7030155, 7160382, 8283185, 8289833, 8750469, 10048724,
            10375240, 10688368, 10704407, 10887387, 10935080, 10938112, 12321328, 12369186, 12763842, 14468080,
            14533135, 15239095, 15263922, 16711680, 16711843, 16741120, 16776960, 16754399, 16764582
        };
        BrushConverter brushConverter = new BrushConverter();
        SessionColors = new ObservableCollection<Brush>();
        foreach (int color in colors)
        {
            SessionColors.Add((SolidColorBrush)brushConverter.ConvertFrom(GetColor(color)));
        }
    }

    internal static ObservableCollection<SessionType> BuiltinSessionTypes = new ObservableCollection<SessionType>();
    private static void AddBuiltinTypes()
    {
        BuiltinSessionTypes.Add(new SessionType("", 0, SessionType.FLAG_BUILTIN) { Program = new Configs.ProgramConfig("") });
        BuiltinSessionTypes.Add(new SessionType("tag", 0, SessionType.FLAG_BUILTIN));
        BuiltinSessionTypes.Add(new SessionType("app", 0, SessionType.FLAG_BUILTIN) { AbbrName="a" });
        BuiltinSessionTypes.Add(new SessionType("window", 0, SessionType.FLAG_BUILTIN) { DisplayName="Window", AbbrDisplayName="WIN" });
        BuiltinSessionTypes.Add(new SessionType("process", 0, SessionType.FLAG_BUILTIN) { DisplayName="Process", AbbrDisplayName="PROC" });
        BuiltinSessionTypes.Add(new SessionType("history", 0, SessionType.FLAG_BUILTIN));
        BuiltinSessionTypes.Add(new SessionType("proxy", 1080, SessionType.FLAG_BUILTIN | SessionType.FLAG_SPECIAL_TYPE | SessionType.FLAG_PROXY_PROVIDER | SessionType.FLAG_CREDENTIAL) { Program = App.Config.PlinkX });
        BuiltinSessionTypes.Add(new SessionType("ssh", 22, SessionType.FLAG_BUILTIN | SessionType.FLAG_PROXY_PROVIDER | SessionType.FLAG_PROXY_CONSUMER | SessionType.FLAG_SSH_PROXY | SessionType.FLAG_CREDENTIAL) { AbbrName="s", Program = App.Config.PuTTY });
        BuiltinSessionTypes.Add(new SessionType("rdp", 3389, SessionType.FLAG_BUILTIN | SessionType.FLAG_PROXY_CONSUMER | SessionType.FLAG_CREDENTIAL) { AbbrName="r", Program = App.Config.MSTSC });
        BuiltinSessionTypes.Add(new SessionType("vnc", 5900, SessionType.FLAG_BUILTIN | SessionType.FLAG_PROXY_CONSUMER | SessionType.FLAG_CREDENTIAL) { AbbrName="v", Program = App.Config.VNCViewer });
        BuiltinSessionTypes.Add(new SessionType("scp", 22, SessionType.FLAG_BUILTIN | SessionType.FLAG_PROXY_PROVIDER | SessionType.FLAG_PROXY_CONSUMER | SessionType.FLAG_SSH_PROXY | SessionType.FLAG_CREDENTIAL) { AbbrName="sc", Program = App.Config.WinSCP });
        BuiltinSessionTypes.Add(new SessionType("sftp", 22, SessionType.FLAG_BUILTIN | SessionType.FLAG_PROXY_PROVIDER | SessionType.FLAG_PROXY_CONSUMER | SessionType.FLAG_SSH_PROXY | SessionType.FLAG_CREDENTIAL) { AbbrName="sf", Program = App.Config.WinSCP });
        BuiltinSessionTypes.Add(new SessionType("ftp", 21, SessionType.FLAG_BUILTIN | SessionType.FLAG_PROXY_CONSUMER | SessionType.FLAG_CREDENTIAL) { AbbrName="f", Program = App.Config.WinSCP });
        BuiltinSessionTypes.Add(new SessionType("telnet", 23, SessionType.FLAG_BUILTIN | SessionType.FLAG_PROXY_CONSUMER | SessionType.FLAG_SSH_PROXY  | SessionType.FLAG_CREDENTIAL) { AbbrName="t", DisplayName="Telnet", AbbrDisplayName="TEL", Program = App.Config.PuTTY });
    }

    public static event EventHandler RefreshOverviewHandler; 
    public static void RefreshOverview()
    {
        RefreshOverviewHandler?.Invoke(null, null);
    }

}
