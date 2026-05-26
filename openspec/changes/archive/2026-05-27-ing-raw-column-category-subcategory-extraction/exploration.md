## Exploration: ing-raw-column-category-subcategory-extraction

### Current State
`IngBankPdfParser.ProcessBlocks(...)` monta bloques con `IngBlockAssembler`, extrae importe/saldo con `IngMonetaryExtractor.ExtractRightToLeft(...)` y después entrega TODO el texto izquierdo a `IngControlledTaxonomy.ExtractLeftToRight(...)` (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs`). Esa taxonomía está cerrada sobre `Seed` y, cuando no reconoce el prefijo, `BuildRawOnlyResult(...)` consume solo los dos primeros tokens como categoría y fuerza `SubCategory = null` (`src/SauronSheet.Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs`). Con el contrato real del PDF ING eso viola los ejemplos confirmados por el usuario, porque categorías como `Otros gastos` y subcategorías como `Suscripciones` o `Parking y garaje` no pueden preservarse literalmente por ese camino.

La geometría útil YA existe: `ReconstructLinesFromWords(...)` llena `IngLineData.Words`, `IngBlock` conserva `Lines`, y `IngColumnThresholds.SplitWords(...)` ya segmenta por X (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs`, `src/SauronSheet.Infrastructure/PDF/Parsers/IngBlock.cs`, `src/SauronSheet.Infrastructure/PDF/Parsers/IngColumnThresholds.cs`). El problema es de cableado: `IngColumnThresholds.FromHeaderWords(...)` y `ParseTextColumns(...)` no participan en el pipeline ING real; hoy solo se usan desde tests. Además, `IngColumnThresholds` solo conoce inicios de categoría/subcategoría/descripción, NO un límite derecho para excluir importe/saldo si se reutiliza como fuente primaria sin ampliar el modelo.

El resto del sistema ya está preparado para trabajar con literales raw del PDF: `ImportTransactionsFromPdfCommandHandler` persiste `row.Category`/`row.SubCategory` en `BankCategory`/`BankSubcategory`, `BankCategoryResolutionService` normaliza y resuelve DESPUÉS, y el frontend prioriza `BankCategory` para mostrar la categoría importada (`src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs`, `src/SauronSheet.Application/Services/BankCategoryResolutionService.cs`, `src/SauronSheet.Frontend/Helpers/TransactionCategoryDisplayHelper.cs`). Así que la dependencia fuerte con taxonomía está localizada en Infrastructure + specs/tests.

### Affected Areas
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs` — punto central: hoy llama a `ExtractTaxonomyInput(...)` + `IngControlledTaxonomy`; habrá que cablear extracción raw por columnas y pasar umbrales geométricos al pipeline.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs` — implementación que hoy impone la lista cerrada y el fallback que nulifica la subcategoría; debe salir de la fase de extracción o quedar fuera del path ING.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngColumnThresholds.cs` — base reutilizable, pero insuficiente tal como está; necesita soportar límites derechos relevantes del bloque real o un extractor que ignore explícitamente zona monetaria.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBlock.cs` — ya conserva `Lines`; probablemente deba convertirse en la superficie de lectura del nuevo extractor raw por columnas.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngControlledTaxonomyTests.cs` — hoy fijan el contrato incorrecto de taxonomía + `RawOnly`; deben sustituirse por tests de extracción literal desde columnas.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserBlockTests.cs` — deben pasar de expectativas semánticas (`Ocio`/`Parking`) a expectativas literales reales (`Vehículo y transporte`/`Parking y garaje`, etc.).
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngColumnThresholdsTests.cs` — siguen siendo útiles, pero ahora deben cubrir el caso completo de fila real, no solo buckets recortados manualmente.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserSingleLineTests.cs` — hoy validan un helper privado no cableado; deben migrar a proteger el extractor real o eliminarse si dejan de representar producción.
- `openspec/specs/pdf-category-extraction/spec.md` — PCE-1 codifica explícitamente `IngControlledTaxonomy` como fuente primaria; necesita delta.
- `openspec/specs/ing-block-reconstruction/spec.md` — IBR-3 también codifica resolución L→R vía taxonomía; necesita delta para extracción literal por columnas.

