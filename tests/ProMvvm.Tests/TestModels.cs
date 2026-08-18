using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProMvvm.Tests;

internal class ObservableModel : INotifyPropertyChanged
{
    private PropertyChangedEventHandler? _propertyChanged;
    private readonly object _eventGate = new();
    private string? _name;
    private int _count;
    private int _subscriberCount;
    private ObservableModel? _child;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            lock (_eventGate)
            {
                _propertyChanged += value;
                _subscriberCount++;
            }
        }

        remove
        {
            lock (_eventGate)
            {
                _propertyChanged -= value;
                _subscriberCount--;
            }
        }
    }

    public int SubscriberCount => Volatile.Read(ref _subscriberCount);

    public string? Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public int Count
    {
        get => _count;
        set => Set(ref _count, value);
    }

    public ObservableModel? Child
    {
        get => _child;
        set => Set(ref _child, value);
    }

    public string Throwing => _count >= 0
        ? throw new InvalidOperationException("getter failed")
        : string.Empty;

    public void Raise(string? propertyName)
    {
        PropertyChangedEventHandler? handler;
        lock (_eventGate)
        {
            handler = _propertyChanged;
        }

        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string GetName() => Name ?? string.Empty;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        Raise(propertyName);
    }
}

internal sealed class DerivedObservableModel : ObservableModel;

internal sealed class RuntimePropertyObservableModel : ObservableModel
{
    public string? RuntimeOnly { get; set; }
}

internal sealed class FieldModel : INotifyPropertyChanged
{
    public int Value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetValue(int value)
    {
        Value = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}

internal sealed class PlainModel
{
    public int Value { get; set; }
}

internal sealed class PlainThreeSegmentRoot
{
    public PlainThreeSegmentMiddle Middle { get; } = new();
}

internal sealed class PlainThreeSegmentMiddle
{
    public PlainModel Leaf { get; } = new() { Value = 11 };
}

internal sealed class ThrowingObserver<T> : IObserver<T>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(T value) => throw new TestObserverException();
}

internal sealed class TestObserverException : Exception;
