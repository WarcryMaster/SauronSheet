# Diseño Técnico: Barra de Progreso en Tiempo Real para Carga de Extractos

## Enfoque Técnico

Se reemplaza el POST síncrono de `Upload.cshtml` por un flujo asíncrono con `fetch()` + polling HTMX. El handler `ImportTransactionsCommandHandler` reporta progreso vía `IImportProgressTracker` (Application/Services/), implementado con `IMemoryCache` en Frontend. La interfaz expone `ReportProgressAsync`, `CompleteAsync` y `FailAsync`. El cliente consulta `GET /Transactions/Upload?handler=Progress&id={uploadId}` cada 1s vía HTMX `hx-trigger="every 1s"` y recibe un partial HTML con barra MDBootstrap accesible. Al completar o fallar, se detiene el polling y se muestra el resultado final inline.

El flujo Alpine.js existente se conserva: `x-data` con flag `uploading` permanece; el handler `@@submit.prevent="handleUpload($el)"` ejecuta `fetch()` con `FormData`, extrae `uploadId`, inicia polling HTMX vía evento `startPolling`. Al recibir `HX-Trigger: stopPolling`, HTMX detiene el intervalo. El token antiforgery se incluye como header en el `fetch()`.

## Decisiones de Arquitectura

| Decisión | Opciones | Trade-off | Elegida |
|----------|----------|-----------|---------|
| 1. Ubicación del tracker | Application/Services/ vs Domain vs handler directo | Capa interna sin acoplamiento a infraestructura | Application/Services (sigue patrón `IBankCategoryResolutionService`) |
| 2. Almacenamiento | IMemoryCache vs ConcurrentDictionary vs DB | Expiración automática 5 min, nativo del framework | IMemoryCache, clave `import-progress-{uploadId}` |
| 3. Polling | HTMX `every 1s` vs SignalR vs setInterval | Sin dependencias nuevas, HTMX v2 ya presente | HTMX con cese vía `HX-Trigger: stopPolling` |
| 4. Envío formulario | fetch() + FormData vs hx-post | HTMX limitado con multipart/form-data | fetch() con `@@submit.prevent` |
| 5. Frecuencia reporte | Cada fila vs cada N filas | Balance entre fluidez visual y overhead de escritura en caché | Cada fila si ≤100 filas, cada 10 si >100 |
| 6. UploadId en Command | Requerido vs opcional | Backward compat con tests y call sites existentes | `string? UploadId = null` — sin progreso si es null |

## Flujo de Datos

```
Usuario → fetch() POST /Upload?handler=UploadAsync (con antiforgery header)
  → PageModel genera uploadId (GUID), inicializa ImportProgress en IMemoryCache
  → Dispara ImportTransactionsCommand(uploadId)
    → Handler: foreach row { procesa; cada N filas → _tracker.ReportProgressAsync() }
    → Handler: al finalizar → _tracker.CompleteAsync(uploadId)
    → Handler: si excepción → _tracker.FailAsync(uploadId, error)

Cliente (HTMX polling cada 1s):
  GET /Upload?handler=Progress&id={uploadId}
  → OnGetProgress valida ownership (userId en ImportProgress)
  → Devuelve Partial HTML con barra accesible + datos de progreso
  → IsComplete → HX-Trigger: stopPolling → reemplaza #progress-container con resultado
  → IsFailed → muestra error alert, re-habilita botón submit
```

## Cambios en Archivos

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/SauronSheet.Application/Services/IImportProgressTracker.cs` | Crear | Interfaz: `InitializeAsync`, `ReportProgressAsync`, `CompleteAsync`, `FailAsync` |
| `src/SauronSheet.Frontend/Services/ImportProgress.cs` | Crear | Record: `UploadId`, `Filename`, `TotalRows`, `ProcessedRows`, `ImportedCount`, `SkippedCount`, `IsComplete`, `IsFailed`, `ErrorMessage?`, `CurrentFileName`, `CurrentFileIndex`, `TotalFiles`, `UserId`, `StartedAt` |
| `src/SauronSheet.Frontend/Services/MemoryImportProgressTracker.cs` | Crear | Implementación IMemoryCache + `GetProgress(uploadId)` |
| `src/.../Commands/ImportTransactionsCommand.cs` | Modificar | Añadir `string? UploadId = null` — backward compatible |
| `src/.../Commands/ImportTransactionsCommandHandler.cs` | Modificar | Inyectar `IImportProgressTracker?`; reportar progreso; `CompleteAsync`/`FailAsync` |
| `src/.../Pages/Transactions/Upload.cshtml` | Modificar | `#progress-container` + `#result-container` + JS `handleUpload()` con antiforgery |
| `src/.../Pages/Transactions/Upload.cshtml.cs` | Modificar | `OnPostUploadAsync` → JSON `{uploadId}`; `OnGetProgress` → Partial HTML con validación de userId |
| `src/SauronSheet.Frontend/Program.cs` | Modificar | `AddMemoryCache()` + `AddScoped<IImportProgressTracker, MemoryImportProgressTracker>()` |

