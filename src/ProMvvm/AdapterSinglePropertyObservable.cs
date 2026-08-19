namespace ProMvvm;

internal sealed class AdapterSinglePropertyObservable<TSource, TValue>(
    TSource source,
    string propertyName,
    Func<TSource, TValue> getter,
    IPropertyNotificationAdapter adapter,
    bool isDistinct,
    IEqualityComparer<TValue> comparer) : IObservable<TValue>
    where TSource : class
{
    public IDisposable Subscribe(IObserver<TValue> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return new Subscription(source, propertyName, getter, adapter, observer, isDistinct, comparer);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly object _gate = new();
        private readonly TSource _source;
        private readonly string _propertyName;
        private readonly Func<TSource, TValue> _getter;
        private readonly IObserver<TValue> _observer;
        private readonly IEqualityComparer<TValue> _comparer;
        private IDisposable? _adapterSubscription;
        private TValue? _lastValue;
        private bool _hasLastValue;
        private bool _stopped;
        private bool _initializing = true;

        public Subscription(
            TSource source,
            string propertyName,
            Func<TSource, TValue> getter,
            IPropertyNotificationAdapter adapter,
            IObserver<TValue> observer,
            bool isDistinct,
            IEqualityComparer<TValue> comparer)
        {
            _source = source;
            _propertyName = propertyName;
            _getter = getter;
            _observer = observer;
            _comparer = comparer;
            IsDistinct = isDistinct;
            _adapterSubscription = adapter.Subscribe(source, HandlePropertyChanged);
            _initializing = false;

            lock (_gate)
            {
                Publish();
            }
        }

        private bool IsDistinct { get; }

        public void Dispose()
        {
            lock (_gate)
            {
                Stop();
            }
        }

        private void HandlePropertyChanged(string? changedPropertyName)
        {
            lock (_gate)
            {
                if (_initializing ||
                    _stopped ||
                    (!string.IsNullOrEmpty(changedPropertyName) &&
                     !string.Equals(changedPropertyName, _propertyName, StringComparison.Ordinal)))
                {
                    return;
                }

                Publish();
            }
        }

        private void Publish()
        {
            try
            {
                var value = _getter(_source);
                if (IsDistinct && _hasLastValue && _comparer.Equals(_lastValue!, value))
                {
                    return;
                }

                _lastValue = value;
                _hasLastValue = true;
                try
                {
                    _observer.OnNext(value);
                }
                catch
                {
                    Stop();
                    throw;
                }
            }
            catch (Exception error) when (!_stopped)
            {
                Stop();
                _observer.OnError(error);
            }
        }

        private void Stop()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            _adapterSubscription?.Dispose();
            _adapterSubscription = null;
        }
    }
}
