using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SolarNG.Utilities;

public class SafeString : IDisposable
{
    private SecureString _SafeString = null;
    public SafeString(SecureString str) 
    {
        _SafeString = str;
    }

    public SecureString ToSecureString()
    {
        return _SafeString;
    }

    public SafeString(string str) 
    {
        if(!string.IsNullOrEmpty(str))
        {
            _SafeString = new SecureString();
            for (int i = 0; i < str.Length; i++)
            {
                _SafeString.AppendChar(str[i]);
            }
        }
    }

    public unsafe override string ToString()
    {
        if(IsNullOrEmpty(this))
        {
            return "";
        }

        IntPtr intPtr = Marshal.SecureStringToBSTR(_SafeString);
        try
        {
            return new string((char*)(void*)intPtr);
        }
        finally
        {
            Marshal.ZeroFreeBSTR(intPtr);
        }
    }  

    public void Dispose()
    {
        if(!IsNullOrEmpty(this))
        {
            _SafeString.Dispose();
        }
    }  

    public static bool IsNullOrEmpty(SafeString safestr)
    {
        if(safestr == null)
        {
            return true;
        }

        if(safestr._SafeString == null)
        {
            return true;
        }

        if(safestr._SafeString.Length <= 0)
        {
            return true;
        }

        return false;
    }
}
