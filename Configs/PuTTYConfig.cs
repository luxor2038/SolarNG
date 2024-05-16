using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SolarNG.Configs;

[DataContract]
internal class PuTTYConfig : ProgramConfig
{
    [DataMember]
    public bool StrictSSHv2Share = false;

    [DataMember]
    public bool SSHPasswordByPipe = true; //0.77+

    [DataMember]
    public bool SSHPasswordByHook = false; //0.77+

    public PuTTYConfig() : base("PuTTY", "%curdir%\\%arch%\\PUTTY.EXE")
    {
        ClassName = AuthClassName = new List<string> { "PuTTY" };
        CommandLine = "%% -a -noagent";

        iFlags = ProgramConfig.FLAG_INTERNAL_APP | ProgramConfig.FLAG_USED_FOR_SESSION | ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;

        HookDll = Name + "X";
        Config = new List<string> { "ScrollBarFullScreen=1", "ProxyLocalhost=1" };
        UseHook = true;
        UsePipe = true;
    }

}
