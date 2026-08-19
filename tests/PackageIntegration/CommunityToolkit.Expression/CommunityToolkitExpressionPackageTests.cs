namespace ProMvvm.PackageIntegration;

public sealed class CommunityToolkitExpressionPackageTests
{
    [Fact]
    public void ExpressionNestedAndMultiSourceApisObserveToolkitProperties()
    {
        var model = new ToolkitModel();
        var count = new RecordingObserver<int>();
        var child = new RecordingObserver<int>();
        var projection = new RecordingObserver<string>();

#pragma warning disable IL2026
        using var countSubscription = WhenAnyValueExtensions.WhenAnyValue(model, value => value.Count)
            .Subscribe(count);
        using var childSubscription = WhenAnyValueExtensions.WhenAnyValue(
                model,
                value => value.Child.Value)
            .Subscribe(child);
        using var projectionSubscription = WhenAnyValueMultiExtensions.WhenAnyValue(
                model,
                value => value.Name,
                value => value.Count,
                static (name, value) => $"{name}:{value}")
            .Subscribe(projection);
#pragma warning restore IL2026

        model.Count = 2;
        model.Child.Value = 3;
        model.Name = "changed";
        model.Child = new ToolkitChild { Value = 4 };

        Assert.Equal([1, 2], count.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["toolkit:1", "toolkit:2", "changed:2"], projection.Values);
    }
}
