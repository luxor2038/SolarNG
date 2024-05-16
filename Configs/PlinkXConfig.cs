using System.Runtime.Serialization;

namespace SolarNG.Configs;

[DataContract]
internal class PlinkXConfig : ProgramConfig
{
    [DataMember]
    public uint IdleTimeout = 600;

    [DataMember]
    public bool RandomLoopbackAddress = true;

    [DataMember]
    public bool CreateNoWindow = true;

    public PlinkXConfig() : base("PlinkX", "%curdir%\\%arch%\\plinkx.exe")
    {
        Arch = "x86";
        CommandLine = "%% -a -noagent -proxy-localhost -no-antispoof";
    }

}
