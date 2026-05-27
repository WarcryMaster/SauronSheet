# Diseño: Sustituir importación PDF por Excel

## Enfoque técnico

Reemplazar `IPdfParser` + toda la infraestructura PDF por un parser Excel basado en **ExcelDataReader** (MIT, .xls + .xlsx, read-only). El contrato de dominio se neutraliza a `IStatementParser`. La lógica de negocio del handler (deduplicación, resolución de categorías, persistencia) se conserva intacta con un renombrado neutral. Si en el futuro se necesita *generar* Excel, se añadirá ClosedXML como dependencia *adicional* sin afectar el parser de lectura.

## Decisiones de arquitectura

| Decisión | Opción elegida | Alternativas descartadas | Justificación |
|----------|---------------|--------------------------|---------------|
| Librería de lectura Excel | ExcelDataReader (MIT) | ClosedXML (no soporta .xls), NPOI (licencia restrictiva para revenue) | ExcelDataReader es MIT puro, soporta .xls + .xlsx. ClosedXML no lee .xls; NPOI tiene cláusula de mantenimiento incompatible con la restricción de coste cero. |
| Contrato de dominio | `IStatementParser` con `ParseAsync(Stream, string filename)` | Mantener `IPdfParser` con adapter | Eliminación limpia: sin adapter ni deuda semántica PDF. El filename permite inferir extensión si se necesitase en el futuro. |
| Ubicación del parser | `Infrastructure/Excel/IngExcelStatementParser.cs` | Poner en Domain | El parser depende de ExcelDataReader (librería I/O) → Infrastructure. Domain solo define la interfaz. |
| Tabla de metadata | Renombrar `pdf_imports` → `import_batches` vía ALTER TABLE | Crear tabla nueva + migrar datos | ALTER TABLE RENAME preserva datos, FKs y RLS sin downtime. |
| Generación futura de Excel | Se agregará ClosedXML en un cambio separado (no afecta diseño de lectura) | Incluir ClosedXML ahora "por si acaso" | YAGNI: añadir ahora acoplaría dos preocupaciones y aumentaría el diff sin beneficio inmediato. |

## Flujo de datos

```
Upload.cshtml (.xls/.xlsx) ──→ ImportTransactionsCommand ──→ Handler
      │                                                          │
      │                                                          ├─ IStatementParser.ParseAsync(stream, filename)
      │                                                          │       └─ IngExcelStatementParser (ExcelDataReader)
      │                                                          │               → List<RawTransactionRow>
      │                                                          │
      │                                                          ├─ Validación + deduplicación (sin cambios)
      │                                                          ├─ IBankCategoryResolutionService (sin cambios)
      │                                                          └─ IImportBatchRepository.AddAsync
      │
      └─ ImportResultDto ──→ UI resultado
```

## Cambios de ficheros

