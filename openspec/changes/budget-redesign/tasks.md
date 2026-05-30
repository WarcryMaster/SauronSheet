# Tareas: Rediseño del Sistema de Presupuestos — Políticas Permanentes

## Forecast de Carga de Revisión

| Campo | Valor |
|-------|-------|
| Líneas cambiadas estimadas | ~1,800-2,200 |
| Riesgo de presupuesto de 400 líneas | Alto |
| PRs encadenados recomendados | Sí |
| Estrategia de cadena | stacked-to-main |
| Estrategia de entrega | force-chained |

**Decisión necesaria antes de apply**: No (ya configurado como force-chained)
**PRs encadenados recomendados**: Sí
**Estrategia de cadena**: stacked-to-main
**Riesgo de presupuesto de 400 líneas**: Alto

### Slices Sugeridos

| Slice | Objetivo | PR | Notas |
|-------|----------|-----|-------|
| 1 | Domain foundation | PR 1 | Budget entity, BudgetPeriod enum, domain tests (~280 líneas) |
| 2 | Calculation service | PR 2 | BudgetCalculationService + unit tests (~320 líneas) |
| 3 | Infrastructure | PR 3 | Migration + repository + integration tests (~380 líneas) |
| 4 | Application commands | PR 4 | Create/Update/Deactivate commands + handler tests (~360 líneas) |
| 5 | Application queries | PR 5 | GetBudgets/Metrics/History queries + handler tests (~390 líneas) |
| 6 | Frontend management | PR 6 | Budget CRUD pages + E2E tests (~380 líneas) |
| 7 | Frontend dashboard | PR 7 | Metrics display + historical view + E2E tests (~350 líneas) |
| 8 | Cleanup | PR 8 | Remove old code + final verification (~150 líneas) |

---

## Slice 1: Domain Foundation (PR 1)

**Objetivo**: Establecer la entidad `Budget` rediseñada como política permanente con granularidad configurable.

### Tareas

- [ ] **1.1** Crear enum `BudgetPeriod` en `Domain/ValueObjects/BudgetPeriod.cs`
  - Valores: `Monthly`, `Quarterly`, `Semester`, `Annual`
  - **Archivos**: Crear `Domain/ValueObjects/BudgetPeriod.cs`
  - **Dependencias**: Ninguna
  - **Criterios de aceptación**: Enum compila y es usable desde otras clases
  - **Líneas estimadas**: 15
  - **Testing**: No requiere tests (es un enum)

- [ ] **1.2** Rediseñar entidad `Budget` en `Domain/Entities/Budget.cs`
  - Eliminar propiedad `DateRange Period`
  - Añadir propiedades: `EffectiveFrom` (DateOnly), `EffectiveUntil?` (DateOnly?), `PeriodGranularity` (BudgetPeriod)
  - Constructor parametrizado con validaciones: `Limit > 0`, `EffectiveUntil >= EffectiveFrom` si no es null
  - **Archivos**: Modificar `Domain/Entities/Budget.cs`
  - **Dependencias**: 1.1
  - **Criterios de aceptación**: Constructor valida invariantes; propiedades son inmutables excepto via métodos de actualización
  - **Líneas estimadas**: 80
  - **Testing**: RED → Escribir tests de constructor (límite negativo, fechas inválidas)

- [ ] **1.3** Añadir métodos de actualización a `Budget`
  - `UpdateLimit(Money newLimit)`: valida `newLimit > 0`
  - `UpdateEffectiveDates(DateOnly from, DateOnly? until)`: valida `until >= from`
  - `UpdateGranularity(BudgetPeriod newGranularity)`: actualiza granularidad
  - `Deactivate(DateOnly asOf)`: fija `EffectiveUntil = asOf`
  - `IsActiveOn(DateOnly date)`: retorna true si `date` está en el rango de vigencia
  - **Archivos**: Modificar `Domain/Entities/Budget.cs`
  - **Dependencias**: 1.2
  - **Criterios de aceptación**: Todos los métodos validan invariantes; `IsActiveOn()` maneja correctamente `EffectiveUntil = null`
  - **Líneas estimadas**: 60
  - **Testing**: RED → Tests para cada método (casos válidos e inválidos)

