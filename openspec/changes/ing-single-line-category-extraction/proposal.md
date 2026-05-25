# Proposal: ING Single-Line Category Extraction Fix

Corrige el bug real del PDF ING enero 2025: filas reconstruidas como línea única pierden
`bank_category`/`bank_subcategory` porque `ParseTextColumns` ignora coordenadas X y trata
todo el texto como descripción. La corrección revoca explícitamente la limitación aceptada
en el cambio anterior `pdf-driven-category-import`.

---

## Intent

`IngBankPdfParser` pierde categoría y subcategoría en el path single-line porque aplana
las palabras de PdfPig a `string` antes de intentar dividir columnas. La información
geométrica (coordenada X de cada palabra) ya existe en `page.GetWords()` pero se descarta.
Esta propuesta restablece el uso de esas posiciones para separar las columnas en filas
de una sola línea física.

**Asunción revocada** — la decisión D5/W3 de `pdf-driven-category-import` que consideraba
aceptable que el path single-line devuelva `category=null, subCategory=null` queda
**reemplazada** por este cambio. Los tests y artefactos que la codifican deben actualizarse.

---

## Scope

### In Scope
- Refactorizar `ParseTextColumns` para aceptar y usar `WordBoundingBox[]` en lugar de `string`.
- Calibrar umbrales X (basados en layout fijo ING) para asignar palabras a las columnas Categoría / Subcategoría / Descripción.
- Fallback explícito: si no hay señal X suficiente, preservar todo en descripción (pérdida mínima vs sobreinferir).
- Reemplazar `IngBankPdfParserSingleLineTests` para que validen extracción correcta en lugar del comportamiento nulo.
- Añadir escenario de regresión basado en el patrón real "Categoría + Subcategoría + Descripción" en una sola línea.
- Delta spec en `pdf-category-extraction` para cubrir el escenario single-line.
- Verificación integrada: test de command handler que confirma que una fila single-line deja de caer en `RawOnly`.

### Out of Scope
- Backfill histórico de transacciones ya importadas con `bank_category=null`.
- Cambios en `ICategoryResolutionService` o `IPdfCategoryResolverService`.
- Soporte para otros bancos/parsers.
- Exportar ni re-clasificar transacciones existentes.

---

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `pdf-category-extraction`: El escenario single-line (anteriormente catalogado como limitación aceptable)
  ahora MUST extraer `bank_category` y `bank_subcategory` usando posiciones X de las palabras PdfPig.
  Se añade PCE-SL (Single-Line) con los escenarios de extracción correcta y el fallback geométrico.

---

## Approach

Refactorizar el pipeline de reconstrucción de líneas para pasar `Word[]` (con `BoundingBox.Left`)
hasta `ParseTextColumns`. Definir tres buckets de columna (categoría / subcategoría / descripción)
usando umbrales X derivados del layout fijo ING observado en enero 2025. Si la distribución de
palabras no produce separación fiable, mantener el comportamiento actual como fallback conservador.

---

## Affected Areas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `src/…/PDF/Parsers/IngBankPdfParser.cs` | Modified | Pasar `Word[]` en lugar de `string`; calibrar umbrales X |
| `src/…/PDF/Parsers/IngTransactionLineParser.cs` | Modified (si aplica) | Ajuste de firma para aceptar posiciones |
| `tests/…/IngBankPdfParserSingleLineTests.cs` | Replaced | Validar extracción correcta; revocar tests de null-category |
| `tests/…/ImportTransactionsFromPdfCommandTests.cs` | Modified | Añadir cobertura single-line → no RawOnly |
| `openspec/specs/pdf-category-extraction/spec.md` | Delta | Añadir PCE-SL (single-line scenarios) |
| `openspec/changes/archive/…/pdf-driven-category-import` | Reference | D5/W3 marcada como supersedida; no se modifica el archivo |

---

## Risks

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Falsos positivos X (descripción truncada en subcategoría) | Media | Umbrales conservadores + fallback + regresión con fixture real |
| Variantes de layout ING distintas a enero 2025 | Media | Parametrizar umbrales; tests con fixture de página alternativa |
| Regresión en el path multi-line ya funcional | Baja | Los tests multi-line existentes deben seguir pasando en green |
| Cobertura insuficiente sin datos reales | Alta | Fixture incrustado con el patrón observado es obligatorio |

---

## Rollback Plan

Revertir el commit que cambia `ParseTextColumns`; los tests actuales (pre-cambio) volverán a
pasar sin modificación adicional porque la firma de entrada al parser no cambia para el path
multi-line. No hay migración de datos involucrada.

---

## Dependencies

- PdfPig ya expone `BoundingBox` por palabra — no hay dependencia externa nueva.
- El layout del PDF ING debe estar documentado (offset X de cada columna) en un comentario o constante del parser.

---

## Success Criteria

- [ ] Una fila ING enero 2025 reconstruida como single-line produce `bank_category != null` y `bank_subcategory != null` tras el parse.
- [ ] El command handler de import no devuelve `CategorySource.RawOnly` para esa fila cuando el parser la extrae correctamente.
- [ ] `IngBankPdfParserSingleLineTests` pasan en green con los nuevos escenarios (sin ningún test que afirme `category=null`).
- [ ] Los tests multi-line existentes continúan en green sin cambios.
- [ ] La especificación `pdf-category-extraction` incluye el escenario PCE-SL que describe el comportamiento single-line correcto.
