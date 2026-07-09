using System.Linq.Expressions;

namespace VshmToolkit.Reports.Common;

public sealed class ColumnBuilder<T>
{
    private readonly Dictionary<string, Func<object, object?>> _resolvers = [];

    public ColumnBuilder<T> Map<TProp>(Expression<Func<T, TProp>> property, Func<T, object?> resolver)
    {
        if (property.Body is not MemberExpression member)
            throw new ArgumentException("Invalid expression");

        _resolvers[member.Member.Name] = x => resolver((T)x);

        return this;
    }

    internal Func<object, object?>? GetResolver(string propertyName)
    {
        _resolvers.TryGetValue(propertyName, out var resolver);
        return resolver;
    }
}