- [ ] **1.4** Implementar tests de dominio para `Budget` entity
  - Tests de constructor: límite positivo, fechas válidas
  - Tests de métodos de actualización: validaciones
  - Tests de `IsActiveOn()`: con `EffectiveUntil` null y no null
  - **Archivos**: Crear `tests/SauronSheet.Domain.Tests/Entities/BudgetTests.cs`
  - **Dependencias**: 1.3
  - **Criterios de aceptación**: Todos los tests pasan (GREEN); cobertura >90% de `Budget.cs`
  - **Líneas estimadas**: 120
  - **Testing**: GREEN → Implementar tests y verificar que pasan

- [ ] **1.5** Modificar `BudgetService` para validación de solapamiento
  - Renombrar `ValidateUniqueBudget()` → `ValidateNoOverlap(userId, categoryId, from, until?, excludeBudgetId?)`
  - Lógica: buscar budgets existentes para user+category cuyo rango se solape con `[from, until]`
  - Si `until` es null, tratar como rango infinito
  - **Archivos**: Modificar `Domain/Services/BudgetService.cs`
  - **Dependencias**: 1.2
  - **Criterios de aceptación**: Detecta solapamientos correctamente; permite rangos adyacentes (ej: uno termina 2026-06-30, otro inicia 2026-07-01)
  - **Líneas estimadas**: 40
  - **Testing**: RED → Tests de solapamiento (con y sin solapamiento, rangos adyacentes, permanentes)

- [ ] **1.6** Implementar tests de `BudgetService.ValidateNoOverlap()`
  - Mock de `IBudgetRepository`
  - Casos: sin solapamiento, con solapamiento, rangos adyacentes, budget permanente vs temporal
  - **Archivos**: Crear `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs`
  - **Dependencias**: 1.5
  - **Criterios de aceptación**: Todos los tests pasan (GREEN)
  - **Líneas estimadas**: 90
  - **Testing**: GREEN → Implementar tests y verificar que pasan

**Total Slice 1**: ~405 líneas (entity + enum + service + tests)

---

## Slice 2: Calculation Service (PR 2)

**Objetivo**: Implementar `BudgetCalculationService` con lógica de cálculo de períodos y métricas.

### Tareas

- [ ] **2.1** Crear record `BudgetCalculationResult` en `Domain/Services/BudgetCalculationResult.cs`
  - Propiedades: `PeriodsElapsed` (int), `AccumulatedLimit` (Money), `Spent` (Money), `Remaining` (Money), `PercentageUsed` (decimal), `StatusLevel` (BudgetStatusLevel)
  - **Archivos**: Crear `Domain/Services/BudgetCalculationResult.cs`
  - **Dependencias**: Ninguna
  - **Criterios de aceptación**: Record es inmutable; propiedades son accesibles
  - **Líneas estimadas**: 20
  - **Testing**: No requiere tests (es un record)

- [ ] **2.2** Implementar `BudgetCalculationService` en `Domain/Services/BudgetCalculationService.cs`
  - Método `Calculate(Budget budget, DateOnly from, DateOnly to, Money spent) → BudgetCalculationResult`
  - Método `PeriodsElapsed(BudgetPeriod granularity, DateOnly from, DateOnly to) → int`
  - Lógica de períodos: contar períodos completos de la granularidad dentro del rango
  - Respetar `EffectiveFrom` y `EffectiveUntil` del budget (solo contar períodos dentro de la vigencia)
  - Períodos parciales cuentan como completos (sin prorrateo)
  - **Archivos**: Crear `Domain/Services/BudgetCalculationService.cs`
  - **Dependencias**: 1.1, 1.2, 2.1
  - **Criterios de aceptación**: Calcula correctamente para cada granularidad; maneja períodos parciales; respeta fechas de vigencia
  - **Líneas estimadas**: 120
  - **Testing**: RED → Tests para cada granularidad (mensual, trimestral, semestral, anual)

- [ ] **2.3** Implementar lógica de cálculo de períodos por granularidad
  - Mensual: contar meses en el rango
  - Trimestral: contar trimestres (Q1, Q2, Q3, Q4)
  - Semestral: contar semestres (H1, H2)
  - Anual: contar años
  - **Archivos**: Modificar `Domain/Services/BudgetCalculationService.cs`
  - **Dependencias**: 2.2
  - **Criterios de aceptación**: Cada granularidad calcula períodos correctamente
  - **Líneas estimadas**: 80
  - **Testing**: RED → Tests específicos para cada granularidad

