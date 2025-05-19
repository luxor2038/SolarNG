using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ChromeTabs;
using GalaSoft.MvvmLight.Command;
using log4net;
using SolarNG.Sessions;
using SolarNG.Utilities;
using SolarNG.ViewModel;

namespace SolarNG;

public partial class MainWindow : Window, IStyleConnector
{
    public MainWindowViewModel MainWindowVM { get; set; }

    private DateTime lastChange;

    private bool waitingMerge;

    private void Win_LocationChanged(object sender, EventArgs e)
    {
        Window window = (Window)sender;
        if (!window.IsLoaded)
        {
            return;
        }
        W32Point pt = default;
        if (!Win32.GetCursorPos(ref pt))
        {
            int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
            log.Error($"Unable to get Cursor position, LastError: {hRForLastWin32Error}");
            return;
        }
        Point point = new Point(pt.X, pt.Y);
        MainWindow mainWindow = (MainWindow)FindWindowUnderThisAt(window, point);
        if (mainWindow != null)
        {
            Point point2 = mainWindow.PointFromScreen(point);
            if (mainWindow.MyChromeTabControl.InputHitTest(point2) is FrameworkElement element && CanInsertTabItem(element))
            {
                if (waitingMerge)
                {
                    return;
                }
                lastChange = DateTime.UtcNow;
                waitingMerge = true;
                Task.Run(delegate
                {
                    Thread.Sleep(1000);
                    if (DateTime.UtcNow - lastChange < TimeSpan.FromMilliseconds(900.0))
                    {
                        waitingMerge = false;
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)window.DataContext;
                            int count = mainWindow.MainWindowVM.ItemCollection.Count;
                            foreach (TabBase item in mainWindowViewModel.ItemCollection)
                            {
                                item.TabNumber = count++;
                                mainWindow.MainWindowVM.ItemCollection.Add(item);
                                item.MainWindow = mainWindow.MainWindowVM.mainWindowInstance;
                            }
                            mainWindowViewModel.ItemCollection.Clear();
                            MainWindowVM = mainWindow.MainWindowVM;
                            mainWindow.MainWindowVM.IsWindowMerging = true;
                            window.Close();
                            mainWindow.MainWindowVM.IsWindowMerging = false;
                            if (mainWindow.MainWindowVM.ItemCollection.Any())
                            {
                                mainWindow.MainWindowVM.SelectedTab = mainWindow.MainWindowVM.ItemCollection.First();
                            }
                        });
                        waitingMerge = false;
                    }
                });
                return;
            }
        }
        lastChange = DateTime.UtcNow;
    }

    private void Win_Closing(object sender, CancelEventArgs e)
    {
        if (MainWindowVM.IsWindowMerging)
        {
            return;
        }
        int num = 0;
        foreach (TabBase item in MainWindowVM.ItemCollection)
        {
            if (item.ConfirmClosingTab())
            {
                num++;
            }
        }
        if (num > 0)
        {
            ConfirmationDialog confirmationDialog = new ConfirmationDialog(this, Application.Current.Resources["CloseWindow"] as string, Application.Current.Resources["ClosingAllTabs"] as string) { Topmost = true };
            confirmationDialog.Focus();
            confirmationDialog.ShowDialog();
            if (!confirmationDialog.Confirmed)
            {
                e.Cancel = true;
                return;
            }
        }
        foreach (TabBase tab in MainWindowVM.ItemCollection)
        {
            tab.CloseTab(noconfirm: true);
            tab.Cleanup();
        }
        if (App.mainWindows.Count == 1)
        {
            try
            {
                App.Sessions.Save(App.DataFilePath);
                App.Histories.Save(App.DataFilePath);
            }
            catch (Exception message)
            {
                log.Error(message);
            }
        }
    }

    public bool IsWindowClosed;
    private void Win_Closed(object sender, EventArgs e)
    {
        IsWindowClosed = true;
        MainWindowVM.Cleanup();
        App.mainWindows.Remove(this);
    }

    private void setMaximizeIcon(WindowState windowState)
    {
        if(App.OSVersion.Major >= 10 &&  App.OSVersion.Build >= 22000)
        {
            if(windowState == WindowState.Maximized)
            {
                maximizeIcon.Data = Application.Current.Resources["Maximized11Path"] as Geometry;;
            }
            else
            {
                maximizeIcon.Data = Application.Current.Resources["Maximize11Path"] as Geometry;;
            }
        }
        else
        {
            if(windowState == WindowState.Maximized)
            {
                maximizeIcon.Data = Application.Current.Resources["MaximizedPath"] as Geometry;;
            }
            else
            {
                maximizeIcon.Data = Application.Current.Resources["MaximizePath"] as Geometry;;
            }
        }
    }

    private void Win_StateChanged(object sender, EventArgs e)
    {
        RowDefinition rowDefinition = MainGrid.RowDefinitions.ElementAt(1);
        switch (base.WindowState)
        {
        case WindowState.Normal:
            Chrome.ResizeBorderThickness = new Thickness(5.0);
            Chrome.GlassFrameThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
            base.BorderThickness = new Thickness(0.0);
            if(App.Config.GUI.Logo)
            {
                rowDefinition.Height = new GridLength(16.0);
            }
            setMaximizeIcon(WindowState.Normal);
            break;
        case WindowState.Minimized:
            if (MainWindowVM.SelectedTab is AppTabViewModel appTabViewModel)
            {
                appTabViewModel.HideWin(true);
            }
            return;
        case WindowState.Maximized:
            Chrome.ResizeBorderThickness = new Thickness(0.0);
            Chrome.GlassFrameThickness = new Thickness(0.0);
            base.BorderThickness = new Thickness(App.Config.GUI.MaximizedBorderThickness);
            rowDefinition.Height = new GridLength(0.0);
            setMaximizeIcon(WindowState.Maximized);
            break;
        }
        MainWindowVM.ActivateTab();
    }

    public IntPtr Handle { get; set; }

    public bool First;

    private double InitWidth;

    private double InitHeight;

    private HwndSource hwndSource;

    private Button minimizeButton;
    private Button maximizeButton;
    private Button closeButton;
    private Path minimizeIcon;
    private Path maximizeIcon;
    private Path closeIcon;

    private ChromeTabPanel tabPanel; 

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        minimizeButton = MyChromeTabControl.Template.FindName("MinimizeButton", MyChromeTabControl) as Button;
        maximizeButton = MyChromeTabControl.Template.FindName("MaximizeButton", MyChromeTabControl) as Button;
        closeButton = MyChromeTabControl.Template.FindName("CloseButton", MyChromeTabControl) as Button;
        minimizeIcon = MyChromeTabControl.Template.FindName("MinimizeIcon", MyChromeTabControl) as Path;
        maximizeIcon = MyChromeTabControl.Template.FindName("MaximizeIcon", MyChromeTabControl) as Path;
        closeIcon = MyChromeTabControl.Template.FindName("CloseIcon", MyChromeTabControl) as Path;
        tabPanel = MyChromeTabControl.Template.FindName("PART_TabPanel", MyChromeTabControl) as ChromeTabPanel;
        
        Handle = new WindowInteropHelper(this).Handle;
        MainWindowVM.RegisterWindowResizeEvent();

        Product.Text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + " " + Assembly.GetExecutingAssembly().GetName().Version;
        Copyright.Text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;

        if (!App.Config.GUI.Logo)
        {
            MainGrid.RowDefinitions.ElementAt(1).Height = new GridLength(0.0);
        }

        setMaximizeIcon(WindowState.Normal);

        hwndSource = HwndSource.FromHwnd(Handle);
        hwndSource.AddHook(WndProc);

        if (!First)
        {
            return;
        }

        System.Windows.Forms.Screen CurScreen = null;

        if(string.IsNullOrEmpty(App.Config.GUI.Monitor))
        {
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                if(base.Left >= screen.Bounds.Left && base.Left < screen.Bounds.Right && base.Top >= screen.Bounds.Top && base.Top < screen.Bounds.Bottom)
                {
                    CurScreen = screen;
                    break;
                }
            }
        }
        else if(App.Config.GUI.Monitor == "*")
        {
            CurScreen = System.Windows.Forms.Screen.PrimaryScreen;
        }

        if(CurScreen == null)
        {
            int monitor = string.IsNullOrEmpty(App.Config.GUI.Monitor) ? 0 : Int32.Parse(App.Config.GUI.Monitor);

            if(monitor < 0 || monitor >= System.Windows.Forms.Screen.AllScreens.Length)
            {
                monitor = System.Windows.Forms.Screen.AllScreens.Length -1;
            }

            CurScreen = System.Windows.Forms.Screen.AllScreens[monitor];
        }

        double num = Win32.GetScalingFactor(this);
        if (num == 0.0)
        {
            num = 1.0;
        }
        double ScreenWidth = CurScreen.WorkingArea.Width/num;
        double ScreenHeight = CurScreen.WorkingArea.Height/num;

        base.MinWidth /= num;
        base.MinHeight /= num;
        base.Width /= num;
        base.Height /= num;

        if (App.Config.GUI.Width != 0 && App.Config.GUI.Height != 0)
        {
            base.Width = (double)App.Config.GUI.Width/num;
            base.Height = (double)App.Config.GUI.Height/num;
        }

        if((base.Width / ScreenWidth) < App.Config.GUI.MinWidthScale)
        {
            base.Width = App.Config.GUI.MinWidthScale * ScreenWidth;
            base.Height = base.Width * 0.75;
        }

        if(base.Width > ScreenWidth)
        {
            base.Width = ScreenWidth;
        }
        if(base.Height > ScreenHeight)
        {
            base.Height = ScreenHeight;
        }

        if (base.MinWidth > base.Width)
        {
            base.MinWidth = base.Width;
        }
        if (base.MinHeight > base.Height)
        {
            base.MinHeight = base.Height;
        }

        InitWidth = base.Width;
        InitHeight = base.Height;

        base.Left = CurScreen.WorkingArea.Left/num;
        if(ScreenWidth > base.Width)
        {
            base.Left += (ScreenWidth - base.Width) / 2.0;
        }
        base.Top = CurScreen.WorkingArea.Top/num;
        if(ScreenHeight > base.Height)
        {
            base.Top += (ScreenHeight - base.Height) / 2.0;
        }

        if (!App.Config.GUI.Maximized)
        {
            return;
        }
        Task.Run(delegate
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                base.WindowState = WindowState.Maximized;
            });
        });
    }

    private void MainWindow_OnActivated(object sender, EventArgs e)
    {
        base.Dispatcher.Invoke(() => Task.Delay(250).ContinueWith(delegate
        {
            MainWindowVM.ActivateTab();
        }));
    }

    private void MainWindow_OnDeactivated(object sender, EventArgs e)
    {
    }

    private void TextBlockTriple_MouseUp(object sender, MouseButtonEventArgs e)
    {
        ContextMenu obj = (ContextMenu)MyChromeTabControl.Template.FindName("MainMenu", MyChromeTabControl);
        obj.PlacementTarget = sender as Grid;
        obj.IsOpen = true;
    }

   private void ContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        MainWindowVM.CanActivate = false;
    }

    private void ContextMenu_OnClosed(object sender, RoutedEventArgs e)
    {
        if (MainWindowVM.SelectedTab is AppTabViewModel)
        {
            (MainWindowVM.SelectedTab as AppTabViewModel).Resize();
        }
        MainWindowVM.CanActivate = true;
    }

    public MainWindow(Options opts)
    {
        InitializeComponent();

        First = (opts != null);

        MainWindowVM = new MainWindowViewModel(this);

        base.DataContext = MainWindowVM;
        if (opts != null)
        {
            ProcessCommandLineArguments(opts);
        }
    }

    public void CloseIME(IntPtr hwnd)
    {
        IntPtr himc = Win32.ImmGetContext(hwnd);
        if (himc != IntPtr.Zero)
        {
            if(!Win32.ImmGetOpenStatus(himc))
            {
                Win32.ImmSetOpenStatus(himc, true);
            }

            Win32.ImmSetOpenStatus(himc, false);

            Win32.ImmReleaseContext(hwnd, himc);
        }
        else
        {
            IntPtr hwndIME = Win32.ImmGetDefaultIMEWnd(hwnd);
            if(hwndIME == IntPtr.Zero)
            {
                return;
            }

            if(Win32.SendMessage(hwndIME, Win32.WM_IME_CONTROL, Win32.IMC_GETOPENSTATUS, 0) == IntPtr.Zero)
            {
                Win32.SendMessage(hwndIME, Win32.WM_IME_CONTROL, Win32.IMC_SETOPENSTATUS, 1);
            }

            Win32.SendMessage(hwndIME, Win32.WM_IME_CONTROL, Win32.IMC_SETOPENSTATUS, 0);
        }
    }

    public void ProcessCommandLineArguments(Options opts)
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            if (!base.IsVisible)
            {
                Show();
            }
            if (base.WindowState == WindowState.Minimized)
            {
                base.WindowState = WindowState.Normal;
            }
            Activate();
            base.Topmost = true;
            base.Topmost = false;
            Focus();

            if(App.Config.GUI.CloseIME)
            {
                CloseIME(Handle);
            }
        });

        if (opts.SessionIDs == null)
        {
            return;
        }
        foreach (string sessionid in opts.SessionIDs)
        {
            Session storedSession = App.Sessions.Sessions.FirstOrDefault((Session t) => t.Id.ToString() == sessionid);
            if (storedSession != null)
            {
                Credential credential = App.Sessions.Credentials.FirstOrDefault((Credential c) => c.Id == storedSession.CredentialId);
                Application.Current.Dispatcher.Invoke(delegate
                {
                    MainWindowVM.AddNewTab(MainWindowVM.CreateAppTab(storedSession, credential, this));
                });
            }
            else
            {
                log.Warn("Unable to find stored session with id " + sessionid);
            }
        }
    }

    private bool CanInsertTabItem(FrameworkElement element)
    {
        if (element is ChromeTabItem)
        {
            return true;
        }
        if (element is ChromeTabPanel)
        {
            return true;
        }
        if (LogicalTreeHelper.GetChildren(element).Cast<object>().FirstOrDefault((object x) => x is ChromeTabPanel) != null)
        {
            return true;
        }
        FrameworkElement frameworkElement = element;
        while (true)
        {
            object obj = frameworkElement?.TemplatedParent;
            if (obj == null)
            {
                break;
            }
            if (obj is ChromeTabItem)
            {
                return true;
            }
            frameworkElement = frameworkElement.TemplatedParent as FrameworkElement;
        }
        return false;
    }

    private Window FindWindowUnderThisAt(Window source, Point screenPoint)
    {
        return (from win in SortWindowsTopToBottom(Application.Current.Windows.OfType<MainWindow>())
            where (win.WindowState == WindowState.Maximized || new Rect(win.Left, win.Top, win.Width, win.Height).Contains(screenPoint)) && !object.Equals(win, source)
            select win).FirstOrDefault();
    }

    private IEnumerable<MainWindow> SortWindowsTopToBottom(IEnumerable<Window> unsorted)
    {
        Dictionary<IntPtr, Window> byHandle = unsorted.ToDictionary((Window win) => ((HwndSource)PresentationSource.FromVisual(win)).Handle);
        IntPtr hWnd = Win32.GetTopWindow(IntPtr.Zero);
        while (hWnd != IntPtr.Zero)
        {
            if (byHandle.ContainsKey(hWnd))
            {
                yield return (MainWindow)byHandle[hWnd];
            }
            hWnd = Win32.GetWindow(hWnd, 2u);
        }
    }

    public void CreateNewMainWindow()
    {
        MainWindow mainWindow = NewMainWindow();
        mainWindow.MainWindowVM.AddOverviewTab();
    }

    public MainWindow NewMainWindow()
    {
        MainWindow newMainWindow = CreateNewWindowInstance();
        newMainWindow.Show();
        newMainWindow.WindowState = base.WindowState;
        return newMainWindow;
    }

    private MainWindow CreateNewWindowInstance()
    {
        MainWindow mainWindow = new MainWindow(null)
        {
            InitWidth = InitWidth,
            InitHeight = InitHeight,
            MinWidth = base.MinWidth,
            MinHeight = base.MinHeight,
            Width = ((base.WindowState == WindowState.Maximized) ? InitWidth : base.Width),
            Height = ((base.WindowState == WindowState.Maximized) ? InitHeight : base.Height)
        };
        App.mainWindows.Add(mainWindow);
        return mainWindow;
    }

    private void CloseWindow()
    {
        Close();
    }

    private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        CloseWindow();
    }

    internal void DetachTabToNewWindow(TabBase detachedTab)
    {
        MainWindow mainWindow = CreateNewWindowInstance();
        log.Debug($"Detaching tab {detachedTab} from old window {GetHashCode()} to new window {mainWindow.GetHashCode()}");
        if (MainWindowVM.ItemCollection.Contains(detachedTab))
        {
            detachedTab.Detaching = true;
            MainWindowVM.ItemCollection.Remove(detachedTab);
        }
        else
        {
            log.Warn($"Unable to find {detachedTab} in ItemCollection");
        }
        detachedTab.MainWindow = mainWindow;
        detachedTab.DetachCommand = new RelayCommand<TabBase>(mainWindow.DetachTabToNewWindow);
        mainWindow.MainWindowVM.ItemCollection.Add(detachedTab);
        detachedTab.Detaching = false;
        mainWindow.MainWindowVM.SelectedTab = detachedTab;

        if(detachedTab is AppTabViewModel detachedAppTab)
        {
            if (detachedAppTab.GetAppProcessExited() != null)
            {
                detachedAppTab.AppProcessExited -= MainWindowVM.RemoveTabWithExitedProcess;
                detachedAppTab.AppProcessExited += mainWindow.MainWindowVM.RemoveTabWithExitedProcess;
            }
        }

        mainWindow.Show();
        mainWindow.WindowState = base.WindowState;
        mainWindow.BringIntoView();
    }

    private bool isMinButtonHovered;
    private bool isMaxButtonHovered;
    private bool isCloseButtonHovered;

    private SolidColorBrush closeColor = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
    private SolidColorBrush closeIconColor = new SolidColorBrush(Colors.White);
    private void UpdateButtonVisualState()
    {
        minimizeButton.Background = isMinButtonHovered ? Application.Current.Resources["t10"] as SolidColorBrush : MyChromeTabControl.Background;
        maximizeButton.Background = isMaxButtonHovered ? Application.Current.Resources["t10"] as SolidColorBrush : MyChromeTabControl.Background;
        closeButton.Background = isCloseButtonHovered ?  closeColor : MyChromeTabControl.Background;

        closeIcon.Fill = isCloseButtonHovered ? closeIconColor : Application.Current.Resources["t00"] as SolidColorBrush;

        Dispatcher.Invoke(() => { });
    }

    private bool IsPointInControl(Point point, FrameworkElement control)
    {
        Point relativePoint = control.PointFromScreen(PointToScreen(point));
        return (relativePoint.X >= 0 && relativePoint.X <= control.ActualWidth &&
                relativePoint.Y >= 0 && relativePoint.Y <= control.ActualHeight);
    }

    private unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch ((uint)msg)
        {
        case Win32.WM_KEYDOWN:
        case Win32.WM_SYSKEYDOWN:
            const int VK_F4 = 0x73;
            var key = wParam.ToInt32();
            bool altPressed = ((lParam.ToInt32() >> 29) & 1) == 1;
            
            if (key == VK_F4 && altPressed && !App.Config.GUI.Hotkey)
            {
                handled = true;
            }
            break;

        case Win32.WM_NCHITTEST:
            Point position = new Point((Int16)(((UIntPtr)lParam.ToPointer()).ToUInt32() & 0xFFFF), (Int16)(((UIntPtr)lParam.ToPointer()).ToUInt32() >> 16));
            Point clientPoint = PointFromScreen(position);
                    
            bool wasMinButtonHovered = isMinButtonHovered;
            bool wasMaxButtonHovered = isMaxButtonHovered;
            bool wasCloseButtonHovered = isCloseButtonHovered;
                    
            isMinButtonHovered = false;
            isMaxButtonHovered = false;
            isCloseButtonHovered = false;
                    
            if (IsPointInControl(clientPoint, minimizeButton))
            {
                isMinButtonHovered = true;
                handled = true;
                if (wasMinButtonHovered != isMinButtonHovered)
                {
                    UpdateButtonVisualState();
                }
                return new IntPtr(Win32.HTMINBUTTON);
            }
            else if (IsPointInControl(clientPoint, maximizeButton))
            {
                isMaxButtonHovered = true;
                handled = true;
                if (wasMaxButtonHovered != isMaxButtonHovered)
                {
                    UpdateButtonVisualState();
                }
                return new IntPtr(Win32.HTMAXBUTTON);
            }
            else if (IsPointInControl(clientPoint, closeButton))
            {
                isCloseButtonHovered = true;
                handled = true;
                if (wasCloseButtonHovered != isCloseButtonHovered)
                {
                    UpdateButtonVisualState();
                }
                return new IntPtr(Win32.HTCLOSE);
            }
                    
            if (wasMinButtonHovered != isMinButtonHovered ||
                wasMaxButtonHovered != isMaxButtonHovered ||
                wasCloseButtonHovered != isCloseButtonHovered)
            {
                UpdateButtonVisualState();
            }
            break;
        case Win32.WM_NCLBUTTONDOWN:
            position = new Point((Int16)(((UIntPtr)lParam.ToPointer()).ToUInt32() & 0xFFFF), (Int16)(((UIntPtr)lParam.ToPointer()).ToUInt32() >> 16));
            clientPoint = PointFromScreen(position);
                    
            if (IsPointInControl(clientPoint, minimizeButton))
            {
                isMinButtonHovered = false;
                isMaxButtonHovered = false;
                isCloseButtonHovered = false;
                UpdateButtonVisualState();

                this.WindowState = WindowState.Minimized;

                handled = true;
                return IntPtr.Zero;
            }
            else if (IsPointInControl(clientPoint, maximizeButton))
            {
                isMinButtonHovered = false;
                isMaxButtonHovered = false;
                isCloseButtonHovered = false;
                UpdateButtonVisualState();

                if (this.WindowState == WindowState.Maximized)
                    this.WindowState = WindowState.Normal;
                else
                    this.WindowState = WindowState.Maximized;

                handled = true;
                return IntPtr.Zero;
            }
            else if (IsPointInControl(clientPoint, closeButton))
            {
                CloseWindow();
                handled = true;
                return IntPtr.Zero;
            }
            break;
        case Win32.WM_NCMOUSELEAVE:
            if (isMinButtonHovered || isMaxButtonHovered || isCloseButtonHovered)
            {
                isMinButtonHovered = false;
                isMaxButtonHovered = false;
                isCloseButtonHovered = false;
                UpdateButtonVisualState();
            }
            break;
        case Win32.WM_NCMOUSEMOVE:
            {
                Win32.TRACKMOUSEEVENT tme = new Win32.TRACKMOUSEEVENT
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(Win32.TRACKMOUSEEVENT)),
                    dwFlags = Win32.TME_NONCLIENT | Win32.TME_LEAVE,
                    hwndTrack = this.Handle,
                    dwHoverTime = 0
                };
                Win32.TrackMouseEvent(ref tme);
            }
            break;
        }
        return IntPtr.Zero;
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
