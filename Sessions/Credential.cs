using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SolarNG.Utilities;

namespace SolarNG.Sessions;

[DataContract]
public class Credential : INotifyPropertyChanged
{
    [DataMember]
    public Guid Id = Guid.NewGuid();

    private string _Name;
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
    public string Username;

    [DataMember]
    public SafeString Password = null;

    private Guid _PrivateKeyId = Guid.Empty;
    [DataMember]
    public Guid PrivateKeyId
    {
        get
        {
            return _PrivateKeyId;
        }
        set
        {
            _PrivateKeyId = value;
            privateKey = null;
            OnPropertyChanged("PrivateKeyId");
            OnPropertyChanged("PrivateKeyName");
        }
    }

    [DataMember]
    public SafeString Passphrase = null;

    [DataMember]
    public string Comment;

    private ConfigFile privateKey;
    public string PrivateKeyName
    {
        get
        {
            if(privateKey == null && PrivateKeyId != Guid.Empty)
            {
                privateKey = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile t) => t.Id == PrivateKeyId);
            }

            if(privateKey == null)
            {
                return null;
            }

            return privateKey.Name;
        }
    }

    public string PrivateKeyPath
    {
        get
        {
            if(privateKey == null && PrivateKeyId != Guid.Empty)
            {
                privateKey = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile t) => t.Id == PrivateKeyId);
            }

            if(privateKey == null)
            {
                return null;
            }

            return privateKey.Path;
        }
    }

    public string RealPrivateKeyPath
    {
        get
        {
            if(privateKey == null && PrivateKeyId != Guid.Empty)
            {
                privateKey = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile t) => t.Id == PrivateKeyId);
            }

            if(privateKey == null)
            {
                return null;
            }

            return privateKey.RealPath;
        }
        set
        {
            if(privateKey == null && PrivateKeyId != Guid.Empty)
            {
                privateKey = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile t) => t.Id == PrivateKeyId);
            }

            if(privateKey == null)
            {
                return;
            }

            privateKey.RealPath = value;
        }
    }

    public string PrivateKeyContent
    { 
        get
        {
            if(privateKey == null && PrivateKeyId != Guid.Empty)
            {
                privateKey = App.Sessions.ConfigFiles.FirstOrDefault((ConfigFile t) => t.Id == PrivateKeyId);
            }

            if(privateKey == null)
            {
                return null;
            }

            return privateKey.Data;
        }
    }

    public bool Matches(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        text = text.Trim().ToLower();

        if (Name.ToLower().Contains(text))
        {
            return true;
        }

        if (Username.ToLower().Contains(text))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(PrivateKeyName))
        {
            if(PrivateKeyName.ToLower().Contains(text))
            {
                return true;
            }
        }


        return false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
