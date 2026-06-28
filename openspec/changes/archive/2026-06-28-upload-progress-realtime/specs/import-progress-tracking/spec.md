# Delta Spec: Import Progress Tracking

Seguimiento de progreso en tiempo real para la carga de extractos Excel, mediante polling HTMX + `IImportProgressTracker` en `IMemoryCache`. Reemplaza el POST síncrono por `fetch()` asíncrono con barra de progreso MDBootstrap.

---

## ADDED Requirements — import-progress-tracking

### REQ-PROG-001: Progress bar displays during import

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-001 |
| Prioridad | CRITICAL |

El sistema DEBE mostrar una barra de progreso con porcentaje (0–100%), total de filas y filas procesadas actualizadas en tiempo real.

#### Escenario: Importación inicia correctamente (CRITICAL)
- **Given** usuario envía archivo Excel válido
- **When** el backend inicia el procesamiento
- **Then** se renderiza barra con `role="progressbar"`, porcentaje 0%, total de filas visible, y contador de procesadas en 0

#### Escenario: Actualización en tiempo real (CRITICAL)
- **Given** importación en progreso (50 de 200 filas)
- **When** el polling HTMX retorna el partial actualizado
- **Then** la barra muestra 25%, procesadas=50, total=200

---

### REQ-PROG-002: Imported vs skipped counts

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-002 |
| Prioridad | CRITICAL |

El sistema DEBE mostrar contadores separados de filas importadas y omitidas, actualizados en cada respuesta de polling.

#### Escenario: Contadores vivos (CRITICAL)
- **Given** importación procesó 30 filas: 25 importadas, 5 omitidas
- **When** polling retorna estado
- **Then** se muestra "Importadas: 25 | Omitidas: 5"

---

### REQ-PROG-003: Polling stops on completion

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-003 |
| Prioridad | CRITICAL |

El sistema DEBE detener el polling automáticamente cuando `IsComplete = true` y mostrar el resultado final inline.

#### Escenario: Completado exitoso (CRITICAL)
- **Given** todas las filas procesadas, `IsComplete = true`
- **When** polling recibe respuesta final
- **Then** polling se detiene, se muestra resumen final (importadas/omitidas/errores), y la barra se reemplaza con el resultado

---

### REQ-PROG-004: Per-file info for multi-file uploads

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-004 |
| Prioridad | NORMAL |

El sistema DEBE mostrar el nombre del archivo actual y el índice (archivo N de M) durante procesamiento secuencial.

#### Escenario: Múltiples archivos (NORMAL)
- **Given** usuario envía 3 archivos Excel
- **When** se procesa el segundo archivo
- **Then** se muestra "Procesando: archivo2.xlsx (2 de 3)"

---

### REQ-PROG-005: Upload does not reload the page

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-005 |
| Prioridad | CRITICAL |

El sistema DEBE usar `fetch()` para el POST. La página NO DEBE recargarse. El botón submit DEBE deshabilitarse; el resto del formulario permanece interactivo.

#### Escenario: Envío asíncrono (CRITICAL)
- **Given** formulario de carga visible
- **When** usuario selecciona archivo y hace click en "Importar"
- **Then** se envía vía `fetch()`, no hay recarga, el botón está `disabled`, la barra de progreso aparece

---

### REQ-PROG-006: Error handling during progress

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-006 |
| Prioridad | CRITICAL |

El sistema DEBE detener la barra y mostrar error inline si ocurre un error fatal. El botón submit DEBE re-habilitarse.

#### Escenario: Error de parseo (CRITICAL)
- **Given** importación en progreso
- **When** ocurre error fatal (ej. formato inválido detectado tarde)
- **Then** barra se detiene, mensaje de error visible inline, botón submit re-habilitado

---

### REQ-PROG-007: Progress state isolated per upload

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-007 |
| Prioridad | CRITICAL |

El sistema DEBE aislar el progreso por `uploadId` (GUID). Cada usuario DEBE ver solo su propio progreso.

#### Escenario: Usuarios concurrentes (CRITICAL)
- **Given** Usuario A (uploadId=X) y Usuario B (uploadId=Y) cargan archivos simultáneamente
- **When** ambos polling activamente
- **Then** cada uno recibe solo su propio `ImportProgress` desde `IMemoryCache`

---

## MODIFIED Requirements — transactions (upload flow)

### REQ-PROG-008: File validation preserved

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-008 |
| Prioridad | CRITICAL |
| Previamente | Validación de archivos mostraba errores tras recarga de página |

El sistema DEBE preservar la validación existente (formato, tamaño, archivo vacío). Los errores DEBEN mostrarse inline sin recargar la página.

#### Escenario: Archivo inválido (CRITICAL)
- **Given** usuario selecciona archivo .csv (formato inválido)
- **When** envía el formulario
- **Then** se muestra error de validación inline, sin recarga de página

#### Escenario: Archivo vacío (CRITICAL)
- **Given** usuario selecciona Excel sin filas de datos
- **When** envía el formulario
- **Then** se muestra error "archivo vacío" inline

---

### REQ-PROG-009: Existing import logic unchanged

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-009 |
| Prioridad | CRITICAL |
| Previamente | Handler ejecutaba parsing, detección de duplicados, resolución de categorías y persistencia de forma síncrona |

El sistema DEBE ejecutar la misma lógica de parsing, detección de duplicados, resolución de categorías y persistencia. `ImportResultDto` DEBE contener los mismos datos que antes.

#### Escenario: Lógica intacta (CRITICAL)
- **Given** archivo Excel válido con 50 filas
- **When** importación se procesa
- **Then** el resultado final contiene las mismas transacciones, duplicados y resoluciones que antes del cambio

---

### REQ-PROG-010: Format guide remains visible

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-010 |
| Prioridad | NORMAL |
| Previamente | Format guide visible above the fold en la página de carga |

El sistema DEBE mantener la tarjeta de guía de formato Excel visible above the fold en la página de carga.

#### Escenario: Guía visible (NORMAL)
- **Given** página /Transactions/Upload renderizada
- **When** usuario carga la página
- **Then** la guía de formato es visible sin scroll

---

## ADDED Requirements — Accessibility

### REQ-PROG-011: Progress bar is accessible

| Campo | Valor |
|-------|-------|
| ID | REQ-PROG-011 |
| Prioridad | CRITICAL |

El sistema DEBE incluir atributos ARIA en la barra de progreso: `role="progressbar"`, `aria-valuenow`, `aria-valuemin="0"`, `aria-valuemax="100"`, y `aria-label` descriptivo.

#### Escenario: Screen reader (CRITICAL)
- **Given** barra de progreso renderizada al 45%
- **When** screen reader inspecciona el elemento
- **Then** anuncia "Progreso de importación: 45%" con `aria-valuenow="45"`, `aria-valuemin="0"`, `aria-valuemax="100"`

---

## Non-Requirements (explícitamente fuera)

- NO modificar la lógica del parser de extractos
- NO añadir procesamiento en background jobs
- NO persistir progreso tras reinicio del servidor
- NO usar WebSockets/SignalR
- NO procesar múltiples archivos en paralelo
