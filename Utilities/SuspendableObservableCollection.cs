using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SolarNG.Utilities;

public class SuspendableObservableCollection<T> : ObservableCollection<T>
{
    private bool _isNotificationSuspended;

    public SuspendableObservableCollection() : base() { }

    public SuspendableObservableCollection(System.Collections.Generic.IEnumerable<T> collection) : base(collection) { }

    public SuspendableObservableCollection(System.Collections.Generic.List<T> list) : base(list) { }

    public void SuspendNotifications()
    {
        _isNotificationSuspended = true;
    }

    public void ResumeNotifications()
    {
        _isNotificationSuspended = false;
    }

    public void TriggerNotifications()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_isNotificationSuspended)
        {
            base.OnCollectionChanged(e);
        }
    }

    public IDisposable SuppressNotifications()
    {
        SuspendNotifications();
        return new NotificationSuspender(this);
    }

    private class NotificationSuspender : IDisposable
    {
        private readonly SuspendableObservableCollection<T> _collection;

        public NotificationSuspender(SuspendableObservableCollection<T> collection)
        {
            _collection = collection;
        }

        public void Dispose()
        {
            _collection.ResumeNotifications();
        }
    }
}