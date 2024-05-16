using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;

namespace SolarNG.Utilities;

public class SendInputHelper
{
    private struct Input
    {
        public uint Type;

        public MixedInput Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MixedInput
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyInput Keyboard;
    }

    public struct MouseInput
    {
        public int X;

        public int Y;

        public uint MouseData;

        public uint Flags;

        public uint Time;

        public IntPtr ExtraInfo;
    }

    public struct KeyInput
    {
        public ushort KeyCode;

        public ushort Scan;

        public uint Flags;

        public uint Time;

        public IntPtr ExtraInfo;
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private const uint INPUT_KEYBOARD = 1u;

    private const uint KEYEVENTF_UNICODE = 4u;
    private const uint KEYEVENTF_KEYUP = 2u;
   
    private const ushort VK_RETURN = 0x0D;
    private const ushort VK_SHIFT = 0x10;

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint numberOfInput, [In][MarshalAs(UnmanagedType.LPArray)] Input[] input, int sizeOfInput);

    public static void Type(string text, bool enter = false)
    {
        try
        {
            Input[] input = GetInput(text, enter);
            SendInput((uint)input.Length, input, Marshal.SizeOf(typeof(Input)));
        }
        catch (Exception exception)
        {
            log.Error("Error typing keyboard input", exception);
        }
    }

    private static Input[] GetInput(string text, bool enter)
    {
        List<Input> list = new List<Input>(2 * text.Length + 2);
        for (int i = 0; i < text.Length; i++)
        {
            list.Add(KeyDown(0, text[i]));
            list.Add(KeyUp(0, text[i]));
        }
        if(enter)
        {
            list.Add(KeyDown(VK_RETURN, 0));
            list.Add(KeyUp(VK_RETURN, 0));
        }
        return list.ToArray();
    }

    public static void TypeShift()
    {
        try
        {
            Input[] input = new Input[] { KeyDown(VK_SHIFT, 0), KeyUp(VK_SHIFT, 0) };
            SendInput((uint)input.Length, input, Marshal.SizeOf(typeof(Input)));
        }
        catch (Exception exception)
        {
            log.Error("Error typing keyboard input", exception);
        }
    }

    private static Input KeyDown(ushort keyCode, ushort scan)
    {
        Input result = default;
        result.Type = INPUT_KEYBOARD;
        result.Data.Keyboard = new KeyInput
        {
            KeyCode = keyCode,
            Scan = scan,
            Flags = KEYEVENTF_UNICODE,
            Time = 0u,
            ExtraInfo = IntPtr.Zero
        };
        return result;
    }

    private static Input KeyUp(ushort keyCode, ushort scan)
    {
        Input result = default;
        result.Type = INPUT_KEYBOARD;
        result.Data.Keyboard = new KeyInput
        {
            KeyCode = keyCode,
            Scan = scan,
            Flags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
            Time = 0u,
            ExtraInfo = IntPtr.Zero
        };
        return result;
    }
}
