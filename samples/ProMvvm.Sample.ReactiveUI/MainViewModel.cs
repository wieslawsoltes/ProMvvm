using ReactiveUI.SourceGenerators;
using RxReactiveObject = ReactiveUI.Reactive.ReactiveObject;

namespace ProMvvm.Sample.ReactiveUI;

[ProMvvm.GeneratePropertyPaths]
public sealed partial class MainViewModel : RxReactiveObject
{
    [Reactive]
    private string _searchText = string.Empty;
}
