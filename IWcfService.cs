using System.ServiceModel;

namespace SolarNG;

[ServiceContract]
public interface IWcfService
{
    [OperationContract]
    void PassArguments(string[] args);
}
