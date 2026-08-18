using ReactiveUI.Builder;

namespace ProMvvm.Benchmarks;

internal static class ReactiveUiCoreBenchmarkAdapter
{
    static ReactiveUiCoreBenchmarkAdapter() =>
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();

    public static IObservable<int> Value(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(model, value => value.Value);

    public static IObservable<int> ChildValue(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(model, value => value.Child!.Value);

    public static IObservable<int> DeepChildValue(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(model, value => value.Child!.Child!.Value);

    public static IObservable<int> Sum(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Value,
            value => value.Other,
            static (value, other) => value + other);

    public static IObservable<int> Sum12(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            static (a, b, c, d, e, f, g, h, i, j, k, l) =>
                a + b + c + d + e + f + g + h + i + j + k + l);
}
