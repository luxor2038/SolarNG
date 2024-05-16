namespace GalaSoft.MvvmLight.Messaging;

public abstract class PropertyChangedMessageBase : MessageBase
{
    public string PropertyName { get; protected set; }

    protected PropertyChangedMessageBase(object sender, string propertyName)
        : base(sender)
    {
        PropertyName = propertyName;
    }

    protected PropertyChangedMessageBase(object sender, object target, string propertyName)
        : base(sender, target)
    {
        PropertyName = propertyName;
    }

    protected PropertyChangedMessageBase(string propertyName)
    {
        PropertyName = propertyName;
    }
}
