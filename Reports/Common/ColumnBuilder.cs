using System.Linq.Expressions;

namespace VshmToolkit.Reports.Common;

public sealed class ColumnBuilder<T>
{
    private readonly Dictionary<string, Func<object, object?>> _resolvers = [];
    private readonly HashSet<string> _ignoredProperties = [];

    public ColumnBuilder<T> Map<TProp>(Expression<Func<T, TProp>> property, Func<T, object?> resolver)
    {
        var propertyName = GetPropertyName(property);

        _resolvers[propertyName] = x => resolver((T)x);

        return this;
    }

    public ColumnBuilder<T> Ignore<TProp>(Expression<Func<T, TProp>> property)
    {
        _ignoredProperties.Add(GetPropertyName(property));

        return this;
    }

    internal Func<object, object?>? GetResolver(string propertyName)
    {
        _resolvers.TryGetValue(propertyName, out var resolver);
        return resolver;
    }

    internal bool IsIgnored(string propertyName) => _ignoredProperties.Contains(propertyName);

    private static string GetPropertyName<TProp>(Expression<Func<T, TProp>> property)
    {
        if (property.Body is MemberExpression member)
            return member.Member.Name;

        if (property.Body is UnaryExpression { Operand: MemberExpression unaryMember })
            return unaryMember.Member.Name;

        throw new ArgumentException("Invalid expression", nameof(property));
    }
}
