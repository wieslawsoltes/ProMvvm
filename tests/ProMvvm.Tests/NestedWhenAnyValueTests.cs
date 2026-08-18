namespace ProMvvm.Tests;

public sealed class NestedWhenAnyValueTests
{
    private static readonly PropertyPath<ObservableModel, string?> NamePath =
        PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child),
                static model => model.Child)
            .Then(nameof(ObservableModel.Name), static child => child!.Name);

    [Fact]
    public void TracksLeafAndRewiresWhenIntermediateChanges()
    {
        var oldChild = new ObservableModel { Name = "old" };
        var model = new ObservableModel { Child = oldChild };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue(NamePath).Subscribe(values.Add);
        oldChild.Name = "old-2";

        var newChild = new ObservableModel { Name = "new" };
        model.Child = newChild;
        oldChild.Name = "ignored";
        newChild.Name = "new-2";

        Assert.Equal(["old", "old-2", "new", "new-2"], values);
        Assert.Equal(0, oldChild.SubscriberCount);
        Assert.Equal(1, newChild.SubscriberCount);
    }

    [Fact]
    public void SuppressesNullIntermediateAndEqualRestoredValue()
    {
        var child = new ObservableModel { Name = "same" };
        var model = new ObservableModel { Child = child };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue(NamePath).Subscribe(values.Add);
        model.Child = null;
        model.Child = new ObservableModel { Name = "same" };
        model.Child.Name = "different";

        Assert.Equal(["same", "different"], values);
        Assert.Equal(0, child.SubscriberCount);
    }

    [Fact]
    public void NullLeafIsAValidValue()
    {
        var child = new ObservableModel { Name = null };
        var model = new ObservableModel { Child = child };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue(NamePath).Subscribe(values.Add);
        child.Name = "set";
        child.Name = null;

        Assert.Equal([null, "set", null], values);
    }

    [Fact]
    public void InitiallyNullIntermediateStartsWhenItBecomesValid()
    {
        var model = new ObservableModel();
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue(NamePath).Subscribe(values.Add);
        Assert.Empty(values);

        model.Child = new ObservableModel { Name = "ready" };

        Assert.Equal(["ready"], values);
    }

    [Fact]
    public void NestedGetterFailureIsReportedAndUnsubscribed()
    {
        var child = new ObservableModel();
        var model = new ObservableModel { Child = child };
        var path = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child), static value => value.Child)
            .Then(nameof(ObservableModel.Throwing), static value => value!.Throwing);
        Exception? error = null;

        using var subscription = model.WhenAnyValue(path)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
        Assert.Equal(0, child.SubscriberCount);
    }

    [Fact]
    public void ReentrantNotificationAfterFailureIsIgnored()
    {
        var model = new ObservableModel { Count = -1 };
        model.Child = model;
        var path = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child), static value => value.Child)
            .Then(nameof(ObservableModel.Throwing), static value => value!.Throwing);
        Exception? error = null;

        using var subscription = model.WhenAnyValue(path)
            .Subscribe(_ => { }, value => error = value);

        model.Count = 0;
        model.Raise(null);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void NestedObserverFailurePropagatesAndUnsubscribes()
    {
        var child = new ObservableModel { Name = "value" };
        var model = new ObservableModel { Child = child };
        var observable = model.WhenAnyValue(NamePath);

        Assert.Throws<TestObserverException>(() =>
            observable.Subscribe(new ThrowingObserver<string?>()));
        Assert.Equal(0, model.SubscriberCount);
        Assert.Equal(0, child.SubscriberCount);
    }

    [Fact]
    public void NestedDisposalIsIdempotent()
    {
        var child = new ObservableModel { Name = "value" };
        var model = new ObservableModel { Child = child };
        var subscription = model.WhenAnyValue(NamePath).Subscribe(_ => { });

        subscription.Dispose();
        subscription.Dispose();

        Assert.Equal(0, model.SubscriberCount);
        Assert.Equal(0, child.SubscriberCount);
    }
}
