namespace ProMvvm.Tests;

public sealed class NestedWhenAnyValueTests
{
    private static readonly PropertyPath<ObservableModel, string?> NamePath =
        PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child),
                static model => model.Child)
            .Then(nameof(ObservableModel.Name), static child => child!.Name);

    private static readonly PropertyPath<ObservableModel, string?> DeepNamePath =
        PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child),
                static model => model.Child)
            .Then(nameof(ObservableModel.Child), static child => child!.Child)
            .Then(nameof(ObservableModel.Name), static child => child!.Name);

    private static readonly PropertyPath<ObservableModel, string?> VeryDeepNamePath =
        PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child),
                static model => model.Child)
            .Then(nameof(ObservableModel.Child), static child => child!.Child)
            .Then(nameof(ObservableModel.Child), static child => child!.Child)
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

    [Fact]
    public void GeneralThreeSegmentPathTracksAndRewiresOnlyItsSuffix()
    {
        var oldLeaf = new ObservableModel { Name = "old" };
        var middle = new ObservableModel { Child = oldLeaf };
        var root = new ObservableModel { Child = middle };
        var values = new List<string?>();
        var subscription = root.WhenAnyValue(DeepNamePath).Subscribe(values.Add);

        root.Raise(nameof(ObservableModel.Count));
        oldLeaf.Name = "changed";
        var newLeaf = new ObservableModel { Name = "new" };
        middle.Child = newLeaf;
        oldLeaf.Name = "ignored";
        middle.Child = null;
        middle.Child = newLeaf;

        subscription.Dispose();
        subscription.Dispose();

        Assert.Equal(["old", "changed", "new"], values);
        Assert.Equal(0, root.SubscriberCount);
        Assert.Equal(0, middle.SubscriberCount);
        Assert.Equal(0, oldLeaf.SubscriberCount);
        Assert.Equal(0, newLeaf.SubscriberCount);
    }

    [Fact]
    public void ThreeSegmentPathTracksAndRewiresEveryLevel()
    {
        var oldLeaf = new ObservableModel { Name = "old" };
        var oldMiddle = new ObservableModel { Child = oldLeaf };
        var root = new ObservableModel { Child = oldMiddle };
        var values = new List<string?>();

        using var subscription = root.WhenAnyValue(DeepNamePath).Subscribe(values.Add);
        root.Raise(nameof(ObservableModel.Count));
        oldMiddle.Raise(nameof(ObservableModel.Count));
        oldLeaf.Raise(nameof(ObservableModel.Count));
        oldLeaf.Name = "leaf";

        var newLeaf = new ObservableModel { Name = "middle" };
        oldMiddle.Child = newLeaf;
        oldLeaf.Name = "ignored-leaf";

        var newMiddle = new ObservableModel
        {
            Child = new ObservableModel { Name = "root" },
        };
        root.Child = newMiddle;
        newLeaf.Name = "ignored-middle";
        root.Raise(null);
        newMiddle.Raise(string.Empty);
        newMiddle.Child!.Raise(null);

        Assert.Equal(["old", "leaf", "middle", "root"], values);
        Assert.Equal(0, oldMiddle.SubscriberCount);
        Assert.Equal(0, oldLeaf.SubscriberCount);
        Assert.Equal(0, newLeaf.SubscriberCount);
        Assert.Equal(1, newMiddle.SubscriberCount);
        Assert.Equal(1, newMiddle.Child.SubscriberCount);
    }

    [Fact]
    public void ThreeSegmentPathHandlesNullsAndCanDisableDistinctness()
    {
        var root = new ObservableModel();
        var values = new List<string?>();
        using var subscription = root.WhenAnyValue(DeepNamePath, isDistinct: false)
            .Subscribe(values.Add);

        Assert.Empty(values);

        var middle = new ObservableModel();
        root.Child = middle;
        Assert.Empty(values);

        var leaf = new ObservableModel { Name = "ready" };
        middle.Child = leaf;
        leaf.Raise(nameof(ObservableModel.Name));
        middle.Child = null;
        middle.Child = leaf;

        Assert.Equal(["ready", "ready", "ready"], values);
    }

    [Fact]
    public void ThreeSegmentPathSupportsNonNotifyingIntermediatesAndRoot()
    {
        var observableRoot = new ObservableModel();
        var path = PropertyPath.Create<ObservableModel, PlainModel>(
                "Plain",
                static _ => new PlainModel { Value = 7 })
            .Then(nameof(PlainModel.Value), static value => value.Value)
            .Then("Text", static value => value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var values = new List<string>();

        using var subscription = observableRoot.WhenAnyValue(path).Subscribe(values.Add);

        var plainRoot = new PlainThreeSegmentRoot();
        var plainValues = new List<int>();
        var plainPath = PropertyPath.Create<PlainThreeSegmentRoot, PlainThreeSegmentMiddle>(
                nameof(PlainThreeSegmentRoot.Middle), static value => value.Middle)
            .Then(nameof(PlainThreeSegmentMiddle.Leaf), static value => value.Leaf)
            .Then(nameof(PlainModel.Value), static value => value.Value);
        using var plainSubscription = plainRoot.WhenAnyValue(plainPath).Subscribe(plainValues.Add);

        Assert.Equal(["7"], values);
        Assert.Equal([11], plainValues);
    }

    [Fact]
    public void ThreeSegmentFailuresAtEveryLevelStopAllWatchers()
    {
        var leaf = new ObservableModel { Count = -1 };
        var middle = new ObservableModel { Count = -1, Child = leaf };
        var root = new ObservableModel { Count = -1, Child = middle };

        var rootFailure = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child),
                value => value.Count >= 0
                    ? throw new InvalidOperationException("root")
                    : value.Child)
            .Then(nameof(ObservableModel.Child), static value => value!.Child)
            .Then(nameof(ObservableModel.Name), static value => value!.Name);
        Exception? rootError = null;
        using var rootSubscription = root.WhenAnyValue(rootFailure)
            .Subscribe(_ => { }, error => rootError = error);
        root.Count = 0;
        root.Raise(nameof(ObservableModel.Child));

        Assert.IsType<InvalidOperationException>(rootError);
        Assert.Equal(0, root.SubscriberCount);

        root.Count = -1;
        var middleFailure = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child), static value => value.Child)
            .Then(nameof(ObservableModel.Throwing), static value => value!.Throwing)
            .Then(nameof(string.Length), static value => value.Length);
        Exception? middleError = null;
        using var middleSubscription = root.WhenAnyValue(middleFailure)
            .Subscribe(_ => { }, error => middleError = error);
        middle.Count = 0;
        middle.Raise(nameof(ObservableModel.Throwing));

        Assert.IsType<InvalidOperationException>(middleError);
        Assert.Equal(0, root.SubscriberCount);
        Assert.Equal(0, middle.SubscriberCount);

        middle.Count = -1;
        var leafFailure = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child), static value => value.Child)
            .Then(nameof(ObservableModel.Child), static value => value!.Child)
            .Then(nameof(ObservableModel.Throwing), static value => value!.Throwing);
        Exception? leafError = null;
        using var leafSubscription = root.WhenAnyValue(leafFailure)
            .Subscribe(_ => { }, error => leafError = error);
        leaf.Count = 0;
        leaf.Raise(nameof(ObservableModel.Throwing));

        Assert.IsType<InvalidOperationException>(leafError);
        Assert.Equal(0, root.SubscriberCount);
        Assert.Equal(0, middle.SubscriberCount);
        Assert.Equal(0, leaf.SubscriberCount);
    }

    [Fact]
    public void ThreeSegmentSubscriptionValidatesObserver()
    {
        var root = new ObservableModel
        {
            Child = new ObservableModel
            {
                Child = new ObservableModel(),
            },
        };
        var observable = root.WhenAnyValue(DeepNamePath);

        Assert.Throws<ArgumentNullException>(() => observable.Subscribe(null!));
    }

    [Fact]
    public void GeneralThreeSegmentFailuresStopAllWatchers()
    {
        var leaf = new ObservableModel { Name = "value" };
        var middle = new ObservableModel { Child = leaf };
        var root = new ObservableModel { Child = middle };

        Assert.Throws<TestObserverException>(() =>
            root.WhenAnyValue(DeepNamePath).Subscribe(new ThrowingObserver<string?>()));

        var throwingPath = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child), static value => value.Child)
            .Then(nameof(ObservableModel.Child), static value => value!.Child)
            .Then(nameof(ObservableModel.Throwing), static value => value!.Throwing);
        Exception? error = null;
        using var subscription = root.WhenAnyValue(throwingPath)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, root.SubscriberCount);
        Assert.Equal(0, middle.SubscriberCount);
        Assert.Equal(0, leaf.SubscriberCount);
    }

    [Fact]
    public void GeneralFourSegmentPathTracksRewiresAndDisposes()
    {
        var oldLeaf = new ObservableModel { Name = "old" };
        var second = new ObservableModel { Child = oldLeaf };
        var first = new ObservableModel { Child = second };
        var root = new ObservableModel { Child = first };
        var values = new List<string?>();
        var subscription = root.WhenAnyValue(VeryDeepNamePath).Subscribe(values.Add);

        oldLeaf.Name = "changed";
        var newLeaf = new ObservableModel { Name = "new" };
        second.Child = newLeaf;
        oldLeaf.Name = "ignored";

        subscription.Dispose();
        subscription.Dispose();

        Assert.Equal(["old", "changed", "new"], values);
        Assert.Equal(0, root.SubscriberCount);
        Assert.Equal(0, first.SubscriberCount);
        Assert.Equal(0, second.SubscriberCount);
        Assert.Equal(0, oldLeaf.SubscriberCount);
        Assert.Equal(0, newLeaf.SubscriberCount);
    }

    [Fact]
    public void GeneralFourSegmentFailuresStopAllWatchers()
    {
        var leaf = new ObservableModel { Name = "value" };
        var second = new ObservableModel { Child = leaf };
        var first = new ObservableModel { Child = second };
        var root = new ObservableModel { Child = first };

        Assert.Throws<TestObserverException>(() =>
            root.WhenAnyValue(VeryDeepNamePath).Subscribe(new ThrowingObserver<string?>()));

        var throwingPath = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child), static value => value.Child)
            .Then(nameof(ObservableModel.Child), static value => value!.Child)
            .Then(nameof(ObservableModel.Child), static value => value!.Child)
            .Then(nameof(ObservableModel.Throwing), static value => value!.Throwing);
        Exception? error = null;
        using var subscription = root.WhenAnyValue(throwingPath)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, root.SubscriberCount);
        Assert.Equal(0, first.SubscriberCount);
        Assert.Equal(0, second.SubscriberCount);
        Assert.Equal(0, leaf.SubscriberCount);
    }

    [Fact]
    public void TwoSegmentRootGetterFailureIsReported()
    {
        var model = new ObservableModel();
        var path = PropertyPath.Create<ObservableModel, ObservableModel?>(
                nameof(ObservableModel.Child),
                static _ => throw new InvalidOperationException("root getter"))
            .Then(nameof(ObservableModel.Name), static value => value!.Name);
        Exception? error = null;

        using var subscription = model.WhenAnyValue(path)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }
}
