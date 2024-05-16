using System;

namespace GalaSoft.MvvmLight.Messaging;

public class NotificationMessageWithCallback : NotificationMessage
{
    private readonly Delegate _callback;

    public NotificationMessageWithCallback(string notification, Delegate callback)
        : base(notification)
    {
        CheckCallback(callback);
        _callback = callback;
    }

    public NotificationMessageWithCallback(object sender, string notification, Delegate callback)
        : base(sender, notification)
    {
        CheckCallback(callback);
        _callback = callback;
    }

    public NotificationMessageWithCallback(object sender, object target, string notification, Delegate callback)
        : base(sender, target, notification)
    {
        CheckCallback(callback);
        _callback = callback;
    }

    public virtual object Execute(params object[] arguments)
    {
        return _callback.DynamicInvoke(arguments);
    }

    private static void CheckCallback(Delegate callback)
    {
        if (callback is null)
        {
            throw new ArgumentNullException("callback", "Callback may not be null");
        }
    }
}
