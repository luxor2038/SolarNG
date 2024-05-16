using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SolarNG.ViewModel;

namespace SolarNG.Utilities;

public class HotKeys : IDisposable
{
    private static object locker = new object();

    private static bool registeredHotkeys = false;

    private HotKey hotKeyCtrlShiftTab;

    private HotKey hotKeyCtrlTab;

    private HotKey hotKeyCtrlT;

    private HotKey hotKeyCtrlE;

    private HotKey hotKeyCtrlF5;

    private HotKey hotKeyCtrlW;

    private HotKey hotKeyCtrlShiftW;

    private HotKey hotKeyCtrlShiftT;

    private HotKey hotKeyCtrl1;

    private HotKey hotKeyCtrl2;

    private HotKey hotKeyCtrl3;

    private HotKey hotKeyCtrl4;

    private HotKey hotKeyCtrl5;

    private HotKey hotKeyCtrl6;

    private HotKey hotKeyCtrl7;

    private HotKey hotKeyCtrl8;

    private HotKey hotKeyCtrl9;

    private HotKey hotKeyCtrlN;

    private HotKey hotKeyCtrlH;

    private HotKey hotKeyCtrlS;

    private HotKey hotKeyAltF4;

    private HotKey hotKeyCtrlLeft;

    private HotKey hotKeyCtrlRight;

    private IntPtr keyboardHookId = IntPtr.Zero;

    private readonly Win32.LowLevelKmProc keyboardCallback;

    private bool disposed;

    private MainWindow MainWindow => GetMainWindowInForeground();

    private bool _HotKeysDisabled;
    public bool HotKeysDisabled
    {
        get
        {
            return _HotKeysDisabled;
        }
        set
        {
            _HotKeysDisabled = value;
            if (_HotKeysDisabled)
            {
                UnregisterHotKeys();
            }
            else
            {
                RegisterHotKeys();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HotKeysDisabled"));
        }
    }

    public bool IsMainWindowOrAppInForeground => GetMainWindowInForeground() != null;

    public event PropertyChangedEventHandler PropertyChanged;

    internal HotKeys()
    {
        keyboardCallback = KeyboardHookCallback;
        keyboardHookId = SetWindowsKeyboardHook(keyboardCallback);
    }

    private const uint WH_KEYBOARD_LL = 13;
    internal IntPtr SetWindowsKeyboardHook(Win32.LowLevelKmProc callback)
    {
        using Process process = Process.GetCurrentProcess();
        using ProcessModule processModule = process.MainModule;
        IntPtr moduleHandle = Win32.GetModuleHandle(processModule.ModuleName);
        return Win32.SetWindowsHookEx(WH_KEYBOARD_LL, callback, moduleHandle, 0u);
    }

    internal IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (IsMainWindowOrAppInForeground)
        {
            if (!HotKeysDisabled)
            {
                RegisterHotKeys();
            }
        }
        else
        {
            UnregisterHotKeys();
        }
        return Win32.CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
    }

    private MainWindow GetMainWindowInForeground()
    {
        IntPtr foregroundWindow = Win32.GetForegroundWindow();
        foreach (MainWindow item in Application.Current.Windows.OfType<MainWindow>())
        {
            if (foregroundWindow == GetMainWindowHandle(item) || foregroundWindow == GetAppWindowHandle(item))
            {
                return item;
            }
        }
        return null;
    }

    private IntPtr GetAppWindowHandle(MainWindow window)
    {
        if (window.MainWindowVM == null)
        {
            return IntPtr.Zero;
        }
        if (window.MainWindowVM.SelectedTab is AppTabViewModel appTabViewModel)
        {
            return appTabViewModel.AppWin;
        }
        return IntPtr.Zero;
    }

    private static IntPtr GetMainWindowHandle(MainWindow window)
    {
        return new WindowInteropHelper(window).Handle;
    }

