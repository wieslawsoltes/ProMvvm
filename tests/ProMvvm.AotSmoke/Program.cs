using ProMvvm;
using System.ComponentModel;

var model = new AotModel();
var values = new List<int>();

using (model.WhenAnyValue(AotModelPropertyPaths.Count).Subscribe(new ListObserver<int>(values)))
{
    model.Count = 1;
    model.Count = 2;
}

if (!values.SequenceEqual([0, 1, 2]))
{
    throw new InvalidOperationException("AOT observation failed.");
}

var arityValues = new List<int>();
using (model.WhenAnyValue(
           AotModelPropertyPaths.Count, AotModelPropertyPaths.Count,
           AotModelPropertyPaths.Count, AotModelPropertyPaths.Count,
           AotModelPropertyPaths.Count, AotModelPropertyPaths.Count,
           AotModelPropertyPaths.Count, AotModelPropertyPaths.Count,
           AotModelPropertyPaths.Count, AotModelPropertyPaths.Count,
           AotModelPropertyPaths.Count, AotModelPropertyPaths.Count,
           static (a, b, c, d, e, f, g, h, i, j, k, l) =>
               a + b + c + d + e + f + g + h + i + j + k + l)
       .Subscribe(new ListObserver<int>(arityValues)))
{
    model.Count = 3;
}

if (arityValues[0] != 24 || arityValues[^1] != 36)
{
    throw new InvalidOperationException("AOT arity-12 observation failed.");
}

var childPath = PropertyPath
    .Create<AotModel, AotChild?>(nameof(AotModel.Child), static value => value.Child)
    .Then(nameof(AotChild.Value), static value => value!.Value);
var oldChild = new AotChild { Value = 4 };
var newChild = new AotChild { Value = 5 };
model.Child = oldChild;
var nestedValues = new List<int>();
using (model.WhenAnyValue(childPath).Subscribe(new ListObserver<int>(nestedValues)))
{
    oldChild.Value = 6;
    model.Child = newChild;
    oldChild.Value = 7;
}

if (!nestedValues.SequenceEqual([4, 6, 5]))
{
    throw new InvalidOperationException("AOT specialized nested observation failed.");
}

var deepPath = PropertyPath
    .Create<AotModel, AotChild?>(nameof(AotModel.Child), static value => value.Child)
    .Then(nameof(AotChild.Child), static value => value!.Child)
    .Then(nameof(AotChild.Value), static value => value!.Value);
var oldLeaf = new AotChild { Value = 10 };
var newLeaf = new AotChild { Value = 11 };
newChild.Child = oldLeaf;
model.Child = newChild;
var deepValues = new List<int>();
using (model.WhenAnyValue(deepPath).Subscribe(new ListObserver<int>(deepValues)))
{
    oldLeaf.Value = 12;
    newChild.Child = newLeaf;
    oldLeaf.Value = 13;
}

if (!deepValues.SequenceEqual([10, 12, 11]))
{
    throw new InvalidOperationException("AOT specialized three-segment observation failed.");
}

var adapterModel = new AdapterModel { Value = 8 };
var adapter = PropertyNotificationAdapters.Create<AdapterModel>(
    static (source, callback) => source.Subscribe(callback));
var adapterValues = new List<int>();
using (adapterModel.WhenAnyValue(AdapterModelPropertyPaths.Value, adapter)
       .Subscribe(new ListObserver<int>(adapterValues)))
{
    adapterModel.Value = 9;
}

if (!adapterValues.SequenceEqual([8, 9]))
{
    throw new InvalidOperationException("AOT notification adapter failed.");
}

Console.WriteLine("ProMvvm NativeAOT smoke test passed.");

[GeneratePropertyPaths]
internal sealed class AotModel : INotifyPropertyChanged
{
    private int _count;
    private AotChild? _child;

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

    public AotChild? Child
    {
        get => _child;
        set
        {
            _child = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Child)));
        }
    }
}

[GeneratePropertyPaths]
internal sealed class AotChild : INotifyPropertyChanged
{
    private int _value;
    private AotChild? _child;

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

    public AotChild? Child
    {
        get => _child;
        set
        {
            _child = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Child)));
        }
    }
}

[GeneratePropertyPaths]
internal sealed class AdapterModel
{
    private Action<string?>? _changed;
    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            _changed?.Invoke(nameof(Value));
        }
    }

    public CallbackDisposable Subscribe(Action<string?> callback)
    {
        _changed += callback;
        return new CallbackDisposable(() => _changed -= callback);
    }
}

internal sealed class CallbackDisposable(Action callback) : IDisposable
{
    private Action? _callback = callback;

    public void Dispose() => Interlocked.Exchange(ref _callback, null)?.Invoke();
}

internal sealed class ListObserver<T>(List<T> values) : IObserver<T>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error) => throw error;

    public void OnNext(T value) => values.Add(value);
}
