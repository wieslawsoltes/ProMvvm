namespace ProMvvm.PackageIntegration;

public sealed class PlainTypedPackageTests
{
    [Fact]
    public void GeneratedNestedAndMultiSourcePathsObservePlainInpcModels()
    {
        var model = new PlainModel();
        var count = new RecordingObserver<int>();
        var child = new RecordingObserver<int>();
        var projection = new RecordingObserver<string>();
        var childPath = PlainModelPropertyPaths.Child
            .Then(nameof(PlainChild.Value), static value => value.Value);

        using var countSubscription = model.WhenAnyValue(PlainModelPropertyPaths.Count)
            .Subscribe(count);
        using var childSubscription = model.WhenAnyValue(childPath).Subscribe(child);
        using var projectionSubscription = model.WhenAnyValue(
                PlainModelPropertyPaths.Name,
                PlainModelPropertyPaths.Count,
                static (name, value) => $"{name}:{value}")
            .Subscribe(projection);

        model.Count = 2;
        model.Child.Value = 3;
        model.Name = "changed";
        model.Child = new PlainChild { Value = 4 };

        Assert.Equal([1, 2], count.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["plain:1", "plain:2", "changed:2"], projection.Values);
    }
}
