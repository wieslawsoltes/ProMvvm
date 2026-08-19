using CommunityToolkit.Mvvm.ComponentModel;

namespace ProMvvm.Sample.CommunityToolkit;

[ProMvvm.GeneratePropertyPaths]
public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}
