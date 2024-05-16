namespace GalaSoft.MvvmLight.Helpers;

public interface IExecuteWithObject
{
    object Target { get; }

    void ExecuteWithObject(object parameter);

    void MarkForDeletion();
}
