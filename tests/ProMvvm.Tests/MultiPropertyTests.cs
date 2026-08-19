namespace ProMvvm.Tests;

public sealed class MultiPropertyTests
{
    private static readonly PropertyPath<ObservableModel, string?> NamePath =
        PropertyPath.Create<ObservableModel, string?>(
            nameof(ObservableModel.Name), static model => model.Name);

    private static readonly PropertyPath<ObservableModel, int> CountPath =
        PropertyPath.Create<ObservableModel, int>(
            nameof(ObservableModel.Count), static model => model.Count);

    [Fact]
    public void ProjectsInitialAndLatestValues()
    {
        var model = new ObservableModel { Name = "items", Count = 1 };
        var values = new List<string>();

        using var subscription = model.WhenAnyValue(
            NamePath,
            CountPath,
            static (name, count) => $"{name}:{count}").Subscribe(values.Add);

        model.Count = 2;
        model.Name = "things";

        Assert.Equal(["items:1", "items:2", "things:2"], values);
    }

    [Fact]
    public void TupleOverloadEmitsValueTuple()
    {
        var model = new ObservableModel { Name = "items", Count = 1 };
        var values = new List<(string?, int)>();

        using var subscription = model.WhenAnyValue(NamePath, CountPath).Subscribe(values.Add);
        model.Count = 2;

        Assert.Equal([("items", 1), ("items", 2)], values);
    }

    [Fact]
    public void ResultDistinctnessIsAppliedAfterProjection()
    {
        var model = new ObservableModel { Name = "a", Count = 1 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(
            NamePath,
            CountPath,
            static (name, count) => (name?.Length ?? 0) + count).Subscribe(values.Add);

        model.Name = "b";
        model.Count = 2;

        Assert.Equal([2, 3], values);
    }

    [Fact]
    public void CanDisableInputAndResultDistinctness()
    {
        var model = new ObservableModel { Name = "a", Count = 1 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(
            NamePath,
            CountPath,
            static (name, count) => (name?.Length ?? 0) + count,
            isDistinct: false).Subscribe(values.Add);

        model.Name = "a";

        Assert.Equal([2, 2], values);
    }

    [Fact]
    public void ExpressionTupleOverloadEmitsValueTuple()
    {
        var model = new ObservableModel { Name = "items", Count = 1 };
        var values = new List<(string?, int)>();

#pragma warning disable IL2026
        using var subscription = model.WhenAnyValue(
            value => value.Name,
            value => value.Count).Subscribe(values.Add);
#pragma warning restore IL2026
        model.Count = 2;

        Assert.Equal([("items", 1), ("items", 2)], values);
    }

    [Fact]
    public void ExpressionSelectorOverloadProjectsValues()
    {
        var model = new ObservableModel { Name = "items", Count = 1 };
        var values = new List<string>();

#pragma warning disable IL2026
        using var subscription = model.WhenAnyValue(
            value => value.Name,
            value => value.Count,
            static (name, count) => $"{name}:{count}").Subscribe(values.Add);
#pragma warning restore IL2026
        model.Count = 2;

        Assert.Equal(["items:1", "items:2"], values);
    }

    [Fact]
    public void ExpressionAndStringSelectorsDistinctInputsButNotProjectedResults()
    {
        var model = new ObservableModel { Name = "a", Count = 1 };
        var expressionValues = new List<string>();
        var stringValues = new List<string>();

#pragma warning disable IL2026
        using var expression = model.WhenAnyValue(
            value => value.Name,
            value => value.Count,
            static (_, _) => "same").Subscribe(expressionValues.Add);
        using var byName = model.WhenAnyValue<ObservableModel, string, string?, int>(
            nameof(ObservableModel.Name),
            nameof(ObservableModel.Count),
            static (_, _) => "same").Subscribe(stringValues.Add);
#pragma warning restore IL2026

        model.Name = "b";
        model.Count = 2;
        model.Count = 2;

        Assert.Equal(["same", "same", "same"], expressionValues);
        Assert.Equal(expressionValues, stringValues);
    }

    [Fact]
    public void ExpressionComparerExplicitlyOptsIntoProjectedResultDistinctness()
    {
        var model = new ObservableModel { Name = "a", Count = 1 };
        var values = new List<string>();

#pragma warning disable IL2026
        using var subscription = model.WhenAnyValue(
            value => value.Name,
            value => value.Count,
            static (_, _) => "same",
            comparer: StringComparer.Ordinal).Subscribe(values.Add);
#pragma warning restore IL2026

        model.Name = "b";
        model.Count = 2;

        Assert.Equal(["same"], values);
    }

    [Fact]
    public void SelectorFailureTerminatesBothSources()
    {
        var model = new ObservableModel { Name = "a", Count = 1 };
        Exception? error = null;

        using var subscription = model.WhenAnyValue(
            NamePath,
            CountPath,
            (Func<string?, int, int>)(static (_, _) =>
                throw new InvalidOperationException("selector")))
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void SourceGetterFailureTerminatesCombinedObservation()
    {
        var model = new ObservableModel { Count = 1 };
        var throwingPath = PropertyPath.Create<ObservableModel, string>(
            nameof(ObservableModel.Throwing), static value => value.Throwing);
        Exception? error = null;

        using var subscription = model.WhenAnyValue(
                throwingPath,
                CountPath,
                static (text, count) => text.Length + count)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void ValidatesArguments()
    {
        var model = new ObservableModel();

        Assert.Throws<ArgumentNullException>(() =>
            model.WhenAnyValue(
                (PropertyPath<ObservableModel, string?>)null!,
                CountPath,
                static (string? name, int count) => count));
        Assert.Throws<ArgumentNullException>(() =>
            model.WhenAnyValue(
                NamePath,
                (PropertyPath<ObservableModel, int>)null!,
                static (string? name, int count) => count));
        Assert.Throws<ArgumentNullException>(() =>
            model.WhenAnyValue(NamePath, CountPath, (Func<string?, int, int>)null!));
    }
}
