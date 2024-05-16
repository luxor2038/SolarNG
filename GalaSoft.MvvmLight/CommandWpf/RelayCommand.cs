using System;
using System.Threading;
using System.Windows.Input;
using GalaSoft.MvvmLight.Helpers;

namespace GalaSoft.MvvmLight.CommandWpf;

public class RelayCommand<T> : ICommand
{
    private readonly WeakAction<T> _execute;

    private readonly WeakFunc<T, bool> _canExecute;

    public event EventHandler CanExecuteChanged
    {
        add
        {
            if (_canExecute != null)
            {
                CommandManager.RequerySuggested += value;
            }
        }
        remove
        {
            if (_canExecute != null)
            {
                CommandManager.RequerySuggested -= value;
            }
        }
    }

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
        CommandManager.InvalidateRequerySuggested();
    }

    public bool CanExecute(object parameter)
    {
        if (_canExecute == null)
        {
            return true;
        }
        if (_canExecute.IsStatic || _canExecute.IsAlive)
        {
            if (parameter == null && typeof(T).IsValueType)
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
        object obj = parameter;
        if (parameter != null && parameter.GetType() != typeof(T) && parameter is IConvertible)
        {
            obj = Convert.ChangeType(parameter, typeof(T), null);
        }
        if (!CanExecute(obj) || _execute == null || (!_execute.IsStatic && !_execute.IsAlive))
        {
            return;
        }
        if (obj == null)
        {
            if (typeof(T).IsValueType)
            {
                _execute.Execute(default(T));
            }
            else
            {
                _execute.Execute((T)obj);
            }
        }
        else
        {
            _execute.Execute((T)obj);
        }
    }
}
public class RelayCommand : ICommand
{
    private readonly WeakAction _execute;

    private readonly WeakFunc<bool> _canExecute;

    private EventHandler _requerySuggestedLocal;

    public event EventHandler CanExecuteChanged
    {
        add
        {
            if (_canExecute != null)
            {
                EventHandler eventHandler = _requerySuggestedLocal;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    eventHandler = Interlocked.CompareExchange(value: (EventHandler)Delegate.Combine(eventHandler2, value), location1: ref _requerySuggestedLocal, comparand: eventHandler2);
                }
                while (eventHandler != eventHandler2);
                CommandManager.RequerySuggested += value;
            }
        }
        remove
        {
            if (_canExecute != null)
            {
                EventHandler eventHandler = _requerySuggestedLocal;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    eventHandler = Interlocked.CompareExchange(value: (EventHandler)Delegate.Remove(eventHandler2, value), location1: ref _requerySuggestedLocal, comparand: eventHandler2);
                }
                while (eventHandler != eventHandler2);
                CommandManager.RequerySuggested -= value;
            }
        }
    }

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
        CommandManager.InvalidateRequerySuggested();
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
