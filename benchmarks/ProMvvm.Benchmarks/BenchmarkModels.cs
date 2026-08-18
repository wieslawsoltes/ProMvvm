using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProMvvm.Benchmarks;

[GeneratePropertyPaths]
public sealed class BenchmarkModel : INotifyPropertyChanged
{
    private int _value;
    private int _other;
    private BenchmarkChild? _child;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value
    {
        get => _value;
        set => Set(ref _value, value);
    }

    public int Other
    {
        get => _other;
        set => Set(ref _other, value);
    }

    public BenchmarkChild? Child
    {
        get => _child;
        set => Set(ref _child, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class BenchmarkChild : INotifyPropertyChanged
{
    private int _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}

internal sealed class BenchmarkObserver<T> : IObserver<T>
{
    public T? LastValue { get; private set; }

    public int Count { get; private set; }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error) => throw error;

    public void OnNext(T value)
    {
        LastValue = value;
        Count++;
    }
}

internal static class BenchmarkPaths
{
    public static readonly PropertyPath<BenchmarkModel, int> Value =
        PropertyPath.Create<BenchmarkModel, int>(
            nameof(BenchmarkModel.Value), static model => model.Value);

    public static readonly PropertyPath<BenchmarkModel, int> Other =
        PropertyPath.Create<BenchmarkModel, int>(
            nameof(BenchmarkModel.Other), static model => model.Other);

    public static readonly PropertyPath<BenchmarkModel, int> ChildValue =
        PropertyPath.Create<BenchmarkModel, BenchmarkChild?>(
                nameof(BenchmarkModel.Child), static model => model.Child)
            .Then(nameof(BenchmarkChild.Value), static child => child!.Value);
}
