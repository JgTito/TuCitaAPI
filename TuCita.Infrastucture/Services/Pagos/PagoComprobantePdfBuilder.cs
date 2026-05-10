using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using TuCita.Infrastucture.Entities;

namespace TuCita.Infrastucture.Pagos;

internal static class PagoComprobantePdfBuilder
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double FooterY = 38;
    private static readonly CultureInfo ChileCulture = CultureInfo.GetCultureInfo("es-CL");
    private static readonly HttpClient HttpClient = new();

    public static async Task<byte[]> BuildAsync(Pago pago, CancellationToken cancellationToken)
    {
        var logo = await TryLoadImageAsync(pago.Negocio.LogoUrl, cancellationToken);
        var document = new PdfDocument();

        var catalog = document.ReserveObject();
        var pages = document.ReserveObject();
        var page = document.ReserveObject();
        var regularFont = document.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        var boldFont = document.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

        int? logoObject = null;
        int? logoMaskObject = null;
        if (logo is not null)
        {
            if (logo.SoftMask is not null)
            {
                logoMaskObject = document.ReserveObject();
            }

            logoObject = document.ReserveObject();
        }

        var content = BuildContent(pago, logo);
        var contentObject = document.AddStream(Encoding.ASCII.GetBytes(content));

        if (logo is not null && logoObject.HasValue)
        {
            if (logo.SoftMask is not null && logoMaskObject.HasValue)
            {
                document.SetObject(logoMaskObject.Value, BuildImageObject(logo.SoftMask, null));
            }

            document.SetObject(logoObject.Value, BuildImageObject(logo, logoMaskObject));
        }

        var xObjectResources = logoObject.HasValue
            ? $"/XObject << /Logo {logoObject.Value} 0 R >>"
            : string.Empty;

        document.SetObject(catalog, $"<< /Type /Catalog /Pages {pages} 0 R >>");
        document.SetObject(pages, $"<< /Type /Pages /Kids [{page} 0 R] /Count 1 >>");
        document.SetObject(
            page,
            $"<< /Type /Page /Parent {pages} 0 R /MediaBox [0 0 {Invariant(PageWidth)} {Invariant(PageHeight)}] /Resources << /Font << /F1 {regularFont} 0 R /F2 {boldFont} 0 R >> {xObjectResources} >> /Contents {contentObject} 0 R >>");

        return document.Build();
    }

    public static string BuildFileName(Pago pago)
    {
        var negocio = SanitizeFileName(string.IsNullOrWhiteSpace(pago.Negocio.Slug)
            ? pago.Negocio.Nombre
            : pago.Negocio.Slug);
        var codigo = SanitizeFileName(pago.Cita.Codigo);

        return $"comprobante-{negocio}-{codigo}-pago-{pago.IdPago}.pdf";
    }

    private static string BuildContent(Pago pago, PdfImage? logo)
    {
        var content = new StringBuilder();
        var comprobante = $"PAG-{pago.IdPago:000000}";
        var montoNeto = Math.Max(pago.Monto - pago.MontoDevuelto, 0m);
        var fechaPago = pago.FechaPago ?? pago.FechaRegistroManual ?? pago.FechaCreacion;
        var prestador = pago.Cita.Prestador?.Nombre;

        Rect(content, 0, 774, PageWidth, 68, 0.08, 0.18, 0.39);
        Rect(content, 0, 768, PageWidth, 6, 0.24, 0.49, 0.91);

        if (logo is not null)
        {
            ImageFit(content, "Logo", logo, 434, 794, 112, 34);
        }
        else
        {
            Text(content, Initials(pago.Negocio.Nombre), 487, 807, 16, bold: true, 1, 1, 1);
        }

        Text(content, "TuCita", 48, 818, 12, bold: true, 1, 1, 1);
        Text(content, "Comprobante de pago", 48, 792, 22, bold: true, 1, 1, 1);
        Text(content, "Documento de respaldo de pago", 48, 779, 8.8, bold: false, 0.82, 0.89, 1);

        Text(content, "Negocio", 48, 735, 8.5, bold: true, 0.39, 0.45, 0.55);
        var negocioY = 716d;
        foreach (var line in WrapText(pago.Negocio.Nombre, 38, 2))
        {
            Text(content, line, 48, negocioY, 16, bold: true, 0.03, 0.05, 0.08);
            negocioY -= 17;
        }

        Badge(content, pago.EstadoPago.Nombre, 407, 715);
        Text(content, "Comprobante", 407, 696, 8.5, bold: true, 0.39, 0.45, 0.55);
        Text(content, comprobante, 407, 679, 13, bold: true, 0.11, 0.26, 0.56);

        SummaryBox(content, 48, 606, "Monto pagado", FormatMoney(pago.Monto, pago.Moneda));
        SummaryBox(content, 220, 606, "Monto devuelto", FormatMoney(pago.MontoDevuelto, pago.Moneda));
        SummaryBox(content, 392, 606, "Monto neto", FormatMoney(montoNeto, pago.Moneda), highlight: true);

        var leftY = Section(content, "Datos del pago", 572, 48, 235);
        leftY = Row(content, leftY, "Estado", pago.EstadoPago.Nombre, x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        leftY = Row(content, leftY, "Método", pago.MetodoPago.Nombre, x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        leftY = Row(content, leftY, "Proveedor", pago.Proveedor, x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        leftY = OptionalRow(content, leftY, "Orden", pago.CommerceOrder, x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        leftY = OptionalRow(content, leftY, "Flow", pago.FlowOrder?.ToString(ChileCulture), x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        leftY = OptionalRow(content, leftY, "Referencia", pago.ReferenciaManual, x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        leftY = OptionalRow(content, leftY, "Pagador", pago.PayerEmail, x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        leftY = Row(content, leftY, "Fecha", FormatDate(fechaPago), x: 48, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);

        var rightY = Section(content, "Datos de la cita", 572, 312, 235);
        rightY = Row(content, rightY, "Código", pago.Cita.Codigo, x: 312, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        rightY = Row(content, rightY, "Cliente", pago.Cita.Cliente.Nombre, x: 312, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        rightY = Row(content, rightY, "Servicio", pago.Cita.Servicio.Nombre, x: 312, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        rightY = OptionalRow(content, rightY, "Prestador", prestador, x: 312, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        rightY = Row(content, rightY, "Fecha", FormatDate(pago.Cita.FechaInicio), x: 312, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        rightY = Row(content, rightY, "Horario", $"{pago.Cita.FechaInicio:HH:mm} - {pago.Cita.FechaFin:HH:mm}", x: 312, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);
        rightY = Row(content, rightY, "Estado", pago.Cita.EstadoCita.Nombre, x: 312, width: 235, labelWidth: 82, maxCharacters: 25, maxLines: 1);

        var y = Section(content, "Datos del negocio", Math.Min(leftY, rightY) - 20);
        y = Row(content, y, "Negocio", pago.Negocio.Nombre);
        y = OptionalRow(content, y, "Dirección", pago.Negocio.Direccion);
        y = OptionalRow(content, y, "Teléfono", pago.Negocio.Telefono);
        y = OptionalRow(content, y, "Email", pago.Negocio.Email);

        if (pago.FechaAnulacion.HasValue ||
            pago.FechaUltimaDevolucion.HasValue ||
            !string.IsNullOrWhiteSpace(pago.MotivoAnulacion))
        {
            y = Section(content, "Anulaciones y devoluciones", y - 20);
            y = OptionalRow(content, y, "Fecha anulación", FormatDateOrNull(pago.FechaAnulacion));
            y = OptionalRow(content, y, "Motivo anulación", pago.MotivoAnulacion);
            y = OptionalRow(content, y, "Referencia anulación", pago.ReferenciaAnulacion);
            y = OptionalRow(content, y, "Última devolución", FormatDateOrNull(pago.FechaUltimaDevolucion));
        }

        Rect(content, 0, 0, PageWidth, 70, 0.98, 0.99, 1);
        Line(content, 48, FooterY + 20, 547, FooterY + 20, 0.86, 0.89, 0.94);
        Text(content, $"Emitido el {FormatDate(DateTime.Now)}", 48, FooterY + 7, 8.8, bold: true, 0.31, 0.38, 0.49);
        Text(content, "Documento generado automáticamente por TuCita. No reemplaza boleta o factura tributaria cuando corresponda.", 48, FooterY - 8, 8.2, bold: false, 0.39, 0.45, 0.55);

        return content.ToString();
    }

    private static string BuildImageObject(PdfImage image, int? softMaskObject)
    {
        var softMask = softMaskObject.HasValue ? $" /SMask {softMaskObject.Value} 0 R" : string.Empty;
        return $"<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /{image.ColorSpace} /BitsPerComponent 8 /Filter /{image.Filter}{softMask} /Length {image.Bytes.Length} >>\nstream\n{BytesAsLatin1(image.Bytes)}\nendstream";
    }

    private static async Task<PdfImage?> TryLoadImageAsync(string? logoUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return null;
        }

        var bytes = await TryReadImageBytesAsync(logoUrl.Trim(), cancellationToken);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        if (TryCreateJpeg(bytes, out var jpeg))
        {
            return jpeg;
        }

        if (TryCreatePng(bytes, out var png))
        {
            return png;
        }

        return null;
    }

    private static async Task<byte[]?> TryReadImageBytesAsync(string logoUrl, CancellationToken cancellationToken)
    {
        try
        {
            if (Uri.TryCreate(logoUrl, UriKind.Absolute, out var absoluteUri))
            {
                if (absoluteUri.IsFile && File.Exists(absoluteUri.LocalPath))
                {
                    return await File.ReadAllBytesAsync(absoluteUri.LocalPath, cancellationToken);
                }

                if (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps)
                {
                    if (IsLocalHost(absoluteUri))
                    {
                        var localFromUri = TryResolveLocalPath(absoluteUri.AbsolutePath);
                        if (localFromUri is not null)
                        {
                            return await File.ReadAllBytesAsync(localFromUri, cancellationToken);
                        }
                    }

                    return await HttpClient.GetByteArrayAsync(absoluteUri, cancellationToken);
                }
            }

            var localPath = TryResolveLocalPath(logoUrl);
            return localPath is null ? null : await File.ReadAllBytesAsync(localPath, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveLocalPath(string pathOrUrl)
    {
        var relative = pathOrUrl.Split('?', '#')[0].Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(relative))
        {
            return null;
        }

        relative = Uri.UnescapeDataString(relative);
        if (relative.StartsWith("~/", StringComparison.Ordinal))
        {
            relative = relative[2..];
        }

        if (Path.IsPathRooted(relative) && File.Exists(relative))
        {
            return relative;
        }

        relative = relative.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative),
            Path.Combine(Directory.GetCurrentDirectory(), "TuCita.Api", "wwwroot", relative),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", relative),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot", relative)),
            Path.Combine(AppContext.BaseDirectory, relative)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static bool IsLocalHost(Uri uri)
    {
        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreateJpeg(byte[] bytes, out PdfImage? image)
    {
        image = null;
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return false;
        }

        var index = 2;
        while (index + 9 < bytes.Length)
        {
            if (bytes[index] != 0xFF)
            {
                index++;
                continue;
            }

            var marker = bytes[index + 1];
            index += 2;

            if (marker is 0xD8 or 0xD9)
            {
                continue;
            }

            if (index + 2 > bytes.Length)
            {
                return false;
            }

            var length = ReadUInt16(bytes, index);
            if (length < 2 || index + length > bytes.Length)
            {
                return false;
            }

            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                var height = ReadUInt16(bytes, index + 3);
                var width = ReadUInt16(bytes, index + 5);
                image = new PdfImage(width, height, "DeviceRGB", "DCTDecode", bytes, null);
                return true;
            }

            index += length;
        }

        return false;
    }

    private static bool TryCreatePng(byte[] bytes, out PdfImage? image)
    {
        image = null;
        var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < signature.Length || !bytes.Take(signature.Length).SequenceEqual(signature))
        {
            return false;
        }

        var index = 8;
        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        byte colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        var idat = new MemoryStream();

        while (index + 8 <= bytes.Length)
        {
            var length = ReadInt32BigEndian(bytes, index);
            var chunkType = Encoding.ASCII.GetString(bytes, index + 4, 4);
            index += 8;

            if (length < 0 || index + length + 4 > bytes.Length)
            {
                return false;
            }

            if (chunkType == "IHDR")
            {
                width = ReadInt32BigEndian(bytes, index);
                height = ReadInt32BigEndian(bytes, index + 4);
                bitDepth = bytes[index + 8];
                colorType = bytes[index + 9];
                var interlace = bytes[index + 12];
                if (bitDepth != 8 || interlace != 0 || colorType is not (0 or 2 or 3 or 6))
                {
                    return false;
                }
            }
            else if (chunkType == "PLTE")
            {
                palette = bytes.Skip(index).Take(length).ToArray();
            }
            else if (chunkType == "tRNS")
            {
                transparency = bytes.Skip(index).Take(length).ToArray();
            }
            else if (chunkType == "IDAT")
            {
                idat.Write(bytes, index, length);
            }
            else if (chunkType == "IEND")
            {
                break;
            }

            index += length + 4;
        }

        if (width <= 0 || height <= 0 || idat.Length == 0)
        {
            return false;
        }

        var channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            6 => 4,
            _ => 0
        };

        var raw = DecompressZlib(idat.ToArray());
        var expectedRawLength = ((width * channels) + 1) * height;
        if (raw.Length < expectedRawLength)
        {
            return false;
        }

        var unfiltered = UnfilterPng(raw, width, height, channels);
        byte[] imageBytes;
        PdfImage? softMask = null;
        var colorSpace = colorType == 0 ? "DeviceGray" : "DeviceRGB";

        if (colorType == 3)
        {
            if (palette is null || palette.Length < 3)
            {
                return false;
            }

            imageBytes = new byte[width * height * 3];
            var alpha = new byte[width * height];
            var hasTransparency = false;

            for (var pixel = 0; pixel < width * height; pixel++)
            {
                var paletteIndex = unfiltered[pixel];
                var paletteOffset = paletteIndex * 3;
                if (paletteOffset + 2 >= palette.Length)
                {
                    return false;
                }

                imageBytes[(pixel * 3) + 0] = palette[paletteOffset + 0];
                imageBytes[(pixel * 3) + 1] = palette[paletteOffset + 1];
                imageBytes[(pixel * 3) + 2] = palette[paletteOffset + 2];

                var alphaValue = transparency is not null && paletteIndex < transparency.Length
                    ? transparency[paletteIndex]
                    : (byte)255;

                alpha[pixel] = alphaValue;
                hasTransparency = hasTransparency || alphaValue < 255;
            }

            if (hasTransparency)
            {
                softMask = new PdfImage(width, height, "DeviceGray", "FlateDecode", CompressZlib(alpha), null);
            }
        }
        else if (colorType == 6)
        {
            imageBytes = new byte[width * height * 3];
            var alpha = new byte[width * height];

            for (var pixel = 0; pixel < width * height; pixel++)
            {
                imageBytes[(pixel * 3) + 0] = unfiltered[(pixel * 4) + 0];
                imageBytes[(pixel * 3) + 1] = unfiltered[(pixel * 4) + 1];
                imageBytes[(pixel * 3) + 2] = unfiltered[(pixel * 4) + 2];
                alpha[pixel] = unfiltered[(pixel * 4) + 3];
            }

            softMask = new PdfImage(width, height, "DeviceGray", "FlateDecode", CompressZlib(alpha), null);
        }
        else
        {
            imageBytes = unfiltered;
        }

        image = new PdfImage(width, height, colorSpace, "FlateDecode", CompressZlib(imageBytes), softMask);
        return true;
    }

    private static byte[] DecompressZlib(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] CompressZlib(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    private static byte[] UnfilterPng(byte[] raw, int width, int height, int channels)
    {
        var stride = width * channels;
        var output = new byte[stride * height];
        var sourceIndex = 0;

        for (var row = 0; row < height; row++)
        {
            var filter = raw[sourceIndex++];
            var rowOffset = row * stride;
            var previousRowOffset = rowOffset - stride;

            for (var column = 0; column < stride; column++)
            {
                var value = raw[sourceIndex++];
                var left = column >= channels ? output[rowOffset + column - channels] : (byte)0;
                var up = row > 0 ? output[previousRowOffset + column] : (byte)0;
                var upperLeft = row > 0 && column >= channels ? output[previousRowOffset + column - channels] : (byte)0;

                output[rowOffset + column] = filter switch
                {
                    0 => value,
                    1 => unchecked((byte)(value + left)),
                    2 => unchecked((byte)(value + up)),
                    3 => unchecked((byte)(value + ((left + up) / 2))),
                    4 => unchecked((byte)(value + Paeth(left, up, upperLeft))),
                    _ => value
                };
            }
        }

        return output;
    }

    private static byte Paeth(byte a, byte b, byte c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);

        if (pa <= pb && pa <= pc)
        {
            return a;
        }

        return pb <= pc ? b : c;
    }

    private static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
    }

    private static int ReadInt32BigEndian(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }

    private static double Section(StringBuilder content, string title, double y, double x = 48, double width = 499)
    {
        Rect(content, x, y - 4, 4, 14, 0.24, 0.49, 0.91);
        Text(content, title, x + 11, y, 12.2, bold: true, 0.06, 0.09, 0.16);
        Line(content, x, y - 10, x + width, y - 10, 0.9, 0.93, 0.97);
        return y - 23;
    }

    private static double OptionalRow(
        StringBuilder content,
        double y,
        string label,
        string? value,
        double x = 48,
        double width = 499,
        double labelWidth = 150,
        int maxCharacters = 58,
        int maxLines = 2)
    {
        return string.IsNullOrWhiteSpace(value)
            ? y
            : Row(content, y, label, value, x, width, labelWidth, maxCharacters, maxLines);
    }

    private static double Row(
        StringBuilder content,
        double y,
        string label,
        string? value,
        double x = 48,
        double width = 499,
        double labelWidth = 150,
        int maxCharacters = 58,
        int maxLines = 2)
    {
        var lines = WrapText(string.IsNullOrWhiteSpace(value) ? "No registrado" : value, maxCharacters, maxLines);
        var rowHeight = Math.Max(22, 10 + (lines.Count * 10.5));
        var bottom = y - rowHeight;
        var textY = y - 14;
        var valueX = x + labelWidth + 14;

        Rect(content, x, bottom, width, rowHeight, 0.985, 0.99, 1);
        Line(content, x, bottom, x + width, bottom, 0.91, 0.94, 0.98);
        Text(content, label, x + 10, textY, 8.5, bold: true, 0.39, 0.45, 0.55);

        foreach (var line in lines)
        {
            Text(content, line, valueX, textY, 9.4, bold: true, 0.07, 0.1, 0.17);
            textY -= 10.5;
        }

        return bottom;
    }

    private static void SummaryBox(StringBuilder content, double x, double y, string label, string value, bool highlight = false)
    {
        var accentR = highlight ? 0.08 : 0.24;
        var accentG = highlight ? 0.18 : 0.49;
        var accentB = highlight ? 0.39 : 0.91;

        Rect(content, x, y, 155, 58, 0.985, 0.99, 1);
        StrokeRect(content, x, y, 155, 58, 0.86, 0.9, 0.96);
        Rect(content, x, y + 54, 155, 4, accentR, accentG, accentB);
        Text(content, label, x + 12, y + 36, 8.6, bold: true, 0.39, 0.45, 0.55);
        Text(content, value, x + 12, y + 14, highlight ? 16 : 15, bold: true, 0.06, 0.09, 0.16);
    }

    private static void Badge(StringBuilder content, string label, double x, double y)
    {
        Rect(content, x, y, 132, 24, 0.89, 0.95, 1);
        Text(content, label.ToUpperInvariant(), x + 11, y + 8, 9, bold: true, 0.11, 0.26, 0.56);
    }

    private static void Text(StringBuilder content, string? text, double x, double y, double size, bool bold, double r, double g, double b)
    {
        content.Append(Invariant(r)).Append(' ').Append(Invariant(g)).Append(' ').Append(Invariant(b)).AppendLine(" rg");
        content.Append("BT /").Append(bold ? "F2" : "F1").Append(' ').Append(Invariant(size)).Append(" Tf ")
            .Append(Invariant(x)).Append(' ').Append(Invariant(y)).Append(" Td (")
            .Append(EscapePdfText(ToWinAnsiSafeText(text))).AppendLine(") Tj ET");
    }

    private static void Rect(StringBuilder content, double x, double y, double width, double height, double r, double g, double b)
    {
        content.Append(Invariant(r)).Append(' ').Append(Invariant(g)).Append(' ').Append(Invariant(b)).AppendLine(" rg");
        content.Append(Invariant(x)).Append(' ').Append(Invariant(y)).Append(' ').Append(Invariant(width)).Append(' ').Append(Invariant(height)).AppendLine(" re f");
    }

    private static void StrokeRect(StringBuilder content, double x, double y, double width, double height, double r, double g, double b)
    {
        content.Append(Invariant(r)).Append(' ').Append(Invariant(g)).Append(' ').Append(Invariant(b)).AppendLine(" RG");
        content.Append(Invariant(x)).Append(' ').Append(Invariant(y)).Append(' ').Append(Invariant(width)).Append(' ').Append(Invariant(height)).AppendLine(" re S");
    }

    private static void Line(StringBuilder content, double x1, double y1, double x2, double y2, double r, double g, double b)
    {
        content.Append(Invariant(r)).Append(' ').Append(Invariant(g)).Append(' ').Append(Invariant(b)).AppendLine(" RG");
        content.Append(Invariant(x1)).Append(' ').Append(Invariant(y1)).Append(" m ").Append(Invariant(x2)).Append(' ').Append(Invariant(y2)).AppendLine(" l S");
    }

    private static void Image(StringBuilder content, string name, double x, double y, double width, double height)
    {
        content.Append("q ").Append(Invariant(width)).Append(" 0 0 ").Append(Invariant(height)).Append(' ')
            .Append(Invariant(x)).Append(' ').Append(Invariant(y)).Append(" cm /").Append(name).AppendLine(" Do Q");
    }

    private static void ImageFit(StringBuilder content, string name, PdfImage image, double x, double y, double width, double height)
    {
        if (image.Width <= 0 || image.Height <= 0)
        {
            Image(content, name, x, y, width, height);
            return;
        }

        var scale = Math.Min(width / image.Width, height / image.Height);
        var drawWidth = image.Width * scale;
        var drawHeight = image.Height * scale;
        var drawX = x + ((width - drawWidth) / 2);
        var drawY = y + ((height - drawHeight) / 2);

        Image(content, name, drawX, drawY, drawWidth, drawHeight);
    }

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var initials = string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
        return string.IsNullOrWhiteSpace(initials) ? "TC" : initials;
    }

    private static List<string> WrapText(string? value, int maxCharacters, int maxLines)
    {
        var text = string.IsNullOrWhiteSpace(value)
            ? "No registrado"
            : ToWinAnsiSafeText(value).Trim();

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

    private static string Ellipsize(string value, int maxCharacters)
    {
        if (value.Length <= maxCharacters - 3)
        {
            return value + "...";
        }

        return value[..Math.Max(0, maxCharacters - 3)].TrimEnd() + "...";
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString("dd-MM-yyyy HH:mm", ChileCulture)
            : "No registrado";
    }

    private static string? FormatDateOrNull(DateTime? value)
    {
        return value?.ToString("dd-MM-yyyy HH:mm", ChileCulture);
    }

    private static string FormatMoney(decimal value, string moneda)
    {
        return $"{moneda} {value.ToString("N0", ChileCulture)}";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value
            .Select(character => invalidChars.Contains(character) ? '-' : character)
            .ToArray());

        sanitized = sanitized.Trim('-', ' ', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "pago" : sanitized;
    }

    private static string ToWinAnsiSafeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Concat(value.Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark))
            .Replace('ñ', 'n')
            .Replace('Ñ', 'N')
            .Replace("•", "-")
            .Replace("–", "-")
            .Replace("—", "-");
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

    private sealed record PdfImage(
        int Width,
        int Height,
        string ColorSpace,
        string Filter,
        byte[] Bytes,
        PdfImage? SoftMask);

    private sealed class PdfDocument
    {
        private readonly List<string?> _objects = [];

        public int ReserveObject()
        {
            _objects.Add(null);
            return _objects.Count;
        }

        public int AddObject(string content)
        {
            var number = ReserveObject();
            SetObject(number, content);
            return number;
        }

        public int AddStream(byte[] content)
        {
            var body = $"<< /Length {content.Length} >>\nstream\n{BytesAsLatin1(content)}\nendstream";
            return AddObject(body);
        }

        public void SetObject(int number, string content)
        {
            _objects[number - 1] = content;
        }

        public byte[] Build()
        {
            using var stream = new MemoryStream();
            WriteAscii(stream, "%PDF-1.4\n%TuCita\n");
            var offsets = new List<long> { 0 };

            for (var index = 0; index < _objects.Count; index++)
            {
                offsets.Add(stream.Position);
                WriteAscii(stream, $"{index + 1} 0 obj\n{_objects[index]}\nendobj\n");
            }

            var xrefOffset = stream.Position;
            WriteAscii(stream, $"xref\n0 {_objects.Count + 1}\n");
            WriteAscii(stream, "0000000000 65535 f \n");

            foreach (var offset in offsets.Skip(1))
            {
                WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
            }

            WriteAscii(stream, $"trailer\n<< /Size {_objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
            return stream.ToArray();
        }

        private static void WriteAscii(Stream stream, string value)
        {
            var bytes = Encoding.Latin1.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
