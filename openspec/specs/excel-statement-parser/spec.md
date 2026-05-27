# Especificación: Excel Statement Parser

## Propósito

Parseo de extractos bancarios en formato Excel (.xls / .xlsx) con contrato estricto de cabecera.
Reemplaza el parser PDF como único mecanismo de importación de movimientos.

---

## Requisitos

### ESP-1: Contrato de cabecera y detección de hoja

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| ESP-1 | El parser MUST detectar la hoja `Movimientos`. MUST validar que la fila 4 contenga exactamente las columnas `F. VALOR \| CATEGORÍA \| SUBCATEGORÍA \| DESCRIPCIÓN \| COMENTARIO \| IMPORTE \| SALDO` por posición. Si la hoja falta o la cabecera no coincide, MUST retornar `ParseError` antes de procesar ninguna fila de datos. | ESP-1a, ESP-1b, ESP-1c |

#### ESP-1a: Hoja y cabecera válidas — parseo iniciado

- GIVEN archivo Excel con hoja `Movimientos` y cabecera exacta en fila 4
- WHEN el parser procesa el archivo
- THEN la extracción comienza en fila 5 sin errores

#### ESP-1b: Hoja ausente — error inmediato

- GIVEN archivo Excel sin hoja llamada `Movimientos`
- WHEN el parser intenta localizar la hoja
- THEN retorna `ParseError` indicando hoja faltante; ninguna fila procesada

#### ESP-1c: Cabecera incorrecta — error inmediato

- GIVEN archivo Excel con hoja `Movimientos` y columnas en orden diferente al oficial
- WHEN el parser valida la fila 4
- THEN retorna `ParseError`; ninguna fila de datos procesada

---

### ESP-2: Mapeo de fila a RawTransactionRow

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| ESP-2 | MUST mapear cada fila de datos (desde fila 5) a `RawTransactionRow`: `F. VALOR` → `ValueDate`, `DESCRIPCIÓN` → `Description`, `IMPORTE` → `Amount`, `CATEGORÍA` → `BankCategory`, `SUBCATEGORÍA` → `BankSubCategory`. `COMENTARIO` y `SALDO` MUST leerse y descartarse. `ImportedFrom` MUST contener el filename original del archivo subido. | ESP-2a, ESP-2b |

#### ESP-2a: Fila completa — mapeo correcto

- GIVEN fila con `F. VALOR = 15/01/2025`, `DESCRIPCIÓN = "DAZN"`, `IMPORTE = -12,99`, `CATEGORÍA = "Compras"`, `SUBCATEGORÍA = "Online"`
- WHEN el parser procesa la fila
- THEN `ValueDate = 2025-01-15`, `Amount = -12.99`, `Description = "DAZN"`, `BankCategory = "Compras"`, `BankSubCategory = "Online"`

#### ESP-2b: COMENTARIO y SALDO descartados

- GIVEN fila con `COMENTARIO = "Recibo luz"` y `SALDO = 1234,56`
- WHEN el parser procesa la fila
- THEN `RawTransactionRow.Comment = null`; `Balance` no se persiste

---

### ESP-3: Manejo de errores por fila

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| ESP-3 | El lote MUST procesarse completo; una fila inválida MUST descartarse sin detener el resto. Una fila es inválida si `F. VALOR` no es fecha parseable o `IMPORTE` no es número parseable. El resultado MUST incluir conteo `imported`, `skipped` (duplicados) y `errors`. | ESP-3a, ESP-3b |

#### ESP-3a: Fila con importe no parseable — descartada

- GIVEN fila con `IMPORTE = "N/A"` y `F. VALOR` válida
- WHEN el parser procesa el lote
- THEN esa fila omitida; resto del lote continúa; conteo `errors` incrementado

#### ESP-3b: Fila duplicada — omitida

- GIVEN fila cuyo hash (userId, ValueDate, Amount, Description) ya existe en `import_batches`
- WHEN el handler persiste el lote
- THEN fila omitida; conteo `skipped` incrementado; no se lanza error

---

### ESP-4: Guía de formato en Upload UI

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| ESP-4 | La página Upload MUST mostrar instrucciones visibles del formato requerido antes de que el usuario seleccione el archivo. El campo de subida MUST aceptar únicamente `.xls` y `.xlsx` (`accept=".xls,.xlsx"`). MUST NOT aceptar `.pdf`. Las instrucciones MUST indicar: nombre de hoja (`Movimientos`), cabecera exacta de 7 columnas, y que los datos comienzan en la fila 5. | ESP-4a, ESP-4b |

#### ESP-4a: PDF rechazado en cliente

- GIVEN campo de subida con `accept=".xls,.xlsx"`
- WHEN el usuario intenta seleccionar un archivo `.pdf`
- THEN el selector de archivos nativo no permite la selección

#### ESP-4b: Guía visible antes de seleccionar archivo

- GIVEN página Upload cargada sin archivo seleccionado
- WHEN el usuario visualiza la página
- THEN las instrucciones de formato (hoja, columnas, fila de inicio) son visibles sin scroll adicional
