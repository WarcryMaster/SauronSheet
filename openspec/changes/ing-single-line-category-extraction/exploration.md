## Exploration: ing-single-line-category-extraction

### Current State
`IngBankPdfParser` reconstruye cada página con `page.GetWords()`, pero `ReconstructLinesFromWords` solo agrupa por eje Y y aplana cada fila a `string`, descartando las coordenadas X (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs:54-61,216-257`).

Cuando una transacción queda en una sola línea física, `FlushRowBuffer` entra en `ParseIngTransactionLine`, extrae fecha e importes, y delega el resto a `ParseTextColumns` (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs:96-115,381-429`). Ese método hoy devuelve siempre `(null, null, fullText, null)` porque asume que ya no existen marcadores de columna (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs:479-501`).

El resultado encaja exactamente con la incidencia real: si enero 2025 entra por el path single-line, el prefijo de categoría/subcategoría queda pegado al `description`, `bank_category`/`bank_subcategory` quedan a null, y el flujo posterior cae en `RawOnly` al no tener rawCategory (`openspec/specs/pdf-category-extraction/spec.md:55-85`; evidencia DB aportada por el usuario).

Además, el comportamiento actual NO es accidental: quedó fijado por `IngBankPdfParserSingleLineTests` y por los artefactos archivados del cambio `pdf-driven-category-import`, que documentan el “single-line guard” como limitación aceptada (`tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserSingleLineTests.cs:7-115`; `openspec/changes/archive/2026-05-25-pdf-driven-category-import/tasks.md:90-95`; `.../design.md:17,76`).

### Affected Areas
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs` — origen del fallo: aplana palabras y luego el path single-line pierde la frontera entre columnas.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngTransactionLineParser.cs` — referencia útil del contrato position-first actual, pero solo cubre multi-line.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserSingleLineTests.cs` — hoy protege exactamente el comportamiento que hay que cambiar.
- `tests/SauronSheet.Application.Tests/Features/Transactions/Commands/ImportTransactionsFromPdfCommandTests.cs` — debería ampliarse para cubrir que una fila single-line con categoría/subcategoría recuperadas ya no termina en `RawOnly`.
- `openspec/specs/pdf-category-extraction/spec.md` — necesita una delta explícita para el caso single-line; el baseline habla de preservar literales, pero no especifica cómo resolver una fila físicamente plana.
- `openspec/changes/archive/2026-05-25-pdf-driven-category-import/*` — contiene decisión previa (D5/W3) que debe ser sustituida o acotada para no dejar un artefacto contradictorio.

### Approaches
1. **Heurística sobre el string ya reconstruido** — intentar partir `textPart` en categoría/subcategoría/descripción usando tokens, mayúsculas, diccionarios o prefijos frecuentes.
   - Pros: cambio localizado en `ParseTextColumns`; bajo impacto estructural.
   - Cons: muy frágil. Categoría, subcategoría y descripción son texto libre; sin X positions no hay separador real. Reintroduce listas cerradas o reglas ad hoc y puede romper descripciones legítimas como "Nómina enero 2025" o "Transferencia recibida...".
   - Effort: Medio.

2. **Preservar y usar posiciones de palabra/columna antes de aplanar** — refactorizar el pipeline para que la fila single-line no trabaje con un `string` plano, sino con palabras + `BoundingBox.Left`, permitiendo repartir cada palabra en buckets de categoría/subcategoría/descripción.
   - Pros: usa la evidencia que PdfPig YA entrega; mantiene el enfoque position-first; evita volver a listas cerradas; es la opción más alineada con el layout fijo del PDF ING.
   - Cons: exige tocar la reconstrucción de líneas o introducir una estructura intermedia; hay que calibrar tolerancias X y cubrir variaciones de maquetación.
   - Effort: Medio/Alto.

3. **Heurística híbrida con fallback** — primero intentar split por posiciones X; si no hay señal suficiente, conservar todo en descripción.
   - Pros: reduce falsos positivos; permite mejorar enero 2025 sin degradar filas ambiguas.
   - Cons: más ramas de comportamiento; requiere tests claros para decidir cuándo se extrae y cuándo se preserva todo.
   - Effort: Alto.

### Recommendation
Recomiendo **Approach 2 con fallback conservador del Approach 3**. El parser NO debería intentar adivinar límites desde el `string` fusionado; eso es construir sobre arena. La información útil existía antes: `page.GetWords()` ya expone posiciones X/Y. La corrección sólida es conservar esa geometría hasta el momento de dividir columnas, y solo si una fila concreta no ofrece separación suficiente, mantener el comportamiento actual como fallback explícito.

En la práctica, este hotfix debería limitarse al parser ING y a sus pruebas/artefactos SDD: no hace falta tocar la lógica de resolución de categorías salvo para añadir cobertura integrada que demuestre que, cuando el parser recupera `rawCategory` y `rawSubcategory`, el import deja de caer en `RawOnly`.

### Risks
- **Falsos positivos de split**: si los umbrales X son demasiado agresivos, parte de la descripción puede acabar en subcategoría.
- **Variantes de layout**: otros PDFs ING podrían no compartir exactamente las mismas anchuras/offsets de enero 2025.
- **Contradicción con tests actuales**: `IngBankPdfParserSingleLineTests` y artefactos archivados hoy validan el comportamiento defectuoso; si no se actualizan, el cambio quedará semánticamente inconsistente.
- **Fallback ambiguo**: habrá filas donde ni siquiera con posiciones X se pueda distinguir con seguridad; hay que preferir pérdida mínima frente a sobreinferir.
- **Cobertura insuficiente con datos reales**: si no se añade un fixture/regresión representativo del patrón `Categoría + Subcategoría + Descripción` en una sola línea, el bug puede reaparecer.

### Ready for Proposal
Sí — con esta instrucción para la propuesta: el cambio debe redefinir el contrato single-line del parser ING. No es solo un ajuste de implementación; también requiere reemplazar el test/artefacto que hoy legitima `category=null, subCategory=null` en ese path y añadir un escenario de regresión basado en el patrón real de enero 2025.
