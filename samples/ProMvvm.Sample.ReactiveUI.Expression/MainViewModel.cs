using ReactiveUI.SourceGenerators;
using RxReactiveObject = ReactiveUI.Reactive.ReactiveObject;

namespace ProMvvm.Sample.ReactiveUI.Expression;

public sealed partial class MainViewModel : RxReactiveObject
{
    [Reactive]
    private string _searchText = string.Empty;

    [Reactive]
    private int _minimumLength = 3;

    [Reactive]
    private SearchOptions? _options = new();
}

public sealed partial class SearchOptions : RxReactiveObject
{
    [Reactive]
    private string _category = "All";
}
