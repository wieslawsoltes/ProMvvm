using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ProMvvm.Tests;

[SuppressMessage("Trimming", "IL2026", Justification = "Expression adapter compatibility is intentional.")]
public sealed class NotificationAdapterTests
{
    [Fact]
    public void TypedAndExpressionSinglePropertiesUseExplicitAdapter()
    {
        var model = new CustomRoot { Value = 1 };
        var adapter = CreateRootAdapter(emitSynchronously: true);
        var typed = new List<int>();
        var expression = new List<int>();

        using var typedSubscription = model.WhenAnyValue(
            static value => value.Value,
            nameof(CustomRoot.Value),
            adapter).Subscribe(typed.Add);
        using var expressionSubscription = model.WhenAnyValue(
            value => value.Value,
            adapter).Subscribe(expression.Add);

        model.Raise(nameof(CustomRoot.Child));
        model.Value = 2;
        model.Raise(null);
        model.Raise(string.Empty);

        Assert.Equal([1, 2], typed);
        Assert.Equal(typed, expression);
        Assert.Equal(2, model.SubscriberCount);

        typedSubscription.Dispose();
        typedSubscription.Dispose();
        Assert.Equal(1, model.SubscriberCount);
    }

    [Fact]
    public void NestedAdapterPathRewiresAcrossDifferentNotificationTypes()
    {
        var oldChild = new CustomChild { Name = "one" };
        var newChild = new CustomChild { Name = "two" };
        var model = new CustomRoot { Child = oldChild };
        var unsupported = PropertyNotificationAdapters.Create<string>(static (_, _) => null);
        var adapter = PropertyNotificationAdapters.FirstSupported(
            unsupported,
            CreateRootAdapter(emitSynchronously: true),
            CreateChildAdapter(emitSynchronously: true));
        var path = PropertyPath
            .Create<CustomRoot, CustomChild?>(nameof(CustomRoot.Child), static value => value.Child)
            .Then(nameof(CustomChild.Name), static value => value!.Name);
        var values = new List<string>();

        using var subscription = model.WhenAnyValue(path, adapter).Subscribe(values.Add);
        oldChild.Name = "changed";
        model.Child = newChild;
        oldChild.Name = "ignored";
        model.Child = null;
        model.Child = newChild;

        Assert.Equal(["one", "changed", "two"], values);
        Assert.Equal(1, model.SubscriberCount);
        Assert.Equal(1, newChild.SubscriberCount);
        Assert.Equal(0, oldChild.SubscriberCount);
    }

    [Fact]
    public void ObservableAndInpcFactoriesAdaptNotifications()
    {
        var model = new PlainAdapterModel { Value = 1 };
        var changes = new ControlledNameObservable();
        var observableAdapter = PropertyNotificationAdapters.FromObservable<PlainAdapterModel>(_ => changes);
        var observableValues = new List<int>();

        using (model.WhenAnyValue(
                   static value => value.Value,
                   nameof(PlainAdapterModel.Value),
                   observableAdapter).Subscribe(observableValues.Add))
        {
            model.Value = 2;
            changes.Next(nameof(PlainAdapterModel.Other));
            changes.Next(nameof(PlainAdapterModel.Value));
            changes.Error(new InvalidOperationException("notification stream ended"));
            changes.Complete();
        }

        var inpc = new ObservableModel { Count = 3 };
        var inpcValues = new List<int>();
        var inpcSubscription = inpc.WhenAnyValue(
            static value => value.Count,
            nameof(ObservableModel.Count),
            PropertyNotificationAdapters.Inpc).Subscribe(inpcValues.Add);
        inpc.Count = 4;
        inpcSubscription.Dispose();
        inpcSubscription.Dispose();

        Assert.Equal([1, 2], observableValues);
        Assert.Equal([3, 4], inpcValues);
        Assert.True(changes.IsDisposed);
    }

    [Fact]
    public void InpcAdapterSubscriptionDisposesIdempotently()
    {
        var model = new ObservableModel();
        var notifications = 0;
        var subscription = PropertyNotificationAdapters.Inpc.Subscribe(
            model,
            _ => notifications++);

        Assert.NotNull(subscription);
        Assert.Equal(1, model.SubscriberCount);

        subscription.Dispose();
        subscription.Dispose();
        model.Count = 1;

        Assert.Equal(0, model.SubscriberCount);
        Assert.Equal(0, notifications);
    }

