using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProMvvm.Tests;

internal sealed class ObservableModel : INotifyPropertyChanged
{
    private PropertyChangedEventHandler? _propertyChanged;
    private string? _name;
    private int _count;
    private ObservableModel? _child;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            _propertyChanged += value;
            SubscriberCount++;
        }

        remove
        {
            _propertyChanged -= value;
            SubscriberCount--;
        }
    }

    public int SubscriberCount { get; private set; }

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

    public void Raise(string? propertyName) =>
        _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string GetName() => Name ?? string.Empty;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        Raise(propertyName);
    }
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
