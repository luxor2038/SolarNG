using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using SolarNG.Sessions;
using SolarNG.Utilities;

namespace SolarNG.ViewModel.Settings;

public class EditCredentialViewModel : ViewModelBase, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public CredentialsListViewModel CredentialsListVM; 

    public Credential EditedCredential { get; set; } = new Credential();

    public Brush TitleBackground { get; set; }

    public bool BatchMode { get; set; }

    public bool NewMode { get; set; }

    public bool EditMode => !BatchMode && !NewMode;

    public bool ControlVisible { get; set; }

    public string Name
    {
        get
        {
            return EditedCredential.Name;
        }
        set
        {
            EditedCredential.Name = value;
            NotifyPropertyChanged("Name");
        }
    }

    public string Username
    {
        get
        {
            return EditedCredential.Username;
        }
        set
        {
            EditedCredential.Username = value;
            NotifyPropertyChanged("Username");
        }
    }

    private SafeString NoChangePassword = new SafeString("!NoChange!");

    public SecureString Password
    {
        get
        {
            return EditedCredential.Password?.ToSecureString();
        }
        set
        {
            if(value != null)
            {
                EditedCredential.Password = new SafeString(value);
            }
            NotifyPropertyChanged("Password");
        }
    }

    public SecureString Passphrase
    {
        get
        {
            return EditedCredential.Passphrase?.ToSecureString();
        }
        set
        {
            if(value != null)
            {
                EditedCredential.Passphrase= new SafeString(value);
            }
            NotifyPropertyChanged("Passphrase");
        }
    }

    private Guid NoChangeId = Guid.NewGuid();

    private bool PrivateKeyHasNoChangeId = false;

    public Guid PrivateKeyId
    {
        get
        {
            return EditedCredential.PrivateKeyId;
        }
        set
        {
            EditedCredential.PrivateKeyId = value;
            NotifyPropertyChanged("PrivateKeyId");
        }
    }

    public ObservableCollection<ComboBoxGuid> _PrivateKeys;
    public ObservableCollection<ComboBoxGuid> PrivateKeys 
    {
        get
        {
            return _PrivateKeys;
        }
        set
        {
            _PrivateKeys = value;
            NotifyPropertyChanged("PrivateKeys");
        }
    }

    public RelayCommand ImportPrivateKeyCommand { get; set; }
    private void OnImportPrivateKey()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "*.ppk|*.ppk|*.*|*.*" };
        openFileDialog.ShowDialog();
        if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            ConfigFile configFile = new ConfigFile("PrivateKey") { RealPath = openFileDialog.FileName };

            int num = 2;
            string text;
            string text2 = (text = Path.GetFileName(configFile.Path));
            while (App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile c) => c.Name == text && c.Type == configFile.Type) != null)
            {
                text = text2 + " (" + num + ")";
                num++;
            }
            configFile.Name = text;
            App.Sessions.ConfigFiles.Add(configFile);
            PrivateKeyId = configFile.Id;
            NotifyPropertyChanged("PrivateKeys");
        }
    }

    public string Comment
    {
        get
        {
            return EditedCredential.Comment;
        }
        set
        {
            EditedCredential.Comment = value;
            NotifyPropertyChanged("Comment");
        }
    }

    public RelayCommand SaveCommand { get; set; }
    private void OnSaveCredential()
    {
        if (!InputIsValid())
        {
            return;
        }

        if(NewMode)
        {
            Credential credential = SaveCredential();
            CredentialsListVM.SelectItem(credential);
            CredentialsListVM.ListUpdate();
            return;
        }
        
        if(BatchMode)
        {
            SaveCredentials();
        }
        else
        {
            SaveCredential();
        }
    }

    private void SaveCredentials()
    {
        foreach(Credential credential in SelectedCredentials)
        {
            bool remove_rdp = false;
            if(EditedCredential.Username != "!NoChange!")
            {
                credential.Username = EditedCredential.Username;
                remove_rdp = true;
            }

            if(EditedCredential.PrivateKeyId != NoChangeId)
            {
                credential.PrivateKeyId = EditedCredential.PrivateKeyId;

                if(credential.PrivateKeyId == Guid.Empty)
                {
                    credential.Passphrase = null;

                    if(EditedCredential.Password != NoChangePassword)
                    {
                        credential.Password = EditedCredential.Password;
                        remove_rdp = true;
                    }
                }
                else
                {
                    if(EditedCredential.Passphrase != NoChangePassword)
                    {
                        credential.Passphrase = EditedCredential.Passphrase;
                    }

                    if(credential.Password != null)
                    {
                        credential.Password = null;
                        remove_rdp = true;
                    }
                }
            }

            if(EditedCredential.Comment != "!NoChange!")
            {
                credential.Comment = string.IsNullOrWhiteSpace(EditedCredential.Comment) ? null : EditedCredential.Comment.Trim();
            }

            if(remove_rdp)
            {
                foreach(Session session in App.Sessions.Sessions.Where(s => s.CredentialId == credential.Id))
                {
                    RemoveRDPFile(session);
                }
            }
        }
    }

    private Credential SaveCredential()
    {
        Credential credential = SelectedCredential ?? new Credential();

        credential.Name = Name;
        credential.Username = Username;
        credential.PrivateKeyId = PrivateKeyId;

        if(credential.PrivateKeyId == Guid.Empty)
        {
            credential.Password = EditedCredential.Password;
            credential.Passphrase = null;
        }
        else
        {
            credential.Password = null;
            credential.Passphrase = EditedCredential.Passphrase;
        }

        credential.Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();

        if(SelectedCredential == null)
        {
            App.Sessions.Credentials.Add(credential);
        }
        else
        {
            foreach(Session session in App.Sessions.Sessions.Where(s => s.CredentialId == credential.Id))
            {
                session.OnPropertyChanged("CredentialName");

                RemoveRDPFile(session);
            }
         }

        return credential;
    }

    private void RemoveRDPFile(Session session)
    {
        string rdpFile = session.GetRDPFile();

        if(string.IsNullOrWhiteSpace(rdpFile))
        {
            return;
        }

        try
        {
            File.Delete(rdpFile);
        }
        catch (Exception)
        {
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

    public EditCredentialViewModel()
    {
        ImportPrivateKeyCommand = new RelayCommand(OnImportPrivateKey);
        SaveCommand = new RelayCommand(OnSaveCredential);

        App.Sessions.ConfigFiles.CollectionChanged += UpdatePrivateKeys;
        UpdateGUI(Visibility.Hidden);
    }

    public override void Cleanup()
    {
        App.Sessions.ConfigFiles.CollectionChanged -= UpdatePrivateKeys;

        foreach (ConfigFile privateKey in App.Sessions.ConfigFiles.Where(c => c.Type == "PrivateKey"))
        {
            if(privateKey.GetNameChange() != null)
            {
                privateKey.NameChange -= UpdatePrivateKey;
            }
        }

        base.Cleanup();
    }


    private List<Credential> SelectedCredentials;
    public void ShowSelectedCredentials(List<Credential> credentials)
    {
        SelectedCredentials = credentials;

        PrivateKeyHasNoChangeId = false;

        if(credentials.Count == 1)
        {
            ShowSelectedCredential(credentials[0]);
            return;
        }
        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = true;
        NewMode = false;

        SelectedCredential = new Credential();

        EditedCredential = null;

        foreach(Credential credential in credentials)
        {
            if(EditedCredential == null)
            {
                EditedCredential = new Credential
                {
                    Username = credential.Username,
                    PrivateKeyId = credential.PrivateKeyId,
                    Password = NoChangePassword,
                    Passphrase = NoChangePassword,
                    Comment = credential.Comment
                };

                UpdatePrivateKeys(null, null);
                continue;
            }

            if(Username != "!NoChange!" && Username != credential.Username)
            {
                Username = "!NoChange!";
            }

            if(PrivateKeyId != NoChangeId && PrivateKeyId != credential.PrivateKeyId)
            {
                if(PrivateKeys.ElementAt(0).Key != NoChangeId)
                {
                    PrivateKeys.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
                }
                PrivateKeyId = NoChangeId;
                PrivateKeyHasNoChangeId = true;
            }

            if(Comment != "!NoChange!" && Comment != credential.Comment)
            {
                Comment = "!NoChange!";
            }
        }

        UpdateGUI();
        HideNotifications();
    }

    public void ShowSelectedCredential(Credential credential)
    {
        TitleBackground = Application.Current.Resources["bg1"] as SolidColorBrush;
        BatchMode = false;
        NewMode = false;

        EditedCredential = LoadSelectedCredential(credential);
        UpdatePrivateKeys(null, null);
        UpdateGUI();
        HideNotifications();
    }

    Credential SelectedCredential;
    private Credential LoadSelectedCredential(Credential credential)
    {
        SelectedCredential = credential;

        EditedCredential = new Credential()
        {
            Id = credential.Id,
            Name = credential.Name,
            Username = credential.Username,
            Password = credential.Password,
            Passphrase = credential.Passphrase,
            PrivateKeyId = credential.PrivateKeyId,
            Comment = credential.Comment
        };
        return EditedCredential;
    }

    public void CreateNewCredential()
    {
        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = false;
        NewMode = true;
        SelectedCredential = null;
        SelectedCredentials = null;
        PrivateKeyHasNoChangeId = false;

        EditedCredential = new Credential();

        UpdatePrivateKeys(null, null);
        UpdateGUI();
        HideNotifications();
    }

    public void SaveCurrent()
    {
        OnSaveCredential();
    }

    private void UpdatePrivateKeys(object sender, NotifyCollectionChangedEventArgs e)
    {
        PrivateKeys = new ObservableCollection<ComboBoxGuid>
        {
            new ComboBoxGuid(Guid.Empty, Application.Current.Resources["ChoosePrivateKey"] as string)
        };

        if(PrivateKeyHasNoChangeId)
        {
            PrivateKeys.Insert(0, new ComboBoxGuid(NoChangeId, "!NoChange!"));
        }

        foreach (ConfigFile privateKey in from s in App.Sessions.ConfigFiles where s.Type == "PrivateKey" orderby s.Name select s)
        {
            PrivateKeys.Add(new ComboBoxGuid(privateKey.Id, privateKey.Name));
            if(privateKey.GetNameChange() != null)
            {
                privateKey.NameChange -= UpdatePrivateKey;
            }
            privateKey.NameChange += UpdatePrivateKey;
        }
        
        NotifyPropertyChanged("PrivateKeys");
        NotifyPropertyChanged("PrivateKeyId");
    }

    private void UpdatePrivateKey(object sender, EventArgs e)
    {
        UpdatePrivateKeys(null, null);
    }

    public void HideControl()
    {
        ControlVisible = false;
        NotifyPropertyChanged("ControlVisible");
    }

    private void HideNotifications()
    {
        RemoveError("Name");
        RemoveError("Username");
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
        NotifyPropertyChanged("Username");
        NotifyPropertyChanged("Password");
        NotifyPropertyChanged("Passphrase");
        NotifyPropertyChanged("PrivateKeys");
        NotifyPropertyChanged("PrivateKeyId");
        NotifyPropertyChanged("Comment");
        OkValidationVisibility = Visibility.Collapsed;
    }
    private bool CredentialNameHasExisted(string name)
    {
        return App.Sessions.Credentials.FirstOrDefault((Credential c) => c.Name == name && c.Id != EditedCredential.Id) == null;
    }

    private bool InputIsValid()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            AddError("Username", string.Format(Application.Current.Resources["Required"] as string, Application.Current.Resources["Username"]));
            return !HasErrors;
        }
        RemoveError("Username");

        Username = Username.Trim();

        Name = Name?.Trim();

        if (string.IsNullOrEmpty(Name))
        {
            int num = 2;
            string name = Username;
            while (!CredentialNameHasExisted(name))
            {
                name = Username + " (" + num + ")";
                num++;
            }
            Name = name;
            RemoveError("Name");
        }
        else if (!CredentialNameHasExisted(Name))
        {
            if (!NewMode)
            {
                AddError("Name", string.Format(Application.Current.Resources["Exist"] as string, Application.Current.Resources["CredentialName"]));
            }
            else
            {
                string name = Name;
                int num = 2;
                while (!CredentialNameHasExisted(name))
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
        List<ValidationResult> list = new List<ValidationResult>();
        ValidationContext validationContext = new ValidationContext(this) { MemberName = propertyName };
        Validator.TryValidateProperty(value, validationContext, list);
        if (list.Any())
        {
            errors[propertyName] = list.Select((ValidationResult r) => r.ErrorMessage).ToList();
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
}
