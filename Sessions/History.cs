using System;
using System.Runtime.Serialization;

namespace SolarNG.Sessions;

public class History
{
    [DataMember]
    public Guid SessionId = Guid.Empty;

    [DataMember]
    public int OpenCounter;

    [DataMember]
    public DateTime OpenTime;
}
