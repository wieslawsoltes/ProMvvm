namespace ProMvvm;

internal sealed class CombineLatestObservable<T1, T2, TResult>(
    IObservable<T1> source1,
    IObservable<T2> source2,
    Func<T1, T2, TResult> selector,
    bool isDistinct,
    IEqualityComparer<TResult> comparer) : IObservable<TResult>
{
    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        var subscription = new Subscription(observer, selector, isDistinct, comparer);
        subscription.Connect(source1, source2);
        return subscription;
    }

    private sealed class Subscription(
        IObserver<TResult> observer,
        Func<T1, T2, TResult> selector,
        bool isDistinct,
        IEqualityComparer<TResult> comparer) : IDisposable
    {
        private readonly object _gate = new();
        private IDisposable? _subscription1;
        private IDisposable? _subscription2;
        private T1? _value1;
        private T2? _value2;
        private TResult? _lastResult;
        private bool _hasValue1;
        private bool _hasValue2;
        private bool _hasLastResult;
        private bool _stopped;

        public void Connect(IObservable<T1> first, IObservable<T2> second)
        {
            try
            {
                var firstSubscription = first.Subscribe(new SourceObserver<T1>(this, 1));
                lock (_gate)
                {
                    if (_stopped)
                    {
                        firstSubscription.Dispose();
                    }
                    else
                    {
                        _subscription1 = firstSubscription;
                    }
                }

                if (!_stopped)
                {
                    var secondSubscription = second.Subscribe(new SourceObserver<T2>(this, 2));
                    lock (_gate)
                    {
                        if (_stopped)
                        {
                            secondSubscription.Dispose();
                        }
                        else
                        {
                            _subscription2 = secondSubscription;
                        }
                    }
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                _stopped = true;
                _subscription1?.Dispose();
                _subscription2?.Dispose();
                _subscription1 = null;
                _subscription2 = null;
            }
        }

        public void Next<T>(int sourceIndex, T value)
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                if (sourceIndex == 1)
                {
                    _value1 = (T1?)(object?)value;
                    _hasValue1 = true;
                }
                else
                {
                    _value2 = (T2?)(object?)value;
                    _hasValue2 = true;
                }

                if (!_hasValue1 || !_hasValue2)
                {
                    return;
                }

                TResult result;
                try
                {
                    result = selector(_value1!, _value2!);
                }
                catch (Exception error)
                {
                    Error(error);
                    return;
                }

                if (isDistinct && _hasLastResult && comparer.Equals(_lastResult!, result))
                {
                    return;
                }

                _lastResult = result;
                _hasLastResult = true;
                observer.OnNext(result);
            }
        }

        public void Error(Exception error)
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                _stopped = true;
                _subscription1?.Dispose();
                _subscription2?.Dispose();
                observer.OnError(error);
            }
        }

        private sealed class SourceObserver<T>(Subscription owner, int sourceIndex) : IObserver<T>
        {
            public void OnCompleted()
            {
            }

            public void OnError(Exception error) => owner.Error(error);

            public void OnNext(T value) => owner.Next(sourceIndex, value);
        }
    }
}
