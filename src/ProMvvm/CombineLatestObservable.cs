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
                var firstSubscription = first.Subscribe(new SourceObserver1(this));
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
                    var secondSubscription = second.Subscribe(new SourceObserver2(this));
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
                Stop();
            }
        }

        private void Next1(T1 value)
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                _value1 = value;
                _hasValue1 = true;
                Publish();
            }
        }

        private void Next2(T2 value)
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                _value2 = value;
                _hasValue2 = true;
                Publish();
            }
        }

        private void Publish()
        {
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

            if (isDistinct)
            {
                if (_hasLastResult && comparer.Equals(_lastResult!, result))
                {
                    return;
                }

                _lastResult = result;
                _hasLastResult = true;
            }

            try
            {
                observer.OnNext(result);
            }
            catch
            {
                Stop();
                throw;
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

                Stop();
                observer.OnError(error);
            }
        }

        private void Stop()
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

        private sealed class SourceObserver1(Subscription owner) : IObserver<T1>
        {
            public void OnCompleted()
            {
            }

            public void OnError(Exception error) => owner.Error(error);

            public void OnNext(T1 value) => owner.Next1(value);
        }

        private sealed class SourceObserver2(Subscription owner) : IObserver<T2>
        {
            public void OnCompleted()
            {
            }

            public void OnError(Exception error) => owner.Error(error);

            public void OnNext(T2 value) => owner.Next2(value);
        }
    }
}
