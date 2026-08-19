using System.ComponentModel;

namespace ProMvvm;

/// <summary>Creates explicit notification adapters without global registration or service lookup.</summary>
public static class PropertyNotificationAdapters
{
    /// <summary>An explicit adapter for <see cref="INotifyPropertyChanged"/> sources.</summary>
    public static IPropertyNotificationAdapter Inpc { get; } =
        Create<INotifyPropertyChanged>(static (source, callback) =>
        {
            PropertyChangedEventHandler handler = (_, args) => callback(args.PropertyName);
            source.PropertyChanged += handler;
            return new CallbackDisposable(() => source.PropertyChanged -= handler);
        });

    /// <summary>Creates a strongly typed adapter around an event subscription callback.</summary>
    public static IPropertyNotificationAdapter Create<TSource>(
        Func<TSource, Action<string?>, IDisposable?> subscribe)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        return new DelegateAdapter<TSource>(subscribe);
    }

    /// <summary>Adapts an observable stream of changed property names.</summary>
    public static IPropertyNotificationAdapter FromObservable<TSource>(
        Func<TSource, IObservable<string?>> changes)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(changes);
        return Create<TSource>((source, callback) =>
            changes(source).Subscribe(new CallbackObserver(callback)));
    }

    /// <summary>
    /// Tries adapters in order for each object in a property chain and uses the first
    /// adapter that supports that object.
    /// </summary>
    public static IPropertyNotificationAdapter FirstSupported(
        params IPropertyNotificationAdapter[] adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        if (adapters.Length == 0)
        {
            throw new ArgumentException("At least one notification adapter is required.", nameof(adapters));
        }

        if (Array.Exists(adapters, static adapter => adapter is null))
        {
            throw new ArgumentException("Notification adapters cannot contain null.", nameof(adapters));
        }

        return new FirstSupportedAdapter((IPropertyNotificationAdapter[])adapters.Clone());
    }

    private sealed class DelegateAdapter<TSource>(
        Func<TSource, Action<string?>, IDisposable?> subscribe) : IPropertyNotificationAdapter
        where TSource : class
    {
        public IDisposable? Subscribe(object source, Action<string?> onPropertyChanged)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(onPropertyChanged);
            return source is TSource typedSource
                ? subscribe(typedSource, onPropertyChanged)
                : null;
        }
    }

    private sealed class FirstSupportedAdapter(
        IPropertyNotificationAdapter[] adapters) : IPropertyNotificationAdapter
    {
        public IDisposable? Subscribe(object source, Action<string?> onPropertyChanged)
        {
            foreach (var adapter in adapters)
            {
                var subscription = adapter.Subscribe(source, onPropertyChanged);
                if (subscription is not null)
                {
                    return subscription;
                }
            }

            return null;
        }
    }

    private sealed class CallbackObserver(Action<string?> callback) : IObserver<string?>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(string? value) => callback(value);
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        private Action? _callback = callback;

        public void Dispose() => Interlocked.Exchange(ref _callback, null)?.Invoke();
    }
}
