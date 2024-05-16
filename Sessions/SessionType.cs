using System;
using System.Runtime.Serialization;
using SolarNG.Configs;

namespace SolarNG.Sessions;

public class SessionType
{
    public const uint FLAG_BUILTIN = 1u;
    public const uint FLAG_SPECIAL_TYPE = 2u;
    public const uint FLAG_CREDENTIAL = 0x10u;
    public const uint FLAG_PROXY_PROVIDER = 0x20u;
    public const uint FLAG_PROXY_CONSUMER = 0x40u;
    public const uint FLAG_SSH_PROXY = 0x80u;

    [DataMember]
    public Guid AppId = Guid.Empty;

    [DataMember]
    public string Name;

    [DataMember]
    public string AbbrName;

    [DataMember]
    public string DisplayName;

    [DataMember]
    public string AbbrDisplayName;

    [DataMember]
    public int Port;

    public ProgramConfig Program;

    [DataMember]
    public string ProgramName => Program.Name;

    [DataMember]
    public uint iFlags;

    public SessionType(string name, int port=0, uint flags=0)
    {
        Name = name;

        AbbrName = name;

        DisplayName = name.ToUpper();
        AbbrDisplayName = name.ToUpper();

        Port = port;
        iFlags = flags | ((Port == 0) ? FLAG_SPECIAL_TYPE : 0);
    }

    public SessionType()
    {

    }
}