### Approaches
1. **Heurística textual sin geometría** — quitar la taxonomía y tratar de partir `taxonomyInput` en categoría/subcategoría/descripción solo con texto.
   - Pros: diff pequeño; no exige nuevo plumbing.
   - Cons: seguiría adivinando; no hay separadores fiables entre categoría, subcategoría y descripción; rompería el contrato literal justo donde el usuario ha dado contraejemplos reales.
   - Effort: Medium

2. **Extracción raw geometry-first con fallback conservador** — mantener `IngBlockAssembler` + `IngMonetaryExtractor`, pero sustituir la fase de taxonomía por un extractor de columnas basado en `PositionedWord[]` y umbrales derivados de la cabecera.
   - Pros: preserva literales exactos del PDF; reutiliza infraestructura ya existente; el impacto queda concentrado en parser/specs/tests; respeta que la resolución semántica ocurra después en Application.
   - Cons: exige pasar umbrales reales hasta `ProcessBlocks(...)` y ampliar el modelo actual para no mezclar importe/saldo con descripción.
   - Effort: Medium

3. **Rehacer todo el parser ING por layout completo** — rediseñar detección, bloques y columnas alrededor de geometría de página completa.
   - Pros: máxima fidelidad potencial.
   - Cons: es MÁS grande de lo necesario para este bug, aumenta riesgo de revisión >400 líneas y mezcla una corrección contractual con un refactor amplio.
   - Effort: High

### Recommendation
Recomiendo **Approach 2**. La corrección mínima SEGURA es sacar la taxonomía de la fase de extracción y hacer que `IngBankPdfParser` lea directamente las columnas reales del PDF.

Plan mínimo:
- Derivar `IngColumnThresholds` desde la cabecera detectada y propagarlos al pipeline real; hoy `ProcessBlocks(...)` no recibe esa información.
- Añadir un extractor raw por bloque que recorra `block.Lines`, agrupe palabras por zonas X y concatene en orden de lectura para `Category`, `SubCategory` y `Description`.
- Mantener `IngMonetaryExtractor` como fuente de importe/saldo y evitar que la zona monetaria contamine la descripción; con el modelo actual no basta reutilizar `SplitWords(...)` tal cual.
- Cuando la geometría no sea suficiente, aplicar fallback conservador SIN taxonomía: conservar el texto como descripción y dejar categoría/subcategoría a `null`, en vez de inventar valores desde una lista cerrada.
- Dejar intacta la resolución posterior: `BankCategoryResolutionService` seguirá siendo el lugar donde se normaliza y se crean/matchean categorías de usuario.

Con `force-chained`, `feature-branch-chain` y presupuesto de 400 líneas, lo razonable es partirlo en **3 slices**:
1. **Slice 1** — plumbing de umbrales + extractor raw puro + tests unitarios de geometría real.
2. **Slice 2** — cableado en `IngBankPdfParser.ProcessBlocks(...)` + regresiones DAZN/parking/nómina/traspaso + fallback conservador.
3. **Slice 3** — delta OpenSpec + sustitución de tests ligados a `IngControlledTaxonomy` + limpieza final del helper obsoleto.

### Risks
- `IngColumnThresholds` hoy no delimita la zona derecha de importe/saldo; si se reutiliza sin ampliar el contrato, la descripción quedará contaminada.
- El parser necesita recordar umbrales por página o por cabecera; si se hace global sin cuidado, una variación de layout podría degradar páginas posteriores.
- El fallback conservador puede dejar más `null` cuando falte señal geométrica, pero eso sigue siendo MENOS dañino que persistir categorías/subcategorías inventadas.
- Si no se actualizan `pdf-category-extraction` e `ing-block-reconstruction`, quedará una contradicción formal entre código y specs archivadas.

### Ready for Proposal
Sí — la propuesta debe decir explícitamente que `IngControlledTaxonomy` deja de ser parte de la extracción ING, que la extracción pasa a preservar literales de columnas del PDF, y que cualquier resolución semántica queda diferida a `BankCategoryResolutionService`. También debe declarar desde el principio que **Chained PRs recommended: Yes** y que el riesgo de una sola PR bajo presupuesto de 400 líneas es alto.
