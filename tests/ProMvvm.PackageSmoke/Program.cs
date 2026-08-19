using System.ComponentModel;
using ProMvvm;

namespace ProMvvm.PackageSmoke;

internal static class Program
{
    public static void Main()
    {
        var model = new PackageModel { Value = 1 };
        var values = new List<int>();

        using (model.WhenAnyValue(PackageModelPropertyPaths.Value).Subscribe(new ListObserver<int>(values)))
        {
            model.Value = 2;
            model.Value = 2;
        }

        if (!values.SequenceEqual([1, 2]))
        {
            throw new InvalidOperationException(
                $"Package observation failed. Actual values: {string.Join(", ", values)}");
        }

        Console.WriteLine("ProMvvm package smoke test passed.");
    }
}

[GeneratePropertyPaths]
public sealed partial class PackageModel : INotifyPropertyChanged
{
    private int _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}

internal sealed class ListObserver<T>(List<T> values) : IObserver<T>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error) => throw error;

    public void OnNext(T value) => values.Add(value);
}
