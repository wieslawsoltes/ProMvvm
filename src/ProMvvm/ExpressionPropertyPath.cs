using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProMvvm;

internal static class ExpressionPropertyPath
{
    private static readonly PropertyInfo ArrayLengthProperty =
        typeof(Array).GetProperty(nameof(Array.Length))!;

    [RequiresUnreferencedCode("Expression compatibility uses reflected property or field metadata. Use PropertyPath.Create for trim-safe and NativeAOT-safe observation.")]
    public static PropertyPath<TSource, TValue> Create<TSource, TValue>(
        Expression<Func<TSource, TValue>> expression)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(expression);

        var body = StripConvert(expression.Body)!;
        if (body is MemberExpression
            {
                Member: PropertyInfo memberProperty,
                Expression: { } target,
            } &&
            StripConvert(target) == expression.Parameters[0] &&
            body.Type == typeof(TValue))
        {
            return SinglePropertyCache<TSource, TValue>.GetOrAdd(memberProperty);
        }

        if (TryGetIndexer(body, out var directIndexer, out var directArguments, out var directTarget) &&
            StripConvert(directTarget) == expression.Parameters[0] &&
            body.Type == typeof(TValue))
        {
            return SingleIndexerCache<TSource, TValue>.GetOrAdd(directIndexer, directArguments);
        }

        if (body is MemberExpression
            {
                Member: PropertyInfo leafProperty,
                Expression: MemberExpression
                {
                    Member: PropertyInfo rootProperty,
                    Expression: { } rootTarget,
                },
            } &&
            StripConvert(rootTarget) == expression.Parameters[0] &&
            body.Type == typeof(TValue))
        {
            return TwoPropertyCache<TSource, TValue>.GetOrAdd(rootProperty, leafProperty);
        }

        if (body is MemberExpression
            {
                Member: PropertyInfo leafProperty3,
                Expression: MemberExpression
                {
                    Member: PropertyInfo middleProperty3,
                    Expression: MemberExpression
                    {
                        Member: PropertyInfo rootProperty3,
                        Expression: { } rootTarget3,
                    },
                },
            } &&
            StripConvert(rootTarget3) == expression.Parameters[0] &&
            body.Type == typeof(TValue))
        {
            return ThreePropertyCache<TSource, TValue>.GetOrAdd(
                rootProperty3,
                middleProperty3,
                leafProperty3);
        }

        var links = new Stack<ExpressionPathLink>();
        Expression? current = body;
        while (current is not null)
        {
            if (current is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo or FieldInfo)
            {
                links.Push(new ExpressionPathLink(memberExpression.Member, null));
                current = StripConvert(memberExpression.Expression);
                continue;
            }

            if (TryGetIndexer(current, out var indexer, out var arguments, out var indexTarget))
            {
                links.Push(new ExpressionPathLink(indexer, GetConstantArguments(arguments)));
                current = StripConvert(indexTarget);
                continue;
            }

            if (current is BinaryExpression { NodeType: ExpressionType.ArrayIndex } arrayIndex)
            {
                if (arrayIndex.Right is not ConstantExpression)
                {
                    throw new NotSupportedException(
                        "Array index expressions are only supported with constants.");
                }

                throw new ArgumentException(
                    "The expression does not have valid member info.",
                    nameof(expression));
            }

            if (current is UnaryExpression { NodeType: ExpressionType.ArrayLength } arrayLength)
            {
                links.Push(new ExpressionPathLink(ArrayLengthProperty, null));
                current = StripConvert(arrayLength.Operand);
                continue;
            }

            break;
        }

        if (current != expression.Parameters[0] || links.Count == 0)
        {
            throw new ArgumentException(
                "The expression must be a property or field chain rooted at its parameter.",
                nameof(expression));
        }

        var orderedLinks = links.ToArray();
        if (orderedLinks.Length <= 3)
        {
            return SpecializedPathCache<TSource, TValue>.GetOrAdd(orderedLinks);
        }

