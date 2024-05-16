using System;
using System.Reflection;
using System.Windows.Input;
using GalaSoft.MvvmLight.Helpers;

namespace GalaSoft.MvvmLight.Command;

public class RelayCommand<T> : ICommand
{
    private readonly WeakAction<T> _execute;

    private readonly WeakFunc<T, bool> _canExecute;

    public event EventHandler CanExecuteChanged;

    public RelayCommand(Action<T> execute, bool keepTargetAlive = false)
        : this(execute, (Func<T, bool>)null, keepTargetAlive)
    {
    }

    public RelayCommand(Action<T> execute, Func<T, bool> canExecute, bool keepTargetAlive = false)
    {
        if (execute == null)
        {
            throw new ArgumentNullException("execute");
        }
        _execute = new WeakAction<T>(execute, keepTargetAlive);
        if (canExecute != null)
        {
            _canExecute = new WeakFunc<T, bool>(canExecute, keepTargetAlive);
        }
    }

    public void RaiseCanExecuteChanged()
    {
        this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanExecute(object parameter)
    {
        if (_canExecute == null)
        {
            return true;
        }
        if (_canExecute.IsStatic || _canExecute.IsAlive)
        {
            if (parameter == null && typeof(T).GetTypeInfo().IsValueType)
            {
                return _canExecute.Execute(default(T));
            }
            if (parameter == null || parameter is T)
            {
                return _canExecute.Execute((T)parameter);
            }
        }
        return false;
    }

    public virtual void Execute(object parameter)
    {
        if (!CanExecute(parameter) || _execute == null || (!_execute.IsStatic && !_execute.IsAlive))
        {
            return;
        }
        if (parameter == null)
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                _execute.Execute(default(T));
            }
            else
            {
                _execute.Execute((T)parameter);
            }
        }
        else
        {
            _execute.Execute((T)parameter);
        }
    }
}
public class RelayCommand : ICommand
{
    private readonly WeakAction _execute;

    private readonly WeakFunc<bool> _canExecute;

    public event EventHandler CanExecuteChanged;

    public RelayCommand(Action execute, bool keepTargetAlive = false)
        : this(execute, null, keepTargetAlive)
    {
    }

    public RelayCommand(Action execute, Func<bool> canExecute, bool keepTargetAlive = false)
    {
        if (execute == null)
        {
            throw new ArgumentNullException("execute");
        }
        _execute = new WeakAction(execute, keepTargetAlive);
        if (canExecute != null)
        {
            _canExecute = new WeakFunc<bool>(canExecute, keepTargetAlive);
        }
    }

    public void RaiseCanExecuteChanged()
    {
        this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanExecute(object parameter)
    {
        if (_canExecute != null)
        {
            if (_canExecute.IsStatic || _canExecute.IsAlive)
            {
                return _canExecute.Execute();
            }
            return false;
        }
        return true;
    }

    public virtual void Execute(object parameter)
    {
        if (CanExecute(parameter) && _execute != null && (_execute.IsStatic || _execute.IsAlive))
        {
            _execute.Execute();
        }
    }
}
