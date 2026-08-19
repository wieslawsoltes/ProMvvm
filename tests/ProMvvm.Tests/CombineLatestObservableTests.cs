namespace ProMvvm.Tests;

public sealed class CombineLatestObservableTests
{
    [Fact]
    public void ValidatesObserver()
    {
        var observable = Create(new ControlledObservable<int>(), new ControlledObservable<int>());

        Assert.Throws<ArgumentNullException>(() => observable.Subscribe(null!));
    }

    [Fact]
    public void DisposesAndRethrowsWhenSourceSubscribeThrows()
    {
        var first = new ControlledObservable<int>();
        var observable = Create(first, new ThrowingObservable<int>());

        Assert.Throws<TestSubscribeException>(() => observable.Subscribe(new RecordingObserver<int>()));
        Assert.True(first.IsDisposed);
    }

    [Fact]
    public void IgnoresLateValuesAndErrorsAfterTermination()
    {
        var first = new ControlledObservable<int>();
        var second = new ControlledObservable<int>();
        var observer = new RecordingObserver<int>();
        var observable = new CombineLatestObservable<int, int, int>(
            first,
            second,
            static (_, _) => throw new InvalidOperationException("selector"),
            true,
            EqualityComparer<int>.Default);

        using var subscription = observable.Subscribe(observer);
        first.Next(1);
        second.Next(2);
        first.Next(3);
        first.Error(new InvalidOperationException("late"));
        second.Complete();

        Assert.IsType<InvalidOperationException>(observer.ErrorValue);
        Assert.Empty(observer.Values);
    }

    [Fact]
    public void ForwardsSourceErrorsAndToleratesCompletion()
    {
        var first = new ControlledObservable<int>();
        var second = new ControlledObservable<int>();
        var observer = new RecordingObserver<int>();
        var observable = Create(first, second);

        using var subscription = observable.Subscribe(observer);
        first.Complete();
        second.Error(new TestSourceException());
        second.Error(new InvalidOperationException("late"));

        Assert.IsType<TestSourceException>(observer.ErrorValue);
    }

    [Fact]
    public void ObserverFailureStopsAndDisposesBothSources()
    {
        var first = new ControlledObservable<int>();
        var second = new ControlledObservable<int>();
        var observable = Create(first, second);
        var subscription = observable.Subscribe(new ThrowingObserver<int>());

        first.Next(1);
        Assert.Throws<TestObserverException>(() => second.Next(2));
        second.Next(3);
        subscription.Dispose();
        subscription.Dispose();

        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    private static CombineLatestObservable<int, int, int> Create(
        IObservable<int> first,
        IObservable<int> second) => new(
        first,
        second,
        static (left, right) => left + right,
        true,
        EqualityComparer<int>.Default);

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

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        public void Dispose() => callback();
    }

    private sealed class RecordingObserver<T> : IObserver<T>
    {
        public List<T> Values { get; } = [];

        public Exception? ErrorValue { get; private set; }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error) => ErrorValue = error;

        public void OnNext(T value) => Values.Add(value);
    }

    private sealed class TestSubscribeException : Exception;

    private sealed class TestSourceException : Exception;
}
