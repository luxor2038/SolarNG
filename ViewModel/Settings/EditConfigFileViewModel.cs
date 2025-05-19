using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using log4net;
using Microsoft.Win32;
using SolarNG.Sessions;
using SolarNG.Utilities;

namespace SolarNG.ViewModel.Settings;

public class EditConfigFileViewModel : ViewModelBase, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public ConfigFilesListViewModel ConfigFilesListVM;

    public Brush TitleBackground { get; set; }

    public bool BatchMode { get; set; }

    public bool NewMode { get; set; }

    public bool EditMode => !BatchMode && !NewMode;

    public bool ControlVisible {  get; set; }

    public ConfigFile EditedConfigFile { get; set; }

    public string Name
    {
        get
        {
            return EditedConfigFile.Name;
        }
        set
        {
            EditedConfigFile.Name = value;
            NotifyPropertyChanged("Name");
        }
    }

    public string ConfigFileType
    {
        get
        {
            return EditedConfigFile.Type;
        }
        set
        {
            EditedConfigFile.Type = value;
            NotifyPropertyChanged("ConfigFileType");
            NotifyPropertyChanged("PuTTYValid");
            NotifyPropertyChanged("ImportFileEnabled");
        }
    }

    private ObservableCollection<ComboBoxOne> _ConfigFileTypeList;
    public ObservableCollection<ComboBoxOne> ConfigFileTypeList
    {
        get
        {
            return _ConfigFileTypeList;
        }
        set
        {
            _ConfigFileTypeList = value;
            NotifyPropertyChanged("ConfigFileTypeList");
        }
    }

    public bool ConfigFileTypeComboxEnabled => NewMode && string.IsNullOrEmpty(EditedConfigFile.Data);

    public string Path
    {
        get
        {
            return EditedConfigFile.Path;
        }
        set
        {
            EditedConfigFile.RealPath = value;
            NotifyPropertyChanged("Path");
            NotifyPropertyChanged("PuTTYValid");
            NotifyPropertyChanged("ImportFileEnabled");
        }
    }

    public bool ImportFileEnabled => NewMode || ConfigFileType != "PuTTY";

    public RelayCommand ImportFileCommand { get; set; }
    private void OnImportFile()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (ConfigFileType == "PrivateKey")
        {
            openFileDialog.Filter = "*.ppk|*.ppk|*.*|*.*";
        }
        else if (ConfigFileType == "Script")
        {
            openFileDialog.Filter = "*.sh;*.txt|*.sh;*.txt|*.*|*.*";
        }
        else if (ConfigFileType == "RDP")
        {
            openFileDialog.Filter = "*.rdp|*.rdp|*.*|*.*";
        }
        else if (ConfigFileType == "WinSCP")
        {
            openFileDialog.Filter = "*.ini|*.ini|*.*|*.*";
        }
        else if (ConfigFileType == "PuTTY")
        {
            openFileDialog.Filter = "*.ini;*.reg|*.ini;*.reg|*.*|*.*";
        }
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            Path = openFileDialog.FileName;
            UpdateGUI();
        }
    }

    public bool PuTTYValid => NewMode && ConfigFileType == "PuTTY" && string.IsNullOrEmpty(EditedConfigFile.Data);

    public RelayCommand PuTTYConfigCommand { get; set; }
    private void OnPuTTYConfig()
    {
        try
        {
            Process.Start(App.Config.PuTTY.FullPath);
        }
        catch (Exception ex)
        {
            log.Error($"Unable to start \"{App.Config.PuTTY.FullPath}\", {ex}");
        }
    }

    private string _PuTTYSession;
    public string PuTTYSession
    {
        get
        {
            return _PuTTYSession;
        }
        set
        {
            _PuTTYSession = value;
            NotifyPropertyChanged("PuTTYSession");
        }
    }

    private ObservableCollection<ComboBoxOne> _PuTTYSessionList;
    public ObservableCollection<ComboBoxOne> PuTTYSessionList
    {
        get
        {
            return _PuTTYSessionList;
        }
        set
        {
            _PuTTYSessionList = value;
            NotifyPropertyChanged("PuTTYSessionList");
        }
    }

    private void CreatePuTTYSessionList()
    {
        PuTTYSessionList = new ObservableCollection<ComboBoxOne>();
        foreach (string puttySession in RegistryHelper.GetPuttySessions())
        {
            PuTTYSessionList.Add(new ComboBoxOne(Uri.UnescapeDataString(puttySession)));
        }
        PuTTYSession = PuTTYSessionList.ElementAt(0).Key;
    }

    public string Comment
    {
        get
        {
            return EditedConfigFile.Comment;
        }
        set
        {
            EditedConfigFile.Comment = value;
            NotifyPropertyChanged("Comment");
        }
    }

    private Visibility _OkValidationVisibility;
    public Visibility OkValidationVisibility
    {
        get
        {
            return _OkValidationVisibility;
        }
        set
        {
            _OkValidationVisibility = value;
            NotifyPropertyChanged("OkValidationVisibility");
        }
    }

    public RelayCommand SaveCommand { get; set; }
    private void OnSaveConfigFile()
    {
        if (!InputIsValid())
        {
            return;
        }

        if(NewMode)
        {
            ConfigFile configFile = SaveConfigFile();
            ConfigFilesListVM.SelectItem(configFile);
            ConfigFilesListVM.ListUpdate();
            return;
        }
        
        if(BatchMode)
        {
            SaveConfigFiles();
        }
        else
        {
            SaveConfigFile();
        }
    }

    private void SaveConfigFiles()
    {
        foreach(ConfigFile configFile in SelectedConfigFiles)
        {
            if(EditedConfigFile.Comment != "!NoChange!")
            {
                configFile.Comment = string.IsNullOrWhiteSpace(EditedConfigFile.Comment) ? null : EditedConfigFile.Comment.Trim();
            }
        }
    }

    private ConfigFile SaveConfigFile()
    {
        ConfigFile configFile = SelectedConfigFile ?? new ConfigFile("PrivateKey");

        configFile.Name = Name;
        configFile.Type = ConfigFileType;
        configFile.Path = Path;
        configFile.Data = EditedConfigFile.Data;
        configFile.Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();

        if(SelectedConfigFile == null)
        {
            App.Sessions.ConfigFiles.Add(configFile);
        }
        else
        {
            if(configFile.Type == "PrivateKey")
            {
                foreach(Credential credential in App.Sessions.Credentials.Where(c => c.PrivateKeyId == configFile.Id))
                {
                    credential.OnPropertyChanged("PrivateKeyName");
                }
            }
            else if(configFile.Type == "RDP")
            {
                foreach(Session session in App.Sessions.Sessions.Where(s => s.MSTSCId == configFile.Id))
                {
                    string rdpFile = session.GetRDPFile();

                    try
                    {
                        File.Delete(rdpFile);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        return configFile;
    }

    public EditConfigFileViewModel()
    {
        EditedConfigFile = new ConfigFile("PrivateKey");
        ConfigFileTypeList = new ObservableCollection<ComboBoxOne>
        {
            new ComboBoxOne("PrivateKey"),
            new ComboBoxOne("Script"),
            new ComboBoxOne("RDP"),
            new ComboBoxOne("WinSCP"),
            new ComboBoxOne("PuTTY")
        };
        PuTTYConfigCommand = new RelayCommand(OnPuTTYConfig);
        ImportFileCommand = new RelayCommand(OnImportFile);
        SaveCommand = new RelayCommand(OnSaveConfigFile);
        UpdateGUI(Visibility.Hidden);
    }

    private List<ConfigFile> SelectedConfigFiles;
    public void ShowSelectedConfigFiles(List<ConfigFile> configFiles)
    {
        SelectedConfigFiles = configFiles;

        if(configFiles.Count == 1)
        {
            ShowSelectedConfigFile(configFiles[0]);
            return;
        }
        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = true;
        NewMode = false;

        SelectedConfigFile = new ConfigFile();

        EditedConfigFile = null;

        foreach(ConfigFile configFile in configFiles)
        {
            if(EditedConfigFile == null)
            {
                EditedConfigFile = new ConfigFile
                {
                    Comment = configFile.Comment
                };
                continue;
            }

            if(Comment != "!NoChange!" && Comment != configFile.Comment)
            {
                Comment = "!NoChange!";
            }
        }

        UpdateGUI();
        HideNotifications();
    }

    public void ShowSelectedConfigFile(ConfigFile configFile)
    {
        TitleBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
        BatchMode = false;
        NewMode = false;

        EditedConfigFile = LoadSelectedConfigFile(configFile);
        UpdateGUI();
        HideNotifications();
    }

    private ConfigFile SelectedConfigFile;
    private ConfigFile LoadSelectedConfigFile(ConfigFile configFile)
    {
        SelectedConfigFile = configFile;

        EditedConfigFile = new ConfigFile("PrivateKey")
        {
            Id = configFile.Id,
            Name = configFile.Name,
            Type = configFile.Type,
            Path = configFile.Path,
            Data = configFile.Data,
            Comment = configFile.Comment
        };
        return EditedConfigFile;
    }

    public void HideControl()
    {
        ControlVisible = false;
        NotifyPropertyChanged("ControlVisible");
    }

    private void HideNotifications()
    {
        RemoveError("Name");
        RemoveError("Path");
    }

    private void UpdateGUI(Visibility controlVisibility = Visibility.Visible)
    {
        ControlVisible = controlVisibility == Visibility.Visible;
        NotifyPropertyChanged("TitleBackground");
        NotifyPropertyChanged("NewMode");
        NotifyPropertyChanged("BatchMode");
        NotifyPropertyChanged("EditMode");
        NotifyPropertyChanged("ControlVisible");
        NotifyPropertyChanged("Name");
        NotifyPropertyChanged("ConfigFileType");
        NotifyPropertyChanged("ConfigFileTypeList");
        NotifyPropertyChanged("ConfigFileTypeComboxEnabled");
        NotifyPropertyChanged("PuTTYValid");
        NotifyPropertyChanged("PuTTYSessionList");
        NotifyPropertyChanged("PuTTYSession");
        NotifyPropertyChanged("Path");
        NotifyPropertyChanged("ImportFileEnabled");
        NotifyPropertyChanged("Comment");
        OkValidationVisibility = Visibility.Collapsed;
    }

    private bool NameHasExisted(string name)
    {
        return App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile c) => c.Name == name && c.Type == ConfigFileType && c.Id != EditedConfigFile.Id) != null;
    }

    public void CreateNewConfigFile()
    {
        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = false;
        NewMode = true;
        SelectedConfigFile = null;
        SelectedConfigFiles = null;
        CreatePuTTYSessionList();

        EditedConfigFile = new ConfigFile("PrivateKey");
        UpdateGUI();
        HideNotifications();
    }

    public void SaveCurrent()
    {
        OnSaveConfigFile();
    }

    private bool InputIsValid()
    {
        if(BatchMode)
        {
            HideNotifications();
            return true;
        }

        if ((ConfigFileType != "PuTTY" || (ConfigFileType == "PuTTY" && PuTTYSession == null)) && (string.IsNullOrWhiteSpace(Path) || string.IsNullOrEmpty(EditedConfigFile.Data)))
        {
            AddError("Path", string.Format(Application.Current.Resources["NotExist"] as string, Application.Current.Resources["ConfigFilePath"]));
            return !HasErrors;
        }
        else
        {
            RemoveError("Path");
        }

        if(string.IsNullOrEmpty(EditedConfigFile.Data))
        {
            EditedConfigFile.Data = ConfigFile.GetPuTTYSession(PuTTYSession);
            EditedConfigFile.Path = PuTTYSession.Trim() + ".ini";

            foreach(char c in System.IO.Path.GetInvalidPathChars())
            {
                int n = c;
                EditedConfigFile.Path = EditedConfigFile.Path.Replace(c.ToString(), "%" + n.ToString("x2"));
            }

            int num = 2;
            string name;
            string name2 = name = PuTTYSession.Trim();
            while (NameHasExisted(name))
            {
                name = name2 + " (" + num + ")";
                num++;
            }
            Name = name;

            RemoveError("Name");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            int num = 2;
            string name;
            string name2 = name = System.IO.Path.GetFileName(Path);
            while (NameHasExisted(name))
            {
                name = name2 + " (" + num + ")";
                num++;
            }
            Name = name;

            RemoveError("Name");
        }

        Name = Name.Trim();
        if (NameHasExisted(Name))
        {
            if (!NewMode)
            {
                AddError("Name", string.Format(Application.Current.Resources["Exist"] as string, Application.Current.Resources["ConfigFileName"]));
            }
            else
            {
                string name = Name;
                int num = 2;
                while (NameHasExisted(name))
                {
                    name = Name + " (" + num + ")";
                    num++;
                }
                Name = name;
                RemoveError("Name");
            }
        }
        else
        {
            RemoveError("Name");
        }
        return !HasErrors;
    }

    private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
    public bool HasErrors => errors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    public IEnumerable GetErrors(string propertyName)
    {
        if (!errors.ContainsKey(propertyName))
        {
            return null;
        }
        return errors[propertyName];
    }

    protected void ValidateProperty<T>(string propertyName, T value)
    {
        List<System.ComponentModel.DataAnnotations.ValidationResult> list = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        ValidationContext validationContext = new ValidationContext(this) { MemberName = propertyName };
        Validator.TryValidateProperty(value, validationContext, list);
        if (list.Any())
        {
            errors[propertyName] = list.Select((System.ComponentModel.DataAnnotations.ValidationResult r) => r.ErrorMessage).ToList();
        }
        else
        {
            errors.Remove(propertyName);
        }
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void AddError(string propertyName, string error)
    {
        errors[propertyName] = new List<string> { error };
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void RemoveError(string propertyName)
    {
        errors.Remove(propertyName);
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    public new event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(string Property)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));
    }

    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
}
