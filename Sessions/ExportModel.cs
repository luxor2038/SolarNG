using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using LitJson;
using log4net;
using SolarNG.Configs;
using SolarNG.Utilities;

namespace SolarNG.Sessions;

internal class ExportModelVersion
{
    [DataMember]
    public int Version = 0;
}

[DataContract]
internal class ExportModel
{
    [DataMember]
    public int Version = 2;

    [DataMember]
    public ObservableCollection<SessionType> SessionTypes { get; set; }

    [DataMember]
    public ObservableCollection<Session> Sessions { get; set; }

    [DataMember]
    public ObservableCollection<Credential> Credentials { get; set; }

    [DataMember]
    public ObservableCollection<ConfigFile> ConfigFiles { get; set; }

    public ExportModel()
    {
        SessionTypes = new ObservableCollection<SessionType>();
        SessionTypesDict = new Dictionary<string, SessionType>();
        Sessions = new ObservableCollection<Session>();
        Credentials = new ObservableCollection<Credential>();
        ConfigFiles = new ObservableCollection<ConfigFile>();
    }

    public Dictionary<string, SessionType> SessionTypesDict { get; set; }
    public void AddBuiltinTypes()
    {
        foreach(SessionType type in App.BuiltinSessionTypes)
        {
            SessionTypesDict[type.Name] = type;
        }

        ObservableCollection<SessionType> types = new ObservableCollection<SessionType>(SessionTypes);

        foreach (SessionType type in types)
        {
            if(SessionTypesDict.ContainsKey(type.Name))
            {
                SessionTypes.Remove(type);

                if(type.AppId != Guid.Empty)
                {
                    SessionTypesDict[type.Name].AppId = type.AppId;
                }
            }
            else
            {
                SessionTypesDict[type.Name] = type;
            }
        }

        foreach(SessionType type in App.BuiltinSessionTypes.Reverse())
        {
            SessionTypes.Insert(0, type);
        }
    }

    public void RemoveBuiltinTypes()
    {     
        ObservableCollection<SessionType> types = new ObservableCollection<SessionType>(from s in SessionTypes
                    where (s.iFlags & SessionType.FLAG_BUILTIN) == SessionType.FLAG_BUILTIN
                    select s);

        foreach (SessionType type in types)
        {
            if(type.AppId == Guid.Empty)
            {
                SessionTypes.Remove(type);
            }
            else
            {
                type.DisplayName = null;
                type.AbbrName = null;
                type.AbbrDisplayName = null;
                type.Port = 0;
                type.iFlags = 0;
            }            
        }
    }

    private byte[] DatHash;
    public bool Save(string datafilepath, bool force=false)
    {
        string dat = Path.Combine(datafilepath, "SolarNG.dat");

        try
        {
            ExportModel sessions = this;

            sessions.FixSessions(false);

            JsonMapper.RegisterExporter(delegate (Guid obj, JsonWriter writer)
            {
                if (obj == Guid.Empty)
                {
                    writer.Write("");
                }
                else
                {
                    writer.Write(obj.ToString());
                }
            });
            JsonMapper.RegisterExporter(delegate (SolidColorBrush obj, JsonWriter writer)
            {
                writer.Write(new BrushConverter().ConvertToString(obj));
            });
            JsonMapper.RegisterExporter(delegate (SafeString obj, JsonWriter writer)
            {
                if (SafeString.IsNullOrEmpty(obj))
                {
                    writer.Write("");
                }
                else
                {
                    writer.Write(obj.ToString());
                }
            });

            App.IsSaving = true;
            JsonWriter writer = new JsonWriter();
            JsonMapper.ToJson(sessions, writer);
            App.IsSaving = false;

            sessions.FixSessions(true);

            byte[] sessions_data = Encoding.UTF8.GetBytes(writer.ToString());
            int plain_lenth = sessions_data.Length;

            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(sessions_data);
            }

            if (force || !StructuralComparisons.StructuralEqualityComparer.Equals(hash, DatHash))
            {
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                {
                    dstream.Write(sessions_data, 0, sessions_data.Length);
                }
                byte[] compressed_data = output.ToArray();

                byte[] header = new byte[] { 0x53, 0x6F, 0x6C, 0x61, 0x72, 0x4E, 0x47, 0x00, 0x00, 0x02, 0x01, 0x00, 0, 0, 0, 0, 0, 0, 0, 0 };

                header[12] = (byte)(uint)plain_lenth;
                header[13] = (byte)((uint)plain_lenth>>8);
                header[14] = (byte)((uint)plain_lenth>>16);
                header[15] = (byte)((uint)plain_lenth>>24);

                if(App.passHash != null)
                {
                    compressed_data = Crypto.Encrypt(App.passHash, App.passSalt, compressed_data);
                    header[10] |= 0x02;
                }

                int compressed_length = compressed_data.Length;

                header[16] = (byte)(uint)compressed_length;
                header[17] = (byte)((uint)compressed_length >> 8);
                header[18] = (byte)((uint)compressed_length >> 16);
                header[19] = (byte)((uint)compressed_length >> 24);

                sessions_data = new byte[header.Length + compressed_length];

                Buffer.BlockCopy(header, 0, sessions_data, 0, header.Length);
                Buffer.BlockCopy(compressed_data, 0, sessions_data, header.Length, compressed_length);

                if(File.Exists(dat))
                {
                    try
                    {
                        File.Delete(dat + ".bak");
                        File.Move(dat, dat + ".bak");
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to backup SolarNG.dat!", ex);
                    }
                }

                File.WriteAllBytes(dat, sessions_data);

                DatHash = hash;
            }
        }
        catch (Exception exception)
        {
            App.IsSaving = false;
            log.Error("Failed to save SolarNG.dat!", exception);
            return false;
        }

