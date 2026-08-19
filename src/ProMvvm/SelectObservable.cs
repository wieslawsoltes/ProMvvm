namespace ProMvvm;

internal sealed class SelectObservable<TSource, TResult>(
    IObservable<TSource> source,
    Func<TSource, TResult> selector) : IObservable<TResult>
{
    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        var sink = new Sink(observer, selector);
        sink.Connect(source);
        return sink;
    }

    private sealed class Sink(
        IObserver<TResult> observer,
        Func<TSource, TResult> selector) : IObserver<TSource>, IDisposable
    {
        private readonly object _gate = new();
        private IDisposable? _subscription;
        private bool _stopped;

        public void Connect(IObservable<TSource> input)
        {
            IDisposable current;
            try
            {
                current = input.Subscribe(this);
            }
            catch
            {
                Dispose();
                throw;
            }

            lock (_gate)
            {
                if (_stopped)
                {
                    current.Dispose();
                }
                else
                {
                    _subscription = current;
                }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                Stop();
            }
        }

        public void OnCompleted()
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                Stop();
                observer.OnCompleted();
            }
        }

        public void OnError(Exception error)
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

        public void OnNext(TSource value)
        {
            lock (_gate)
            {
                if (_stopped)
                {
                    return;
                }

                TResult result;
                try
                {
                    result = selector(value);
                }
                catch (Exception error)
                {
                    Stop();
                    observer.OnError(error);
                    return;
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
        }

        private void Stop()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
