# Tasks: Sustituir importación PDF por Excel

## Review Workload Forecast

| Campo | Valor |
|-------|-------|
| Líneas estimadas cambiadas | 700–900 (adiciones + eliminaciones, ~30 archivos) |
| Riesgo presupuesto 400 líneas | High |
| PRs encadenados recomendados | Yes |
| Split sugerido | PR 1 → PR 2 → PR 3 |
| Estrategia de entrega | auto-chain |
| Estrategia de cadena | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Unidades de trabajo sugeridas

| Unidad | Meta autónoma | PR previsto | Base de rama |
|--------|---------------|-------------|--------------|
| 1 | Contrato de dominio + parser Excel + tests de parser | PR 1 | `feature/replace-pdf-import-with-excel` |
| 2 | Neutralización Application/Handler + persistence + migración DB + DI | PR 2 | rama de PR 1 |
| 3 | Frontend Excel-only + eliminación completa de código PDF | PR 3 | rama de PR 2 |

---

## Fase 1 — Contrato de dominio + Parser Excel [PR 1]

- [x] 1.1 [RED] Crear `tests/SauronSheet.Infrastructure.Tests/Excel/IngExcelStatementParserTests.cs` — ESP-1a: hoja `Movimientos` + cabecera válida → filas parseadas desde row 5
- [x] 1.2 [RED] Test ESP-1b: hoja ausente → `ParseResult.Error`; ESP-1c: cabecera fila 4 incorrecta → `ParseResult.Error`
- [x] 1.3 [RED] Test ESP-2a: fila completa → `ValueDate`, `Amount`, `Description`, `BankCategory`, `BankSubCategory` mapeados correctamente
- [x] 1.4 [RED] Test ESP-3a: `IMPORTE="N/A"` → fila descartada, `errors+1`; ESP-3b: hash duplicado → `skipped+1`
- [x] 1.5 [GREEN] Crear `Domain/Services/IStatementParser.cs` — `ParseAsync(Stream stream, string filename)`
- [x] 1.6 [GREEN] Crear `Domain/Repositories/IImportBatchRepository.cs` — `AddAsync` + `GetByUserIdAsync`
- [x] 1.7 [GREEN] Añadir `ExcelDataReader` (MIT) a `SauronSheet.Infrastructure.csproj`; no eliminar PdfPig en este PR
- [x] 1.8 [GREEN] Crear `Infrastructure/Excel/IngExcelStatementParser.cs` — detección hoja, validación cabecera fila 4, mapeo fila→`RawTransactionRow`, manejo de error por fila sin detener el lote
- [x] 1.9 [REFACTOR] Actualizar XML-doc `Domain/ValueObjects/RawTransactionRow.cs` — quitar toda referencia a PDF

---

## Fase 2 — Application + Persistence + DI + Migración [PR 2]

- [x] 2.1 [RED] Crear `tests/SauronSheet.Application.Tests/Commands/ImportTransactionsCommandHandlerTests.cs` — happy path con `IStatementParser` + `IImportBatchRepository` mockeados → `ImportResultDto` correcto
- [x] 2.2 [RED] Test handler: `IStatementParser` falla → `DomainException` con mensaje genérico; Sentry captura la excepción de infra
- [x] 2.3 [GREEN] Crear `Application/Commands/ImportTransactionsCommand.cs` + `ImportTransactionsCommandHandler.cs` (deduplicación, categorías, métricas `app.import.*`)
- [x] 2.4 [GREEN] Crear `Infrastructure/Persistence/SupabaseImportBatchRepository.cs` — DTO mapeado a tabla `import_batches`
- [x] 2.5 [GREEN] Crear migración `Infrastructure/Persistence/Migrations/{ts}_rename_pdf_imports.sql` — `ALTER TABLE pdf_imports RENAME TO import_batches`
- [x] 2.6 [GREEN] Actualizar `Infrastructure/DependencyInjection.cs` — registrar `IStatementParser → IngExcelStatementParser` y `IImportBatchRepository → SupabaseImportBatchRepository`; quitar PdfPig de `SauronSheet.Infrastructure.csproj`
- [x] 2.7 [REFACTOR] Test integración DI en `SauronSheet.Integration.Tests` — `IStatementParser` e `IImportBatchRepository` resuelven sin excepción

---

## Fase 3 — Frontend Excel-only + Eliminación PDF [PR 3]

