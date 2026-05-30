# Especificación: Cálculo de Métricas de Presupuesto

## Propósito

Servicio de dominio que calcula métricas de presupuesto bajo demanda a partir
de las transacciones existentes. Dado un rango de fechas y una política de
presupuesto, determina cuántos períodos han transcurrido, el límite acumulado
y el gasto real ejecutado.

---

## Requisitos

### Requisito: BudgetCalculationService

El sistema DEBE incluir un `BudgetCalculationService` en la capa de dominio que,
dado un `Budget` y un rango de fechas (`DateOnly from`, `DateOnly to`), calcule:

| Métrica | Fórmula |
|---------|---------|
| `PeriodsElapsed` | Número de períodos completos de la granularidad dentro del rango |
| `AccumulatedLimit` | `Limit × PeriodsElapsed` |
| `Spent` | Suma de expenses de la categoría en el rango (desde transacciones) |
| `Remaining` | `AccumulatedLimit − Spent` |
| `PercentageUsed` | `(Spent ÷ AccumulatedLimit) × 100` |
| `IsOverBudget` | `Spent > AccumulatedLimit` |

#### Escenario: Presupuesto mensual — cálculo de un mes

- DADO un presupuesto mensual con `Limit = 500 €` y `EffectiveFrom = 2026-01-01`
- CUANDO se calcula para el rango `2026-05-01` a `2026-05-31`
- ENTONCES `PeriodsElapsed = 1`, `AccumulatedLimit = 500 €`

#### Escenario: Presupuesto mensual — cálculo acumulado anual

- DADO un presupuesto mensual con `Limit = 500 €` y `EffectiveFrom = 2026-01-01`
- CUANDO se calcula para el rango `2026-01-01` a `2026-12-31`
- ENTONCES `PeriodsElapsed = 12`, `AccumulatedLimit = 6000 €`

#### Escenario: Presupuesto trimestral — cálculo de un trimestre

- DADO un presupuesto trimestral con `Limit = 1500 €`
- CUANDO se calcula para el rango de un trimestre completo
- ENTONCES `PeriodsElapsed = 1`, `AccumulatedLimit = 1500 €`

#### Escenario: Presupuesto anual — cálculo de un año

- DADO un presupuesto anual con `Limit = 6000 €`
- CUANDO se calcula para el rango de un año completo
- ENTONCES `PeriodsElapsed = 1`, `AccumulatedLimit = 6000 €`

---

### Requisito: Cálculo de períodos parciales

Cuando el rango de consulta no cubre un período completo de la granularidad,
el sistema DEBE contar ese período como completo (sin prorrateo).

#### Escenario: Presupuesto mensual consultado a mitad de mes

- DADO un presupuesto mensual con `Limit = 500 €`
- CUANDO se calcula para el rango `2026-05-01` a `2026-05-15`
- ENTONCES `PeriodsElapsed = 1` (el mes parcial cuenta como un período completo)

#### Escenario: Presupuesto trimestral consultado en el segundo mes del trimestre

- DADO un presupuesto trimestral con `Limit = 1500 €`
- CUANDO se calcula para el rango que cubre solo 2 meses del trimestre
- ENTONCES `PeriodsElapsed = 1` (el trimestre parcial cuenta como uno completo)

---

### Requisito: Respetar EffectiveFrom y EffectiveUntil en el cálculo

El servicio DEBE considerar exclusivamente los períodos que caen dentro de la
vigencia del presupuesto. Los períodos anteriores a `EffectiveFrom` o posteriores
a `EffectiveUntil` NO cuentan para `PeriodsElapsed`.

#### Escenario: Presupuesto iniciado a mitad de año — consulta anual

- DADO un presupuesto mensual con `EffectiveFrom = 2026-04-01` y `Limit = 500 €`
- CUANDO se calcula para el rango `2026-01-01` a `2026-12-31`
- ENTONCES `PeriodsElapsed = 9` (abril a diciembre), `AccumulatedLimit = 4500 €`

#### Escenario: Presupuesto desactivado — consulta posterior a EffectiveUntil

