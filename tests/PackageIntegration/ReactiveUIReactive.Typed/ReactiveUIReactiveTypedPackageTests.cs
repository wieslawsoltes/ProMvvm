using ReactiveUI.Reactive.Builder;

namespace ProMvvm.PackageIntegration;

public sealed class ReactiveUIReactiveTypedPackageTests
{
    static ReactiveUIReactiveTypedPackageTests() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    [Fact]
    public void GeneratedNestedAndMultiSourcePathsObserveReactiveUIReactiveProperties()
    {
        var model = new ReactiveUIReactiveModel();
        var count = new RecordingObserver<int>();
        var child = new RecordingObserver<int>();
        var projection = new RecordingObserver<string>();
        var childPath = ReactiveUIReactiveModelPropertyPaths.Child
            .Then(nameof(ReactiveUIReactiveChild.Value), static value => value.Value);

        using var countSubscription = model.WhenAnyValue(
                ReactiveUIReactiveModelPropertyPaths.Count)
            .Subscribe(count);
        using var childSubscription = model.WhenAnyValue(childPath).Subscribe(child);
        using var projectionSubscription = model.WhenAnyValue(
                ReactiveUIReactiveModelPropertyPaths.Name,
                ReactiveUIReactiveModelPropertyPaths.Count,
                static (name, value) => $"{name}:{value}")
            .Subscribe(projection);

        model.Count = 2;
        model.Child.Value = 3;
        model.Name = "changed";
        model.Child = new ReactiveUIReactiveChild { Value = 4 };

        Assert.Equal([1, 2], count.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["reactive:1", "reactive:2", "changed:2"], projection.Values);
    }
}