- [ ] **2.4** Implementar lógica de intersección con vigencia del budget
  - Si `from < EffectiveFrom`, ajustar `from = EffectiveFrom`
  - Si `to > EffectiveUntil`, ajustar `to = EffectiveUntil`
  - Si el rango ajustado es inválido (`from > to`), retornar `PeriodsElapsed = 0`
  - **Archivos**: Modificar `Domain/Services/BudgetCalculationService.cs`
  - **Dependencias**: 2.3
  - **Criterios de aceptación**: Solo cuenta períodos dentro de la vigencia del budget
  - **Líneas estimadas**: 40
  - **Testing**: RED → Tests con budgets que inician a mitad de rango, terminan antes del fin del rango

- [ ] **2.5** Implementar cálculo de métricas derivadas
  - `AccumulatedLimit = Limit × PeriodsElapsed`
  - `Remaining = AccumulatedLimit − Spent`
  - `PercentageUsed = (Spent ÷ AccumulatedLimit) × 100` (manejar división por cero)
  - `StatusLevel`: Green (<75%), Yellow (75-99%), Red (100%), Overage (>100%)
  - **Archivos**: Modificar `Domain/Services/BudgetCalculationService.cs`
  - **Dependencias**: 2.4
  - **Criterios de aceptación**: Métricas calculadas correctamente; estado semáforo asignado según umbrales
  - **Líneas estimadas**: 50
  - **Testing**: RED → Tests de métricas y estados semáforo

- [ ] **2.6** Implementar tests completos de `BudgetCalculationService`
  - Tests para cada granularidad (mensual, trimestral, semestral, anual)
  - Tests de períodos parciales (cuentan como completos)
  - Tests de intersección con vigencia (budget inicia/termina a mitad del rango)
  - Tests de métricas y estados semáforo
  - **Archivos**: Crear `tests/SauronSheet.Domain.Tests/Services/BudgetCalculationServiceTests.cs`
  - **Dependencias**: 2.5
  - **Criterios de aceptación**: Todos los tests pasan (GREEN); cobertura >95%
  - **Líneas estimadas**: 180
  - **Testing**: GREEN → Implementar tests y verificar que pasan

**Total Slice 2**: ~490 líneas (service + tests)

---

## Slice 3: Infrastructure (PR 3)

**Objetivo**: Implementar migración de base de datos y repository para el nuevo schema.

### Tareas

- [ ] **3.1** Crear migración de base de datos
  - Drop tabla `budgets` existente
  - Create nueva tabla `budgets` con columnas: `id`, `user_id`, `category_id`, `effective_from`, `effective_until`, `period_granularity`, `limit_amount`, `currency`, `created_at`, `updated_at`
  - Constraint CHECK: `limit_amount > 0`, `effective_until IS NULL OR effective_until >= effective_from`
  - Exclusion constraint con `btree_gist` para evitar solapamientos por user+category
  - Índices: `user_id`, `(user_id, category_id)`, `(effective_from, effective_until)`
  - RLS policies: SELECT, INSERT, UPDATE, DELETE solo para `auth.uid() = user_id`
  - **Archivos**: Crear `supabase/migrations/YYYYMMDDHHMMSS_budget_policies.sql`
  - **Dependencias**: Ninguna
  - **Criterios de aceptación**: Migración aplica sin errores; constraints funcionan correctamente
  - **Líneas estimadas**: 80
  - **Testing**: Aplicar migración en entorno local y verificar schema

- [ ] **3.2** Rediseñar `BudgetRow` en infraestructura
  - Eliminar campos de periodo mensual (`year`, `month`)
  - Añadir campos: `effective_from` (DateOnly), `effective_until` (DateOnly?), `period_granularity` (string)
  - **Archivos**: Modificar `Infrastructure/Persistence/SupabaseBudgetRepository.cs` (clase interna `BudgetRow`)
  - **Dependencias**: 3.1
  - **Criterios de aceptación**: `BudgetRow` mapea correctamente a la nueva tabla
  - **Líneas estimadas**: 30
  - **Testing**: No requiere tests (es un DTO de infraestructura)