- DADO un presupuesto con `EffectiveUntil = 2026-06-30`
- CUANDO se calcula para el rango `2026-07-01` a `2026-12-31`
- ENTONCES `PeriodsElapsed = 0`, `AccumulatedLimit = 0 €`

---

### Requisito: Estado semáforo (Green / Yellow / Red / Overage)

El sistema DEBE asignar un estado a cada resultado de cálculo con los mismos
criterios que el modelo anterior:

| Estado | Condición |
|--------|-----------|
| **Green** | `PercentageUsed < 75 %` |
| **Yellow** | `75 % ≤ PercentageUsed < 100 %` |
| **Red** | `PercentageUsed = 100 %` |
| **Overage** | `PercentageUsed > 100 %` |

#### Escenario: Cálculo con estado Green

- DADO un presupuesto mensual con `Limit = 500 €` y gasto de `300 €` en el período
- ENTONCES `PercentageUsed = 60 %`, estado = **Green**

#### Escenario: Cálculo con estado Overage

- DADO un presupuesto mensual con `Limit = 500 €` y gasto de `600 €` en el período
- ENTONCES `PercentageUsed = 120 %`, estado = **Overage**, `Remaining = −100 €`

---

### Requisito: Vistas de consulta

El sistema DEBE soportar las siguientes vistas de consulta, todas derivadas
del mismo `BudgetCalculationService`:

| Vista | Rango de fechas | Descripción |
|-------|-----------------|-------------|
| Mes actual | Primer y último día del mes calendario actual | Gasto y límite del mes en curso |
| Período actual | Inicio y fin del período de granularidad actual | Gasto y límite del período en curso |
| Año actual | `1 enero` a `31 diciembre` del año actual | Acumulado anual |
| Histórico por año | `1 enero` a `31 diciembre` de un año dado | Acumulado de un año específico |

#### Escenario: Vista mes actual

- DADO un presupuesto mensual con `Limit = 500 €` y la fecha actual es `2026-05-15`
- CUANDO el usuario consulta la vista de mes actual
- ENTONCES el sistema calcula métricas para el rango `2026-05-01` a `2026-05-31`

#### Escenario: Vista año actual

- DADO un presupuesto mensual con `Limit = 500 €` y `EffectiveFrom = 2026-01-01`
- CUANDO el usuario consulta la vista de año actual (siendo 2026)
- ENTONCES el sistema muestra `AccumulatedLimit = 6000 €` y el gasto acumulado de enero a diciembre

#### Escenario: Vista histórica de un año anterior

- DADO un presupuesto mensual activo desde 2024
- CUANDO el usuario consulta la vista histórica del año 2025
- ENTONCES el sistema calcula métricas para `2025-01-01` a `2025-12-31`

---

### Requisito: Comparación budget vs actual

El sistema DEBE incluir en la vista de comparación **todas** las categorías
con actividad de gasto en el rango consultado, incluyendo las que no tienen
presupuesto definido.

#### Escenario: Categoría sin presupuesto con gasto

- DADO que "Ocio" tiene 45 € gastados en el rango consultado y no tiene presupuesto
- CUANDO el usuario abre la comparación
- ENTONCES "Ocio" aparece con la etiqueta "Sin presupuesto" y 45 € gastados

---

### Requisito: Widget de presupuestos en el dashboard

El widget del dashboard DEBE mostrar el resumen de presupuestos del **mes
calendario actual** usando la vista de mes actual del `BudgetCalculationService`.

#### Escenario: Dashboard con presupuestos activos

- DADO que existen presupuestos con estados mixtos en el mes actual
- CUANDO el usuario abre el dashboard
- ENTONCES el widget muestra el porcentaje total consumido y el recuento por estado

#### Escenario: Dashboard sin presupuestos

- DADO que el usuario no tiene presupuestos configurados
- CUANDO abre el dashboard
- ENTONCES el widget muestra un estado vacío con llamada a la acción

---

## Fuera del alcance

Prorrateo de períodos parciales, forecasting, burn rate, notificaciones
de umbral, rollover de excedentes.