    [Fact]
    public void GeneralAdapterPathIgnoresCallbacksRetainedAfterDisposal()
    {
        var model = new CustomRoot { Child = new CustomChild { Name = "initial" } };
        var adapter = new RetainingAdapter();
        var path = PropertyPath
            .Create<CustomRoot, CustomChild?>(nameof(CustomRoot.Child), static value => value.Child)
            .Then(nameof(CustomChild.Name), static value => value!.Name);
        var values = new List<string>();
        var subscription = model.WhenAnyValue(path, adapter).Subscribe(values.Add);

        subscription.Dispose();
        subscription.Dispose();
        adapter.Raise(null);

        Assert.Equal(["initial"], values);
    }

    [Fact]
    public void UnsupportedAdapterStillPublishesInitialValue()
    {
        var model = new CustomRoot { Value = 5 };
        var adapter = PropertyNotificationAdapters.FirstSupported(
            PropertyNotificationAdapters.Create<string>(static (_, _) => null));
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(
            static value => value.Value,
            nameof(CustomRoot.Value),
            adapter).Subscribe(values.Add);
        model.Value = 6;

        Assert.Equal([5], values);
    }

    [Fact]
    public void MultiPropertyAdaptersSupportTypedExpressionAndArityTwelve()
    {
        var model = new CustomRoot { Value = 1 };
        var adapter = CreateRootAdapter();
        var path = PropertyPath.Create<CustomRoot, int>(
            nameof(CustomRoot.Value), static value => value.Value);
        var typed = new List<int>();
        var expression = new List<int>();
        var typedTuples = new List<(int, int)>();
        var expressionTuples = new List<(int, int)>();
        var typedTwelve = new List<int>();
        var expressionTwelve = new List<int>();

        using var typedSubscription = model.WhenAnyValue(
            path, path, static (left, right) => left + right, adapter).Subscribe(typed.Add);
        using var expressionSubscription = model.WhenAnyValue(
            value => value.Value, value => value.Value,
            static (left, right) => left + right, adapter).Subscribe(expression.Add);
        using var typedTupleSubscription = model.WhenAnyValue(path, path, adapter)
            .Subscribe(typedTuples.Add);
        using var expressionTupleSubscription = model.WhenAnyValue(
                value => value.Value, value => value.Value, adapter)
            .Subscribe(expressionTuples.Add);
        using var typedTwelveSubscription = model.WhenAnyValue(
            path, path, path, path, path, path, path, path, path, path, path, path,
            static (a, b, c, d, e, f, g, h, i, j, k, l) =>
                a + b + c + d + e + f + g + h + i + j + k + l,
            adapter).Subscribe(typedTwelve.Add);
        using var expressionTwelveSubscription = model.WhenAnyValue(
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            value => value.Value, value => value.Value, value => value.Value,
            static (a, b, c, d, e, f, g, h, i, j, k, l) =>
                a + b + c + d + e + f + g + h + i + j + k + l,
            adapter).Subscribe(expressionTwelve.Add);

        model.Value = 2;

        Assert.Equal(4, typed[^1]);
        Assert.Equal(typed[^1], expression[^1]);
        Assert.Equal((2, 2), typedTuples[^1]);
        Assert.Equal(typedTuples[^1], expressionTuples[^1]);
        Assert.Equal(24, typedTwelve[^1]);
        Assert.Equal(typedTwelve[^1], expressionTwelve[^1]);
    }

    [Fact]
    public void AdapterSinglePropertyPropagatesGetterAndObserverFailures()
    {
        var model = new CustomRoot();
        var adapter = CreateRootAdapter();
        Exception? error = null;
        Func<CustomRoot, int> throwingGetter = static _ =>
            throw new InvalidOperationException("getter");

        using var getterFailure = model.WhenAnyValue(
                throwingGetter,
                nameof(CustomRoot.Value),
                adapter)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);

