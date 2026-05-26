# Delta para pdf-category-extraction

## MODIFIED Requirements

### Requirement: PCE-1 Extracción de literales sin listas cerradas

| ID | Requisito | Escenarios |
|----|-----------|------------|
| PCE-1 | Para el path ING, `IngBankPdfParser` MUST extraer categoría y subcategoría directamente desde las zonas X del bloque (`IngRawColumnExtractor` + `IngColumnThresholds`) sin instanciar `IngControlledTaxonomy` ni usar ninguna lista cerrada. Cuando la geometría no produce señal fiable, MUST aplicar fallback conservador: `Category = null`, `SubCategory = null`, descripción literal conservada; la transacción NO debe descartarse. Para el path no-ING el comportamiento es inalterado. Solo whitespace trim en ambos paths. | PCE-1a, PCE-1b, PCE-1c, PCE-1d, PCE-1e, PCE-1f |

(Previously: path ING usaba `IngControlledTaxonomy` L→R como fuente primaria; valores no reconocidos preservados como `RawOnly`.)

#### PCE-1a: DAZN — categoría + subcategoría + descripción desde columnas

- GIVEN fila ING con palabras en zona categoría `Compras`, zona subcategoría `Online`, zona descripción `DAZN`
- WHEN `IngRawColumnExtractor` clasifica por zona X usando `IngColumnThresholds`
- THEN `Category = "Compras"`, `SubCategory = "Online"`, `Description = "DAZN"`

#### PCE-1b: Parking — literal multipalabra preservado en subcategoría

- GIVEN fila ING con zona categoría `Vehículo y transporte` y zona subcategoría `Parking y garaje`
- WHEN `IngRawColumnExtractor` clasifica palabras por zona X
- THEN `Category = "Vehículo y transporte"`, `SubCategory = "Parking y garaje"`

#### PCE-1c: Nómina — categoría sin subcategoría

- GIVEN fila ING con zona categoría `Nominas` y zona subcategoría sin palabras
- WHEN `IngRawColumnExtractor` procesa el bloque
- THEN `Category = "Nominas"`, `SubCategory = null`

#### PCE-1d: Traspaso entre cuentas propias

- GIVEN fila ING con zona categoría `Mis cuentas y depósitos` y zona subcategoría `Traspasos propios`
- WHEN `IngRawColumnExtractor` clasifica por zona X
- THEN `Category = "Mis cuentas y depósitos"`, `SubCategory = "Traspasos propios"`

#### PCE-1e: Fallback conservador — geometría insuficiente

- GIVEN bloque ING donde `IngColumnThresholds` no derivan umbrales fiables para ese bloque
- WHEN `IngRawColumnExtractor` no produce señal de columna
- THEN `Category = null`, `SubCategory = null`; descripción literal conservada; transacción no descartada

#### PCE-1f: Path no-ING sin lista cerrada

- GIVEN PDF de otro banco procesado por parser genérico
- WHEN el parser procesa la fila
- THEN categoría y subcategoría se extraen como literales del PDF sin comparar contra ninguna lista
