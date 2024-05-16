using System;
using System.Text;

namespace SolarNG.Utilities;

internal class PipeServer
{
    static public bool Start()
    {
        return SolarNGX.StartPipeFileServer(64) != 64;
    }

    static public void Stop()
    {
        SolarNGX.StopPipeFileServer();
    }

    private int pipeId = -1;
    public PipeServer(string name, byte[] data, int maxInstances)
    {
        pipeId = SolarNGX.CreatePipeFile(name, data, data.Length, maxInstances);
    }

    public PipeServer(string name, string data, int maxInstances) : this(name, Encoding.ASCII.GetBytes(data), maxInstances)
    {

    }

    public PipeServer(string name, string data) : this(name, data, 1)
    {

    }

    public int Test()
    {
        try
        {
            return SolarNGX.TestPipeFile(pipeId);
        }
        catch(Exception)
        {

        }
        return 0;
    }

    public void Close()
    {
        SolarNGX.ClosePipeFile(pipeId);
    }

}
