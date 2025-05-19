using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace SolarNG.ViewModel;

public abstract class TabBase : ViewModelBase, INotifyDataErrorInfo
{
    private int _TabNumber;
    public int TabNumber
    {
        get
        {
            return _TabNumber;
        }
        set
        {
            if (_TabNumber != value)
            {
                Set(() => TabNumber, ref _TabNumber, value);
            }
        }
    }

    private string _TabName;
    public string TabName
    {
        get
        {
            return _TabName;
        }
        set
        {
            if (_TabName != value)
            {
                Set(() => TabName, ref _TabName, value);
            }
        }
    }

    private Color _UnderlineColor = Colors.Transparent;
    public Color UnderlineColor
    {
        get
        {
            return _UnderlineColor;
        }
        set
        {
            if (_UnderlineColor != value)
            {
                Set(() => UnderlineColor, ref _UnderlineColor, value);
            }
        }
    }

    protected virtual void ProcessExited(TabBase tab)
    {
        tab.UnderlineColor = Colors.Red;
    }

    protected virtual void ResetUnderlineColor(TabBase tab)
    {
        tab.UnderlineColor = Colors.Transparent;
    }

    private Brush _TabColor;
    public Brush TabColor
    {
        get
        {
            return _TabColor;
        }
        set
        {
            if (_TabColor != value)
            {
                Set(() => TabColor, ref _TabColor, value);
            }
        }
    }

    private Visibility _TabColorVisibility = Visibility.Collapsed;
    public Visibility TabColorVisibility
    {
        get
        {
            return _TabColorVisibility;
        }
        set
        {
            if (_TabColorVisibility != value)
            {
                Set(() => TabColorVisibility, ref _TabColorVisibility, value);
            }
        }
    }

    private Geometry _TabPath;
    public Geometry TabPath
    {
        get
        {
            return _TabPath;
        }
        set
        {
            if (_TabPath != value)
            {
                Set(() => TabPath, ref _TabPath, value);
            }
        }
    }

    private Visibility _TabPathVisibility = Visibility.Collapsed;
    public Visibility TabPathVisibility
    {
        get
        {
            return _TabPathVisibility;
        }
        set
        {
            if (_TabPathVisibility != value)
            {
                Set(() => TabPathVisibility, ref _TabPathVisibility, value);
            }
        }
    }

    private string _TabColorName;
    public string TabColorName
    {
        get
        {
            return _TabColorName;
        }
        set
        {
            if (_TabColorName != value)
            {
                Set(() => TabColorName, ref _TabColorName, value);
            }
        }
    }

    private Visibility _TabColorNameVisibility = Visibility.Collapsed;
    public Visibility TabColorNameVisibility
    {
        get
        {
            return _TabColorNameVisibility;
        }
        set
        {
            if (_TabColorNameVisibility != value)
            {
                Set(() => TabColorNameVisibility, ref _TabColorNameVisibility, value);
            }
        }
    }

    public virtual bool DuplicateTabCommandVisible => false;
    public RelayCommand DuplicateTabCommand { get; set; }

    public virtual bool ReconnectCommandVisible => false;
    public RelayCommand ReconnectCommand { get; set; }

    private string _CtrlF5;
    public string CtrlF5
    {
        get
        {
            if (_CtrlF5 == null)
            {
                CtrlF5 = (App.hotKeys.HotKeysDisabled ? "" : "Ctrl+F5");
            }
            return _CtrlF5;
        }
        set
        {
            _CtrlF5 = value;
        }
    }

    public virtual bool EditCommandVisible => false;
    public RelayCommand EditCommand { get; set; }

    public virtual bool SFTPCommandVisible => false;
    public RelayCommand SFTPCommand { get; set; }

    public virtual bool SCPCommandVisible => false;
    public RelayCommand SCPCommand { get; set; }

    public virtual bool OrderByCommandVisible => false;
    public RelayCommand OrderByCommand { get; set; }
    public virtual string Menu_OrderBy { get; set; }

    public virtual bool SwitchWindowTitleBarCommandVisible => false;
    public RelayCommand SwitchWindowTitleBarCommand { get; set; }

    public virtual bool KickCommandVisible => false;
    public RelayCommand KickCommand { get; set; }

    public virtual bool KickAllCommandVisible => true;
    public RelayCommand KickAllCommand { get; set; }

    public RelayCommand<TabBase> DetachCommand { get; set; }

    private string _CtrlW;
    public string CtrlW
    {
        get
        {
            if (_CtrlW == null)
            {
                App.hotKeys.PropertyChanged += HotKeysDisabledPropChanged;
                CtrlW = (App.hotKeys.HotKeysDisabled ? "" : "Ctrl+W");
            }
            return _CtrlW;
        }
        set
        {
            _CtrlW = value;
        }
    }

    private void HotKeysDisabledPropChanged(object sender, PropertyChangedEventArgs args)
    {
        CtrlW = (App.hotKeys.HotKeysDisabled ? "" : "Ctrl+W");
        RaisePropertyChanged("CtrlW");
        CtrlF5 = (App.hotKeys.HotKeysDisabled ? "" : "Ctrl+F5");
        RaisePropertyChanged("CtrlF5");
    }

    public virtual string WindowTitle => "SolarNG";

    public bool Detaching;

    public bool NeedDisableHotkey;

    public MainWindow MainWindow { get; set; }

    protected TabBase(MainWindow mainWindow)
    {
        MainWindow = mainWindow;
        TabColor = Application.Current.Resources["t8"] as SolidColorBrush;
        DetachCommand = new RelayCommand<TabBase>(MainWindow.DetachTabToNewWindow);
    }

    public override void Cleanup()
    {
        App.hotKeys.PropertyChanged -= HotKeysDisabledPropChanged;
        base.Cleanup();
    }

    public virtual bool ConfirmClosingTab()
    {
        return false;
    }

    public bool Killed;

    public bool Closed;
    public virtual bool CloseTab(bool noconfirm = false)
    {
        Closed = true;
        return true;
    }

    public bool CanActivate { get; set; } = true;
    public void Activate()
    {
        if (CanActivate)
        {
            ActivateTab();
        }
    }
    protected virtual void ActivateTab()
    {
    }

    public void Deactivate()
    {
        DeactivateTab();
    }

    protected virtual void DeactivateTab()
    {
    }

    private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
    public bool HasErrors => errors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

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

    public IEnumerable GetErrors(string propertyName)
    {
        if (!errors.ContainsKey(propertyName))
        {
            return null;
        }
        return errors[propertyName];
    }
}
