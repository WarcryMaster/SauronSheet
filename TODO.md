    # TODO — SauronSheet

## Prioridad Alta

### 1. Consolidar funcionalidades existentes
Revisar y asegurar que todas las funcionalidades actuales funcionan correctamente:
- Flujo completo de importación de movimientos (Excel/PDF)
- CRUD de categorías y subcategorías
- CRUD de transacciones
- Presupuestos (creación, comparación)
- Dashboard (resumen, gráficos)
- Autenticación y sesión
- Tests unitarios, de integración y E2E

~~### 2. Bug: desfase horario en transacciones (producción vs local)~~ ✅ Completado

### 2. Editar transacciones existentes
Añadir funcionalidad de edición inline en la lista de transacciones (`Transactions/Index`).

**Funcionalidad:**
- Botón con icono de lápiz en cada fila de la tabla de transacciones
- Al hacer clic, la fila se vuelve editable: fecha, descripción, importe, categoría y subcategoría
- Botón "Guardar" para persistir los cambios con validaciones en cliente y servidor
- Botón "Cancelar" para descartar cambios y volver al modo vista

**Tareas previstas:**
- Implementar `UpdateTransactionCommandHandler` con validaciones
- Endpoint HTMX o handler Razor para guardar la edición
- Componente Alpine.js `x-data` para la edición inline (alternar modo vista/edición)
- Selectores de categoría/subcategoría dependientes en el modo edición
- Flatpickr para el campo fecha en el formulario de edición
- Validación: importe > 0, fecha requerida, descripción requerida
- Tests unitarios del handler + tests E2E del flujo de edición

### 3. Nueva página: "Análisis Anual" — Gastos fijos vs variables por mes
Crear una nueva página (no en Dashboard) para visualizar el desglose de ingresos y gastos fijos/variables por mes del año seleccionado.

**Funcionalidad:**
- Selector de año (por defecto el año actual)
- Obtener todos los movimientos del año seleccionado
- Sumar todos los gastos/ingresos por mes
- Catalogar cada movimiento como **fijo** o **variable** según el tipo de movimiento
- Calcular la media mensual por cada tipo de movimiento
- Mostrar tabla con columnas:
  - Tipo de movimiento (con indicador fijo/variable/ingreso fijo/ingreso variable)
  - Media mensual
  - Enero, Febrero, ..., Diciembre (totales por mes)

**Posible estructura de tabla:**

| Movimiento | Tipo | Media | Ene | Feb | Mar | ... | Dic |
|---|---|---|---|---|---|---|---|
| Hipoteca | Gasto Fijo | 850€ | 850€ | 850€ | 850€ | ... | 850€ |
| Supermercado | Gasto Variable | 320€ | 350€ | 280€ | 330€ | ... | 320€ |
| Nómina | Ingreso Fijo | 2400€ | 2400€ | 2400€ | 2400€ | ... | 2400€ |

**Tareas de implementación:**
- Definir especificación (Spec) y diseño (Design) vía SDD
- Implementar Query/Handler en Application layer para obtener el resumen anual
- Crear PageModel + Razor Page (Page: `/AnnualAnalysis`)
- Frontend con Alpine.js + MDBootstrap + Chart.js (opcional para gráficos)
- Tests unitarios + E2E

~~### 4. Migrar `_BudgetStatusModal.cshtml` a Alpine.js~~ ✅ Completado
~~**Fichero:** `src/SauronSheet.Frontend/Pages/Shared/Components/_BudgetStatusModal.cshtml`~~

~~Es el último componente de toda la app que usa JavaScript vanilla. Viola múltiples reglas de AGENTS.md:~~

~~### 5. Bug: `JwtCookieMiddleware` — riesgo de deadlock y HttpClient sin gestión~~ ✅ Completado
~~**Fichero:** `src/SauronSheet.Infrastructure/Auth/JwtCookieMiddleware.cs` (líneas 188-191)~~

~~Dos problemas graves:~~

~~### 6. Bug: `DeleteTransactionsByIdsAsync` borra uno por uno sin transacción real~~ ✅ Completado
~~**Fichero:** `src/SauronSheet.Infrastructure/Persistence/SupabaseTransactionRepository.cs` (líneas 433-439)~~

~~Aunque el método sugiere atomicidad, hace N queries individuales sin transacción.
Si el ítem 3 falla, los ítems 1 y 2 ya se borraron.~~

~~**Tareas:**
- Implementar un único DELETE con `IN (...)` o usar el cliente Postgrest con filtro múltiple
- Verificar atomicidad PostgreSQL
- Añadir tests de integración~~

## Prioridad Media

~~### 7. Catch silenciosos en `SupabaseAuthService` sin Sentry~~ ✅ Completado
~~**Fichero:** `src/SauronSheet.Infrastructure/Auth/SupabaseAuthService.cs`~~

