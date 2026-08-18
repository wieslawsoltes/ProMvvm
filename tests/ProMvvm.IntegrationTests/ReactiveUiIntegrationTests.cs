using ReactiveUI.SourceGenerators;
using ReactiveUI.Reactive.Builder;
using System.Reactive.Linq;
using RxReactiveObject = ReactiveUI.Reactive.ReactiveObject;

namespace ProMvvm.IntegrationTests;

[GeneratePropertyPaths]
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

        using var subscription = model.WhenAnyValue(ReactiveViewModelPropertyPaths.Name)
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

        using var proSubscription = model.WhenAnyValue(ReactiveViewModelPropertyPaths.Count)
            .Subscribe(proValues.Add);

        using var reactiveUiSubscription = ReactiveUiAdapter.ObserveCount(model)
            .Subscribe(reactiveUiValues.Add);

        model.Count = 2;

        Assert.Equal([1, 2], proValues);
        Assert.Equal(proValues, reactiveUiValues);
    }

    [Fact]
    public void ExpressionAndStringMigrationOverloadsMatchReactiveUiReactive()
    {
        var model = new ReactiveViewModel();
        var proExpressionValues = new List<string>();
        var proStringValues = new List<string>();
        var reactiveExpressionValues = new List<string>();
        var reactiveStringValues = new List<string>();

#pragma warning disable IL2026
        using var proExpression = WhenAnyValueExtensions.WhenAnyValue<ReactiveViewModel, string, int>(
                model,
                value => value.Count,
                static value => $"#{value}")
            .Subscribe(proExpressionValues.Add);
        using var proString = WhenAnyValueExtensions.WhenAnyValue<ReactiveViewModel, string, int>(
                model,
                nameof(ReactiveViewModel.Count),
                static value => $"#{value}")
            .Subscribe(proStringValues.Add);
#pragma warning restore IL2026
        using var reactiveExpression = ReactiveUiAdapter.ObserveSelectedCount(model)
            .Subscribe(reactiveExpressionValues.Add);
        using var reactiveString = ReactiveUiAdapter.ObserveSelectedCountByName(model)
            .Subscribe(reactiveStringValues.Add);

        model.Count = 2;

        Assert.Equal(["#1", "#2"], proExpressionValues);
        Assert.Equal(proExpressionValues, proStringValues);
        Assert.Equal(proExpressionValues, reactiveExpressionValues);
        Assert.Equal(proExpressionValues, reactiveStringValues);
    }
}

internal static class ReactiveUiAdapter
{
    static ReactiveUiAdapter() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    public static IObservable<int> ObserveCount(ReactiveViewModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Count);

    public static IObservable<string> ObserveSelectedCount(ReactiveViewModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Count,
            (Func<int, string>)(static value => $"#{value}"));

    public static IObservable<string> ObserveSelectedCountByName(ReactiveViewModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue<ReactiveViewModel, string, int>(
            model,
            nameof(ReactiveViewModel.Count),
            static value => $"#{value}");
}
