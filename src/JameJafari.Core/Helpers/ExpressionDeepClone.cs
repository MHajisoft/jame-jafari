using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace JameJafari.Core.Helpers;

/// <summary>
/// Compiles and caches per-type deep-clone delegates via expression trees.
/// Used when stripping gated fields so cached FusionCache instances are never mutated.
/// </summary>
public static class ExpressionDeepClone
{
    static readonly ConcurrentDictionary<Type, Func<object, object>> Cache = new();

    static readonly MethodInfo CloneObjectMethod =
        typeof(ExpressionDeepClone).GetMethod(nameof(CloneObject), BindingFlags.Public | BindingFlags.Static)!;

    static readonly MethodInfo CloneListMethod =
        typeof(ExpressionDeepClone).GetMethod(nameof(CloneList), BindingFlags.Public | BindingFlags.Static)!;

    public static T Clone<T>(T source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return (T)CloneObject(source);
    }

    public static object CloneObject(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var type = source.GetType();
        if (IsImmutableLeaf(type))
            return source;
        return Cache.GetOrAdd(type, Compile)(source);
    }

    public static IReadOnlyList<T> CloneList<T>(IReadOnlyList<T>? source)
    {
        if (source is null || source.Count == 0)
            return source ?? Array.Empty<T>();

        var result = new T[source.Count];
        for (var i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (item is null || IsImmutableLeaf(typeof(T)))
                result[i] = item!;
            else if (item is object obj)
                result[i] = (T)CloneObject(obj);
            else
                result[i] = item;
        }
        return result;
    }

    static Func<object, object> Compile(Type type)
    {
        var sourceParam = Expression.Parameter(typeof(object), "source");
        var typed = Expression.Variable(type, "typed");
        var assignTyped = Expression.Assign(typed, Expression.Convert(sourceParam, type));

        Expression body;
        if (TryBuildListClone(type, typed, out var listClone))
        {
            body = listClone;
        }
        else
        {
            body = BuildObjectClone(type, typed);
        }

        var block = Expression.Block(
            [typed],
            assignTyped,
            Expression.Convert(body, typeof(object)));

        return Expression.Lambda<Func<object, object>>(block, sourceParam).Compile();
    }

    static Expression BuildObjectClone(Type type, Expression source)
    {
        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor is not null)
            return BuildMemberInitClone(type, source, ctor);

        var primary = FindPrimaryConstructor(type);
        if (primary is not null)
            return BuildConstructorClone(type, source, primary);

        throw new NotSupportedException($"Type '{type.FullName}' cannot be deep-cloned (no suitable constructor).");
    }

    static Expression BuildMemberInitClone(Type type, Expression source, ConstructorInfo ctor)
    {
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters().Length == 0)
            .ToArray();

        var bindings = props.Select(p =>
            (MemberBinding)Expression.Bind(p, BuildPropertyValue(source, p))).ToArray();

        return Expression.MemberInit(Expression.New(ctor), bindings);
    }

    static Expression BuildConstructorClone(Type type, Expression source, ConstructorInfo ctor)
    {
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var args = ctor.GetParameters().Select(param =>
        {
            if (!props.TryGetValue(param.Name!, out var prop))
                throw new NotSupportedException(
                    $"Constructor parameter '{param.Name}' on '{type.Name}' has no matching property.");
            return BuildPropertyValue(source, prop);
        });

        return Expression.New(ctor, args);
    }

    static Expression BuildPropertyValue(Expression source, PropertyInfo prop)
    {
        var access = Expression.Property(source, prop);
        var propType = prop.PropertyType;

        if (IsImmutableLeaf(propType))
            return access;

        if (TryGetEnumerableElementType(propType, out var elementType))
        {
            var closed = CloneListMethod.MakeGenericMethod(elementType);
            // CloneList expects IReadOnlyList<T>; cast/coerce common list shapes
            Expression listArg = access;
            var iro = typeof(IReadOnlyList<>).MakeGenericType(elementType);
            if (!iro.IsAssignableFrom(propType))
            {
                // e.g. IEnumerable — materialize via helper path through object clone of runtime list not needed for our DTOs
                throw new NotSupportedException(
                    $"Property '{prop.DeclaringType?.Name}.{prop.Name}' must be IReadOnlyList<{elementType.Name}> for deep clone.");
            }

            return Expression.Convert(Expression.Call(closed, listArg), propType);
        }

        if (propType.IsClass || propType.IsInterface)
        {
            var cloned = Expression.Call(CloneObjectMethod, Expression.Convert(access, typeof(object)));
            var nullCheck = Expression.Condition(
                Expression.Equal(access, Expression.Constant(null, propType)),
                Expression.Constant(null, propType),
                Expression.Convert(cloned, propType));
            return nullCheck;
        }

        return access;
    }

    static bool TryBuildListClone(Type type, Expression typedSource, out Expression cloneExpr)
    {
        cloneExpr = null!;
        if (!TryGetEnumerableElementType(type, out var elementType))
            return false;
        if (!typeof(IReadOnlyList<>).MakeGenericType(elementType).IsAssignableFrom(type)
            && type != typeof(IReadOnlyList<>).MakeGenericType(elementType))
            return false;

        var closed = CloneListMethod.MakeGenericMethod(elementType);
        cloneExpr = Expression.Convert(Expression.Call(closed, typedSource), type);
        return true;
    }

    static ConstructorInfo? FindPrimaryConstructor(Type type)
    {
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Where(c => c.GetParameters().Length > 0)
            .Where(c => c.GetParameters().All(p => p.Name is not null && props.Contains(p.Name)))
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
    }

    static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        elementType = null!;
        if (type == typeof(string) || typeof(IDictionary).IsAssignableFrom(type))
            return false;

        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IReadOnlyList<>) || def == typeof(IList<>) || def == typeof(List<>)
                || def == typeof(IEnumerable<>) || def == typeof(ICollection<>) || def == typeof(IReadOnlyCollection<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        foreach (var i in type.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = i.GetGenericArguments()[0];
                return true;
            }
        }

        return false;
    }

    static bool IsImmutableLeaf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(DateOnly)
               || type == typeof(TimeOnly)
               || type == typeof(Guid)
               || type == typeof(TimeSpan);
    }
}
