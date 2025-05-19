using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using SolarNG.Configs;
using static SolarNG.Utilities.ShortcutParser.ItemIDData;

namespace SolarNG.Utilities;

public class ShortcutInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string AppName { get; set; }
    public string AppPath { get; set; }

    public string Arguments { get; set; }
    public string WorkingDirectory { get; set; }
    public string DirectoryPath { get; set; } 
    public string LocationType { get; set; }
    public bool IsDirectory { get; set; }
}

public class ShortcutParser
{
    public class ItemID
    {
        public ushort Size;
        public byte[] Data;
        public byte Type;
        public object itemIDData;
    }

    public class IDList
    {
        public ushort Size;
        public List<ItemID> Items = new List<ItemID>();
    }

    public class ItemIDData
    {
        public class Root
        {
            public byte Type;
            public byte SortIndex;
            public Guid Identifier; 
        }

        [Flags]
        public enum UriFlags : byte
        {
            IsUnicode = 0x80
        }
        public class Uri
        {
            public byte Type;
            public UriFlags Flags;       
            public string uri; 
        }
    }

    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != 0)
        {
            bytes.Add(b);
        }
        return Encoding.Default.GetString(bytes.ToArray());
    }

    private static string ReadNullTerminatedUnicodeString(BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        ushort u;
        while ((u = reader.ReadUInt16()) != 0)
        {
            bytes.Add((byte)(u & 0xFF));
            bytes.Add((byte)(u >> 8));
        }
        return Encoding.Unicode.GetString(bytes.ToArray());
    }

    private static ItemIDData.Root ParseRoot(BinaryReader reader, byte type)
    {
        byte sortIndex = reader.ReadByte();

        var root = new ItemIDData.Root
        {
            Type = type,
            SortIndex = sortIndex
        };

        byte[] guidBytes = reader.ReadBytes(16);
        root.Identifier = new Guid(guidBytes);
        return root;
    }
    private static ItemIDData.Uri ParseUri(BinaryReader reader, byte type)
    {
        UriFlags flags = (UriFlags)reader.ReadByte();

        var uri = new ItemIDData.Uri
        {
            Type = type,
            Flags = flags
        };

        uint unk = reader.ReadUInt32();
        if(unk != 0)
        {
            return uri;
        }

        if(flags.HasFlag(UriFlags.IsUnicode))
        {
            uri.uri = ReadNullTerminatedUnicodeString(reader);
        }
        else
        {
            uri.uri = ReadNullTerminatedString(reader);
        }

        return uri;
    }

    private static object ParseItemIDData(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            byte type = reader.ReadByte();

            switch (type & 0x70)
            {
                case 0x10:
                    return ParseRoot(reader, type);
                case 0x60:
                    return ParseUri(reader, type);
                default:
                    return null;
            }
        }
    }

    public static IDList ParseLinkTargetIDList(BinaryReader reader)
    {
        IDList idList = new IDList();
        
        idList.Size = reader.ReadUInt16();
        
        int remainingSize = idList.Size;
        
        while (remainingSize > 0)
        {
            ushort itemSize = reader.ReadUInt16();
            if (itemSize == 0)
                break;

            ItemID item = new ItemID
            {
                Size = itemSize,
                Data = reader.ReadBytes(itemSize - 2)
            };

            item.Type = item.Data[0];
            
            item.itemIDData = ParseItemIDData(item.Data);

            idList.Items.Add(item);
            
            remainingSize -= itemSize;
        }

        return idList;
    }

    [Flags]
    public enum LinkInfoFlag
    {
        VolumeIdAndLocalBasePath = 0x0001,
        CommonNetworkRelativeLinkAndPathSuffix = 0x0002
    }

    private static string ParseLinkInfo(BinaryReader reader)
    {
        long startPosition = reader.BaseStream.Position;

        uint linkInfoSize = reader.ReadUInt32();
        uint linkInfoHeaderSize = reader.ReadUInt32();
        LinkInfoFlag linkInfoFlags = (LinkInfoFlag)reader.ReadUInt32();
        uint volumeIdOffset = reader.ReadUInt32();
        uint localBasePathOffset = reader.ReadUInt32();
        uint commonNetworkRelativeLinkOffset = reader.ReadUInt32();
        uint commonPathSuffixOffset = reader.ReadUInt32();
        uint localBasePathOffsetUnicode = 0;
        uint commonPathSuffixOffsetUnicode = 0;
        if (linkInfoHeaderSize >= 0x24)
        {
            localBasePathOffsetUnicode = reader.ReadUInt32();
            commonPathSuffixOffsetUnicode = reader.ReadUInt32();
        }

        string localBasePath = null;

        if (linkInfoFlags.HasFlag(LinkInfoFlag.VolumeIdAndLocalBasePath))
        {
            reader.BaseStream.Position = startPosition + localBasePathOffset;
            localBasePath = ReadNullTerminatedString(reader);

            if (localBasePathOffsetUnicode >= 0x24)
            {
                reader.BaseStream.Position = startPosition + localBasePathOffsetUnicode;
                string localBasePathUnicode = ReadNullTerminatedUnicodeString(reader);
                if(!string.IsNullOrEmpty(localBasePathUnicode))
                {
                    localBasePath = localBasePathUnicode;
                }
            }
        }

        reader.BaseStream.Position = startPosition + linkInfoSize;
        return localBasePath;
    }

    private enum ExtraDataBlockSignature : uint
    {
        EnvironmentVariableDataBlock = 0xA0000001,
    }

    private static string ParseEnvironmentVariableDataBlock(BinaryReader reader)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            uint blockSize = reader.ReadUInt32();
            if (blockSize < 4)
                break;

            ExtraDataBlockSignature blockSignature = (ExtraDataBlockSignature)reader.ReadUInt32();

            if(blockSignature != ExtraDataBlockSignature.EnvironmentVariableDataBlock)
            {
                reader.BaseStream.Position += blockSize - 8;
                continue;
            }

            byte[] blockData = reader.ReadBytes((int)blockSize - 8);

            using (MemoryStream ms = new MemoryStream(blockData))
            using (BinaryReader reader2 = new BinaryReader(ms))
            {
                reader2.BaseStream.Position += 260;
                return Encoding.Unicode.GetString(reader2.ReadBytes(blockData.Length-260)).TrimEnd('\0');
            }
        }

        return null;
    }

    [Flags]
    private enum LinkFlags : uint
    {
        HasLinkTargetIDList = 0x00000001,
        HasLinkInfo = 0x00000002,
        HasName = 0x00000004,
        HasRelativePath = 0x00000008,
        HasWorkingDir = 0x00000010,
        HasArguments = 0x00000020,
        HasIconLocation = 0x00000040,
        IsUnicode = 0x00000080,
        ForceNoLinkInfo = 0x00000100,
        HasExpString = 0x00000200,
        PreferEnvironmentPath = 0x02000000,
        KeepLocalIDListForUNCTarget = 0x04000000
    }

    private static byte[] ShellLinkHeader = new byte[]{ 0x4C, 0x00, 0x00, 0x00, 0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x46 };

    public static void ParseLnkFile(string lnkPath, ref ShortcutInfo shortcutInfo)
    {
        try
        {
            using (FileStream fs = new FileStream(lnkPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                byte[] header = reader.ReadBytes(20);

                if (!header.SequenceEqual(ShellLinkHeader))
                {
                    throw new Exception("Invalid LNK format");
                }
               
                LinkFlags flags = (LinkFlags)reader.ReadUInt32();
                reader.BaseStream.Position += 52;

                IDList idList;
                if (flags.HasFlag(LinkFlags.HasLinkTargetIDList))
                {
                    idList = ParseLinkTargetIDList(reader);

                    foreach(var id in idList.Items) 
                    {
                        switch (id.Type & 0x70)
                        {
                            case 0x10:
                                shortcutInfo.AppPath = "shell:::{" + (id.itemIDData as ItemIDData.Root).Identifier + "}";
                                break;
                            case 0x60:
                                if(!string.IsNullOrEmpty((id.itemIDData as ItemIDData.Uri).uri))
                                {
                                    shortcutInfo.AppPath = (id.itemIDData as ItemIDData.Uri).uri;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (flags.HasFlag(LinkFlags.HasLinkInfo))
                {
                    string localBasePath = ParseLinkInfo(reader);
                    if(!string.IsNullOrEmpty(localBasePath))
                    {
                        shortcutInfo.AppPath = localBasePath;
                    }
                }

                if (flags.HasFlag(LinkFlags.HasName))
                {
                    shortcutInfo.AppName = ReadStringData(reader, flags.HasFlag(LinkFlags.IsUnicode));
                }

                if (flags.HasFlag(LinkFlags.HasRelativePath))
                {
                    string relativePath = ReadStringData(reader, flags.HasFlag(LinkFlags.IsUnicode));
                    if(string.IsNullOrEmpty(shortcutInfo.AppPath) || shortcutInfo.AppPath.StartsWith("shell:::"))
                    {
                        shortcutInfo.AppPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(shortcutInfo.Path), relativePath));
                    }
                }

                if (flags.HasFlag(LinkFlags.HasWorkingDir))
                {
                    shortcutInfo.WorkingDirectory = ReadStringData(reader, flags.HasFlag(LinkFlags.IsUnicode));
                }

                if (flags.HasFlag(LinkFlags.HasArguments))
                {
                    shortcutInfo.Arguments = ReadStringData(reader, flags.HasFlag(LinkFlags.IsUnicode));
                }

                if (flags.HasFlag(LinkFlags.HasIconLocation))
                {
                    ReadStringData(reader, flags.HasFlag(LinkFlags.IsUnicode));
                }

                if(!flags.HasFlag(LinkFlags.HasExpString))
                {
                    return;
                }
                       
                if(string.IsNullOrEmpty(shortcutInfo.AppPath) || shortcutInfo.AppPath.StartsWith("shell:::") || flags.HasFlag(LinkFlags.PreferEnvironmentPath))
                {
                    string envPath = ParseEnvironmentVariableDataBlock(reader);
                    if(!string.IsNullOrEmpty(envPath))
                    {
                        shortcutInfo.AppPath = envPath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Warn($"Failed to parse \"{lnkPath}\": {ex.Message}");
        }
    }

    private static string ReadStringData(BinaryReader reader, bool isUnicode, string fieldName="")
    {
        byte[] stringBytes;
        string value;
        ushort charCount = reader.ReadUInt16();
        if (charCount == 0)
        {
            return "";
        }

        if (isUnicode)
        {
            stringBytes = reader.ReadBytes(charCount * 2);
            value = Encoding.Unicode.GetString(stringBytes);
        }
        else
        {
            stringBytes = reader.ReadBytes(charCount);
            value = Encoding.ASCII.GetString(stringBytes);
        }

        return value;
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}

public class ShortcutScanner
{
    private List<ShortcutInfo> shortcuts = new List<ShortcutInfo>();

    public List<ShortcutInfo> ScanAllLocations()
    {
        foreach(var dir in App.Config.ShortcutsLocations)
        {
            ScanDirectory(ProgramConfig.ExpandEnvironmentVariables(dir.Value), dir.Key);
        }

        return shortcuts;
    }

    private void ScanDirectory(string directoryPath, string locationType)
    {
        try
        {
            shortcuts.Add(new ShortcutInfo
            {
                Name = Path.GetFileName(directoryPath),
                Path = directoryPath,
                DirectoryPath = Path.GetDirectoryName(directoryPath),
                LocationType = locationType,
                IsDirectory = true
            });

            Dictionary<string, string> localizedNames = ParseLocalizedFileNames(Path.Combine(directoryPath, "desktop.ini"));

            foreach (string file in Directory.GetFiles(directoryPath, "*.lnk"))
            {
                var shortcut = GetShortcutInfo(file, locationType);
                if (shortcut != null)
                {
                    if(localizedNames.Count > 0)
                    {
                        string key = shortcut.Name.ToLower();
                        if(localizedNames.ContainsKey(key))
                        {
                            shortcut.Name = localizedNames[key];
                        }
                    }

                    shortcuts.Add(shortcut);
                }
            }

            foreach (string dir in Directory.GetDirectories(directoryPath))
            {
                ScanDirectory(dir, locationType);
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed to scan \"{directoryPath}\": {ex.Message}");
        }
    }

    public Dictionary<string, string> ParseLocalizedFileNames(string filePath)
    {
        var localizedNames = new Dictionary<string, string>();
        
        try
        {
            if (!File.Exists(filePath))
            {
                return localizedNames;
            }

            string[] lines = File.ReadAllLines(filePath);
            bool isInLocalizedFileNamesSection = false;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                
                if (trimmedLine.Equals("[LocalizedFileNames]", StringComparison.OrdinalIgnoreCase))
                {
                    isInLocalizedFileNamesSection = true;
                    continue;
                }
                else if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    isInLocalizedFileNamesSection = false;
                    continue;
                }

                if (isInLocalizedFileNamesSection && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string fileName = parts[0].Trim();
                        string resourceInfo = parts[1].Trim();

                        fileName = Path.GetFileNameWithoutExtension(fileName).ToLower();

                        string localizedName = GetLocalizedResourceString(resourceInfo);
                        if (!string.IsNullOrEmpty(localizedName))
                        {
                            localizedNames[fileName] = localizedName;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Warn($"Failed to parse \"{filePath}\": {ex.Message}");
        }

        return localizedNames;
    }

    private string GetLocalizedResourceString(string resourceInfo)
    {
        if (string.IsNullOrEmpty(resourceInfo) || !resourceInfo.StartsWith("@"))
        {
            return null;
        }

        try
        {
            string info = resourceInfo.Substring(1);
            
            string[] parts = info.Split(',');
            if (parts.Length != 2)
            {
                return null;
            }

            string dllPath = Environment.ExpandEnvironmentVariables(parts[0]);
            
            if (!uint.TryParse(parts[1].TrimStart('-'), out uint resourceId))
            {
                return null;
            }

            IntPtr libraryHandle = Win32.LoadLibraryEx(dllPath, IntPtr.Zero, Win32.LOAD_LIBRARY_AS_DATAFILE);
            if (libraryHandle == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                StringBuilder buffer = new StringBuilder(1024);
                int length = Win32.LoadString(libraryHandle, resourceId, buffer, buffer.Capacity);
                
                if (length > 0)
                {
                    return buffer.ToString();
                }
            }
            finally
            {
                Win32.FreeLibrary(libraryHandle);
            }
        }
        catch (Exception ex)
        {
            log.Warn($"Failed to parse \"{resourceInfo}\": {ex.Message}");
        }
        return null;
    }

    private ShortcutInfo GetShortcutInfo(string shortcutPath, string locationType)
    {
        ShortcutInfo shortcutInfo = new ShortcutInfo
        {
            Name = Path.GetFileNameWithoutExtension(shortcutPath),
            Path = shortcutPath,
            DirectoryPath = Path.GetDirectoryName(shortcutPath),
            LocationType = locationType,
            IsDirectory = false
        };

        ShortcutParser.ParseLnkFile(shortcutPath, ref shortcutInfo);

        return shortcutInfo;
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}