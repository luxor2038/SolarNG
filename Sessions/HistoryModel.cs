using System;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using LitJson;
using System.Security.Cryptography;
using System.Text;
using System.Collections;
using log4net;
using System.Reflection;

namespace SolarNG.Sessions;

public class HistoryModel
{
    [DataMember]
    public int Version = 2;

    [DataMember]
    public ObservableCollection<History> Histories = new ObservableCollection<History>();

    private byte[] DatHash;
    public bool Save(string datafilepath)
    {
        string his_file = Path.Combine(datafilepath, "SolarNG.his");

        try
        {
            HistoryModel histories = this;

            histories.FixHistories();

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

            JsonWriter writer = new JsonWriter();
            JsonMapper.ToJson(histories, writer);

            byte[] file_data = Encoding.UTF8.GetBytes(writer.ToString());
            int plain_lenth = file_data.Length;

            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(file_data);
            }

            if (!StructuralComparisons.StructuralEqualityComparer.Equals(hash, DatHash))
            {
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                {
                    dstream.Write(file_data, 0, file_data.Length);
                }
                byte[] compressed_data = output.ToArray();

                byte[] header = new byte[] { 0x53, 0x6F, 0x6C, 0x61, 0x72, 0x4E, 0x47, 0x00, 0x00, 0x02, 0x01, 0x00, 0, 0, 0, 0, 0, 0, 0, 0 };

                header[12] = (byte)(uint)plain_lenth;
                header[13] = (byte)((uint)plain_lenth>>8);
                header[14] = (byte)((uint)plain_lenth>>16);
                header[15] = (byte)((uint)plain_lenth>>24);

                int compressed_length = compressed_data.Length;

                header[16] = (byte)(uint)compressed_length;
                header[17] = (byte)((uint)compressed_length >> 8);
                header[18] = (byte)((uint)compressed_length >> 16);
                header[19] = (byte)((uint)compressed_length >> 24);

                file_data = new byte[header.Length + compressed_length];

                Buffer.BlockCopy(header, 0, file_data, 0, header.Length);
                Buffer.BlockCopy(compressed_data, 0, file_data, header.Length, compressed_length);

                if(File.Exists(his_file))
                {
                    try
                    {
                        File.Delete(his_file + ".bak");
                        File.Move(his_file, his_file + ".bak");
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to backup SolarNG.his!", ex);
                    }
                }

                File.WriteAllBytes(his_file, file_data);

                DatHash = hash;
            }
        }
        catch (Exception exception)
        {
            log.Error("Failed to save SolarNG.his!", exception);
            return false;
        }

        return true;
    }

    public static HistoryModel Load(string datafilepath)
    {
        HistoryModel histories = new HistoryModel();
        string his_file = Path.Combine(datafilepath, "SolarNG.his");

        if (File.Exists(his_file))
        {
            try
            {
                byte[] file_data = File.ReadAllBytes(his_file);

                if (Encoding.ASCII.GetString(file_data.Take(10).ToArray()) != "SolarNG\0\0\x02")
                {
                    throw new Exception("Wrong Header");
                }

                uint plain_lenth = 0;

                plain_lenth |= file_data[15];
                plain_lenth <<= 8;
                plain_lenth |= file_data[14];
                plain_lenth <<= 8;
                plain_lenth |= file_data[13];
                plain_lenth <<= 8;
                plain_lenth |= file_data[12];

                uint length = 0;

                length |= file_data[19];
                length <<= 8;
                length |= file_data[18];
                length <<= 8;
                length |= file_data[17];
                length <<= 8;
                length |= file_data[16];

                if((length + 20) != file_data.Length)
                {
                    throw new Exception("Wrong Header");
                }

                byte flag = file_data[10];

                byte[] input_data = new byte[file_data.Length - 20];

                Buffer.BlockCopy(file_data, 20, input_data, 0, input_data.Length);

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

                string input_data_str = Encoding.UTF8.GetString(input_data);

                histories = JsonMapper.ToObject<HistoryModel>(input_data_str);

                histories.DatHash = hash;

                histories.FixHistories();
            }
            catch (Exception exception)
            {
                log.Error("Failed to load SolarNG.his!", exception);

                try
                {
                    File.Delete(his_file + ".bad");
                    File.Move(his_file, his_file + ".bad");
                }
                catch (Exception ex)
                {
                    log.Error("Failed to backup SolarNG.his!", ex);
                }
            }
        }

        return histories;
    }

    private void FixHistories()
    {
        foreach(History history in Histories.ToList())
        {
            if(history.OpenCounter == 0)
            {
                Histories.Remove(history);
                continue;
            }

            Session session = App.Sessions.Sessions.FirstOrDefault(s => s.Id == history.SessionId);
            if(session == null)
            {
                Histories.Remove(history);
                continue;
            }

            if(session.SessionHistory == null)
            {
                Session historySession = new Session("history")
                {
                    History = history,
                    HistorySession = session
                };

                session.SessionHistory = historySession;

                App.HistorySessions.Add(historySession);
            }
        }
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
