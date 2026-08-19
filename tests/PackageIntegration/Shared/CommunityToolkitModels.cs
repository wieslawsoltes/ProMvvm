using CommunityToolkit.Mvvm.ComponentModel;

namespace ProMvvm.PackageIntegration;

[GeneratePropertyPaths]
public sealed partial class ToolkitModel : ObservableObject
{
    [ObservableProperty]
    private int _count = 1;

    [ObservableProperty]
    private string _name = "toolkit";

    [ObservableProperty]
    private ToolkitChild _child = new() { Value = 2 };
}

public sealed partial class ToolkitChild : ObservableObject
{
    [ObservableProperty]
    private int _value;
}