- [ ] **3.3** Implementar métodos de query en `SupabaseBudgetRepository`
  - `GetActiveByUserAndCategoryAsync(userId, categoryId, DateOnly asOf)`: obtiene budget activo en una fecha dada
  - `GetByUserAndDateRangeAsync(userId, from, to)`: obtiene budgets que se solapan con el rango
  - Eliminar método obsoleto `GetByUserAndCategoryAndMonthAsync`
  - **Archivos**: Modificar `Infrastructure/Persistence/SupabaseBudgetRepository.cs`
  - **Dependencias**: 3.2
  - **Criterios de aceptación**: Queries retornan resultados correctos; filtran por fechas de vigencia
  - **Líneas estimadas**: 80
  - **Testing**: RED → Tests de integración con base de datos en memoria

- [ ] **3.4** Implementar mapeo entre `Budget` (domain) y `BudgetRow` (infrastructure)
  - `ToDomain(BudgetRow row) → Budget`: mapea campos y reconstruye entidad
  - `ToRow(Budget budget) → BudgetRow`: mapea entidad a fila para persistencia
  - **Archivos**: Modificar `Infrastructure/Persistence/SupabaseBudgetRepository.cs`
  - **Dependencias**: 1.2, 3.2
  - **Criterios de aceptación**: Mapeo bidireccional preserva todos los campos
  - **Líneas estimadas**: 50
  - **Testing**: Tests de mapeo (ida y vuelta)

- [ ] **3.5** Implementar tests de integración del repository
  - Tests de `AddAsync()`: inserción correcta
  - Tests de `GetActiveByUserAndCategoryAsync()`: con budget activo, sin budget activo
  - Tests de `GetByUserAndDateRangeAsync()`: con solapamiento, sin solapamiento
  - Tests de `UpdateAsync()` y `DeleteAsync()`
  - **Archivos**: Crear `tests/SauronSheet.Infrastructure.Tests/Persistence/SupabaseBudgetRepositoryTests.cs`
  - **Dependencias**: 3.3, 3.4
  - **Criterios de aceptación**: Todos los tests pasan (GREEN)
  - **Líneas estimadas**: 140
  - **Testing**: GREEN → Implementar tests y verificar que pasan

**Total Slice 3**: ~380 líneas (migration + repository + tests)

---

## Slice 4: Application Commands (PR 4)

**Objetivo**: Implementar commands para crear, actualizar y desactivar budgets.

### Tareas

- [ ] **4.1** Rediseñar `CreateBudgetCommand` y su handler
  - Command: `CreateBudgetCommand(Guid CategoryId, decimal LimitAmount, DateOnly EffectiveFrom, DateOnly? EffectiveUntil, BudgetPeriod PeriodGranularity)`
  - Handler: valida categoría existe, valida no solapamiento via `BudgetService.ValidateNoOverlap()`, crea budget, persiste
  - **Archivos**: Modificar `Application/Features/Budgets/Commands/CreateBudgetCommand.cs` y `CreateBudgetCommandHandler.cs`
  - **Dependencias**: 1.5, 3.3
  - **Criterios de aceptación**: Crea budget válido; rechaza categorías inexistentes; rechaza solapamientos
  - **Líneas estimadas**: 80
  - **Testing**: RED → Tests de handler (casos válidos e inválidos)

- [ ] **4.2** Crear `UpdateBudgetLimitCommand` y su handler
  - Command: `UpdateBudgetLimitCommand(Guid BudgetId, decimal NewLimitAmount)`
  - Handler: obtiene budget, llama a `budget.UpdateLimit()`, persiste cambios
  - **Archivos**: Crear `Application/Features/Budgets/Commands/UpdateBudgetLimitCommand.cs` y `UpdateBudgetLimitCommandHandler.cs`
  - **Dependencias**: 1.3, 3.3
  - **Criterios de aceptación**: Actualiza límite; valida que nuevo límite sea positivo
  - **Líneas estimadas**: 60
  - **Testing**: RED → Tests de handler

