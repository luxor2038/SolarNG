using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight.Helpers;
using GalaSoft.MvvmLight.Messaging;

namespace GalaSoft.MvvmLight;

public abstract class ViewModelBase : ObservableObject, ICleanup
{
    private IMessenger _messengerInstance;

    public bool IsInDesignMode => IsInDesignModeStatic;

    public static bool IsInDesignModeStatic => DesignerLibrary.IsInDesignMode;

    protected IMessenger MessengerInstance
    {
        get
        {
            return _messengerInstance ?? Messenger.Default;
        }
        set
        {
            _messengerInstance = value;
        }
    }

    public ViewModelBase()
        : this(null)
    {
    }

    public ViewModelBase(IMessenger messenger)
    {
        MessengerInstance = messenger;
    }

    public virtual void Cleanup()
    {
        MessengerInstance.Unregister(this);
    }

    protected virtual void Broadcast<T>(T oldValue, T newValue, string propertyName)
    {
        PropertyChangedMessage<T> message = new PropertyChangedMessage<T>(this, oldValue, newValue, propertyName);
        MessengerInstance.Send(message);
    }

    public virtual void RaisePropertyChanged<T>([CallerMemberName] string propertyName = null, T oldValue = default(T), T newValue = default(T), bool broadcast = false)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            throw new ArgumentException("This method cannot be called with an empty string", "propertyName");
        }
        RaisePropertyChanged(propertyName);
        if (broadcast)
        {
            Broadcast(oldValue, newValue, propertyName);
        }
    }

    public virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression, T oldValue, T newValue, bool broadcast)
    {
        RaisePropertyChanged(propertyExpression);
        if (broadcast)
        {
            Broadcast(oldValue, newValue, ObservableObject.GetPropertyName(propertyExpression));
        }
    }

    protected bool Set<T>(Expression<Func<T>> propertyExpression, ref T field, T newValue, bool broadcast)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }
        T oldValue = field;
        field = newValue;
        RaisePropertyChanged(propertyExpression, oldValue, field, broadcast);
        return true;
    }

    protected bool Set<T>(string propertyName, ref T field, T newValue = default(T), bool broadcast = false)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }
        T oldValue = field;
        field = newValue;
        RaisePropertyChanged(propertyName, oldValue, field, broadcast);
        return true;
    }

    protected bool Set<T>(ref T field, T newValue = default(T), bool broadcast = false, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }
        T oldValue = field;
        field = newValue;
        RaisePropertyChanged(propertyName, oldValue, field, broadcast);
        return true;
    }
}
