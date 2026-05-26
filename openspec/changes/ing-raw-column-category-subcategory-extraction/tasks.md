# Tasks: ING Raw Column Category/Subcategory Extraction

## Review Workload Forecast

| Campo | Valor |
|-------|-------|
| Líneas estimadas (A+D total) | ~880–1,000 (distribuidas en 3 slices) |
| Riesgo presupuesto 400 líneas | Alto global; PR 1: Medio (~350), PR 2: Bajo (~270), PR 3: Alto (~651) |
| Chained PRs recomendados | Sí |
| Split sugerido | PR 1 → PR 2 → PR 3 |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unidad | Objetivo | PR | Base de rama | Notas |
|--------|----------|----|--------------|-------|
| 1 | Fundación: `IngColumnThresholds` + `IngRawColumnExtractor` | PR 1 | feature/tracker branch | ~350 A+D; TDD incluido |
| 2 | Integración: `IngBankPdfParser` cableado raw | PR 2 | rama PR 1 | ~270 A+D; regresión con literales reales |
| 3 | Limpieza: eliminar `IngControlledTaxonomy` | PR 3 | rama PR 2 | ~651 A+D solo deletions; pedir `size:exception` al maintainer |

---

## Fase 1: Fundación — IngColumnThresholds + IngRawColumnExtractor (PR 1)

- [x] 1.1 [RED] `IngColumnThresholdsTests.cs`: añadir test que `MonetaryZoneStart` se deriva de la posición X de "IMPORTE"/"COMENTARIO" en la cabecera detectada real
- [x] 1.2 [GREEN] `IngColumnThresholds.cs`: añadir propiedad `MonetaryZoneStart` calculada desde la cabecera detectada (límite derecho de la zona de descripción)
- [x] 1.3 [RED] Crear `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngRawColumnExtractorTests.cs`: escenarios IBR-3a (DAZN → Compras Online / Online / DAZN), IBR-3b (parking multipalabra → Vehículo y transporte / Parking y garaje), IBR-3c (nómina → Nominas / null), IBR-3d (traspaso con categoría multipalabra), IBR-3e (geometría insuficiente → null/null), IBR-3f (zona monetaria excluida de descripción)
- [x] 1.4 [GREEN] Crear `src/SauronSheet.Infrastructure/PDF/Parsers/IngRawColumnExtractor.cs`: `readonly record struct IngRawColumnResult(string? Category, string? SubCategory, string? Description)` + clase estática interna con `static IngRawColumnResult Extract(PositionedWord[] anchorWords, IngLineData[]? continuationLines, IngColumnThresholds thresholds)` clasificando palabras por zonas X
- [x] 1.5 [REFACTOR] `IngRawColumnExtractor.cs`: limpiar agrupación de palabras multipalabra por zona, garantizar exclusión explícita del rango `[MonetaryZoneStart, ∞)`, y asegurar que continuaciones aportan solo a descripción

---

## Fase 2: Integración — IngBankPdfParser (PR 2)

- [x] 2.1 [RED] `IngBankPdfParserBlockTests.cs`: actualizar escenarios DAZN, parking, nómina, Bizum y traspaso para esperar literales exactos del PDF en `Category`, `SubCategory` y `Description`; eliminar toda expectativa de valores de taxonomy controlada
- [x] 2.2 [RED] `IngBankPdfParserSingleLineTests.cs`: ajustar los tests afectados por la eliminación del path taxonomy (campo `Category` y `SubCategory` ahora puede ser `null` en casos sin geometría)
- [x] 2.3 [GREEN] `IngBankPdfParser.cs` — `ProcessBlocks()`: añadir parámetro `IngColumnThresholds thresholds`; derivar umbrales (con `MonetaryZoneStart`) desde la cabecera detectada en `ParseAsync`; reemplazar la llamada a `IngControlledTaxonomy.ExtractLeftToRight()` por `IngRawColumnExtractor.Extract(anchorWords, continuations, thresholds)`
- [x] 2.4 [GREEN] `IngBankPdfParser.cs`: implementar fallback conservador — cuando `Extract()` devuelve geometría insuficiente: `Category=null`, `SubCategory=null`, `Description=fullText`; la transacción nunca se descarta solo por falta de categoría
- [x] 2.5 [REFACTOR] `IngBankPdfParser.cs`: confirmar que los fixes de multiline y repeated-header permanecen intactos; dejar `IngControlledTaxonomy` como dead code (no instanciado) hasta PR 3

---

## Fase 3: Limpieza — Eliminar IngControlledTaxonomy (PR 3, size:exception)

- [ ] 3.1 Eliminar `src/SauronSheet.Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs`; verificar que `IngBankPdfParser.cs` no contiene ningún `using`, instancia ni referencia a la clase
- [ ] 3.2 Eliminar `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngControlledTaxonomyTests.cs`
- [ ] 3.3 `dotnet build && dotnet test` — compilación limpia, 0 referencias a `IngControlledTaxonomy`, todos los tests verdes
