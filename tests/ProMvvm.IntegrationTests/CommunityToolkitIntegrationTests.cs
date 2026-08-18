using CommunityToolkit.Mvvm.ComponentModel;
using System.Reactive.Linq;

namespace ProMvvm.IntegrationTests;

[GeneratePropertyPaths]
public sealed partial class ToolkitViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "Toolkit";

    [ObservableProperty]
    private int _count = 1;
}

public sealed class CommunityToolkitIntegrationTests
{
    [Fact]
    public void GeneratedPropertiesComposeWithSystemReactive()
    {
        var model = new ToolkitViewModel();
        var values = new List<string>();

        using var subscription = model.WhenAnyValue(ToolkitViewModelPropertyPaths.Name)
            .Select(static value => value.ToUpperInvariant())
            .Subscribe(values.Add);

        model.Name = "changed";

        Assert.Equal(["TOOLKIT", "CHANGED"], values);
    }

    [Fact]
    public void MultipleGeneratedPropertiesCanBeProjected()
    {
        var model = new ToolkitViewModel();
        var values = new List<string>();

        using var subscription = model.WhenAnyValue(
            ToolkitViewModelPropertyPaths.Name,
            ToolkitViewModelPropertyPaths.Count,
            static (currentName, currentCount) => $"{currentName}:{currentCount}")
            .Subscribe(values.Add);

        model.Count = 2;

        Assert.Equal(["Toolkit:1", "Toolkit:2"], values);
    }
}
