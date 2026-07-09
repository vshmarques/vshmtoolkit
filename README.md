# VshmToolkit

Biblioteca .NET com utilitarios para exportacao de relatorios em CSV e Excel.

O projeto tem como foco gerar arquivos a partir de colecoes de objetos, usando reflexao para descobrir propriedades publicas e atributos para ajustar cabecalhos ou ignorar colunas.

## Recursos

- Exportacao CSV a partir de `IEnumerable<T>` ou `IAsyncEnumerable<T>`.
- Exportacao Excel `.xlsx` a partir de `IEnumerable<T>` ou `IAsyncEnumerable<T>`.
- Suporte a multiplas abas no mesmo arquivo Excel.
- Cabecalhos baseados em `DescriptionAttribute`.
- Atributos para ignorar propriedades no CSV ou no Excel.
- Mapeamento customizado de valores por coluna com `ColumnBuilder<T>` nos metodos assincronos.

## Requisitos

- .NET 10.0 SDK
- Pacote `DocumentFormat.OpenXml` 3.5.1

## Instalacao

Para usar como referencia de projeto:

```bash
dotnet add reference caminho/para/VshmToolkit.csproj
```

Ao compilar, o projeto tambem gera um pacote NuGet com o identificador configurado no `.csproj`:

```bash
dotnet build
```

O pacote gerado fica em `bin/Debug` ou `bin/Release`, conforme a configuracao de build.

## Modelo de exemplo

```csharp
using System.ComponentModel;
using VshmToolkit.Reports.Csv;
using VshmToolkit.Reports.Excel;

public sealed class Pedido
{
    [Description("Codigo")]
    public int Id { get; set; }

    [Description("Cliente")]
    public string Cliente { get; set; } = string.Empty;

    [Description("Total")]
    public decimal Total { get; set; }

    [CsvIgnore]
    [ExcelIgnore]
    public string ObservacaoInterna { get; set; } = string.Empty;
}
```

## Exportando CSV

```csharp
using VshmToolkit.Reports.Csv;

var pedidos = new[]
{
    new Pedido { Id = 1, Cliente = "Maria", Total = 150.75m },
    new Pedido { Id = 2, Cliente = "Joao", Total = 89.90m }
};

await using var stream = File.Create("pedidos.csv");

var exporter = new CsvExporter(new CsvOptions
{
    Delimiter = ";",
    WriteHeader = true
});

await exporter.Export(pedidos, stream);
```

### CSV assincrono com valor customizado

```csharp
async IAsyncEnumerable<Pedido> ObterPedidosAsync()
{
    foreach (var pedido in pedidos)
    {
        yield return pedido;
        await Task.Yield();
    }
}

await using var stream = File.Create("pedidos.csv");

var exporter = new CsvExporter(new CsvOptions());

await exporter.ExportAsync(ObterPedidosAsync(), stream, columns =>
{
    columns.Map(p => p.Total, p => p.Total.ToString("N2"));
});
```

## Exportando Excel

```csharp
using VshmToolkit.Reports.Excel;

var pedidos = new[]
{
    new Pedido { Id = 1, Cliente = "Maria", Total = 150.75m },
    new Pedido { Id = 2, Cliente = "Joao", Total = 89.90m }
};

await using var stream = File.Create("pedidos.xlsx");
using var exporter = new ExcelExporter(stream);

exporter.AddSheet("Pedidos", pedidos);
exporter.Complete();
```

### Excel assincrono com valor customizado

```csharp
async IAsyncEnumerable<Pedido> ObterPedidosAsync()
{
    foreach (var pedido in pedidos)
    {
        yield return pedido;
        await Task.Yield();
    }
}

await using var stream = File.Create("pedidos.xlsx");
using var exporter = new ExcelExporter(stream);

await exporter.AddSheetAsync("Pedidos", ObterPedidosAsync(), configure: columns =>
{
    columns.Map(p => p.Total, p => p.Total.ToString("C"));
});

exporter.Complete();
```

## Opcoes de CSV

| Opcao | Padrao | Descricao |
| --- | --- | --- |
| `Delimiter` | `;` | Separador usado entre colunas. |
| `Encoding` | UTF-8 com BOM | Encoding usado na escrita do arquivo. |
| `WriteHeader` | `true` | Define se a primeira linha tera cabecalhos. |
| `FlushInterval` | `5000` | Intervalo usado para descarregar o buffer durante a escrita. |

## Atributos suportados

- `DescriptionAttribute`: define o nome da coluna no cabecalho.
- `CsvIgnoreAttribute`: remove a propriedade da exportacao CSV.
- `ExcelIgnoreAttribute`: remove a propriedade da exportacao Excel.

## Estrutura do projeto

```text
Reports/
  Common/
    ColumnBuilder.cs
    ColumnInfo.cs
  Csv/
    CsvExporter.cs
    CsvIgnoreAttribute.cs
    CsvOptions.cs
  Excel/
    ExcelExporter.cs
    ExcelIgnoreAttribute.cs
```

## Licenca

Este projeto esta configurado para distribuicao sob a licenca MIT.


