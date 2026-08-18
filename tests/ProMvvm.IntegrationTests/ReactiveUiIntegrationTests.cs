using ReactiveUI.SourceGenerators;
using ReactiveUI.Reactive.Builder;
using System.Reactive.Linq;
using RxReactiveObject = ReactiveUI.Reactive.ReactiveObject;

namespace ProMvvm.IntegrationTests;

public sealed partial class ReactiveViewModel : RxReactiveObject
{
    [Reactive]
    private string _name = "ReactiveUI";

    [Reactive]
    private int _count = 1;
}

public sealed class ReactiveUiIntegrationTests
{
    [Fact]
    public void ReactiveUiGeneratedPropertiesComposeWithSystemReactive()
    {
        var model = new ReactiveViewModel();
        var values = new List<string>();

        using var subscription = model.WhenAnyValue(
                static value => value.Name,
                nameof(ReactiveViewModel.Name))
            .Select(static value => value.ToUpperInvariant())
            .Subscribe(values.Add);

        model.Name = "changed";

        Assert.Equal(["REACTIVEUI", "CHANGED"], values);
    }

    [Fact]
    public void ProMvvmAndReactiveUiReactiveCanCoexist()
    {
        var model = new ReactiveViewModel();
        var proValues = new List<int>();
        var reactiveUiValues = new List<int>();

        using var proSubscription = model.WhenAnyValue(
            static value => value.Count,
            nameof(ReactiveViewModel.Count)).Subscribe(proValues.Add);

        using var reactiveUiSubscription = ReactiveUiAdapter.ObserveCount(model)
            .Subscribe(reactiveUiValues.Add);

        model.Count = 2;

        Assert.Equal([1, 2], proValues);
        Assert.Equal(proValues, reactiveUiValues);
    }
}

internal static class ReactiveUiAdapter
{
    static ReactiveUiAdapter() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    public static IObservable<int> ObserveCount(ReactiveViewModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Count);
}
