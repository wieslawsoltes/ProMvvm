using ReactiveUI.Reactive.Builder;

namespace ProMvvm.Benchmarks;

internal static class ReactiveUiReactiveBenchmarkAdapter
{
    static ReactiveUiReactiveBenchmarkAdapter() =>
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();

    public static IObservable<int> Value(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Value);

    public static IObservable<int> ValueByName(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue<BenchmarkModel, int>(
            model,
            nameof(BenchmarkModel.Value));

    public static IObservable<int> SelectedValue(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Value,
            (Func<int, int>)(static value => value));

    public static IObservable<int> SelectedValueByName(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue<BenchmarkModel, int, int>(
            model,
            nameof(BenchmarkModel.Value),
            static value => value);

    public static IObservable<int> ChildValue(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Child!.Value);

    public static IObservable<int> DeepChildValue(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Child!.Child!.Value);

    public static IObservable<int> Index(BenchmarkIndexerModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value[0]);

    public static IObservable<int> ChildIndex(BenchmarkIndexerModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Child![0]);

    public static IObservable<int> Sum(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Value,
            value => value.Other,
            static (value, other) => value + other);

    public static IObservable<int> Sum12(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            static (a, b, c, d, e, f, g, h, i, j, k, l) =>
                a + b + c + d + e + f + g + h + i + j + k + l);
}
