using CommunityToolkit.Mvvm.ComponentModel;
using System.Reactive.Linq;

namespace ProMvvm.IntegrationTests;

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

        using var subscription = model.WhenAnyValue(
                static value => value.Name,
                nameof(ToolkitViewModel.Name))
            .Select(static value => value.ToUpperInvariant())
            .Subscribe(values.Add);

        model.Name = "changed";

        Assert.Equal(["TOOLKIT", "CHANGED"], values);
    }

    [Fact]
    public void MultipleGeneratedPropertiesCanBeProjected()
    {
        var model = new ToolkitViewModel();
        var name = PropertyPath.Create<ToolkitViewModel, string>(
            nameof(ToolkitViewModel.Name), static value => value.Name);
        var count = PropertyPath.Create<ToolkitViewModel, int>(
            nameof(ToolkitViewModel.Count), static value => value.Count);
        var values = new List<string>();

        using var subscription = model.WhenAnyValue(
            name,
            count,
            static (currentName, currentCount) => $"{currentName}:{currentCount}")
            .Subscribe(values.Add);

        model.Count = 2;

        Assert.Equal(["Toolkit:1", "Toolkit:2"], values);
    }
}
