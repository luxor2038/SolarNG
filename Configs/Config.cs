using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using LitJson;
using log4net;

namespace SolarNG.Configs;

[DataContract]
internal class Config
{
    [DataMember]
    public int Version = 2;

    [DataMember]
    public bool MasterPassword = false;

    [DataMember]
    public Dictionary<string, string> ShortcutsLocations = new Dictionary<string, string>()
    { 
        {"CommonStartMenu", "%ALLUSERSPROFILE%\\Microsoft\\Windows\\Start Menu\\Programs" },
        {"StartMenu", "%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs" }, 
        {"CommonDesktop", "%PUBLIC%\\Desktop" },
        {"Desktop", "%USERPROFILE%\\Desktop" },
        {"QuickLaunch", "%APPDATA%\\Microsoft\\Internet Explorer\\Quick Launch" }
    };

    [DataMember]
    public GUIConfig GUI = new GUIConfig();

    [DataMember]
    public PuTTYConfig PuTTY = new PuTTYConfig();

    [DataMember]
    public MSTSCConfig MSTSC = new MSTSCConfig();

    [DataMember]
    public WinSCPConfig WinSCP = new WinSCPConfig();

    [DataMember]
    public VNCViewerConfig VNCViewer = new VNCViewerConfig();

    [DataMember]
    public PlinkXConfig PlinkX = new PlinkXConfig();

    [DataMember]
    public ProgramConfig ExeLoader = new ProgramConfig("ExeLoader", "%curdir%\\%arch%\\ExeLoader.exe");

    [DataMember]
    public ProgramConfig Notepad = new ProgramConfig("Notepad", "%windir%\\%sysnative%\\notepad.exe");

    public bool Save(string datafilepath)
    {
        string cfg = Path.Combine(datafilepath, "SolarNG.cfg");

        try
        {
            JsonWriter writer = new JsonWriter
            {
                PrettyPrint = true
            };
            JsonMapper.ToJson(this, writer);

            using (FileStream fileStream = new FileStream(cfg, FileMode.Create))
            {
                using (StreamWriter streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(writer.ToString());
                }
            }
        }
        catch (Exception exception)
        {
            log.Error("Failed to save SolarNG.cfg!", exception);
            return false;
        }

        return true;
    }

    public static Config Load(string datafilepath)
    {
        Config config = null;
        string cfg = Path.Combine(datafilepath, "SolarNG.cfg");

        if (File.Exists(cfg))
        {
            try
            {
                using (FileStream fileStream = new FileStream(cfg, FileMode.Open))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        string text = streamReader.ReadToEnd();
                        config = JsonMapper.ToObject<Config>(text);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error("Failed to load SolarNG.cfg!", exception);

                try
                {
                    File.Delete(cfg + ".bad");
                    File.Move(cfg, cfg + ".bad");
                }
                catch (Exception ex)
                {
                    log.Error("Failed to backup SolarNG.cfg!", ex);
                }

                config = null;
            }
        }

        if (config == null)
        {
            config = new Config();

            if(!config.Save(datafilepath))
            {
                config = null;
            }
        }
        return config;
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

}
