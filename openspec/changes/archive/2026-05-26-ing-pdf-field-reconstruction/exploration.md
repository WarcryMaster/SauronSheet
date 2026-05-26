## Exploration: ing-pdf-field-reconstruction

### Current State
`AdaptivePdfParser` decide si un PDF es de ING llamando a `IngBankPdfParser.ParseAsync(...)` y comprobando si devuelve filas; por tanto, la detección actual depende de que el parser ya haya parseado al menos una transacción (`src/SauronSheet.Infrastructure/PDF/Parsers/AdaptivePdfParser.cs`).

`IngBankPdfParser` ya reconstruye líneas desde PdfPig, detecta la cabecera `F. VALOR` / `CATEGORÍA` y monta bloques con `rowBuffer` a partir de líneas que empiezan por fecha (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs`). El problema es que después mezcla tres responsabilidades en ramas distintas: ensamblado de bloque, extracción numérica e identificación de campos.

`ParseIngTransactionLine(...)` usa `ExtractTrailingNumbers(...)` sobre todo el texto restante y toma los dos últimos matches como importe/saldo. Eso funciona solo cuando el final de la fila ya está limpio; no modela el contrato real del export de enero 2025, donde importe y saldo deben sacarse de derecha a izquierda sobre el bloque lógico completo.

`ParseMultiLineTransaction(...)` sigue dos caminos frágiles: si la primera línea ya contiene dos números delega en `ParseIngTransactionLine(..., thresholds: null)` y concatena el resto a descripción; si no, asume que las últimas líneas numéricas son importe/saldo y que `textLines[0/1/2+]` equivalen a categoría/subcategoría/descripción. Ese contrato por posición de línea explica exactamente los fallos observados: filas adyacentes unidas, categoría/subcategoría metidas en descripción y nóminas sin taxonomía correcta.

El cambio previo `ing-single-line-category-extraction` mejoró el split geométrico por X para filas single-line (`IngColumnThresholds`), pero sigue siendo DEMASIADO ESTRECHO para este bug: la lógica real rota está antes, en el ensamblado de bloques y en la extracción de campos del bloque completo.

Además, el baseline OpenSpec actual de `pdf-category-extraction` prohíbe listas cerradas, mientras que este cambio necesita una taxonomía ING controlada para recuperar categoría/subcategoría de izquierda a derecha en casos como nómina. Eso exige una delta explícita de spec, no solo tocar código.

### Affected Areas
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs` — punto central del rediseño: ensamblado por bloques, continuación multi-línea, extracción numérica y fallback seguro.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngTransactionLineParser.cs` — hoy impone un contrato por índice de línea; deberá reducirse, sustituirse o quedar como helper secundario.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngColumnThresholds.cs` — el split por X debe pasar a ser apoyo/fallback, no la fuente principal de verdad para este export path.
- `src/SauronSheet.Infrastructure/PDF/Parsers/AdaptivePdfParser.cs` — la detección de ING no debería depender de que el parser devuelva filas parseadas si el nuevo fallback se vuelve más conservador.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserSingleLineTests.cs` — cubren solo un slice estrecho; ya no bastan para proteger el flujo real.
- `tests/SauronSheet.Infrastructure.Tests/PDF/IngTransactionLineParserTests.cs` — documentan el contrato viejo por orden de líneas y deben revisarse frente al nuevo contrato por bloque.
- `tests/SauronSheet.Application.Tests/Features/Transactions/Commands/ImportTransactionsFromPdfCommandTests.cs` — conviene mantener solo guards de integración del pipeline; la corrección funcional del parser debe moverse a tests de Infrastructure con ejemplos reales de enero 2025.
- `openspec/specs/pdf-category-extraction/spec.md` — requiere delta para acotar o sustituir la regla actual de “sin listas cerradas” en este camino ING.
- `src/SauronSheet.Infrastructure/PDF/Parsers/` (probable nuevo helper interno) — hace falta una fuente controlada y ordenada de taxonomía ING, por ejemplo un helper tipo `IngControlledTaxonomy.cs`, para extraer categoría/subcategoría sin heurísticas libres.

### Approaches
1. **Parchear las ramas actuales** — retocar `ParseIngTransactionLine`, `ParseMultiLineTransaction` y `ExtractTrailingNumbers` sin cambiar el modelo mental del parser.
   - Pros: diff corto; reutiliza bastante código existente.
   - Cons: mantiene la duplicidad single-line/multi-line, no corrige bien los bloques unidos y deja el bug estructural donde está.
   - Effort: Medio.

2. **Rediseño block-first dentro del parser actual** — conservar `ReconstructLinesFromWords` y el disparador por fecha, pero introducir un parseo unificado por bloque: ensamblado lógico, extracción numérica de derecha a izquierda, extracción de taxonomía de izquierda a derecha y fallback conservador.
   - Pros: ataca la causa real con el menor cambio arquitectónico; no exige rehacer PdfPig ni el pipeline Application.
   - Cons: necesita helpers internos nuevos, fixtures reales de enero 2025 y una delta de spec.
   - Effort: Medio/Alto.

3. **Parser geométrico completo por coordenadas X/Y** — llevar toda la lógica de ING a un modelo plenamente posicional, también para multiline y taxonomía.
   - Pros: máxima fidelidad al layout visual del PDF.
   - Cons: es más grande de lo necesario para este cambio, aumenta riesgo de review >400 líneas y sigue necesitando reglas de taxonomía/fallback.
   - Effort: Alto.

### Recommendation
Recomiendo **Approach 2**. La mínima corrección SEGURA es rediseñar `IngBankPdfParser` alrededor del bloque lógico real, no de la fila física ni del split single-line.

Implementación mínima sugerida:
- **Stage 1 — Block assembly**: seguir usando líneas que empiezan por fecha como delimitador, pero formalizar un bloque lógico único por transacción y unir continuaciones antes de extraer campos.
- **Stage 2 — Right-to-left numeric extraction**: sustituir `ExtractTrailingNumbers(...)` por un extractor de cola monetaria que recorra el bloque desde la derecha y aísle primero `saldo` y luego `importe`, sin contaminar descripción.
- **Stage 3 — Left-to-right taxonomy extraction**: sobre el texto ya limpio de números, consumir `categoría` y `subcategoría` desde la izquierda usando una taxonomía ING controlada y ordenada; el resto es `DESCRIPCIÓN`; `COMENTARIO` se fija a `null` para este path.
- **Stage 4 — Conservative fallback**: si el bloque no permite aislar con seguridad fecha + importe, devolver `null` para esa fila; si los importes son seguros pero la taxonomía no, persistir `Category/SubCategory = null` y descripción limpia, sin inventar nada.
- **Stage 5 — Detection hardening**: desacoplar `AdaptivePdfParser` de `rows.Count > 0` o, como mínimo, basar la detección ING en la cabecera reconocible y no en el éxito del parseo completo.

Para respetar el presupuesto de revisión de 400 líneas, lo veo como **3 PRs encadenadas**:
- **PR 1**: fixtures/text tests enero 2025 + helpers puros de ensamblado de bloque y extracción numérica.
- **PR 2**: cableado en `IngBankPdfParser` + fallback conservador + hardening de `AdaptivePdfParser`.
- **PR 3**: taxonomía controlada, regresiones de nómina/DAZN/parking y delta OpenSpec.

### Risks
- **Conflicto de especificación**: el cambio contradice parcialmente el baseline actual que prohíbe listas cerradas; si no se revoca o acota, quedará deuda documental.
- **Detección falsa de no-ING**: un parser más estricto puede devolver 0 filas y disparar el fallback genérico si `AdaptivePdfParser` no se endurece.
- **Taxonomía incompleta**: si la lista controlada no cubre todas las combinaciones reales del export ING, aparecerán falsos `RawOnly`.
- **Fallback demasiado agresivo**: saltarse filas ambiguas protege datos, pero puede bajar el número de filas importadas si no se testea con ejemplos reales.
- **Arrastre del cambio previo**: si se deja `IngColumnThresholds` como camino principal, se repetirá el error de arreglar solo el caso single-line.

### Ready for Proposal
Sí — la propuesta debe decir explícitamente que `ing-single-line-category-extraction` fue un arreglo parcial y que este nuevo cambio reemplaza el foco por un contrato block-first. También debe incluir una delta de spec para la taxonomía ING controlada y planificar implementación en chained PRs con ejemplos reales de enero 2025 (DAZN, parking, nómina y caso de filas adyacentes mal ensambladas).