## Interfaces / Contratos

```csharp
// Application/Services/IImportProgressTracker.cs
public interface IImportProgressTracker
{
    Task InitializeAsync(string uploadId, string filename, int totalRows, string userId,
        string currentFileName, int currentFileIndex, int totalFiles, CancellationToken ct);
    Task ReportProgressAsync(string uploadId, int processedRows, int importedCount,
        int skippedCount, string? currentFileName = null, CancellationToken ct = default);
    Task CompleteAsync(string uploadId);
    Task FailAsync(string uploadId, string error);
}
```

```csharp
// Frontend/Services/ImportProgress.cs
public record ImportProgress(
    string UploadId, string Filename, int TotalRows, int ProcessedRows,
    int ImportedCount, int SkippedCount, bool IsComplete, bool IsFailed,
    string? ErrorMessage, string CurrentFileName, int CurrentFileIndex,
    int TotalFiles, string UserId, DateTime StartedAt);
```

```csharp
// Commands/ImportTransactionsCommand.cs — backward compatible
public record ImportTransactionsCommand(
    Stream FileStream, string Filename, string? UploadId = null) : IRequest<ImportResultDto>;
```

## Partial HTML — Barra de Progreso Accesible (REQ-PROG-011)

```html
<div id="progress-container" role="progressbar"
     aria-valuenow="${percentage}" aria-valuemin="0" aria-valuemax="100"
     aria-label="Import progress: ${processedRows} of ${totalRows} rows (${percentage}%)">
  <p class="small text-muted mb-1">
    Processing file ${currentFileIndex} of ${totalFiles}: ${currentFileName}
  </p>
  <div class="progress" style="height: 20px;">
    <div class="progress-bar" style="width: ${percentage}%">${percentage}%</div>
  </div>
  <p class="small mt-1">${processedRows}/${totalRows} rows | ✅ ${importedCount} | ⏭️ ${skippedCount}</p>
</div>
```

```html
<!-- Error state (when IsFailed) — reemplaza progress-container -->
<div class="alert alert-danger" role="alert">
  <p class="fw-semibold">❌ Import failed</p>
  <p class="small">${errorMessage}</p>
  <button type="button" class="btn btn-outline-danger btn-sm" @@click="uploading = false; progressVisible = false">
    Try again
  </button>
</div>
```

## Configuración Alpine.js + HTMX (Warnings 7 y 11)

El `x-data` existente (`uploading`, `files`, etc.) se extiende con:
```javascript
x-data="{
    // ... existing props ...
    progress: { uploadId: null, visible: false },
    async handleUpload(form) {
        this.uploading = true;
        const fd = new FormData(form);
        const token = form.querySelector('input[name=__RequestVerificationToken]').value;
        const resp = await fetch(form.action + '&handler=UploadAsync', {
            method: 'POST', body: fd,
            headers: { 'RequestVerificationToken': token }
        });
        const data = await resp.json();
        this.progress = { uploadId: data.uploadId, visible: true };
        document.body.dispatchEvent(new CustomEvent('startPolling', { detail: data.uploadId }));
    }
}"
```

HTMX polling se inicia vía evento `startPolling` y se detiene al recibir `HX-Trigger: {"stopPolling": true}`. Al recibir `IsFailed`, JS reemplaza progress bar por alerta de error y vuelve a habilitar el botón submit (`uploading = false`).

## Estrategia de Tests

| Nivel | Qué probar | Enfoque |
|-------|-----------|---------|
| Unit | `MemoryImportProgressTracker`: Initialize/ReportProgress/Complete/Fail/GetProgress | xUnit — IMemoryCache con `Mock.Of<>` o `new MemoryCache()` |
| Integration | Handler con tracker mockeado; UploadModel.OnGetProgress con userId validation | Verificar `FailAsync` en excepción; 403 si userId no coincide |
| E2E | Subir Excel → barra progreso visible con ARIA → avance → resultado final; probar fallo | `e2e/tests/02-upload-excel.spec.ts` — Playwright |

## Migración / Despliegue

No requiere migración. Sin cambios de esquema. Rollback: revertir archivos modificados. `AddMemoryCache()` es inocuo.

## Preguntas Abiertas

Ninguna. Todos los requisitos (REQ-PROG-004, 006, 011) y warnings (5, 7, 8, 9, 11) están cubiertos en este diseño.
