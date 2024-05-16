using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SolarNG.Configs;

[DataContract]
internal class WinSCPConfig : ProgramConfig
{
    [DataMember]
    public bool PasswordByPipe = true; //6.0+

    public WinSCPConfig() : base("WinSCP", "%curdir%\\%arch%\\WinSCP.exe")
    {
        Arch = "x86";
        ClassName = new List<string> { "TScpCommanderForm", "TScpExplorerForm" };
        AuthClassName = new List<string> { "TAuthenticateForm" };
        CommandLine = "%% /rawconfig Interface\\Updates\\ShowOnStartup=0 Interface\\ConfirmClosingSession=0 Interface\\Commander\\SessionsTabs=0 Interface\\Explorer\\SessionsTabs=0 Interface\\ExternalSessionInExistingInstance=0";

        WorkingDir = "%datadir%\\Temp\\";

        iFlags =  ProgramConfig.FLAG_INTERNAL_APP | ProgramConfig.FLAG_USED_FOR_SESSION;
        Flags = new List<string> { "winscp" };

        HookDll = Name + "X";
        UseHook = true;
        UsePipe = true;
    }
}
