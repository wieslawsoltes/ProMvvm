using ReactiveUI;

namespace ProMvvm.PackageIntegration;

[GeneratePropertyPaths]
public sealed partial class ReactiveUICoreModel : ReactiveObject
{
    private int _count = 1;
    private string _name = "core";
    private ReactiveUICoreChild _child = new() { Value = 2 };

    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public ReactiveUICoreChild Child
    {
        get => _child;
        set => this.RaiseAndSetIfChanged(ref _child, value);
    }
}

public sealed class ReactiveUICoreChild : ReactiveObject
{
    private int _value;

    public int Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }
}
