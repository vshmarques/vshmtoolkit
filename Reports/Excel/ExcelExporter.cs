using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel;
using System.Reflection;
using VshmToolkit.Reports.Common;

namespace VshmToolkit.Reports.Excel;

public sealed class ExcelExporter : IDisposable
{
    private uint _sheetId = 1;
    private readonly SpreadsheetDocument _document;
    private readonly WorkbookPart _workbookPart;
    private readonly Sheets _sheets;
    private readonly uint _headerStyleIndex;

    public ExcelExporter(Stream output)
    {
        _document = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook, true);

        _workbookPart = _document.AddWorkbookPart();
        _workbookPart.Workbook = new Workbook();
        _sheets = _workbookPart.Workbook.AppendChild(new Sheets());
        _headerStyleIndex = CreateHeaderStyle();
    }

    public async Task AddSheetAsync<T>(string sheetName, IAsyncEnumerable<T> data, bool includeHeader = true, Action<ColumnBuilder<T>>? configure = null)
    {
        var worksheetPart = _workbookPart.AddNewPart<WorksheetPart>();
        var relationshipId = _workbookPart.GetIdOfPart(worksheetPart);

        _sheets.Append(new Sheet
        {
            Id = relationshipId,
            SheetId = _sheetId++,
            Name = sheetName
        });

        var builder = new ColumnBuilder<T>();
        configure?.Invoke(builder);

        var columns = GetColumns(builder);

        using var writer = OpenXmlWriter.Create(worksheetPart);

        writer.WriteStartElement(new Worksheet());
        writer.WriteStartElement(new SheetData());

        if (includeHeader)
            WriteHeader(writer, columns);

        await foreach (var item in data)
        {
            WriteRow(writer, item!, columns);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    public void AddSheet<T>(string sheetName, IEnumerable<T> data, bool includeHeader = true)
    {
        var worksheetPart = _workbookPart.AddNewPart<WorksheetPart>();
        var relationshipId = _workbookPart.GetIdOfPart(worksheetPart);

        _sheets.Append(new Sheet
        {
            Id = relationshipId,
            SheetId = _sheetId++,
            Name = sheetName
        });

        var columns = GetColumns(new ColumnBuilder<T>());

        using var writer = OpenXmlWriter.Create(worksheetPart);

        writer.WriteStartElement(new Worksheet());
        writer.WriteStartElement(new SheetData());

        if (includeHeader)
            WriteHeader(writer, columns);

        foreach (var item in data)
        {
            WriteRow(writer, item!, columns);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    public void Complete()
    {
        _workbookPart.Workbook!.Save();
        _document.Dispose();
    }

    public void Dispose()
    {
        _document.Dispose();
    }

    private List<ColumnInfo> GetColumns<T>(ColumnBuilder<T> builder)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead &&
                                         p.GetCustomAttribute<ExcelIgnoreAttribute>() == null)
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

    private void WriteHeader(OpenXmlWriter writer, IReadOnlyCollection<ColumnInfo> columns)
    {
        writer.WriteStartElement(new Row());

        foreach (var column in columns)
        {
            writer.WriteElement(CreateTextCell(column.Header, _headerStyleIndex));
        }

        writer.WriteEndElement();
    }

    private void WriteRow(OpenXmlWriter writer, object item, IReadOnlyCollection<ColumnInfo> columns)
    {
        writer.WriteStartElement(new Row());

        foreach (var column in columns)
        {
            var value = column.ValueResolver?.Invoke(item) ?? column.Property.GetValue(item);

            WriteCell(writer, value);
        }

        writer.WriteEndElement();
    }

    private void WriteCell(OpenXmlWriter writer, object? value)
    {
        writer.WriteElement(new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(value?.ToString() ?? "")
        });
    }

    private Cell CreateTextCell(string value, uint? styleIndex = null)
    {
        return new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(value),
            StyleIndex = styleIndex
        };
    }

    private uint CreateHeaderStyle()
    {
        var stylesPart = _workbookPart.AddNewPart<WorkbookStylesPart>();

        var stylesheet = new Stylesheet(
            new Fonts(new Font(), new Font(new Bold())),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 })
            ),
            new Borders(new Border()),
            new CellFormats(
                new CellFormat(),
                new CellFormat
                {
                    FontId = 1,
                    ApplyFont = true
                }
            )
        );

        stylesPart.Stylesheet = stylesheet;
        stylesheet.Save();

        return 1;
    }
}