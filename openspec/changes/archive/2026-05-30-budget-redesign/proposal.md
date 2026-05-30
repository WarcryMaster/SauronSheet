# Proposal: Rediseño del Sistema de Presupuestos — Políticas Permanentes con Cálculo por Período

## Intent

El sistema actual de presupuestos es mensual por diseño: cada presupuesto representa un mes específico y el usuario debe recrearlos manualmente cada mes. No existe visión anual, acumulada ni multi-período. El cambio convierte el presupuesto en una **política permanente** con granularidad configurable (mensual, trimestral, semestral, anual), donde el cálculo de métricas se deriva en tiempo de consulta desde las transacciones, sin snapshots históricos.

## Scope

### In Scope
- Nueva entidad `Budget` como política permanente: `EffectiveFrom`, `EffectiveUntil?`, `BudgetPeriod` (enum), `Money Limit`
- Unicidad: un solo presupuesto activo por usuario + categoría
- `BudgetCalculationService`: cálculo de períodos transcurridos y límite acumulado bajo demanda
- Cuatro vistas: mes actual, período actual, año actual, histórico por año
- Migración de base de datos: drop + recreate de tabla `budgets` (sin migración de datos existentes)
- Reescritura completa de commands, queries, DTOs, frontend (Razor Pages) y tests

### Out of Scope
- Migración de datos de presupuestos mensuales existentes (se eliminan)
- Notificaciones, alertas o forecasting
- Presupuestos compartidos entre usuarios
- Rollover de excedentes entre períodos
- Subcategorías de presupuesto

## Capabilities

### New Capabilities
- `budget-policies`: Presupuestos permanentes con granularidad de período, CRUD con unicidad usuario+categoría, servicio de cálculo de períodos, y todas las vistas derivadas (mes, período, año, histórico)

### Modified Capabilities
- `monthly-budgets`: Esta capacidad queda **reemplazada en su totalidad** por `budget-policies`. La delta spec marcará todos los requisitos como deprecados y referenciará la nueva capacidad.

## Approach

**Rediseño limpio (Approach 2 de la exploración)**: `Budget` se redefine como aggregate root con semántica de política permanente. Se elimina `DateRange Period` y se introduce `BudgetPeriod` (enum) + `EffectiveFrom/Until`. Un `BudgetCalculationService` concentra toda la lógica de cálculo: dado un rango de fechas, determina cuántos períodos han transcurrido y el límite acumulado (`Limit × periods`). El spending se calcula siempre desde transacciones en tiempo de consulta — nunca se almacena.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Domain/Budgets/` | Modified | Rediseño de `Budget.cs`, nuevo `BudgetPeriod.cs`, nuevo `BudgetCalculationService.cs`, actualización de `BudgetService.cs` e `IBudgetRepository.cs` |
| `Infrastructure/Persistence/` | Modified | Reescritura de `SupabaseBudgetRepository.cs` y `BudgetRow` |
| `supabase/migrations/` | New | Nueva migración: drop + recreate tabla `budgets` |
| `Application/Budgets/` | Modified | Reescritura de todos los commands, queries, handlers y DTOs (~12-14 archivos) |
| `Frontend/Pages/Budgets/` | Modified | Rediseño de 5 páginas existentes + nuevas vistas (~10-12 archivos) |
| `tests/` | Modified | Reescritura de ~8 archivos de tests de presupuestos |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Regresión en queries de dashboard que consumen budgets | High | Tests de integración para cada query antes de desplegar |
| Edge case: solapamiento de `EffectiveFrom` entre dos budgets misma categoría | Medium | Validación en `BudgetService` + constraint UNIQUE en DB |
| Rendimiento: queries de spending con muchos períodos históricos | Medium | Una sola consulta de transactions por rango, distribución en memoria |
| Complejidad conceptual de UI para usuarios acostumbrados al modelo mensual | Medium | Diseño de UI con selector de granularidad claro y valores por defecto |

## Rollback Plan

Revertir la migración de base de datos (recrear tabla vieja con schema original) y restaurar el código anterior desde la rama de release. Los datos de presupuestos viejos se habrán eliminado, pero esto es aceptable según los requisitos confirmados.

## Dependencies

- Decisión de diseño sobre regla de solapamiento temporal (un solo budget activo por user+category)
- Definición exacta de cálculo de períodos parciales (¿prorrateo o período completo?)

## Success Criteria

- [ ] Un presupuesto creado aplica automáticamente a todos los períodos futuros sin recreación manual
- [ ] El cambio de límite recalcula métricas de todos los períodos históricos
- [ ] Las cuatro vistas (mes, período, año, histórico) muestran datos correctos
- [ ] Todos los tests existentes de budgets reescritos y pasando
- [ ] Cobertura ≥ 80% en Domain y ≥ 70% en Application para el nuevo código
