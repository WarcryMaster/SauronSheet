# Delta para ing-block-reconstruction

> **Capacidad eliminada íntegramente** junto con el parser PDF.
> No existe spec reemplazante para esta capacidad — desaparece del sistema.
> El agente `sdd-archive` MUST eliminar `openspec/specs/ing-block-reconstruction/`
> como parte del cierre de este cambio.

---

## REMOVED Requirements

### IBR-1: Ensamblaje de bloques lógicos por transacción

(Razón: Depende de `IngBankPdfParser` y `IngBlockAssembler` — infraestructura PDF eliminada en su totalidad. El Excel ya entrega una fila por transacción; no hay ensamblaje geométrico.)

### IBR-2: Extracción R→L de importe y saldo

(Razón: Depende de `IngBankPdfParser` — eliminado junto con el parser PDF. En Excel, importe y saldo se leen de columnas fijas.)

### IBR-3: Extracción de categoría/subcategoría por zonas X

(Razón: Depende de `IngRawColumnExtractor` e `IngColumnThresholds` — eliminados con el parser PDF. En Excel, categoría y subcategoría provienen de columnas nombradas directamente.)

### IBR-4: Transacciones nulas por bloque no parseable

(Razón: Lógica de fallback específica del parser geométrico PDF — no tiene equivalente en parseo de celdas Excel.)

### IBR-5: Selección de parser ING por cabecera PDF

(Razón: `AdaptivePdfParser` y su lógica de selección por tokens de cabecera PDF son eliminados en su totalidad. El contrato Excel usa `IStatementParser` neutro sin dispatch por banco.)
