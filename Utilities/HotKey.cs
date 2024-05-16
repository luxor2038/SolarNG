using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace SolarNG.Utilities;

public class HotKey : IDisposable
{
    private static Dictionary<int, HotKey> _dictHotKeyToCalBackProc;

    public const int WmHotKey = 786;

    private bool _disposed;

    public Key Key { get; private set; }

    public KeyModifier KeyModifiers { get; private set; }

    public Action<HotKey> Action { get; private set; }

    public int Id { get; set; }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vlc);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public HotKey(Key k, KeyModifier keyModifiers, Action<HotKey> action, bool register = true)
    {
        Key = k;
        KeyModifiers = keyModifiers;
        Action = action;
        if (register)
        {
            Register();
        }
    }

    public bool Register()
    {
        int num = KeyInterop.VirtualKeyFromKey(Key);
        Id = num + (int)KeyModifiers * 65536;
        bool result = RegisterHotKey(IntPtr.Zero, Id, (uint)KeyModifiers, (uint)num);
        if (_dictHotKeyToCalBackProc == null)
        {
            _dictHotKeyToCalBackProc = new Dictionary<int, HotKey>();
            ComponentDispatcher.ThreadFilterMessage += ComponentDispatcherThreadFilterMessage;
        }
        _dictHotKeyToCalBackProc.Add(Id, this);
        return result;
    }

    public void Unregister()
    {
        if (_dictHotKeyToCalBackProc.TryGetValue(Id, out var _))
        {
            UnregisterHotKey(IntPtr.Zero, Id);
            _dictHotKeyToCalBackProc.Remove(Id);
        }
    }

    private static void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
    {
        if (!handled && msg.message == 786 && _dictHotKeyToCalBackProc.TryGetValue((int)msg.wParam, out var value))
        {
            value.Action?.Invoke(value);
            handled = true;
        }
    }

    public override string ToString()
    {
        return $"{KeyModifiers} {Key}";
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Unregister();
            }
            _disposed = true;
        }
    }
}
