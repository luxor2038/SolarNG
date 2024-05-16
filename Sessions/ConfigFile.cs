using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using log4net;
using SolarNG.Utilities;

namespace SolarNG.Sessions;

[DataContract]
public class ConfigFile : INotifyPropertyChanged
{
    [DataMember]
    public Guid Id = Guid.NewGuid();

    [DataMember]
    public string Type;

    private string _Name = "";
    [DataMember]
    public string Name
    {
        get
        {
            return _Name;
        }
        set
        {
            _Name = value;
            OnPropertyChanged("Name");
            NameChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler NameChange;
    public EventHandler GetNameChange()
    {
        return NameChange;
    }

    [DataMember]
    public string Comment;

    [DataMember]
    public string Path;

    public string RealPath
    {
        get
        {
            if (string.IsNullOrEmpty(Data))
            {
                return null;
            }
            try
            {
                string id = Id.ToString();
                id = id.Substring(id.Length - 12);
                string TempFile = System.IO.Path.Combine(App.DataFilePath, "Temp", System.IO.Path.GetFileNameWithoutExtension(Path) + "_" + id + System.IO.Path.GetExtension(Path));
                if (!File.Exists(TempFile))
                {
                    string directoryName = System.IO.Path.GetDirectoryName(TempFile);
                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                    using FileStream stream = File.Open(TempFile, FileMode.Create);
                    using StreamWriter streamWriter = new StreamWriter(stream);
                    streamWriter.Write(Data);
                }
                return TempFile;
            }
            catch (Exception message)
            {
                log.Error(message);
            }
            return null;
        }
        set
        {
            Data = null;
            if (string.IsNullOrEmpty(value))
            {
                Path = value;
                return;
            }
            try
            {
                using FileStream stream = File.OpenRead(value);
                using StreamReader streamReader = new StreamReader(stream);
                Data = streamReader.ReadToEnd();
                if(Type == "PuTTY")
                {
                    Data = ConvertRegToIni(Data);

                    if(!value.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                    {
                        value += ".ini";
                    }
                }

                string id = Id.ToString();
                id = id.Substring(id.Length - 12);
                File.Delete(System.IO.Path.Combine(App.DataFilePath, "Temp", System.IO.Path.GetFileNameWithoutExtension(value) + "_" + id + System.IO.Path.GetExtension(value)));
            }
            catch
            {
            }
            Path = System.IO.Path.GetFileName(value);
        }
    }

    public string RealPath2;

    public bool RealPathExists
    {
        get
        {
            string id = Id.ToString();
            id = id.Substring(id.Length - 12);
            return File.Exists(System.IO.Path.Combine(App.DataFilePath, "Temp", System.IO.Path.GetFileNameWithoutExtension(Path) + "_" + id + System.IO.Path.GetExtension(Path)));
        }
    }

    public string StagingPath
    {
        get
        {
            if (string.IsNullOrEmpty(Data))
            {
                return null;
            }
            try
            {
                string TempFile = System.IO.Path.Combine(App.DataFilePath, "Staging", System.IO.Path.GetFileName(Path));
                string directoryName = System.IO.Path.GetDirectoryName(TempFile);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                using (FileStream stream = File.Open(TempFile, FileMode.Create))
                {
                    using StreamWriter streamWriter = new StreamWriter(stream);
                    streamWriter.Write(Data);
                }
                return TempFile;
            }
            catch (Exception message)
            {
                log.Error(message);
            }
            return null;
        }
    }

    [DataMember]
    public string Data;

    public bool Matches(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        text = text.Trim().ToLower();

        if (Type.ToLower().Contains(text))
        {
            return true;
        }

        if (Name.ToLower().Contains(text))
        {
            return true;
        }

        if(Path.ToLower().Contains(text))
        {
            return true;
        }

        return false;
    }

    public ConfigFile(string type)
    {
        Type = type;
    }

    public ConfigFile()
    {
        Type = "";
    }

    private string ConvertRegToIni(string data)
    {
        bool found = false;
        string ini = string.Empty;
        if(!data.StartsWith("Windows Registry Editor Version 5.00", StringComparison.OrdinalIgnoreCase) && !data.StartsWith("REGEDIT4", StringComparison.OrdinalIgnoreCase))
        {
            return data;
        }
        try
        {
            foreach(string line0 in data.Split('\n'))
            {
                string line = line0.Trim();

                if (line.StartsWith("["))
                {
                    string keyPath = line.Trim('[', ']');

                    found = keyPath.StartsWith("HKEY_CURRENT_USER\\Software\\SimonTatham\\PuTTY\\Sessions\\", StringComparison.OrdinalIgnoreCase);
                }
                else if (!string.IsNullOrEmpty(line) && found)
                {
                    int i = line.IndexOf('=');
                    if(i == -1)
                    {
                        continue;
                    }

                    string name = line.Substring(0, i).Trim().Trim('"');
                    string value = line.Substring(i + 1).Trim();

                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = Regex.Unescape(value);

                        ini += name + "=" + value + "\n";
                    }
                    else if (value.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
                    {
                        value = value.Substring(6);
                        ini += name + "=" + Int32.Parse(value, System.Globalization.NumberStyles.HexNumber) + "\n";
                    }
                    else if (value.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
                    {
                        //todo                         
                    }
                }
            }
        }
        catch(Exception ex)
        {
            log.Error(ex);
            return ini;
        }

        return ini;
    }

    private static string EscapeDataString(string sessionName)
    {
        bool candot = false;
        string escapedSessionName = string.Empty;

        foreach(char c in sessionName)
        {
            if (c == ' ' || c == '\\' || c == '*' || c == '?' || c == '%' || c < ' ' || c > '~' || (c == '.' && !candot)) 
            {
                escapedSessionName += "%" + Convert.ToByte(c).ToString("x2");
            }
            else
            {
                escapedSessionName += c;
            }
            candot = true;
        }

        return escapedSessionName;
    }

    public static string GetPuTTYSession(string sessionName)
    {
        sessionName = EscapeDataString(sessionName);
        Dictionary<string, object> puttySession = RegistryHelper.GetPuTTYSession(sessionName);
        string sessionOptions = string.Empty;

        foreach(var item in puttySession)
        {
            if(item.Value is string)
            {
                sessionOptions += item.Key + "=\"" + item.Value + "\"\n";
            }
            else if(item.Value is int)
            {
                sessionOptions += item.Key + "=" + item.Value + "\n";
            }
            else
            {
                log.Warn($"\"{item.Key}\" type is {item.Value.GetType()}!");
            }
        }

        return sessionOptions;
    }

    private static Dictionary<string, object> AddPuTTYConfig(Dictionary<string, object> puttySession)
    {
        foreach(string config in App.Config.PuTTY.Config)
        {
            int i = config.IndexOf('=');
            string name = config.Substring(0, i);
            string value = config.Substring(i + 1);

            if (value.StartsWith("\""))
            {
                if (value.EndsWith("\""))
                {
                    puttySession[name] = value.Substring(1, value.Length - 2);
                } 
                else
                {
                    puttySession[name] = value.Substring(1);
                }
            }
            else
            {
                puttySession[name] = Int32.Parse(value);
            }
        }

        return puttySession;
    }

    public static bool SetPuTTYSession(string sessionName, string sessionOptions, bool force_write)
    {
        Dictionary<string, object> puttySession = new Dictionary<string, object>();
        string name, value;
        try
        {
            foreach(string line in sessionOptions.Split('\n'))
            {
                if(string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                int i = line.IndexOf('=');
                if(i == -1)
                {
                    continue;
                }

                name = line.Substring(0, i).Trim().Trim('"');
                value = line.Substring(i + 1).Trim();

                if (name.Equals("SessionName", StringComparison.OrdinalIgnoreCase))
                {
                    if(!string.IsNullOrEmpty(sessionName))
                    {
                        puttySession = AddPuTTYConfig(puttySession);

                        if(!RegistryHelper.SetPuTTYSession(sessionName, puttySession, force_write))
                        {
                            return false;
                        }

                        puttySession.Clear();
                    }

                    if (value.EndsWith("\""))
                    {
                        sessionName = value.Substring(1, value.Length - 2);
                    } 
                    else
                    {
                        sessionName = value.Substring(1);
                    }

                    continue;
                }
                
                bool found = false;
                foreach(string config in App.Config.PuTTY.Config)
                {
                    if(config.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if(found)
                {
                    continue;
                }

                if (value.StartsWith("\""))
                {
                    if (value.EndsWith("\""))
                    {
                        puttySession[name] = value.Substring(1, value.Length - 2);
                    } 
                    else
                    {
                        puttySession[name] = value.Substring(1);
                    }
                }
                else
                {
                    puttySession[name] = Int32.Parse(value);
                }
            }
        }
        catch(Exception ex)
        {
            log.Error(ex);
            return false;
        }

        if(string.IsNullOrEmpty(sessionName))
        {
            sessionName = "Default%20Settings";
        }

        puttySession = AddPuTTYConfig(puttySession);
        return RegistryHelper.SetPuTTYSession(sessionName, puttySession, force_write);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