        Assert.Throws<TestObserverException>(() => model.WhenAnyValue(
                static value => value.Value,
                nameof(CustomRoot.Value),
                adapter)
            .Subscribe(new ThrowingObserver<int>()));
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void AdapterFactoriesAndOverloadsValidateArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PropertyNotificationAdapters.Create<CustomRoot>(null!));
        Assert.Throws<ArgumentNullException>(() =>
            PropertyNotificationAdapters.FromObservable<CustomRoot>(null!));
        Assert.Throws<ArgumentNullException>(() =>
            PropertyNotificationAdapters.FirstSupported(null!));
        Assert.Throws<ArgumentException>(() =>
            PropertyNotificationAdapters.FirstSupported());
        Assert.Throws<ArgumentException>(() =>
            PropertyNotificationAdapters.FirstSupported([null!]));

        var adapter = CreateRootAdapter();
        Assert.Throws<ArgumentNullException>(() => adapter.Subscribe(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => adapter.Subscribe(new CustomRoot(), null!));

        var model = new CustomRoot();
        var path = PropertyPath.Create<CustomRoot, int>(
            nameof(CustomRoot.Value), static value => value.Value);
        CustomRoot nullModel = null!;

        Assert.Throws<ArgumentNullException>(() => nullModel.WhenAnyValue(path, adapter));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue((PropertyPath<CustomRoot, int>)null!, adapter));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue(path, null!));
        Assert.Throws<ArgumentException>(() => model.WhenAnyValue(static value => value.Value, " ", adapter));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue((Func<CustomRoot, int>)null!, "Value", adapter));
        Assert.Throws<ArgumentNullException>(() => nullModel.WhenAnyValue(static value => value.Value, "Value", adapter));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue(static value => value.Value, "Value", null!));
        Assert.Throws<ArgumentNullException>(() => nullModel.WhenAnyValue(value => value.Value, adapter));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue(
            (System.Linq.Expressions.Expression<Func<CustomRoot, int>>)(value => value.Value),
            null!));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue((System.Linq.Expressions.Expression<Func<CustomRoot, int>>)null!, adapter));
        Assert.Throws<ArgumentNullException>(() => model.WhenAnyValue(path, path, static (left, right) => left + right, null!));
    }

    private static IPropertyNotificationAdapter CreateRootAdapter(bool emitSynchronously = false) =>
        PropertyNotificationAdapters.Create<CustomRoot>((source, callback) =>
        {
            if (emitSynchronously)
            {
                callback(nameof(CustomRoot.Value));
            }

            return source.Subscribe(callback);
        });

    private static IPropertyNotificationAdapter CreateChildAdapter(bool emitSynchronously = false) =>
        PropertyNotificationAdapters.Create<CustomChild>((source, callback) =>
        {
            if (emitSynchronously)
            {
                callback(nameof(CustomChild.Name));
            }

            return source.Subscribe(callback);
        });

    private sealed class CustomRoot
    {
        private Action<string?>? _changed;
        private int _value;
        private CustomChild? _child;

        public int SubscriberCount { get; private set; }

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                Raise(nameof(Value));
            }
        }

        public CustomChild? Child
        {
            get => _child;
            set
            {
                _child = value;
                Raise(nameof(Child));
            }
        }

        public ActionDisposable Subscribe(Action<string?> callback)
        {
            _changed += callback;
            SubscriberCount++;
            return new ActionDisposable(() =>
            {
                _changed -= callback;
                SubscriberCount--;
            });
        }

        public void Raise(string? name) => _changed?.Invoke(name);
    }

    private sealed class CustomChild
    {
        private Action<string?>? _changed;
        private string _name = string.Empty;

        public int SubscriberCount { get; private set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _changed?.Invoke(nameof(Name));
            }
        }

        public ActionDisposable Subscribe(Action<string?> callback)
        {
            _changed += callback;
            SubscriberCount++;
            return new ActionDisposable(() =>
            {
                _changed -= callback;
                SubscriberCount--;
            });
        }
    }

    private sealed class PlainAdapterModel
    {
        public int Value { get; set; }

        public int Other { get; set; }
    }

    private sealed class ControlledNameObservable : IObservable<string?>
    {
        private IObserver<string?>? _observer;

        public bool IsDisposed { get; private set; }

        public IDisposable Subscribe(IObserver<string?> observer)
        {
            _observer = observer;
            return new ActionDisposable(() => IsDisposed = true);
        }

        public void Next(string? name) => _observer?.OnNext(name);

        public void Error(Exception error) => _observer?.OnError(error);

        public void Complete() => _observer?.OnCompleted();
    }

    private sealed class RetainingAdapter : IPropertyNotificationAdapter
    {
        private readonly List<Action<string?>> _callbacks = [];

        public IDisposable Subscribe(object source, Action<string?> onPropertyChanged)
        {
            _callbacks.Add(onPropertyChanged);
            return new ActionDisposable(static () => { });
        }

        public void Raise(string? propertyName)
        {
            foreach (var callback in _callbacks)
            {
                callback(propertyName);
            }
        }
    }

    private sealed class ActionDisposable(Action callback) : IDisposable
    {
        private Action? _callback = callback;

        public void Dispose() => Interlocked.Exchange(ref _callback, null)?.Invoke();
    }
}
