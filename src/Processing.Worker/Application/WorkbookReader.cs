using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Processing.Worker.Application;

internal sealed record ProductRow(int RowNumber, string Period, string Code, string Name, decimal Price);
internal sealed record RowFailure(int RowNumber, string? Code, string Reason);
internal sealed record WorkbookRows(IReadOnlyList<ProductRow> Products, IReadOnlyList<RowFailure> Failures);

internal static partial class WorkbookReader
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const long MaxXmlBytes = 25 * 1024 * 1024;

    internal static WorkbookRows Read(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidDataException("The first worksheet is missing.");
        var shared = archive.GetEntry("xl/sharedStrings.xml");
        var strings = shared is null ? [] : ReadXml(shared).Descendants(Main + "si")
            .Select(item => string.Concat(item.Descendants(Main + "t").Select(text => text.Value))).ToArray();
        var rows = ReadXml(sheet).Descendants(Main + "sheetData").Elements(Main + "row").ToList();
        if (rows.Count == 0 || rows.Count > 100001)
            throw new InvalidDataException("Workbook must contain a header and at most 100000 rows.");

        var headers = Cells(rows[0], strings).ToDictionary(pair => pair.Key, pair => pair.Value.Trim());
        var columns = new[] { "Periodo", "CodigoProducto", "NombreProducto", "Precio" }
            .ToDictionary(name => name, name => headers.SingleOrDefault(pair =>
                string.Equals(pair.Value, name, StringComparison.OrdinalIgnoreCase)).Key);
        if (columns.Values.Any(string.IsNullOrEmpty) || columns.Values.Distinct().Count() != 4)
            throw new InvalidDataException("Required columns: Periodo, CodigoProducto, NombreProducto, Precio.");

        var products = new List<ProductRow>();
        var failures = new List<RowFailure>();
        foreach (var row in rows.Skip(1))
        {
            var number = (int?)row.Attribute("r") ?? 0;
            var cells = Cells(row, strings);
            string Value(string name) => cells.GetValueOrDefault(columns[name], "").Trim();
            var period = Value("Periodo");
            var code = Value("CodigoProducto").ToUpperInvariant();
            var name = Value("NombreProducto");
            var priceText = Value("Precio");
            if (period.Length == 0 && code.Length == 0 && name.Length == 0 && priceText.Length == 0)
                continue;
            if (!PeriodPattern().IsMatch(period))
                failures.Add(new(number, code, "Periodo inválido; use AAAA-MM."));
            else if (code.Length is 0 or > 100)
                failures.Add(new(number, code, "CodigoProducto obligatorio y de máximo 100 caracteres."));
            else if (name.Length > 200)
                failures.Add(new(number, code, "NombreProducto excede 200 caracteres."));
            else if (!decimal.TryParse(priceText.Length == 0 ? "0" : priceText,
                         NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                     || price < 0 || Math.Abs(price - decimal.Round(price, 2)) > 0.0000001m)
                failures.Add(new(number, code, "Precio debe ser un decimal no negativo con máximo dos decimales."));
            else
                products.Add(new(number, period, code, name.Length == 0 ? "Sin nombre" : name,
                    decimal.Round(price, 2)));
        }
        return new(products, failures);
    }

    private static XDocument ReadXml(ZipArchiveEntry entry)
    {
        if (entry.Length > MaxXmlBytes) throw new InvalidDataException("Worksheet XML is too large.");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxXmlBytes
        });
        return XDocument.Load(reader);
    }

    private static Dictionary<string, string> Cells(XElement row, string[] shared)
    {
        var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in row.Elements(Main + "c"))
        {
            var reference = (string?)cell.Attribute("r") ?? "";
            var column = new string(reference.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
            if (column.Length == 0) continue;
            if (cell.Element(Main + "f") is not null)
                throw new InvalidDataException("Formula cells are not accepted.");
            var type = (string?)cell.Attribute("t");
            var raw = (string?)cell.Element(Main + "v") ?? "";
            cells[column] = type switch
            {
                "inlineStr" => string.Concat(cell.Descendants(Main + "t").Select(text => text.Value)),
                "s" when int.TryParse(raw, out var index) && index >= 0 && index < shared.Length => shared[index],
                "s" => throw new InvalidDataException("Invalid shared string index."),
                "e" => throw new InvalidDataException("Excel error cell encountered."),
                _ => raw
            };
        }
        return cells;
    }

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial Regex PeriodPattern();
}