- [ ] **4.3** Crear `UpdateBudgetPeriodCommand` y su handler
  - Command: `UpdateBudgetPeriodCommand(Guid BudgetId, BudgetPeriod NewPeriod, decimal NewLimitAmount)`
  - Handler: obtiene budget, llama a `budget.UpdateGranularity()` y `budget.UpdateLimit()`, persiste
  - **Archivos**: Crear `Application/Features/Budgets/Commands/UpdateBudgetPeriodCommand.cs` y `UpdateBudgetPeriodCommandHandler.cs`
  - **Dependencias**: 1.3, 3.3
  - **Criterios de aceptación**: Actualiza granularidad y límite por período
  - **Líneas estimadas**: 65
  - **Testing**: RED → Tests de handler

- [ ] **4.4** Crear `DeactivateBudgetCommand` y su handler
  - Command: `DeactivateBudgetCommand(Guid BudgetId, DateOnly AsOf)`
  - Handler: obtiene budget, llama a `budget.Deactivate(asOf)`, persiste cambios
  - **Archivos**: Crear `Application/Features/Budgets/Commands/DeactivateBudgetCommand.cs` y `DeactivateBudgetCommandHandler.cs`
  - **Dependencias**: 1.3, 3.3
  - **Criterios de aceptación**: Desactiva budget fijando `EffectiveUntil`
  - **Líneas estimadas**: 55
  - **Testing**: RED → Tests de handler

- [ ] **4.5** Implementar tests de todos los command handlers
  - Tests de `CreateBudgetCommandHandler`: categoría válida, solapamiento, límite inválido
  - Tests de `UpdateBudgetLimitCommandHandler`: límite válido, límite inválido
  - Tests de `UpdateBudgetPeriodCommandHandler`: granularidad válida
  - Tests de `DeactivateBudgetCommandHandler`: desactivación correcta
  - Mock de repositorios y servicios de dominio
  - **Archivos**: Modificar `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/` (varios archivos)
  - **Dependencias**: 4.1, 4.2, 4.3, 4.4
  - **Criterios de aceptación**: Todos los tests pasan (GREEN); cobertura >85%
  - **Líneas estimadas**: 160
  - **Testing**: GREEN → Implementar tests y verificar que pasan

**Total Slice 4**: ~420 líneas (commands + handlers + tests)

---

## Slice 5: Application Queries (PR 5)

**Objetivo**: Implementar queries para obtener budgets, métricas e histórico.

### Tareas

- [ ] **5.1** Rediseñar `GetBudgetsQuery` y su handler
  - Query: `GetBudgetsQuery(DateOnly? AsOf = null)` — si `AsOf` es null, retorna todos los budgets del usuario
  - Handler: obtiene budgets via repository, mapea a `BudgetDto`
  - **Archivos**: Modificar `Application/Features/Budgets/Queries/GetBudgetsQuery.cs` y `GetBudgetsQueryHandler.cs`
  - **Dependencias**: 3.3
  - **Criterios de aceptación**: Retorna budgets activos en fecha dada o todos los budgets
  - **Líneas estimadas**: 70
  - **Testing**: RED → Tests de handler

- [ ] **5.2** Crear `GetBudgetMetricsQuery` y su handler
  - Query: `GetBudgetMetricsQuery(DateOnly From, DateOnly To)`
  - Handler: obtiene budgets activos en el rango, obtiene transacciones de categorías con budgets, llama a `BudgetCalculationService.Calculate()` para cada budget, retorna `List<BudgetMetricsDto>`
  - Incluir categorías sin budget con etiqueta "Sin presupuesto"
  - **Archivos**: Crear `Application/Features/Budgets/Queries/GetBudgetMetricsQuery.cs` y `GetBudgetMetricsQueryHandler.cs`
  - **Dependencias**: 2.2, 3.3
  - **Criterios de aceptación**: Calcula métricas correctamente para el rango; incluye categorías sin budget
  - **Líneas estimadas**: 120
  - **Testing**: RED → Tests de handler con múltiples budgets y categorías

- [ ] **5.3** Crear `GetBudgetHistoryQuery` y su handler
  - Query: `GetBudgetHistoryQuery(int Year)`
  - Handler: obtiene budgets activos en el año dado, calcula métricas para el rango del año, retorna `List<BudgetPeriodSummaryDto>`
  - **Archivos**: Crear `Application/Features/Budgets/Queries/GetBudgetHistoryQuery.cs` y `GetBudgetHistoryQueryHandler.cs`
  - **Dependencias**: 2.2, 3.3
  - **Criterios de aceptación**: Retorna resumen histórico del año solicitado
  - **Líneas estimadas**: 80
  - **Testing**: RED → Tests de handler

