## Exploration: budgets-follow-up-quality-gaps

### Current State
La warning de cobertura SIGUE viva y está verificada con evidencia actual, no solo con el archivo histórico. Al ejecutar `dotnet test tests/SauronSheet.Domain.Tests --collect:"XPlat Code Coverage"`, el reporte generado en `test-results/dotnet/8978e5fc-1b04-4c3f-9702-eeb851df222c/coverage.cobertura.xml` deja Domain en **432/553 líneas cubiertas = 78,11 %**. Para llegar al 80 % hacen falta **11 líneas cubiertas adicionales** si el denominador no cambia.

Los candidatos con menor ruido en ese mismo reporte son: `DuplicateEntityException` (**0/9**), `BankCategoryTranslation` dentro de `IBankCategoryTranslationRepository.cs` (**0/5**), `TransactionByMultipleImportedFromsSpecification` (**0/5**), `StatementParseResult` + `StatementParseRowError` (**0/10**), `RawTransactionRow` (**0/11**) y `UserProfile` (**0/14**). Eso significa que NO hace falta tocar `Money` ni reabrir entidades grandes para pasar el umbral.

La warning E2E también está respaldada por código real. `e2e/tests/03-budgets.spec.ts` hace `skip` cuando: (a) el usuario autenticado no tiene categorías en `/budgets/create`, (b) no hay gasto del mes actual para `/budgets/comparison`, o (c) el dashboard entra en empty state y no renderiza el widget de budgets. Esto NO es casualidad: `Create.cshtml.cs` carga categorías vía `GetCategoriesQuery`, el handler usa `ICategoryRepository.GetByUserIdAsync(userId)`, y `SupabaseCategoryRepository.GetByUserIdAsync` devuelve solo categorías del usuario. Las categorías globales del sistema creadas en `supabase/migrations/20260101000007_system_categories_global_scope.sql` NO alimentan ese dropdown.

Además, los datos que necesitan TC-B02 y TC-B03 dependen de transacciones reales del mes actual. `GetBudgetVsActualQueryHandler` solo muestra gasto negativo agrupado por categoría, y `Dashboard.cshtml` solo renderiza el widget de budget cuando `Summary.TransactionCount > 0`. O sea: para que budgets E2E dejen de saltarse, el usuario de prueba necesita **categorías propias** y **al menos una transacción de gasto del mes actual**. Para que TC-B02 sea fiable tras TC-B01, lo más seguro es tener **dos categorías propias** y dejar una con gasto pero sin budget.

Hay un dato importante de arquitectura de entorno: `src/SauronSheet.Frontend/appsettings.json` apunta por defecto al proyecto alojado `https://zoebndeleapdejmqznif.supabase.co`, `e2e/playwright.config.ts` solo arranca la web local, y en el repo NO existe `supabase/seed.sql`. Por tanto, un seed local de Supabase por sí solo NO arregla el flujo actual de Playwright. La recomendación archivada de “meter seed data en migración” no es la vía más segura para este repo tal y como está cableado hoy.

### Affected Areas
- `test-results/dotnet/8978e5fc-1b04-4c3f-9702-eeb851df222c/coverage.cobertura.xml` — baseline real de cobertura Domain y clases 0 %.
- `src/SauronSheet.Domain/Exceptions/DuplicateEntityException.cs` — **0/9**; candidato más barato para sumar cobertura.
- `src/SauronSheet.Domain/Repositories/IBankCategoryTranslationRepository.cs` — `BankCategoryTranslation` está a **0/5**; segundo candidato barato.
- `src/SauronSheet.Domain/Specifications/TransactionByMultipleImportedFromsSpecification.cs` — **0/5**; margen extra simple si hiciera falta.
- `src/SauronSheet.Domain/ValueObjects/UserProfile.cs` — **0/14**; cierre completo posible en un fichero pequeño, aunque más ancho que los dos anteriores.
- `e2e/tests/03-budgets.spec.ts` — contiene la estrategia de auth y los `skip` por falta de datos.
- `src/SauronSheet.Frontend/Pages/Budgets/Create.cshtml.cs` — prueba que el alta de budget depende de categorías propias visibles.
- `src/SauronSheet.Application/Features/Categories/Queries/GetCategoriesQueryHandler.cs` — obtiene categorías del usuario actual.
- `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs` — excluye categorías globales del sistema del listado de usuario.
- `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetVsActualQueryHandler.cs` — requiere gasto negativo categorizado para mostrar comparación útil.
- `src/SauronSheet.Frontend/Pages/Dashboard.cshtml(.cs)` — el widget de budgets desaparece en empty state si no hay transacciones.
- `src/SauronSheet.Frontend/Pages/Transactions/Add.cshtml(.cs)` — flujo existente para crear transacciones desde UI, reutilizable por helper E2E.
- `src/SauronSheet.Frontend/Pages/Categories/Index.cshtml(.cs)` — flujo existente para crear categorías desde UI, reutilizable por helper E2E.
- `supabase/config.toml` — hoy no define seeds; no existe patrón local ya montado para `supabase/seed.sql`.
- `test.runsettings` y `e2e/playwright.config.ts` — ya centralizan artefactos en `test-results/`; no hace falta abrir otro frente de configuración.

