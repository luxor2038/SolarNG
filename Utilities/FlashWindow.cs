using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SolarNG.Utilities;
public static class FlashWindow
{
    private struct FLASHWINFO
    {
        public uint cbSize;

        public IntPtr hwnd;

        public uint dwFlags;

        public uint uCount;

        public uint dwTimeout;
    }

    public const uint FLASHW_STOP = 0u;

    public const uint FLASHW_CAPTION = 1u;

    public const uint FLASHW_TRAY = 2u;

    public const uint FLASHW_ALL = 3u;

    public const uint FLASHW_TIMER = 4u;

    public const uint FLASHW_TIMERNOFG = 12u;

    private static bool Win2000OrLater => Environment.OSVersion.Version.Major >= 5;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    public static bool Flash(Form form)
    {
        if (Win2000OrLater)
        {
            FLASHWINFO pwfi = Create_FLASHWINFO(form.Handle, 15u, uint.MaxValue, 0u);
            return FlashWindowEx(ref pwfi);
        }
        return false;
    }

    private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
    {
        FLASHWINFO fLASHWINFO = default;
        fLASHWINFO.cbSize = Convert.ToUInt32(Marshal.SizeOf((object)fLASHWINFO));
        fLASHWINFO.hwnd = handle;
        fLASHWINFO.dwFlags = flags;
        fLASHWINFO.uCount = count;
        fLASHWINFO.dwTimeout = timeout;
        return fLASHWINFO;
    }

    public static bool Stop(IntPtr handle)
    {
        if (Win2000OrLater)
        {
            FLASHWINFO pwfi = Create_FLASHWINFO(handle, 0u, 0u, 0u);
            return FlashWindowEx(ref pwfi);
        }
        return false;
    }
}
