using ReactiveUI.SourceGenerators;
using RxReactiveObject = ReactiveUI.Reactive.ReactiveObject;

namespace ProMvvm.PackageIntegration;

[GeneratePropertyPaths]
public sealed partial class ReactiveUIReactiveModel : RxReactiveObject
{
    [Reactive]
    private int _count = 1;

    [Reactive]
    private string _name = "reactive";

    [Reactive]
    private ReactiveUIReactiveChild _child = new() { Value = 2 };
}

public sealed partial class ReactiveUIReactiveChild : RxReactiveObject
{
    [Reactive]
    private int _value;
}
