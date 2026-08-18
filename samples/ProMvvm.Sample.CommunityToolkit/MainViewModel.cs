using CommunityToolkit.Mvvm.ComponentModel;

namespace ProMvvm.Sample.CommunityToolkit;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}
