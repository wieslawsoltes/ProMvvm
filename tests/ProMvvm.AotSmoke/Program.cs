using ProMvvm;
using System.ComponentModel;

var model = new AotModel();
var values = new List<int>();

using (model.WhenAnyValue(
           static value => value.Count,
           nameof(AotModel.Count)).Subscribe(new ListObserver<int>(values)))
{
    model.Count = 1;
    model.Count = 2;
}

if (!values.SequenceEqual([0, 1, 2]))
{
    throw new InvalidOperationException("AOT observation failed.");
}

Console.WriteLine("ProMvvm NativeAOT smoke test passed.");

internal sealed class AotModel : INotifyPropertyChanged
{
    private int _count;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
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
