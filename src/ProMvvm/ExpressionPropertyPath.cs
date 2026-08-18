using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace ProMvvm;

internal static class ExpressionPropertyPath
{
    [RequiresUnreferencedCode("Expression compatibility uses reflected property or field metadata. Use PropertyPath.Create for trim-safe and NativeAOT-safe observation.")]
    public static PropertyPath<TSource, TValue> Create<TSource, TValue>(
        Expression<Func<TSource, TValue>> expression)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(expression);

        var body = StripConvert(expression.Body);
        if (body is MemberExpression
            {
                Member: PropertyInfo memberProperty,
                Expression: { } target,
            } &&
            StripConvert(target) == expression.Parameters[0] &&
            memberProperty.DeclaringType == typeof(TSource) &&
            memberProperty.PropertyType == typeof(TValue) &&
            memberProperty.GetMethod is { IsStatic: false })
        {
            return SinglePropertyCache<TSource, TValue>.GetOrAdd(memberProperty);
        }

        var members = new Stack<MemberInfo>();
        Expression? current = body;
        while (current is MemberExpression memberExpression)
        {
            members.Push(memberExpression.Member);
            current = StripConvert(memberExpression.Expression);
        }

        if (current != expression.Parameters[0] || members.Count == 0)
        {
            throw new ArgumentException(
                "The expression must be a property or field chain rooted at its parameter.",
                nameof(expression));
        }

        var builder = ImmutableArray.CreateBuilder<IPropertyPathSegment>(members.Count);
        foreach (var member in members)
        {
            builder.Add(member is PropertyInfo property
                ? new ReflectedPropertyPathSegment(property)
                : new ReflectedFieldPathSegment((FieldInfo)member));
        }

        return PropertyPath<TSource, TValue>.FromSegments(builder.MoveToImmutable());
    }

    private static Expression? StripConvert(Expression? expression)
    {
        while (expression is UnaryExpression
               {
                   NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
               } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }

    private sealed class ReflectedPropertyPathSegment(PropertyInfo property) : IPropertyPathSegment
    {
        public string PropertyName => property.Name;

        public object? GetValue(object instance) => property.GetValue(instance);
    }

    private sealed class ReflectedFieldPathSegment(FieldInfo reflectedField) : IPropertyPathSegment
    {
        public string PropertyName => reflectedField.Name;

        public object? GetValue(object instance) => reflectedField.GetValue(instance);
    }

    private static class SinglePropertyCache<TSource, TValue>
        where TSource : class
    {
        private static readonly ConcurrentDictionary<PropertyInfo, CacheEntry> Paths = new();
        private static CacheEntry? _lastEntry;

        public static PropertyPath<TSource, TValue> GetOrAdd(PropertyInfo property)
        {
            var lastEntry = Volatile.Read(ref _lastEntry);
            if (lastEntry is not null && ReferenceEquals(lastEntry.Property, property))
            {
                return lastEntry.Path;
            }

            var entry = Paths.GetOrAdd(
                property,
                static member => new CacheEntry(
                    member,
                    PropertyPath<TSource, TValue>.FromSingle(
                        member.Name,
                        member.GetMethod!.CreateDelegate<Func<TSource, TValue>>())));

            Volatile.Write(ref _lastEntry, entry);
            return entry.Path;
        }

        private sealed class CacheEntry(
            PropertyInfo property,
            PropertyPath<TSource, TValue> path)
        {
            public PropertyInfo Property { get; } = property;

            public PropertyPath<TSource, TValue> Path { get; } = path;
        }
    }
}
