using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using log4net;

namespace SolarNG.Utilities;

public class ProcessHelper
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private static readonly string conhost = Path.Combine(Environment.SystemDirectory, "conhost.exe");

    internal static void KillProcessChildren(int parent, int current)
    {
        if (current == 0)
        {
            return;
        }
        foreach (ManagementBaseObject item in new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + current).Get())
        {
            if (string.Compare(Convert.ToString(item["ExecutablePath"]), conhost, ignoreCase: true) != 0)
            {
                KillProcessChildren(parent, Convert.ToInt32(item["ProcessID"]));
            }
        }
        if (parent != current)
        {
            KillProcess(current);
        }
    }

    internal static void KillProcess(int processId)
    {
        try
        {
            Process.GetProcessById(processId).Kill();
        }
        catch (Exception)
        {
            log.Warn($"Unable to kill process with ID: {processId}");
        }
    }

    internal static void KillProcessByEnv(List<string> InstanceIDs)
    {
        int pid = Process.GetCurrentProcess().Id;
        Process[] processes = Process.GetProcesses();
        foreach (Process process in processes)
        {
            if (process.Id == pid || process.ProcessName == "conhost")
            {
                continue;
            }
            try
            {
                string id = SolarNGX.GetProcessEnvironmentVariable(process.Id, "SolarNG-id");
                if (id != null)
                {
                    if (InstanceIDs.Contains(id))
                    {
                        process.Kill();
                    }
                }
                else
                {
                    string path = SolarNGX.GetProcessEnvironmentVariable(process.Id, "Path");
                    if(path == null)
                    {
                        continue;
                    }
                    int start = path.IndexOf("SolarNG-Id-");
                    if (start != -1)
                    {
                        id = path.Substring(start + "SolarNG-Id-".Length, 36);
                        if (InstanceIDs.Contains(id))
                        {
                            process.Kill();
                        }
                    }
                }
            }
            catch (Exception)
            {
                continue;
            }
        }
    }
}
