using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using GalaSoft.MvvmLight.Helpers;

namespace GalaSoft.MvvmLight.Messaging;

public class Messenger : IMessenger
{
    private struct WeakActionAndToken
    {
        public WeakAction Action;

        public object Token;
    }

    private static readonly object CreationLock = new object();

    private static IMessenger _defaultInstance;

    private readonly object _registerLock = new object();

    private Dictionary<Type, List<WeakActionAndToken>> _recipientsOfSubclassesAction;

    private Dictionary<Type, List<WeakActionAndToken>> _recipientsStrictAction;

    private readonly SynchronizationContext _context = SynchronizationContext.Current;

    private bool _isCleanupRegistered;

    public static IMessenger Default
    {
        get
        {
            if (_defaultInstance == null)
            {
                lock (CreationLock)
                {
                    _defaultInstance ??= new Messenger();
                }
            }
            return _defaultInstance;
        }
    }

    public virtual void Register<TMessage>(object recipient, Action<TMessage> action, bool keepTargetAlive = false)
    {
        Register(recipient, null, receiveDerivedMessagesToo: false, action, keepTargetAlive);
    }

    public virtual void Register<TMessage>(object recipient, object token, Action<TMessage> action, bool keepTargetAlive = false)
    {
        Register(recipient, token, receiveDerivedMessagesToo: false, action, keepTargetAlive);
    }

    public virtual void Register<TMessage>(object recipient, object token, bool receiveDerivedMessagesToo, Action<TMessage> action, bool keepTargetAlive = false)
    {
        lock (_registerLock)
        {
            Type typeFromHandle = typeof(TMessage);
            Dictionary<Type, List<WeakActionAndToken>> dictionary;
            if (receiveDerivedMessagesToo)
            {
                _recipientsOfSubclassesAction ??= new Dictionary<Type, List<WeakActionAndToken>>();
                dictionary = _recipientsOfSubclassesAction;
            }
            else
            {
                _recipientsStrictAction ??= new Dictionary<Type, List<WeakActionAndToken>>();
                dictionary = _recipientsStrictAction;
            }
            lock (dictionary)
            {
                List<WeakActionAndToken> list;
                if (!dictionary.ContainsKey(typeFromHandle))
                {
                    list = new List<WeakActionAndToken>();
                    dictionary.Add(typeFromHandle, list);
                }
                else
                {
                    list = dictionary[typeFromHandle];
                }
                WeakAction<TMessage> action2 = new WeakAction<TMessage>(recipient, action, keepTargetAlive);
                WeakActionAndToken item = default;
                item.Action = action2;
                item.Token = token;
                list.Add(item);
            }
        }
        RequestCleanup();
    }

    public virtual void Register<TMessage>(object recipient, bool receiveDerivedMessagesToo, Action<TMessage> action, bool keepTargetAlive = false)
    {
        Register(recipient, null, receiveDerivedMessagesToo, action, keepTargetAlive);
    }

    public virtual void Send<TMessage>(TMessage message)
    {
        SendToTargetOrType(message, null, null);
    }

    public virtual void Send<TMessage, TTarget>(TMessage message)
    {
        SendToTargetOrType(message, typeof(TTarget), null);
    }

    public virtual void Send<TMessage>(TMessage message, object token)
    {
        SendToTargetOrType(message, null, token);
    }

    public virtual void Unregister(object recipient)
    {
        UnregisterFromLists(recipient, _recipientsOfSubclassesAction);
        UnregisterFromLists(recipient, _recipientsStrictAction);
    }

    public virtual void Unregister<TMessage>(object recipient)
    {
        Unregister<TMessage>(recipient, null, null);
    }

    public virtual void Unregister<TMessage>(object recipient, object token)
    {
        Unregister<TMessage>(recipient, token, null);
    }

    public virtual void Unregister<TMessage>(object recipient, Action<TMessage> action)
    {
        Unregister(recipient, null, action);
    }

    public virtual void Unregister<TMessage>(object recipient, object token, Action<TMessage> action)
    {
        UnregisterFromLists(recipient, token, action, _recipientsStrictAction);
        UnregisterFromLists(recipient, token, action, _recipientsOfSubclassesAction);
        RequestCleanup();
    }

    public static void OverrideDefault(IMessenger newMessenger)
    {
        _defaultInstance = newMessenger;
    }

    public static void Reset()
    {
        _defaultInstance = null;
    }

    public void ResetAll()
    {
        Reset();
    }

