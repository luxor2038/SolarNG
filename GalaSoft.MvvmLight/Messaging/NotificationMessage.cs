namespace GalaSoft.MvvmLight.Messaging;

public class NotificationMessage<T> : GenericMessage<T>
{
    public string Notification { get; private set; }

    public NotificationMessage(T content, string notification)
        : base(content)
    {
        Notification = notification;
    }

    public NotificationMessage(object sender, T content, string notification)
        : base(sender, content)
    {
        Notification = notification;
    }

    public NotificationMessage(object sender, object target, T content, string notification)
        : base(sender, target, content)
    {
        Notification = notification;
    }
}
public class NotificationMessage : MessageBase
{
    public string Notification { get; private set; }

    public NotificationMessage(string notification)
    {
        Notification = notification;
    }

    public NotificationMessage(object sender, string notification)
        : base(sender)
    {
        Notification = notification;
    }

    public NotificationMessage(object sender, object target, string notification)
        : base(sender, target)
    {
        Notification = notification;
    }
}
