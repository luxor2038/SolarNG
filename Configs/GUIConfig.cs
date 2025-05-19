using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SolarNG.Configs;

[DataContract]
internal class GUIConfig
{
    [DataMember]
    public string Language = null;

    [DataMember]
    public string Theme = null;

    [DataMember]
    public bool Logo = true;

    [DataMember]
    public bool Hotkey = true;

    [DataMember]
    public bool ConfirmClosingTab = false;

    [DataMember]
    public bool ConfirmClosingWindow = true;

    [DataMember]
    public bool RandomColor = true;

    [DataMember]
    public int MaximizedBorderThickness = 8;

    [DataMember]
    public bool Maximized = false;

    [DataMember]
    public int Width = 1024;

    [DataMember]
    public int Height = 768;

    [DataMember]
    public double MinWidthScale = 0.53333;

    [DataMember]
    public string Monitor = null;

    [DataMember]
    public uint WindowStyleMask = 0xC40000;

    [DataMember]
    public string OverviewOrderBy = "Name";

    [DataMember]
    public bool AutoSaveQuickNew = false;

    [DataMember]
    public bool AutoCloseOverview = true;

    [DataMember]
    public int WaitTimeout = 50;

    [DataMember]
    public bool CloseIME = true;

    [DataMember]
    public List<string> ExcludeClasses = new List<string>{ "Shell_TrayWnd",  "Windows.UI.Core.CoreWindow", "NarratorHelperWindow", "ThumbnailDeviceHelperWnd", "WorkerW" };

    [DataMember]
    public List<string> ExcludeShortcuts = new List<string>{ "SolarNG",  "unins" };

    public GUIConfig()
    {
    }
}