- [ ] **5.4** Adaptar `GetBudgetVsActualQuery` al nuevo modelo
  - Modificar handler para usar `BudgetCalculationService` en lugar de lógica hardcoded
  - Aceptar rango de fechas como parámetro
  - **Archivos**: Modificar `Application/Features/Budgets/Queries/GetBudgetVsActualQuery.cs` y `GetBudgetVsActualQueryHandler.cs`
  - **Dependencias**: 2.2, 3.3
  - **Criterios de aceptación**: Comparación budget vs actual funciona con el nuevo modelo
  - **Líneas estimadas**: 60
  - **Testing**: RED → Tests de handler

- [ ] **5.5** Crear DTOs para queries
  - `BudgetDto`: id, categoryId, categoryName, effectiveFrom, effectiveUntil, periodGranularity, limit
  - `BudgetMetricsDto`: budgetId, categoryId, categoryName, periodsElapsed, accumulatedLimit, spent, remaining, percentageUsed, statusLevel
  - `BudgetPeriodSummaryDto`: month/period, accumulatedLimit, spent, remaining, statusLevel
  - **Archivos**: Modificar `Application/Features/Budgets/DTOs/` (varios archivos)
  - **Dependencias**: Ninguna
  - **Criterios de aceptación**: DTOs contienen todos los campos necesarios para las vistas
  - **Líneas estimadas**: 80
  - **Testing**: No requiere tests (son DTOs)

- [ ] **5.6** Implementar tests de todos los query handlers
  - Tests de `GetBudgetsQueryHandler`: con y sin filtro de fecha
  - Tests de `GetBudgetMetricsQueryHandler`: múltiples budgets, categorías sin budget, métricas correctas
  - Tests de `GetBudgetHistoryQueryHandler`: resumen histórico
  - Tests de `GetBudgetVsActualQueryHandler`: comparación correcta
  - Mock de repositorios y `BudgetCalculationService`
  - **Archivos**: Modificar `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/` (varios archivos)
  - **Dependencias**: 5.1, 5.2, 5.3, 5.4, 5.5
  - **Criterios de aceptación**: Todos los tests pasan (GREEN); cobertura >85%
  - **Líneas estimadas**: 180
  - **Testing**: GREEN → Implementar tests y verificar que pasan

**Total Slice 5**: ~590 líneas (queries + handlers + DTOs + tests)

---

## Slice 6: Frontend Management (PR 6)

**Objetivo**: Implementar páginas de gestión de budgets (CRUD).

### Tareas

- [ ] **6.1** Rediseñar página de creación de budget
  - Formulario con campos: categoría (dropdown), límite, fecha inicio, fecha fin (opcional), granularidad (dropdown)
  - Validación client-side y server-side
  - **Archivos**: Modificar `Frontend/Pages/Budgets/Create.cshtml` y `Create.cshtml.cs`
  - **Dependencias**: 4.1
  - **Criterios de aceptación**: Usuario puede crear budget con todos los campos; validaciones funcionan
  - **Líneas estimadas**: 120
  - **Testing**: E2E test de creación de budget

- [ ] **6.2** Rediseñar página de edición de budget
  - Formulario para editar límite, granularidad, fechas
  - Botón para desactivar budget
  - **Archivos**: Modificar `Frontend/Pages/Budgets/Edit.cshtml` y `Edit.cshtml.cs`
  - **Dependencias**: 4.2, 4.3, 4.4
  - **Criterios de aceptación**: Usuario puede editar límite, granularidad y fechas; puede desactivar budget
  - **Líneas estimadas**: 130
  - **Testing**: E2E test de edición y desactivación

- [ ] **6.3** Rediseñar página de lista de budgets
  - Tabla con columnas: categoría, granularidad, límite, vigencia (desde-hasta), estado (activo/inactivo)
  - Filtros: mostrar solo activos, por categoría
  - **Archivos**: Modificar `Frontend/Pages/Budgets/Index.cshtml` y `Index.cshtml.cs`
  - **Dependencias**: 5.1
  - **Criterios de aceptación**: Lista muestra todos los budgets con filtros funcionales
  - **Líneas estimadas**: 100
  - **Testing**: E2E test de navegación y filtros