    internal void RegisterHotKeys()
    {
        lock (locker)
        {
            if (!registeredHotkeys)
            {
                registeredHotkeys = true;
                hotKeyCtrlShiftTab = new HotKey(Key.Tab, KeyModifier.Ctrl | KeyModifier.Shift, OnCtrlShiftTabHandler);
                hotKeyCtrlTab = new HotKey(Key.Tab, KeyModifier.Ctrl, OnCtrlTabHandler);
                hotKeyCtrlT = new HotKey(Key.T, KeyModifier.Ctrl, OnCtrl_THandler);
                hotKeyCtrlE = new HotKey(Key.E, KeyModifier.Ctrl, OnCtrl_EHandler);
                hotKeyCtrlF5 = new HotKey(Key.F5, KeyModifier.Ctrl, OnCtrl_F5Handler);
                hotKeyCtrlW = new HotKey(Key.W, KeyModifier.Ctrl, OnCtrl_WHandler);
                hotKeyCtrlShiftW = new HotKey(Key.W, KeyModifier.Ctrl | KeyModifier.Shift, OnCtrlShift_WHandler);
                hotKeyCtrlShiftT = new HotKey(Key.T, KeyModifier.Ctrl | KeyModifier.Shift, OnCtrlShift_THandler);
                hotKeyCtrl1 = new HotKey(Key.D1, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl2 = new HotKey(Key.D2, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl3 = new HotKey(Key.D3, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl4 = new HotKey(Key.D4, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl5 = new HotKey(Key.D5, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl6 = new HotKey(Key.D6, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl7 = new HotKey(Key.D7, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl8 = new HotKey(Key.D8, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrl9 = new HotKey(Key.D9, KeyModifier.Ctrl, OnCtrl1_9Handler);
                hotKeyCtrlN = new HotKey(Key.N, KeyModifier.Ctrl, OnCtrlNHandler);
                hotKeyCtrlH = new HotKey(Key.H, KeyModifier.Ctrl, OnCtrlHHandler);
                hotKeyCtrlS = new HotKey(Key.S, KeyModifier.Ctrl, OnCtrlSHandler);
                hotKeyAltF4 = new HotKey(Key.F4, KeyModifier.Alt, OnAltF4Handler);
                hotKeyCtrlLeft = new HotKey(Key.Left, KeyModifier.Ctrl, OnCtrl_LeftHandler);
                hotKeyCtrlRight = new HotKey(Key.Right, KeyModifier.Ctrl, OnCtrl_RightHandler);
            }
        }
    }

    internal void UnregisterHotKeys()
    {
        lock (locker)
        {
            if (registeredHotkeys && hotKeyCtrlShiftTab != null)
            {
                hotKeyCtrlShiftTab.Unregister();
                hotKeyCtrlTab.Unregister();
                hotKeyCtrlE.Unregister();
                hotKeyCtrlF5.Unregister();
                hotKeyCtrlT.Unregister();
                hotKeyCtrlW.Unregister();
                hotKeyCtrlShiftW.Unregister();
                hotKeyCtrlShiftT.Unregister();
                hotKeyCtrl1.Unregister();
                hotKeyCtrl2.Unregister();
                hotKeyCtrl3.Unregister();
                hotKeyCtrl4.Unregister();
                hotKeyCtrl5.Unregister();
                hotKeyCtrl6.Unregister();
                hotKeyCtrl7.Unregister();
                hotKeyCtrl8.Unregister();
                hotKeyCtrl9.Unregister();
                hotKeyCtrlN.Unregister();
                hotKeyCtrlH.Unregister();
                hotKeyCtrlS.Unregister();
                hotKeyCtrlE.Unregister();
                hotKeyAltF4.Unregister();
                hotKeyCtrlLeft.Unregister();
                hotKeyCtrlRight.Unregister();
                registeredHotkeys = false;
            }
        }
    }

    private void DoAction(Action action, HotKey hotKey)
    {
        action();
    }

    private void OnCtrlShiftTabHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.ShiftSelectedTabLeft, hotKey);
        }
    }

    private void OnCtrlTabHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.ShiftSelectedTabRight, hotKey);
        }
    }

    private DateTime lastHotKeyTime = DateTime.UtcNow;
    private void OnCtrl_THandler(HotKey hotKey)
    {
        if ((DateTime.UtcNow - lastHotKeyTime) < TimeSpan.FromMilliseconds(1000.0))
        {
            return;
        }

        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.AddOverviewTab, hotKey);
            lastHotKeyTime = DateTime.UtcNow;
        }
    }

    private void OnCtrl_F5Handler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.Reconnect, hotKey);
        }
    }

    private void OnCtrl_EHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.AddNewSessionTab, hotKey);
        }
    }

    private void OnCtrl_WHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.CloseSelectedTab, hotKey);
        }
    }

    private void OnCtrlShift_WHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.CloseAllTab, hotKey);
        }
    }

    private void OnCtrlShift_THandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.OpenPreviouslyCloseTab, hotKey);
        }
    }

    private void OnCtrl1_9Handler(HotKey hotKey)
    {
        MainWindow?.MainWindowVM.SwitchToSpecifiedTab(hotKey);
    }

    private void OnCtrlNHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.CreateNewMainWindow, hotKey);
        }
    }

    private void OnCtrlHHandler(HotKey hotKey)
    {
        MainWindow?.MainWindowVM.OpenHistoryTab();
    }

    private void OnCtrlSHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.OnOpenSettings, hotKey);
        }
    }

    private void OnAltF4Handler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.CloseCurrentWindow, hotKey);
        }
    }

    private void OnCtrl_LeftHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.MoveSelectedTabLeft, hotKey);
        }
    }

    private void OnCtrl_RightHandler(HotKey hotKey)
    {
        if (MainWindow != null)
        {
            DoAction(MainWindow.MainWindowVM.MoveSelectedTabRight, hotKey);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                UnregisterHotKeys();
                Win32.UnhookWindowsHookEx(keyboardHookId);
            }
            disposed = true;
        }
    }
}
