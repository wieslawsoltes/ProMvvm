namespace ProMvvm.Tests;

public sealed class SelectObservableTests
{
    [Fact]
    public void ForwardsValuesCompletionAndErrorsOnce()
    {
        var source = new ControlledObservable<int>();
        var observer = new RecordingObserver<string>();
        using var subscription = new SelectObservable<int, string>(
                source,
                static value => value.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Subscribe(observer);

        source.Next(1);
        source.Complete();
        source.Complete();
        source.Next(2);
        source.Error(new InvalidOperationException("late"));

        Assert.Equal(["1"], observer.Values);
        Assert.True(observer.IsCompleted);
        Assert.Null(observer.ErrorValue);
        Assert.True(source.IsDisposed);
    }

    [Fact]
    public void ForwardsSourceErrorAndDisposes()
    {
        var source = new ControlledObservable<int>();
        var observer = new RecordingObserver<int>();
        using var subscription = new SelectObservable<int, int>(source, static value => value)
            .Subscribe(observer);
        var error = new InvalidOperationException("source");

        source.Error(error);

        Assert.Same(error, observer.ErrorValue);
        Assert.False(observer.IsCompleted);
        Assert.True(source.IsDisposed);
    }

    [Fact]
    public void DisposesAndRethrowsWhenSourceSubscribeThrows()
    {
        var observable = new SelectObservable<int, int>(
            new ThrowingObservable<int>(),
            static value => value);

        Assert.Throws<TestSubscribeException>(() => observable.Subscribe(new RecordingObserver<int>()));
    }

    [Fact]
    public void ValidatesObserverAndDisposalIsIdempotent()
    {
        var source = new ControlledObservable<int>();
        var observable = new SelectObservable<int, int>(source, static value => value);

        Assert.Throws<ArgumentNullException>(() => observable.Subscribe(null!));
        var subscription = observable.Subscribe(new RecordingObserver<int>());
        subscription.Dispose();
        subscription.Dispose();
        source.Next(1);

        Assert.True(source.IsDisposed);
    }

    [Fact]
    public void SelectorFailureStopsAndReportsError()
    {
        var source = new ControlledObservable<int>();
        var observer = new RecordingObserver<int>();
        using var subscription = new SelectObservable<int, int>(
                source,
                static _ => throw new InvalidOperationException("selector"))
            .Subscribe(observer);

        source.Next(1);
        source.Next(2);

        Assert.IsType<InvalidOperationException>(observer.ErrorValue);
        Assert.True(source.IsDisposed);
    }

    [Fact]
    public void ObserverFailureStopsDisposesAndRethrows()
    {
        var source = new ControlledObservable<int>();
        using var subscription = new SelectObservable<int, int>(source, static value => value)
            .Subscribe(new ThrowingObserver<int>());

        Assert.Throws<TestObserverException>(() => source.Next(1));
        source.Next(2);

        Assert.True(source.IsDisposed);
    }

    [Fact]
    public void SynchronousSelectorFailureDisposesReturnedSubscription()
    {
        var source = new SynchronousObservable<int>(1);
        var observer = new RecordingObserver<int>();

        using var subscription = new SelectObservable<int, int>(
                source,
                static _ => throw new InvalidOperationException("selector"))
            .Subscribe(observer);

        Assert.IsType<InvalidOperationException>(observer.ErrorValue);
        Assert.True(source.IsDisposed);
    }

    private sealed class ControlledObservable<T> : IObservable<T>
    {
        private IObserver<T>? _observer;

        public bool IsDisposed { get; private set; }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observer = observer;
            return new CallbackDisposable(() => IsDisposed = true);
        }

        public void Next(T value) => _observer?.OnNext(value);

        public void Error(Exception error) => _observer?.OnError(error);

        public void Complete() => _observer?.OnCompleted();
    }

    private sealed class ThrowingObservable<T> : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer) => throw new TestSubscribeException();
    }

    private sealed class SynchronousObservable<T>(T value) : IObservable<T>
    {
        public bool IsDisposed { get; private set; }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(value);
            return new CallbackDisposable(() => IsDisposed = true);
        }
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        public void Dispose() => callback();
    }

    private sealed class RecordingObserver<T> : IObserver<T>
    {
        public List<T> Values { get; } = [];

        public Exception? ErrorValue { get; private set; }

        public bool IsCompleted { get; private set; }

        public void OnCompleted() => IsCompleted = true;

        public void OnError(Exception error) => ErrorValue = error;

        public void OnNext(T value) => Values.Add(value);
    }

    private sealed class TestSubscribeException : Exception;
}
