# Delta para ing-block-reconstruction

## MODIFIED Requirements

### Requirement: IBR-3 Extracción de categoría, subcategoría y descripción

| ID | Requisito | Escenarios |
|----|-----------|------------|
| IBR-3 | Tras aislar importe y saldo (R→L), `IngRawColumnExtractor` MUST extraer categoría, subcategoría y descripción del bloque clasificando `PositionedWord[]` por zonas X derivadas de `IngColumnThresholds`. La zona monetaria MUST quedar explícitamente excluida para no contaminar la descripción. No se instancia ni referencia `IngControlledTaxonomy`. Cuando la geometría no produce señal fiable, `Category = null`, `SubCategory = null`, descripción literal conservada; la transacción NO debe descartarse. | IBR-3a, IBR-3b, IBR-3c, IBR-3d, IBR-3e, IBR-3f |

(Previously: `IngControlledTaxonomy` consumía texto limpio L→R para categoría/subcategoría; no reconocido preservado como `RawOnly`.)

#### IBR-3a: DAZN — extracción por zonas X

- GIVEN bloque `15/01/2025 Compras Online DAZN  -12,99 1.234,56` con thresholds derivados de cabecera
- WHEN `IngRawColumnExtractor` clasifica `PositionedWord[]` por zona X
- THEN `Category = "Compras"`, `SubCategory = "Online"`, `Description = "DAZN"`

#### IBR-3b: Parking — literal multipalabra en subcategoría

- GIVEN bloque con palabras en zona categoría `Vehículo y transporte` y zona subcategoría `Parking y garaje`
- WHEN `IngRawColumnExtractor` procesa las zonas X
- THEN `Category = "Vehículo y transporte"`, `SubCategory = "Parking y garaje"`

#### IBR-3c: Nómina — sin subcategoría

- GIVEN bloque con zona categoría `Nominas` y zona subcategoría sin palabras
- WHEN `IngRawColumnExtractor` procesa el bloque
- THEN `Category = "Nominas"`, `SubCategory = null`

#### IBR-3d: Traspaso — categorías de varias palabras preservadas

- GIVEN bloque con zona categoría `Mis cuentas y depósitos` y zona subcategoría `Traspasos propios`
- WHEN `IngRawColumnExtractor` procesa las zonas X
- THEN `Category = "Mis cuentas y depósitos"`, `SubCategory = "Traspasos propios"`

#### IBR-3e: Fallback conservador — geometría insuficiente

- GIVEN bloque ING con importe/saldo aislables pero `IngColumnThresholds` sin umbrales fiables
- WHEN `IngRawColumnExtractor` no puede clasificar palabras en zonas de categoría/subcategoría
- THEN `Category = null`, `SubCategory = null`; descripción literal conservada; transacción NO descartada

#### IBR-3f: Descripción excluye tokens de categoría y zona monetaria

- GIVEN bloque completo con categoría, subcategoría, descripción e importe/saldo correctamente clasificados
- WHEN `IngRawColumnExtractor` completa la extracción
- THEN `Description` no contiene tokens de categoría, subcategoría, importe ni saldo

---

### Requirement: IBR-4 Fallback conservador por fila inválida

| ID | Requisito | Escenarios |
|----|-----------|------------|
| IBR-4 | Si el bloque no permite aislar importe y fecha de valor, el parser MUST retornar `null` para esa transacción. Geometría insuficiente en categoría/subcategoría NO activa `null` de transacción — solo aplica fallback de campos (`Category = null`, `SubCategory = null`). | IBR-4a, IBR-4b |

(Previously: IBR-4b describía "Taxonomía no reconocida → RawOnly, no null" usando `IngControlledTaxonomy`.)

#### IBR-4a: Bloque sin importe aislable → null

- GIVEN línea donde R→L no produce dos tokens numéricos distintos
- WHEN el extractor aplica fallback conservador
- THEN el parser retorna `null` para ese bloque (no se crea transacción)

#### IBR-4b: Geometría insuficiente en categoría → null de campos, no de transacción

- GIVEN bloque válido con importe/saldo aislables y geometría de categoría no fiable
- WHEN `IngRawColumnExtractor` no puede clasificar palabras en zonas categoría/subcategoría
- THEN transacción creada con `Category = null`, `SubCategory = null`; el parser NO retorna null
