using System.ComponentModel;

namespace ProMvvm;

internal sealed class TwoSegmentPropertyObservable<TSource, TIntermediate, TValue>(
    TSource source,
    string propertyName1,
    Func<TSource, TIntermediate> getter1,
    string propertyName2,
    Func<TIntermediate, TValue> getter2,
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
            observer,
            isDistinct,
            comparer);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly object _gate = new();
        private readonly TSource _source;
        private readonly string _propertyName1;
        private readonly Func<TSource, TIntermediate> _getter1;
        private readonly string _propertyName2;
        private readonly Func<TIntermediate, TValue> _getter2;
        private readonly IObserver<TValue> _observer;
        private readonly IEqualityComparer<TValue> _comparer;
        private readonly INotifyPropertyChanged? _rootNotifications;
        private readonly PropertyChangedEventHandler? _rootHandler;
        private readonly PropertyChangedEventHandler _childHandler;
        private INotifyPropertyChanged? _childNotifications;
        private TIntermediate? _intermediate;
        private TValue? _lastValue;
        private bool _hasIntermediate;
        private bool _hasLastValue;
        private bool _stopped;

        public Subscription(
            TSource source,
            string propertyName1,
            Func<TSource, TIntermediate> getter1,
            string propertyName2,
            Func<TIntermediate, TValue> getter2,
            IObserver<TValue> observer,
            bool isDistinct,
            IEqualityComparer<TValue> comparer)
        {
            _source = source;
            _propertyName1 = propertyName1;
            _getter1 = getter1;
            _propertyName2 = propertyName2;
            _getter2 = getter2;
            _observer = observer;
            _comparer = comparer;
            IsDistinct = isDistinct;
            _rootNotifications = source as INotifyPropertyChanged;
            _childHandler = HandleChildChanged;

            if (_rootNotifications is not null)
            {
                _rootHandler = HandleRootChanged;
                _rootNotifications.PropertyChanged += _rootHandler;
            }

            lock (_gate)
            {
                RewireAndPublish();
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

                RewireAndPublish();
            }
        }

        private void HandleChildChanged(object? sender, PropertyChangedEventArgs args)
        {
            lock (_gate)
            {
                if (_stopped || !Matches(args.PropertyName, _propertyName2))
                {
                    return;
                }

                PublishLeaf();
            }
        }

        private void RewireAndPublish()
        {
            DetachChild();
            try
            {
                _intermediate = _getter1(_source);
                _hasIntermediate = _intermediate is not null;
                if (!_hasIntermediate)
                {
                    return;
                }

                _childNotifications = (object?)_intermediate as INotifyPropertyChanged;
                if (_childNotifications is not null)
                {
                    _childNotifications.PropertyChanged += _childHandler;
                }

                PublishLeaf();
            }
            catch (Exception error) when (!_stopped)
            {
                Stop();
                _observer.OnError(error);
            }
        }

        private void PublishLeaf()
        {
            try
            {
                var value = _getter2(_intermediate!);
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

        private static bool Matches(string? changedPropertyName, string propertyName) =>
            string.IsNullOrEmpty(changedPropertyName) ||
            string.Equals(changedPropertyName, propertyName, StringComparison.Ordinal);

        private void DetachChild()
        {
            if (_childNotifications is not null)
            {
                _childNotifications.PropertyChanged -= _childHandler;
                _childNotifications = null;
            }

            _intermediate = default;
            _hasIntermediate = false;
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

            DetachChild();
        }
    }
}
