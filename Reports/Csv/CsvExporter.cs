using System.ComponentModel;
using System.Reflection;
using VshmToolkit.Reports.Common;

namespace VshmToolkit.Reports.Csv;

public sealed class CsvExporter
{
    private const int _bufferSize = 65536;
    private readonly CsvOptions _options;

    public CsvExporter(CsvOptions options)
    {
        _options = options;
    }

    public async Task ExportAsync<T>(IAsyncEnumerable<T> data, Stream outputStream, Action<ColumnBuilder<T>>? configure = null)
    {
        var builder = new ColumnBuilder<T>();
        configure?.Invoke(builder);
        var columns = GetColumns(builder);

        await using var writer = new StreamWriter(outputStream, _options.Encoding, _bufferSize, leaveOpen: true);
        if (_options.WriteHeader)
        {
            await writer.WriteLineAsync(Join(columns.Select(c => Escape(c.Header))));
        }

        await foreach (var item in data)
        {
            var values = new string[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                values[i] = columns[i].ValueResolver?.Invoke(item!)?.ToString() ?? columns[i].Property.GetValue(item)?.ToString() ?? string.Empty;
                values[i] = Escape(values[i]);
            }

            await writer.WriteLineAsync(Join(values));

            if (_options.FlushInterval > 0 && writer.BaseStream.Position % _options.FlushInterval == 0)
                await writer.FlushAsync();
        }
    }

    public async Task Export<T>(IEnumerable<T> data, Stream outputStream)
    {
        var builder = new ColumnBuilder<T>();
        var columns = GetColumns(builder);

        await using var writer = new StreamWriter(outputStream, _options.Encoding, _bufferSize, leaveOpen: true);
        if (_options.WriteHeader)
        {
            await writer.WriteLineAsync(Join(columns.Select(c => Escape(c.Header))));
        }

        foreach (var item in data)
        {
            var values = new string[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                values[i] = columns[i].Property.GetValue(item)?.ToString() ?? string.Empty;
                values[i] = Escape(values[i]);
            }

            await writer.WriteLineAsync(Join(values));

            if (_options.FlushInterval > 0 && writer.BaseStream.Position % _options.FlushInterval == 0)
                await writer.FlushAsync();
        }
    }

    private string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(_options.Delimiter))
            return value.Replace(_options.Delimiter, string.Empty);

        return value;
    }

    private string Join(IEnumerable<string> values) => string.Join(_options.Delimiter, values);

    private List<ColumnInfo> GetColumns<T>(ColumnBuilder<T> builder)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead &&
                                         p.GetCustomAttribute<CsvIgnoreAttribute>() == null)
                             .ToList();

        return props.Select(p =>
        {
            var description = p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name;

            return new ColumnInfo
            {
                Property = p,
                Header = description,
                ValueResolver = builder.GetResolver(p.Name)
            };
        }).ToList();
    }
}
