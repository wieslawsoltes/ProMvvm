using ReactiveUI.Reactive.Builder;

namespace ProMvvm.PackageIntegration;

public sealed class ReactiveUIReactiveExpressionPackageTests
{
    static ReactiveUIReactiveExpressionPackageTests() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    [Fact]
    public void ProMvvmExpressionsMatchReactiveUIReactiveAndRewireNestedProperties()
    {
        var model = new ReactiveUIReactiveModel();
        var proCount = new RecordingObserver<int>();
        var reactiveCount = new RecordingObserver<int>();
        var child = new RecordingObserver<int>();
        var projection = new RecordingObserver<string>();

#pragma warning disable IL2026
        using var proCountSubscription = WhenAnyValueExtensions.WhenAnyValue(
                model,
                value => value.Count)
            .Subscribe(proCount);
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
        using var reactiveCountSubscription = ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(
                model,
                value => value.Count)
            .Subscribe(reactiveCount);

        model.Count = 2;
        model.Child.Value = 3;
        model.Name = "changed";
        model.Child = new ReactiveUIReactiveChild { Value = 4 };

        Assert.Equal([1, 2], proCount.Values);
        Assert.Equal(proCount.Values, reactiveCount.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["reactive:1", "reactive:2", "changed:2"], projection.Values);
    }
}
