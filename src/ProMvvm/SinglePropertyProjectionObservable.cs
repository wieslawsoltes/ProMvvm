using System.ComponentModel;

namespace ProMvvm;

internal sealed class SinglePropertyProjectionObservable<TSource, TValue, TResult>(
    TSource source,
    string propertyName,
    Func<TSource, TValue> getter,
    Func<TValue, TResult> selector,
    bool isDistinct,
    IEqualityComparer<TValue> comparer) : IObservable<TResult>
    where TSource : class
{
    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return new Subscription(
            source,
            propertyName,
            getter,
            selector,
            observer,
            isDistinct,
            comparer);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly object _gate = new();
        private readonly TSource _source;
        private readonly string _propertyName;
        private readonly Func<TSource, TValue> _getter;
        private readonly Func<TValue, TResult> _selector;
        private readonly IObserver<TResult> _observer;
        private readonly IEqualityComparer<TValue> _comparer;
        private readonly INotifyPropertyChanged? _notifications;
        private readonly PropertyChangedEventHandler? _handler;
        private TValue? _lastValue;
        private bool _hasLastValue;
        private bool _stopped;

        public Subscription(
            TSource source,
            string propertyName,
            Func<TSource, TValue> getter,
            Func<TValue, TResult> selector,
            IObserver<TResult> observer,
            bool isDistinct,
            IEqualityComparer<TValue> comparer)
        {
            _source = source;
            _propertyName = propertyName;
            _getter = getter;
            _selector = selector;
            _observer = observer;
            IsDistinct = isDistinct;
            _comparer = comparer;
            _notifications = source as INotifyPropertyChanged;

            if (_notifications is not null)
            {
                _handler = HandlePropertyChanged;
                _notifications.PropertyChanged += _handler;
            }

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

        private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            lock (_gate)
            {
                if (_stopped ||
                    (!string.IsNullOrEmpty(args.PropertyName) &&
                     !string.Equals(args.PropertyName, _propertyName, StringComparison.Ordinal)))
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
                var result = _selector(value);

                try
                {
                    _observer.OnNext(result);
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
            if (_notifications is not null)
            {
                _notifications.PropertyChanged -= _handler;
            }
        }
    }
}
