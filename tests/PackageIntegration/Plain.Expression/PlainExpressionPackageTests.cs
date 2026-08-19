namespace ProMvvm.PackageIntegration;

public sealed class PlainExpressionPackageTests
{
    [Fact]
    public void ExpressionStringNestedAndMultiSourceApisObservePlainInpcModels()
    {
        var model = new PlainModel();
        var count = new RecordingObserver<int>();
        var named = new RecordingObserver<int>();
        var child = new RecordingObserver<int>();
        var projection = new RecordingObserver<string>();

#pragma warning disable IL2026
        using var countSubscription = WhenAnyValueExtensions.WhenAnyValue(model, value => value.Count)
            .Subscribe(count);
        using var namedSubscription = WhenAnyValueExtensions.WhenAnyValue<PlainModel, int>(
                model,
                nameof(PlainModel.Count))
            .Subscribe(named);
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
        model.Child = new PlainChild { Value = 4 };

        Assert.Equal([1, 2], count.Values);
        Assert.Equal(count.Values, named.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["plain:1", "plain:2", "changed:2"], projection.Values);
    }
}
