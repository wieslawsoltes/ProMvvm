namespace ProMvvm;

/// <summary>Factory methods for concise, fully AOT-safe property paths.</summary>
public static class PropertyPath
{
    /// <summary>Creates a strongly typed property path.</summary>
    public static PropertyPath<TSource, TValue> Create<TSource, TValue>(
        string propertyName,
        Func<TSource, TValue> getter)
        where TSource : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(getter);

        return PropertyPath<TSource, TValue>.FromSingle(propertyName, getter);
    }
}
