# Design: ING Single-Line Category Extraction Fix

Preservar coordenadas X de PdfPig hasta `ParseTextColumns` para segmentar columnas en filas single-line; fallback conservador cuando la geometría no ofrece separación clara. El path multi-line queda intacto.

---

## Technical Approach

Introducir un value object `PositionedWord(string Text, double Left)` que viaja desde `ReconstructLinesFromWords` hasta el punto de split de columnas. El método `ParseTextColumns` recibe opcionalmente la lista de positioned words y usa umbrales X derivados del header ING para asignar cada palabra a un bucket (Categoría / Subcategoría / Descripción). Si la fila no ofrece señal X suficiente, todo queda en descripción como hasta ahora.

---

## Architecture Decisions

| Decision | Alternatives | Rationale |
|----------|-------------|-----------|
| Introducir `PositionedWord` record interno en Infrastructure | Usar `Word` de PdfPig directamente; pasar `List<(string, double)>` | Record propio desacopla del tipo externo PdfPig; inmutable, testeable sin dependencia de PdfPig en tests unitarios |
| Derivar umbrales X del header de la misma página | Hardcodear offsets fijos; usar clustering dinámico | El header "F. VALOR / CATEGORÍA / SUBCATEGORÍA / CONCEPTO" define las columnas — su posición X es la fuente de verdad más fiable y resiliente a variaciones de renderizado |
| Fallback conservador = todo en descripción | Lanzar excepción; inferir mejor guess | Pérdida mínima > sobreinferir. Evita falsos positivos que corrompen datos |
| Refactorizar `ReconstructLinesFromWords` para devolver `IngLineData` (texto + words posicionados) | Crear un segundo método paralelo | Un solo pipeline reduce duplicación y riesgo de divergencia |

---

## Data Flow

```
page.GetWords()
      │
      ▼
ReconstructLinesFromWords(words)
      │ returns List<IngLineData>
      │   IngLineData = { Text, PositionedWord[] }
      ▼
FlushRowBuffer()
      │ single-line path
      ▼
ParseIngTransactionLine(lineData, rowNumber)
      │ strips date + trailing numbers
      ▼
ParseTextColumns(textPart, positionedWords[], headerThresholds)
      │
      ├── señal X suficiente → split por buckets → (cat, subcat, desc)
      └── señal X insuficiente → (null, null, fullText) [fallback]
```

El path multi-line usa `lineData.Text` → comportamiento idéntico al actual (sin posiciones X, delega a `IngTransactionLineParser.ExtractFromMultiLine`).

---

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/.../PDF/Parsers/IngBankPdfParser.cs` | Modify | Añadir `PositionedWord` record; refactorizar `ReconstructLinesFromWords` → `List<IngLineData>`; capturar header X thresholds; pasar positioned words a `ParseTextColumns` |
| `src/.../PDF/Parsers/IngColumnThresholds.cs` | Create | Record/static que encapsula la extracción de umbrales X del header ING y la lógica de bucket assignment |
| `tests/.../PDF/Parsers/IngBankPdfParserSingleLineTests.cs` | Replace | Nuevos tests que validan extracción exitosa de categoría/subcategoría con positioned words y fallback conservador |
| `tests/.../PDF/Parsers/IngColumnThresholdsTests.cs` | Create | Tests unitarios para la lógica de thresholds y bucket assignment sin dependencia PdfPig |
| `tests/.../Commands/ImportTransactionsFromPdfCommandTests.cs` | Modify | Añadir caso single-line con categoría recuperada → no `RawOnly` |

---

## Interfaces / Contracts

```csharp
// Infrastructure/PDF/Parsers/IngBankPdfParser.cs (internal)
internal readonly record struct PositionedWord(string Text, double Left);

internal readonly record struct IngLineData(string Text, PositionedWord[] Words);

// Infrastructure/PDF/Parsers/IngColumnThresholds.cs (internal)
internal sealed class IngColumnThresholds
{
    public double CategoryStart { get; }
    public double SubCategoryStart { get; }
    public double DescriptionStart { get; }

    // Construir desde las palabras del header ("CATEGORÍA", "SUBCATEGORÍA", "CONCEPTO")
    public static IngColumnThresholds? FromHeaderWords(PositionedWord[] headerWords);

    // Asignar palabras a buckets; devuelve null si no hay señal suficiente
    public (string? Category, string? SubCategory, string? Description)?
        SplitWords(PositionedWord[] words);
}
```

**Contract de `ParseTextColumns` refactorizado:**
```csharp
private (string? category, string? subCategory, string? description, string? comment)
    ParseTextColumns(string? text, PositionedWord[]? words = null, IngColumnThresholds? thresholds = null);
```

- `words != null && thresholds != null` → intenta split geométrico
- Split exitoso → devuelve (cat, subcat, desc, null)
- Split fallido o parámetros null → devuelve (null, null, text, null) [comportamiento actual]

---

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `IngColumnThresholds.FromHeaderWords` | Fixture con posiciones X reales del header ING enero 2025 |
| Unit | `IngColumnThresholds.SplitWords` — happy path | Words distribuidos en 3 buckets → cat + subcat + desc |
| Unit | `IngColumnThresholds.SplitWords` — fallback | Words todos en bucket desc → returns null |
| Unit | `IngColumnThresholds.SplitWords` — solo categoría | Words en bucket cat + desc, nada en subcat → cat + null + desc |
| Unit | `ParseTextColumns` con geometry | Reemplaza tests actuales de single-line; valida PCE-SLa/SLb/SLc |
| Unit | Multi-line path inalterado | Tests existentes `IngTransactionLineParserTests` pasan sin cambio (PCE-SLd) |
| Integration | Import command handler + fila single-line | Mock de parser devuelve cat/subcat → no cae en `RawOnly` |

---

## Migration / Rollout

No migration required. El cambio es parser-only con fallback conservador — transacciones existentes no se re-procesan. El rollback es revertir el commit.

---

## Open Questions

- [x] ¿Los umbrales X del header son consistentes entre PDFs ING de distintos meses? — Mitigado: se extraen dinámicamente del header de cada página, no se hardcodean.
- [ ] ¿Existe algún caso ING donde el header no contenga "CATEGORÍA" / "SUBCATEGORÍA" / "CONCEPTO"? — Si se detecta, el fallback conservador aplica automáticamente (thresholds = null).
