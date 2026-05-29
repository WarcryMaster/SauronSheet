## Exploration: clarify-budgets-feature

### Current State
El repositorio YA tiene una base funcional de budgets; no es una idea vacía ni algo solo planificado. Hay dominio (`Budget`, `BudgetService`, `IBudgetRepository`), persistencia Supabase con RLS y unicidad por usuario-categoría-mes (`supabase/migrations/20260101000006_create_budgets_table.sql`), CQRS para crear/editar/eliminar/listar/comparar (`src/SauronSheet.Application/Features/Budgets/*`), páginas Razor completas (`/budgets`, `/budgets/create`, `/budgets/edit/{id}`, `/budgets/detail/{id}`, `/budgets/comparison`) y widget en dashboard (`src/SauronSheet.Frontend/Pages/Dashboard.cshtml` y `.cs`). Además hay tests Domain y Application específicos para budgets.

Lo que NO está cerrado hoy es la definición de producto y la fuente de verdad SDD. En `openspec/specs/` no existe ningún spec vigente de budgets; la referencia funcional vive en documentos heredados `specs/phase-5/phase-5-spec.md` y `specs/phase-5/phase-5-plan.md`, fuera del flujo OpenSpec actual. Eso explica por qué la funcionalidad puede sentirse “pendiente”: el código existe, pero la propuesta de valor y el alcance vigente no están formalizados en OpenSpec.

Para SauronSheet, el valor real de budgets no es “otro CRUD”. La app ya importa extractos y enseña analytics históricos; budgets añade una capa de decisión: convertir “qué gasté” en “cuánto me puedo permitir gastar este mes por categoría”. Ese valor sí aparece en el código: la home promete “Budget Tracking”, el dashboard muestra estado de budgets, y la comparación revela categorías con gasto pero sin presupuesto (`GetBudgetVsActualQueryHandler`). Aun así, el producto actual tiene dos señales de ambigüedad: el widget del dashboard siempre usa el mes actual, no el filtro visible (`Dashboard.cshtml.cs`), y el resumen considera “on track” todo lo que no esté en overage, aunque esté en rojo (`GetBudgetSummaryForDashboardQueryHandler`).

### Affected Areas
- `src/SauronSheet.Domain/Entities/Budget.cs` — modelo de presupuesto mensual por categoría.
- `src/SauronSheet.Domain/Services/BudgetService.cs` — unicidad y semáforo Green/Yellow/Red/Overage.
- `src/SauronSheet.Domain/Repositories/IBudgetRepository.cs` — contrato de persistencia.
- `src/SauronSheet.Infrastructure/Persistence/SupabaseBudgetRepository.cs` — acceso Supabase/Postgrest.
- `supabase/migrations/20260101000006_create_budgets_table.sql` — tabla `budgets`, `UNIQUE(user_id, category_id, period_start)` y RLS.
- `src/SauronSheet.Application/Features/Budgets/*` — CRUD, detalle, dashboard summary y budget-vs-actual.
- `src/SauronSheet.Frontend/Pages/Budgets/*` — UI de gestión y comparación.
- `src/SauronSheet.Frontend/Pages/Dashboard.cshtml(.cs)` — widget de valor inmediato.
- `tests/SauronSheet.Domain.Tests/*Budget*` y `tests/SauronSheet.Application.Tests/Features/Budgets/*` — cobertura existente.
- `specs/phase-5/phase-5-spec.md` — definición heredada/draft; `openspec/specs/` no tiene spec vigente de budgets.

### Approaches
1. **No hacer budgets todavía / aplazar** — dejar budgets fuera por ahora y centrarse en analytics/importación.
   - Pros: evita invertir en una funcionalidad cuyo “para qué” aún no está cerrado.
   - Cons: deja incoherente el producto actual, porque home, README, navegación y dashboard YA hablan de budgets; además seguirías teniendo código y UI de budgets sin una dirección clara.
   - Effort: Low

2. **Presupuestos mensuales simples por categoría** — fijar un límite mensual por categoría y comparar presupuesto frente a gasto real.
   - Pros: encaja con la arquitectura y con lo que YA existe; aporta valor directo en una app de finanzas personal porque permite decidir dónde frenar gasto este mes, no solo analizar el pasado; usa datos y categorías ya presentes; permite medir utilidad con métricas simples: budgets creados, categorías con gasto sin budget, budgets over limit y desviación total budget vs actual.
   - Cons: no anticipa ritmo de gasto ni ofrece previsión; depende mucho de que la categorización sea correcta; requiere definir mejor qué significa “on track”.
   - Effort: Low

3. **Budgeting más rico** — añadir alertas, rollover, forecasting y progreso más inteligente.
   - Pros: convierte budgets en una herramienta más accionable y diferenciadora; responde mejor a “¿voy bien para fin de mes?”.
   - Cons: hoy abre demasiadas decisiones a la vez: canales de alerta, reglas de rollover, exactitud del forecast y UX adicional; aumenta mucho el scope y el riesgo sin tener antes una definición simple y estable de valor.
   - Effort: High

### Recommendation
La mejor primera slice es **Option B: presupuestos mensuales simples por categoría**, pero planteada como **consolidación del MVP que el repositorio ya tiene**, no como una expansión ambiciosa.

**Por qué**: SauronSheet ya resuelve importación, categorización y analytics históricos. Lo que falta para que budgets tenga sentido es cerrar una promesa de producto simple y útil: “defino límites mensuales por categoría y veo, durante el mes, si mis gastos reales van dentro o fuera de lo planificado”. Eso sí habilita decisiones concretas: recortar una categoría, detectar gasto sin plan, comparar presupuesto vs realidad y revisar rápidamente el estado mensual desde dashboard.

**In scope recomendado para la propuesta**:
- presupuesto mensual por categoría de gasto;
- create/edit/delete + listado + detalle;
- cálculo de gasto actual, restante, porcentaje usado y estado;
- comparación “budget vs actual” incluyendo categorías con gasto pero sin budget;
- definición clara de semáforo y del texto “on track”;
- decisión explícita sobre si el widget sigue siempre el mes actual o el filtro visible.

**Out of scope inicial**:
- push/email notifications;
- burn rate;
- rollover;
- forecasting;
- plantillas/copiar mes anterior;
- budgets por subcategoría o agrupaciones;
- shared/social budgets.

### Risks
- La utilidad depende de la calidad de categorización: si las transacciones caen en categorías incorrectas, el budget pierde credibilidad.
- Hay ambigüedad de producto en el dashboard: hoy el widget usa mes actual fijo y “on track” no distingue rojo de verde.
- No he encontrado specs OpenSpec vigentes ni tests E2E específicos de budgets; el riesgo es seguir ampliando una feature sin fuente de verdad actual ni cobertura end-to-end.
- Un scope rico (Option C) probablemente rompería el presupuesto de revisión de 400 líneas y exigiría chained PR desde el principio.

### Ready for Proposal
Yes — la propuesta debería arrancar desde una premisa clara: **budgets en SauronSheet sirve para convertir analytics históricos en control mensual accionable por categoría**, y la primera slice debe limitarse a ese valor básico antes de añadir inteligencia avanzada.
