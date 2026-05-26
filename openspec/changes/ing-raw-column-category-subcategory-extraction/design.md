# Design: ING Raw Column Category/Subcategory Extraction

## Enfoque técnico

Reemplazar `IngControlledTaxonomy.ExtractLeftToRight()` en `ProcessBlocks` por extracción geométrica
directa sobre `PositionedWord[]` de la línea ancla del bloque. Los umbrales de columna se derivan
de la cabecera detectada (ya existente) y se propagan como parámetro al pipeline.
Zona monetaria explícitamente excluida mediante umbral derecho nuevo.

## Decisiones de arquitectura

| Decisión | Opción elegida | Alternativas descartadas | Razón |
|----------|---------------|--------------------------|-------|
| Extractor por bloque | Nuevo `IngRawColumnExtractor` estático | Reusar `ParseTextColumns` existente | `ParseTextColumns` es instancia y está acoplado al path single-line; un extractor estático puro es testeable sin instanciar el parser |
| Propagación de umbrales | `ProcessBlocks` recibe `IngColumnThresholds?` como parámetro | Derivar thresholds dentro de cada bloque | La cabecera se detecta una vez en `ParseAsync`; propagarla evita re-escanear y es determinista |
| Límite derecho (zona monetaria) | Añadir `MonetaryZoneStart` a `IngColumnThresholds` derivado de "COMENTARIO" o "IMPORTE" | Excluir por regex en el extractor | El header ya tiene X posición de IMPORTE (~465pt); usarla como hard boundary es más fiable que heurística por contenido |
| Multiline: columnas de continuación | Solo la línea ancla aporta categoría/subcategoría; continuaciones → descripción | Extraer columnas de cada línea | Continuaciones carecen de geometría columnar fiable; solo el anchor tiene las 7 zonas alineadas |
| Fallback conservador | `Category=null`, `SubCategory=null`, `Description=fullText` | Tomar primeros tokens como raw category (actual `BuildRawOnlyResult`) | Preservar `null` es honesto; no inventar literales si la geometría no produce señal |

## Flujo de datos

```
ParseAsync
  │
  ├─ allLines[headerLineIndex].Words
  │    └─► IngColumnThresholds.FromHeaderWords(words)  ← añadir MonetaryZoneStart
  │
  ▼
ProcessBlocks(dataLines, thresholds?)
  │
  ├─ IngBlockAssembler.Assemble(dataLines)  [sin cambios]
  │
  ├─ IngMonetaryExtractor.ExtractRightToLeft(block.FullText)  [sin cambios]
  │
  └─► IngRawColumnExtractor.Extract(block.Lines[0].Words, thresholds)
         │
         ├─ Words con X < thresholds.CategoryStart → ignorar (fecha)
         ├─ Words con X ∈ [CategoryStart, SubCategoryStart) → Category
         ├─ Words con X ∈ [SubCategoryStart, DescriptionStart) → SubCategory
         ├─ Words con X ∈ [DescriptionStart, MonetaryZoneStart) → Description (anchor)
         ├─ Words con X ≥ MonetaryZoneStart → excluir
         └─ Continuaciones (Lines[1..n]) → append Description
```

## Cambios de ficheros

| Fichero | Acción | Descripción |
|---------|--------|-------------|
| `Infrastructure/PDF/Parsers/IngColumnThresholds.cs` | Modificar | Añadir propiedad `MonetaryZoneStart`; detectar "COMENTARIO"/"IMPORTE" en `FromHeaderWords` |
| `Infrastructure/PDF/Parsers/IngRawColumnExtractor.cs` | Crear | Clase estática con `Extract(PositionedWord[] anchorWords, IngLineData[]? continuations, IngColumnThresholds thresholds)` → `IngRawColumnResult` |
| `Infrastructure/PDF/Parsers/IngBankPdfParser.cs` | Modificar | Derivar thresholds del header; pasar a `ProcessBlocks`; reemplazar llamada a `IngControlledTaxonomy` |
| `Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs` | Eliminar | Sale del path de extracción ING (Slice 3) |
| `Tests/.../IngColumnThresholdsTests.cs` | Modificar | Tests para `MonetaryZoneStart` y detección de IMPORTE/COMENTARIO |
| `Tests/.../IngRawColumnExtractorTests.cs` | Crear | Tests unitarios con fixtures reales (DAZN, Parking, Nómina, Traspaso) |
| `Tests/.../IngBankPdfParserBlockTests.cs` | Modificar | Actualizar assertions: `Category`/`SubCategory` con literales reales |
| `Tests/.../IngControlledTaxonomyTests.cs` | Eliminar | Cubiertos por los nuevos tests del extractor raw |

## Interfaces / Contratos

```csharp
// Resultado del extractor raw por columnas
internal readonly record struct IngRawColumnResult(
    string? Category,
    string? SubCategory,
    string? Description);

// Nuevo método estático
internal static class IngRawColumnExtractor
{
    internal static IngRawColumnResult Extract(
        PositionedWord[] anchorWords,
        IngLineData[]? continuationLines,
        IngColumnThresholds thresholds);
}

// IngColumnThresholds — propiedad añadida
public double MonetaryZoneStart { get; }
```

## Estrategia de testing

| Capa | Qué probar | Enfoque |
|------|-----------|---------|
| Unit | `IngColumnThresholds.FromHeaderWords` con IMPORTE/COMENTARIO | xUnit, fixtures reales X positions |
| Unit | `IngRawColumnExtractor.Extract` — DAZN, Parking, Nómina, Traspaso, fallback null | xUnit, `PositionedWord[]` fixtures |
| Unit | `IngRawColumnExtractor.Extract` — exclusión zona monetaria | xUnit, words en zona X ≥ MonetaryZoneStart |
| Integration | `ProcessBlocks` end-to-end con thresholds | xUnit, tests existentes actualizados |

## Migración / Rollout

Sin migración de datos. Cambio interno al parser — no afecta schema, API ni persistencia.
Rollout en 3 slices independientes (merge por orden). Rollback por revert de slice.

## Preguntas abiertas

Ninguna. Todas las decisiones están resueltas con evidencia del código existente.
