using System.Collections.Concurrent;
using System.Text;

namespace Infrastructure.Helpers;

/// <summary>
/// Provides helper methods for generating human-readable type names,
/// including support for generic types, nullable types, arrays, tuples, and nested structures.
/// Results are cached for performance to avoid repeated reflection and string allocations.
/// </summary>
/// <remarks>
/// This helper is used in performance-sensitive paths (such as logging and telemetry pipelines),
/// so results are cached per <see cref="Type"/> to ensure reflection and formatting only occur once.
/// </remarks>
public static class TypeNameHelper
{
    private static readonly ConcurrentDictionary<Type, string> _cache = new();

    /// <summary>
    /// Gets a friendly, human-readable name for the specified <see cref="Type"/>.
    /// Handles generic types, nullable types, arrays, tuples, and nested types.
    /// </summary>
    /// <param name="type">The type to generate a friendly name for.</param>
    /// <returns>
    /// A string representation of the type, including generic arguments if applicable.
    /// For example:
    /// <list type="bullet">
    /// <item><description><c>List&lt;int&gt;</c> → <c>List&lt;Int32&gt;</c></description></item>
    /// <item><description><c>Dictionary&lt;string, List&lt;int&gt;&gt;</c> → <c>Dictionary&lt;String, List&lt;Int32&gt;&gt;</c></description></item>
    /// <item><description><c>int?</c> → <c>Int32?</c></description></item>
    /// <item><description><c>int[]</c> → <c>Int32[]</c></description></item>
    /// <item><description><c>(int, string)</c> → <c>(Int32, String)</c></description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static string GetFriendlyName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        // Cache ensures we only build the name once per Type
        return _cache.GetOrAdd(type, static t => BuildFriendlyName(t));
    }

    /// <summary>
    /// Builds a friendly name for the given <see cref="Type"/>.
    /// This method is only invoked once per type and cached thereafter.
    /// </summary>
    /// <param name="type">The type to process.</param>
    /// <returns>A formatted string representing the type.</returns>
    private static string BuildFriendlyName(Type type)
    {
        // Handle arrays (e.g., int[] → Int32[])
        if (type.IsArray)
            return $"{GetFriendlyName(type.GetElementType()!)}[]";

        // Handle by-ref types (e.g., ref int → Int32&)
        if (type.IsByRef)
            return $"{GetFriendlyName(type.GetElementType()!)}&";

        // Handle pointer types (rare, but supported)
        if (type.IsPointer)
            return $"{GetFriendlyName(type.GetElementType()!)}*";

        // Handle nullable types (e.g., int? instead of Nullable<int>)
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
            return $"{GetFriendlyName(underlying)}?";

        // Handle non-generic types
        if (!type.IsGenericType)
            return type.Name;

        // Handle ValueTuple<T1, ...> → (T1, T2, ...)
        if (IsValueTuple(type))
        {
            var args = type.GetGenericArguments();
            var sb = new StringBuilder("(");

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(GetFriendlyName(args[i]));
            }

            sb.Append(')');
            return sb.ToString();
        }

        // Handle generic types (e.g., Dictionary<TKey, TValue>)
        var name = type.Name;

        // Remove generic arity suffix (e.g., `1, `2)
        var tickIndex = name.IndexOf('`');
        if (tickIndex > 0)
            name = name[..tickIndex];

        var genericArgs = type.GetGenericArguments();

        var builder = new StringBuilder(name.Length + 16);
        builder.Append(name);
        builder.Append('<');

        for (int i = 0; i < genericArgs.Length; i++)
        {
            if (i > 0)
                builder.Append(", ");

            builder.Append(GetFriendlyName(genericArgs[i]));
        }

        builder.Append('>');
        return builder.ToString();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> is a <see cref="ValueTuple"/> type.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns><c>true</c> if the type is a ValueTuple; otherwise, <c>false</c>.</returns>
    private static bool IsValueTuple(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();

        return definition == typeof(ValueTuple<>) ||
               definition == typeof(ValueTuple<,>) ||
               definition == typeof(ValueTuple<,,>) ||
               definition == typeof(ValueTuple<,,,>) ||
               definition == typeof(ValueTuple<,,,,>) ||
               definition == typeof(ValueTuple<,,,,,>) ||
               definition == typeof(ValueTuple<,,,,,,>) ||
               definition == typeof(ValueTuple<,,,,,,,>);
    }
}