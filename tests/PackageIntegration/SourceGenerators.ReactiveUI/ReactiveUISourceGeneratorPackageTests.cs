using ReactiveUI.Reactive.Builder;

namespace ProMvvm.PackageIntegration;

public sealed class ReactiveUISourceGeneratorPackageTests
{
    static ReactiveUISourceGeneratorPackageTests() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    [Fact]
    public void ExplicitGeneratorDiscoversReactiveFieldsGeneratedByReactiveUISourceGenerators()
    {
        var model = new ReactiveUIReactiveModel();
        var values = new RecordingObserver<string>();

        using var subscription = model.WhenAnyValue(
                ReactiveUIReactiveModelPropertyPaths.Name,
                ReactiveUIReactiveModelPropertyPaths.Count,
                ReactiveUIReactiveModelPropertyPaths.Child,
                static (name, count, child) => $"{name}:{count}:{child.Value}")
            .Subscribe(values);

        model.Count = 2;

        Assert.Equal(["reactive:1:2", "reactive:2:2"], values.Values);
    }
}
