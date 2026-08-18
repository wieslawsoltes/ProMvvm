using System.ComponentModel;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace ProMvvm;

internal sealed class PropertyPathObservable<TSource, TValue>(
    TSource source,
    PropertyPath<TSource, TValue> path,
    bool isDistinct,
    IEqualityComparer<TValue> comparer) : IObservable<TValue>
    where TSource : class
{
    public IDisposable Subscribe(IObserver<TValue> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return new Subscription(source, path.Segments, observer, isDistinct, comparer);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly object _gate = new();
        private readonly ImmutableArray<IPropertyPathSegment> _segments;
        private readonly IObserver<TValue> _observer;
        private readonly IEqualityComparer<TValue> _comparer;
        private readonly object?[] _values;
        private readonly Watcher[] _watchers;
        private bool _hasLastValue;
        private TValue? _lastValue;
        private bool _stopped;

        public Subscription(
            TSource source,
            ImmutableArray<IPropertyPathSegment> segments,
            IObserver<TValue> observer,
            bool isDistinct,
            IEqualityComparer<TValue> comparer)
        {
            _segments = segments;
            _observer = observer;
            _comparer = comparer;
            IsDistinct = isDistinct;
            _values = new object?[segments.Length + 1];
            _watchers = new Watcher[segments.Length];
            _values[0] = source;

            for (var index = 0; index < _watchers.Length; index++)
            {
                _watchers[index] = new Watcher(this, index);
            }

            lock (_gate)
            {
                RebuildAndPublish(0, attachStart: 0);
            }
        }

        private bool IsDistinct { get; }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                _stopped = true;
                DetachFrom(0);
                Array.Clear(_values, 0, _values.Length);
            }
        }

        private void OnPropertyChanged(int segmentIndex, string? changedPropertyName)
        {
            lock (_gate)
            {
                if (_stopped ||
                    (!string.IsNullOrEmpty(changedPropertyName) &&
                     !string.Equals(
                         changedPropertyName,
                         _segments[segmentIndex].PropertyName,
                         StringComparison.Ordinal)))
                {
                    return;
                }

                RebuildAndPublish(segmentIndex, attachStart: segmentIndex + 1);
            }
        }

        private void RebuildAndPublish(int valueStart, int attachStart)
        {
            DetachFrom(attachStart);

            try
            {
                var valid = true;
                for (var index = valueStart; index < _segments.Length; index++)
                {
                    var parent = _values[index];
                    if (parent is null)
                    {
                        valid = false;
                        ClearValuesFrom(index + 1);
                        break;
                    }

                    if (index >= attachStart)
                    {
                        _watchers[index].Attach(parent as INotifyPropertyChanged);
                    }

                    _values[index + 1] = _segments[index].GetValue(parent);
                }

                if (!valid)
                {
                    return;
                }

                var value = (TValue?)_values[^1];
                if (IsDistinct && _hasLastValue && _comparer.Equals(_lastValue!, value!))
                {
                    return;
                }

                _lastValue = value;
                _hasLastValue = true;

                try
                {
                    _observer.OnNext(value!);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearValuesFrom(int start) =>
            Array.Clear(_values, start, _values.Length - start);

        private void DetachFrom(int start)
        {
            for (var index = start; index < _watchers.Length; index++)
            {
                _watchers[index].Detach();
            }

            ClearValuesFrom(start + 1);
        }

        private void Stop()
        {
            _stopped = true;
            DetachFrom(0);
            Array.Clear(_values, 0, _values.Length);
        }

        private sealed class Watcher
        {
            private readonly Subscription _owner;
            private readonly int _segmentIndex;
            private readonly PropertyChangedEventHandler _handler;
            private INotifyPropertyChanged? _source;

            public Watcher(Subscription owner, int segmentIndex)
            {
                _owner = owner;
                _segmentIndex = segmentIndex;
                _handler = HandlePropertyChanged;
            }

            public void Attach(INotifyPropertyChanged? source)
            {
                _source = source;
                if (source is not null)
                {
                    source.PropertyChanged += _handler;
                }
            }

            public void Detach()
            {
                if (_source is not null)
                {
                    _source.PropertyChanged -= _handler;
                    _source = null;
                }
            }

            private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs args) =>
                _owner.OnPropertyChanged(_segmentIndex, args.PropertyName);
        }
    }
}
