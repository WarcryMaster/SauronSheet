# Propuesta: Sustituir importación PDF por importación Excel

## Intención

El flujo de importación actual usa PDF y acumula naming específico en toda la arquitectura
(`ImportTransactionsFromPdfCommand`, `IPdfParser`, `pdf_imports`, métricas `app.pdf.import.*`).
Los archivos ING reales son Excel (.xls/.xlsx) con un contrato de columnas estable
(`F. VALOR | CATEGORÍA | SUBCATEGORÍA | DESCRIPCIÓN | COMENTARIO | IMPORTE | SALDO`).
Sustituir PDF por Excel elimina el parser geométrico de bloques, toda la deuda semántica,
y deja una arquitectura limpia sin vestigios PDF.

## Alcance

### En alcance
- Parser Excel (.xls + .xlsx); hoja `Movimientos`; cabecera estricta en fila 4; datos desde fila 5.
- Contrato neutro `IStatementParser` → reemplaza `IPdfParser`.
- Renombrado completo: comando, handler, repositorio, interfaz, tabla DB (`pdf_imports` → `import_batches`), métricas (`app.import.*`).
- `COMENTARIO` y `SALDO`: leídos, descartados (comportamiento de negocio actual preservado).
- `ImportedFrom`: sigue guardando el filename original del archivo subido.
- Eliminación completa de `Infrastructure/PDF/` y sus tests asociados.
- Upload UI: accept `.xls,.xlsx`; guía visible del formato esperado (7 columnas, hoja `Movimientos`, datos desde fila 5).
- Tests del parser sobre las muestras reales existentes.

### Fuera de alcance
- Persistencia de `COMENTARIO` ni `SALDO` (deferred).
- Matching tolerante de cabecera (typos, columnas reordenadas).
- Soporte de bancos con un contrato de columnas diferente.
- Optimización de rendimiento para archivos de gran volumen.
- Coexistencia temporal PDF + Excel.

## Capacidades

> Contrato para la fase sdd-spec. Investigar `openspec/specs/` antes de aplicar.

### Nuevas capacidades
- `excel-statement-parser`: Parseo de extractos Excel con contrato estricto de cabecera,
  detección de hoja `Movimientos`, mapeo a `RawTransactionRow` y reporte de errores por fila.
  Soporta .xls y .xlsx.

### Capacidades modificadas
- `pdf-category-extraction`: PCE-1 eliminado (comportamiento PDF-específico desaparece con el parser).
  PCE-2/3/4/5 conservados con naming neutro. Spec renombrada a `statement-category-extraction`.
- `ing-block-reconstruction`: Marcada como obsoleta. Capacidad eliminada junto con el parser PDF.

## Enfoque

3 slices encadenados, cada uno dentro del presupuesto de 400 líneas de revisión:

| Slice | Contenido | Estimación |
|-------|-----------|------------|
| 1 — Parser + tests | `IStatementParser`, `IngExcelStatementParser`, dependencia NuGet (NPOI / ExcelDataReader), tests sobre muestras reales | ~150 líneas |
| 2 — Neutralización Application/Infra | Renombrar comando/handler/repositorio/interfaz; migración DB; métricas; registro DI | ~180 líneas |
| 3 — UX + limpieza | Eliminar `Infrastructure/PDF/`; actualizar Upload/Index/Dashboard; guía de formato en Upload | ~130 líneas nuevas + eliminaciones |

## Áreas afectadas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `Domain/Services/IPdfParser.cs` | Removed | Reemplazado por `IStatementParser.cs` |
| `Application/Features/Transactions/Commands/ImportTransactionsFromPdf*` | Removed | Renombrado a `ImportTransactions*` |
| `Infrastructure/PDF/` | Removed | Directorio completo eliminado |
| `Infrastructure/Excel/` | New | Parser Excel implementado aquí |
| `Infrastructure/Persistence/IPdfImportRepository*` + `SupabasePdfImportRepository*` | Renamed | → `IImportBatchRepository*` + `SupabaseImportBatchRepository*` |
| `Infrastructure/Persistence/Migrations/` | New migration | Renombrar tabla `pdf_imports` → `import_batches` |
| `Frontend/Pages/Transactions/Upload.cshtml*` | Modified | Accept `.xls,.xlsx`; guía de formato explícita |
| `Frontend/Pages/Transactions/Index.cshtml` | Modified | Copy: "Import PDF" → "Import statement" |
| `Frontend/Pages/Dashboard.cshtml` | Modified | Copy en empty state |

## Riesgos

| Riesgo | Prob. | Mitigación |
|--------|-------|------------|
| Librería Excel no maneja code pages de .xls legacy (acentos) | Media | Validar con todas las muestras existentes al inicio de Slice 1; NPOI tiene mejor soporte legacy que ExcelDataReader |
| Variantes .xlsx no coinciden con estructura de muestras .xls | Baja | Confirmar con al menos un .xlsx real antes de cerrar Slice 1 |
| Datos en producción en tabla `pdf_imports` | Baja | Migración `RENAME TABLE`; filtros usan filename (`ImportedFrom`), no el nombre de tabla |

## Plan de rollback

- **Slice 1**: revertir archivos parser + entrada en `.csproj`. Sin impacto en producción.
- **Slice 2**: requiere migración inversa para la tabla (`import_batches` → `pdf_imports`). Bajo riesgo si se hace en feature branch antes de merge.
- **Slice 3**: cambios de UI revertibles sin impacto en datos ni esquema.

## Dependencias

- Seleccionar librería NuGet (NPOI vs ExcelDataReader) al inicio de Slice 1.
- Confirmar existencia de al menos un `.xlsx` real antes de cerrar Slice 1.

## Criterios de éxito

- [ ] Importar un archivo `.xls` de muestra real produce el mismo resultado que hoy (imported/skipped/errores).
- [ ] Importar un archivo `.xlsx` produce resultado equivalente.
- [ ] `Upload.cshtml` no acepta `.pdf` y muestra la guía explícita del formato esperado.
- [ ] Ningún identificador, tabla, métrica ni spec contiene "Pdf" / "pdf" en el contexto de importación.
- [ ] Tests del parser cubren: happy path, cabecera incorrecta, hoja ausente, fila malformada, duplicado.
- [ ] `dotnet test` verde; cobertura Domain ≥ 80%, Application ≥ 70%.