    private static void CleanupList(IDictionary<Type, List<WeakActionAndToken>> lists)
    {
        if (lists == null)
        {
            return;
        }
        lock (lists)
        {
            List<Type> list = new List<Type>();
            foreach (KeyValuePair<Type, List<WeakActionAndToken>> list2 in lists)
            {
                foreach (WeakActionAndToken item in list2.Value.Where((WeakActionAndToken item) => item.Action == null || !item.Action.IsAlive).ToList())
                {
                    list2.Value.Remove(item);
                }
                if (list2.Value.Count == 0)
                {
                    list.Add(list2.Key);
                }
            }
            foreach (Type item2 in list)
            {
                lists.Remove(item2);
            }
        }
    }

    private static void SendToList<TMessage>(TMessage message, IEnumerable<WeakActionAndToken> weakActionsAndTokens, Type messageTargetType, object token)
    {
        if (weakActionsAndTokens == null)
        {
            return;
        }
        List<WeakActionAndToken> source = weakActionsAndTokens.ToList();
        foreach (WeakActionAndToken item in source.Take(source.Count()).ToList())
        {
            if (item.Action is IExecuteWithObject executeWithObject && item.Action.IsAlive && item.Action.Target != null && (messageTargetType == null || item.Action.Target.GetType() == messageTargetType || messageTargetType.GetTypeInfo().IsAssignableFrom(item.Action.Target.GetType().GetTypeInfo())) && ((item.Token == null && token == null) || (item.Token != null && item.Token.Equals(token))))
            {
                executeWithObject.ExecuteWithObject(message);
            }
        }
    }

    private static void UnregisterFromLists(object recipient, Dictionary<Type, List<WeakActionAndToken>> lists)
    {
        if (recipient == null || lists == null || lists.Count == 0)
        {
            return;
        }
        lock (lists)
        {
            foreach (Type key in lists.Keys)
            {
                foreach (WeakActionAndToken item in lists[key])
                {
                    IExecuteWithObject executeWithObject = (IExecuteWithObject)item.Action;
                    if (executeWithObject != null && recipient == executeWithObject.Target)
                    {
                        executeWithObject.MarkForDeletion();
                    }
                }
            }
        }
    }

    private static void UnregisterFromLists<TMessage>(object recipient, object token, Action<TMessage> action, Dictionary<Type, List<WeakActionAndToken>> lists)
    {
        Type typeFromHandle = typeof(TMessage);
        if (recipient == null || lists == null || lists.Count == 0 || !lists.ContainsKey(typeFromHandle))
        {
            return;
        }
        lock (lists)
        {
            foreach (WeakActionAndToken item in lists[typeFromHandle])
            {
                if (item.Action is WeakAction<TMessage> weakAction && recipient == weakAction.Target && (action == null || action.GetMethodInfo().Name == weakAction.MethodName) && (token == null || token.Equals(item.Token)))
                {
                    item.Action.MarkForDeletion();
                }
            }
        }
    }

    public void RequestCleanup()
    {
        if (_isCleanupRegistered)
        {
            return;
        }
        Action cleanupAction = Cleanup;
        if (_context != null)
        {
            _context.Post(delegate
            {
                cleanupAction();
            }, null);
        }
        else
        {
            cleanupAction();
        }
        _isCleanupRegistered = true;
    }

    public void Cleanup()
    {
        CleanupList(_recipientsOfSubclassesAction);
        CleanupList(_recipientsStrictAction);
        _isCleanupRegistered = false;
    }

    private void SendToTargetOrType<TMessage>(TMessage message, Type messageTargetType, object token)
    {
        Type typeFromHandle = typeof(TMessage);
        if (_recipientsOfSubclassesAction != null)
        {
            foreach (Type item in _recipientsOfSubclassesAction.Keys.Take(_recipientsOfSubclassesAction.Count()).ToList())
            {
                List<WeakActionAndToken> weakActionsAndTokens = null;
                if (typeFromHandle == item || typeFromHandle.GetTypeInfo().IsSubclassOf(item) || item.GetTypeInfo().IsAssignableFrom(typeFromHandle.GetTypeInfo()))
                {
                    lock (_recipientsOfSubclassesAction)
                    {
                        weakActionsAndTokens = _recipientsOfSubclassesAction[item].Take(_recipientsOfSubclassesAction[item].Count()).ToList();
                    }
                }
                SendToList(message, weakActionsAndTokens, messageTargetType, token);
            }
        }
        if (_recipientsStrictAction != null)
        {
            List<WeakActionAndToken> list = null;
            lock (_recipientsStrictAction)
            {
                if (_recipientsStrictAction.ContainsKey(typeFromHandle))
                {
                    list = _recipientsStrictAction[typeFromHandle].Take(_recipientsStrictAction[typeFromHandle].Count()).ToList();
                }
            }
            if (list != null)
            {
                SendToList(message, list, messageTargetType, token);
            }
        }
        RequestCleanup();
    }
}
