using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ProMvvm.Tests;

[SuppressMessage("Trimming", "IL2026", Justification = "Expression overload compatibility is intentional.")]
public sealed class MultiArityTests
{
    [Fact]
    public void TypedSelectorOverloadsCoverAritiesThreeThroughTwelve()
    {
        var model = new ArityModel();

        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, static (a, b, c) => a + b + c), 6);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, static (a, b, c, d) => a + b + c + d), 10);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, static (a, b, c, d, e) => a + b + c + d + e), 15);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6, static (a, b, c, d, e, f) => a + b + c + d + e + f), 21);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6, Paths.P7, static (a, b, c, d, e, f, g) => a + b + c + d + e + f + g), 28);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6, Paths.P7, Paths.P8, static (a, b, c, d, e, f, g, h) => a + b + c + d + e + f + g + h), 36);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6, Paths.P7, Paths.P8, Paths.P9, static (a, b, c, d, e, f, g, h, i) => a + b + c + d + e + f + g + h + i), 45);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6, Paths.P7, Paths.P8, Paths.P9, Paths.P10, static (a, b, c, d, e, f, g, h, i, j) => a + b + c + d + e + f + g + h + i + j), 55);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6, Paths.P7, Paths.P8, Paths.P9, Paths.P10, Paths.P11, static (a, b, c, d, e, f, g, h, i, j, k) => a + b + c + d + e + f + g + h + i + j + k), 66);
        AssertInitial(model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6, Paths.P7, Paths.P8, Paths.P9, Paths.P10, Paths.P11, Paths.P12, static (a, b, c, d, e, f, g, h, i, j, k, l) => a + b + c + d + e + f + g + h + i + j + k + l), 78);
    }

    [Fact]
    public void ArityTwelveTypedAndExpressionStreamsTrackChanges()
    {
        var model = new ArityModel();
        var typed = new List<int>();
        var expression = new List<int>();

        using var typedSubscription = model.WhenAnyValue(
            Paths.P1, Paths.P2, Paths.P3, Paths.P4, Paths.P5, Paths.P6,
            Paths.P7, Paths.P8, Paths.P9, Paths.P10, Paths.P11, Paths.P12,
            static (a, b, c, d, e, f, g, h, i, j, k, l) =>
                a + b + c + d + e + f + g + h + i + j + k + l).Subscribe(typed.Add);
        using var expressionSubscription = model.WhenAnyValue(
            value => value.P1, value => value.P2, value => value.P3, value => value.P4,
            value => value.P5, value => value.P6, value => value.P7, value => value.P8,
            value => value.P9, value => value.P10, value => value.P11, value => value.P12,
            static (a, b, c, d, e, f, g, h, i, j, k, l) =>
                a + b + c + d + e + f + g + h + i + j + k + l).Subscribe(expression.Add);

        model.P12 = 20;

        Assert.Equal([78, 86], typed);
        Assert.Equal(typed, expression);
    }

    [Fact]
    public void TupleOverloadsSupportThreeAndTwelveValues()
    {
        var model = new ArityModel();
        (int Value1, int Value2, int Value3) tuple3 = default;
        (int Value1, int Value2, int Value3, int Value4, int Value5, int Value6,
            int Value7, int Value8, int Value9, int Value10, int Value11, int Value12) tuple12 = default;

        using var subscription3 = model.WhenAnyValue(Paths.P1, Paths.P2, Paths.P3)
            .Subscribe(value => tuple3 = value);
        using var subscription12 = model.WhenAnyValue(
                value => value.P1, value => value.P2, value => value.P3, value => value.P4,
                value => value.P5, value => value.P6, value => value.P7, value => value.P8,
                value => value.P9, value => value.P10, value => value.P11, value => value.P12)
            .Subscribe(value => tuple12 = value);

        Assert.Equal((1, 2, 3), tuple3);
        Assert.Equal(12, tuple12.Value12);
    }

    [Fact]
    public void StringSelectorOverloadsCoverTwoAndTwelveValues()
    {
        var model = new ArityModel();
        var pair = new List<string>();
        var twelve = new List<int>();

        using var pairSubscription = model.WhenAnyValue<ArityModel, string, int, int>(
                nameof(ArityModel.P1),
                nameof(ArityModel.P2),
                static (left, right) => $"{left}:{right}")
            .Subscribe(pair.Add);
        using var twelveSubscription = model.WhenAnyValue(
                nameof(ArityModel.P1), nameof(ArityModel.P2), nameof(ArityModel.P3),
                nameof(ArityModel.P4), nameof(ArityModel.P5), nameof(ArityModel.P6),
                nameof(ArityModel.P7), nameof(ArityModel.P8), nameof(ArityModel.P9),
                nameof(ArityModel.P10), nameof(ArityModel.P11), nameof(ArityModel.P12),
                static (int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l) =>
                    a + b + c + d + e + f + g + h + i + j + k + l)
            .Subscribe(twelve.Add);

        model.P12 = 20;

        Assert.Equal(["1:2"], pair);
        Assert.Equal([78, 86], twelve);
    }

    [Fact]
    public void StringTupleOverloadsCoverTwoAndTwelveValues()
    {
        var model = new ArityModel();
        (int Value1, int Value2) pair = default;
        (int Value1, int Value2, int Value3, int Value4, int Value5, int Value6,
            int Value7, int Value8, int Value9, int Value10, int Value11, int Value12) twelve = default;

        using var pairSubscription = model.WhenAnyValue<ArityModel, int, int>(
                nameof(ArityModel.P1), nameof(ArityModel.P2))
            .Subscribe(value => pair = value);
        using var twelveSubscription = model.WhenAnyValue<ArityModel,
                int, int, int, int, int, int, int, int, int, int, int, int>(
                nameof(ArityModel.P1), nameof(ArityModel.P2), nameof(ArityModel.P3),
                nameof(ArityModel.P4), nameof(ArityModel.P5), nameof(ArityModel.P6),
                nameof(ArityModel.P7), nameof(ArityModel.P8), nameof(ArityModel.P9),
                nameof(ArityModel.P10), nameof(ArityModel.P11), nameof(ArityModel.P12))
            .Subscribe(value => twelve = value);

        Assert.Equal((1, 2), pair);
        Assert.Equal(12, twelve.Value12);
    }

    [Fact]
    public void ExpressionSelectorUsesReactiveUiGenericOrdering()
    {
        var model = new ArityModel();
        var values = new List<string>();

        using var subscription = model.WhenAnyValue<ArityModel, string, int, int>(
                value => value.P1,
                value => value.P2,
                static (left, right) => $"{left}:{right}")
            .Subscribe(values.Add);
        model.P2 = 7;

        Assert.Equal(["1:2", "1:7"], values);
    }

    [Fact]
    public void GeneratedExpressionAndStringSelectorsDoNotDistinctProjectedResults()
    {
        var model = new ArityModel();
        var expressionValues = new List<string>();
        var stringValues = new List<string>();

        using var expression = model.WhenAnyValue(
                value => value.P1, value => value.P2, value => value.P3, value => value.P4,
                value => value.P5, value => value.P6, value => value.P7, value => value.P8,
                value => value.P9, value => value.P10, value => value.P11, value => value.P12,
                static (_, _, _, _, _, _, _, _, _, _, _, _) => "same")
            .Subscribe(expressionValues.Add);
        using var byName = model.WhenAnyValue(
                nameof(ArityModel.P1), nameof(ArityModel.P2), nameof(ArityModel.P3),
                nameof(ArityModel.P4), nameof(ArityModel.P5), nameof(ArityModel.P6),
                nameof(ArityModel.P7), nameof(ArityModel.P8), nameof(ArityModel.P9),
                nameof(ArityModel.P10), nameof(ArityModel.P11), nameof(ArityModel.P12),
                static (int _, int _, int _, int _, int _, int _, int _, int _, int _, int _, int _, int _) =>
                    "same")
            .Subscribe(stringValues.Add);

        model.P1 = 20;
        model.P12 = 30;
        model.P12 = 30;

        Assert.Equal(["same", "same", "same"], expressionValues);
        Assert.Equal(expressionValues, stringValues);
    }

    [Fact]
    public void GeneratedTypedSelectorRetainsProjectedResultDistinctness()
    {
        var model = new ArityModel();
        var values = new List<string>();

        using var subscription = model.WhenAnyValue(
                Paths.P1,
                Paths.P2,
                Paths.P3,
                static (_, _, _) => "same")
            .Subscribe(values.Add);

        model.P1 = 20;
        model.P2 = 30;

        Assert.Equal(["same"], values);
    }

    private static void AssertInitial(IObservable<int> observable, int expected)
    {
        var actual = 0;
        using var subscription = observable.Subscribe(value => actual = value);
        Assert.Equal(expected, actual);
    }

    private static class Paths
    {
        public static readonly PropertyPath<ArityModel, int> P1 = Create(nameof(ArityModel.P1), static value => value.P1);
        public static readonly PropertyPath<ArityModel, int> P2 = Create(nameof(ArityModel.P2), static value => value.P2);
        public static readonly PropertyPath<ArityModel, int> P3 = Create(nameof(ArityModel.P3), static value => value.P3);
        public static readonly PropertyPath<ArityModel, int> P4 = Create(nameof(ArityModel.P4), static value => value.P4);
        public static readonly PropertyPath<ArityModel, int> P5 = Create(nameof(ArityModel.P5), static value => value.P5);
        public static readonly PropertyPath<ArityModel, int> P6 = Create(nameof(ArityModel.P6), static value => value.P6);
        public static readonly PropertyPath<ArityModel, int> P7 = Create(nameof(ArityModel.P7), static value => value.P7);
        public static readonly PropertyPath<ArityModel, int> P8 = Create(nameof(ArityModel.P8), static value => value.P8);
        public static readonly PropertyPath<ArityModel, int> P9 = Create(nameof(ArityModel.P9), static value => value.P9);
        public static readonly PropertyPath<ArityModel, int> P10 = Create(nameof(ArityModel.P10), static value => value.P10);
        public static readonly PropertyPath<ArityModel, int> P11 = Create(nameof(ArityModel.P11), static value => value.P11);
        public static readonly PropertyPath<ArityModel, int> P12 = Create(nameof(ArityModel.P12), static value => value.P12);

        private static PropertyPath<ArityModel, int> Create(
            string name,
            Func<ArityModel, int> getter) => PropertyPath.Create(name, getter);
    }

    private sealed class ArityModel : INotifyPropertyChanged
    {
        private readonly int[] _values = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

        public event PropertyChangedEventHandler? PropertyChanged;

        public int P1 { get => Get(); set => Set(value); }
        public int P2 { get => Get(); set => Set(value); }
        public int P3 { get => Get(); set => Set(value); }
        public int P4 { get => Get(); set => Set(value); }
        public int P5 { get => Get(); set => Set(value); }
        public int P6 { get => Get(); set => Set(value); }
        public int P7 { get => Get(); set => Set(value); }
        public int P8 { get => Get(); set => Set(value); }
        public int P9 { get => Get(); set => Set(value); }
        public int P10 { get => Get(); set => Set(value); }
        public int P11 { get => Get(); set => Set(value); }
        public int P12 { get => Get(); set => Set(value); }

        private int Get([CallerMemberName] string propertyName = "") =>
            _values[int.Parse(propertyName.AsSpan(1), CultureInfo.InvariantCulture) - 1];

        private void Set(int value, [CallerMemberName] string propertyName = "")
        {
            _values[int.Parse(propertyName.AsSpan(1), CultureInfo.InvariantCulture) - 1] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