### Approaches
1. **Un único cambio y un único PR** — cerrar cobertura y E2E en el mismo lote.
   - Pros: una sola propuesta y un solo cierre funcional.
   - Cons: mezcla dos problemas distintos (cobertura Domain y datos E2E/Supabase); empeora claridad de review aunque no parezca grande en líneas.
   - Effort: Medium

2. **Un único cambio OpenSpec con dos slices apiladas** — mantener `budgets-follow-up-quality-gaps`, pero entregar PR1 cobertura y PR2 datos/helper E2E.
   - Pros: respeta que ambas tareas nacen de las dos warnings del mismo archive; encaja bien con `force-chained` y `stacked-to-main`; mantiene reviews pequeñas y nítidas.
   - Cons: sigue agrupando dos temas técnicamente independientes bajo un mismo change.
   - Effort: Low/Medium

3. **Dos cambios OpenSpec separados y secuenciados** — un change para cobertura y otro para datos E2E.
   - Pros: máxima separación conceptual y auditoría más estricta.
   - Cons: más overhead SDD para dos arreglos pequeños; rompe la intención del follow-up unificado ya sugerido para estas warnings.
   - Effort: Medium

### Recommendation
Recomiendo **la opción 2: un único change `budgets-follow-up-quality-gaps`, pero con dos slices encadenadas**.

La slice 1 debe ser **solo cobertura Domain**. El camino más corto y menos ruidoso es empezar por:

- `DuplicateEntityException.cs` (**0/9**) — tests de los tres constructores y del mensaje/inner exception.
- `IBankCategoryTranslationRepository.cs` (`BankCategoryTranslation`, **0/5**) — test de construcción/equality del record.

Solo con esos dos objetivos ya hay margen teórico de **+14 líneas**, suficiente para pasar de **432** a al menos **446** líneas cubiertas sobre **553**, por encima del 80 %. Si el mapeo de Coverlet no diera todo ese rendimiento, el tercer candidato de respaldo debe ser `TransactionByMultipleImportedFromsSpecification.cs` (**0/5**), no `Money` ni otras clases más ruidosas.

La slice 2 debe ser **preparación de datos E2E budgets**, pero **NO mediante migración de producto**. La evidencia del repo empuja a otra decisión: el frontend de desarrollo apunta al Supabase alojado, no hay `supabase/seed.sql`, y `public.users.id` referencia `auth.users(id)`. Meter datos de `e2e@saurontest.local` en una migración mezclaría test data con el entorno real o dependería de un usuario Auth específico. La vía más segura y alineada con el patrón existente es un **helper/fixture de Playwright en `e2e/fixtures/`** que, para el usuario autenticado de la prueba, garantice antes del suite:

- **2 categorías personales** deterministas,
- **1 transacción negativa del mes actual** ligada a la segunda categoría,
- **0 budgets previos** sobre esa categoría con gasto, para que TC-B02 pueda seguir verificando “No budget”.

Eso deja el flujo robusto tanto para el usuario seeded como para el usuario inyectado por `TEST_USER_EMAIL/TEST_USER_PASSWORD`, sin contaminar migraciones de negocio.

### Risks
- El cálculo de “+11 líneas” depende del snapshot actual de Coverlet; si otra clase Domain cambia antes de aplicar la slice 1, hay que rerunear cobertura antes de cerrar la propuesta.
- Un helper E2E basado en UI puede ser frágil si se apoya en selectores inestables; necesita apoyarse en rutas y elementos ya existentes o introducir selectores estables en una fase posterior.
- Sembrar datos de `e2e@saurontest.local` mediante migración es arriesgado en este repo porque `public.users` depende de `auth.users` y el entorno local/CI no está aislado del proyecto alojado por defecto.
- TC-B01 selecciona la primera categoría válida del dropdown; si los datos de prueba no son deterministas, TC-B02 puede perder la categoría “sin presupuesto”.

### Ready for Proposal
Yes — la propuesta debería abrir **un único change con dos work units apiladas**: primero cobertura Domain mínima y verificable; después helper/fixture E2E para asegurar categorías y transacciones del usuario de prueba, dejando explícito que **no** se usará una migración de producto para sembrar esos datos.
