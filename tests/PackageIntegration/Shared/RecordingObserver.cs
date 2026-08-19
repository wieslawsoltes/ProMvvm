global using Xunit;

namespace ProMvvm.PackageIntegration;

internal sealed class RecordingObserver<T> : IObserver<T>
{
    public List<T> Values { get; } = [];

    public void OnCompleted()
    {
    }

    public void OnError(Exception error) => throw error;

    public void OnNext(T value) => Values.Add(value);
}
