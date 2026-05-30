# Especificación: Políticas de Presupuesto Permanentes

## Propósito

Gestión de presupuestos como políticas permanentes con granularidad configurable.
Un presupuesto define un límite de gasto por período (mensual, trimestral, semestral
o anual) que aplica de forma continua desde su fecha de inicio hasta que se desactive
o se modifique.

---

## Requisitos

### Requisito: Entidad Budget como política permanente

El sistema DEBE modelar `Budget` como aggregate root con los siguientes atributos:

| Atributo | Tipo | Descripción |
|----------|------|-------------|
| `Id` | `BudgetId` (strong-typed) | Identificador único |
| `UserId` | `UserId` | Propietario del presupuesto |
| `CategoryId` | `CategoryId` | Categoría asociada |
| `EffectiveFrom` | `DateOnly` | Fecha desde la que aplica |
| `EffectiveUntil` | `DateOnly?` | Fecha hasta la que aplica (null = permanente) |
| `PeriodGranularity` | `BudgetPeriod` (enum) | Monthly, Quarterly, Semester, Annual |
| `Limit` | `Money` | Límite por período de la granularidad elegida |

#### Escenario: Crear presupuesto permanente válido

- DADO que el usuario está autenticado y la categoría "Alimentación" existe
- CUANDO crea un presupuesto con `EffectiveFrom = 2026-01-01`, `PeriodGranularity = Monthly`, `Limit = 500 €`
- ENTONCES el presupuesto se guarda con `EffectiveUntil = null` y aplica a todos los períodos futuros

#### Escenario: Crear presupuesto con fecha de fin

- DADO que el usuario está autenticado
- CUANDO crea un presupuesto con `EffectiveFrom = 2026-01-01`, `EffectiveUntil = 2026-12-31`
- ENTONCES el presupuesto aplica exclusivamente durante el año 2026

#### Escenario: Límite debe ser positivo

- DADO que el usuario intenta crear un presupuesto
- CUANDO el `Limit` es cero o negativo
- ENTONCES el sistema DEBE rechazar la operación con error de validación

#### Escenario: EffectiveUntil anterior a EffectiveFrom

- DADO que el usuario intenta crear un presupuesto
- CUANDO `EffectiveUntil` es anterior a `EffectiveFrom`
- ENTONCES el sistema DEBE rechazar la operación con error de validación

---

### Requisito: Unicidad usuario + categoría (sin solapamiento temporal)

El sistema DEBE garantizar que para un mismo usuario y categoría, no existan
dos presupuestos cuyos rangos de vigencia se solapen. Solo puede haber un
presupuesto activo por categoría en cualquier punto del tiempo.

#### Escenario: Crear segundo presupuesto para la misma categoría sin solapamiento

- DADO un presupuesto activo para "Alimentación" con `EffectiveUntil = 2026-06-30`
- CUANDO el usuario crea otro para "Alimentación" con `EffectiveFrom = 2026-07-01`
- ENTONCES el sistema DEBE permitir la creación (no hay solapamiento)

#### Escenario: Crear segundo presupuesto con solapamiento temporal

- DADO un presupuesto activo para "Alimentación" con `EffectiveFrom = 2026-01-01`, `EffectiveUntil = null`
- CUANDO el usuario intenta crear otro para "Alimentación" con `EffectiveFrom = 2026-06-01`
- ENTONCES el sistema DEBE rechazar la operación con error de solapamiento

#### Escenario: Constraint UNIQUE en base de datos

- DADO que la capa de dominio valida la unicidad
- CUANDO por error de concurrencia se insertan dos presupuestos solapados
- ENTONCES la base de datos DEBE tener un constraint que impida la inserción duplicada

---

### Requisito: Ciclo de vida del presupuesto

El sistema DEBE soportar las siguientes operaciones sobre presupuestos existentes:

| Operación | Campos modificables | Restricción |
|-----------|---------------------|-------------|
| Actualizar límite | `Limit` | El nuevo límite DEBE ser positivo |
| Actualizar período | `PeriodGranularity` | Recalcula métricas de períodos históricos |
| Actualizar fechas | `EffectiveFrom`, `EffectiveUntil` | No debe generar solapamiento |
| Desactivar | `EffectiveUntil` = fecha actual o pasada | El presupuesto deja de aplicar a períodos futuros |

#### Escenario: Actualizar límite de un presupuesto activo

- DADO un presupuesto activo con `Limit = 500 €`
- CUANDO el usuario actualiza el límite a `600 €`
- ENTONCES el nuevo límite aplica a TODOS los períodos (pasados y futuros) en el cálculo de métricas

#### Escenario: Desactivar presupuesto permanente

- DADO un presupuesto con `EffectiveUntil = null` (permanente)
- CUANDO el usuario lo desactiva con `EffectiveUntil = 2026-05-30`
- ENTONCES el presupuesto deja de aplicar a períodos posteriores al 30 de mayo de 2026

#### Escenario: Cambio de granularidad

- DADO un presupuesto mensual con `Limit = 500 €`
- CUANDO el usuario cambia la granularidad a `Annual` con `Limit = 6000 €`
- ENTONCES el sistema actualiza la granularidad y el límite por período

---

### Requisito: Eliminación de presupuesto

El sistema DEBE permitir eliminar un presupuesto. La eliminación es física
(permanente) y no afecta a las transacciones existentes.

#### Escenario: Eliminar presupuesto activo

- DADO un presupuesto activo para "Alimentación"
- CUANDO el usuario lo elimina
- ENTONCES el presupuesto desaparece y la categoría queda sin política de presupuesto

#### Escenario: Eliminar presupuesto histórico

- DADO un presupuesto con `EffectiveUntil` en el pasado (ya desactivado)
- CUANDO el usuario lo elimina
- ENTONCES el presupuesto se elimina físicamente del sistema

---

## Fuera del alcance

Notificaciones, alertas, forecasting, rollover de excedentes entre períodos,
subcategorías de presupuesto, presupuestos compartidos entre usuarios.
