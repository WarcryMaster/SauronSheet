# Proposal: ING Raw Column Category/Subcategory Extraction

## Intent

`IngBankPdfParser` entrega el texto izquierdo del bloque a `IngControlledTaxonomy`,
que impone una lista cerrada y, cuando no reconoce el prefijo, fuerza `SubCategory = null`.
Esto viola el contrato real: literales como `Otros gastos`/`Suscripciones` o
`Vehículo y transporte`/`Parking y garaje` no se preservan.
**`IngControlledTaxonomy` debe salir completamente del path de extracción ING.**
La corrección cablea un extractor raw por columnas (`PositionedWord[]` + umbrales
derivados de la cabecera detectada) y preserva los literales exactos del PDF.

## Scope

### In Scope
- Eliminar `IngControlledTaxonomy` de la fase de extracción ING.
- Extractor raw geometry-first: categoría, subcategoría y descripción leídas desde
  zonas X de `IngBlock.Lines`, sin ninguna lista cerrada.
- Fallback conservador: `Category=null`, `SubCategory=null`, descripción literal
  conservada cuando la geometría no produce señal fiable.
- Tests de extracción literal (DAZN, parking, nómina, traspaso) con valores reales del PDF.
- Delta specs para `pdf-category-extraction` (PCE-1) e `ing-block-reconstruction` (IBR-3).

### Out of Scope
- `BankCategoryResolutionService` (sin cambios; sigue siendo el lugar de resolución semántica).
- Otros parsers (path no-ING inalterado).
- Rediseño completo por layout de página completa (Approach 3 descartado).

## Capabilities

### New Capabilities
- None

### Modified Capabilities
- `pdf-category-extraction`: PCE-1 cambia — path ING ya **no** usa `IngControlledTaxonomy`;
  extrae literales de columnas reales con fallback conservador (`null/null`).
- `ing-block-reconstruction`: IBR-3 cambia — extracción de categoría/subcategoría/descripción
  pasa a geometry-first por columnas; elimina resolución L→R vía taxonomía controlada.

## Approach

**Approach 2 — geometry-first con fallback conservador.**
Derivar `IngColumnThresholds` desde cabecera detectada y propagarlos al pipeline real.
Nuevo extractor raw por bloque agrupa palabras por zonas X; zona monetaria explícitamente
excluida para no contaminar descripción. `IngMonetaryExtractor` permanece intacto.

## Affected Areas — 3 Chained Slices (budget 400 líneas/PR)

| Slice | Área | Impacto | Descripción |
|-------|------|---------|-------------|
| 1 | `IngColumnThresholds.cs`, `IngBlock.cs` | Modified | Añadir límite derecho; nuevo `IngRawColumnExtractor` |
| 1 | `IngColumnThresholdsTests.cs`, `IngRawColumnExtractorTests.cs` | New/Modified | Tests unitarios geometría real |
| 2 | `IngBankPdfParser.cs` | Modified | Cablear extractor raw en `ProcessBlocks`; fallback conservador |
| 2 | `IngBankPdfParserBlockTests.cs`, `IngBankPdfParserSingleLineTests.cs` | Modified | Regresiones con literales reales del PDF |
| 3 | `IngControlledTaxonomy.cs`, `IngControlledTaxonomyTests.cs` | Removed | Sale del path; tests sustituidos |
| 3 | `openspec/changes/.../spec-delta.md` | New | Delta specs PCE-1 e IBR-3 |

## Risks

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Zona monetaria contamina descripción | Media | Límite derecho explícito; tests con bloque real antes del cableado |
| Layout variable por página degrada filas posteriores | Baja | Umbrales derivados por cabecera detectada, no globales |
| Más `null` en categoría con fallback conservador | Baja | Preferible a persistir valores inventados |

## Rollback Plan

Revert en orden inverso (Slice 3 → 2 → 1). Cada slice tiene rama autónoma.
`IngControlledTaxonomy` permanece en el repo hasta que Slice 3 se mergea, permitiendo
rollback limpio de Slice 2 sin desestabilizar producción.

## Dependencies

- Exploración completada: Engram `sdd/ing-raw-column-category-subcategory-extraction/explore`.

## Success Criteria

- [ ] `IngBankPdfParser` no importa ni instancia `IngControlledTaxonomy` en ningún path de extracción.
- [ ] Literales del PDF (`Otros gastos`, `Suscripciones`, `Parking y garaje`) preservados exactamente en `RawTransactionRow.Category` y `.SubCategory`.
- [ ] Fallback conservador produce `Category=null`, `SubCategory=null` (no valores inventados).
- [ ] Tests de parsers ING pasan con valores literales reales; `IngControlledTaxonomyTests` eliminados.
- [ ] Delta specs `pdf-category-extraction` e `ing-block-reconstruction` publicadas.