        var builder = ImmutableArray.CreateBuilder<IPropertyPathSegment>(orderedLinks.Length);
        foreach (var link in orderedLinks)
        {
            builder.Add(link.CreateSegment());
        }

        return PropertyPath<TSource, TValue>.FromSegments(builder.MoveToImmutable());
    }

    private static bool TryGetIndexer(
        Expression expression,
        [NotNullWhen(true)] out MemberInfo? indexer,
        [NotNullWhen(true)] out ReadOnlyCollection<Expression>? arguments,
        [NotNullWhen(true)] out Expression? target)
    {
        switch (expression)
        {
            case IndexExpression
                {
                    Indexer: { } indexProperty,
                    Object: { } indexTarget,
                } indexExpression:
                indexer = indexProperty;
                target = indexTarget;
                arguments = indexExpression.Arguments;
                ValidateConstantArguments(arguments);
                return true;

            case MethodCallExpression
                {
                    Object: { } callTarget,
                    Method:
                    {
                        IsSpecialName: true,
                        Name: var getterName,
                    } getter,
                } call:
                if (!getterName.StartsWith("get_", StringComparison.Ordinal) ||
                    call.Arguments.Count == 0)
                {
                    indexer = null;
                    arguments = null;
                    target = null;
                    return false;
                }

                indexer = getter;
                target = callTarget;
                arguments = call.Arguments;
                ValidateConstantArguments(arguments);
                return true;

            default:
                indexer = null;
                arguments = null;
                target = null;
                return false;
        }
    }

    private static void ValidateConstantArguments(ReadOnlyCollection<Expression> argumentExpressions)
    {
        for (var index = 0; index < argumentExpressions.Count; index++)
        {
            if (argumentExpressions[index] is not ConstantExpression)
            {
                throw new NotSupportedException(
                    "Index expressions are only supported with constants.");
            }
        }
    }

    private static object?[] GetConstantArguments(ReadOnlyCollection<Expression> argumentExpressions)
    {
        var arguments = new object?[argumentExpressions.Count];
        for (var index = 0; index < argumentExpressions.Count; index++)
        {
            arguments[index] = ((ConstantExpression)argumentExpressions[index]).Value;
        }

        return arguments;
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

        public object GetValue(object instance) =>
            reflectedField.GetValue(instance) ?? throw new InvalidOperationException();
    }

    private sealed class ReflectedIndexerPathSegment(
        MethodInfo getter,
        string propertyName,
        object?[] arguments) : IPropertyPathSegment
    {
        private readonly MethodInvoker _invoker = MethodInvoker.Create(getter);

        public string PropertyName { get; } = $"{propertyName}[]";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? GetValue(object instance) => arguments.Length switch
        {
            1 => _invoker.Invoke(instance, arguments[0]),
            2 => _invoker.Invoke(instance, arguments[0], arguments[1]),
            3 => _invoker.Invoke(instance, arguments[0], arguments[1], arguments[2]),
            4 => _invoker.Invoke(instance, arguments[0], arguments[1], arguments[2], arguments[3]),
            _ => _invoker.Invoke(instance, arguments.AsSpan()),
        };
    }

    private sealed class ExpressionPathLink(MemberInfo member, object?[]? arguments)
    {
        public MemberInfo Member { get; } = member;

        public object?[]? Arguments { get; } = arguments;

        public string PropertyName => Arguments is null
            ? Member.Name
            : $"{GetIndexerName(Member)}[]";

        public IPropertyPathSegment CreateSegment()
        {
            if (Arguments is not null)
            {
                var getter = Member is PropertyInfo indexer
                    ? indexer.GetMethod!
                    : (MethodInfo)Member;
                return new ReflectedIndexerPathSegment(
                    getter,
                    GetIndexerName(Member),
                    Arguments);
            }

            return Member is PropertyInfo property
                ? new ReflectedPropertyPathSegment(property)
                : new ReflectedFieldPathSegment((FieldInfo)Member);
        }

        private static string GetIndexerName(MemberInfo member) =>
            member is PropertyInfo property
                ? property.Name
                : ((MethodInfo)member).Name[4..];

        public bool IsSameAs(ExpressionPathLink other)
        {
            if (!ReferenceEquals(Member, other.Member) ||
                (Arguments is null) != (other.Arguments is null))
            {
                return false;
            }

            if (Arguments is null)
            {
                return true;
            }

            for (var index = 0; index < Arguments.Length; index++)
            {
                if (!ConstantEquals(Arguments[index], other.Arguments![index]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsSameIndexer(
            MemberInfo member,
            ReadOnlyCollection<Expression> argumentExpressions)
        {
            if (!ReferenceEquals(Member, member) ||
                Arguments is null ||
                Arguments.Length != argumentExpressions.Count)
            {
                return false;
            }

            for (var index = 0; index < Arguments.Length; index++)
            {
                var value = ((ConstantExpression)argumentExpressions[index]).Value;
                if (!ConstantEquals(Arguments[index], value))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetStructuralHashCode()
        {
            var hash = new HashCode();
            hash.Add(RuntimeHelpers.GetHashCode(Member));
            if (Arguments is not null)
            {
                hash.Add(Arguments.Length);
                foreach (var argument in Arguments)
                {
                    hash.Add(ConstantHashCode(argument));
                }
            }

            return hash.ToHashCode();
        }

        private static bool ConstantEquals(object? left, object? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null || left.GetType() != right.GetType())
            {
                return false;
            }

            return left is string || left.GetType().IsValueType
                ? left.Equals(right)
                : false;
        }

        private static int ConstantHashCode(object? value)
        {
            if (value is null)
            {
                return 0;
            }

            return value is string || value.GetType().IsValueType
                ? value.GetHashCode()
                : RuntimeHelpers.GetHashCode(value);
        }
    }

    private readonly struct ExpressionPathKey
    {
        private readonly int _hashCode;

        public ExpressionPathKey(ExpressionPathLink[] links)
        {
            Links = links;
            var hash = new HashCode();
            hash.Add(links.Length);
            foreach (var link in links)
            {
                hash.Add(link.GetStructuralHashCode());
            }

            _hashCode = hash.ToHashCode();
        }

        public ExpressionPathLink[] Links { get; }

        public override int GetHashCode() => _hashCode;

        public bool Matches(ExpressionPathLink[] links)
        {
            if (Links.Length != links.Length)
            {
                return false;
            }

            for (var index = 0; index < Links.Length; index++)
            {
                if (!Links[index].IsSameAs(links[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private sealed class ExpressionPathKeyComparer : IEqualityComparer<ExpressionPathKey>
    {
        public static ExpressionPathKeyComparer Instance { get; } = new();

        public bool Equals(ExpressionPathKey left, ExpressionPathKey right) =>
            left.Matches(right.Links);

        public int GetHashCode(ExpressionPathKey key) => key.GetHashCode();
    }

    private static class SingleIndexerCache<TSource, TValue>
        where TSource : class
    {
        private static readonly ConcurrentDictionary<ExpressionPathKey, PropertyPath<TSource, TValue>> Paths =
            new(ExpressionPathKeyComparer.Instance);
        private static CacheEntry? _lastEntry;

        public static PropertyPath<TSource, TValue> GetOrAdd(
            MemberInfo indexer,
            ReadOnlyCollection<Expression> argumentExpressions)
        {
            var lastEntry = Volatile.Read(ref _lastEntry);
            if (lastEntry is not null &&
                lastEntry.Link.IsSameIndexer(indexer, argumentExpressions))
            {
                return lastEntry.Path;
            }

            var link = new ExpressionPathLink(
                indexer,
                GetConstantArguments(argumentExpressions));
            var key = new ExpressionPathKey([link]);
            var path = Paths.GetOrAdd(
                key,
                static pathKey => CreatePath(pathKey.Links[0]));

            Volatile.Write(ref _lastEntry, new CacheEntry(link, path));
            return path;
        }

        private static PropertyPath<TSource, TValue> CreatePath(ExpressionPathLink link)
        {
            var getter = link.Member is PropertyInfo property
                ? property.GetMethod!
                : (MethodInfo)link.Member;
            var arguments = link.Arguments!;
            var parameters = getter.GetParameters();

            if (arguments.Length == 1 && parameters[0].ParameterType == typeof(int))
            {
                var typedGetter = getter.CreateDelegate<Func<TSource, int, TValue>>();
                var argument = (int)arguments[0]!;
                return PropertyPath<TSource, TValue>.FromSingle(
                    link.PropertyName,
                    source => typedGetter(source, argument));
            }

            if (arguments.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                var typedGetter = getter.CreateDelegate<Func<TSource, string?, TValue>>();
                var argument = (string?)arguments[0];
                return PropertyPath<TSource, TValue>.FromSingle(
                    link.PropertyName,
                    source => typedGetter(source, argument));
            }

            if (arguments.Length == 2 &&
                parameters[0].ParameterType == typeof(int) &&
                parameters[1].ParameterType == typeof(int))
            {
                var typedGetter = getter.CreateDelegate<Func<TSource, int, int, TValue>>();
                var argument1 = (int)arguments[0]!;
                var argument2 = (int)arguments[1]!;
                return PropertyPath<TSource, TValue>.FromSingle(
                    link.PropertyName,
                    source => typedGetter(source, argument1, argument2));
            }

            var segment = (ReflectedIndexerPathSegment)link.CreateSegment();
            return PropertyPath<TSource, TValue>.FromSingle(
                link.PropertyName,
                source => (TValue)segment.GetValue(source)!);
        }

        private sealed class CacheEntry(
            ExpressionPathLink link,
            PropertyPath<TSource, TValue> path)
        {
            public ExpressionPathLink Link { get; } = link;

            public PropertyPath<TSource, TValue> Path { get; } = path;
        }
    }

    private static class SpecializedPathCache<TSource, TValue>
        where TSource : class
    {
        private static readonly ConcurrentDictionary<ExpressionPathKey, PropertyPath<TSource, TValue>> Paths =
            new(ExpressionPathKeyComparer.Instance);
        private static CacheEntry? _lastEntry;

        public static PropertyPath<TSource, TValue> GetOrAdd(ExpressionPathLink[] links)
        {
            var lastEntry = Volatile.Read(ref _lastEntry);
            if (lastEntry is not null && lastEntry.Key.Matches(links))
            {
                return lastEntry.Path;
            }

            var key = new ExpressionPathKey(links);
            var path = Paths.GetOrAdd(key, static pathKey => CreatePath(pathKey.Links));
            Volatile.Write(ref _lastEntry, new CacheEntry(key, path));
            return path;
        }

        private static PropertyPath<TSource, TValue> CreatePath(ExpressionPathLink[] links)
        {
            var first = links[0].CreateSegment();
            if (links.Length == 1)
            {
                return PropertyPath<TSource, TValue>.FromSingle(
                    links[0].PropertyName,
                    source => (TValue)first.GetValue(source)!);
            }

            var second = links[1].CreateSegment();
            if (links.Length == 2)
            {
                return PropertyPath<TSource, TValue>.FromTwo<object?>(
                    links[0].PropertyName,
                    source => first.GetValue(source),
                    links[1].PropertyName,
                    intermediate => (TValue)second.GetValue(intermediate!)!);
            }

            var third = links[2].CreateSegment();
            return PropertyPath<TSource, TValue>.FromThree<object?, object?>(
                links[0].PropertyName,
                source => first.GetValue(source),
                links[1].PropertyName,
                intermediate => second.GetValue(intermediate!),
                links[2].PropertyName,
                intermediate => (TValue)third.GetValue(intermediate!)!);
        }

        private sealed class CacheEntry(
            ExpressionPathKey key,
            PropertyPath<TSource, TValue> path)
        {
            public ExpressionPathKey Key { get; } = key;

            public PropertyPath<TSource, TValue> Path { get; } = path;
        }
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

    private static class TwoPropertyCache<TSource, TValue>
        where TSource : class
    {
        private static readonly ConcurrentDictionary<(PropertyInfo Root, PropertyInfo Leaf), CacheEntry> Paths = new();
        private static CacheEntry? _lastEntry;

        public static PropertyPath<TSource, TValue> GetOrAdd(
            PropertyInfo rootProperty,
            PropertyInfo leafProperty)
        {
            var lastEntry = Volatile.Read(ref _lastEntry);
            if (lastEntry is not null &&
                ReferenceEquals(lastEntry.RootProperty, rootProperty) &&
                ReferenceEquals(lastEntry.LeafProperty, leafProperty))
            {
                return lastEntry.Path;
            }

            var entry = Paths.GetOrAdd(
                (rootProperty, leafProperty),
                static properties => new CacheEntry(
                    properties.Root,
                    properties.Leaf,
                    CreatePath(properties.Root, properties.Leaf)));

            Volatile.Write(ref _lastEntry, entry);
            return entry.Path;
        }

        private static PropertyPath<TSource, TValue> CreatePath(
            PropertyInfo rootProperty,
            PropertyInfo leafProperty) => PropertyPath<TSource, TValue>.FromTwo<object?>(
            rootProperty.Name,
            source => rootProperty.GetValue(source),
            leafProperty.Name,
            intermediate => (TValue)leafProperty.GetValue(intermediate!)!);

        private sealed class CacheEntry(
            PropertyInfo rootProperty,
            PropertyInfo leafProperty,
            PropertyPath<TSource, TValue> path)
        {
            public PropertyInfo RootProperty { get; } = rootProperty;

            public PropertyInfo LeafProperty { get; } = leafProperty;

            public PropertyPath<TSource, TValue> Path { get; } = path;
        }
    }

    private static class ThreePropertyCache<TSource, TValue>
        where TSource : class
    {
        private static readonly ConcurrentDictionary<
            (PropertyInfo Root, PropertyInfo Middle, PropertyInfo Leaf),
            CacheEntry> Paths = new();
        private static CacheEntry? _lastEntry;

        public static PropertyPath<TSource, TValue> GetOrAdd(
            PropertyInfo rootProperty,
            PropertyInfo middleProperty,
            PropertyInfo leafProperty)
        {
            var lastEntry = Volatile.Read(ref _lastEntry);
            if (lastEntry is not null &&
                ReferenceEquals(lastEntry.RootProperty, rootProperty) &&
                ReferenceEquals(lastEntry.MiddleProperty, middleProperty) &&
                ReferenceEquals(lastEntry.LeafProperty, leafProperty))
            {
                return lastEntry.Path;
            }

            var entry = Paths.GetOrAdd(
                (rootProperty, middleProperty, leafProperty),
                static properties => new CacheEntry(
                    properties.Root,
                    properties.Middle,
                    properties.Leaf,
                    CreatePath(properties.Root, properties.Middle, properties.Leaf)));

            Volatile.Write(ref _lastEntry, entry);
            return entry.Path;
        }

        private static PropertyPath<TSource, TValue> CreatePath(
            PropertyInfo rootProperty,
            PropertyInfo middleProperty,
            PropertyInfo leafProperty) =>
            PropertyPath<TSource, TValue>.FromThree<object?, object?>(
                rootProperty.Name,
                source => rootProperty.GetValue(source),
                middleProperty.Name,
                intermediate => middleProperty.GetValue(intermediate!),
                leafProperty.Name,
                intermediate => (TValue)leafProperty.GetValue(intermediate!)!);

        private sealed class CacheEntry(
            PropertyInfo rootProperty,
            PropertyInfo middleProperty,
            PropertyInfo leafProperty,
            PropertyPath<TSource, TValue> path)
        {
            public PropertyInfo RootProperty { get; } = rootProperty;

            public PropertyInfo MiddleProperty { get; } = middleProperty;

            public PropertyInfo LeafProperty { get; } = leafProperty;

            public PropertyPath<TSource, TValue> Path { get; } = path;
        }
    }
}
