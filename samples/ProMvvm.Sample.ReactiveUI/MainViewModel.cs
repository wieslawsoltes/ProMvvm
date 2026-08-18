using ReactiveUI.SourceGenerators;
using RxReactiveObject = ReactiveUI.Reactive.ReactiveObject;

namespace ProMvvm.Sample.ReactiveUI;

public sealed partial class MainViewModel : RxReactiveObject
{
    [Reactive]
    private string _searchText = string.Empty;
}
