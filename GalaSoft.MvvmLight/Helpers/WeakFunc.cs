using System;
using System.Reflection;

namespace GalaSoft.MvvmLight.Helpers;

public class WeakFunc<T, TResult> : WeakFunc<TResult>, IExecuteWithObjectAndResult
{
    private Func<T, TResult> _staticFunc;

    public override string MethodName
    {
        get
        {
            if (_staticFunc != null)
            {
                return _staticFunc.GetMethodInfo().Name;
            }
            return base.Method.Name;
        }
    }

    public override bool IsAlive
    {
        get
        {
            if (_staticFunc == null && base.Reference == null)
            {
                return false;
            }
            if (_staticFunc != null)
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

    public WeakFunc(Func<T, TResult> func, bool keepTargetAlive = false)
        : this(func?.Target, func, keepTargetAlive)
    {
    }

    public WeakFunc(object target, Func<T, TResult> func, bool keepTargetAlive = false)
    {
        if (func.GetMethodInfo().IsStatic)
        {
            _staticFunc = func;
            if (target != null)
            {
                base.Reference = new WeakReference(target);
            }
        }
        else
        {
            base.Method = func.GetMethodInfo();
            base.FuncReference = new WeakReference(func.Target);
            base.LiveReference = (keepTargetAlive ? func.Target : null);
            base.Reference = new WeakReference(target);
        }
    }

    public new TResult Execute()
    {
        return Execute(default(T));
    }

    public TResult Execute(T parameter)
    {
        if (_staticFunc != null)
        {
            return _staticFunc(parameter);
        }
        object funcTarget = base.FuncTarget;
        if (IsAlive && base.Method != null && (base.LiveReference != null || base.FuncReference != null) && funcTarget != null)
        {
            return (TResult)base.Method.Invoke(funcTarget, new object[1] { parameter });
        }
        return default;
    }

    public object ExecuteWithObject(object parameter)
    {
        return Execute((T)parameter);
    }

    public new void MarkForDeletion()
    {
        _staticFunc = null;
        base.MarkForDeletion();
    }
}
public class WeakFunc<TResult>
{
    private Func<TResult> _staticFunc;

    protected MethodInfo Method { get; set; }

    public bool IsStatic => _staticFunc != null;

    public virtual string MethodName
    {
        get
        {
            if (_staticFunc != null)
            {
                return _staticFunc.GetMethodInfo().Name;
            }
            return Method.Name;
        }
    }

    protected WeakReference FuncReference { get; set; }

    protected object LiveReference { get; set; }

    protected WeakReference Reference { get; set; }

    public virtual bool IsAlive
    {
        get
        {
            if (_staticFunc == null && Reference == null && LiveReference == null)
            {
                return false;
            }
            if (_staticFunc != null)
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

    protected object FuncTarget
    {
        get
        {
            if (LiveReference != null)
            {
                return LiveReference;
            }
            if (FuncReference == null)
            {
                return null;
            }
            return FuncReference.Target;
        }
    }

    protected WeakFunc()
    {
    }

    public WeakFunc(Func<TResult> func, bool keepTargetAlive = false)
        : this(func?.Target, func, keepTargetAlive)
    {
    }

    public WeakFunc(object target, Func<TResult> func, bool keepTargetAlive = false)
    {
        if (func.GetMethodInfo().IsStatic)
        {
            _staticFunc = func;
            if (target != null)
            {
                Reference = new WeakReference(target);
            }
        }
        else
        {
            Method = func.GetMethodInfo();
            FuncReference = new WeakReference(func.Target);
            LiveReference = (keepTargetAlive ? func.Target : null);
            Reference = new WeakReference(target);
        }
    }

    public TResult Execute()
    {
        if (_staticFunc != null)
        {
            return _staticFunc();
        }
        object funcTarget = FuncTarget;
        if (IsAlive && Method != null && (LiveReference != null || FuncReference != null) && funcTarget != null)
        {
            return (TResult)Method.Invoke(funcTarget, null);
        }
        return default;
    }

    public void MarkForDeletion()
    {
        Reference = null;
        FuncReference = null;
        LiveReference = null;
        Method = null;
        _staticFunc = null;
    }
}
