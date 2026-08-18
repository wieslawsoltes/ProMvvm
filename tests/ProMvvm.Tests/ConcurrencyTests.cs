namespace ProMvvm.Tests;

public sealed class ConcurrencyTests
{
    [Fact]
    public void SinglePropertySerializesConcurrentNotifications()
    {
        var model = new ObservableModel { Count = 1 };
        var observer = new ConcurrencyObserver<int>();

        using var subscription = model.WhenAnyValue(
            static value => value.Count,
            nameof(ObservableModel.Count),
            isDistinct: false).Subscribe(observer);

        Parallel.For(0, 2_000, _ => model.Raise(nameof(ObservableModel.Count)));

        Assert.False(observer.Overlapped);
        Assert.Equal(2_001, observer.Count);
    }

    [Fact]
    public void MultiPropertySinkSerializesConcurrentSources()
    {
        var model = new ObservableModel { Count = 1, Name = "value" };
        var count = PropertyPath.Create<ObservableModel, int>(
            nameof(ObservableModel.Count), static value => value.Count);
        var name = PropertyPath.Create<ObservableModel, string?>(
            nameof(ObservableModel.Name), static value => value.Name);
        var observer = new ConcurrencyObserver<string>();

        using var subscription = model.WhenAnyValue(
            count,
            name,
            static (currentCount, currentName) => $"{currentCount}:{currentName}",
            isDistinct: false).Subscribe(observer);

        Parallel.For(0, 2_000, index =>
            model.Raise(index % 2 == 0
                ? nameof(ObservableModel.Count)
                : nameof(ObservableModel.Name)));

        Assert.False(observer.Overlapped);
        Assert.Equal(2_001, observer.Count);
    }

    [Fact]
    public void IndependentSubscriptionsCanBeCreatedAndDisposedConcurrently()
    {
        var model = new ObservableModel { Count = 1 };
        var failures = 0;

        Parallel.For(0, 1_000, _ =>
        {
            try
            {
                using var subscription = model.WhenAnyValue(
                    static value => value.Count,
                    nameof(ObservableModel.Count)).Subscribe(static _ => { });
            }
            catch
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.Equal(0, failures);
        Assert.Equal(0, model.SubscriberCount);
    }

    private sealed class ConcurrencyObserver<T> : IObserver<T>
    {
        private int _active;
        private int _count;
        private int _overlapped;

        public int Count => Volatile.Read(ref _count);

        public bool Overlapped => Volatile.Read(ref _overlapped) != 0;

        public void OnCompleted()
        {
        }

        public void OnError(Exception error) => throw error;

        public void OnNext(T value)
        {
            if (Interlocked.Increment(ref _active) != 1)
            {
                Volatile.Write(ref _overlapped, 1);
            }

            Thread.SpinWait(100);
            Interlocked.Increment(ref _count);
            Interlocked.Decrement(ref _active);
        }
    }
}
