using System.Reflection;

namespace VshmToolkit.Reports.Common;

public sealed class ColumnInfo
{
    public required PropertyInfo Property { get; init; }

    public string Header { get; init; } = string.Empty;

    public Func<object, object?>? ValueResolver { get; set; }
}
