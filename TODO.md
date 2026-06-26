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

~~### 2. Editar transacciones existentes~~ ✅ Completado
~~Añadir funcionalidad de edición en página dedicada (`/transactions/edit/{id}`).~~

~~**Funcionalidad implementada:**~~
- ~~Botón con icono de lápiz en cada fila de la tabla de transacciones~~
- ~~Página dedicada `/transactions/edit/{id}` con formulario completo~~
- ~~Campos editables: fecha (Flatpickr), descripción, importe, moneda, categoría, subcategoría~~
- ~~Validación: duplicados, pertenencia de categoría/subcategoría, tenant isolation~~
- ~~Preserva metadata de importación (ImportedFrom, BankCategory, BankSubcategory, Balance)~~

~~**Archivos creados/modificados:**~~
- ~~`src/SauronSheet.Domain/Entities/Transaction.cs` — método `Update()`~~
- ~~`src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionByIdQuery.cs` + Handler~~
- ~~`src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCommand.cs` + Handler~~
- ~~`src/SauronSheet.Frontend/Pages/Transactions/Edit.cshtml` + `Edit.cshtml.cs`~~
- ~~`src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml` — botón editar~~
- ~~`e2e/tests/03-edit-transaction.spec.ts` — 5 tests E2E~~

~~**Tests:** 6 Domain + 3 Query + 5 Command + 5 E2E = 19 tests~~
~~**Nota E2E:** Requiere `--config=e2e/playwright.config.ts` para cargar baseURL y webServer correctamente~~

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

~~### 10. Tests faltantes para handlers existentes~~ ✅ Completado

~~| Handler | Archivo | Estado |
|---|---|---|
| `DeleteTransactionCommandHandler` | `src/.../Commands/DeleteTransactionCommandHandler.cs` | Sin tests |
| `UpdateTransactionCategoryCommandHandler` | `src/.../Commands/UpdateTransactionCategoryCommandHandler.cs` | Sin tests |
| `DeleteBudgetCommandHandler` | `src/.../Budgets/Commands/DeleteBudgetCommandHandler.cs` | Sin tests |
| `SupabaseUserRepository` | `src/.../Persistence/SupabaseUserRepository.cs` | Sin tests |
| `SupabaseImportBatchRepository` | `src/.../Persistence/SupabaseImportBatchRepository.cs` | Sin tests |
| `PeriodsElapsed` switch default (BudgetCalculationService) | `src/.../Services/BudgetCalculationService.cs:131-141` | `ArgumentOutOfRangeException` no testeado |~~

~~### 11. Revisar tests E2E de upload (siempre en skip)~~ ✅ Completado
~~**Fichero:** `e2e/tests/02-upload-excel.spec.ts`

Los tests `TC-U01`, `TC-U02`, `TC-U03` estaban comentados como "RED: fail" pero la página Upload.cshtml ya tenía todo implementado. Limpiados comentarios desactualizados y verificados: 3/3 pasan.~~

~~### 12. `var` en Domain Layer (contra reglas de AGENTS.md)~~ ✅ Completado
~~9 usos de `var` reemplazados por tipos explícitos en 4 ficheros: TransactionByIdSpecification (3), CategoryService (2), CompositeSpecification (3), ColorHex (1).~~

## Prioridad Baja

~~### 13. Domain Events colección comentada en `AggregateRoot.cs`~~ ✅ Completado
~~**Fichero:** `src/SauronSheet.Domain/Common/AggregateRoot.cs`

Código muerto comentado (`// protected List<IDomainEvent> _domainEvents = new();`).
Eliminado — sin consumidores reales hoy.~~

~~### 14. `FailedTransactionIds` siempre `null` en `BulkDeleteTransactionsCommandHandler`~~ ✅ Completado
~~**Fichero:** `src/.../Commands/BulkDeleteTransactionsCommandHandler.cs` (líneas 61-64, 111-114)

Ahora devuelve los IDs solicitados como fallidos en cualquier ruta de error.
Tests actualizados para verificar la población de FailedTransactionIds.~~

~~### 15. `SupabaseCategoryRepository` referencia directa a `TransactionRow` (violación SRP)~~ ✅ Completado
~~**Fichero:** `src/.../Persistence/SupabaseCategoryRepository.cs` (líneas 360, 371)

Los métodos `HasTransactionsAsync` y `GetTransactionCountAsync` hacían queries contra la tabla de transacciones. Movidos a `ITransactionRepository` + `SupabaseTransactionRepository`. Lo mismo para `SupabaseSubcategoryRepository`. Handlers actualizados para usar `ITransactionRepository`. Tests actualizados.~~

~~### 16. Try-catch-Sentry repetido en repositorios~~ ✅ Completado
~~**Ficheros:** Todos en `src/SauronSheet.Infrastructure/Persistence/*.cs`

35+ métodos con el mismo patrón try-catch-Sentry. El `SentryTracingBehavior` de MediatR ya captura errores. Evaluar si el patrón en repositorios es redundante y extraer a helper.~~

~~### 17. `Edit.cshtml.cs` carga todos los budgets para encontrar uno~~ ✅ Completado
~~**Fichero:** `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml.cs` (líneas 154-155)

`GetBudgetsQuery()` devuelve todos los budgets del usuario y luego se filtra con LINQ en memoria. Crear `GetBudgetByIdQuery` dedicado.~~