- [ ] **6.4** Implementar tests E2E de gestión de budgets
  - Test de creación de budget válido
  - Test de creación con validación (límite negativo)
  - Test de edición de límite
  - Test de desactivación de budget
  - Test de navegación por la lista
  - **Archivos**: Crear `e2e/tests/budgets/management.spec.ts`
  - **Dependencias**: 6.1, 6.2, 6.3
  - **Criterios de aceptación**: Todos los tests E2E pasan; interacción real de usuario (sin `page.evaluate()`)
  - **Líneas estimadas**: 150
  - **Testing**: E2E → Implementar tests Playwright con interacción real

**Total Slice 6**: ~500 líneas (pages + E2E tests)

---

## Slice 7: Frontend Dashboard (PR 7)

**Objetivo**: Implementar páginas de visualización de métricas e histórico.

### Tareas

- [ ] **7.1** Crear página de métricas de budget
  - Vista de mes actual: métricas del mes calendario actual
  - Vista de período actual: métricas del período de granularidad actual
  - Vista de año actual: métricas acumuladas del año en curso
  - Tarjetas con: límite acumulado, gastado, restante, porcentaje, estado semáforo
  - **Archivos**: Crear `Frontend/Pages/Budgets/Metrics.cshtml` y `Metrics.cshtml.cs`
  - **Dependencias**: 5.2
  - **Criterios de aceptación**: Página muestra métricas correctas para las tres vistas
  - **Líneas estimadas**: 140
  - **Testing**: E2E test de visualización de métricas

- [ ] **7.2** Crear página de histórico de budget
  - Selector de año
  - Tabla/gráfico con resumen mensual del año seleccionado
  - **Archivos**: Crear `Frontend/Pages/Budgets/History.cshtml` y `History.cshtml.cs`
  - **Dependencias**: 5.3
  - **Criterios de aceptación**: Página muestra histórico del año seleccionado
  - **Líneas estimadas**: 110
  - **Testing**: E2E test de navegación y selector de año

- [ ] **7.3** Actualizar página de comparación budget vs actual
  - Adaptar a nuevo modelo con rango de fechas
  - Mostrar categorías sin budget con etiqueta "Sin presupuesto"
  - **Archivos**: Modificar `Frontend/Pages/Budgets/Comparison.cshtml` y `Comparison.cshtml.cs`
  - **Dependencias**: 5.4
  - **Criterios de aceptación**: Comparación muestra todas las categorías con actividad
  - **Líneas estimadas**: 80
  - **Testing**: E2E test de comparación

- [ ] **7.4** Actualizar widget de budgets en dashboard
  - Consumir vista de mes actual de `GetBudgetMetricsQuery`
  - Mostrar porcentaje total consumido y recuento por estado
  - **Archivos**: Modificar `Frontend/Pages/Dashboard/Index.cshtml` y `Index.cshtml.cs` (sección de budgets)
  - **Dependencias**: 5.2
  - **Criterios de aceptación**: Widget muestra resumen de budgets del mes actual
  - **Líneas estimadas**: 60
  - **Testing**: E2E test de widget en dashboard

- [ ] **7.5** Implementar tests E2E de visualización
  - Test de página de métricas (tres vistas)
  - Test de página de histórico (selector de año)
  - Test de página de comparación
  - Test de widget en dashboard
  - **Archivos**: Crear `e2e/tests/budgets/visualization.spec.ts`
  - **Dependencias**: 7.1, 7.2, 7.3, 7.4
  - **Criterios de aceptación**: Todos los tests E2E pasan
  - **Líneas estimadas**: 140
  - **Testing**: E2E → Implementar tests Playwright

**Total Slice 7**: ~530 líneas (pages + widget + E2E tests)

---

## Slice 8: Cleanup (PR 8)

**Objetivo**: Eliminar código obsoleto y verificar integración completa.

### Tareas

