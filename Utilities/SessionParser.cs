using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using SolarNG.Configs;
using SolarNG.Sessions;

namespace SolarNG.Utilities;

public class SessionParser
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    internal static Session TryParseSession(string arguments)
    {
        Session result = null;
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            try
            {
                result = ParseSession(arguments);
                return result;
            }
            catch (Exception message)
            {
                log.Error(message);
                return result;
            }
        }
        return result;
    }

    internal static Credential ParseCredential(string arguments)
    {
        Credential credential = null;
        if (!string.IsNullOrWhiteSpace(arguments) && arguments.Contains("@"))
        {
            arguments = arguments.Trim();

            foreach( SessionType type in App.BuiltinSessionTypes.Where((SessionType t) => (t.iFlags & SessionType.FLAG_CREDENTIAL)!=0))
            {
                if (arguments.StartsWith(type.Name + ":"))
                {
                    credential = new Credential();
                    arguments = arguments.Substring((type.Name + ":").Length);
                    break;
                }

                if (arguments.StartsWith(type.AbbrName + ":"))
                {
                    credential = new Credential();
                    arguments = arguments.Substring((type.AbbrName + ":").Length);
                    break;
                }
            }

            if(credential == null)
            {
                return null;
            }

            arguments = arguments.Substring(0, arguments.IndexOf("@"));
            if (arguments.Contains(":"))
            {
                credential.Username = arguments.Substring(0, arguments.IndexOf(":"));
                credential.Password = new SafeString(arguments.Substring(arguments.IndexOf(":") + 1));
            }
            else
            {
                credential.Username = arguments;
            }
        }
        return credential;
    }

    private static Session ParseSession(string arguments)
    {
        Session session = null;
        arguments = arguments.Trim();

        foreach (SessionType type in App.BuiltinSessionTypes.Where((SessionType t) => (t.iFlags & SessionType.FLAG_SPECIAL_TYPE)==0 || t.Name == "app"))
        {
            if (arguments.StartsWith(type.Name + ":"))
            {
                session = new Session(type.Name);
                arguments = arguments.Substring((type.Name + ":").Length);
                break;
            }

            if (arguments.StartsWith(type.AbbrName + ":"))
            {
                session = new Session(type.Name);;
                arguments = arguments.Substring((type.AbbrName + ":").Length);
                break;
            }
        }

        session ??= new Session("ssh");

        if (session.Type == "app")
        {
            session.Program.Path = arguments;
            session.Name = Path.GetFileName(arguments);
            return session;
        }

        if (session.Type == "ssh" || session.Type == "telnet")
        {
            session.iFlags = ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;
        }

        if (arguments.Contains("@"))
        {
            arguments = arguments.Substring(arguments.IndexOf("@", StringComparison.Ordinal) + 1);
        }
        if (IsPortSpecified(arguments))
        {
            session.Port = ParsePort(arguments);
            arguments = arguments.Substring(0, arguments.LastIndexOf(":", StringComparison.Ordinal));
        }

        session.Ip = arguments;

        if(string.IsNullOrEmpty(session.Ip))
        {
            return null;
        }

        if(session.Ip[0] == '[')
        {
            if(session.Ip.Length == 1)
            {
                return null;
            }

            if(!char.IsLetterOrDigit(session.Ip, 1))
            {
                return null;
            }
        }

        if(!char.IsLetterOrDigit(session.Ip, 0))
        {
            return null;
        }

        return session;
    }

    public static int ParsePort(string arguments)
    {
        return int.Parse(arguments.Substring(arguments.LastIndexOf(":", StringComparison.Ordinal) + 1));
    }

    public static bool IsPortSpecified(string arguments)
    {
        int num = arguments.IndexOf(":", StringComparison.Ordinal);
        int num2 = arguments.LastIndexOf(":", StringComparison.Ordinal);
        if (num > 0)
        {
            if (num != num2)
            {
                return arguments[num2 - 1] == ']';
            }
            return true;
        }
        return false;
    }
}
