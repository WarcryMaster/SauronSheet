# Delta para: PDF Category Extraction

---

## MODIFIED Requirements

### PCE-1: Extracción de literales sin listas cerradas

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| PCE-1 | Para el path ING, `IngBankPdfParser` MUST usar `IngControlledTaxonomy` como fuente primaria de categoría/subcategoría (extracción L→R sobre texto limpio). Valores no reconocidos MUST preservarse como `RawOnly`. Para el path no-ING, el parser MUST continuar extrayendo literales del PDF sin comparar contra ninguna lista cerrada. Solo se aplica whitespace trim en ambos paths. | PCE-1a (valor fuera de taxonomía → RawOnly), PCE-1b (categoría null), PCE-1c (subcategoría null), PCE-1d (path no-ING sin lista cerrada) |

(Previously: el requisito no distinguía entre path ING y no-ING, ni referenciaba `IngControlledTaxonomy`.)

#### PCE-1a: Valor no contemplado en taxonomía ING → preservado como RawOnly

- GIVEN PDF ING con categoría `"Inversiones Fondos"` (ausente en `IngControlledTaxonomy`)
- WHEN `IngBankPdfParser` procesa la fila
- THEN `RawTransactionRow.Category = "Inversiones Fondos"`, `source = RawOnly` (no descartado ni nulificado)

#### PCE-1b: Categoría vacía en el PDF

- GIVEN fila del PDF sin campo de categoría detectable
- WHEN `IngBankPdfParser` procesa la fila
- THEN `RawTransactionRow.Category = null`

#### PCE-1c: Subcategoría vacía en el PDF

- GIVEN PDF con categoría `"Compras"` y sin subcategoría
- WHEN `IngBankPdfParser` procesa la fila
- THEN `RawTransactionRow.SubCategory = null`

#### PCE-1d: Path no-ING no usa lista cerrada

- GIVEN PDF de otro banco procesado por parser genérico
- WHEN el parser procesa la fila
- THEN categoría y subcategoría se extraen como literales del PDF sin comparar contra ninguna lista
