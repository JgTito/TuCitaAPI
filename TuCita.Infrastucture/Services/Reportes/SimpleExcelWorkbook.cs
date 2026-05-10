using System.Globalization;
using System.IO.Compression;
using System.Xml;

namespace TuCita.Infrastucture.Reportes;

internal sealed class SimpleExcelWorkbook
{
    private readonly List<SimpleExcelWorksheet> worksheets = [];

    public SimpleExcelWorksheet AddWorksheet(string name)
    {
        var worksheet = new SimpleExcelWorksheet(SanitizeWorksheetName(name, worksheets.Count + 1));
        worksheets.Add(worksheet);
        return worksheet;
    }

    public byte[] ToByteArray()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteContentTypes(archive);
            WriteRootRelationships(archive);
            WriteWorkbook(archive);
            WriteWorkbookRelationships(archive);
            WriteStyles(archive);

            for (var index = 0; index < worksheets.Count; index++)
            {
                WriteWorksheet(archive, worksheets[index], index + 1);
            }
        }

        return stream.ToArray();
    }

    private void WriteContentTypes(ZipArchive archive)
    {
        using var writer = CreateXmlWriter(archive.CreateEntry("[Content_Types].xml"));
        writer.WriteStartDocument();
        writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
        writer.WriteStartElement("Default");
        writer.WriteAttributeString("Extension", "rels");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml");
        writer.WriteEndElement();
        writer.WriteStartElement("Default");
        writer.WriteAttributeString("Extension", "xml");
        writer.WriteAttributeString("ContentType", "application/xml");
        writer.WriteEndElement();
        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", "/xl/workbook.xml");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
        writer.WriteEndElement();
        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", "/xl/styles.xml");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
        writer.WriteEndElement();

        for (var index = 1; index <= worksheets.Count; index++)
        {
            writer.WriteStartElement("Override");
            writer.WriteAttributeString("PartName", $"/xl/worksheets/sheet{index}.xml");
            writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteRootRelationships(ZipArchive archive)
    {
        using var writer = CreateXmlWriter(archive.CreateEntry("_rels/.rels"));
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", "rId1");
        writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
        writer.WriteAttributeString("Target", "xl/workbook.xml");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private void WriteWorkbook(ZipArchive archive)
    {
        using var writer = CreateXmlWriter(archive.CreateEntry("xl/workbook.xml"));
        writer.WriteStartDocument();
        writer.WriteStartElement("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        writer.WriteAttributeString("xmlns", "r", null, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
        writer.WriteStartElement("sheets");

        for (var index = 0; index < worksheets.Count; index++)
        {
            writer.WriteStartElement("sheet");
            writer.WriteAttributeString("name", worksheets[index].Name);
            writer.WriteAttributeString("sheetId", (index + 1).ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(
                "r",
                "id",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
                $"rId{index + 1}");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private void WriteWorkbookRelationships(ZipArchive archive)
    {
        using var writer = CreateXmlWriter(archive.CreateEntry("xl/_rels/workbook.xml.rels"));
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");

        for (var index = 0; index < worksheets.Count; index++)
        {
            writer.WriteStartElement("Relationship");
            writer.WriteAttributeString("Id", $"rId{index + 1}");
            writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet");
            writer.WriteAttributeString("Target", $"worksheets/sheet{index + 1}.xml");
            writer.WriteEndElement();
        }

        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", $"rId{worksheets.Count + 1}");
        writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles");
        writer.WriteAttributeString("Target", "styles.xml");
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteStyles(ZipArchive archive)
    {
        using var writer = CreateXmlWriter(archive.CreateEntry("xl/styles.xml"));
        writer.WriteStartDocument();
        writer.WriteStartElement("styleSheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

        writer.WriteStartElement("numFmts");
        writer.WriteAttributeString("count", "4");
        WriteNumFmt(writer, 164, "yyyy-mm-dd");
        WriteNumFmt(writer, 165, "yyyy-mm-dd hh:mm");
        WriteNumFmt(writer, 166, "\"$\"#,##0;[Red]-\"$\"#,##0");
        WriteNumFmt(writer, 167, "0.00%");
        writer.WriteEndElement();

        writer.WriteStartElement("fonts");
        writer.WriteAttributeString("count", "4");
        WriteFont(writer, bold: false, color: "111827", size: 11);
        WriteFont(writer, bold: true, color: "FFFFFF", size: 14);
        WriteFont(writer, bold: true, color: "111827", size: 11);
        WriteFont(writer, bold: true, color: "FFFFFF", size: 11);
        writer.WriteEndElement();

        writer.WriteStartElement("fills");
        writer.WriteAttributeString("count", "5");
        WriteFill(writer, null);
        WriteFill(writer, "gray125");
        WriteSolidFill(writer, "0F766E");
        WriteSolidFill(writer, "CCFBF1");
        WriteSolidFill(writer, "F3F4F6");
        writer.WriteEndElement();

        writer.WriteStartElement("borders");
        writer.WriteAttributeString("count", "2");
        WriteBorder(writer, hasBorder: false);
        WriteBorder(writer, hasBorder: true);
        writer.WriteEndElement();

        writer.WriteStartElement("cellStyleXfs");
        writer.WriteAttributeString("count", "1");
        WriteXf(writer, 0, 0, 0, 0);
        writer.WriteEndElement();

        writer.WriteStartElement("cellXfs");
        writer.WriteAttributeString("count", "12");
        WriteXf(writer, 0, 0, 0, 0);
        WriteXf(writer, 0, 1, 2, 0, applyFill: true, applyFont: true, horizontal: "center");
        WriteXf(writer, 0, 2, 3, 1, applyFill: true, applyFont: true, applyBorder: true);
        WriteXf(writer, 0, 3, 2, 1, applyFill: true, applyFont: true, applyBorder: true, horizontal: "center", wrapText: true);
        WriteXf(writer, 0, 0, 0, 1, applyBorder: true);
        WriteXf(writer, 1, 0, 0, 1, applyNumberFormat: true, applyBorder: true);
        WriteXf(writer, 4, 0, 0, 1, applyNumberFormat: true, applyBorder: true);
        WriteXf(writer, 166, 0, 0, 1, applyNumberFormat: true, applyBorder: true);
        WriteXf(writer, 164, 0, 0, 1, applyNumberFormat: true, applyBorder: true);
        WriteXf(writer, 165, 0, 0, 1, applyNumberFormat: true, applyBorder: true);
        WriteXf(writer, 167, 0, 0, 1, applyNumberFormat: true, applyBorder: true);
        WriteXf(writer, 0, 2, 4, 1, applyFill: true, applyFont: true, applyBorder: true);
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorksheet(ZipArchive archive, SimpleExcelWorksheet worksheet, int sheetIndex)
    {
        using var writer = CreateXmlWriter(archive.CreateEntry($"xl/worksheets/sheet{sheetIndex}.xml"));
        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

        writer.WriteStartElement("sheetViews");
        writer.WriteStartElement("sheetView");
        writer.WriteAttributeString("showGridLines", "0");
        writer.WriteAttributeString("workbookViewId", "0");
        if (worksheet.FrozenRows > 0)
        {
            writer.WriteStartElement("pane");
            writer.WriteAttributeString("ySplit", worksheet.FrozenRows.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("topLeftCell", $"A{worksheet.FrozenRows + 1}");
            writer.WriteAttributeString("activePane", "bottomLeft");
            writer.WriteAttributeString("state", "frozen");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();

        if (worksheet.ColumnWidths.Count > 0)
        {
            writer.WriteStartElement("cols");
            foreach (var item in worksheet.ColumnWidths.OrderBy(item => item.Key))
            {
                writer.WriteStartElement("col");
                writer.WriteAttributeString("min", item.Key.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("max", item.Key.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("width", item.Value.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("customWidth", "1");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        writer.WriteStartElement("sheetData");
        for (var rowIndex = 0; rowIndex < worksheet.Rows.Count; rowIndex++)
        {
            var rowNumber = rowIndex + 1;
            var row = worksheet.Rows[rowIndex];
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));

            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                WriteCell(writer, row[columnIndex], rowNumber, columnIndex + 1);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        if (!string.IsNullOrWhiteSpace(worksheet.AutoFilterReference))
        {
            writer.WriteStartElement("autoFilter");
            writer.WriteAttributeString("ref", worksheet.AutoFilterReference);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteCell(XmlWriter writer, SimpleExcelCell cell, int rowNumber, int columnNumber)
    {
        if (cell.Value is null && cell.Style == ExcelStyles.Normal)
        {
            return;
        }

        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", $"{GetColumnName(columnNumber)}{rowNumber}");
        if (cell.Style != ExcelStyles.Normal)
        {
            writer.WriteAttributeString("s", ((int)cell.Style).ToString(CultureInfo.InvariantCulture));
        }

        switch (cell.Value)
        {
            case null:
                break;
            case string value:
                writer.WriteAttributeString("t", "inlineStr");
                writer.WriteStartElement("is");
                writer.WriteElementString("t", value);
                writer.WriteEndElement();
                break;
            case bool value:
                writer.WriteAttributeString("t", "b");
                writer.WriteElementString("v", value ? "1" : "0");
                break;
            case DateTime value:
                writer.WriteElementString("v", value.ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case DateOnly value:
                writer.WriteElementString("v", value.ToDateTime(TimeOnly.MinValue).ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case decimal value:
                writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture));
                break;
            case double value:
                writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture));
                break;
            case float value:
                writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture));
                break;
            case int value:
                writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture));
                break;
            case long value:
                writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture));
                break;
            default:
                writer.WriteAttributeString("t", "inlineStr");
                writer.WriteStartElement("is");
                writer.WriteElementString("t", cell.Value.ToString() ?? string.Empty);
                writer.WriteEndElement();
                break;
        }

        writer.WriteEndElement();
    }

    private static void WriteNumFmt(XmlWriter writer, int id, string formatCode)
    {
        writer.WriteStartElement("numFmt");
        writer.WriteAttributeString("numFmtId", id.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("formatCode", formatCode);
        writer.WriteEndElement();
    }

    private static void WriteFont(XmlWriter writer, bool bold, string color, int size)
    {
        writer.WriteStartElement("font");
        if (bold)
        {
            writer.WriteElementString("b", string.Empty);
        }

        writer.WriteStartElement("sz");
        writer.WriteAttributeString("val", size.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
        writer.WriteStartElement("color");
        writer.WriteAttributeString("rgb", $"FF{color}");
        writer.WriteEndElement();
        writer.WriteStartElement("name");
        writer.WriteAttributeString("val", "Calibri");
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteFill(XmlWriter writer, string? patternType)
    {
        writer.WriteStartElement("fill");
        writer.WriteStartElement("patternFill");
        if (patternType is not null)
        {
            writer.WriteAttributeString("patternType", patternType);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteSolidFill(XmlWriter writer, string color)
    {
        writer.WriteStartElement("fill");
        writer.WriteStartElement("patternFill");
        writer.WriteAttributeString("patternType", "solid");
        writer.WriteStartElement("fgColor");
        writer.WriteAttributeString("rgb", $"FF{color}");
        writer.WriteEndElement();
        writer.WriteStartElement("bgColor");
        writer.WriteAttributeString("indexed", "64");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteBorder(XmlWriter writer, bool hasBorder)
    {
        writer.WriteStartElement("border");
        WriteBorderSide(writer, "left", hasBorder);
        WriteBorderSide(writer, "right", hasBorder);
        WriteBorderSide(writer, "top", hasBorder);
        WriteBorderSide(writer, "bottom", hasBorder);
        writer.WriteElementString("diagonal", string.Empty);
        writer.WriteEndElement();
    }

    private static void WriteBorderSide(XmlWriter writer, string name, bool hasBorder)
    {
        writer.WriteStartElement(name);
        if (hasBorder)
        {
            writer.WriteAttributeString("style", "thin");
            writer.WriteStartElement("color");
            writer.WriteAttributeString("rgb", "FFE5E7EB");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteXf(
        XmlWriter writer,
        int numFmtId,
        int fontId,
        int fillId,
        int borderId,
        bool applyNumberFormat = false,
        bool applyFont = false,
        bool applyFill = false,
        bool applyBorder = false,
        string? horizontal = null,
        bool wrapText = false)
    {
        writer.WriteStartElement("xf");
        writer.WriteAttributeString("numFmtId", numFmtId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fontId", fontId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fillId", fillId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("borderId", borderId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("xfId", "0");

        if (applyNumberFormat)
        {
            writer.WriteAttributeString("applyNumberFormat", "1");
        }

        if (applyFont)
        {
            writer.WriteAttributeString("applyFont", "1");
        }

        if (applyFill)
        {
            writer.WriteAttributeString("applyFill", "1");
        }

        if (applyBorder)
        {
            writer.WriteAttributeString("applyBorder", "1");
        }

        if (horizontal is not null || wrapText)
        {
            writer.WriteAttributeString("applyAlignment", "1");
            writer.WriteStartElement("alignment");
            if (horizontal is not null)
            {
                writer.WriteAttributeString("horizontal", horizontal);
            }

            if (wrapText)
            {
                writer.WriteAttributeString("wrapText", "1");
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static XmlWriter CreateXmlWriter(ZipArchiveEntry entry)
    {
        return XmlWriter.Create(entry.Open(), new XmlWriterSettings
        {
            Encoding = System.Text.Encoding.UTF8,
            Indent = false,
            CloseOutput = true
        });
    }

    private static string GetColumnName(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string SanitizeWorksheetName(string name, int fallbackIndex)
    {
        var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var sanitized = invalidChars.Aggregate(name, (current, invalid) => current.Replace(invalid, ' ')).Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"Hoja {fallbackIndex}";
        }

        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }
}

internal sealed class SimpleExcelWorksheet(string name)
{
    public string Name { get; } = name;

    public List<IReadOnlyList<SimpleExcelCell>> Rows { get; } = [];

    public Dictionary<int, double> ColumnWidths { get; } = [];

    public int FrozenRows { get; set; } = 1;

    public string? AutoFilterReference { get; set; }

    public void SetColumnWidths(params double[] widths)
    {
        for (var index = 0; index < widths.Length; index++)
        {
            ColumnWidths[index + 1] = widths[index];
        }
    }

    public void AddRow(params SimpleExcelCell[] cells)
    {
        Rows.Add(cells);
    }

    public void AddEmptyRow()
    {
        Rows.Add([]);
    }
}

internal readonly record struct SimpleExcelCell(object? Value, ExcelStyles Style)
{
    public static SimpleExcelCell Text(string? value, ExcelStyles style = ExcelStyles.Text) => new(value ?? string.Empty, style);

    public static SimpleExcelCell Integer(int value, ExcelStyles style = ExcelStyles.Integer) => new(value, style);

    public static SimpleExcelCell Number(decimal value, ExcelStyles style = ExcelStyles.Decimal) => new(value, style);

    public static SimpleExcelCell Number(double value, ExcelStyles style = ExcelStyles.Decimal) => new(value, style);

    public static SimpleExcelCell Currency(decimal value) => new(value, ExcelStyles.Currency);

    public static SimpleExcelCell Date(DateTime value) => new(value.Date, ExcelStyles.Date);

    public static SimpleExcelCell DateTime(DateTime value) => new(value, ExcelStyles.DateTime);

    public static SimpleExcelCell Percent(decimal value) => new(value, ExcelStyles.Percent);

    public static SimpleExcelCell Blank(ExcelStyles style = ExcelStyles.Normal) => new(null, style);
}

internal enum ExcelStyles
{
    Normal = 0,
    Title = 1,
    Section = 2,
    Header = 3,
    Text = 4,
    Integer = 5,
    Decimal = 6,
    Currency = 7,
    Date = 8,
    DateTime = 9,
    Percent = 10,
    Total = 11
}
