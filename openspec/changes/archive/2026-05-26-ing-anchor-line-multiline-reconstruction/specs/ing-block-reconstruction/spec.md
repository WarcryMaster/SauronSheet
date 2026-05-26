# Delta para ing-block-reconstruction

## Requisitos MODIFICADOS

### Requisito: IBR-1 — Ensamblado de bloques lógicos

El sistema MUST ensamblar exactamente un bloque lógico por transacción. Una transacción SÓLO
comienza cuando la primera columna contiene una fecha `dd/mm/yyyy`. El tratamiento de las líneas
sin fecha depende del estado del bloque abierto en curso:

- **Bloque incompleto** (sin par importe/saldo aislable en la línea de fecha): las líneas sin
  fecha MUST adjuntarse al bloque previo (backward). Preserva el comportamiento de cabeceras de
  página repetidas.
- **Bloque completo** (ancla fuerte detectada — fecha + par importe/saldo aislable en la misma
  línea): las líneas sin fecha MUST acumularse en un buffer ambiguo.
  - Si llega una nueva ancla fuerte: el buffer MUST anteponerse al nuevo bloque.
  - Si llega EOF sin nueva ancla: el buffer MUST reanearse al bloque actual.

**Ancla fuerte**: línea que supera `TryGetBlockStartDate(...)` Y permite aislar par importe/saldo
mediante `IngMonetaryExtractor.ExtractRightToLeft(...)` en esa misma línea.

(Previously: toda línea sin fecha se adjuntaba INCONDICIONALMENTE al bloque previo, sin distinguir bloque completo de incompleto.)

#### Escenario IBR-1a: Fila de una sola línea

- GIVEN PDF con fila `15/01/2025 Compras Online DAZN  -12,99 1.234,56`
- WHEN `IngBankPdfParser` procesa el documento
- THEN el bloque tiene una sola línea; `Amount = -12.99`, `Balance = 1234.56`

#### Escenario IBR-1b: Bloque incompleto — continuación sin fecha hacia atrás

- GIVEN bloque abierto con fecha `15/01/2025` sin par importe/saldo aislable en esa misma línea
- WHEN llega una línea sin fecha inicial
- THEN la línea sin fecha se adjunta al bloque previo (backward)

#### Escenario IBR-1c: Filas adyacentes con fecha no se fusionan

- GIVEN línea 1 con fecha `15/01/2025` y línea 2 con fecha `16/01/2025`
- WHEN `IngBankPdfParser` ensambla bloques
- THEN cada línea produce un bloque independiente

#### Escenario IBR-1d: Nómina — ancla fuerte en medio (happy path)

- GIVEN tres líneas físicas consecutivas:
  (1) `NÓMINA EMPRESA S.L.` sin fecha,
  (2) `15/01/2025 Nominas  2.500,00 3.200,00` con fecha + par importe/saldo (ancla fuerte),
  (3) `ENERO 2025` sin fecha
- WHEN `IngBlockAssembler` ensambla los bloques
- THEN la línea (1) se antepone al bloque de la línea (2) mediante buffer ambiguo
- AND la línea (3) se adjunta al mismo bloque
- AND la transacción anterior NO contiene las líneas (1) ni (3)

#### Escenario IBR-1e: Buffer ambiguo reasignado hacia delante

- GIVEN bloque A con ancla fuerte completa, seguido de línea `FRAGMENTO` sin fecha,
  seguido de bloque B con nueva ancla fuerte
- WHEN `IngBlockAssembler` ensambla los bloques
- THEN `FRAGMENTO` se antepone al bloque B
- AND el bloque A no contiene `FRAGMENTO`

#### Escenario IBR-1f: Regresión repeated-page-header — backward preservado

- GIVEN bloque incompleto abierto (sin par importe/saldo aislable) y primera línea sin fecha de
  página 2 cuya cabecera de control fue eliminada por `StripLeadingRepeatedPageHeaderSection`
- WHEN `IngBlockAssembler` ensambla los bloques
- THEN la línea sin fecha se adjunta al bloque previo (backward)
- AND no se crea ningún bloque nuevo erróneo
