namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
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
            var allLines = new List<IngLineData>();
            var pageCount = document.NumberOfPages;

            SentrySdk.AddBreadcrumb(
                $"PDF opened: {pageCount} pages",
                "pdf.parse",
                data: new Dictionary<string, string> { ["pages"] = pageCount.ToString() });

            // 1. Extraer todas las líneas con sus posiciones X por página
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

            // Umbrales X del header de la página actual (re-calibrado en cada header detectado)
            IngColumnThresholds? pageThresholds = null;

            // Buffer para filas multi-línea
            List<IngLineData> rowBuffer = new();

            void FlushRowBuffer()
            {
                if (rowBuffer.Count == 0) return;

                RawTransactionRow? parsed;
                string rawLineForLog;

                if (rowBuffer.Count == 1)
                {
                    var lineData = rowBuffer[0];
                    var singleLine = lineData.Text.Trim();
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
                    parsed = ParseIngTransactionLine(lineData, rowNumber, pageThresholds);
                }
                else
                {
                    rawLineForLog = string.Join(" | ", rowBuffer.Take(3).Select(x => x.Text));
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

            foreach (var lineData in allLines)
            {
                var trimmed = lineData.Text.Trim();

                // Detectar inicio de la sección de datos (después del header) y capturar umbrales X
                if (trimmed.Contains("F. VALOR") && trimmed.Contains("CATEGORÍA"))
                {
                    SentrySdk.Logger?.LogDebug("IngBankPdfParser: detected ING header section");
                    SentrySdk.AddBreadcrumb("Header section detected", "pdf.parse");
                    pageThresholds = IngColumnThresholds.FromHeaderWords(lineData.Words);
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
                    rowBuffer.Add(lineData);
                }
                else if (rowBuffer.Count > 0)
                {
                    // Si ya hay una fila en buffer, agregar líneas adicionales
                    rowBuffer.Add(lineData);
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
    /// Devuelve <see cref="IngLineData"/> que lleva tanto el texto como las posiciones X
    /// de cada palabra para permitir segmentación posterior de columnas.
    /// </summary>
    private List<IngLineData> ReconstructLinesFromWords(List<Word> words)
    {
        if (!words.Any())
            return new List<IngLineData>();

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
        // y construir IngLineData con texto + words posicionadas
        var result = new List<IngLineData>();
        foreach (var group in lineGroups)
        {
            var orderedWords = group.OrderBy(w => w.BoundingBox.Left).ToList();
            var lineText = string.Join(" ", orderedWords.Select(w => w.Text));
            var positionedWords = orderedWords
                .Select(w => new PositionedWord(w.Text, w.BoundingBox.Left))
                .ToArray();
            result.Add(new IngLineData(lineText, positionedWords));
        }

        return result;
    }

    /// <summary>
    /// Parsea un bloque multi-línea de transacción ING.
    /// Maneja dos casos:
    /// 1. Línea 0 con datos completos: [0]=Fecha+Categoría+Subcategoría+Descripción+Importe+Saldo, [1+]=Descripción adicional
    /// 2. Línea 0 con solo fecha: [0]=Fecha, [1]=Categoría, [2]=Subcategoría, [3..n-2]=Descripción, [n-1]=Importe, [n]=Saldo
    ///
    /// PCE-SLd: el path multi-línea usa únicamente el texto de cada línea (.Text).
    /// No se propagan coordenadas X ni umbrales al extractor multi-línea.
    /// </summary>
    private RawTransactionRow? ParseMultiLineTransaction(List<IngLineData> lines, int rowNumber)
    {
        try
        {
            var firstLine = lines[0].Text.Trim();
            var dateMatch = DatePattern.Match(firstLine);
            if (!dateMatch.Success) return null;

            string? date = dateMatch.Value;

            // Intenta parsear línea 0 como una línea completa (contiene fecha + datos + números)
            string? amount = null;
            string? balance = null;
            string? textPart = firstLine.Substring(dateMatch.Length).Trim();

            // Buscar números al final de la primera línea
            List<string> firstLineNumbers = ExtractTrailingNumbers(textPart);

            if (firstLineNumbers.Count >= 2)
            {
                // Línea 0 contiene importe y saldo: delegar al path single-line
                // PCE-SLd: multi-line path no propaga posiciones — thresholds: null
                RawTransactionRow? result = ParseIngTransactionLine(lines[0], rowNumber, thresholds: null);

                if (result != null && lines.Count > 1)
                {
                    // Si hay líneas adicionales, concatenarlas a la descripción
                    List<string> additionalLines = new();
                    for (int i = 1; i < lines.Count; i++)
                    {
                        string trimmedAdditional = lines[i].Text.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedAdditional))
                        {
                            additionalLines.Add(trimmedAdditional);
                        }
                    }

                    if (additionalLines.Count > 0)
                    {
                        string additionalDesc = string.Join(" ", additionalLines);
                        string combinedDesc = string.IsNullOrEmpty(result.Description)
                            ? additionalDesc
                            : $"{result.Description} {additionalDesc}";

                        result = result with { Description = combinedDesc };
                    }
                }

                return result;
            }

            // Caso 2: Línea 0 solo contiene fecha; buscar números en líneas posteriores
            int firstNumericIndex = lines.Count;

            for (int i = lines.Count - 1; i >= 1; i--)
            {
                string trimmed = lines[i].Text.Trim();
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
                    break;
                }
            }

            // Líneas de texto: desde índice 1 hasta firstNumericIndex (exclusive)
            List<string> textLines = new();
            for (int i = 1; i < firstNumericIndex; i++)
            {
                string trimmedLine = lines[i].Text.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    textLines.Add(trimmedLine);
                }
            }

            // PCE-SLd: multi-line extraction — position-first by line order, no X coordinates.
            // textLines[0] → category, textLines[1] → subcategory, textLines[2+] → description.
            var (category, subCategory, description) =
                IngTransactionLineParser.ExtractFromMultiLine(textLines);

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
    /// Parsea una línea de transacción del formato ING (path single-line).
    /// Formato esperado: DD/MM/YYYY Categoría Subcategoría Descripción [Comentario] Importe Saldo
    /// </summary>
    private RawTransactionRow? ParseIngTransactionLine(
        IngLineData lineData,
        int rowNumber,
        IngColumnThresholds? thresholds = null)
    {
        try
        {
            var line = lineData.Text;

            // Extraer la fecha (primeros 10 caracteres)
            var dateMatch = DatePattern.Match(line);
            if (!dateMatch.Success)
                return null;

            var date = dateMatch.Value;
            var remainder = line.Substring(dateMatch.Length).Trim();

            // Extraer los números del final (Importe y Saldo)
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
            // usando coordenadas X cuando están disponibles (PCE-SL)
            var (category, subCategory, description, comment) =
                ParseTextColumns(textPart, lineData.Words, thresholds);

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
                    ["rawLine"] = lineData.Text.Length > 200 ? lineData.Text[..200] : lineData.Text
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
    /// Extrae categoría, subcategoría, descripción y comentario del texto de la
    /// parte central de una línea single-line (después de quitar fecha y números finales).
    ///
    /// Cuando se proporcionan <paramref name="words"/> y <paramref name="thresholds"/>,
    /// intenta segmentar las columnas usando las coordenadas X de las palabras (PCE-SL).
    /// Si la segmentación no produce señal de categoría (PCE-SLb) o los parámetros de
    /// posición no están disponibles, aplica el fallback conservador: todo el texto se
    /// devuelve como descripción y categoría/subcategoría quedan en null.
    /// </summary>
    private (string? category, string? subCategory, string? description, string? comment)
        ParseTextColumns(string? text, PositionedWord[]? words = null, IngColumnThresholds? thresholds = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null, null, null);

        // Intentar segmentación por geometría X cuando ambos parámetros están disponibles
        if (words is { Length: > 0 } && thresholds is not null)
        {
            var split = thresholds.SplitWords(words);
            if (split is not null)
            {
                // PCE-SLa / PCE-SLc: segmentación geométrica exitosa
                return (split.Value.Category, split.Value.SubCategory, split.Value.Description, null);
            }

            // PCE-SLb: señal X insuficiente — fallback conservador
            SentrySdk.AddBreadcrumb(
                "ParseTextColumns: X-signal insufficient — fallback to full-text description",
                "pdf.parse",
                data: new Dictionary<string, string>
                {
                    ["text"] = text.Length > 100 ? text[..100] : text
                });
        }

        // Fallback: texto completo como descripción (comportamiento anterior)
        var description = text.Trim();
        return (null, null, string.IsNullOrEmpty(description) ? null : description, null);
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
