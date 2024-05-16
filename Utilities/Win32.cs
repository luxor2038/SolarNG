using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace SolarNG.Utilities;

public class Win32
{
    [ComImportAttribute()]
    [GuidAttribute("c43dc798-95d1-4bea-9030-bb99e2983a1a")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITaskbarList4
    {
        // ITaskbarList
        [PreserveSig]
        void HrInit();
        [PreserveSig]
        void AddTab(IntPtr hwnd);
        [PreserveSig]
        void DeleteTab(IntPtr hwnd);
        [PreserveSig]
        void ActivateTab(IntPtr hwnd);
        [PreserveSig]
        void SetActiveAlt(IntPtr hwnd);

        // ITaskbarList2
        [PreserveSig]
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        // ITaskbarList3
        [PreserveSig]
        void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
        [PreserveSig]
        void SetProgressState(IntPtr hwnd, int tbpFlags);
        [PreserveSig]
        void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        [PreserveSig]
        void UnregisterTab(IntPtr hwndTab);
        [PreserveSig]
        void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        [PreserveSig]
        void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, int dwReserved);
    }

    [GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterfaceAttribute(ClassInterfaceType.None)]
    [ComImportAttribute()]
    internal class CTaskbarList { }

    internal struct OSVERSIONINFOEX
    {
        internal int OSVersionInfoSize;

        internal int MajorVersion;

        internal int MinorVersion;

        internal int BuildNumber;

        internal int PlatformId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string CSDVersion;

        internal ushort ServicePackMajor;

        internal ushort ServicePackMinor;

        internal short SuiteMask;

        internal byte ProductType;

        internal byte Reserved;
    }

    internal delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

    internal delegate IntPtr LowLevelKmProc(int nCode, IntPtr wParam, IntPtr lParam);

    internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    internal enum WindowShowStyle : uint
    {
        Hide = 0u,
        ShowNormal = 1u,
        ShowMinimized = 2u,
        ShowMaximized = 3u,
        Maximize = 3u,
        ShowNormalNoActivate = 4u,
        Show = 5u,
        Minimize = 6u,
        ShowMinNoActivate = 7u,
        ShowNoActivate = 8u,
        Restore = 9u,
        ShowDefault = 10u,
        ForceMinimized = 11u
    }

    internal const uint EVENT_SYSTEM_CAPTUREEND = 0x0009;
    internal const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
    internal const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;

    internal const uint EVENT_OBJECT_DESTROY = 0x8001;
    internal const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
    internal const uint EVENT_OBJECT_NAMECHANGE = 0x800C;

    internal const int OBJID_WINDOW = 0;

    internal const int GWL_STYLE = -16;
    	
    internal const uint WS_VISIBLE = 0x10000000;
    internal const uint WS_CAPTION = 0x00C00000;
    internal const uint WS_MAXIMIZE = 0x01000000;
    internal const uint WS_MINIMIZE = 0x20000000;
    internal const uint WS_POPUP = 0x80000000;
    internal const uint WS_THICKFRAME = 0x00040000;

    internal const int GW_HWNDNEXT = 2;
    internal const int GW_HWNDPREV = 3;
    internal const int GW_OWNER = 4;

    internal const uint SWP_SHOWWINDOW = 0x0040;

    internal const uint WM_IME_CONTROL = 0x0283;
    internal const uint IMC_GETOPENSTATUS = 0x0005;
    internal const uint IMC_SETOPENSTATUS = 0x0006;


    [DllImport("imm32.dll")]
    internal static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hwnd);

    [DllImport("imm32.dll")]
    internal static extern IntPtr ImmGetContext(IntPtr hwnd);

    [DllImport("imm32.dll")]
    internal static extern bool ImmReleaseContext(IntPtr hwnd, IntPtr himc);

    [DllImport("imm32.dll")]
    internal static extern bool ImmGetOpenStatus(IntPtr himc);

    [DllImport("imm32.dll")]
    internal static extern bool ImmSetOpenStatus(IntPtr himc, bool b);


    [DllImport("ntdll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(ref W32Point pt);

    [DllImport("user32.dll")]
    internal static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll")]
    internal static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll", SetLastError=true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    internal static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetTopWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetWindow(IntPtr hWnd, uint wCmd);

    [DllImport("user32.dll")]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool PostMessage(IntPtr hwnd, uint Msg, uint wParam, uint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SendMessage(IntPtr hwnd, uint Msg, uint wParam, uint lParam);

    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

    [DllImport("user32.DLL")]
    internal static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(uint idHook, LowLevelKmProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    internal static extern int GetDpiForWindow(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetWindowRect(IntPtr hwnd, out W32Rect lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    internal static double GetScalingFactor(MainWindow win)
    {
        IntPtr handle = win.Handle;
        PresentationSource presentationSource = PresentationSource.FromVisual(win);
        if (presentationSource != null)
        {
            return presentationSource.CompositionTarget.TransformToDevice.M11;
        }
        try
        {
            return (double)GetDpiForWindow(handle) / 96.0;
        }
        catch
        {
        }
        using Graphics graphics = Graphics.FromHwnd(handle);
        IntPtr hdc = graphics.GetHdc();
        int deviceCaps = GetDeviceCaps(hdc, 10);
        double num = (double)GetDeviceCaps(hdc, 117) / (double)deviceCaps;
        return (num == 0.0) ? 1.0 : num;
    }
}
