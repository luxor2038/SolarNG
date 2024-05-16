using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SolarNG.Configs;

[DataContract]
internal class VNCViewerConfig : ProgramConfig
{
    public VNCViewerConfig() : base("VNCViewer", "%curdir%\\%arch%\\tvnviewer.exe")
    {
        ClassName = new List<string> { "TvnWindowClass" };
        AuthClassName = new List<string> { "#32770" };
        Args = "%host::%port";
        CommandLine = "%%";

        iFlags = ProgramConfig.FLAG_INTERNAL_APP | ProgramConfig.FLAG_USED_FOR_SESSION | ProgramConfig.FLAG_FULLSCREEN_CHECK;
    }
}
