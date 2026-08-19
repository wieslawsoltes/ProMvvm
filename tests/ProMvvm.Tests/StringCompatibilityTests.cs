using System.Diagnostics.CodeAnalysis;

namespace ProMvvm.Tests;

[SuppressMessage("Trimming", "IL2026", Justification = "These tests intentionally cover ReactiveUI-compatible string property lookup.")]
public sealed class StringCompatibilityTests
{
    [Fact]
    public void ObservesPublicPropertyByNameAndFiltersDuplicates()
    {
        var model = new ObservableModel { Count = 1 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue<ObservableModel, int>(nameof(ObservableModel.Count))
            .Subscribe(values.Add);
        model.Count = 2;
        model.Count = 2;
        model.Name = "unrelated";

        Assert.Equal([1, 2], values);
    }

    [Fact]
    public void SupportsInheritedPropertyAndRuntimeDerivedSource()
    {
        ObservableModel model = new DerivedObservableModel { Count = 3 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue<ObservableModel, int>(nameof(ObservableModel.Count))
            .Subscribe(values.Add);
        model.Count = 4;

        Assert.Equal([3, 4], values);
    }

    [Fact]
    public void MissingPropertyUsesDefaultAndCanDisableDistinctFiltering()
    {
        var model = new ObservableModel { Count = 3 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue<ObservableModel, int>("Missing", isDistinct: false)
            .Subscribe(values.Add);
        model.Raise("Missing");
        model.Raise(null);

        Assert.Equal([0, 0, 0], values);
    }

    [Fact]
    public void NullPropertyValueUsesDefault()
    {
        var model = new ObservableModel { Name = null };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue<ObservableModel, string?>(nameof(ObservableModel.Name))
            .Subscribe(values.Add);
        model.Name = "value";

        Assert.Equal([null, "value"], values);
    }

    [Fact]
    public void RuntimeDerivedNullablePropertyUsesReflectionFallback()
    {
        ObservableModel model = new RuntimePropertyObservableModel { RuntimeOnly = null };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue<ObservableModel, string?>(
                nameof(RuntimePropertyObservableModel.RuntimeOnly))
            .Subscribe(values.Add);

        Assert.Equal([null], values);
    }

    [Fact]
    public void IncompatiblePropertyTypeReportsErrorAndDetaches()
    {
        var model = new ObservableModel { Count = 3 };
        Exception? error = null;

        using var subscription = model.WhenAnyValue<ObservableModel, string>(nameof(ObservableModel.Count))
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidCastException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void SinglePropertySelectorsMatchReactiveUiGenericOrdering()
    {
        var model = new ObservableModel { Count = 4 };
        var expressionValues = new List<string>();
        var stringValues = new List<string>();

        using var expression = model.WhenAnyValue<ObservableModel, string, int>(
            value => value.Count,
            static value => value.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Subscribe(expressionValues.Add);
        using var byName = model.WhenAnyValue<ObservableModel, string, int>(
            nameof(ObservableModel.Count),
            static value => value.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Subscribe(stringValues.Add);
        model.Count = 5;
        model.Count = 5;
        model.Name = "unrelated";

        Assert.Equal(["4", "5"], expressionValues);
        Assert.Equal(expressionValues, stringValues);
    }

    [Fact]
    public void ProjectedStringObservationCanDisableDistinctFiltering()
    {
        var model = new ObservableModel { Count = 2 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue<ObservableModel, int, int>(
                nameof(ObservableModel.Count),
                static value => value * 2,
                isDistinct: false)
            .Subscribe(values.Add);
        model.Count = 2;

        Assert.Equal([4, 4], values);
    }

    [Fact]
    public void NestedExpressionProjectionUsesPathProjectionFallback()
    {
        var model = new ObservableModel { Child = new ObservableModel { Count = 2 } };
        var values = new List<string>();

        using var subscription = model.WhenAnyValue<ObservableModel, string, int>(
                value => value.Child!.Count,
                static value => $"#{value}")
            .Subscribe(values.Add);
        model.Child.Count = 3;

        Assert.Equal(["#2", "#3"], values);
    }

    [Fact]
    public void ProjectedGetterFailureIsReportedAndDetaches()
    {
        var model = new ObservableModel();
        Exception? error = null;

        using var subscription = model.WhenAnyValue<ObservableModel, string, string>(
                value => value.Throwing,
                static value => value)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void ProjectedPlainObjectEmitsInitialValueOnly()
    {
        var model = new PlainModel { Value = 2 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue<PlainModel, int, int>(
                nameof(PlainModel.Value),
                static value => value * 2)
            .Subscribe(values.Add);
        model.Value = 3;

        Assert.Equal([4], values);
    }

    [Fact]
    public void SelectorFailureIsReportedAndDetaches()
    {
        var model = new ObservableModel { Count = 1 };
        Exception? error = null;

        using var subscription = model.WhenAnyValue<ObservableModel, string, int>(
                nameof(ObservableModel.Count),
                static _ => throw new InvalidOperationException("selector"))
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void SelectorObserverFailurePropagatesAndDetaches()
    {
        var model = new ObservableModel { Count = 1 };
        var observable = model.WhenAnyValue<ObservableModel, int, int>(
            nameof(ObservableModel.Count),
            static value => value);

        Assert.Throws<TestObserverException>(() => observable.Subscribe(new ThrowingObserver<int>()));
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void ValidatesStringCompatibilityArguments()
    {
        var model = new ObservableModel();

        Assert.Throws<ArgumentNullException>(() =>
            WhenAnyValueExtensions.WhenAnyValue<ObservableModel, int>(null!, nameof(ObservableModel.Count)));
        Assert.Throws<ArgumentNullException>(() =>
            model.WhenAnyValue<ObservableModel, int>((string)null!));
        Assert.Throws<ArgumentException>(() =>
            model.WhenAnyValue<ObservableModel, int>(" "));
        Assert.Throws<ArgumentNullException>(() =>
            WhenAnyValueExtensions.WhenAnyValue<ObservableModel, int, int>(
                model,
                nameof(ObservableModel.Count),
                null!));
    }
}
