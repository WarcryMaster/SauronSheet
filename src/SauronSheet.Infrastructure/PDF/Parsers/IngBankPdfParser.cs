namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using SauronSheet.Application.Common.Models;
using SauronSheet.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class IngBankPdfParser : IPdfParser
{
    // Regex para detectar líneas que empiezan con una fecha DD/MM/YYYY
    private static readonly Regex DatePattern = new(
        @"^\d{2}/\d{2}/\d{4}", 
        RegexOptions.Compiled);

    // Regex para extraer importes (positivos y negativos, con decimales)
    private static readonly Regex AmountPattern = new(
        @"-?[\d.,]+", 
        RegexOptions.Compiled);

    // Regex para detectar si una línea completa es un número (importe o saldo en formato multi-línea)
    private static readonly Regex NumericLinePattern = new(
        @"^-?[\d]+[.,\d]*$",
        RegexOptions.Compiled);

    private static readonly string[] KnownCategories =
    [
        "Compras", "Vehículo y transporte", "Alimentación",
        "Otros gastos", "Movimientos excluidos", "Otros ingresos",
        "Educación y salud", "Hogar"
    ];

    private static readonly string[] KnownSubCategories =
    [
        "Belleza, peluquería y perfumería", "Gasolina y combustible",
        "Supermercados y alimentación", "ONG", "Traspaso entre cuentas",
        "Otros ingresos (otros)", "Transferencias", "Ropa y complementos",
        "Mantenimiento de vehículo", "Farmacia, herbolario y nutrición",
        "Luz y gas", "Agua", "Suscripciones", "Teléfono e internet"
    ];

    public Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        var rows = new List<RawTransactionRow>();

        SentrySdk.Logger?.LogInfo("IngBankPdfParser: attempting to parse ING Bank PDF format");
        try
        {
            // PdfPig necesita un byte array o un file path
            using var memoryStream = new MemoryStream();
            pdfStream.CopyTo(memoryStream);
            var pdfBytes = memoryStream.ToArray();

            using var document = PdfDocument.Open(pdfBytes);
            var allLines = new List<string>();
            var pageCount = document.NumberOfPages;

            SentrySdk.AddBreadcrumb(
                $"PDF opened: {pageCount} pages",
                "pdf.parse",
                data: new Dictionary<string, string> { ["pages"] = pageCount.ToString() });

            // 1. Extraer todo el texto línea por línea
            var pageNumber = 0;
            foreach (var page in document.GetPages())
            {
                pageNumber++;
                var words = page.GetWords().ToList();
                var reconstructedLines = ReconstructLinesFromWords(words);
                allLines.AddRange(reconstructedLines);

                SentrySdk.AddBreadcrumb(
                    $"Page {pageNumber}: {words.Count} words → {reconstructedLines.Count} lines",
                    "pdf.parse",
                    data: new Dictionary<string, string>
                    {
                        ["page"] = pageNumber.ToString(),
                        ["words"] = words.Count.ToString(),
                        ["lines"] = reconstructedLines.Count.ToString()
                    });
            }

            SentrySdk.AddBreadcrumb(
                $"Total lines extracted: {allLines.Count}",
                "pdf.parse",
                data: new Dictionary<string, string> { ["totalLines"] = allLines.Count.ToString() });

            // 2. Parsear las líneas extraídas (soportar filas multi-línea)
            var rowNumber = 0;
            var isDataSection = false;
            var skippedLines = 0;
            var failedLines = 0;
            var dateLineCount = 0;

            // Buffer para filas multi-línea
            List<string> rowBuffer = new();

            void FlushRowBuffer()
            {
                if (rowBuffer.Count == 0) return;

                RawTransactionRow? parsed;
                string rawLineForLog;

                if (rowBuffer.Count == 1)
                {
                    var singleLine = rowBuffer[0].Trim();
                    rawLineForLog = singleLine;

                    // Skip bare date-only lines (F. OPERACIÓN date with no transaction data)
                    var dateOnlyRemainder = singleLine.Substring(Math.Min(10, singleLine.Length)).Trim();
                    if (singleLine.Length <= 10 || string.IsNullOrWhiteSpace(dateOnlyRemainder))
                    {
                        SentrySdk.AddBreadcrumb(
                            "Skipping date-only line (operation date without transaction data)",
                            "pdf.row",
                            data: new Dictionary<string, string> { ["line"] = singleLine });
                        rowBuffer.Clear();
                        return;
                    }

                    rowNumber++;
                    parsed = ParseIngTransactionLine(singleLine, rowNumber);
                }
                else
                {
                    rawLineForLog = string.Join(" | ", rowBuffer.Take(3));
                    rowNumber++;
                    parsed = ParseMultiLineTransaction(rowBuffer, rowNumber);
                }

                if (parsed != null)
                {
                    rows.Add(parsed);
                    SentrySdk.AddBreadcrumb(
                        $"Row {rowNumber} parsed: {parsed.Date} | {parsed.Description} | {parsed.Amount}",
                        "pdf.row",
                        data: new Dictionary<string, string>
                        {
                            ["row"] = rowNumber.ToString(),
                            ["date"] = parsed.Date ?? "",
                            ["description"] = parsed.Description ?? "",
                            ["amount"] = parsed.Amount ?? "",
                            ["rawLine"] = rawLineForLog.Length > 200 ? rawLineForLog[..200] : rawLineForLog
                        });
                }
                else
                {
                    failedLines++;
                    SentrySdk.AddBreadcrumb(
                        $"Row {rowNumber} FAILED to parse",
                        "pdf.row",
                        level: BreadcrumbLevel.Warning,
                        data: new Dictionary<string, string>
                        {
                            ["row"] = rowNumber.ToString(),
                            ["rawLine"] = rawLineForLog.Length > 200 ? rawLineForLog[..200] : rawLineForLog
                        });
                }
                rowBuffer.Clear();
            }

            foreach (var line in allLines)
            {
                var trimmed = line.Trim();

                // Detectar inicio de la sección de datos (después del header)
                if (trimmed.Contains("F. VALOR") && trimmed.Contains("CATEGORÍA"))
                {
                    SentrySdk.Logger?.LogDebug("IngBankPdfParser: detected ING header section");
                    SentrySdk.AddBreadcrumb("Header section detected", "pdf.parse");
                    isDataSection = true;
                    continue;
                }

                if (!isDataSection)
                {
                    skippedLines++;
                    continue;
                }

                // Si la línea empieza con fecha, es inicio de nueva transacción
                if (DatePattern.IsMatch(trimmed))
                {
                    // Si hay buffer, procesar la fila anterior
                    FlushRowBuffer();
                    dateLineCount++;
                    rowBuffer.Add(trimmed);
                }
                else if (rowBuffer.Count > 0)
                {
                    // Si ya hay una fila en buffer, agregar líneas adicionales (categoría, subcategoría, descripción, importe, saldo)
                    rowBuffer.Add(trimmed);
                }
                // Si no hay buffer y la línea no empieza con fecha, ignorar
            }
            // Procesar la última fila en buffer
            FlushRowBuffer();

            SentrySdk.Logger?.LogDebug("IngBankPdfParser complete: {0} parsed, {1} failed, {2} date-lines found, {3} pre-header lines skipped", rows.Count, failedLines, dateLineCount, skippedLines);

            SentrySdk.Logger?.LogDebug("IngBankPdfParser: parsed {0} transactions from ING PDF", rows.Count);
        }
        catch (Exception ex) when (ex.Message.Contains("password") || 
                                    ex.Message.Contains("encrypted"))
        {
            SentrySdk.Logger?.LogError("IngBankPdfParser: PDF is password-protected or encrypted");
            throw new InvalidOperationException(
                "El PDF no se puede leer. Puede estar protegido con contraseña o corrupto.", ex);
        }
        catch (Exception ex)
        {
            SentrySdk.Logger?.LogError("IngBankPdfParser: unexpected error — {0}", ex.Message);
            throw new InvalidOperationException(
                $"Error inesperado al parsear el PDF: {ex.Message}", ex);
        }

        return Task.FromResult(rows);
    }

    /// <summary>
    /// Reconstruye líneas a partir de las palabras y sus posiciones Y en la página.
    /// Las palabras con la misma coordenada Y (±tolerancia) pertenecen a la misma línea.
    /// </summary>
    private List<string> ReconstructLinesFromWords(List<Word> words)
    {
        if (!words.Any())
            return new List<string>();

        const double yTolerance = 3.0; // Tolerancia en puntos para agrupar en la misma línea

        // Agrupar palabras por su posición Y (de arriba a abajo)
        var lineGroups = new List<List<Word>>();
        var sortedByY = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();

        List<Word>? currentLine = null;
        double currentY = double.MaxValue;

        foreach (var word in sortedByY)
        {
            var wordY = word.BoundingBox.Bottom;

            if (currentLine == null || Math.Abs(wordY - currentY) > yTolerance)
            {
                // Nueva línea
                currentLine = new List<Word> { word };
                lineGroups.Add(currentLine);
                currentY = wordY;
            }
            else
            {
                currentLine.Add(word);
            }
        }

        // Ordenar palabras dentro de cada línea por posición X (izquierda a derecha)
        var result = new List<string>();
        foreach (var group in lineGroups)
        {
            var orderedWords = group.OrderBy(w => w.BoundingBox.Left);
            var lineText = string.Join(" ", orderedWords.Select(w => w.Text));
            result.Add(lineText);
        }

        return result;
    }

    /// <summary>
    /// Parsea un bloque multi-línea de transacción ING donde cada campo está en su propia línea.
    /// Estructura esperada: [0]=Fecha, [1]=Categoría, [2]=Subcategoría, [3..n-2]=Descripción, [n-1]=Importe, [n]=Saldo
    /// </summary>
    private RawTransactionRow? ParseMultiLineTransaction(List<string> lines, int rowNumber)
    {
        try
        {
            var firstLine = lines[0].Trim();
            var dateMatch = DatePattern.Match(firstLine);
            if (!dateMatch.Success) return null;
            var date = dateMatch.Value;

            // Buscar importe y saldo desde el final (líneas numéricas puras)
            string? amount = null;
            string? balance = null;
            int firstNumericIndex = lines.Count; // índice exclusivo del primer numérico desde el final

            for (int i = lines.Count - 1; i >= 1; i--)
            {
                var trimmed = lines[i].Trim();
                if (NumericLinePattern.IsMatch(trimmed))
                {
                    if (balance == null)
                    {
                        balance = trimmed;
                        firstNumericIndex = i;
                    }
                    else if (amount == null)
                    {
                        amount = trimmed;
                        firstNumericIndex = i;
                        break;
                    }
                }
                else
                {
                    break; // Parar al primer no-numérico desde el final
                }
            }

            // Líneas de texto: desde índice 1 hasta firstNumericIndex (exclusive)
            // Skip(1) elimina la fecha; Take(firstNumericIndex - 1) toma hasta antes de los numéricos
            var textLines = lines
                .Skip(1)
                .Take(firstNumericIndex - 1)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            string? category = null;
            string? subCategory = null;
            string? description = null;
            int descriptionStart = 0;

            for (int i = 0; i < textLines.Count; i++)
            {
                if (category == null && KnownCategories.Any(c => string.Equals(c, textLines[i], StringComparison.OrdinalIgnoreCase)))
                {
                    category = textLines[i];
                    descriptionStart = i + 1;
                }
                else if (category != null && subCategory == null && KnownSubCategories.Any(s => string.Equals(s, textLines[i], StringComparison.OrdinalIgnoreCase)))
                {
                    subCategory = textLines[i];
                    descriptionStart = i + 1;
                }
                else
                {
                    // Esta y el resto de líneas forman la descripción
                    descriptionStart = i;
                    break;
                }
            }

            if (descriptionStart < textLines.Count)
            {
                var desc = string.Join(" ", textLines.Skip(descriptionStart)).Trim();
                description = string.IsNullOrWhiteSpace(desc) ? null : desc;
            }

            return new RawTransactionRow(
                rowNumber, date, category, subCategory, description, null,
                NormalizeAmount(amount), NormalizeAmount(balance));
        }
        catch (Exception ex)
        {
            SentrySdk.AddBreadcrumb(
                $"Row {rowNumber} multi-line exception: {ex.Message}",
                "pdf.row",
                level: BreadcrumbLevel.Error,
                data: new Dictionary<string, string>
                {
                    ["row"] = rowNumber.ToString(),
                    ["error"] = ex.Message
                });
            return null;
        }
    }

    /// <summary>
    /// Parsea una línea de transacción del formato ING.
    /// Formato esperado: DD/MM/YYYY Categoría Subcategoría Descripción [Comentario] Importe Saldo
    /// </summary>
    private RawTransactionRow? ParseIngTransactionLine(string line, int rowNumber)
    {
        try
        {
            // Extraer la fecha (primeros 10 caracteres)
            var dateMatch = DatePattern.Match(line);
            if (!dateMatch.Success)
                return null;

            var date = dateMatch.Value;
            var remainder = line.Substring(dateMatch.Length).Trim();

            // Extraer los números del final (Importe y Saldo)
            // Buscamos los últimos dos números con formato -?X,XXX.XX o -?X.XXX,XX
            var numbers = ExtractTrailingNumbers(remainder);

            string? amount = null;
            string? balance = null;
            string? textPart = remainder;

            if (numbers.Count >= 2)
            {
                balance = numbers[^1];
                amount = numbers[^2];

                // Remover los números del texto
                var lastNumberIndex = remainder.LastIndexOf(numbers[^1]);
                if (lastNumberIndex > 0)
                {
                    var secondLastIndex = remainder.LastIndexOf(numbers[^2], lastNumberIndex - 1);
                    if (secondLastIndex > 0)
                    {
                        textPart = remainder.Substring(0, secondLastIndex).Trim();
                    }
                }
            }
            else if (numbers.Count == 1)
            {
                amount = numbers[0];
                var lastIndex = remainder.LastIndexOf(numbers[0]);
                if (lastIndex > 0)
                    textPart = remainder.Substring(0, lastIndex).Trim();
            }

            // Intentar separar Categoría, Subcategoría, Descripción del texto
            // Esto es heurístico ya que depende de cómo PdfPig extraiga el texto
            var (category, subCategory, description, comment) = 
                ParseTextColumns(textPart);

            return new RawTransactionRow(
                rowNumber,
                date,
                category,
                subCategory,
                description,
                comment,
                NormalizeAmount(amount),
                NormalizeAmount(balance)
            );
        }
        catch (Exception ex)
        {
            SentrySdk.AddBreadcrumb(
                $"Row {rowNumber} exception: {ex.Message}",
                "pdf.row",
                level: BreadcrumbLevel.Error,
                data: new Dictionary<string, string>
                {
                    ["row"] = rowNumber.ToString(),
                    ["error"] = ex.Message,
                    ["rawLine"] = line.Length > 200 ? line[..200] : line
                });
            return null;
        }
    }

    /// <summary>
    /// Extrae los números del final de la cadena (importe y saldo).
    /// </summary>
    private List<string> ExtractTrailingNumbers(string text)
    {
        // Patrón para números con formato europeo: -1.000,00 o 1,000.00
        var numberPattern = new Regex(@"-?[\d]+[.,\d]*");
        var matches = numberPattern.Matches(text);
        
        var numbers = new List<string>();
        foreach (Match match in matches)
        {
            // Verificar que parece un número válido (tiene al menos un dígito)
            if (match.Value.Any(char.IsDigit))
            {
                numbers.Add(match.Value);
            }
        }

        return numbers;
    }

    /// <summary>
    /// Intenta separar las columnas de texto (Categoría, Subcategoría, Descripción, Comentario).
    /// </summary>
    private (string? category, string? subCategory, string? description, string? comment) 
        ParseTextColumns(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null, null, null);

        string? category = null;
        string? subCategory = null;
        string? description = null;
        string? comment = null;

        // Intentar encontrar la categoría conocida
        foreach (var cat in KnownCategories.OrderByDescending(c => c.Length))
        {
            if (text.Contains(cat, StringComparison.OrdinalIgnoreCase))
            {
                category = cat;
                var catIndex = text.IndexOf(cat, StringComparison.OrdinalIgnoreCase);
                text = text.Remove(catIndex, cat.Length).Trim();
                break;
            }
        }

        // Intentar encontrar la subcategoría conocida
        foreach (var sub in KnownSubCategories.OrderByDescending(s => s.Length))
        {
            if (text.Contains(sub, StringComparison.OrdinalIgnoreCase))
            {
                subCategory = sub;
                var subIndex = text.IndexOf(sub, StringComparison.OrdinalIgnoreCase);
                text = text.Remove(subIndex, sub.Length).Trim();
                break;
            }
        }

        // Lo que queda es Descripción + posible Comentario
        // La descripción suele empezar con "Pago en", "Recibo", "Traspaso", "Transferencia"
        description = text.Trim();
        if (string.IsNullOrEmpty(description))
            description = null;

        return (category, subCategory, description, comment);
    }

    /// <summary>
    /// Normaliza el formato del importe: soporta ambos formatos (europeo con coma decimal y anglo con punto decimal).
    /// Convierte a formato estándar con punto decimal y sin separadores de miles.
    /// Ejemplos: "1,246.74" → "1246.74", "1.246,74" → "1246.74", "0.82" → "0.82", "0,82" → "0.82"
    /// </summary>
    private static string? NormalizeAmount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount))
            return null;

        amount = amount.Trim();

        // Si no tiene punto ni coma, retornar como está
        if (!amount.Contains(',') && !amount.Contains('.'))
            return amount;

        // Si tiene ambos separadores, detectar cuál es decimal y cuál es de miles
        if (amount.Contains(',') && amount.Contains('.'))
        {
            // El último separador (más a la derecha) es el decimal
            var lastCommaIndex = amount.LastIndexOf(',');
            var lastDotIndex = amount.LastIndexOf('.');

            string normalized;
            if (lastCommaIndex > lastDotIndex)
            {
                // Coma es decimal: "1.246,74" → "1246.74"
                normalized = amount
                    .Replace(".", string.Empty)  // Quitar separador de miles
                    .Replace(",", ".");          // Coma a punto decimal
            }
            else
            {
                // Punto es decimal: "1,246.74" → "1246.74"
                normalized = amount.Replace(",", string.Empty);  // Quitar separador de miles
            }

            return normalized;
        }

        // Si solo tiene coma, es probablemente decimal (formato europeo)
        if (amount.Contains(',') && !amount.Contains('.'))
        {
            return amount.Replace(",", ".");
        }

        // Si solo tiene punto, es probablemente decimal o parte de "1.234" sin decimales
        // Retornar como está (ya tiene punto decimal)
        return amount;
    }
}