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
using System.Windows.Media.Imaging;
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
        foreach (TabBase tab in MainWindowVM.ItemCollection)
        {
            tab.CloseTab(noconfirm: true);
        }
    }

    public bool IsWindowClosed;
    private void Win_Closed(object sender, EventArgs e)
    {
        IsWindowClosed = true;
        MainWindowVM.Cleanup();
        App.mainWindows.Remove(this);
    }

    private void Win_StateChanged(object sender, EventArgs e)
    {
        ((Image)MyChromeTabControl.Template.FindName("ImageMaximizeNormalize", MyChromeTabControl)).Source = ((base.WindowState == WindowState.Normal) ? (Application.Current.Resources["ImageMaximized"] as BitmapImage) : (Application.Current.Resources["ImageNormalized"] as BitmapImage));
        RowDefinition rowDefinition = MainGrid.RowDefinitions.ElementAt(1);
        switch (base.WindowState)
        {
        case WindowState.Normal:
            Chrome.ResizeBorderThickness = new Thickness(10.0);
            base.BorderThickness = new Thickness(0.0);
            if(App.Config.GUI.Logo)
            {
                rowDefinition.Height = new GridLength(16.0);
            }

            break;
        case WindowState.Minimized:
            if (MainWindowVM.SelectedTab is AppTabViewModel appTabViewModel)
            {
                appTabViewModel.HideWin(true);
            }
            return;
        case WindowState.Maximized:
            Chrome.ResizeBorderThickness = new Thickness(0.0);
            base.BorderThickness = new Thickness(App.Config.GUI.MaximizedBorderThickness);
            rowDefinition.Height = new GridLength(0.0);
            break;
        }
        MainWindowVM.ActivateTab();
    }

    public IntPtr Handle { get; set; }

    public bool First;

    private double InitWidth;

    private double InitHeight;

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        Handle = new WindowInteropHelper(this).Handle;
        MainWindowVM.RegisterWindowResizeEvent();

        Product.Text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + " " + Assembly.GetExecutingAssembly().GetName().Version;
        Copyright.Text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;

        if (!App.Config.GUI.Logo)
        {
            MainGrid.RowDefinitions.ElementAt(1).Height = new GridLength(0.0);
        }
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

        if (App.Config.GUI.Width != 0 && App.Config.GUI.Height != 0)
        {
            double num = Win32.GetScalingFactor(this);
            if (num == 0.0)
            {
                num = 1.0;
            }
            if (base.MinWidth > (double)App.Config.GUI.Width)
            {
                base.MinWidth = App.Config.GUI.Width;
            }
            if (base.MinHeight > (double)App.Config.GUI.Height)
            {
                base.MinHeight = App.Config.GUI.Height;
            }
            base.MinWidth /= num;
            base.MinHeight /= num;
            base.Width = (double)App.Config.GUI.Width / num;
            base.Height = (double)App.Config.GUI.Height / num;

            if((base.Width / CurScreen.WorkingArea.Width) < App.Config.GUI.MinWidthScale)
            {
                base.Width = App.Config.GUI.MinWidthScale * CurScreen.WorkingArea.Width;
                base.Height = base.Width * 0.75;
            }

            if(base.Width > CurScreen.WorkingArea.Width)
            {
                base.Width = CurScreen.WorkingArea.Width;
            }
            if(base.Height > CurScreen.WorkingArea.Height)
            {
                base.Height = CurScreen.WorkingArea.Height;
            }
        }
        InitWidth = base.Width;
        InitHeight = base.Height;
        base.Left = CurScreen.WorkingArea.Left;
        if(CurScreen.WorkingArea.Width > base.Width)
        {
            base.Left += (CurScreen.WorkingArea.Width - base.Width) / 2.0;
        }
        base.Top = CurScreen.WorkingArea.Top;
        if(CurScreen.WorkingArea.Height > base.Height)
        {
            base.Top += (CurScreen.WorkingArea.Height - base.Height) / 2.0;
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

    private void TextBlockMinimize_MouseUp(object sender, MouseButtonEventArgs e)
    {
        base.WindowState = WindowState.Minimized;
    }

    private void TextBlockMaximize_MouseUp(object sender, MouseButtonEventArgs e)
    {
        base.WindowState = ((base.WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal);
    }

    private void TextBlockClose_MouseUp(object sender, MouseButtonEventArgs e)
    {
        CloseWindow();
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
                return;
            }
        }
        Close();
    }

    public void CloseCurrentWindow()
    {
        CloseWindow();
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

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
