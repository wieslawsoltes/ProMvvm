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

    public string? Text;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetValue(int value)
    {
        Value = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    public void Raise(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void SetText(string? text) => Text = text;
}

internal sealed class IndexerModel : INotifyPropertyChanged
{
    private PropertyChangedEventHandler? _propertyChanged;
    private readonly int[] _values = [0, 10, 20];
    private readonly Dictionary<string, string> _namedValues = new(StringComparer.Ordinal)
    {
        ["first"] = "one",
    };
    private readonly Dictionary<(int Row, int Column), int> _matrix = new()
    {
        [(1, 2)] = 12,
    };
    private IndexerModel? _child;
    private int _subscriberCount;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            _propertyChanged += value;
            _subscriberCount++;
        }

        remove
        {
            _propertyChanged -= value;
            _subscriberCount--;
        }
    }

    public int SubscriberCount => _subscriberCount;

    public IndexerModel? Child
    {
        get => _child;
        set
        {
            _child = value;
            Raise(nameof(Child));
        }
    }

    public int[] Values => _values;

    public int this[int index] => _values[index];

    public string this[string? key] => key is null ? "<null>" : _namedValues[key];

    public int this[int row, int column] => _matrix[(row, column)];

    public long this[long index] => index + 100;

    public long this[long row, long column] => row + column;

    public int this[int first, int second, int third] => first + second + third;

    public int this[int first, int second, int third, int fourth] =>
        first + second + third + fourth;

    public int this[int first, int second, int third, int fourth, int fifth] =>
        first + second + third + fourth + fifth;

    public int this[object? key] => key is null ? -1 : RuntimeHelpers.GetHashCode(key);

    public void Set(int index, int value, string? notificationName = "Item[]")
    {
        _values[index] = value;
        Raise(notificationName);
    }

    public void Set(string key, string value, string? notificationName = "Item[]")
    {
        _namedValues[key] = value;
        Raise(notificationName);
    }

    public void Set(int row, int column, int value, string? notificationName = "Item[]")
    {
        _matrix[(row, column)] = value;
        Raise(notificationName);
    }

    private void Raise(string? propertyName) =>
        _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class CustomIndexerModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [IndexerName("Entry")]
    public int this[short index] => index + 200;

    public void Raise(string? notificationName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(notificationName));
}

internal sealed class FieldChainModel : INotifyPropertyChanged
{
    public FieldChainModel? Child;

    public int Value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Raise(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
