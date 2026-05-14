using System.Globalization;
using System.Text;
using TuCita.Application.InformesInteligentes;

namespace TuCita.Infrastucture.InformesInteligentes;

internal static class InformeInteligentePdfBuilder
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double MarginX = 48;
    private const double BottomMargin = 62;
    private static readonly CultureInfo ChileCulture = CultureInfo.GetCultureInfo("es-CL");

    public static byte[] Build(
        InformeInteligenteContextoDto contexto,
        InformeAiGenerationResult informe)
    {
        var writer = new ReportWriter();

        writer.AddCoverHeader(contexto, informe.Model);
        writer.AddIndicatorCards(contexto);
        writer.AddSectionTitle("Informe generado por IA");
        writer.AddMarkdown(informe.Text);
        writer.AddSectionTitle("Anexo de indicadores");
        writer.AddKeyValue("Período analizado", contexto.Periodo.Etiqueta);
        writer.AddKeyValue("Citas registradas", contexto.Indicadores.TotalCitas.ToString(ChileCulture));
        writer.AddKeyValue("Citas atendidas", contexto.Indicadores.CitasAtendidas.ToString(ChileCulture));
        writer.AddKeyValue("Citas canceladas", contexto.Indicadores.CitasCanceladas.ToString(ChileCulture));
        writer.AddKeyValue("No asistidas", contexto.Indicadores.CitasNoAsistidas.ToString(ChileCulture));
        writer.AddKeyValue("Ingresos estimados", FormatMoney(contexto.Indicadores.IngresosEstimados));
        writer.AddKeyValue("Ocupación estimada", FormatPercent(contexto.Indicadores.TasaOcupacionAgenda));
        writer.AddKeyValue("Clientes únicos", contexto.Indicadores.ClientesUnicos.ToString(ChileCulture));
        writer.AddKeyValue("Clientes nuevos", contexto.Indicadores.ClientesNuevos.ToString(ChileCulture));
        writer.AddKeyValue("Clientes recurrentes", contexto.Indicadores.ClientesRecurrentes.ToString(ChileCulture));

        if (contexto.CalidadDatos.Advertencias.Count > 0)
        {
            writer.AddSectionTitle("Advertencias de calidad de datos");
            foreach (var warning in contexto.CalidadDatos.Advertencias)
            {
                writer.AddBullet(warning);
            }
        }

        return writer.Build();
    }

    public static string BuildFileName(InformeInteligenteContextoDto contexto)
    {
        var negocio = SanitizeFileName(string.IsNullOrWhiteSpace(contexto.Negocio.Slug)
            ? contexto.Negocio.Nombre
            : contexto.Negocio.Slug);

        return $"informe-inteligente-{negocio}-{contexto.Periodo.FechaDesde:yyyyMMdd}-{contexto.Periodo.FechaHasta:yyyyMMdd}.pdf";
    }

    private static string FormatMoney(decimal value)
    {
        return value.ToString("C0", ChileCulture);
    }

    private static string FormatPercent(decimal value)
    {
        return value.ToString("P1", ChileCulture);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value
            .Select(character => invalidChars.Contains(character) ? '-' : character)
            .ToArray());

        sanitized = sanitized.Trim('-', ' ', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "negocio" : sanitized;
    }

    private sealed class ReportWriter
    {
        private readonly List<StringBuilder> _pages = [];
        private StringBuilder _content = new();
        private double _y;
        private bool _hasStarted;

        public void AddCoverHeader(InformeInteligenteContextoDto contexto, string model)
        {
            StartPage(firstPage: true);

            Rect(0, 740, PageWidth, 102, 0.05, 0.11, 0.23);
            Rect(0, 734, PageWidth, 6, 0.20, 0.52, 0.93);

            Text("TuCita", 48, 816, 12, bold: true, 0.82, 0.90, 1);
            Text("Informe inteligente", 48, 790, 24, bold: true, 1, 1, 1);
            Text(contexto.Negocio.Nombre, 48, 767, 13, bold: false, 0.89, 0.94, 1);
            Text($"Rubro: {contexto.Negocio.Rubro}", 48, 750, 9.5, bold: false, 0.72, 0.81, 0.93);

            Rect(386, 768, 161, 42, 0.09, 0.19, 0.36);
            Text("Período", 403, 794, 8.5, bold: true, 0.72, 0.81, 0.93);
            Text(contexto.Periodo.Etiqueta, 403, 777, 9.5, bold: true, 1, 1, 1);

            Text($"Modelo IA: {model}", 48, 712, 8.5, bold: false, 0.39, 0.45, 0.55);
            Text($"Generado el {DateTime.Now.ToString("dd-MM-yyyy HH:mm", ChileCulture)}", 384, 712, 8.5, bold: false, 0.39, 0.45, 0.55);
            Line(48, 696, 547, 696, 0.86, 0.89, 0.94);
            _y = 664;
        }

        public void AddIndicatorCards(InformeInteligenteContextoDto contexto)
        {
            var indicators = contexto.Indicadores;
            var cards = new[]
            {
                ("Citas", indicators.TotalCitas.ToString(ChileCulture)),
                ("Atendidas", indicators.CitasAtendidas.ToString(ChileCulture)),
                ("Asistencia", FormatPercent(indicators.TasaAsistencia)),
                ("Ingresos", FormatMoney(indicators.IngresosEstimados)),
                ("Cancelación", FormatPercent(indicators.TasaCancelacion)),
                ("No asistencia", FormatPercent(indicators.TasaNoAsistencia)),
                ("Ocupación", FormatPercent(indicators.TasaOcupacionAgenda)),
                ("Clientes", indicators.ClientesUnicos.ToString(ChileCulture))
            };

            EnsureSpace(124);
            const double cardWidth = 115;
            const double cardHeight = 48;
            const double gap = 13;

            for (var index = 0; index < cards.Length; index++)
            {
                var row = index / 4;
                var column = index % 4;
                var x = MarginX + (column * (cardWidth + gap));
                var y = _y - (row * (cardHeight + 12));

                Rect(x, y - cardHeight, cardWidth, cardHeight, 0.96, 0.98, 1);
                StrokeRect(x, y - cardHeight, cardWidth, cardHeight, 0.86, 0.89, 0.94);
                Text(cards[index].Item1, x + 12, y - 17, 8.2, bold: true, 0.39, 0.45, 0.55);
                Text(cards[index].Item2, x + 12, y - 36, 14, bold: true, 0.05, 0.11, 0.23);
            }

            _y -= 124;
        }

        public void AddSectionTitle(string title)
        {
            EnsureSpace(38);
            _y -= 4;
            Text(title, MarginX, _y, 15, bold: true, 0.05, 0.11, 0.23);
            Line(MarginX, _y - 9, PageWidth - MarginX, _y - 9, 0.86, 0.89, 0.94);
            _y -= 28;
        }

        public void AddKeyValue(string label, string value)
        {
            EnsureSpace(25);
            Text(label, MarginX, _y, 9, bold: true, 0.39, 0.45, 0.55);
            Text(value, 210, _y, 9.5, bold: false, 0.05, 0.11, 0.23);
            _y -= 19;
        }

        public void AddMarkdown(string markdown)
        {
            var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    _y -= 6;
                    continue;
                }

                if (line.StartsWith('#'))
                {
                    var text = line.TrimStart('#').Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        AddSubheading(text);
                    }

                    continue;
                }

                if (line.StartsWith("- ", StringComparison.Ordinal) ||
                    line.StartsWith("* ", StringComparison.Ordinal))
                {
                    AddBullet(line[2..].Trim());
                    continue;
                }

                if (TryRemoveNumberedPrefix(line, out var numbered))
                {
                    AddBullet(numbered);
                    continue;
                }

                AddParagraph(line);
            }
        }

        public void AddBullet(string text)
        {
            var clean = CleanInlineMarkdown(text);
            var lines = WrapText(clean, maxCharacters: 88, maxLines: 20);
            var needed = Math.Max(1, lines.Count) * 13 + 3;
            EnsureSpace(needed);

            Text("-", MarginX + 6, _y, 10, bold: true, 0.20, 0.52, 0.93);
            for (var index = 0; index < lines.Count; index++)
            {
                Text(lines[index], MarginX + 22, _y - (index * 13), 9.5, bold: false, 0.10, 0.15, 0.25);
            }

            _y -= needed;
        }

        public byte[] Build()
        {
            AddFooters();
            return PdfDocument.Build(_pages.Select(page => page.ToString()).ToArray());
        }

        private void AddSubheading(string title)
        {
            EnsureSpace(34);
            _y -= 4;
            Text(CleanInlineMarkdown(title), MarginX, _y, 12.5, bold: true, 0.11, 0.26, 0.56);
            _y -= 21;
        }

        private void AddParagraph(string text)
        {
            var clean = CleanInlineMarkdown(text);
            var lines = WrapText(clean, maxCharacters: 96, maxLines: 80);
            var needed = Math.Max(1, lines.Count) * 13 + 4;
            EnsureSpace(needed);

            for (var index = 0; index < lines.Count; index++)
            {
                Text(lines[index], MarginX, _y - (index * 13), 9.7, bold: false, 0.10, 0.15, 0.25);
            }

            _y -= needed;
        }

        private void EnsureSpace(double requiredHeight)
        {
            if (!_hasStarted)
            {
                StartPage(firstPage: true);
            }

            if (_y - requiredHeight < BottomMargin)
            {
                StartPage(firstPage: false);
            }
        }

        private void StartPage(bool firstPage)
        {
            _content = new StringBuilder();
            _pages.Add(_content);
            _hasStarted = true;

            if (firstPage)
            {
                _y = 790;
                return;
            }

            Text("TuCita", 48, 812, 10, bold: true, 0.11, 0.26, 0.56);
            Text("Informe inteligente del negocio", 100, 812, 10, bold: false, 0.39, 0.45, 0.55);
            Line(48, 796, 547, 796, 0.86, 0.89, 0.94);
            _y = 768;
        }

        private void AddFooters()
        {
            for (var index = 0; index < _pages.Count; index++)
            {
                _content = _pages[index];
                Rect(0, 0, PageWidth, 48, 0.98, 0.99, 1);
                Line(48, 48, 547, 48, 0.86, 0.89, 0.94);
                Text("Documento generado automáticamente por TuCita.", 48, 29, 8.2, bold: false, 0.39, 0.45, 0.55);
                Text($"Página {index + 1} de {_pages.Count}", 485, 29, 8.2, bold: false, 0.39, 0.45, 0.55);
            }
        }

        private void Text(
            string text,
            double x,
            double y,
            double size,
            bool bold,
            double r,
            double g,
            double b)
        {
            _content
                .Append(Invariant(r)).Append(' ')
                .Append(Invariant(g)).Append(' ')
                .Append(Invariant(b)).AppendLine(" rg")
                .Append("BT /").Append(bold ? "F2" : "F1").Append(' ')
                .Append(Invariant(size)).Append(" Tf ")
                .Append(Invariant(x)).Append(' ')
                .Append(Invariant(y)).Append(" Td (")
                .Append(EscapePdfText(ToPdfSafeText(text))).AppendLine(") Tj ET");
        }

        private void Rect(double x, double y, double width, double height, double r, double g, double b)
        {
            _content
                .Append(Invariant(r)).Append(' ')
                .Append(Invariant(g)).Append(' ')
                .Append(Invariant(b)).AppendLine(" rg")
                .Append(Invariant(x)).Append(' ')
                .Append(Invariant(y)).Append(' ')
                .Append(Invariant(width)).Append(' ')
                .Append(Invariant(height)).AppendLine(" re f");
        }

        private void StrokeRect(double x, double y, double width, double height, double r, double g, double b)
        {
            _content
                .Append(Invariant(r)).Append(' ')
                .Append(Invariant(g)).Append(' ')
                .Append(Invariant(b)).AppendLine(" RG")
                .Append(Invariant(x)).Append(' ')
                .Append(Invariant(y)).Append(' ')
                .Append(Invariant(width)).Append(' ')
                .Append(Invariant(height)).AppendLine(" re S");
        }

        private void Line(double x1, double y1, double x2, double y2, double r, double g, double b)
        {
            _content
                .Append(Invariant(r)).Append(' ')
                .Append(Invariant(g)).Append(' ')
                .Append(Invariant(b)).AppendLine(" RG")
                .Append(Invariant(x1)).Append(' ')
                .Append(Invariant(y1)).Append(" m ")
                .Append(Invariant(x2)).Append(' ')
                .Append(Invariant(y2)).AppendLine(" l S");
        }

        private static bool TryRemoveNumberedPrefix(string line, out string text)
        {
            var dotIndex = line.IndexOf('.', StringComparison.Ordinal);
            if (dotIndex is <= 0 or > 3)
            {
                text = line;
                return false;
            }

            if (!line[..dotIndex].All(char.IsDigit))
            {
                text = line;
                return false;
            }

            text = line[(dotIndex + 1)..].Trim();
            return !string.IsNullOrWhiteSpace(text);
        }
    }

    private static List<string> WrapText(string? value, int maxCharacters, int maxLines)
    {
        var text = string.IsNullOrWhiteSpace(value)
            ? "Sin información"
            : ToPdfSafeText(value).Trim();

        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length <= maxCharacters)
        {
            return [text];
        }

        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > maxCharacters)
            {
                if (current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }

                for (var index = 0; index < word.Length; index += maxCharacters)
                {
                    lines.Add(word.Substring(index, Math.Min(maxCharacters, word.Length - index)));
                }

                continue;
            }

            var candidateLength = current.Length == 0
                ? word.Length
                : current.Length + 1 + word.Length;

            if (candidateLength > maxCharacters)
            {
                lines.Add(current.ToString());
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            lines.Add(current.ToString());
        }

        if (lines.Count <= maxLines)
        {
            return lines;
        }

        var visibleLines = lines.Take(maxLines).ToList();
        visibleLines[^1] = Ellipsize(visibleLines[^1], maxCharacters);
        return visibleLines;
    }

    private static string CleanInlineMarkdown(string value)
    {
        return value
            .Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string Ellipsize(string value, int maxCharacters)
    {
        if (value.Length <= maxCharacters - 3)
        {
            return value + "...";
        }

        return value[..Math.Max(0, maxCharacters - 3)].TrimEnd() + "...";
    }

    private static string ToPdfSafeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('‘', '\'')
            .Replace('’', '\'')
            .Replace('–', '-')
            .Replace('—', '-')
            .Replace('•', '-')
            .Replace('→', '>')
            .Replace("≤", "<=")
            .Replace("≥", ">=");

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (character is '\n' or '\r' or '\t')
            {
                builder.Append(' ');
                continue;
            }

            if (char.IsControl(character))
            {
                continue;
            }

            builder.Append(character <= 255 ? character : '?');
        }

        return builder.ToString();
    }

    private static string EscapePdfText(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static string Invariant(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string BytesAsLatin1(byte[] bytes)
    {
        return Encoding.Latin1.GetString(bytes);
    }

    private sealed class PdfDocument
    {
        public static byte[] Build(IReadOnlyList<string> pageContents)
        {
            var objects = new List<string?>();
            var catalog = Reserve(objects);
            var pages = Reserve(objects);
            var regularFont = AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            var boldFont = AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
            var pageObjects = new List<int>();

            foreach (var pageContent in pageContents)
            {
                var contentBytes = Encoding.Latin1.GetBytes(pageContent);
                var contentObject = AddObject(
                    objects,
                    $"<< /Length {contentBytes.Length} >>\nstream\n{BytesAsLatin1(contentBytes)}\nendstream");
                var page = AddObject(
                    objects,
                    $"<< /Type /Page /Parent {pages} 0 R /MediaBox [0 0 {Invariant(PageWidth)} {Invariant(PageHeight)}] /Resources << /Font << /F1 {regularFont} 0 R /F2 {boldFont} 0 R >> >> /Contents {contentObject} 0 R >>");

                pageObjects.Add(page);
            }

            SetObject(objects, catalog, $"<< /Type /Catalog /Pages {pages} 0 R >>");
            SetObject(objects, pages, $"<< /Type /Pages /Kids [{string.Join(' ', pageObjects.Select(page => $"{page} 0 R"))}] /Count {pageObjects.Count} >>");

            using var stream = new MemoryStream();
            Write(stream, "%PDF-1.4\n%TuCita\n");
            var offsets = new List<long> { 0 };

            for (var index = 0; index < objects.Count; index++)
            {
                offsets.Add(stream.Position);
                Write(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
            }

            var xrefOffset = stream.Position;
            Write(stream, $"xref\n0 {objects.Count + 1}\n");
            Write(stream, "0000000000 65535 f \n");

            foreach (var offset in offsets.Skip(1))
            {
                Write(stream, $"{offset:0000000000} 00000 n \n");
            }

            Write(stream, $"trailer\n<< /Size {objects.Count + 1} /Root {catalog} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
            return stream.ToArray();
        }

        private static int Reserve(List<string?> objects)
        {
            objects.Add(null);
            return objects.Count;
        }

        private static int AddObject(List<string?> objects, string content)
        {
            var number = Reserve(objects);
            SetObject(objects, number, content);
            return number;
        }

        private static void SetObject(List<string?> objects, int number, string content)
        {
            objects[number - 1] = content;
        }

        private static void Write(Stream stream, string value)
        {
            var bytes = Encoding.Latin1.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
