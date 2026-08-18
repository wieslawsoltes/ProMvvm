using ReactiveUI.SourceGenerators;
using ReactiveUI.Reactive.Builder;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
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

    [Fact]
    public void MultiSourceExpressionSelectorMatchesBothReactiveUiDistributions()
    {
        var model = new ReactiveUiParityModel { A = 1, B = 2 };
        var proValues = new List<string>();
        var reactiveValues = new List<string>();

#pragma warning disable IL2026
        using var pro = WhenAnyValueMultiExtensions.WhenAnyValue(
                model,
                value => value.A,
                value => value.B,
                static (_, _) => "same")
            .Subscribe(proValues.Add);
#pragma warning restore IL2026
        using var reactive = ReactiveUiAdapter.ObserveConstantProjection(model)
            .Subscribe(reactiveValues.Add);

        model.A = 3;
        model.B = 4;
        model.A = 3;

        Assert.Equal(["same", "same", "same"], proValues);
        Assert.Equal(proValues, reactiveValues);
    }

    [Fact]
    public void ConstantIndexerMatchesBothReactiveUiDistributions()
    {
        var model = new ReactiveUiParityModel();
        var proValues = new List<int>();
        var reactiveValues = new List<int>();

#pragma warning disable IL2026
        using var pro = WhenAnyValueExtensions.WhenAnyValue(model, value => value[0])
            .Subscribe(proValues.Add);
#pragma warning restore IL2026
        using var reactive = ReactiveUiAdapter.ObserveIndex(model).Subscribe(reactiveValues.Add);

        model.SetIndex(0, 1, "Item[]");
        model.SetIndex(0, 2, "Item");
        model.SetIndex(0, 3, null);

        Assert.Equal([0, 1, 3], proValues);
        Assert.Equal(proValues, reactiveValues);
    }

    [Fact]
    public void ArrayLengthRewriteMatchesReactiveUiReactive()
    {
        var model = new ReactiveUiParityModel();
        var proValues = new List<int>();
        var reactiveValues = new List<int>();

#pragma warning disable IL2026
        using var pro = WhenAnyValueExtensions.WhenAnyValue(model, value => value.Values.Length)
            .Subscribe(proValues.Add);
#pragma warning restore IL2026
        using var reactive = ReactiveUiAdapter.ObserveArrayLength(model)
            .Subscribe(reactiveValues.Add);

        Assert.Equal([3], proValues);
        Assert.Equal(proValues, reactiveValues);
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

    public static IObservable<string> ObserveConstantProjection(ReactiveUiParityModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.A,
            value => value.B,
            (Func<int, int, string>)(static (_, _) => "same"));

    public static IObservable<int> ObserveIndex(ReactiveUiParityModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value[0]);

    public static IObservable<int> ObserveArrayLength(ReactiveUiParityModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Values.Length);
}

internal sealed class ReactiveUiParityModel : INotifyPropertyChanged
{
    private readonly int[] _indices = [0];
    private int _a;
    private int _b;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int A
    {
        get => _a;
        set => Set(ref _a, value);
    }

    public int B
    {
        get => _b;
        set => Set(ref _b, value);
    }

    public int this[int index] => _indices[index];

    public int[] Values { get; } = [1, 2, 3];

    public void SetIndex(int index, int value, string? notificationName)
    {
        _indices[index] = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(notificationName));
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
