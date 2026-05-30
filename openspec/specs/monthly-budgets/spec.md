# Presupuestos Mensuales por Categoría — DEPRECATED

> **⚠️ DEPRECATED**: Esta capacidad ha sido reemplazada en su totalidad por
> `budget-policies` y `budget-calculation` (cambio `budget-redesign`,
> archivado el 2026-05-30). No debe modificarse ni utilizarse como
> referencia para nuevas funcionalidades.

---

## Propósito (original — obsoleto)

Control accionable del gasto mensual por categoría. Convierte el histórico de
transacciones en límites de gasto concretos con indicador de estado, permitiendo
comparar lo presupuestado contra lo ejecutado en cualquier mes.

---

## Requisitos

Todos los requisitos originales han sido eliminados y reemplazados por:

| Requisito original | Reemplazado por |
|--------------------|-----------------|
| Gestión CRUD de presupuestos | `budget-policies` — Requisito: Entidad Budget como política permanente |
| Cálculo de métricas derivadas | `budget-calculation` — Requisito: BudgetCalculationService |
| Estado semáforo (Green / Yellow / Red / Overage) | `budget-calculation` — Requisito: Estado semáforo |
| Comparación budget vs actual | `budget-calculation` — Requisito: Comparación budget vs actual |
| Widget de presupuestos en el dashboard | `budget-calculation` — Requisito: Widget de presupuestos en el dashboard |

---

## Nota de migración

La tabla `budgets` existente fue eliminada sin migración de datos. Se creó una
nueva tabla con el schema de `budget-policies`. Los presupuestos mensuales
anteriores se perdieron de forma intencional según los requisitos confirmados.

---

## Fuera del alcance

Notificaciones push/email, burn rate, forecasting, rollover, plantillas de meses
anteriores, subcategorías, presupuestos compartidos.
