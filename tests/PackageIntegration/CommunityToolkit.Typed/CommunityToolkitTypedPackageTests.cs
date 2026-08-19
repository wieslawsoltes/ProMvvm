namespace ProMvvm.PackageIntegration;

public sealed class CommunityToolkitTypedPackageTests
{
    [Fact]
    public void GeneratedNestedAndMultiSourcePathsObserveToolkitProperties()
    {
        var model = new ToolkitModel();
        var count = new RecordingObserver<int>();
        var child = new RecordingObserver<int>();
        var projection = new RecordingObserver<string>();
        var childPath = ToolkitModelPropertyPaths.Child
            .Then(nameof(ToolkitChild.Value), static value => value.Value);

        using var countSubscription = model.WhenAnyValue(ToolkitModelPropertyPaths.Count)
            .Subscribe(count);
        using var childSubscription = model.WhenAnyValue(childPath).Subscribe(child);
        using var projectionSubscription = model.WhenAnyValue(
                ToolkitModelPropertyPaths.Name,
                ToolkitModelPropertyPaths.Count,
                static (name, value) => $"{name}:{value}")
            .Subscribe(projection);

        model.Count = 2;
        model.Child.Value = 3;
        model.Name = "changed";
        model.Child = new ToolkitChild { Value = 4 };

        Assert.Equal([1, 2], count.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["toolkit:1", "toolkit:2", "changed:2"], projection.Values);
    }
}
