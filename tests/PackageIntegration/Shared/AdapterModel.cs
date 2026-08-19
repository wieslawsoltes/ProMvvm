namespace ProMvvm.PackageIntegration;

[GeneratePropertyPaths]
public sealed partial class AdapterModel
{
    private int _value = 1;

    public event Action<string?>? Changed;

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
            Changed?.Invoke(nameof(Value));
        }
    }

    public IDisposable Subscribe(Action<string?> callback)
    {
        Changed += callback;
        return new Subscription(this, callback);
    }

    private sealed class Subscription(AdapterModel source, Action<string?> callback) : IDisposable
    {
        public void Dispose() => source.Changed -= callback;
    }
}
