using System;
using System.Reflection;

namespace GalaSoft.MvvmLight.Helpers;

public class WeakAction<T> : WeakAction, IExecuteWithObject
{
    private Action<T> _staticAction;

    public override string MethodName
    {
        get
        {
            if (_staticAction != null)
            {
                return _staticAction.GetMethodInfo().Name;
            }
            return base.Method.Name;
        }
    }

    public override bool IsAlive
    {
        get
        {
            if (_staticAction == null && base.Reference == null)
            {
                return false;
            }
            if (_staticAction != null)
            {
                if (base.Reference != null)
                {
                    return base.Reference.IsAlive;
                }
                return true;
            }
            return base.Reference.IsAlive;
        }
    }

    public WeakAction(Action<T> action, bool keepTargetAlive = false)
        : this(action?.Target, action, keepTargetAlive)
    {
    }

    public WeakAction(object target, Action<T> action, bool keepTargetAlive = false)
    {
        if (action.GetMethodInfo().IsStatic)
        {
            _staticAction = action;
            if (target != null)
            {
                base.Reference = new WeakReference(target);
            }
        }
        else
        {
            base.Method = action.GetMethodInfo();
            base.ActionReference = new WeakReference(action.Target);
            base.LiveReference = (keepTargetAlive ? action.Target : null);
            base.Reference = new WeakReference(target);
        }
    }

    public new void Execute()
    {
        Execute(default(T));
    }

    public void Execute(T parameter)
    {
        if (_staticAction != null)
        {
            _staticAction(parameter);
            return;
        }
        object actionTarget = base.ActionTarget;
        if (IsAlive && base.Method != null && (base.LiveReference != null || base.ActionReference != null) && actionTarget != null)
        {
            base.Method.Invoke(actionTarget, new object[1] { parameter });
        }
    }

    public void ExecuteWithObject(object parameter)
    {
        Execute((T)parameter);
    }

    public new void MarkForDeletion()
    {
        _staticAction = null;
        base.MarkForDeletion();
    }
}
public class WeakAction
{
    private Action _staticAction;

    protected MethodInfo Method { get; set; }

    public virtual string MethodName
    {
        get
        {
            if (_staticAction != null)
            {
                return _staticAction.GetMethodInfo().Name;
            }
            return Method.Name;
        }
    }

    protected WeakReference ActionReference { get; set; }

    protected object LiveReference { get; set; }

    protected WeakReference Reference { get; set; }

    public bool IsStatic => _staticAction != null;

    public virtual bool IsAlive
    {
        get
        {
            if (_staticAction == null && Reference == null && LiveReference == null)
            {
                return false;
            }
            if (_staticAction != null)
            {
                if (Reference != null)
                {
                    return Reference.IsAlive;
                }
                return true;
            }
            if (LiveReference == null)
            {
                if (Reference != null)
                {
                    return Reference.IsAlive;
                }
                return false;
            }
            return true;
        }
    }

    public object Target
    {
        get
        {
            if (Reference == null)
            {
                return null;
            }
            return Reference.Target;
        }
    }

    protected object ActionTarget
    {
        get
        {
            if (LiveReference != null)
            {
                return LiveReference;
            }
            if (ActionReference == null)
            {
                return null;
            }
            return ActionReference.Target;
        }
    }

    protected WeakAction()
    {
    }

    public WeakAction(Action action, bool keepTargetAlive = false)
        : this(action?.Target, action, keepTargetAlive)
    {
    }

    public WeakAction(object target, Action action, bool keepTargetAlive = false)
    {
        if (action.GetMethodInfo().IsStatic)
        {
            _staticAction = action;
            if (target != null)
            {
                Reference = new WeakReference(target);
            }
        }
        else
        {
            Method = action.GetMethodInfo();
            ActionReference = new WeakReference(action.Target);
            LiveReference = (keepTargetAlive ? action.Target : null);
            Reference = new WeakReference(target);
        }
    }

    public void Execute()
    {
        if (_staticAction != null)
        {
            _staticAction();
            return;
        }
        object actionTarget = ActionTarget;
        if (IsAlive && Method != null && (LiveReference != null || ActionReference != null) && actionTarget != null)
        {
            Method.Invoke(actionTarget, null);
        }
    }

    public void MarkForDeletion()
    {
        Reference = null;
        ActionReference = null;
        LiveReference = null;
        Method = null;
        _staticAction = null;
    }
}
