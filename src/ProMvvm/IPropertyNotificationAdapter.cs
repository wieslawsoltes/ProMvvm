namespace ProMvvm;

/// <summary>
/// Explicitly adapts a model's change-notification mechanism to ProMvvm. Returning
/// <see langword="null"/> means that the adapter does not support the supplied source.
/// </summary>
public interface IPropertyNotificationAdapter
{
    /// <summary>Subscribes to property-name notifications from one source instance.</summary>
    IDisposable? Subscribe(object source, Action<string?> onPropertyChanged);
}
