# Especificación: ING Block Reconstruction

## Propósito

Ensambla bloques lógicos por transacción ING desde líneas físicas del PDF según el contrato
`F. VALOR | CATEGORÍA | SUBCATEGORÍA | DESCRIPCIÓN | COMENTARIO | IMPORTE | SALDO`,
con extracción numérica R→L y resolución de taxonomía L→R via `IngControlledTaxonomy`.

---

## Requisitos

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| IBR-1 | El sistema MUST ensamblar exactamente un bloque lógico por transacción. Una transacción SÓLO comienza cuando la primera columna contiene una fecha `dd/mm/yyyy`. El tratamiento de las líneas sin fecha depende del estado del bloque abierto: (a) **bloque incompleto** (sin par importe/saldo aislable en la línea de fecha): las líneas sin fecha MUST adjuntarse al bloque previo (backward); (b) **bloque completo** (ancla fuerte: fecha + par importe/saldo aislable en la misma línea): las líneas sin fecha MUST acumularse en un buffer ambiguo que se antepone al siguiente bloque o se reaneja al actual en EOF. | IBR-1a, IBR-1b, IBR-1c, IBR-1d, IBR-1e, IBR-1f |
| IBR-2 | El sistema MUST extraer importe y saldo mediante recorrido R→L sobre el bloque completo: el último token numérico es saldo, el penúltimo es importe. `COMENTARIO` MUST ser siempre `null`. | IBR-2a, IBR-2b |
| IBR-3 | Sobre el texto limpio de importe y saldo, `IngControlledTaxonomy` MUST consumir categoría y subcategoría L→R. El texto restante MUST ser la descripción final. Categoría no reconocida MUST preservarse como `RawOnly` sin descartar la transacción. | IBR-3a, IBR-3b, IBR-3c |
| IBR-4 | Si el bloque no permite aislar importe y fecha de valor, el parser MUST retornar `null` para esa transacción (fallback conservador). Taxonomía no reconocida NO activa fallback. | IBR-4a, IBR-4b |
| IBR-5 | `AdaptivePdfParser` MUST seleccionar `IngBankPdfParser` cuando la cabecera contiene `F. VALOR` y `CATEGORÍA`, independientemente del número de filas del documento. | IBR-5a, IBR-5b |

---

### IBR-1a: Fila de una sola línea

- GIVEN PDF con fila `15/01/2025 Compras Online DAZN  -12,99 1.234,56`
- WHEN `IngBankPdfParser` procesa el documento
- THEN el bloque tiene una sola línea; `Amount = -12.99`, `Balance = 1234.56`

### IBR-1b: Fila multilinea — continuación sin fecha

- GIVEN dos líneas físicas: primera con fecha `15/01/2025`; segunda sin fecha inicial
- WHEN `IngBankPdfParser` ensambla bloques
- THEN ambas líneas forman un único bloque lógico para una sola transacción

### IBR-1c: Filas adyacentes no se fusionan

- GIVEN línea 1 con fecha `15/01/2025` y línea 2 con fecha `16/01/2025`
- WHEN `IngBankPdfParser` ensambla bloques
- THEN cada línea produce un bloque independiente

### IBR-1d: Nómina — ancla fuerte en medio

- GIVEN bloque previo completo seguido de (1) línea sin fecha `NÓMINA EMPRESA S.L.`, (2) línea `15/01/2025 Nominas 2.500,00 3.200,00` con ancla fuerte, (3) línea sin fecha `ENERO 2025`
- WHEN `IngBlockAssembler` ensambla los bloques
- THEN la línea (1) se antepone al bloque de la línea (2) mediante buffer ambiguo
- AND la línea (3) se adjunta al mismo bloque en EOF
- AND el bloque previo NO contiene las líneas (1) ni (3)

### IBR-1e: Buffer ambiguo reasignado hacia delante

- GIVEN bloque A con ancla fuerte completa, seguido de línea `FRAGMENTO` sin fecha, seguido de bloque B con nueva ancla fuerte
- WHEN `IngBlockAssembler` ensambla los bloques
- THEN `FRAGMENTO` se antepone al bloque B
- AND el bloque A no contiene `FRAGMENTO`

### IBR-1f: Regresión repeated-page-header — backward preservado

- GIVEN bloque incompleto abierto (sin par importe/saldo aislable en la línea de fecha)
- WHEN llega una línea sin fecha (p. ej. continuación de página 2 tras eliminar cabecera repetida)
- THEN la línea sin fecha se adjunta al bloque previo (backward)
- AND no se crea ningún bloque nuevo erróneo

---

### IBR-2a: Extracción R→L — happy path

- GIVEN bloque con texto terminado en `… -12,99 1.234,56`
- WHEN el extractor recorre R→L
- THEN `Amount = -12.99`, `Balance = 1234.56`

### IBR-2b: COMENTARIO siempre null

- GIVEN cualquier fila ING válida
- WHEN `IngBankPdfParser` procesa la fila
- THEN `RawTransactionRow.Comment = null`

---

### IBR-3a: Categoría en taxonomía controlada

- GIVEN texto limpio `Compras Online DAZN`
- WHEN `IngControlledTaxonomy` consume L→R
- THEN `Category = "Compras"`, `SubCategory = "Online"`, `Description = "DAZN"`

### IBR-3b: Categoría fuera de taxonomía → RawOnly

- GIVEN texto limpio `Inversiones Fondos Pago especial`
- WHEN `IngControlledTaxonomy` no reconoce el primer token
- THEN `Category = "Inversiones Fondos"`, `source = RawOnly`, transacción creada

### IBR-3c: Descripción no contiene campos extraídos

- GIVEN bloque `15/01/2025 Compras Online DAZN  -12,99 1.234,56`
- WHEN parser completa extracción
- THEN `Description = "DAZN"` (sin importe, saldo, categoría ni subcategoría)

---

### IBR-4a: Bloque sin importe aislable → null

- GIVEN línea donde R→L no produce dos tokens numéricos distintos
- WHEN el extractor aplica fallback conservador
- THEN el parser retorna `null` para ese bloque (no se crea transacción)

### IBR-4b: Taxonomía no reconocida → RawOnly, no null

- GIVEN bloque válido con importe/saldo aislables y categoría desconocida
- WHEN `IngControlledTaxonomy` no encuentra match
- THEN transacción creada con `source = RawOnly`; el parser NO retorna null

---

### IBR-5a: Cabecera ING detectada

- GIVEN PDF cuya primera fila de cabecera contiene `F. VALOR` y `CATEGORÍA`
- WHEN `AdaptivePdfParser` analiza el documento
- THEN selecciona `IngBankPdfParser` sin importar el número de filas

### IBR-5b: Cabecera no-ING → no detectado

- GIVEN PDF sin tokens `F. VALOR` ni `CATEGORÍA` en cabecera
- WHEN `AdaptivePdfParser` analiza el documento
- THEN NO selecciona `IngBankPdfParser`
