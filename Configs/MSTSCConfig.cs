using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SolarNG.Configs;

[DataContract]
internal class MSTSCConfig : ProgramConfig
{
    [DataMember]
    public int WidthDelta = 0;

    [DataMember]
    public int HeightDelta = 0;

    [DataMember]
    public List<string> FullScreen = null;

    public MSTSCConfig() : base("MSTSC", "%windir%\\%sysnative%\\mstsc.exe")
    {
        ClassName = new List<string> { "TscShellContainerClass" };
        Args = "/v:%host:%port /w:%width /h:%height";
        FullScreen = new List<string> { "/f", "/fullscreen", "/span", "/multimon" };
        CommandLine = "%%";

        iFlags = ProgramConfig.FLAG_INTERNAL_APP | ProgramConfig.FLAG_USED_FOR_SESSION | ProgramConfig.FLAG_FULLSCREEN_CHECK;
        Flags = new List<string> { "mstsc" };

        HookDll = Name + "X";
        UseHook = true;
        UsePipe = true;
    }

}

