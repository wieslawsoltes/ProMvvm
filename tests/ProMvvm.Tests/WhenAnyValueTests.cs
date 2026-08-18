namespace ProMvvm.Tests;

public sealed class WhenAnyValueTests
{
    [Fact]
    public void EmitsInitialAndChangedValuesAndFiltersDuplicates()
    {
        var model = new ObservableModel { Name = "initial" };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue(
            static value => value.Name,
            nameof(ObservableModel.Name)).Subscribe(values.Add);

        model.Name = "next";
        model.Name = "next";
        model.Count = 42;

        Assert.Equal(["initial", "next"], values);
        Assert.Equal(1, model.SubscriberCount);
    }

    [Fact]
    public void CanDisableDistinctFiltering()
    {
        var model = new ObservableModel { Count = 3 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(
            static value => value.Count,
            nameof(ObservableModel.Count),
            isDistinct: false).Subscribe(values.Add);

        model.Count = 3;

        Assert.Equal([3, 3], values);
    }

    [Fact]
    public void UsesCustomComparer()
    {
        var model = new ObservableModel { Name = "hello" };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue(
            static value => value.Name,
            nameof(ObservableModel.Name),
            comparer: StringComparer.OrdinalIgnoreCase).Subscribe(values.Add);

        model.Name = "HELLO";
        model.Name = "world";

        Assert.Equal(["hello", "world"], values);
    }

    [Fact]
    public void EmptyPropertyNameNotificationMeansAllProperties()
    {
        var model = new ObservableModel { Count = 1 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(
            static value => value.Count,
            nameof(ObservableModel.Count),
            isDistinct: false).Subscribe(values.Add);

        model.Raise(null);
        model.Raise(string.Empty);

        Assert.Equal([1, 1, 1], values);
    }

    [Fact]
    public void PlainObjectEmitsInitialValueOnly()
    {
        var model = new PlainModel { Value = 4 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(
            static value => value.Value,
            nameof(PlainModel.Value)).Subscribe(values.Add);

        model.Value = 8;

        Assert.Equal([4], values);
    }

    [Fact]
    public void ObservableIsColdAndDisposalUnsubscribes()
    {
        var model = new ObservableModel { Count = 1 };
        var observable = model.WhenAnyValue(
            static value => value.Count,
            nameof(ObservableModel.Count));

        Assert.Equal(0, model.SubscriberCount);

        var firstValues = new List<int>();
        var secondValues = new List<int>();
        var first = observable.Subscribe(firstValues.Add);
        var second = observable.Subscribe(secondValues.Add);
        Assert.Equal(2, model.SubscriberCount);

        first.Dispose();
        first.Dispose();
        model.Count = 2;
        Assert.Equal(1, model.SubscriberCount);
        Assert.Equal([1], firstValues);
        Assert.Equal([1, 2], secondValues);

        second.Dispose();
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void GetterFailureIsReportedAndUnsubscribed()
    {
        var model = new ObservableModel();
        Exception? error = null;

        using var subscription = model.WhenAnyValue(
            static value => value.Throwing,
            nameof(ObservableModel.Throwing)).Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void ObserverFailurePropagatesAndUnsubscribes()
    {
        var model = new ObservableModel();
        var observable = model.WhenAnyValue(
            static value => value.Count,
            nameof(ObservableModel.Count));

        Assert.Throws<TestObserverException>(() => observable.Subscribe(new ThrowingObserver<int>()));
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void PublicOverloadsValidateArguments()
    {
        var model = new ObservableModel();
        var path = PropertyPath.Create<ObservableModel, int>(
            nameof(ObservableModel.Count), static value => value.Count);

        Assert.Throws<ArgumentNullException>(() =>
            WhenAnyValueExtensions.WhenAnyValue<ObservableModel, int>(null!, path));
        Assert.Throws<ArgumentNullException>(() =>
            WhenAnyValueExtensions.WhenAnyValue<ObservableModel, int>(
                null!, static value => value.Count, nameof(ObservableModel.Count)));
        Assert.Throws<ArgumentNullException>(() =>
            model.WhenAnyValue(
                (Func<ObservableModel, int>)null!, nameof(ObservableModel.Count)));
        Assert.Throws<ArgumentException>(() =>
            model.WhenAnyValue(static value => value.Count, string.Empty));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue((PropertyPath<ObservableModel, int>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            model.WhenAnyValue((System.Linq.Expressions.Expression<Func<ObservableModel, int>>)null!));
    }
}
