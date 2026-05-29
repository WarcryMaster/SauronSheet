# Presupuestos Mensuales por Categoría

## Propósito

Control accionable del gasto mensual por categoría. Convierte el histórico de
transacciones en límites de gasto concretos con indicador de estado, permitiendo
comparar lo presupuestado contra lo ejecutado en cualquier mes.

---

## Requisitos

### Requisito: Gestión CRUD de presupuestos

El sistema DEBE permitir crear, consultar, editar y eliminar presupuestos mensuales
por categoría. Solo puede existir un presupuesto por combinación (usuario, categoría,
mes-año). El importe DEBE ser mayor que cero. La categoría DEBE existir en el
catálogo del usuario.

#### Escenario: Crear presupuesto con datos válidos

- DADO que el usuario está autenticado y la categoría "Alimentación" existe
- CUANDO crea un presupuesto de 500 € para "Alimentación" en mayo 2026
- ENTONCES el presupuesto se guarda y aparece en el listado del mes

#### Escenario: Duplicado rechazado

- DADO que ya existe un presupuesto para "Alimentación" en mayo 2026
- CUANDO el usuario intenta crear otro para la misma combinación (usuario, categoría, mes-año)
- ENTONCES el sistema DEBE rechazar la operación con error de validación

#### Escenario: Editar importe de presupuesto existente

- DADO un presupuesto de 500 € para "Alimentación" en mayo 2026
- CUANDO el usuario lo edita con un nuevo importe de 600 €
- ENTONCES el sistema guarda el nuevo importe y recalcula todas las métricas derivadas

#### Escenario: Eliminar presupuesto

- DADO un presupuesto existente para cualquier categoría
- CUANDO el usuario lo elimina
- ENTONCES desaparece del listado; la categoría pasa a mostrarse como "sin presupuesto"

---

### Requisito: Cálculo de métricas derivadas

Para cada presupuesto activo el sistema DEBE calcular en tiempo de consulta:

| Métrica | Cálculo |
|---------|---------|
| `gastado` | Suma de transacciones de esa categoría en el mes-año del presupuesto |
| `restante` | `presupuesto − gastado` (puede ser negativo si hay exceso) |
| `porcentaje_usado` | `(gastado ÷ presupuesto) × 100` |

#### Escenario: Sin transacciones en el mes

- DADO un presupuesto de 200 € para una categoría sin transacciones en el mes
- CUANDO el usuario consulta el detalle
- ENTONCES `gastado = 0`, `restante = 200`, `porcentaje_usado = 0 %`

---

### Requisito: Estado semáforo (Green / Yellow / Red / Overage)

El sistema DEBE asignar un estado a cada presupuesto con los criterios siguientes:

| Estado | Condición | Significado |
|--------|-----------|-------------|
| **Green** | `porcentaje_usado < 75 %` | Dentro del límite |
| **Yellow** | `75 % ≤ porcentaje_usado < 100 %` | Cerca del límite |
| **Red** | `porcentaje_usado = 100 %` | Límite alcanzado exactamente |
| **Overage** | `porcentaje_usado > 100 %` | Límite superado |

"On track" DEBE definirse como estado **Green** o **Yellow**.
Estado **Red** u **Overage** NO son "on track".

#### Escenario: Transición a Yellow

- DADO un presupuesto de 100 € con 76 € gastados en el mes
- ENTONCES el estado DEBE ser **Yellow** y `porcentaje_usado = 76 %`

#### Escenario: Overage con importe negativo

- DADO un presupuesto de 100 € con 110 € gastados en el mes
- ENTONCES el estado DEBE ser **Overage**, `porcentaje_usado = 110 %` y `restante = −10`

---

### Requisito: Comparación budget vs actual

El sistema DEBE incluir en la vista de comparación de un mes **todas** las categorías
con actividad de gasto en ese mes, incluyendo las que no tienen presupuesto definido.

| Caso | Visualización |
|------|---------------|
| Categoría con presupuesto | Estado semáforo + métricas (gastado, restante, % usado) |
| Sin presupuesto, con gasto | Etiqueta "Sin presupuesto" + importe gastado |
| Sin presupuesto y sin gasto | No aparece en la vista |

#### Escenario: Categoría sin presupuesto con gasto

- DADO que "Ocio" tiene 45 € gastados en mayo 2026 y no tiene presupuesto asignado
- CUANDO el usuario abre la comparación de mayo 2026
- ENTONCES "Ocio" aparece en la lista con la etiqueta "Sin presupuesto" y 45 € gastados

---

### Requisito: Widget de presupuestos en el dashboard

El widget DEBE mostrar siempre el resumen de presupuestos del **mes calendario actual**.
No existe selector de periodo en el widget.

| Situación | Comportamiento esperado |
|-----------|-------------------------|
| Con presupuestos activos | Porcentaje total consumido + recuento de presupuestos por estado |
| Sin presupuestos configurados | Estado vacío con llamada a la acción para crear el primero |

#### Escenario: Dashboard con presupuestos activos en el mes actual

- DADO que el mes actual es mayo 2026 y existen presupuestos con estados mixtos
- CUANDO el usuario abre el dashboard
- ENTONCES el widget muestra exclusivamente el resumen de mayo 2026 (no mes anterior ni futuro)

#### Escenario: Dashboard sin presupuestos configurados

- DADO que el usuario no ha creado ningún presupuesto
- CUANDO abre el dashboard
- ENTONCES el widget DEBE mostrar un estado vacío con llamada a la acción

---

## Fuera del alcance

Notificaciones push/email, burn rate, forecasting, rollover, plantillas de meses
anteriores, subcategorías, presupuestos compartidos.
