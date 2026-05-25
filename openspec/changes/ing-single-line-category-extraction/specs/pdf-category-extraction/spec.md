# Delta for pdf-category-extraction

Change: `ing-single-line-category-extraction`

Revoca la limitación D5/W3 de `pdf-driven-category-import` que aceptaba `category=null`
en el path single-line del parser ING. A partir de este delta, una fila single-line con
categoría/subcategoría presente en el texto de la fila MUST extraerlas usando geometría X
de PdfPig. Se añade el requisito PCE-SL y se modifica PCE-1b para precisar su alcance.

---

## ADDED Requirements

### Requirement: PCE-SL — Extracción single-line con geometría X

`IngBankPdfParser` MUST usar coordenadas X de las palabras PdfPig para segmentar columnas
(Categoría / Subcategoría / Descripción) en filas ING de una sola línea física, cuando la
distribución de palabras produce señal X suficiente. Si la señal es insuficiente, el parser
MUST aplicar fallback conservador: preservar todo el texto en descripción sin inventar
límites de columna. Las filas multi-línea existentes MUST quedar sin ningún cambio.

#### Scenario: PCE-SLa — Fila enero 2025 extrae categoría y subcategoría

- GIVEN fila ING reconstruida como una sola línea física con texto de categoría,
  subcategoría y descripción distribuidos en posiciones X distintas (patrón real enero 2025)
- WHEN `IngBankPdfParser` procesa la fila con umbrales X calibrados
- THEN `RawTransactionRow.Category != null` y `RawTransactionRow.SubCategory != null`
- AND la fila no produce `CategorySource.RawOnly` tras la resolución

#### Scenario: PCE-SLb — Geometría X insuficiente → fallback conservador

- GIVEN fila single-line donde las palabras no producen separación X fiable
  (todas las palabras caen en el rango de columna descripción)
- WHEN el parser evalúa los umbrales X
- THEN el parser preserva todo el texto en descripción sin truncar
- AND `RawTransactionRow.Category = null` (pérdida mínima, nunca sobreinferida)

#### Scenario: PCE-SLc — Solo categoría detectable, sin subcategoría

- GIVEN fila single-line con señal X de categoría pero sin palabras en el rango de subcategoría
- WHEN el parser evalúa las posiciones X
- THEN `RawTransactionRow.Category != null` y `RawTransactionRow.SubCategory = null`

#### Scenario: PCE-SLd — Path multi-línea existente no afectado

- GIVEN fila ING reconstruida como múltiples líneas físicas (path normal existente)
- WHEN el parser procesa la fila
- THEN el comportamiento es idéntico al previo a este delta (sin regresión en multi-line)

---

## MODIFIED Requirements

### Requirement: PCE-1 — Extracción de literales sin listas cerradas

(Previously: PCE-1b aceptaba `category=null` en filas single-line como limitación válida;
ahora aplica únicamente a filas genuinamente sin texto de categoría en el layout del PDF)

`IngBankPdfParser` MUST extraer categoría y subcategoría como literales del PDF sin
comparar contra ninguna lista predefinida ni enum cerrado. Valores no reconocidos MUST
preservarse. Solo se aplica whitespace trim. Las filas single-line con categoría presente
en el texto de la fila se rigen por PCE-SL, no por PCE-1b.

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| PCE-1 | `IngBankPdfParser` MUST extraer categoría y subcategoría como literales del PDF sin listas cerradas. Filas single-line con señal X de categoría → PCE-SL. | PCE-1a, PCE-1b, PCE-1c |

#### PCE-1a: Valor fuera de lista anterior → preservado

- GIVEN PDF con categoría "Inversiones Fondos" (ausente en `KnownCategories` previas)
- WHEN `IngBankPdfParser` procesa la fila
- THEN `RawTransactionRow.Category = "Inversiones Fondos"` (no descartado ni nulificado)

#### PCE-1b: Categoría genuinamente ausente en el PDF

- GIVEN fila multi-línea del PDF sin texto de categoría en la columna del layout (ausencia real, no geométrica)
- WHEN `IngBankPdfParser` procesa la fila
- THEN `RawTransactionRow.Category = null`
- AND esta condición NOT aplica a filas single-line cuya categoría existe en el texto de la fila (ver PCE-SL)

#### PCE-1c: Subcategoría vacía en el PDF

- GIVEN PDF con categoría "Compras" y sin subcategoría detectable
- WHEN `IngBankPdfParser` procesa la fila
- THEN `RawTransactionRow.SubCategory = null`

---

## REMOVED Requirements

(Ninguno — D5/W3 era una decisión de diseño registrada en el archivo de change
`pdf-driven-category-import`, no un requisito numerado en este spec. Su revocación
queda documentada en la sección MODIFIED de PCE-1b.)
