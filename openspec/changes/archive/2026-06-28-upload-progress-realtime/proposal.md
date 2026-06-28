# Propuesta: Barra de Progreso en Tiempo Real para Carga de Extractos

## Intención

La página `/Transactions/Upload` realiza un POST síncrono sin retroalimentación hasta que la página se recarga con resultados. Con archivos grandes o múltiples, esto genera ansiedad y mala UX. Esta propuesta añade una barra de progreso en tiempo real que muestra total de filas, procesadas, porcentaje y conteos importadas/omitidas, actualizados en vivo sin recarga de página.

## Alcance

### Dentro del Alcance
- Interfaz `IImportProgressTracker` en capa Application
- Implementación `MemoryImportProgressTracker` con `IMemoryCache` en Frontend
- Modificación de `ImportTransactionsCommandHandler` para reportar progreso por fila
- Nuevo endpoint `GET /Transactions/Upload?handler=Progress&id={uploadId}` → HTML parcial
- Conversión del formulario de carga a `fetch()` asíncrono + polling HTMX (`hx-trigger="every 1s"`)
- Barra de progreso MDBootstrap animada con porcentaje y estadísticas
- Cese del polling al completarse la importación
- Preservación de validación de archivos existente (formato, tamaño, errores)
- Actualización de tests E2E

### Fuera del Alcance
- Sin SignalR / WebSockets
- Sin procesamiento en background jobs
- Sin cambios en la lógica del parser
- Sin progreso compartido multi-usuario
- Sin persistencia del progreso tras reinicio del servidor

## Capacidades

### Nuevas Capacidades
- `import-progress-tracking`: Seguimiento de progreso de importación en tiempo real mediante `IImportProgressTracker` + `IMemoryCache` + polling HTMX

### Capacidades Modificadas
- `transactions`: El flujo de carga se modifica de POST síncrono a `fetch()` asíncrono + polling, y el handler ahora reporta progreso por fila

## Enfoque

**Arquitectura**: `IImportProgressTracker` (interfaz en Application) → `MemoryImportProgressTracker` (implementación en Frontend con `IMemoryCache`). Respeta Clean Architecture: interfaz en capa interna, implementación de caché en capa externa.

**Flujo**:
1. Usuario envía archivo → `fetch()` POST al backend
2. Backend genera `uploadId`, crea `ImportProgress` en `IMemoryCache`
3. Inicia procesamiento de filas, actualizando `ImportProgress` por cada fila
4. Cliente hace polling `GET /Upload?handler=Progress&id=X` cada 1s con HTMX
5. Backend devuelve Partial HTML con barra de progreso MDBootstrap + estadísticas
6. Al completar: resultado final inline, polling se detiene

## Áreas Afectadas

| Área | Impacto | Descripción |
|------|--------|-------------|
| `src/.../Commands/ImportTransactionsCommandHandler.cs` | Modificado | Añade `IImportProgressTracker` y reporta progreso por fila |
| `src/.../Services/IImportProgressTracker.cs` | Nuevo | Interfaz de tracking en Application |
| `src/.../MemoryImportProgressTracker.cs` | Nuevo | Implementación con `IMemoryCache` en Frontend |
| `src/.../Pages/Transactions/Upload.cshtml` | Modificado | `fetch()` + polling HTMX + barra de progreso |
| `src/.../Pages/Transactions/Upload.cshtml.cs` | Modificado | Handler `OnGetProgress`; `OnPostAsync` devuelve JSON |
| `src/.../Program.cs` | Modificado | `AddMemoryCache()` |
| `e2e/tests/upload.spec.ts` | Modificado | Tests E2E para barra de progreso |

## Riesgos

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| `IMemoryCache` evicción bajo presión de memoria | Baja | `ImportProgress` es ~100 bytes; la importación dura segundos |
| Cargas concurrentes del mismo usuario | Baja | `uploadId` por GUID garantiza aislamiento |
| Polling continúa tras completar importación | Media | HTMX condicional: `every 1s` solo mientras exista `#progress-bar`; al recibir resultado final se hace swap completo |
| Cambio de firma del handler rompe tests | Baja | `IImportProgressTracker?` es opcional (null-safe para tests existentes) |

## Plan de Rollback

Revertir los 7 archivos a su estado anterior. Sin migraciones ni cambios de esquema. `AddMemoryCache()` en Program.cs es inocuo si se deja.

## Dependencias

Ninguna dependencia externa nueva. HTMX v2 ya está presente (CDN). `IMemoryCache` es parte del framework.

## Criterios de Éxito

- [ ] La barra de progreso muestra porcentaje actualizado cada segundo durante la importación
- [ ] Los contadores de importadas/omitidas/totales son correctos al finalizar
- [ ] El polling se detiene automáticamente al recibir el resultado final
- [ ] La validación de archivos existente sigue funcionando (errores de formato/tamaño)
- [ ] Tests E2E verifican que la barra de progreso aparece, avanza y desaparece al completar
- [ ] El flujo funciona con archivos pequeños (<10 filas) y grandes (>500 filas)