~~Tres métodos tienen `catch { }` vacío que traga excepciones sin enviar a Sentry:
- `LogoutAsync` (línea 203) — comentario: "Logout errors are non-fatal"
- `RefreshTokenAsync` (línea 246)
- `GetUserProfileAsync` (línea 281)

**Tareas:**
- Capturar con `SentrySdk.CaptureException` en todos
- Decidir si relanzar o tragar en cada caso~~

~~### 8. Eliminar duplicación de `GetStatusLevel` entre `BudgetCalculationService` y `BudgetService`~~ ✅ Completado
~~**Ficheros:**
- `src/SauronSheet.Domain/Services/BudgetCalculationService.cs` (líneas 54-57)
- `src/SauronSheet.Domain/Services/BudgetService.cs` (líneas 100-108)

Ambos tienen lógica de thresholds con los mismos valores pero no comparten código.

**Tareas:**
- Extraer a un método compartido o mover a `BudgetService.GetStatusLevel`
- Hacer que `BudgetCalculationService` delegue~~

~~### 9. Unificar `CategoryName` y `SubcategoryName` en un Value Object común~~ ✅ Completado
~~**Ficheros:**
- `src/SauronSheet.Domain/ValueObjects/CategoryName.cs`
- `src/SauronSheet.Domain/ValueObjects/SubcategoryName.cs`

Son idénticos: validación no vacío + `.Trim()`. Podrían compartir base o unificarse en un `Name`.~~

### 10. Tests faltantes para handlers existentes

| Handler | Archivo | Estado |
|---|---|---|
| `DeleteTransactionCommandHandler` | `src/.../Commands/DeleteTransactionCommandHandler.cs` | Sin tests |
| `UpdateTransactionCategoryCommandHandler` | `src/.../Commands/UpdateTransactionCategoryCommandHandler.cs` | Sin tests |
| `DeleteBudgetCommandHandler` | `src/.../Budgets/Commands/DeleteBudgetCommandHandler.cs` | Sin tests |
| `SupabaseUserRepository` | `src/.../Persistence/SupabaseUserRepository.cs` | Sin tests |
| `SupabaseImportBatchRepository` | `src/.../Persistence/SupabaseImportBatchRepository.cs` | Sin tests |
| `PeriodsElapsed` switch default (BudgetCalculationService) | `src/.../Services/BudgetCalculationService.cs:131-141` | `ArgumentOutOfRangeException` no testeado |

### 11. Revisar tests E2E de upload (siempre en skip)
**Fichero:** `e2e/tests/02-upload-excel.spec.ts`

Los tests `TC-U01`, `TC-U02`, `TC-U03` están marcados como `test.skip(true, ...)` y nunca se ejecutan. Diseñados originalmente para fallar (documentaban WIP). Decidir si:
- Completarlos y activarlos, o
- Eliminarlos si ya no representan funcionalidad requerida

### 12. `var` en Domain Layer (contra reglas de AGENTS.md)
Decenas de usos de `var` en Domain donde AGENTS.md exige tipos explícitos.
Aplicar refactor global.

## Prioridad Baja

### 13. Domain Events colección comentada en `AggregateRoot.cs`
**Fichero:** `src/SauronSheet.Domain/Common/AggregateRoot.cs`

Código muerto comentado (`// protected List<IDomainEvent> _domainEvents = new();`).
Decidir si implementar Domain Events o limpiar.

### 14. `FailedTransactionIds` siempre `null` en `BulkDeleteTransactionsCommandHandler`
**Fichero:** `src/.../Commands/BulkDeleteTransactionsCommandHandler.cs` (líneas 61-64, 111-114)

En todos los casos de error se devuelve `null` en vez de los IDs que fallaron.
Completar funcionalidad para que la UI pueda mostrar qué transacciones fallaron.

### 15. `SupabaseCategoryRepository` referencia directa a `TransactionRow` (violación SRP)
**Fichero:** `src/.../Persistence/SupabaseCategoryRepository.cs` (líneas 360, 371)

Los métodos `HasTransactionsAsync` y `GetTransactionCountAsync` hacen queries contra la tabla de transacciones desde el repositorio de categorías. Delegar al repositorio de transacciones.

### 16. Try-catch-Sentry repetido en repositorios
**Ficheros:** Todos en `src/SauronSheet.Infrastructure/Persistence/*.cs`

35+ métodos con el mismo patrón try-catch-Sentry. El `SentryTracingBehavior` de MediatR ya captura errores. Evaluar si el patrón en repositorios es redundante y extraer a helper.

### 17. `Edit.cshtml.cs` carga todos los budgets para encontrar uno
**Fichero:** `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml.cs` (líneas 154-155)

`GetBudgetsQuery()` devuelve todos los budgets del usuario y luego se filtra con LINQ en memoria. Crear `GetBudgetByIdQuery` dedicado.
