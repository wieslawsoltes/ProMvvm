using System.ComponentModel;

namespace ProMvvm;

internal sealed class ThreeSegmentPropertyObservable<TSource, TIntermediate1, TIntermediate2, TValue>(
    TSource source,
    string propertyName1,
    Func<TSource, TIntermediate1> getter1,
    string propertyName2,
    Func<TIntermediate1, TIntermediate2> getter2,
    string propertyName3,
    Func<TIntermediate2, TValue> getter3,
    bool isDistinct,
    IEqualityComparer<TValue> comparer) : IObservable<TValue>
    where TSource : class
{
    public IDisposable Subscribe(IObserver<TValue> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return new Subscription(
            source,
            propertyName1,
            getter1,
            propertyName2,
            getter2,
            propertyName3,
            getter3,
            observer,
            isDistinct,
            comparer);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly object _gate = new();
        private readonly TSource _source;
        private readonly string _propertyName1;
        private readonly Func<TSource, TIntermediate1> _getter1;
        private readonly string _propertyName2;
        private readonly Func<TIntermediate1, TIntermediate2> _getter2;
        private readonly string _propertyName3;
        private readonly Func<TIntermediate2, TValue> _getter3;
        private readonly IObserver<TValue> _observer;
        private readonly IEqualityComparer<TValue> _comparer;
        private readonly INotifyPropertyChanged? _rootNotifications;
        private readonly PropertyChangedEventHandler? _rootHandler;
        private readonly PropertyChangedEventHandler _firstHandler;
        private readonly PropertyChangedEventHandler _secondHandler;
        private INotifyPropertyChanged? _firstNotifications;
        private INotifyPropertyChanged? _secondNotifications;
        private TIntermediate1? _intermediate1;
        private TIntermediate2? _intermediate2;
        private TValue? _lastValue;
        private bool _hasLastValue;
        private bool _stopped;

        public Subscription(
            TSource source,
            string propertyName1,
            Func<TSource, TIntermediate1> getter1,
            string propertyName2,
            Func<TIntermediate1, TIntermediate2> getter2,
            string propertyName3,
            Func<TIntermediate2, TValue> getter3,
            IObserver<TValue> observer,
            bool isDistinct,
            IEqualityComparer<TValue> comparer)
        {
            _source = source;
            _propertyName1 = propertyName1;
            _getter1 = getter1;
            _propertyName2 = propertyName2;
            _getter2 = getter2;
            _propertyName3 = propertyName3;
            _getter3 = getter3;
            _observer = observer;
            _comparer = comparer;
            IsDistinct = isDistinct;
            _rootNotifications = source as INotifyPropertyChanged;
            _firstHandler = HandleFirstChanged;
            _secondHandler = HandleSecondChanged;

            if (_rootNotifications is not null)
            {
                _rootHandler = HandleRootChanged;
                _rootNotifications.PropertyChanged += _rootHandler;
            }

            lock (_gate)
            {
                RewireFromRootAndPublish();
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

        private void HandleRootChanged(object? sender, PropertyChangedEventArgs args)
        {
            lock (_gate)
            {
                if (_stopped || !Matches(args.PropertyName, _propertyName1))
                {
                    return;
                }

                RewireFromRootAndPublish();
            }
        }

        private void HandleFirstChanged(object? sender, PropertyChangedEventArgs args)
        {
            lock (_gate)
            {
                if (_stopped || !Matches(args.PropertyName, _propertyName2))
                {
                    return;
                }

                RewireFromFirstAndPublish();
            }
        }

        private void HandleSecondChanged(object? sender, PropertyChangedEventArgs args)
        {
            lock (_gate)
            {
                if (_stopped || !Matches(args.PropertyName, _propertyName3))
                {
                    return;
                }

                TryPublishLeaf();
            }
        }

        private void RewireFromRootAndPublish()
        {
            DetachFirst();
            try
            {
                _intermediate1 = _getter1(_source);
                if (_intermediate1 is null)
                {
                    return;
                }

                _firstNotifications = (object?)_intermediate1 as INotifyPropertyChanged;
                if (_firstNotifications is not null)
                {
                    _firstNotifications.PropertyChanged += _firstHandler;
                }

                RewireFromFirstCoreAndPublish();
            }
            catch (Exception error) when (!_stopped)
            {
                Stop();
                _observer.OnError(error);
            }
        }

        private void RewireFromFirstAndPublish()
        {
            try
            {
                RewireFromFirstCoreAndPublish();
            }
            catch (Exception error) when (!_stopped)
            {
                Stop();
                _observer.OnError(error);
            }
        }

        private void RewireFromFirstCoreAndPublish()
        {
            DetachSecond();
            _intermediate2 = _getter2(_intermediate1!);
            if (_intermediate2 is null)
            {
                return;
            }

            _secondNotifications = (object?)_intermediate2 as INotifyPropertyChanged;
            if (_secondNotifications is not null)
            {
                _secondNotifications.PropertyChanged += _secondHandler;
            }

            PublishLeafCore();
        }

        private void TryPublishLeaf()
        {
            try
            {
                PublishLeafCore();
            }
            catch (Exception error) when (!_stopped)
            {
                Stop();
                _observer.OnError(error);
            }
        }

        private void PublishLeafCore()
        {
            var value = _getter3(_intermediate2!);
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

        private static bool Matches(string? changedPropertyName, string propertyName) =>
            string.IsNullOrEmpty(changedPropertyName) ||
            string.Equals(changedPropertyName, propertyName, StringComparison.Ordinal);

        private void DetachFirst()
        {
            DetachSecond();
            if (_firstNotifications is not null)
            {
                _firstNotifications.PropertyChanged -= _firstHandler;
                _firstNotifications = null;
            }

            _intermediate1 = default;
        }

        private void DetachSecond()
        {
            if (_secondNotifications is not null)
            {
                _secondNotifications.PropertyChanged -= _secondHandler;
                _secondNotifications = null;
            }

            _intermediate2 = default;
        }

        private void Stop()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            if (_rootNotifications is not null)
            {
                _rootNotifications.PropertyChanged -= _rootHandler;
            }

            DetachFirst();
        }
    }
}
