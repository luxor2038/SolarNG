namespace GalaSoft.MvvmLight.Messaging;

public class MessageBase
{
    public object Sender { get; protected set; }

    public object Target { get; protected set; }

    public MessageBase()
    {
    }

    public MessageBase(object sender)
    {
        Sender = sender;
    }

    public MessageBase(object sender, object target)
        : this(sender)
    {
        Target = target;
    }
}
