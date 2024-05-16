using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.Win32;

namespace SolarNG.Utilities;

internal class RegistryHelper
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public static Dictionary<string, object> GetPuTTYSession(string sessionName)
    {
        Dictionary<string, object> keys = new Dictionary<string, object>();
        try
        {
            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\SimonTatham\\PuTTY\\Sessions\\" + sessionName, false))
            {
                foreach (string ValueName in registryKey.GetValueNames())
                {
                    try
                    {
                        keys[ValueName] = registryKey.GetValue(ValueName);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }

        return keys;
    }

    public static bool SetPuTTYSession(string sessionName, Dictionary<string, object> sessionOptions, bool force_write)
    {
        try
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\SimonTatham\\PuTTY\\Sessions\\" + sessionName, true);
            if(registryKey != null && !force_write)
            {
                registryKey.Close();
                return true;
            }

            registryKey ??= Registry.CurrentUser.CreateSubKey("Software\\SimonTatham\\PuTTY\\Sessions\\" + sessionName);
            if(registryKey == null)
            {
                return false;
            }

            foreach(var item in sessionOptions)
            {
                registryKey.SetValue(item.Key, item.Value);
            }

            registryKey.Close();
        }
        catch (Exception ex)
        {
            log.Error(ex);
            return false;
        }

        return true;
    }

    public static IEnumerable<string> GetPuttySessions()
    {
        using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\SimonTatham\\PuTTY\\Sessions", writable: false);
        return registryKey?.GetSubKeyNames() ?? Enumerable.Empty<string>();
    }

    public static bool IsConsoleV2()
    {
        try
        {
            using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Console", writable: false);
            object version = registryKey.GetValue("ForceV2");
            return (int)version == 1;
        }
        catch (Exception)
        {
        }

        return false;
    }

    public static bool AppsUseLightTheme()
    {
        try
        {
            using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\\", writable: false);
            object AppsUseLightThemeValue = registryKey.GetValue("AppsUseLightTheme");
            return (int)AppsUseLightThemeValue == 1;
        }
        catch (Exception)
        {
        }

        return false;
    }
}
