# Delta para Presupuestos Mensuales por Categoría

## REMOVED Requirements

### Requisito: Gestión CRUD de presupuestos

(Reason: Reemplazado por la capacidad `budget-policies`. El modelo de presupuesto mensual con unicidad usuario+categoría+mes se sustituye por políticas permanentes con granularidad configurable y unicidad usuario+categoría.)

### Requisito: Cálculo de métricas derivadas

(Reason: Reemplazado por `budget-calculation`. El cálculo de métricas pasa de ser por mes fijo a ser derivado bajo demanda por el `BudgetCalculationService` con soporte para múltiples granularidades y rangos de fecha.)

### Requisito: Estado semáforo (Green / Yellow / Red / Overage)

(Reason: Reemplazado por `budget-calculation`. La lógica de estado semáforo se mantiene idéntica pero ahora reside en el `BudgetCalculationService` y aplica a cualquier granularidad de período, no solo mensual.)

### Requisito: Comparación budget vs actual

(Reason: Reemplazado por `budget-calculation`. La vista de comparación ahora opera sobre rangos de fecha arbitrarios en lugar de un mes fijo, e incluye todas los períodos soportados por la granularidad del presupuesto.)

### Requisito: Widget de presupuestos en el dashboard

(Reason: Reemplazado por `budget-calculation`. El widget del dashboard consume la vista de mes actual del `BudgetCalculationService`. El comportamiento observable para el usuario es equivalente.)

---

## Nota de migración

La tabla `budgets` existente se elimina sin migración de datos. Se crea una
nueva tabla con el schema de `budget-policies`. Los presupuestos mensuales
anteriores se pierden de forma intencional según los requisitos confirmados.