- [ ] **8.1** Eliminar código de monthly budgets obsoleto
  - Eliminar comandos/queries/handlers de monthly budgets que ya no se usan
  - Eliminar DTOs obsoletos
  - **Archivos**: Modificar `Application/Features/Budgets/` (eliminar archivos obsoletos)
  - **Dependencias**: 4.1, 5.1
  - **Criterios de aceptación**: No queda código de monthly budgets; compilación exitosa
  - **Líneas estimadas**: -80 (eliminación)
  - **Testing**: Verificar que compilación pasa

- [ ] **8.2** Actualizar documentación inline
  - Comentarios en código sobre el nuevo modelo
  - XML docs en métodos públicos de domain services
  - **Archivos**: Modificar varios archivos de Domain y Application
  - **Dependencias**: 1.1, 2.2
  - **Criterios de aceptación**: Código documentado según convenciones
  - **Líneas estimadas**: 40
  - **Testing**: No requiere tests

- [ ] **8.3** Ejecutar suite completa de tests
  - Tests unitarios de dominio
  - Tests de integración de aplicación
  - Tests E2E de frontend
  - **Archivos**: Ninguno (ejecución de tests)
  - **Dependencias**: Todas las slices anteriores
  - **Criterios de aceptación**: Todos los tests pasan; cobertura >80% en Domain, >70% en Application
  - **Líneas estimadas**: 0
  - **Testing**: Ejecutar `dotnet test` y `npx playwright test`

- [ ] **8.4** Verificar migración en entorno de staging
  - Aplicar migración en base de datos de staging
  - Verificar que constraints funcionan
  - Verificar que RLS policies funcionan
  - **Archivos**: Ninguno (verificación manual)
  - **Dependencias**: 3.1
  - **Criterios de aceptación**: Migración aplica sin errores; constraints y RLS funcionan
  - **Líneas estimadas**: 0
  - **Testing**: Verificación manual en staging

**Total Slice 8**: ~-40 líneas (neto, por eliminaciones)

---

## Resumen de Implementación

### Orden Recomendado

1. **Slice 1** (Domain foundation) → **Slice 2** (Calculation service) → **Slice 3** (Infrastructure)
   - Razón: Establece la base de dominio antes de implementar infraestructura
2. **Slice 4** (Commands) → **Slice 5** (Queries)
   - Razón: Commands primero para poder crear datos de prueba para queries
3. **Slice 6** (Frontend management) → **Slice 7** (Frontend dashboard)
   - Razón: CRUD primero para tener datos que visualizar
4. **Slice 8** (Cleanup)
   - Razón: Solo después de que todo funcione

### Dependencias Críticas

- Slice 2 depende de Slice 1 (usa `Budget` entity y `BudgetPeriod` enum)
- Slice 3 depende de Slice 1 (mapea `Budget` entity a `BudgetRow`)
- Slice 4 depende de Slices 1 y 3 (usa domain services y repository)
- Slice 5 depende de Slices 2 y 3 (usa `BudgetCalculationService` y repository)
- Slices 6 y 7 dependen de Slices 4 y 5 (usan commands y queries)

### Estrategia de Testing

- **TDD estricto**: Cada tarea sigue RED → GREEN → REFACTOR
- **Cobertura objetivo**: Domain >80%, Application >70%
- **E2E tests**: Solo en Slices 6 y 7, con interacción real de usuario

---

## Forecast de Carga de Revisión (Detallado)

| Slice | Líneas estimadas | PR | Excede 400 líneas? |
|-------|------------------|-----|-------------------|
| 1 | ~405 | PR 1 | Sí (leve) |
| 2 | ~490 | PR 2 | Sí |
| 3 | ~380 | PR 3 | No |
| 4 | ~420 | PR 4 | Sí (leve) |
| 5 | ~590 | PR 5 | Sí |
| 6 | ~500 | PR 6 | Sí |
| 7 | ~530 | PR 7 | Sí |
| 8 | ~-40 | PR 8 | No |

**Total estimado**: ~2,875 líneas (sin contar eliminaciones)

**Recomendación**: Proceder con PRs encadenados usando estrategia `stacked-to-main`. Cada PR se mergea a `main` en orden. Algunos PRs exceden levemente las 400 líneas, pero son autónomos y revisables.

**Alternativa**: Si se requiere estrictamente <400 líneas por PR, dividir Slices 2, 5, 6 y 7 en sub-slices adicionales (aumentaría complejidad de coordinación).