        return true;
    }

    private void SetNoDuplicatedName(Session newSession)
    {
        int num = 2;
        string name = newSession.Name;
        while (Sessions.FirstOrDefault((Session s) => s.Name == name && s.Type == newSession.Type && s.Id != newSession.Id) != null)
        {
            name = newSession.Name + " (" + num + ")";
            num++;
        }
        newSession.Name = name;
    }

    private void SetNoDuplicatedName(Credential newCredential)
    {
        int num = 2;
        string name = newCredential.Name;
        while (Credentials.FirstOrDefault((Credential s) => s.Name == name && s.Id != newCredential.Id) != null)
        {
            name = newCredential.Name + " (" + num + ")";
            num++;
        }
        newCredential.Name = name;
    }

    private void SetNoDuplicatedName(ConfigFile newConfigFile)
    {
        int num = 2;
        string name = newConfigFile.Name;
        while (ConfigFiles.FirstOrDefault((ConfigFile s) => s.Name == name && s.Type == newConfigFile.Type && s.Id != newConfigFile.Id) != null)
        {
            name = newConfigFile.Name + " (" + num + ")";
            num++;
        }
        newConfigFile.Name = name;
    }

    private static ExportModel ImportPuTTY()
    {
        ExportModel sessions = new ExportModel();

        foreach (string sessionName in RegistryHelper.GetPuttySessions())
        {
            Dictionary<string, object> puttySession = RegistryHelper.GetPuTTYSession(sessionName);

            try
            {
                string HostName = puttySession["HostName"] as string;

                if (string.IsNullOrEmpty(HostName))
                {
                    continue;
                }

                ConfigFile privateKey = null;
                Credential credential = null;

                string Username = puttySession["UserName"] as string;
                string PublicKeyFile = puttySession["PublicKeyFile"] as string;

                if(!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(PublicKeyFile))
                {
                    credential = new Credential()
                    {
                        Name = (string.IsNullOrEmpty(Username) ? (HostName ?? "") : (Username + "@" + HostName)),
                        Username = Username,
                    };

                    if(!string.IsNullOrEmpty(PublicKeyFile))
                    {
                        privateKey = sessions.ConfigFiles.FirstOrDefault(c => c.Type == "PrivateKey" && c.RealPath2 == PublicKeyFile);
                        if(privateKey == null)
                        {
                            privateKey = new ConfigFile("PrivateKey")
                            {
                                RealPath = PublicKeyFile,
                                RealPath2 = PublicKeyFile,
                            };

                            if(!string.IsNullOrEmpty(privateKey.Data))
                            {
                                credential.PrivateKeyId = privateKey.Id;
                            }
                            else
                            {
                                privateKey = null;
                            }
                        }
                        else
                        {
                            credential.PrivateKeyId = privateKey.Id;
                            privateKey = null;
                        }
                    }
                }

                string Protocol = puttySession["Protocol"] as string;

                string type = "ssh";

                if(Protocol == "telnet")
                {
                    type = "telnet";
                } 
                else if(Protocol == "ssh")
                {
                }
                else
                {
                    continue;
                }

                Session session = new Session(type) { Port = (int)puttySession["PortNumber"] };

                if(credential != null) 
                { 
                    session.CredentialId = credential.Id;
                }

                session.Ip = HostName;

                session.Name = Uri.UnescapeDataString(sessionName);

                session.Logging = (int)puttySession["LogType"] > 0;
                session.iFlags = ProgramConfig.FLAG_CLOSE_BY_WM_QUIT;

                if((int)puttySession["SshProt"] < 2)
                {
                    session.Additional = "-1";
                }

                session.Tags = new ObservableCollection<string>{ "PuTTY" };

                if(privateKey != null)
                {
                    privateKey.Name = privateKey.Path;
                    sessions.SetNoDuplicatedName(privateKey);
                    sessions.ConfigFiles.Add(privateKey);
                }

                if(credential != null)
                {
                    sessions.SetNoDuplicatedName(credential);
                    sessions.Credentials.Add(credential);
                }

                sessions.SetNoDuplicatedName(session);
                sessions.Sessions.Add(session);
            }
            catch (Exception message)
            {
                log.Warn(message);
            }
        }


        return sessions;
    }

    private Dictionary<string, Session> Tags;
    private void FixTags(Session session)
    {
        if(session.Tags == null)
        {
            return;
        }

        if(session.Tags.Count == 0)
        {
            session.Tags = null;
            return;
        }

        foreach(string tagName in session.Tags.ToList())
        {
            Session tag;
            if(!Tags.ContainsKey(tagName))
            {
                tag = new Session("tag") { Name = tagName };
                Tags[tagName] = tag;      
                Sessions.Add(tag);
            }
            else
            {
                tag = Tags[tagName];
                FixTags(tag);
            }

            if(!tag.ChildSessions.Contains(session))
            {
                tag.ChildSessions.Add(session);
            }

            if(tag.Tags == null)
            {
                continue;
            }

            foreach (string parentName in tag.Tags)
            {
                if(!session.Tags.Contains(parentName))
                {   
                    session.Tags.Add(parentName);
                }

                if(!Tags[parentName].ChildSessions.Contains(session))
                {
                    Tags[parentName].ChildSessions.Add(session);
                }
            }
        }
    }

    private void FixSessions(bool forLoading)
    {
        if(forLoading)
        {
            AddBuiltinTypes();

            List<Session> notFoundedTags = new List<Session>();

            Tags = new Dictionary<string, Session>();

            foreach (Session tag in Sessions.Where((Session s) => s.Type == "tag"))
            {
                tag.Id = Guid.NewGuid();
                tag.ChildSessions.Clear();
                Tags[tag.Name] = tag;
            }

            foreach(KeyValuePair<string, Session> pair in Tags.ToList())
            {
                FixTags(pair.Value);
            }

            foreach (Session session in Sessions)
            {
                if(session.SessionTypeIsNormal)
                {
                    session.Color ??= (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));
                }

                if(session.Tags == null || session.Type == "tag")
                {
                    continue;
                }

                foreach(string tagName in session.Tags.ToList())
                {
                    Session tag;
                    if(!Tags.ContainsKey(tagName))
                    {
                        tag = new Session("tag") { Name = tagName };
                        notFoundedTags.Add(tag);

                        Tags[tagName] = tag;
                    }
                    else
                    {
                        tag = Tags[tagName];

                        if(tag.Tags != null)
                        {
                            foreach (string parentName in tag.Tags)
                            {
                                if(!session.Tags.Contains(parentName))
                                {   
                                    session.Tags.Add(parentName);
                                }

                                if(!Tags[parentName].ChildSessions.Contains(session))
                                {
                                    Tags[parentName].ChildSessions.Add(session);
                                }
                            }
                        }

                    }
                    if(!tag.ChildSessions.Contains(session))
                    {
                        tag.ChildSessions.Add(session);
                    }
                }
            }

            foreach (Session tag in notFoundedTags)
            {
                Sessions.Add(tag);
            }

            return;
        }
        
        RemoveBuiltinTypes();

        foreach (Session tag in Sessions.Where((Session s) => s.Type == "tag"))
        {
            tag.Id = Guid.Empty;
        }
    }

    public static ExportModel Load(string datafilepath)
    {
        ExportModel sessions = null;
        string dat = Path.Combine(datafilepath, "SolarNG.dat");

        if (File.Exists(dat))
        {
            try
            {
                byte[] sessions_data = File.ReadAllBytes(dat);

                if (Encoding.ASCII.GetString(sessions_data.Take(10).ToArray()) != "SolarNG\0\0\x02")
                {
                    throw new Exception("Wrong Header");
                }

                uint plain_lenth = 0;

                plain_lenth |= sessions_data[15];
                plain_lenth <<= 8;
                plain_lenth |= sessions_data[14];
                plain_lenth <<= 8;
                plain_lenth |= sessions_data[13];
                plain_lenth <<= 8;
                plain_lenth |= sessions_data[12];

                uint length = 0;

                length |= sessions_data[19];
                length <<= 8;
                length |= sessions_data[18];
                length <<= 8;
                length |= sessions_data[17];
                length <<= 8;
                length |= sessions_data[16];

                if((length + 20) != sessions_data.Length)
                {
                    throw new Exception("Wrong Header");
                }

                byte flag = sessions_data[10];

                byte[] input_data = new byte[sessions_data.Length - 20];

                Buffer.BlockCopy(sessions_data, 20, input_data, 0, input_data.Length);

                if ((flag & 0x02) == 0x02)
                {
                    App.passSalt = input_data.Take(24).ToArray();

                    while (App.passHash == null)
                    {
                        PromptDialog promptDialog;
                        promptDialog = new PromptDialog(null, System.Windows.Application.Current.Resources["InputMasterPassword"] as string, System.Windows.Application.Current.Resources["EnterMasterPassword"] as string, "", password: true) { Topmost = true };
                        promptDialog.Focus();
                        bool? flag2 = promptDialog.ShowDialog();
                        if (!flag2.HasValue || !flag2.Value)
                        {
                            return null;    
                        }

                        App.passHash = Crypto.Argon2dHash(promptDialog.MyPassword.Password, App.passSalt);

                        byte[] plain_data = Crypto.Decrypt(App.passHash, input_data);
                        if(plain_data != null)
                        {
                            input_data = plain_data;
                            break;
                        }

                        App.passHash = null;
                    }
                }

                if ((flag & 0x01) == 0x01)
                {
                    byte[] plain_data = new byte[plain_lenth];

                    MemoryStream input = new MemoryStream(input_data);
                    using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
                    {
                        dstream.Read(plain_data, 0, (int)plain_lenth);
                    }

                    input_data = plain_data;
                }

                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(input_data);
                }

                JsonMapper.RegisterImporter((string obj) => string.IsNullOrEmpty(obj) ? Guid.Empty : new Guid(obj));
                JsonMapper.RegisterImporter((ImporterFunc<string, Brush>)((string obj) => (SolidColorBrush)new BrushConverter().ConvertFrom(obj)));
                JsonMapper.RegisterImporter((string obj) => new SafeString(obj));

                string input_data_str = Encoding.UTF8.GetString(input_data);

                ExportModelVersion exportModelVersion = JsonMapper.ToObject<ExportModelVersion>(input_data_str);
                if(exportModelVersion.Version == 2)
                {
                    sessions = JsonMapper.ToObject<ExportModel>(input_data_str);
                }
                else
                {
                    throw new Exception($"wrong version({exportModelVersion.Version})!");
                }

                sessions.DatHash = hash;
            }
            catch (Exception exception)
            {
                log.Error("Failed to load SolarNG.dat!", exception);

                try
                {
                    File.Delete(dat + ".bad");
                    File.Move(dat, dat + ".bad");
                }
                catch (Exception ex)
                {
                    log.Error("Failed to backup SolarNG.dat!", ex);
                }

                sessions = null;
            }
        }

        if (sessions == null)
        {
            sessions = ImportPuTTY();

            if (!sessions.Save(datafilepath))
            {
                sessions = null;
            }
        } 
        else
        {
            if (!App.Config.MasterPassword && App.passHash != null)
            {
                App.passHash = null;
                sessions.Save(datafilepath, true);
            }
        }

        sessions?.FixSessions(true);

        return sessions;
    }

    public static bool IsEncrypted(string datafilepath)
    {
        string dat = Path.Combine(datafilepath, "SolarNG.dat");

        if (File.Exists(dat))
        {
            try
            {
                byte[] sessions_data = File.ReadAllBytes(dat);

                if (Encoding.ASCII.GetString(sessions_data.Take(10).ToArray()) != "SolarNG\0\0\x02")
                {
                    throw new Exception("Wrong Header");
                }

                byte flag = sessions_data[10];
                if ((flag & 0x02) == 0x02)
                {
                    return true;
                }

            }
            catch (Exception exception)
            {
                log.Error("Failed to load SolarNG.dat!", exception);
            }
        }

        return false;
    }

    public bool Export(string filepath)
    {
        try
        {
            ExportModel sessions = this;

            sessions.FixSessions(false);

            JsonMapper.RegisterExporter(delegate (Guid obj, JsonWriter writer)
            {
                if (obj == Guid.Empty)
                {
                    writer.Write("");
                }
                else
                {
                    writer.Write(obj.ToString());
                }
            });
            JsonMapper.RegisterExporter(delegate (SolidColorBrush obj, JsonWriter writer)
            {
                writer.Write(new BrushConverter().ConvertToString(obj));
            });
            JsonMapper.RegisterExporter(delegate (SafeString obj, JsonWriter writer)
            {
                if (SafeString.IsNullOrEmpty(obj))
                {
                    writer.Write("");
                }
                else
                {
                    writer.Write(obj.ToString());
                }
            });
            JsonWriter writer = new JsonWriter() { PrettyPrint = true };
            JsonMapper.ToJson(sessions, writer);

            sessions.FixSessions(true);

            byte[] sessions_data = Encoding.UTF8.GetBytes(writer.ToString());

            string directoryName = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            File.WriteAllBytes(filepath, sessions_data);

            return true;
        }
        catch (Exception exception)
        {
            log.Error("Failed to save \"" + filepath + "\"!", exception);
        }
        return false;

    }

    public bool Import(string filepath)
    {
        ExportModel sessions;
        try
        {
            byte[] sessions_data = File.ReadAllBytes(filepath);

            JsonMapper.RegisterImporter((string obj) => string.IsNullOrEmpty(obj) ? Guid.Empty : new Guid(obj));
            JsonMapper.RegisterImporter((ImporterFunc<string, Brush>)((string obj) => (SolidColorBrush)new BrushConverter().ConvertFrom(obj)));
            JsonMapper.RegisterImporter((string obj) => new SafeString(obj));

            string sessions_data_str = Encoding.UTF8.GetString(sessions_data);

            ExportModelVersion exportModelVersion = JsonMapper.ToObject<ExportModelVersion>(sessions_data_str);
            if(exportModelVersion.Version == 2)
            {
                sessions = JsonMapper.ToObject<ExportModel>(sessions_data_str);
            }
            else
            {
                throw new Exception($"wrong version({exportModelVersion.Version})!");
            }

            foreach (Session importedSession in sessions.Sessions)
            {
                importedSession.Color ??= (SolidColorBrush)new BrushConverter().ConvertFrom(App.GetColor(true));

                if(importedSession.Type == "tag")
                {
                    if (Sessions.FirstOrDefault((Session s) => s.Name == importedSession.Name && s.Type == importedSession.Type) == null)
                    {
                        Sessions.Add(importedSession);
                        continue;
                    }

                    for (int num = Sessions.Count - 1; num >= 0; num--)
                    {
                        if (Sessions[num].Name == importedSession.Name && Sessions[num].Type == importedSession.Type)
                        {
                            Sessions[num] = importedSession;
                        }
                    }
                }
                else
                {
                    SetNoDuplicatedName(importedSession);
                    if (Sessions.FirstOrDefault((Session s) => s.Id == importedSession.Id) == null)
                    {
                        Sessions.Add(importedSession);
                        continue;
                    }

                    for (int num = Sessions.Count - 1; num >= 0; num--)
                    {
                        if ((Sessions[num].Id == importedSession.Id))
                        {
                            Sessions[num] = importedSession;
                            break;
                        }
                    }
                }
            }

            foreach (Credential importedCredential in sessions.Credentials)
            {
                SetNoDuplicatedName(importedCredential);
                if (Credentials.FirstOrDefault((Credential c) => c.Id == importedCredential.Id) == null)
                {
                    Credentials.Add(importedCredential);
                    continue;
                }
                for (int num = Credentials.Count - 1; num >= 0; num--)
                {
                    if (Credentials[num].Id == importedCredential.Id)
                    {
                        Credentials[num] = importedCredential;
                        break;
                    }
                }
            }

            foreach (ConfigFile importedConfigFile in sessions.ConfigFiles)
            {
                SetNoDuplicatedName(importedConfigFile);
                if (ConfigFiles.FirstOrDefault((ConfigFile s) => s.Id == importedConfigFile.Id) == null)
                {
                    ConfigFiles.Add(importedConfigFile);
                    continue;
                }
                for (int num = ConfigFiles.Count - 1; num >= 0; num--)
                {
                    if (ConfigFiles[num].Id == importedConfigFile.Id)
                    {
                        ConfigFiles[num] = importedConfigFile;
                        break;
                    }
                }
            }

            FixSessions(true);

            return true;
        }
        catch (Exception exception)
        {
            log.Error("Failed to import \"" + filepath+ "\"!", exception);
        }

        return false;
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
