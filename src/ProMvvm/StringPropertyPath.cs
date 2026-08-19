using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ProMvvm;

internal static class StringPropertyPath
{
    [RequiresUnreferencedCode("String property compatibility resolves public property metadata by name. Use a generated or typed PropertyPath for trim-safe and NativeAOT-safe observation.")]
    public static PropertyPath<TSource, TValue> Create<TSource, TValue>(
        TSource source,
        string propertyName)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return Cache<TSource, TValue>.GetOrAdd(source.GetType(), propertyName);
    }

    private static class Cache<TSource, TValue>
        where TSource : class
    {
        private static readonly ConcurrentDictionary<(Type RuntimeType, string PropertyName), CacheEntry> Paths = new();
        private static CacheEntry? _lastEntry;

        [RequiresUnreferencedCode("String property compatibility resolves public property metadata by name.")]
        public static PropertyPath<TSource, TValue> GetOrAdd(
            Type runtimeType,
            string propertyName)
        {
            var lastEntry = Volatile.Read(ref _lastEntry);
            if (lastEntry is not null &&
                ReferenceEquals(lastEntry.RuntimeType, runtimeType) &&
                string.Equals(lastEntry.PropertyName, propertyName, StringComparison.Ordinal))
            {
                return lastEntry.Path;
            }

            var entry = Paths.GetOrAdd(
                (runtimeType, propertyName),
                static key => new CacheEntry(
                    key.RuntimeType,
                    key.PropertyName,
                    CreatePath(key.RuntimeType, key.PropertyName)));

            Volatile.Write(ref _lastEntry, entry);
            return entry.Path;
        }

        [RequiresUnreferencedCode("String property compatibility resolves public property metadata by name.")]
        private static PropertyPath<TSource, TValue> CreatePath(
            Type runtimeType,
            string propertyName)
        {
            var property = runtimeType.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (property?.GetMethod is { } getter &&
                property.PropertyType == typeof(TValue) &&
                property.DeclaringType!.IsAssignableFrom(typeof(TSource)))
            {
                return PropertyPath<TSource, TValue>.FromSingle(
                    propertyName,
                    getter.CreateDelegate<Func<TSource, TValue>>());
            }

            return PropertyPath<TSource, TValue>.FromSingle(
                propertyName,
                property is null
                    ? static _ => default!
                    : source => ReadValue(property, source));
        }

        private static TValue ReadValue(PropertyInfo property, TSource source)
        {
            var value = property.GetValue(source);
            return value is null ? default! : (TValue)value;
        }

        private sealed class CacheEntry(
            Type runtimeType,
            string propertyName,
            PropertyPath<TSource, TValue> path)
        {
            public Type RuntimeType { get; } = runtimeType;

            public string PropertyName { get; } = propertyName;

            public PropertyPath<TSource, TValue> Path { get; } = path;
        }
    }
}