| Fichero | Acción | Descripción |
|---------|--------|-------------|
| `Domain/Services/IPdfParser.cs` | Delete | Reemplazado por `IStatementParser.cs` |
| `Domain/Services/IStatementParser.cs` | Create | Contrato neutro: `Task<List<RawTransactionRow>> ParseAsync(Stream stream, string filename)` |
| `Domain/Repositories/IPdfImportRepository.cs` | Delete | Reemplazado |
| `Domain/Repositories/IImportBatchRepository.cs` | Create | Mismo contrato, naming neutral |
| `Domain/ValueObjects/RawTransactionRow.cs` | Modify | Actualizar XML-doc (quitar "PDF") |
| `Infrastructure/Excel/IngExcelStatementParser.cs` | Create | Implementación con ExcelDataReader; hoja `Movimientos`, cabecera fila 4, datos fila 5+ |
| `Infrastructure/PDF/` (8 archivos) | Delete | Eliminación completa |
| `Infrastructure/Persistence/SupabasePdfImportRepository.cs` | Delete | Reemplazado |
| `Infrastructure/Persistence/SupabaseImportBatchRepository.cs` | Create | DTO apunta a `import_batches` |
| `Infrastructure/Persistence/Migrations/` | Create | `ALTER TABLE pdf_imports RENAME TO import_batches;` |
| `Infrastructure/DependencyInjection.cs` | Modify | Registrar ExcelDataReader, `IStatementParser`, `IImportBatchRepository`; eliminar refs PDF |
| `Infrastructure/SauronSheet.Infrastructure.csproj` | Modify | Quitar PdfPig; añadir ExcelDataReader + ExcelDataReader.DataSet |
| `Application/Commands/ImportTransactionsFromPdfCommand.cs` | Delete | Reemplazado |
| `Application/Commands/ImportTransactionsCommand.cs` | Create | `record ImportTransactionsCommand(Stream FileStream, string Filename)` |
| `Application/Commands/ImportTransactionsCommandHandler.cs` | Create | Misma lógica del handler actual; usa `IStatementParser` + `IImportBatchRepository`; extensión validada `.xls`/`.xlsx` |
| `Frontend/Pages/Transactions/Upload.cshtml` | Modify | accept=".xls,.xlsx"; guía de formato visible; quitar copy PDF |
| `Frontend/Pages/Transactions/Upload.cshtml.cs` | Modify | Propiedad `ExcelFile`; validar .xls/.xlsx; métricas `app.import.*`; despachar `ImportTransactionsCommand` |
| `Frontend/Pages/Transactions/Index.cshtml` | Modify | Copy "Upload PDF" → "Import Transactions" |
| `Frontend/Pages/Dashboard.cshtml` | Modify | Empty-state copy neutral |
| `tests/Application.Tests/.../ImportTransactionsFromPdfCommandTests.cs` | Delete → recrear | `ImportTransactionsCommandTests.cs` con misma cobertura + escenarios Excel |
| `tests/Infrastructure.Tests/PDF/` (3 archivos) | Delete | Tests PDF obsoletos |
| `tests/Infrastructure.Tests/Excel/IngExcelStatementParserTests.cs` | Create | Cobertura ESP-1..ESP-3 con muestras .xls reales |

## Interfaces / Contratos

```csharp
// Domain/Services/IStatementParser.cs
public interface IStatementParser
{
    Task<List<RawTransactionRow>> ParseAsync(Stream stream, string filename);
}

// Domain/Repositories/IImportBatchRepository.cs
public interface IImportBatchRepository
{
    Task AddAsync(ImportBatch importBatch, UserId userId);
    Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId);
}
```

## Estrategia de testing

| Capa | Qué se prueba | Enfoque |
|------|--------------|---------|
| Unit (Infra) | `IngExcelStatementParser`: cabecera válida, hoja ausente, cabecera incorrecta, fila malformada, fechas/importes | xUnit + muestras .xls reales copiadas al proyecto de test |
| Unit (App) | `ImportTransactionsCommandHandler`: deduplicación, resolución de categorías, errores por fila, extensión inválida | xUnit + Moq (mock `IStatementParser`, repos) |
| Integration | Registro DI resuelve `IStatementParser` y `IImportBatchRepository` correctamente | xUnit + ServiceCollection |
| E2E | Upload .xls → resultado visible; .pdf rechazado; guía de formato visible | Playwright chromium |

## Migración / Rollout

- Migración SQL: `ALTER TABLE pdf_imports RENAME TO import_batches;` — preserva datos existentes, índices y RLS.
- No se requiere feature flag: es sustitución completa en feature branch. La coexistencia PDF/Excel está explícitamente fuera de alcance.
- Rollback: revertir el rename con `ALTER TABLE import_batches RENAME TO pdf_imports;`.

## Riesgo de conflicto con spec activa

La spec `statement-category-extraction` renombra la interfaz `IPdfCategoryResolverService` → `IStatementCategoryResolverService`. En el código actual ese rename NO existe aún (sigue siendo `IBankCategoryResolutionService`). La implementación de este diseño NO debe tocar ese servicio — solo consume su interfaz existente. El rename del servicio de categorías se dejará para el archive o un cambio posterior dedicado.

## Preguntas abiertas

- Ninguna bloqueante. Todas las decisiones tienen evidencia verificada.
