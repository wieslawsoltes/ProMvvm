using CommunityToolkit.Mvvm.ComponentModel;

namespace ProMvvm.Sample.CommunityToolkit.Expression;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _minimumLength = 3;

    [ObservableProperty]
    private SearchOptions? _options = new();
}

public sealed partial class SearchOptions : ObservableObject
{
    [ObservableProperty]
    private string _category = "All";
}
