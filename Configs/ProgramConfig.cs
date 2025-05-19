using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace SolarNG.Configs;

[DataContract]
public class ProgramConfig
{
    public string Name;

    [DataMember]
    public string DisplayName = null;

    private string _Arch;
    [DataMember]
    public string Arch
    {
        get
        {
            return _Arch;
        }
        set
        {
            _Arch = value;
            _FullPath = null;
            _NativeFullPath = null;
            _FullWorkingDir = null;
        }
    }

    private string _Path;
    [DataMember]
    public string Path
    {
        get
        {
            return _Path;
        }
        set
        {
            _Path = value;
            _FullPath = null;
            _NativeFullPath = null;
        }
    }

    private string _FullPath;
    public string FullPath
    {
        get
        {
            if(_FullPath == null)
            {
                _FullPath = ExpandEnvironmentVariables(Path, Arch);
            }

            return _FullPath;
        }
    }

    private string _NativeFullPath;
    public string NativeFullPath
    {
        get
        {
            if(_NativeFullPath == null)
            {
                _NativeFullPath = ExpandEnvironmentVariables(Path, Arch, true);
            }

            return _NativeFullPath;
        }
    }

    [DataMember]
    public List<string> ClassName = null;

    [DataMember]
    public List<string> AuthClassName = null;

    [DataMember]
    public string Args = null;

    [DataMember]
    public string CommandLine = null;

    private string _WorkingDir;
    [DataMember]
    public string WorkingDir
    {
        get
        {
            return _WorkingDir;
        }
        set
        {
            _WorkingDir = value;
            _FullWorkingDir = null;
        }
    }

    private string _FullWorkingDir;
    public string FullWorkingDir
    {
        get
        {
            if(_FullWorkingDir == null)
            {
                _FullWorkingDir = ExpandEnvironmentVariables(WorkingDir, Arch);
            }

            return string.IsNullOrWhiteSpace(_FullWorkingDir) ? null : _FullWorkingDir;
        }
    }

    [DataMember]
    public string ProcessName = null;

    internal const uint FLAG_NOTINTAB = 1u;
    internal const uint FLAG_PINNED = 2u;
    internal const uint FLAG_SYNCTITLE = 4u;

    internal const uint FLAG_CLOSE_MASK = 0x18u;
    internal const uint FLAG_CLOSE_BY_WM_CLOSE = 0x00u;
    internal const uint FLAG_CLOSE_BY_KILL = 0x08u;
    internal const uint FLAG_CLOSE_BY_WM_QUIT = 0x10u;
    internal const uint FLAG_CLOSE_BY_KICK = 0x18u;

    internal const uint FLAG_SSHV2SHARE = 0x20u;
    internal const uint FLAG_USED_FOR_SESSION = 0x40u;
    internal const uint FLAG_INTERNAL_APP = 0x80u;
    internal const uint FLAG_FULLSCREEN_CHECK = 0x0100u;
    internal const uint FLAG_NOTINOVERVIEW = 0x0200u;
    internal const uint FLAG_PASSWORD_ONLY = 0x0400u;
    internal const uint FLAG_NOTCLOSEIME = 0x0800u;

    internal const uint FLAG_ENABLEHOTKEY = 0x1000u;

    [DataMember]
    public uint iFlags = 0;

    [DataMember]
    public List<string> Flags = null;

    [DataMember]
    public uint WindowStyleMask = 0;

    internal const uint AUTOINPUT_SENDMESSAGE = 0u;
    internal const uint AUTOINPUT_SENDINPUT = 1u;

    [DataMember]
    public uint AutoInput = AUTOINPUT_SENDMESSAGE;

    [DataMember]
    public bool UsePipe = false;

    [DataMember]
    public bool UseHook = false;

    [DataMember]
    public string HookDll = null;

    [DataMember]
    public List<string> Config = null;

    public string GetFullPath(string arch)
    {
        return ExpandEnvironmentVariables(Path, arch);
    }

    private static readonly string sysnative = ((Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess) ? "Sysnative" : "System32");
    private static readonly string programfilesnative = ((Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess) ? "%ProgramW6432%" : "%ProgramFiles%");
    private static readonly string programfiles32 = ((Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess) ? "%ProgramFiles%" : "%ProgramFiles(x86)%");
    private static readonly string archnative = Environment.Is64BitOperatingSystem ? "x64" : "x86";
    private static readonly string CurDir = new Uri(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).LocalPath;

    public static string ExpandEnvironmentVariables(string path, string arch=null, bool native=false)
    {
        if(string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        path = path.Replace("%curdir%", CurDir);
        path = path.Replace("%arch%", string.IsNullOrEmpty(arch)?archnative:arch);
        path = path.Replace("%sysnative%", native?"System32":sysnative);
        path = path.Replace("%programfilesnative%", programfilesnative);
        path = path.Replace("%programfiles32%", programfiles32);
        path = path.Replace("%datadir%", App.DataFilePath);

        return Environment.ExpandEnvironmentVariables(path);
    }

    public ProgramConfig(string name, string path=null)
    {
        Name = name;
        Path = path;
    }

    public ProgramConfig() { }

    public ProgramConfig Clone()
    {
        ProgramConfig programConfig = (ProgramConfig)MemberwiseClone();

        programConfig.Flags = (Flags == null)? null: new List<string>(Flags);

        return programConfig;
    }
}