- [x] 3.1 [RED] Test Playwright `e2e/` — Upload page: input no acepta `.pdf`; bloque de guía visible sin scroll (hoja `Movimientos`, 7 columnas, datos desde fila 5)
- [x] 3.2 [GREEN] Actualizar `Frontend/Pages/Transactions/Upload.cshtml` — `accept=".xls,.xlsx"` + bloque de instrucciones de formato requerido
- [x] 3.3 [GREEN] Actualizar `Frontend/Pages/Transactions/Upload.cshtml.cs` — despachar `ImportTransactionsCommand` vía MediatR
- [x] 3.4 [GREEN] Actualizar `Frontend/Pages/Transactions/Index.cshtml` y `Dashboard.cshtml` — copy neutral, eliminar toda mención PDF
- [x] 3.5 [CLEANUP] Eliminar `Infrastructure/PDF/` completo (8 archivos) + `Infrastructure/Persistence/SupabasePdfImportRepository.cs`
- [x] 3.6 [CLEANUP] Eliminar `Application/Commands/ImportTransactionsFromPdf*.cs` + `Domain/Services/IPdfParser.cs` + `Domain/Repositories/IPdfImportRepository.cs`
- [x] 3.7 [CLEANUP] Eliminar tests PDF obsoletos (3 archivos en `tests/`)
- [x] 3.8 [VERIFY] `dotnet test` verde; cobertura Domain ≥ 80%, Application ≥ 70%; `dotnet build` limpio sin warnings

---

## Fase 4 — Remediación post-verify [Batch de remediación]

- [x] R-W1a [RED→GREEN] Eliminar patrón `return;` (fake-pass): reemplazar `ParseAsync_RealIngSample_ReturnsNonEmptyResultWithoutCrashing` por guard `Assert.True(File.Exists(...), msg)` que falla ruidosamente si el fixture está ausente
- [x] R-W1b [RED→GREEN] Añadir test fact `ParseAsync_RealXlsGolden_ParsesExactly21ValidRowsWithNoErrorsOrSkips` — verifica exactamente 21 filas, 0 errores, 0 skips con el fichero .xls real
- [x] R-W1c [RED→GREEN] Añadir test theory `ParseAsync_RealXlsGolden_EachRow_MapsAllFieldsExhaustively` — 21 casos data-driven con todos los campos mapeados (Date, Category, SubCategory, Description, Amount) + confirmación ESP-2b (Comment=null, Balance=null, Currency=EUR) para cada fila
- [x] R-W3a [GREEN] Añadir `app.import.started` counter + breadcrumb en `ImportTransactionsCommandHandler.Handle` tras la validación de extensión
- [x] R-W3b [GREEN] Añadir `app.import.failed` counter con tag `reason=parse_error` en el bloque catch de excepciones no-domain del parser
- [x] R-W3c [GREEN] Añadir `app.import.completed` counter (con tag `result=success|partial_success`) + breadcrumb de completion + `app.import.rows_imported` counter de throughput antes del return
- [x] R-W3d [VERIFY] `dotnet test` 469/469 verde tras remediación; `dotnet build` 0 warnings 0 errors
- [ ] R-W2  [BLOCKED] Tests Playwright upload requieren auth Supabase viva + credenciales
  - [x] R-W2a Actualizar `02-upload-excel.spec.ts` — beforeEach intenta self-registration vía `/auth/register` antes de requerir env vars; skip message ahora es diagnóstico con causa raíz
  - [x] R-W2b Actualizar `e2e/playwright.config.ts` — `channel: 'msedge'` local + `video: 'off'` local para evitar dependencias de ffmpeg en entorno sin `playwright install`
  - [x] R-W2c Runtime evidence capturado: Playwright lanzó Edge, navegó a `/auth/register`, registro con `e2e.sauron.*@gmail.com` → app devolvió `"email rate limit exceeded"` (Supabase email confirmation IS ON → signup no crea sesión)
  - [x] R-W2d 3/3 tests correctamente SKIPPED con mensaje: `"W-2 BLOCKED: both auth paths failed. Self-register error on page: 'email rate limit exceeded'"`
  - [ ] R-W2e [PENDING] Desbloquear: desactivar email confirmation en Supabase Auth > Email settings, o proveer TEST_USER_EMAIL/TEST_USER_PASSWORD de cuenta pre-verificada
