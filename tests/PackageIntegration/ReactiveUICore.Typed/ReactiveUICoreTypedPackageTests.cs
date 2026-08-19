using ReactiveUI.Builder;

namespace ProMvvm.PackageIntegration;

public sealed class ReactiveUICoreTypedPackageTests
{
    static ReactiveUICoreTypedPackageTests() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    [Fact]
    public void GeneratedNestedAndMultiSourcePathsObserveReactiveUICoreObjects()
    {
        var model = new ReactiveUICoreModel();
        var count = new RecordingObserver<int>();
        var child = new RecordingObserver<int>();
        var projection = new RecordingObserver<string>();
        var childPath = ReactiveUICoreModelPropertyPaths.Child
            .Then(nameof(ReactiveUICoreChild.Value), static value => value.Value);

        using var countSubscription = model.WhenAnyValue(ReactiveUICoreModelPropertyPaths.Count)
            .Subscribe(count);
        using var childSubscription = model.WhenAnyValue(childPath).Subscribe(child);
        using var projectionSubscription = model.WhenAnyValue(
                ReactiveUICoreModelPropertyPaths.Name,
                ReactiveUICoreModelPropertyPaths.Count,
                static (name, value) => $"{name}:{value}")
            .Subscribe(projection);

        model.Count = 2;
        model.Child.Value = 3;
        model.Name = "changed";
        model.Child = new ReactiveUICoreChild { Value = 4 };

        Assert.Equal([1, 2], count.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["core:1", "core:2", "changed:2"], projection.Values);
    }
}
