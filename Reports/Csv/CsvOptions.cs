using System.Text;

namespace VshmToolkit.Reports.Csv;

public sealed class CsvOptions
{
    public string Delimiter { get; set; } = ";";
    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    public bool WriteHeader { get; set; } = true;
    public int FlushInterval { get; set; } = 5000;
}