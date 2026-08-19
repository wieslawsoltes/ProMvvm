using ReactiveUI.Builder;

namespace ProMvvm.PackageIntegration;

public sealed class ReactiveUICoreExpressionPackageTests
{
    static ReactiveUICoreExpressionPackageTests() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    [Fact]
    public void ProMvvmExpressionsMatchReactiveUICoreAndRewireNestedProperties()
    {
        var model = new ReactiveUICoreModel();
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
        using var reactiveCountSubscription = ReactiveUI.WhenAnyMixins.WhenAnyValue(
                model,
                value => value.Count)
            .Subscribe(reactiveCount);

        model.Count = 2;
        model.Child.Value = 3;
        model.Name = "changed";
        model.Child = new ReactiveUICoreChild { Value = 4 };

        Assert.Equal([1, 2], proCount.Values);
        Assert.Equal(proCount.Values, reactiveCount.Values);
        Assert.Equal([2, 3, 4], child.Values);
        Assert.Equal(["core:1", "core:2", "changed:2"], projection.Values);
    }
}
