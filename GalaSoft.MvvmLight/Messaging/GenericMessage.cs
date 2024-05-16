namespace GalaSoft.MvvmLight.Messaging;

public class GenericMessage<T> : MessageBase
{
    public T Content { get; protected set; }

    public GenericMessage(T content)
    {
        Content = content;
    }

    public GenericMessage(object sender, T content)
        : base(sender)
    {
        Content = content;
    }

    public GenericMessage(object sender, object target, T content)
        : base(sender, target)
    {
        Content = content;
    }
}
