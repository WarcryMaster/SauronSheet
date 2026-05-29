# Propuesta: Consolidar presupuestos mensuales por categoría

## Intención

SauronSheet ya importa extractos y muestra analytics históricos. La funcionalidad de budgets existe en el código pero carece de definición de producto en OpenSpec. Esta propuesta cierra esa brecha: formalizar los presupuestos mensuales por categoría como control accionable que convierte "qué gasté" en "cuánto me puedo permitir este mes".

## Alcance

### Dentro del alcance
- Gestión CRUD de presupuestos mensuales por categoría de gasto (create / edit / delete / list / detail)
- Cálculo: gasto actual, importe restante, porcentaje usado
- Semáforo con estados explícitos y criterios precisos: Green / Yellow / Red / Overage
- Texto "on track" redefinido con precisión (hoy es ambiguo en `GetBudgetSummaryForDashboardQueryHandler`)
- Comparación "budget vs actual" incluyendo categorías con gasto pero sin presupuesto
- Widget del dashboard: decisión explícita documentada — sigue siempre el mes actual (corregir ambigüedad en `Dashboard.cshtml.cs`)
- Cobertura E2E (flujos principales hoy no cubiertos por Playwright)

### Fuera del alcance
- Notificaciones push/email
- Burn rate y forecasting
- Rollover (arrastrar saldo al mes siguiente)
- Plantillas o copiar presupuesto del mes anterior
- Presupuestos por subcategoría o agrupaciones
- Presupuestos compartidos/sociales

## Capacidades

> Contrato con la fase de specs. No existe ningún spec vigente de budgets en `openspec/specs/` — la referencia funcional vive en `specs/phase-5/` (legado, fuera del flujo OpenSpec).

### Nuevas capacidades
- `monthly-budgets`: gestión y seguimiento de presupuestos mensuales por categoría — CRUD, semáforo Green/Yellow/Red/Overage, comparación budget-vs-actual y widget de dashboard.

### Capacidades modificadas
Ninguna.

## Enfoque

Consolidar el MVP existente bajo OpenSpec sin expansión de scope. El dominio (`Budget`, `BudgetService`), la persistencia Supabase, los handlers CQRS y las páginas Razor ya existen. El trabajo consiste en:

1. Crear el spec `monthly-budgets` como fuente de verdad del comportamiento esperado.
2. Corregir las dos ambigüedades detectadas: widget fijo al mes actual y definición de "on track".
3. Añadir cobertura E2E de los flujos principales.

## Áreas afectadas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `src/SauronSheet.Domain/Services/BudgetService.cs` | Modificado | Redefinir semáforo con criterios explícitos |
| `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandler.cs` | Modificado | Corregir lógica "on track" |
| `src/SauronSheet.Frontend/Pages/Dashboard.cshtml(.cs)` | Modificado | Documentar y fijar comportamiento del widget |
| `openspec/specs/monthly-budgets/spec.md` | Nuevo | Spec de la capacidad |
| `e2e/` | Nuevo | Tests Playwright para flujos de budgets |
| `tests/SauronSheet.Domain.Tests/*Budget*` | Verificado | Cobertura ≥ 80 % |
| `tests/SauronSheet.Application.Tests/Features/Budgets/*` | Verificado | Cobertura ≥ 70 % |

## Riesgos

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Categorización incorrecta invalida los budgets | Media | Documentar dependencia en spec; no bloquea propuesta |
| Ambigüedad en semáforo genera bugs sutiles | Alta | El spec define cada estado con condición y ejemplo concreto |
| Sin tests E2E actuales para budgets | Alta | Las tasks incluirán slice dedicada a cobertura E2E |
| Chained PR necesario si el scope crece | Media | Slices: (1) spec + dominio, (2) handlers + UI, (3) E2E |

## Plan de rollback

No hay migraciones nuevas en esta primera slice. Si hay regresión:

1. `git revert` de los cambios en `BudgetService.cs` y handlers afectados.
2. El spec queda archivado como evidencia — no se pierde contexto ni decisiones.
3. El resto de la app no depende de `BudgetService` para funcionar.

## Dependencias

- `openspec/specs/category-resolution` — los budgets dependen de categorías correctamente asignadas a las transacciones.

## Criterios de éxito

- [ ] Spec `monthly-budgets` creado y revisado en `openspec/specs/`
- [ ] Semáforo Green/Yellow/Red/Overage documentado y cubierto por tests Domain
- [ ] Widget del dashboard con comportamiento explícito (mes actual) y tests correspondientes
- [ ] "On track" redefinido, verificado en Domain + Application Tests
- [ ] Tests E2E cubren flujo crear / ver / comparar budget
- [ ] Cobertura Domain ≥ 80 % y Application ≥ 70 % después del cambio
